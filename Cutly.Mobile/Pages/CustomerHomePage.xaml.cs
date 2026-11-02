using Cutly.Mobile.Pages;
using Cutly.Mobile.Services;

namespace Cutly.Mobile.Pages;

public partial class CustomerHomePage : ContentPage
{
    private readonly SalonApiService _salonApiService;
    private List<SalonDto> _salons = new();
    private readonly FavoritesApiService _favoritesApiService;
    private readonly HashSet<int> _favoriteSalonIds = new();
    private readonly AuthApiService _authApiService;
    private List<string> _userRoles = new();

    public CustomerHomePage(
    SalonApiService salonApiService,
    FavoritesApiService favoritesApiService,
    AuthApiService authApiService)
    {
        InitializeComponent();

        _salonApiService = salonApiService;
        _favoritesApiService = favoritesApiService;
        _authApiService = authApiService;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadUserRolesAsync();
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

        if (string.IsNullOrWhiteSpace(query))
        {
            DisplaySalons(_salons);
            return;
        }

        // Разделяме търсенето на отделни думи.
        var searchTerms = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var filteredSalons = _salons.Where(salon =>
        {
            // Обединяваме информацията за салона.
            var searchableText = string.Join(" ",
                salon.Name,
                salon.Description,
                salon.City,
                salon.Address);

            // Всяка въведена дума трябва да бъде намерена.
            return searchTerms.All(term =>
                searchableText.Contains(
                    term,
                    StringComparison.OrdinalIgnoreCase));
        }).ToList();

        DisplaySalons(filteredSalons);
    }

    private void DisplaySalons(IEnumerable<SalonDto> salons)
    {
        var salonList = salons.ToList();

        SalonsContainer.Children.Clear();

        NoResultsLabel.IsVisible = salonList.Count == 0;

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

    private async void OnFavoritesButtonClicked(
    object sender,
    EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(FavoritesPage));
    }

    private async void OnAppointmentsClicked(
     object sender,
     EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(MyAppointmentsPage));
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


    private async Task LoadUserRolesAsync()
    {
        _userRoles = await _authApiService.GetUserRolesAsync();

        BuildBottomNavigation();
    }

    private void BuildBottomNavigation()
    {
        BottomNavigation.Children.Clear();
        BottomNavigation.ColumnDefinitions.Clear();

        bool isBusinessOwner = _userRoles.Contains(
            "BusinessOwner",
            StringComparer.OrdinalIgnoreCase);

        bool isEmployee = _userRoles.Contains(
            "Employee",
            StringComparer.OrdinalIgnoreCase);

        var items = new List<(string Icon, string Title, string Action)>();

        if (isBusinessOwner)
        {
            items.Add(("▦", "Табло", "dashboard"));
            items.Add(("▣", "Резервации", "management"));
            items.Add(("▦", "Моят график", "schedule"));
            items.Add(("▤", "Салони", "salons"));
        }
        else if (isEmployee)
        {
            items.Add(("⌂", "Начало", "home"));
            items.Add(("▦", "Моят график", "schedule"));
            items.Add(("▣", "Резервации", "management"));
        }
        else
        {
            items.Add(("⌂", "Начало", "home"));
            items.Add(("▣", "Часове", "appointments"));
            items.Add(("♡", "Любими", "favorites"));
        }

        // Разпределяме бутоните равномерно по цялата ширина.
        foreach (var item in items)
        {
            BottomNavigation.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
        }

        // Създаваме бутоните в правилния ред.
        for (int i = 0; i < items.Count; i++)
        {
            AddNavigationItem(
                items[i].Icon,
                items[i].Title,
                i,
                items[i].Action);
        }
    }



    private void AddNavigationItem(
        string icon,
        string title,
        int column,
        string action)
    {
        var iconLabel = new Label
        {
            Text = icon,
            FontSize = 22,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var titleLabel = new Label
        {
            Text = title,
            FontSize = 10,
            TextColor = Color.FromArgb("#AAAAAA"),
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var content = new VerticalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children = { iconLabel, titleLabel }
        };

        var itemGrid = new Grid
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        itemGrid.Children.Add(content);

        var button = new Button
        {
            BackgroundColor = Colors.Transparent,
            Text = string.Empty,
            Padding = 0,
            Margin = 0,
            BorderWidth = 0,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        button.Clicked += async (_, _) =>
            await NavigateFromBottomNavigationAsync(action);

        itemGrid.Children.Add(button);

        BottomNavigation.Add(itemGrid, column, 0);
    }


    private async Task NavigateFromBottomNavigationAsync(string action)
    {
        switch (action)
        {
            case "home":
            case "dashboard":
                // Началната страница засега е обща.
                break;

            case "appointments":
                await Shell.Current.GoToAsync(
                    nameof(MyAppointmentsPage));
                break;

            case "favorites":
                await Shell.Current.GoToAsync(
                    nameof(FavoritesPage));
                break;

            case "management":
                await Shell.Current.GoToAsync(
                    nameof(BusinessAppointmentsPage));
                break;

            case "schedule":
                await Shell.Current.GoToAsync(
                    nameof(MySchedulePage));
                break;

            case "salons":
                await DisplayAlert(
                    "Моите салони",
                    "Управлението на салоните ще бъде добавено в следващия етап.",
                    "OK");
                break;
        }
    }

}
