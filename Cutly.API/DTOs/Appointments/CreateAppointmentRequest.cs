using System.ComponentModel.DataAnnotations;

namespace Cutly.API.DTOs.Appointments;

public class CreateAppointmentRequest
{
    [Range(1, int.MaxValue)]
    public int EmployeeServiceId { get; set; }

    [Required]
    public DateTimeOffset StartDateTime { get; set; }

    [StringLength(500)]
    public string? CustomerNotes { get; set; }
}
