
using Cutly.API.Models.Users;

namespace Cutly.API.Models.Salon;

public class SalonEmployee
{
    public int Id { get; set; }

    public int SalonId { get; set; }

    public Salon Salon { get; set; } = null!;

    public string EmployeeId { get; set; } = string.Empty;

    public ApplicationUser Employee { get; set; } = null!;

    public string? JobTitle { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
