using System.ComponentModel.DataAnnotations;

namespace Cutly.API.DTOs.Appointments;

public class AvailableSlotsRequest
{
    [Range(1, int.MaxValue)]
    public int EmployeeServiceId { get; set; }

    [Required]
    public DateOnly Date { get; set; }
}
