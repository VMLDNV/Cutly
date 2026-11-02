using System.Globalization;
using Cutly.Mobile.Services;
using Microsoft.Maui.Controls.Shapes;

namespace Cutly.Mobile.Pages;

public partial class MySchedulePage : ContentPage
{
    private readonly AppointmentApiService _appointmentApiService;

    private readonly CultureInfo _bulgarianCulture =
        new("bg-BG");

    private DateTime _selectedDate = DateTime.Today;

    private bool _isLoading;

    private List<ManagedAppointmentDto> _allAppointments = new();

    public MySchedulePage(
        AppointmentApiService appointmentApiService)
    {
        InitializeComponent();

        _appointmentApiService = appointmentApiService;

        BuildCalendar();
        UpdateSelectedDate();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadAppointmentsAsync();
    }

    // ==========================================
    // CALENDAR
    // ==========================================

    private void BuildCalendar()
    {
        WeekDaysContainer.Children.Clear();

        // Намираме понеделника на текущата седмица.
        int difference = ((int)_selectedDate.DayOfWeek + 6) % 7;

        DateTime monday = _selectedDate.Date.AddDays(-difference);

        MonthLabel.Text = Capitalize(
            _selectedDate.ToString("MMMM yyyy", _bulgarianCulture));

        for (int i = 0; i < 7; i++)
        {
            DateTime day = monday.AddDays(i);

            var dayButton = new Button
            {
                Text =
                    $"{day.ToString("ddd", _bulgarianCulture).ToUpper(_bulgarianCulture)}\n" +
                    $"{day:dd}",

                WidthRequest = 76,
                HeightRequest = 60,

                FontSize = 13,
                FontAttributes = FontAttributes.Bold,

                CornerRadius = 12,
                Padding = 4,

                TextColor = Colors.White,

                BackgroundColor =
                    day.Date == _selectedDate.Date
                        ? Color.FromArgb("#C30F45")
                        : Color.FromArgb("#242424")
            };

            dayButton.Clicked += async (sender, args) =>
            {
                _selectedDate = day;

                BuildCalendar();
                UpdateSelectedDate();

                await LoadAppointmentsAsync();
            };

            WeekDaysContainer.Children.Add(dayButton);
        }
    }

    private void UpdateSelectedDate()
    {
        SelectedDateLabel.Text = Capitalize(
            _selectedDate.ToString(
                "dddd, dd MMMM",
                _bulgarianCulture));
    }

