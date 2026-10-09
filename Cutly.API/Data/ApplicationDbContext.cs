using Cutly.API.Models.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Cutly.API.Models.Business;
using Cutly.API.Models.Salon;
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
    }
}
