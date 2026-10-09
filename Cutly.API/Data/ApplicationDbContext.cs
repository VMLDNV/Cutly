using Cutly.API.Models.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Cutly.API.Models.Business;
using Cutly.API.Models.Salon;
using Cutly.API.Models.Appointments;
using NetTopologySuite.Geometries;

namespace Cutly.API.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Salon> Salons => Set<Salon>();
    public DbSet<SalonEmployee> SalonEmployees => Set<SalonEmployee>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<EmployeeService> EmployeeServices => Set<EmployeeService>();
    public DbSet<EmployeeWorkingHour> EmployeeWorkingHours => Set<EmployeeWorkingHour>();
    public DbSet<EmployeeTimeOff> EmployeeTimeOffs => Set<EmployeeTimeOff>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Salon>()
            .Property(s => s.Location)
            .HasColumnType("geography (point, 4326)");

        builder.Entity<SalonEmployee>()
            .HasOne(se => se.Salon)
            .WithMany()
            .HasForeignKey(se => se.SalonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SalonEmployee>()
            .HasOne(se => se.Employee)
            .WithMany()
            .HasForeignKey(se => se.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SalonEmployee>()
            .HasIndex(se => new { se.SalonId, se.EmployeeId })
            .IsUnique();

        builder.Entity<EmployeeService>()
            .HasOne(es => es.SalonEmployee)
            .WithMany()
            .HasForeignKey(es => es.SalonEmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EmployeeService>()
            .HasOne(es => es.Service)
            .WithMany()
            .HasForeignKey(es => es.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<EmployeeService>()
            .HasIndex(es => new { es.SalonEmployeeId, es.ServiceId })
            .IsUnique();

        builder.Entity<EmployeeService>()
            .Property(es => es.Price)
            .HasPrecision(10, 2);

        builder.Entity<EmployeeService>()
            .ToTable("EmployeeServices", table =>
            {
                table.HasCheckConstraint(
                    "CK_EmployeeServices_Price",
                    "\"Price\" >= 0");

                table.HasCheckConstraint(
                    "CK_EmployeeServices_DurationMinutes",
                    "\"DurationMinutes\" > 0");
            });
        builder.Entity<EmployeeWorkingHour>()
            .HasOne(wh => wh.SalonEmployee)
            .WithMany()
            .HasForeignKey(wh => wh.SalonEmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EmployeeWorkingHour>()
            .HasIndex(wh => new
            {
                wh.SalonEmployeeId,
                wh.DayOfWeek
            })
            .IsUnique();

        builder.Entity<EmployeeWorkingHour>()
            .ToTable("EmployeeWorkingHours", table =>
            {
                table.HasCheckConstraint(
                    "CK_EmployeeWorkingHours_DayOfWeek",
                    "\"DayOfWeek\" BETWEEN 1 AND 7");

                table.HasCheckConstraint(
                    "CK_EmployeeWorkingHours_TimeRange",
                    "\"IsDayOff\" = TRUE OR \"EndTime\" > \"StartTime\"");
            });

        builder.Entity<EmployeeTimeOff>()
            .HasOne(to => to.SalonEmployee)
            .WithMany()
            .HasForeignKey(to => to.SalonEmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EmployeeTimeOff>()
            .ToTable("EmployeeTimeOffs", table =>
            {
                table.HasCheckConstraint(
                    "CK_EmployeeTimeOffs_DateRange",
                    "\"EndDateTime\" > \"StartDateTime\"");
            });

        builder.Entity<Appointment>()
            .HasOne(a => a.Customer)
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
            .HasOne(a => a.SalonEmployee)
            .WithMany()
            .HasForeignKey(a => a.SalonEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
            .HasOne(a => a.EmployeeService)
            .WithMany()
            .HasForeignKey(a => a.EmployeeServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
            .Property(a => a.Price)
            .HasPrecision(10, 2);

        builder.Entity<Appointment>()
            .ToTable("Appointments", table =>
            {
                table.HasCheckConstraint(
                    "CK_Appointments_DateRange",
                    "\"EndDateTime\" > \"StartDateTime\"");

                table.HasCheckConstraint(
                    "CK_Appointments_Price",
                    "\"Price\" >= 0");
            });

        builder.Entity<Appointment>()
            .HasIndex(a => new
            {
                a.SalonEmployeeId,
                a.StartDateTime
            });

    }
}
