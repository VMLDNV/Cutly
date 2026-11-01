namespace Cutly.API.DTOs.Appointments;

public class MyAppointmentDto
{
    public int Id { get; set; }

    public string SalonName { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int DurationMinutes { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? CustomerNotes { get; set; }
}
