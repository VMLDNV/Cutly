using Cutly.Mobile.Pages;
using Cutly.Mobile.Services;

namespace Cutly.Mobile.Pages;

public partial class CustomerHomePage : ContentPage
{
    private readonly SalonApiService _salonApiService;
    private List<SalonDto> _salons = new();
    private readonly FavoritesApiService _favoritesApiService;
    private readonly HashSet<int> _favoriteSalonIds = new();

    public CustomerHomePage(
        SalonApiService salonApiService,
        FavoritesApiService favoritesApiService)
    {
        InitializeComponent();

        _salonApiService = salonApiService;
        _favoritesApiService = favoritesApiService;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadFavoritesAsync();
        await LoadSalonsAsync();
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
    private async Task LoadFavoritesAsync()
    {
        try
        {
            var favorites = await _favoritesApiService
                .GetFavoriteSalonsAsync();

            _favoriteSalonIds.Clear();

            foreach (var salon in favorites)
                _favoriteSalonIds.Add(salon.Id);
        }
        catch (UnauthorizedAccessException)
        {
            // Потребителят не е влязъл или няма токен.
        }
        catch (HttpRequestException)
        {
            await DisplayAlert(
                "Грешка",
                "Не успяхме да заредим любимите салони.",
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
            var salonImage = new Image
            {
                Source = string.IsNullOrWhiteSpace(salon.ImageUrl)
                    ? "barber_welcome.jpg"
                    : salon.ImageUrl,
                Aspect = Aspect.AspectFill,
                HeightRequest = 150
            };

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

            var rating = new Label
            {
                Text = salon.AverageRating.HasValue
                  ? $"★ {salon.AverageRating.Value:F1} ({salon.ReviewCount} отзива)"
                  : "☆ Все още няма оценки",
                FontSize = 12,
                TextColor = salon.AverageRating.HasValue
                 ? Color.FromArgb("#F5C542")
                 : Colors.LightGray
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
                Padding = new Thickness(14, 12),
                Spacing = 5,
                Children =
                {
                    name,
                    location,
                    description,
                    rating
                }
            };

            var favoriteButton = new Button
            {
                Text = _favoriteSalonIds.Contains(salon.Id) ? "♥" : "♡",
                FontSize = 25,
                TextColor = Color.FromArgb("#F04452"),
                BackgroundColor = Color.FromArgb("#99000000"),
                WidthRequest = 48,
                HeightRequest = 48,
                CornerRadius = 24,
                Padding = 0,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(10)
            };

            favoriteButton.Clicked += async (_, _) =>
            {
                await ToggleFavoriteAsync(salon, favoriteButton);
            };

            var imageContainer = new Grid
            {
                HeightRequest = 150
            };

            imageContainer.Children.Add(salonImage);
            imageContainer.Children.Add(favoriteButton);

            var cardContent = new VerticalStackLayout
            {
                Spacing = 0,
                Children =
                {
                    imageContainer,
                    details
                }
            };

            var card = new Border
            {
                Stroke = Color.FromArgb("#343434"),
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#1B1B1B"),
                Padding = 0,
                Margin = new Thickness(0, 0, 0, 12),
                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = 14
                    },
                Content = cardContent
            };

            var tapGesture = new TapGestureRecognizer();

            tapGesture.Tapped += async (_, _) =>
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(SalonDetailsPage)}?Id={salon.Id}");
            };

            card.GestureRecognizers.Add(tapGesture);
            SalonsContainer.Children.Add(card);
        }
    }

    private async void OnFavoritesClicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(FavoritesPage));
    }

    private async Task ToggleFavoriteAsync(
        SalonDto salon,
        Button favoriteButton)
    {
        try
        {
            bool isFavorite = _favoriteSalonIds.Contains(salon.Id);

            bool success;

            if (isFavorite)
            {
                success = await _favoritesApiService
                    .RemoveFavoriteSalonAsync(salon.Id);
            }
            else
            {
                success = await _favoritesApiService
                    .AddFavoriteSalonAsync(salon.Id);
            }

            if (!success)
            {
                await DisplayAlert(
                    "Грешка",
                    "Не успяхме да променим любимите салони.",
                    "OK");

                return;
            }

            if (isFavorite)
                _favoriteSalonIds.Remove(salon.Id);
            else
                _favoriteSalonIds.Add(salon.Id);

            favoriteButton.Text =
                _favoriteSalonIds.Contains(salon.Id) ? "♥" : "♡";
        }
        catch (UnauthorizedAccessException)
        {
            await DisplayAlert(
                "Необходим е вход",
                "Влез в профила си, за да добавяш любими салони.",
                "OK");
        }
        catch (HttpRequestException)
        {
            await DisplayAlert(
                "Грешка",
                "Провери интернет връзката и опитай отново.",
                "OK");
        }
    }
}
