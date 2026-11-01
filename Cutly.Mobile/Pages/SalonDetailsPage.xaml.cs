
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

            DisplayServices(salon.Employees);
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

    private void DisplayServices(
        IEnumerable<SalonEmployeeDto> employees)
    {
        ServicesContainer.Children.Clear();

        // Взимаме всички услуги от всички служители.
        // Една услуга се показва само веднъж.
        var services = employees
            .SelectMany(employee => employee.Services)
            .GroupBy(service => service.ServiceId)
            .Select(group => group.First())
            .OrderBy(service => service.ServiceName)
            .ToList();

        if (services.Count == 0)
        {
            ServicesContainer.Children.Add(new Label
            {
                Text = "Все още няма добавени услуги.",
                FontSize = 14,
                TextColor = Colors.LightGray
            });

            return;
        }

        foreach (var service in services)
        {
            var serviceName = new Label
            {
                Text = service.ServiceName,
                FontSize = 17,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };

            var serviceDescription = new Label
            {
                Text = service.Description ?? string.Empty,
                FontSize = 13,
                TextColor = Color.FromArgb("#AAAAAA")
            };

            var selectLabel = new Label
            {
                Text = "Избери услуга →",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#F5C542"),
                Margin = new Thickness(0, 5, 0, 0)
            };

            var serviceContent = new VerticalStackLayout
            {
                Spacing = 7,
                Children =
                {
                    serviceName,
                    serviceDescription,
                    selectLabel
                }
            };

            var serviceCard = new Border
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
                Content = serviceContent
            };

            var tapGesture = new TapGestureRecognizer();

            tapGesture.Tapped += async (_, _) =>
            {
                await Shell.Current.GoToAsync(
                    nameof(ServiceEmployeesPage),
                    new Dictionary<string, object>
                    {
                        ["SalonId"] = _salonId,
                        ["ServiceId"] = service.ServiceId,
                        ["ServiceName"] = service.ServiceName
                    });
            };

            serviceCard.GestureRecognizers.Add(tapGesture);

            ServicesContainer.Children.Add(serviceCard);
        }
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
