namespace Cutly.API.Models.Salon;

public class EmployeeService
{
    public int Id { get; set; }

    public int SalonEmployeeId { get; set; }

    public SalonEmployee SalonEmployee { get; set; } = null!;

    public int ServiceId { get; set; }

    public Service Service { get; set; } = null!;

    public decimal Price { get; set; }

    public int DurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;
}