    private string Capitalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return char.ToUpper(
            value[0],
            _bulgarianCulture) + value[1..];
    }

    // ==========================================
    // LOAD APPOINTMENTS
    // ==========================================

    private async Task LoadAppointmentsAsync()
    {
        if (_isLoading)
            return;

        try
        {
            _isLoading = true;

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            EmptyLabel.IsVisible = false;

            AppointmentsContainer.Children.Clear();

            _allAppointments =
                await _appointmentApiService
                    .GetManagedAppointmentsAsync();

            DisplayAppointments();
        }
        catch (UnauthorizedAccessException ex)
        {
            await DisplayAlert(
                "Вход",
                ex.Message,
                "OK");
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlert(
                "Грешка",
                ex.Message,
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Грешка",
                $"Възникна проблем: {ex.Message}",
                "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            _isLoading = false;
        }
    }

    // ==========================================
    // DISPLAY APPOINTMENTS
    // ==========================================
    private void DisplayAppointments()
    {
        AppointmentsContainer.Children.Clear();

        var dailyAppointments = _allAppointments
            .Where(a =>
                a.StartDateTime.ToLocalTime().Date == _selectedDate.Date &&
                !a.Status.Equals(
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.StartDateTime)
            .ToList();

        int count = dailyAppointments.Count;

        AppointmentCountLabel.Text =
            count == 1
                ? "1 резервация"
                : $"{count} резервации";

        if (count == 0)
        {
            EmptyLabel.Text = "Няма активни резервации за този ден.";
            EmptyLabel.IsVisible = true;
            return;
        }

        EmptyLabel.IsVisible = false;

        foreach (var appointment in dailyAppointments)
        {
            AppointmentsContainer.Children.Add(
                CreateAppointmentCard(appointment));
        }
    }


    // ==========================================
    // APPOINTMENT CARD
    // ==========================================

    private View CreateAppointmentCard(
        ManagedAppointmentDto appointment)
    {
        var content = new VerticalStackLayout
        {
            Spacing = 9
        };

        // Час и статус
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var start = appointment.StartDateTime.ToLocalTime();
        var end = appointment.EndDateTime.ToLocalTime();

        header.Add(new Label
        {
            Text = $"{start:HH:mm} – {end:HH:mm}",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#DDDDDD"),
            VerticalOptions = LayoutOptions.Center
        });

        var statusLabel = new Label
        {
            Text = GetStatusText(appointment.Status),
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = GetStatusColor(appointment.Status),
            BackgroundColor = GetStatusBackground(appointment.Status),
            Padding = new Thickness(9, 4),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };

        header.Add(statusLabel, 1, 0);

        content.Children.Add(header);

        // Клиент
        content.Children.Add(new Label
        {
            Text = appointment.CustomerName,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#DDDDDD")
        });

        // Услуга
        content.Children.Add(new Label
        {
            Text = appointment.ServiceName,
            FontSize = 13,
            TextColor = Color.FromArgb("#BBBBBB")
        });

        // При собственик показваме и салона/служителя.
        content.Children.Add(new Label
        {
            Text = $"{appointment.SalonName} • {appointment.EmployeeName}",
            FontSize = 11,
            TextColor = Color.FromArgb("#888888")
        });

        // Бутон за отмяна под статуса и информацията.
        bool canCancel =
            appointment.Status.Equals(
                "Confirmed",
                StringComparison.OrdinalIgnoreCase)
            ||
            appointment.Status.Equals(
                "Pending",
                StringComparison.OrdinalIgnoreCase);

        if (canCancel)
        {
            var cancelButton = new Button
            {
                Text = "Отмени резервацията",

                FontSize = 12,
                FontAttributes = FontAttributes.Bold,

                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#C30F45"),

                CornerRadius = 9,
                HeightRequest = 38,

                Padding = new Thickness(12, 4),

                HorizontalOptions = LayoutOptions.End,

                Margin = new Thickness(0, 3, 0, 0)
            };

            cancelButton.Clicked += async (sender, args) =>
            {
                await CancelAppointmentAsync(appointment);
            };

            content.Children.Add(cancelButton);
        }

        return new Border
        {
            BackgroundColor = Color.FromArgb("#242424"),

            Stroke = Colors.Transparent,
            StrokeThickness = 0,

            Padding = 13,

            StrokeShape = new RoundRectangle
            {
                CornerRadius = 13
            },

            Content = content
        };
    }

    // ==========================================
    // STATUS
    // ==========================================

    private string GetStatusText(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "pending" => "Чакаща",
            "confirmed" => "Потвърдена",
            "completed" => "Завършена",
            "cancelled" => "Отменена",
            "rejected" => "Отказана",
            "noshow" => "Неявяване",
            _ => status
        };
    }

    private Color GetStatusColor(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "pending" => Color.FromArgb("#E5B94C"),
            "confirmed" => Color.FromArgb("#00C853"),
            "completed" => Color.FromArgb("#64B5F6"),
            "cancelled" => Color.FromArgb("#999999"),
            "rejected" => Color.FromArgb("#E05252"),
            "noshow" => Color.FromArgb("#E05252"),
            _ => Colors.White
        };
    }

    private Color GetStatusBackground(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "pending" => Color.FromArgb("#332B16"),
            "confirmed" => Color.FromArgb("#153522"),
            "completed" => Color.FromArgb("#172B3D"),
            "cancelled" => Color.FromArgb("#303030"),
            "rejected" => Color.FromArgb("#351B1B"),
            "noshow" => Color.FromArgb("#351B1B"),
            _ => Color.FromArgb("#303030")
        };
    }
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // ==========================================
    // CANCEL APPOINTMENT
    // ==========================================

    private async Task CancelAppointmentAsync(
        ManagedAppointmentDto appointment)
    {
        bool confirm = await DisplayAlert(
            "Отмяна на резервация",
            $"Сигурен ли си, че искаш да отмениш резервацията на {appointment.CustomerName}?",
            "Да, отмени",
            "Назад");

        if (!confirm)
            return;

        try
        {
            await _appointmentApiService
                .CancelManagedAppointmentAsync(appointment.Id);

            await DisplayAlert(
                "Успешно",
                "Резервацията е отменена успешно.",
                "OK");

            await LoadAppointmentsAsync();
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlert(
                "Грешка",
                ex.Message,
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Грешка",
                ex.Message,
                "OK");
        }
    }
}
