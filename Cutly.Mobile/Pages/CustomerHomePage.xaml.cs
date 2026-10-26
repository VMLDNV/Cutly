
using Cutly.Mobile.Services;

namespace Cutly.Mobile.Pages;

public partial class CustomerHomePage : ContentPage
{
    private readonly SalonApiService _salonApiService;
    private List<SalonDto> _salons = new();

    public CustomerHomePage(SalonApiService salonApiService)
    {
        InitializeComponent();

        _salonApiService = salonApiService;

        Loaded += async (_, _) => await LoadSalonsAsync();
    }

    private async Task LoadSalonsAsync()
    {
        try
        {
            var salons = await _salonApiService.GetSalonsAsync();

            _salons = salons;

            DisplaySalons(_salons);
        }
        catch (HttpRequestException)
        {
            await DisplayAlert(
                "Грешка",
                "Не успяхме да заредим салоните. Провери дали API работи.",
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

    private void OnSearchTextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim();

        var filteredSalons = string.IsNullOrWhiteSpace(query)
            ? _salons
            : _salons.Where(salon =>
                (salon.Name?.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase) ?? false)
                ||
                (salon.Address?.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase) ?? false)
                ||
                (salon.City?.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

        DisplaySalons(filteredSalons);
    }

    private void DisplaySalons(IEnumerable<SalonDto> salons)
    {
        SalonsContainer.Children.Clear();

        foreach (var salon in salons)
        {
            var name = new Label
            {
                Text = salon.Name,
                FontSize = 17,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };

            var location = new Label
            {
                Text = string.Join(
                    ", ",
                    new[] { salon.City, salon.Address }
                        .Where(value =>
                            !string.IsNullOrWhiteSpace(value))),
                FontSize = 13,
                TextColor = Colors.LightGray
            };

            var description = new Label
            {
                Text = salon.Description ?? string.Empty,
                FontSize = 13,
                TextColor = Colors.LightGray,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 2
            };

            var details = new VerticalStackLayout
            {
                Spacing = 5,
                Children = { name, location, description }
            };

            var card = new Border
            {
                Stroke = Color.FromArgb("#343434"),
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#1B1B1B"),
                Padding = 16,
                Margin = new Thickness(0, 0, 0, 12),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 14
                },
                Content = details
            };

            SalonsContainer.Children.Add(card);
        }
    }
}
