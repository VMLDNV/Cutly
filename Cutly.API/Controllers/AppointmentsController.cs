using System.Security.Claims;
using Cutly.API.Data;
using Cutly.API.DTOs.Appointments;
using Cutly.API.Models.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cutly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    private const int SlotIntervalMinutes = 30;

    public AppointmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreateAppointment(
    [FromBody] CreateAppointmentRequest request)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();

        // Получаваме избрания час в местната часова зона на сървъра.
        var localStart = TimeZoneInfo.ConvertTime(
            request.StartDateTime, TimeZoneInfo.Local);

        var localDate = DateOnly.FromDateTime(localStart.DateTime);

        if (localStart <= TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.Local))
            return BadRequest("Не можете да резервирате час в миналото.");

        var startUtc = request.StartDateTime.UtcDateTime;

        await using var transaction = await _context.Database
            .BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        try
        {
            // Проверяваме дали услугата и служителят са активни.
            var employeeService = await _context.EmployeeServices
                .Include(es => es.SalonEmployee)
                    .ThenInclude(se => se.Salon)
                .Include(es => es.Service)
                .FirstOrDefaultAsync(es =>
                    es.Id == request.EmployeeServiceId &&
                    es.IsActive &&
                    es.SalonEmployee.IsActive &&
                    es.SalonEmployee.Salon.IsActive &&
                    es.Service.IsActive);

            if (employeeService == null)
                return NotFound("Избраната услуга не е намерена или не е активна.");

            var duration = employeeService.DurationMinutes;

            if (duration <= 0)
                return BadRequest("Невалидна продължителност на услугата.");

            var endUtc = startUtc.AddMinutes(duration);

            // Проверяваме работното време.
            int dayOfWeek = ((int)localDate.DayOfWeek + 6) % 7 + 1;

            var workingHour = await _context.EmployeeWorkingHours
                .FirstOrDefaultAsync(wh =>
                    wh.SalonEmployeeId == employeeService.SalonEmployeeId &&
                    wh.DayOfWeek == dayOfWeek);

            if (workingHour == null || workingHour.IsDayOff)
                return Conflict("Служителят не работи на избрания ден.");

            var localWorkStart = localDate.ToDateTime(
                workingHour.StartTime);

            var localWorkEnd = localDate.ToDateTime(
                workingHour.EndTime);

            var workStartUtc = DateTime.SpecifyKind(
                localWorkStart, DateTimeKind.Local).ToUniversalTime();

            var workEndUtc = DateTime.SpecifyKind(
                localWorkEnd, DateTimeKind.Local).ToUniversalTime();

            if (startUtc < workStartUtc || endUtc > workEndUtc)
                return Conflict("Избраният час е извън работното време.");

            // Проверяваме дали часът е през 30-минутен интервал.
            var minutesFromWorkStart =
                (localStart.DateTime - localWorkStart).TotalMinutes;

            if (minutesFromWorkStart < 0 ||
                minutesFromWorkStart % 30 != 0)
            {
                return Conflict("Избраният час не е валиден интервал.");
            }

            // Проверяваме отсъствията на служителя.
            var hasTimeOff = await _context.EmployeeTimeOffs
                .AnyAsync(t =>
                    t.SalonEmployeeId == employeeService.SalonEmployeeId &&
                    t.StartDateTime < endUtc &&
                    t.EndDateTime > startUtc);

            if (hasTimeOff)
                return Conflict("Служителят не е свободен в този период.");

            // Проверяваме за друга резервация.
            var hasAppointment = await _context.Appointments
                .AnyAsync(a =>
                    a.SalonEmployeeId == employeeService.SalonEmployeeId &&
                    a.StartDateTime < endUtc &&
                    a.EndDateTime > startUtc &&
                    (a.Status == AppointmentStatus.Pending ||
                     a.Status == AppointmentStatus.Confirmed));

            if (hasAppointment)
                return Conflict("Този час вече е резервиран.");

            // Създаваме резервацията.
            var appointment = new Appointment
            {
                CustomerId = customerId,
                SalonEmployeeId = employeeService.SalonEmployeeId,
                EmployeeServiceId = employeeService.Id,
                StartDateTime = startUtc,
                EndDateTime = endUtc,
                Price = employeeService.Price,
                Status = AppointmentStatus.Pending,
                CustomerNotes = request.CustomerNotes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                appointment.Id,
                appointment.StartDateTime,
                appointment.EndDateTime,
                appointment.Price,
                Status = appointment.Status.ToString(),
                Message = "Резервацията е създадена успешно."
            });
        }
        catch (PostgresException ex)
            when (ex.SqlState == "40001")
        {
            await transaction.RollbackAsync();

            return Conflict(
                "Часът беше резервиран от друг потребител. Моля, изберете друг.");
        }
    }


    private async Task<IActionResult> UpdateManagementStatusAsync(
        int appointmentId,
        AppointmentStatus newStatus)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var appointment = await _context.Appointments
            .Include(a => a.SalonEmployee)
                .ThenInclude(se => se.Salon)
                    .ThenInclude(s => s.Business)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            return NotFound("Резервацията не е намерена.");

        // Проверяваме дали потребителят е служителят за тази резервация
        // или собственикът на бизнеса, към който принадлежи салонът.
        bool isEmployee =
            User.IsInRole("Employee") &&
            appointment.SalonEmployee.EmployeeId == userId;

        bool isBusinessOwner =
            User.IsInRole("BusinessOwner") &&
            appointment.SalonEmployee.Salon.Business.OwnerId == userId;

        if (!isEmployee && !isBusinessOwner)
            return Forbid();

        if (appointment.Status != AppointmentStatus.Pending)
        {
            return Conflict(
                "Само резервации със статус Pending могат да бъдат потвърждавани или отказвани.");
        }

        appointment.Status = newStatus;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            appointment.Id,
            Status = appointment.Status.ToString(),
            Message = newStatus == AppointmentStatus.Confirmed
                ? "Резервацията е потвърдена успешно."
                : "Резервацията е отказана успешно."
        });
    }

    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = "Employee,BusinessOwner")]
    public async Task<IActionResult> ConfirmAppointment(int id)
    {
        return await UpdateManagementStatusAsync(
            id,
            AppointmentStatus.Confirmed);
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Employee,BusinessOwner")]
    public async Task<IActionResult> RejectAppointment(int id)
    {
        return await UpdateManagementStatusAsync(
            id,
            AppointmentStatus.Rejected);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        var customerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return NotFound("Резервацията не е намерена.");

        if (appointment.CustomerId != customerId)
            return Forbid();

        if (appointment.Status != AppointmentStatus.Pending &&
            appointment.Status != AppointmentStatus.Confirmed)
        {
            return Conflict(
                "Тази резервация вече не може да бъде отменена.");
        }

        appointment.Status = AppointmentStatus.Cancelled;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            appointment.Id,
            Status = appointment.Status.ToString(),
            Message = "Резервацията е отменена успешно."
        });
    }


    // GET: api/appointments/available-slots
    [HttpGet("available-slots")]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] AvailableSlotsRequest request)
    {
        if (request.EmployeeServiceId <= 0)
        {
            return BadRequest(new
            {
                message = "Невалидна услуга."
            });
        }

        if (request.Date < DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest(new
            {
                message = "Не може да изберете дата в миналото."
            });
        }

        var employeeService = await _context.EmployeeServices
            .AsNoTracking()
            .Include(es => es.SalonEmployee)
            .Include(es => es.Service)
            .FirstOrDefaultAsync(es =>
                es.Id == request.EmployeeServiceId &&
                es.IsActive &&
                es.SalonEmployee.IsActive &&
                es.SalonEmployee.Salon.IsActive &&
                es.Service.IsActive);

        if (employeeService == null)
        {
            return NotFound(new
            {
                message = "Услугата не беше намерена."
            });
        }

        var date = request.Date.ToDateTime(TimeOnly.MinValue);

        // Monday = 1, Sunday = 7
        int dayOfWeek = ((int)request.Date.DayOfWeek + 6) % 7 + 1;

        var workingHour = await _context.EmployeeWorkingHours
            .AsNoTracking()
            .FirstOrDefaultAsync(wh =>
                wh.SalonEmployeeId == employeeService.SalonEmployeeId &&
                wh.DayOfWeek == dayOfWeek);

        if (workingHour == null || workingHour.IsDayOff)
        {
            return Ok(new List<AvailableSlotDto>());
        }

        var duration = employeeService.DurationMinutes;

        if (duration <= 0)
        {
            return BadRequest(new
            {
                message = "Невалидна продължителност на услугата."
            });
        }

        var localWorkStart = date.Add(workingHour.StartTime.ToTimeSpan());
        var localWorkEnd = date.Add(workingHour.EndTime.ToTimeSpan());

        // Преобразуваме местното време към UTC.
        var workStart = DateTime.SpecifyKind(
            localWorkStart, DateTimeKind.Local).ToUniversalTime();

        var workEnd = DateTime.SpecifyKind(
            localWorkEnd, DateTimeKind.Local).ToUniversalTime();

        if (workEnd <= workStart)
        {
            return Ok(new List<AvailableSlotDto>());
        }

        var existingAppointments = await _context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.SalonEmployeeId == employeeService.SalonEmployeeId &&
                a.StartDateTime < workEnd &&
                a.EndDateTime > workStart &&
                (a.Status == AppointmentStatus.Pending ||
                 a.Status == AppointmentStatus.Confirmed))
            .Select(a => new
            {
                a.StartDateTime,
                a.EndDateTime
            })
            .ToListAsync();

        var timeOffs = await _context.EmployeeTimeOffs
            .AsNoTracking()
            .Where(t =>
                t.SalonEmployeeId == employeeService.SalonEmployeeId &&
                t.StartDateTime < workEnd &&
                t.EndDateTime > workStart)
            .Select(t => new
            {
                t.StartDateTime,
                t.EndDateTime
            })
            .ToListAsync();

        var now = DateTime.UtcNow;

        var slots = new List<AvailableSlotDto>();

        for (var start = workStart;
             start.AddMinutes(duration) <= workEnd;
             start = start.AddMinutes(SlotIntervalMinutes))
        {
            var end = start.AddMinutes(duration);

            // Не предлагаме часове в миналото.
            if (start <= now)
                continue;

            // Проверяваме за застъпване с резервации.
            bool hasAppointmentConflict = existingAppointments.Any(a =>
                start < a.EndDateTime &&
                end > a.StartDateTime);

            if (hasAppointmentConflict)
                continue;

            // Проверяваме за застъпване с отсъствия.
            bool hasTimeOffConflict = timeOffs.Any(t =>
                start < t.EndDateTime &&
                end > t.StartDateTime);

            if (hasTimeOffConflict)
                continue;

            slots.Add(new AvailableSlotDto
            {
                StartDateTime = start,
                EndDateTime = end
            });
        }

        return Ok(slots);
    }


    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyAppointments()
    {
        var customerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.StartDateTime)
            .Select(a => new
            {
                a.Id,

                SalonName = a.SalonEmployee.Salon.Name,
                City = a.SalonEmployee.Salon.City,
                Address = a.SalonEmployee.Salon.Address,

                EmployeeFirstName =
                    a.SalonEmployee.Employee.FirstName,

                EmployeeLastName =
                    a.SalonEmployee.Employee.LastName,

                ServiceName = a.EmployeeService.Service.Name,

                a.Price,
                DurationMinutes =
                    a.EmployeeService.DurationMinutes,

                a.StartDateTime,
                a.EndDateTime,
                a.Status,
                a.CustomerNotes
            })
            .ToListAsync();

        var result = appointments.Select(a => new MyAppointmentDto
        {
            Id = a.Id,

            SalonName = a.SalonName,
            City = a.City,
            Address = a.Address,

            EmployeeName =
                $"{a.EmployeeFirstName} {a.EmployeeLastName}".Trim(),

            ServiceName = a.ServiceName,

            Price = a.Price,
            DurationMinutes = a.DurationMinutes,

            StartDateTime = a.StartDateTime,
            EndDateTime = a.EndDateTime,

            Status = a.Status.ToString(),

            CustomerNotes = a.CustomerNotes
        }).ToList();

        return Ok(result);
    }

}
