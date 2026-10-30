
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

    public async Task<List<SalonDto>> GetSalonsAsync()
    {
        using var response = await _httpClient.GetAsync("api/salons");

        response.EnsureSuccessStatusCode();

        var salons = await response.Content
            .ReadFromJsonAsync<List<SalonDto>>();

        return salons ?? new List<SalonDto>();
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
}
