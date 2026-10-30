using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Maui.Storage;

namespace Cutly.Mobile.Services;

public sealed class FavoritesApiService
{
    private readonly HttpClient _httpClient;

    public FavoritesApiService()
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

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string endpoint)
    {
        var token = await SecureStorage.Default.GetAsync("auth_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException(
                "Необходимо е да влезете в профила си.");
        }

        var request = new HttpRequestMessage(method, endpoint);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return request;
    }

    // GET: api/favorites/salons
    public async Task<List<SalonDto>> GetFavoriteSalonsAsync()
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            "api/favorites/salons");

        using var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var favorites = await response.Content
            .ReadFromJsonAsync<List<SalonDto>>();

        return favorites ?? new List<SalonDto>();
    }

    // POST: api/favorites/salons/{salonId}
    public async Task<bool> AddFavoriteSalonAsync(int salonId)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            $"api/favorites/salons/{salonId}");

        using var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    // DELETE: api/favorites/salons/{salonId}
    public async Task<bool> RemoveFavoriteSalonAsync(int salonId)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Delete,
            $"api/favorites/salons/{salonId}");

        using var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }
}
