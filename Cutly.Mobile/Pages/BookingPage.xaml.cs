using Cutly.Mobile.Services;
using System.Net;

namespace Cutly.Mobile.Pages;

public partial class BookingPage : ContentPage, IQueryAttributable
{
    private readonly AppointmentApiService _appointmentApiService;

    private int _employeeServiceId;
    private decimal _price;
    private int _durationMinutes;

    private AvailableSlotDto? _selectedSlot;
    private Button? _selectedSlotButton;

    private bool _initialized;
    private bool _isBooking;

    public BookingPage(
        AppointmentApiService appointmentApiService)
    {
        InitializeComponent();

        _appointmentApiService = appointmentApiService;

        AppointmentDatePicker.MinimumDate = DateTime.Today;
        AppointmentDatePicker.MaximumDate = DateTime.Today.AddDays(90);

        AppointmentDatePicker.Date = DateTime.Today.AddDays(1);
    }

    public void ApplyQueryAttributes(
        IDictionary<string, object> query)
    {
        if (query.TryGetValue("EmployeeServiceId", out var id))
            _employeeServiceId = Convert.ToInt32(id);

        if (query.TryGetValue("EmployeeName", out var employeeName))
            EmployeeNameLabel.Text = employeeName?.ToString() ?? "Служител";

        if (query.TryGetValue("ServiceName", out var serviceName))
            ServiceNameLabel.Text = serviceName?.ToString() ?? "Услуга";

        if (query.TryGetValue("Price", out var price))
            _price = Convert.ToDecimal(price);

        if (query.TryGetValue("DurationMinutes", out var duration))
            _durationMinutes = Convert.ToInt32(duration);

        ServiceDetailsLabel.Text =
            $"{_price:F2} лв. · {_durationMinutes} мин.";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialized || _employeeServiceId <= 0)
            return;

        _initialized = true;

        await LoadAvailableSlotsAsync();
    }

    private async Task LoadAvailableSlotsAsync()
    {
        SlotsContainer.Children.Clear();

        _selectedSlot = null;
        _selectedSlotButton = null;

        BookButton.IsEnabled = false;

        SelectedSlotLabel.Text = "Все още не е избран час.";
        NoSlotsLabel.IsVisible = false;

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var selectedDate = DateOnly.FromDateTime(
                 AppointmentDatePicker.Date ?? DateTime.Today);

            var slots = await _appointmentApiService
                .GetAvailableSlotsAsync(
                    _employeeServiceId,
                    selectedDate);

            if (slots.Count == 0)
            {
                NoSlotsLabel.IsVisible = true;
                return;
            }

            foreach (var slot in slots.OrderBy(
                         s => s.StartDateTime))
            {
                var localTime = slot.StartDateTime.ToLocalTime();

                var slotButton = new Button
                {
                    Text = localTime.ToString("HH:mm"),
                    BackgroundColor = Color.FromArgb("#242424"),
                    TextColor = Colors.White,
                    BorderColor = Color.FromArgb("#444444"),
                    BorderWidth = 1,
                    CornerRadius = 10,
                    WidthRequest = 95,
                    HeightRequest = 48,
                    Margin = new Thickness(0, 0, 10, 10),
                    FontSize = 15
                };

                slotButton.Clicked += (_, _) =>
                {
                    SelectSlot(slot, slotButton);
                };

                SlotsContainer.Children.Add(slotButton);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            await DisplayAlert("Необходимо е влизане", ex.Message, "OK");
        }
        catch (HttpRequestException)
        {
            await DisplayAlert(
                "Грешка",
                "Не успяхме да заредим свободните часове.",
                "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlert(
                "Връзката прекъсна",
                "Заявката отне твърде много време.",
                "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private void SelectSlot(
        AvailableSlotDto slot,
        Button button)
    {
        if (_selectedSlotButton != null)
        {
            _selectedSlotButton.BackgroundColor =
                Color.FromArgb("#242424");

            _selectedSlotButton.TextColor = Colors.White;
        }

        _selectedSlot = slot;
        _selectedSlotButton = button;

        button.BackgroundColor = Color.FromArgb("#F2F2F2");
        button.TextColor = Color.FromArgb("#111111");

        SelectedSlotLabel.Text =
            $"Избран час: {slot.StartDateTime.ToLocalTime():HH:mm}";

        BookButton.IsEnabled = true;
    }

    private async void OnDateSelected(
        object sender,
        DateChangedEventArgs e)
    {
        if (!_initialized)
            return;

        await LoadAvailableSlotsAsync();
    }

    private async void OnBookClicked(
        object sender,
        EventArgs e)
    {
        if (_selectedSlot == null || _isBooking)
            return;

        _isBooking = true;
        BookButton.IsEnabled = false;
        BookButton.Text = "Изпращане...";

        try
        {
            var request = new CreateAppointmentRequest
            {
                EmployeeServiceId = _employeeServiceId,

                // Изпращаме оригиналния UTC час от API.
                StartDateTime = _selectedSlot.StartDateTime,

                CustomerNotes = string.IsNullOrWhiteSpace(
                    NotesEditor.Text)
                    ? null
                    : NotesEditor.Text.Trim()
            };

            var result = await _appointmentApiService
                .CreateAppointmentAsync(request);

            await DisplayAlert(
                "Резервацията е изпратена",
                $"Резервация №{result.Id}\n" +
                $"Статус: {result.Status}\n" +
                $"Цена: {result.Price:F2} лв.\n\n" +
                "Очаквайте потвърждение от салона.",
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (UnauthorizedAccessException ex)
        {
            await DisplayAlert("Необходимо е влизане", ex.Message, "OK");
        }
        catch (HttpRequestException ex)
        {
            var message = ex.StatusCode == HttpStatusCode.Conflict
                ? "Този час вече не е свободен. Моля, изберете друг."
                : "Неуспешно създаване на резервацията. Опитайте отново.";

            await DisplayAlert("Резервацията не беше създадена", message, "OK");

            await LoadAvailableSlotsAsync();
        }
        catch (TaskCanceledException)
        {
            await DisplayAlert(
                "Връзката прекъсна",
                "Не успяхме да изпратим резервацията.",
                "OK");
        }
        finally
        {
            _isBooking = false;
            BookButton.Text = "Потвърди резервацията";

            if (_selectedSlot != null)
                BookButton.IsEnabled = true;
        }
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
