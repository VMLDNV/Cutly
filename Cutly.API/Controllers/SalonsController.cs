using Cutly.API.Data;
using Cutly.API.Models.Salon;
using Cutly.API.Models.Appointments;
using Cutly.API.DTOs.Salons;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cutly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalonsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SalonsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/salons
    [HttpGet]
    public async Task<IActionResult> GetSalons()
    {
        var salons = await _context.Salons
            .AsNoTracking()
            .Select(salon => new
            {
                salon.Id,
                salon.Name,
                salon.Description,
                salon.Address,
                salon.City,
                salon.ImageUrl,

                AverageRating = _context.Reviews
                    .Where(review =>
                        review.Appointment.SalonEmployee.SalonId == salon.Id &&
                        review.Appointment.Status == AppointmentStatus.Completed)
                    .Select(review => (double?)review.Rating)
                    .Average(),

                ReviewCount = _context.Reviews
                    .Count(review =>
                        review.Appointment.SalonEmployee.SalonId == salon.Id &&
                        review.Appointment.Status == AppointmentStatus.Completed)
            })
            .ToListAsync();

        return Ok(salons);
    }

    // GET: api/salons/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSalonDetails(int id)
    {
        var salon = await _context.Salons
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SalonDetailsDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                City = s.City,
                Address = s.Address,
                PhoneNumber = s.PhoneNumber,
                Email = s.Email,
                ImageUrl = s.ImageUrl,

                AverageRating = _context.Reviews
                    .Where(review =>
                        review.Appointment.SalonEmployee.SalonId == s.Id &&
                        review.Appointment.Status == AppointmentStatus.Completed)
                    .Select(review => (double?)review.Rating)
                    .Average(),

                ReviewCount = _context.Reviews
                    .Count(review =>
                        review.Appointment.SalonEmployee.SalonId == s.Id &&
                        review.Appointment.Status == AppointmentStatus.Completed),

                Employees = _context.SalonEmployees
                    .Where(employee =>
                        employee.SalonId == s.Id &&
                        employee.IsActive)
                    .Select(employee => new SalonEmployeeDto
                    {
                        EmployeeId = employee.EmployeeId,

                        FullName = (
                            employee.Employee.FirstName + " " +
                            employee.Employee.LastName
                        ).Trim(),

                        JobTitle = employee.JobTitle,

                        Services = _context.EmployeeServices
                            .Where(es =>
                                es.SalonEmployeeId == employee.Id &&
                                es.IsActive &&
                                es.Service.IsActive)
                            .Select(es => new EmployeeServiceDto
                            {
                                EmployeeServiceId = es.Id,
                                ServiceId = es.ServiceId,
                                ServiceName = es.Service.Name,
                                Description = es.Service.Description,
                                Price = es.Price,
                                DurationMinutes = es.DurationMinutes
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (salon == null)
        {
            return NotFound(new
            {
                message = "Салонът не беше намерен."
            });
        }

        return Ok(salon);
    }

    // POST: api/salons
    [HttpPost]
    public async Task<IActionResult> CreateSalon(
        [FromBody] CreateSalonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.City))
        {
            return BadRequest(new
            {
                message = "Името на салона и градът са задължителни."
            });
        }

        var businessExists = await _context.Businesses
            .AnyAsync(b => b.Id == request.BusinessId);

        if (!businessExists)
        {
            return BadRequest(new
            {
                message = "Посоченият бизнес не съществува."
            });
        }

        var salon = new Salon
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            City = request.City.Trim(),
            Address = request.Address,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            BusinessId = request.BusinessId
        };

        _context.Salons.Add(salon);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetSalonDetails),
            new { id = salon.Id },
            new
            {
                salon.Id,
                salon.Name,
                salon.City,
                salon.Address,
                salon.BusinessId
            });
    }
}

public sealed class CreateSalonRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public int BusinessId { get; set; }
}
