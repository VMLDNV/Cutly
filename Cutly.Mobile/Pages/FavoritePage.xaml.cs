using Cutly.Mobile.Services;

namespace Cutly.Mobile.Pages;

public partial class FavoritesPage : ContentPage
{
    private readonly FavoritesApiService _favoritesApiService;

    private bool _isLoading;

    public FavoritesPage(
        FavoritesApiService favoritesApiService)
    {
        InitializeComponent();

        _favoritesApiService = favoritesApiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isLoading)
            return;

        await LoadFavoritesAsync();
    }

    private async Task LoadFavoritesAsync()
    {
        _isLoading = true;

        try
        {
            FavoritesContainer.Children.Clear();

            var favorites = await _favoritesApiService
                .GetFavoriteSalonsAsync();

            EmptyStateLabel.IsVisible = favorites.Count == 0;

            foreach (var salon in favorites)
            {
                var image = new Image
                {
                    Source = string.IsNullOrWhiteSpace(salon.ImageUrl)
                        ? "barber_welcome.jpg"
                        : salon.ImageUrl,
                    Aspect = Aspect.AspectFill,
                    HeightRequest = 150
                };

                var removeButton = new Button
                {
                    Text = "♥",
                    FontSize = 25,
                    TextColor = Color.FromArgb("#F04452"),
                    BackgroundColor = Color.FromArgb("#99000000"),
                    WidthRequest = 48,
                    HeightRequest = 48,
                    CornerRadius = 24,
                    Padding = 0,
                    Margin = 10,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Start
                };

                removeButton.Clicked += async (_, _) =>
                {
                    await RemoveFavoriteAsync(salon.Id);
                };

                var imageContainer = new Grid
                {
                    HeightRequest = 150
                };

                imageContainer.Children.Add(image);
                imageContainer.Children.Add(removeButton);

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
                    TextColor = Color.FromArgb("#F5C542")
                };

                var details = new VerticalStackLayout
                {
                    Padding = 14,
                    Spacing = 6,
                    Children =
                    {
                        name,
                        location,
                        rating
                    }
                };

                var content = new VerticalStackLayout
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
                    StrokeShape =
                        new Microsoft.Maui.Controls.Shapes.RoundRectangle
                        {
                            CornerRadius = 14
                        },
                    Content = content
                };

                var tapGesture = new TapGestureRecognizer();

                tapGesture.Tapped += async (_, _) =>
                {
                    await Shell.Current.GoToAsync(
                        $"{nameof(SalonDetailsPage)}?Id={salon.Id}");
                };

                card.GestureRecognizers.Add(tapGesture);

                FavoritesContainer.Children.Add(card);
            }
        }
        catch (UnauthorizedAccessException)
        {
            await DisplayAlert(
                "Необходим е вход",
                "Влез в профила си, за да видиш любимите салони.",
                "OK");
        }
        catch (HttpRequestException)
        {
            await DisplayAlert(
                "Грешка",
                "Не успяхме да заредим любимите салони.",
                "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task RemoveFavoriteAsync(int salonId)
    {
        try
        {
            var success = await _favoritesApiService
                .RemoveFavoriteSalonAsync(salonId);

            if (success)
                await LoadFavoritesAsync();
            else
                await DisplayAlert(
                    "Грешка",
                    "Салонът не беше премахнат.",
                    "OK");
        }
        catch (HttpRequestException)
        {
            await DisplayAlert(
                "Грешка",
                "Не успяхме да премахнем салона.",
                "OK");
        }
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
    private async void OnHomeClicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            $"//{nameof(CustomerHomePage)}");
    }

    private async void OnAppointmentsClicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(MyAppointmentsPage));
    }

    private async void OnFavoritesClicked(
        object sender,
        EventArgs e)
    {
        await LoadFavoritesAsync();
    }

    private async void OnProfileClicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlert(
            "Профил",
            "Потребителският профил ще бъде добавен в следващ етап.",
            "OK");
    }

}
