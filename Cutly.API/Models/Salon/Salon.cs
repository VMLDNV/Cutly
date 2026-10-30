using BusinessEntity = Cutly.API.Models.Business.Business;
using NetTopologySuite.Geometries;

namespace Cutly.API.Models.Salon;

public class Salon
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string City { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }
    public string?  ImageUrl { get; set; }

    public int BusinessId { get; set; }

    public BusinessEntity Business { get; set; } = null!;

    // Geographic location: longitude and latitude
    public Point? Location { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
