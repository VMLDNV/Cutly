using Cutly.Mobile.Services;
using System.Net;

namespace Cutly.Mobile.Pages;

public partial class MyAppointmentsPage : ContentPage
{
    private readonly AppointmentApiService _appointmentApiService;

    private bool _isLoading;

    public MyAppointmentsPage(
        AppointmentApiService appointmentApiService)
    {
        InitializeComponent();

        _appointmentApiService = appointmentApiService;
    }
    private async void OnBackClicked(
    object sender,
    EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadAppointmentsAsync();
    }

    private async Task LoadAppointmentsAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        ErrorLabel.IsVisible = false;

        UpcomingContainer.Children.Clear();
        HistoryContainer.Children.Clear();

        try
        {
            var appointments = await _appointmentApiService
                .GetMyAppointmentsAsync();

            var now = DateTimeOffset.UtcNow;

            var upcoming = appointments
                .Where(a =>
                    a.StartDateTime > now &&
                    (a.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                     a.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(a => a.StartDateTime)
                .ToList();

            var history = appointments
                .Except(upcoming)
                .OrderByDescending(a => a.StartDateTime)
                .ToList();

            UpcomingCountLabel.Text = upcoming.Count.ToString();
            HistoryCountLabel.Text = history.Count.ToString();

            NoUpcomingLabel.IsVisible = upcoming.Count == 0;
            NoHistoryLabel.IsVisible = history.Count == 0;

            foreach (var appointment in upcoming)
            {
                UpcomingContainer.Children.Add(
                    CreateAppointmentCard(appointment, true));
            }

            foreach (var appointment in history)
            {
                HistoryContainer.Children.Add(
                    CreateAppointmentCard(appointment, false));
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
        catch (HttpRequestException ex)
        {
            ErrorLabel.Text =
                ex.StatusCode == HttpStatusCode.Unauthorized
                    ? "Сесията ти е изтекла. Моля, влез отново."
                    : "Не успяхме да заредим резервациите. Опитай отново.";

            ErrorLabel.IsVisible = true;
        }
        catch (TaskCanceledException)
        {
            ErrorLabel.Text =
                "Връзката със сървъра прекъсна. Опитай отново.";

            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            _isLoading = false;
        }
    }

    private Border CreateAppointmentCard(
        MyAppointmentDto appointment,
        bool isUpcoming)
    {
        var localStart = appointment.StartDateTime.ToLocalTime();
        var localEnd = appointment.EndDateTime.ToLocalTime();

        var statusColor = GetStatusColor(appointment.Status);

        var salonName = new Label
        {
            Text = appointment.SalonName,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var statusLabel = new Label
        {
            Text = GetStatusText(appointment.Status),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = statusColor,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };

        header.Add(salonName, 0, 0);
        header.Add(statusLabel, 1, 0);

        var serviceLabel = new Label
        {
            Text = appointment.ServiceName,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#F5C542")
        };

        var employeeLabel = new Label
        {
            Text = $"Служител: {appointment.EmployeeName}",
            FontSize = 13,
            TextColor = Color.FromArgb("#BBBBBB")
        };

        var locationLabel = new Label
        {
            Text = string.Join(
                ", ",
                new[] { appointment.City, appointment.Address }
                    .Where(value => !string.IsNullOrWhiteSpace(value))),
            FontSize = 13,
            TextColor = Color.FromArgb("#999999")
        };

        var dateLabel = new Label
        {
            Text = localStart.ToString("dd.MM.yyyy"),
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White
        };

        var timeLabel = new Label
        {
            Text = $"{localStart:HH:mm} – {localEnd:HH:mm}",
            FontSize = 14,
            TextColor = Colors.White
        };

        var priceLabel = new Label
        {
            Text = $"{appointment.Price:F2} лв.",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#F5C542")
        };

        var durationLabel = new Label
        {
            Text = $"{appointment.DurationMinutes} мин.",
            FontSize = 12,
            TextColor = Color.FromArgb("#999999"),
            VerticalOptions = LayoutOptions.Center
        };

        var priceRow = new HorizontalStackLayout
        {
            Spacing = 10,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                priceLabel,
                durationLabel
            }
        };

        var content = new VerticalStackLayout
        {
            Spacing = 9,
            Children =
            {
                header,
                serviceLabel,
                employeeLabel,
                locationLabel,

                new BoxView
                {
                    HeightRequest = 1,
                    BackgroundColor = Color.FromArgb("#383838"),
                    Margin = new Thickness(0, 3)
                },

                dateLabel,
                timeLabel,
                priceRow
            }
        };

        if (!string.IsNullOrWhiteSpace(appointment.CustomerNotes))
        {
            content.Children.Add(new Label
            {
                Text = $"Бележка: {appointment.CustomerNotes}",
                FontSize = 12,
                TextColor = Color.FromArgb("#999999")
            });
        }

        if (isUpcoming)
        {
            var cancelButton = new Button
            {
                Text = "Отмени резервацията",
                BackgroundColor = Color.FromArgb("#292020"),
                TextColor = Color.FromArgb("#FF8585"),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 10,
                HeightRequest = 46,
                Margin = new Thickness(0, 5, 0, 0)
            };

            cancelButton.Clicked += async (_, _) =>
            {
                await CancelAppointmentAsync(appointment);
            };

            content.Children.Add(cancelButton);
        }

        return new Border
        {
            BackgroundColor = Color.FromArgb("#1B1B1B"),
            Stroke = Color.FromArgb("#343434"),
            StrokeThickness = 1,
            Padding = 16,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = 16
            },
            Content = content
        };
    }

    private async Task CancelAppointmentAsync(
        MyAppointmentDto appointment)
    {
        bool confirmed = await DisplayAlert(
            "Отмяна на резервация",
            $"Сигурен ли си, че искаш да отмениш часа си в {appointment.SalonName}?",
            "Да, отмени",
            "Назад");

        if (!confirmed)
            return;

        try
        {
            await _appointmentApiService.CancelAppointmentAsync(
                appointment.Id);

            await DisplayAlert(
                "Успешно",
                "Резервацията е отменена.",
                "OK");

            await LoadAppointmentsAsync();
        }
        catch (HttpRequestException ex)
        {
            var message = ex.StatusCode == HttpStatusCode.Conflict
                ? "Тази резервация вече не може да бъде отменена."
                : "Не успяхме да отменим резервацията. Опитай отново.";

            await DisplayAlert("Грешка", message, "OK");

            await LoadAppointmentsAsync();
        }
        catch (UnauthorizedAccessException ex)
        {
            await DisplayAlert("Необходимо е влизане", ex.Message, "OK");
        }
    }

    private static string GetStatusText(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "pending" => "Очаква потвърждение",
            "confirmed" => "Потвърдена",
            "completed" => "Завършена",
            "cancelled" => "Отменена",
            "rejected" => "Отказана",
            "noshow" => "Неявяване",
            _ => status
        };
    }

    private static Color GetStatusColor(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "pending" => Color.FromArgb("#F5C542"),
            "confirmed" => Color.FromArgb("#70D69A"),
            "completed" => Color.FromArgb("#70AFFF"),
            "cancelled" => Color.FromArgb("#FF8585"),
            "rejected" => Color.FromArgb("#FF8585"),
            "noshow" => Color.FromArgb("#AAAAAA"),
            _ => Colors.White
        };
    }
}
