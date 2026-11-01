using Cutly.Mobile.Services;

namespace Cutly.Mobile.Pages;

public partial class ServiceEmployeesPage : ContentPage, IQueryAttributable
{
    private readonly SalonApiService _salonApiService;

    private int _salonId;
    private int _serviceId;
    private string _serviceName = string.Empty;

    private bool _isLoaded;

    public ServiceEmployeesPage(SalonApiService salonApiService)
    {
        InitializeComponent();

        _salonApiService = salonApiService;
    }

    public void ApplyQueryAttributes(
        IDictionary<string, object> query)
    {
        if (query.TryGetValue("SalonId", out var salonId))
            _salonId = Convert.ToInt32(salonId);

        if (query.TryGetValue("ServiceId", out var serviceId))
            _serviceId = Convert.ToInt32(serviceId);

        if (query.TryGetValue("ServiceName", out var serviceName))
        {
            _serviceName = serviceName?.ToString() ?? string.Empty;
            ServiceNameLabel.Text = _serviceName;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isLoaded || _salonId <= 0 || _serviceId <= 0)
            return;

        _isLoaded = true;

        await LoadEmployeesAsync();
    }

    private async Task LoadEmployeesAsync()
    {
        EmployeesContainer.Children.Clear();

        try
        {
            var salon = await _salonApiService
                .GetSalonDetailsAsync(_salonId);

            if (salon == null)
            {
                await DisplayAlert(
                    "Грешка",
                    "Салонът не беше намерен.",
                    "OK");

                await Shell.Current.GoToAsync("..");
                return;
            }

            var matchingEmployees = salon.Employees
                .Select(employee => new
                {
                    Employee = employee,

                    Service = employee.Services.FirstOrDefault(
                        service => service.ServiceId == _serviceId)
                })
                .Where(item => item.Service != null)
                .ToList();

            if (matchingEmployees.Count == 0)
            {
                EmployeesContainer.Children.Add(new Label
                {
                    Text = "Няма служители, които предлагат тази услуга.",
                    FontSize = 14,
                    TextColor = Colors.LightGray
                });

                return;
            }

            foreach (var item in matchingEmployees)
            {
                var employee = item.Employee;
                var service = item.Service!;

                var employeeName = new Label
                {
                    Text = employee.FullName,
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White
                };

                var jobTitle = new Label
                {
                    Text = employee.JobTitle ?? "Служител",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#AAAAAA")
                };

                var priceLabel = new Label
                {
                    Text = $"{service.Price:F2} лв.",
                    FontSize = 21,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#F5C542")
                };

                var durationLabel = new Label
                {
                    Text = $"Продължителност: {service.DurationMinutes} мин.",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#BBBBBB")
                };

                var selectButton = new Button
                {
                    Text = "Избери служител",
                    BackgroundColor = Color.FromArgb("#F2F2F2"),
                    TextColor = Color.FromArgb("#111111"),
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    CornerRadius = 10,
                    HeightRequest = 46,
                    Margin = new Thickness(0, 8, 0, 0)
                };

                selectButton.Clicked += async (_, _) =>
                {
                    await Shell.Current.GoToAsync(
                        nameof(BookingPage),
                        new Dictionary<string, object>
                        {
                            ["EmployeeServiceId"] = service.EmployeeServiceId,
                            ["EmployeeName"] = employee.FullName,
                            ["ServiceName"] = service.ServiceName,
                            ["Price"] = service.Price,
                            ["DurationMinutes"] = service.DurationMinutes
                        });
                };

                var employeeContent = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        employeeName,
                        jobTitle,

                        new BoxView
                        {
                            HeightRequest = 1,
                            BackgroundColor = Color.FromArgb("#333333"),
                            Margin = new Thickness(0, 5)
                        },

                        priceLabel,
                        durationLabel,
                        selectButton
                    }
                };

                var employeeCard = new Border
                {
                    BackgroundColor = Color.FromArgb("#1B1B1B"),
                    Stroke = Color.FromArgb("#343434"),
                    StrokeThickness = 1,
                    Padding = 16,
                    StrokeShape =
                        new Microsoft.Maui.Controls.Shapes.RoundRectangle
                        {
                            CornerRadius = 14
                        },
                    Content = employeeContent
                };

                EmployeesContainer.Children.Add(employeeCard);
            }
        }
        catch (HttpRequestException)
        {
            await DisplayAlert(
                "Грешка",
                "Не успяхме да заредим служителите.",
                "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlert(
                "Връзката прекъсна",
                "Заявката отне твърде много време.",
                "OK");
        }
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
