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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Salon>()
            .Property(s => s.Location)
            .HasColumnType("geography (point, 4326)");
    }
}
