using Cutly.API.Models.Salon;
using Cutly.API.Models.Users;

namespace Cutly.API.Models.Appointments;

public enum AppointmentStatus
{
    Pending = 1,
    Confirmed = 2,
    Completed = 3,
    Cancelled = 4,
    Rejected = 5,
    NoShow = 6
}

public class Appointment
{
    public int Id { get; set; }

    public string CustomerId { get; set; } = string.Empty;

    public ApplicationUser Customer { get; set; } = null!;

    public int SalonEmployeeId { get; set; }

    public SalonEmployee SalonEmployee { get; set; } = null!;

    public int EmployeeServiceId { get; set; }

    public EmployeeService EmployeeService { get; set; } = null!;

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    // Snapshot of the price when the booking was made
    public decimal Price { get; set; }

    public AppointmentStatus Status { get; set; }
        = AppointmentStatus.Pending;

    public string? CustomerNotes { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
