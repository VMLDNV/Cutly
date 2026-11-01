using Cutly.API.Models.Salon;
using Cutly.API.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cutly.API.Data;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        // Намираме тестовия салон.
        var salon = await context.Salons
            .FirstOrDefaultAsync(s => s.Name == "Barber House");

        if (salon == null)
        {
            throw new Exception("Тестовият салон Barber House не е намерен.");
        }

        // Намираме тестовия служител.
        var employee = await userManager.FindByEmailAsync(
            "testowner@example.com");

        if (employee == null)
        {
            throw new Exception("Тестовият потребител не е намерен.");
        }

        // Създаваме служителя към салона.
        var salonEmployee = await context.SalonEmployees
            .FirstOrDefaultAsync(se =>
                se.SalonId == salon.Id &&
                se.EmployeeId == employee.Id);

        if (salonEmployee == null)
        {
            salonEmployee = new SalonEmployee
            {
                SalonId = salon.Id,
                EmployeeId = employee.Id,
                JobTitle = "Бръснар",
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            context.SalonEmployees.Add(salonEmployee);
            await context.SaveChangesAsync();
        }

        // Създаваме услугата.
        var service = await context.Services
            .FirstOrDefaultAsync(s =>
                s.Name == "Мъжко подстригване");

        if (service == null)
        {
            service = new Service
            {
                Name = "Мъжко подстригване",
                Description = "Класическо мъжко подстригване",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Services.Add(service);
            await context.SaveChangesAsync();
        }

        // Свързваме услугата със служителя.
        var employeeService = await context.EmployeeServices
            .FirstOrDefaultAsync(es =>
                es.SalonEmployeeId == salonEmployee.Id &&
                es.ServiceId == service.Id);

        if (employeeService == null)
        {
            employeeService = new EmployeeService
            {
                SalonEmployeeId = salonEmployee.Id,
                ServiceId = service.Id,
                Price = 25.00m,
                DurationMinutes = 60,
                IsActive = true
            };

            context.EmployeeServices.Add(employeeService);
            await context.SaveChangesAsync();
        }

        // Добавяме работно време:
        // Понеделник - Събота, 09:00 - 18:00.
        for (int day = 1; day <= 6; day++)
        {
            var workingHour = await context.EmployeeWorkingHours
                .FirstOrDefaultAsync(wh =>
                    wh.SalonEmployeeId == salonEmployee.Id &&
                    wh.DayOfWeek == day);

            if (workingHour == null)
            {
                context.EmployeeWorkingHours.Add(
                    new EmployeeWorkingHour
                    {
                        SalonEmployeeId = salonEmployee.Id,
                        DayOfWeek = day,
                        StartTime = new TimeOnly(9, 0),
                        EndTime = new TimeOnly(18, 0),
                        IsDayOff = false
                    });
            }
        }

        // Неделя - почивен ден.
        var sunday = await context.EmployeeWorkingHours
            .FirstOrDefaultAsync(wh =>
                wh.SalonEmployeeId == salonEmployee.Id &&
                wh.DayOfWeek == 7);

        if (sunday == null)
        {
            context.EmployeeWorkingHours.Add(
                new EmployeeWorkingHour
                {
                    SalonEmployeeId = salonEmployee.Id,
                    DayOfWeek = 7,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(18, 0),
                    IsDayOff = true
                });
        }

        await context.SaveChangesAsync();
    }
}
