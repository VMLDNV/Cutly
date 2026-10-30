using System.Net.Http.Json;

namespace Cutly.Mobile.Services;

public sealed class SalonApiService
{
    private readonly HttpClient _httpClient;

    public SalonApiService()
    {
#if DEBUG
        var handler = new HttpClientHandler();

        handler.ServerCertificateCustomValidationCallback =
            (message, certificate, chain, errors) =>
            {
                if (certificate != null &&
                    certificate.Issuer.Contains(
                        "CN=localhost",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return errors ==
                    System.Net.Security.SslPolicyErrors.None;
            };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://10.0.2.2:7225/")
        };
#else
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://10.0.2.2:7225/")
        };
#endif
    }

    // Взима всички салони
    public async Task<List<SalonDto>> GetSalonsAsync()
    {
        using var response = await _httpClient.GetAsync("api/salons");

        response.EnsureSuccessStatusCode();

        var salons = await response.Content
            .ReadFromJsonAsync<List<SalonDto>>();

        return salons ?? new List<SalonDto>();
    }

    // Взима подробностите за конкретен салон
    public async Task<SalonDetailsDto?> GetSalonDetailsAsync(int id)
    {
        using var response = await _httpClient.GetAsync(
            $"api/salons/{id}");

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<SalonDetailsDto>();
    }
}

public sealed class SalonDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ImageUrl { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public sealed class SalonDetailsDto
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

public sealed class SalonEmployeeDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public List<EmployeeServiceDto> Services { get; set; } = new();
}

public sealed class EmployeeServiceDto
{
    public int EmployeeServiceId { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
}
