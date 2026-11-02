using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace Cutly.Mobile.Services;

public sealed class AppointmentApiService
{
    private readonly HttpClient _httpClient;

    public AppointmentApiService()
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

    // GET: api/appointments/available-slots
    public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(
        int employeeServiceId,
        DateOnly date)
    {
        var formattedDate = date.ToString("yyyy-MM-dd");

        var endpoint =
            $"api/appointments/available-slots" +
            $"?EmployeeServiceId={employeeServiceId}" +
            $"&Date={formattedDate}";

        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            endpoint);

        using var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var slots = await response.Content
            .ReadFromJsonAsync<List<AvailableSlotDto>>();

        return slots ?? new List<AvailableSlotDto>();
    }

    // POST: api/appointments
    public async Task<CreateAppointmentResult> CreateAppointmentAsync(
        CreateAppointmentRequest requestData)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            "api/appointments");

        request.Content = JsonContent.Create(requestData);

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(errorMessage)
                    ? "Неуспешно създаване на резервацията."
                    : errorMessage,
                null,
                response.StatusCode);
        }

        var result = await response.Content
            .ReadFromJsonAsync<CreateAppointmentResult>();

        return result ?? throw new InvalidOperationException(
            "Сървърът не върна информация за резервацията.");
    }

    // GET: api/appointments/my
    public async Task<List<MyAppointmentDto>> GetMyAppointmentsAsync()
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            "api/appointments/my");

        using var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var appointments = await response.Content
            .ReadFromJsonAsync<List<MyAppointmentDto>>();

        return appointments ?? new List<MyAppointmentDto>();
    }

    // POST: api/appointments/{id}/cancel
    public async Task CancelAppointmentAsync(int appointmentId)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            $"api/appointments/{appointmentId}/cancel");

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(errorMessage)
                    ? "Неуспешна отмяна на резервацията."
                    : errorMessage,
                null,
                response.StatusCode);
        }
    }


    // GET: api/appointments/management
    public async Task<List<ManagedAppointmentDto>> GetManagedAppointmentsAsync()
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            "api/appointments/management");

        using var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var appointments = await response.Content
            .ReadFromJsonAsync<List<ManagedAppointmentDto>>();

        return appointments ?? new List<ManagedAppointmentDto>();
    }

    // POST: api/appointments/{id}/management-cancel
    public async Task CancelManagedAppointmentAsync(int appointmentId)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            $"api/appointments/{appointmentId}/management-cancel");

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(errorMessage)
                    ? "Неуспешна отмяна на резервацията."
                    : errorMessage,
                null,
                response.StatusCode);
        }
    }
}

public class AvailableSlotDto
{
    public DateTimeOffset StartDateTime { get; set; }

    public DateTimeOffset EndDateTime { get; set; }
}

public class CreateAppointmentRequest
{
    public int EmployeeServiceId { get; set; }

    public DateTimeOffset StartDateTime { get; set; }

    public string? CustomerNotes { get; set; }
}

public class CreateAppointmentResult
{
    public int Id { get; set; }

    public DateTimeOffset StartDateTime { get; set; }

    public DateTimeOffset EndDateTime { get; set; }

    public decimal Price { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

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

    public DateTimeOffset StartDateTime { get; set; }

    public DateTimeOffset EndDateTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? CustomerNotes { get; set; }
}

public class ManagedAppointmentDto
{
    public int Id { get; set; }

    public string SalonName { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int DurationMinutes { get; set; }

    public DateTimeOffset StartDateTime { get; set; }

    public DateTimeOffset EndDateTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? CustomerNotes { get; set; }
}
