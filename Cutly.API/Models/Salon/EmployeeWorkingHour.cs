namespace Cutly.API.Models.Salon;

public class EmployeeWorkingHour
{
    public int Id { get; set; }

    public int SalonEmployeeId { get; set; }

    public SalonEmployee SalonEmployee { get; set; } = null!;

    // Monday = 1, Sunday = 7
    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsDayOff { get; set; } = false;
}
