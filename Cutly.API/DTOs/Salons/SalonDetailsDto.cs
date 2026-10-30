namespace Cutly.API.DTOs.Salons;

public class SalonDetailsDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? City { get; set; }

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? ImageUrl { get; set; }

    public double? AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public List<SalonEmployeeDto> Employees { get; set; } = new();
}

public class SalonEmployeeDto
{
    public string EmployeeId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? JobTitle { get; set; }

    public List<EmployeeServiceDto> Services { get; set; } = new();
}

public class EmployeeServiceDto
{
    public int EmployeeServiceId { get; set; }

    public int ServiceId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int DurationMinutes { get; set; }
}
