using Cutly.Mobile.Services;
using Microsoft.Maui.Controls.Shapes;

namespace Cutly.Mobile.Pages;

public partial class BusinessAppointmentsPage : ContentPage
{
    private readonly AppointmentApiService _appointmentApiService;

    private List<ManagedAppointmentDto> _appointments = new();

    private bool _isLoading;

    public BusinessAppointmentsPage(
        AppointmentApiService appointmentApiService)
    {
        InitializeComponent();

        _appointmentApiService = appointmentApiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadAppointmentsAsync();
    }

    // Зареждане на резервациите от API
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

            _appointments =
                await _appointmentApiService.GetManagedAppointmentsAsync();

            DisplayAppointments();
        }
        catch (UnauthorizedAccessException ex)
        {
            await DisplayAlert("Вход", ex.Message, "OK");
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
            AppointmentsRefreshView.IsRefreshing = false;
        }
    }

    // Показване на резервациите
    private void DisplayAppointments()
    {
        AppointmentsContainer.Children.Clear();

        if (_appointments.Count == 0)
        {
            EmptyLabel.IsVisible = true;
            AppointmentsContainer.Children.Add(EmptyLabel);
            return;
        }

        EmptyLabel.IsVisible = false;

        var sortedAppointments = _appointments
             .OrderByDescending(a => a.StartDateTime)
             .ToList();

        foreach (var appointment in sortedAppointments)
        {
            AppointmentsContainer.Children.Add(
                CreateAppointmentCard(appointment));
        }
    }

    // Създаване на карта за резервация
    private View CreateAppointmentCard(
        ManagedAppointmentDto appointment)
    {
        var content = new VerticalStackLayout
        {
            Spacing = 12
        };

        // Име на салона и статус
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        header.Add(new Label
        {
            Text = appointment.SalonName,
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            VerticalOptions = LayoutOptions.Center
        });

        header.Add(new Label
        {
            Text = GetStatusText(appointment.Status),
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = GetStatusColor(appointment.Status),
            VerticalOptions = LayoutOptions.Center
        }, 1, 0);

        content.Children.Add(header);

        AddInfo(content, "Клиент", appointment.CustomerName);
        AddInfo(content, "Имейл", appointment.CustomerEmail);
        AddInfo(content, "Служител", appointment.EmployeeName);
        AddInfo(content, "Услуга", appointment.ServiceName);

        AddInfo(
            content,
            "Дата",
            appointment.StartDateTime
                .ToLocalTime()
                .ToString("dd.MM.yyyy"));

        AddInfo(
            content,
            "Час",
            $"{appointment.StartDateTime.ToLocalTime():HH:mm} - " +
            $"{appointment.EndDateTime.ToLocalTime():HH:mm}");

        AddInfo(
            content,
            "Продължителност",
            $"{appointment.DurationMinutes} мин.");

        AddInfo(
            content,
            "Цена",
            $"{appointment.Price:F2} лв.");

        if (!string.IsNullOrWhiteSpace(appointment.CustomerNotes))
        {
            AddInfo(
                content,
                "Бележка от клиента",
                appointment.CustomerNotes);
        }

        // Бутон за отмяна само при потвърдена резервация
        bool canCancel =
            appointment.Status.Equals(
                "Confirmed",
                StringComparison.OrdinalIgnoreCase);

        if (canCancel)
        {
            var cancelButton = new Button
            {
                Text = "Отмени резервацията",
                BackgroundColor = Color.FromArgb("#C30F45"),
                TextColor = Colors.White,
                CornerRadius = 12,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 8, 0, 0)
            };

            cancelButton.Clicked += async (sender, args) =>
            {
                await CancelAppointmentAsync(appointment);
            };

            content.Children.Add(cancelButton);
        }

        return new Border
        {
            BackgroundColor = Color.FromArgb("#171717"),
            Stroke = Color.FromArgb("#292929"),
            StrokeThickness = 1,
            Padding = 16,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = 16
            },
            Content = content
        };
    }

    // Добавяне на информационен ред
    private void AddInfo(
        VerticalStackLayout container,
        string title,
        string value)
    {
        var row = new VerticalStackLayout
        {
            Spacing = 2
        };

        row.Children.Add(new Label
        {
            Text = title,
            FontSize = 11,
            TextColor = Color.FromArgb("#888888")
        });

        row.Children.Add(new Label
        {
            Text = value,
            FontSize = 14,
            TextColor = Colors.White
        });

        container.Children.Add(row);
    }

    // Превод на статусите
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

    // Цветове на статусите
    private Color GetStatusColor(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "pending" => Color.FromArgb("#E5B94C"),
            "confirmed" => Color.FromArgb("#4CAF50"),
            "completed" => Color.FromArgb("#64B5F6"),
            "cancelled" => Color.FromArgb("#999999"),
            "rejected" => Color.FromArgb("#E05252"),
            "noshow" => Color.FromArgb("#E05252"),
            _ => Colors.White
        };
    }

    // Отмяна на резервация от собственик или служител
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

    // Pull-to-refresh
    private async void OnRefresh(object sender, EventArgs e)
    {
        await LoadAppointmentsAsync();
    }

    // Назад
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
