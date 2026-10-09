namespace Cutly.API.Models.Salon;

public class EmployeeTimeOff
{
    public int Id { get; set; }

    public int SalonEmployeeId { get; set; }

    public SalonEmployee SalonEmployee { get; set; } = null!;

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
