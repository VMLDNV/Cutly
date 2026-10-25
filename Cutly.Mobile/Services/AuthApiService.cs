using System.Net.Http.Json;
using Microsoft.Maui.Storage;

namespace Cutly.Mobile.Services;

public sealed class AuthApiService
{
    private readonly HttpClient _httpClient;


    public AuthApiService()
    {
#if DEBUG
        var handler = new HttpClientHandler();

        handler.ServerCertificateCustomValidationCallback =
            (message, certificate, chain, errors) =>
            {
                // Само за локалния ASP.NET Core сертификат
                // по време на Debug.
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


    public async Task<bool> CheckApiAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                "api/health/database");

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<bool> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        string accountType)
    {
        var request = new
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Password = password,
            AccountType = accountType
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/register",
            request);

        return response.IsSuccessStatusCode;
    }


    public sealed class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public sealed class LoginResponse
    {
        public string? Token { get; set; }
        public string? AccessToken { get; set; }
        public string? Message { get; set; }
    }

    public async Task<bool> LoginAsync(
        string email,
        string password)
    {
        var request = new LoginRequest
        {
            Email = email,
            Password = password
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            request);

        if (!response.IsSuccessStatusCode)
            return false;

        var result = await response.Content
            .ReadFromJsonAsync<LoginResponse>();

        var token = result?.Token ?? result?.AccessToken;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        await SecureStorage.Default.SetAsync(
            "auth_token",
            token);

        return true;
    }


}
