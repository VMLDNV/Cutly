using Cutly.Mobile.Services;

namespace Cutly.Mobile.Pages;

public partial class SalonDetailsPage : ContentPage, IQueryAttributable
{
    private readonly SalonApiService _salonApiService;

    private int _salonId;
    private bool _isLoaded;

    public SalonDetailsPage(SalonApiService salonApiService)
    {
        InitializeComponent();

        _salonApiService = salonApiService;
    }

    public void ApplyQueryAttributes(
        IDictionary<string, object> query)
    {
        if (query.TryGetValue("Id", out var idValue) &&
            int.TryParse(idValue?.ToString(), out var id))
        {
            _salonId = id;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isLoaded || _salonId <= 0)
            return;

        _isLoaded = true;

        await LoadSalonDetailsAsync();
    }

    private async Task LoadSalonDetailsAsync()
    {
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

            // Salon information
            SalonNameLabel.Text = salon.Name;

            SalonImage.Source =
                string.IsNullOrWhiteSpace(salon.ImageUrl)
                    ? "barber_welcome.jpg"
                    : salon.ImageUrl;

            RatingLabel.Text = salon.AverageRating.HasValue
                ? $"★ {salon.AverageRating.Value:F1} ({salon.ReviewCount} отзива)"
                : "☆ Все още няма оценки";

            AddressLabel.Text = string.Join(
                ", ",
                new[] { salon.City, salon.Address }
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value)));

            PhoneLabel.Text = salon.PhoneNumber ?? string.Empty;

            DescriptionLabel.Text =
                string.IsNullOrWhiteSpace(salon.Description)
                    ? "Няма добавено описание."
                    : salon.Description;

            DisplayEmployees(salon.Employees);
        }
        catch (HttpRequestException)
        {
            await DisplayAlert(
                "Грешка",
                "Не успяхме да заредим информацията за салона.",
                "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlert(
                "Връзката прекъсна",
                "Заявката към сървъра отне твърде много време.",
                "OK");
        }
    }

    private void DisplayEmployees(
        IEnumerable<SalonEmployeeDto> employees)
    {
        EmployeesContainer.Children.Clear();

        var employeeList = employees.ToList();

        if (employeeList.Count == 0)
        {
            EmployeesContainer.Children.Add(new Label
            {
                Text = "Все още няма добавени служители.",
                FontSize = 14,
                TextColor = Colors.LightGray
            });

            return;
        }

        foreach (var employee in employeeList)
        {
            var employeeName = new Label
            {
                Text = employee.FullName,
                FontSize = 17,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };

            var jobTitle = new Label
            {
                Text = employee.JobTitle ?? "Служител",
                FontSize = 13,
                TextColor = Color.FromArgb("#AAAAAA")
            };

            var employeeContent = new VerticalStackLayout
            {
                Spacing = 5,
                Children =
                {
                    employeeName,
                    jobTitle
                }
            };

            if (employee.Services.Count == 0)
            {
                employeeContent.Children.Add(new Label
                {
                    Text = "Няма добавени услуги.",
                    FontSize = 13,
                    TextColor = Colors.LightGray,
                    Margin = new Thickness(0, 8, 0, 0)
                });
            }

            foreach (var service in employee.Services)
            {
                var serviceName = new Label
                {
                    Text = service.ServiceName,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White
                };

                var serviceDescription = new Label
                {
                    Text = service.Description ?? string.Empty,
                    FontSize = 12,
                    TextColor = Colors.LightGray
                };

                var priceAndDuration = new Label
                {
                    Text = $"{service.Price:F2} лв. · {service.DurationMinutes} мин.",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#F5C542")
                };

                employeeContent.Children.Add(
                    new BoxView
                    {
                        HeightRequest = 1,
                        BackgroundColor = Color.FromArgb("#333333"),
                        Margin = new Thickness(0, 8)
                    });

                employeeContent.Children.Add(serviceName);
                employeeContent.Children.Add(serviceDescription);
                employeeContent.Children.Add(priceAndDuration);
            }

            var employeeCard = new Border
            {
                BackgroundColor = Color.FromArgb("#1B1B1B"),
                Stroke = Color.FromArgb("#343434"),
                StrokeThickness = 1,
                Padding = 15,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 14
                },
                Content = employeeContent
            };

            EmployeesContainer.Children.Add(employeeCard);
        }
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnBookAppointmentClicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlert(
            "Запазване на час",
            "Ще добавим функционалността за резервации в следващата стъпка.",
            "OK");
    }
}
