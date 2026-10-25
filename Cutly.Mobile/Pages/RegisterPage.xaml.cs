
using Cutly.Mobile.Services;

namespace Cutly.Mobile.Pages;

public partial class RegisterPage : ContentPage, IQueryAttributable
{
    private readonly AuthApiService _authApiService;
    private string _accountType = "Customer";

    public RegisterPage(AuthApiService authApiService)
    {
        InitializeComponent();
        _authApiService = authApiService;
    }

    public void ApplyQueryAttributes(
        IDictionary<string, object> query)
    {
        if (query.TryGetValue("AccountType", out var value)
            && value is string accountType)
        {
            _accountType = accountType;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AccountTypeLabel.Text =
                    accountType == "Barber"
                        ? "Регистрация като бръснар"
                        : "Регистрация като клиент";
            });
        }
    }

    private async void OnRegisterClicked(
        object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        var firstName = FirstNameEntry.Text?.Trim();
        var lastName = LastNameEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var confirmPassword = ConfirmPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            ShowError("Попълни всички полета.");
            return;
        }

        if (password != confirmPassword)
        {
            ShowError("Паролите не съвпадат.");
            return;
        }

        RegisterButton.IsEnabled = false;
        RegisterButton.Text = "Регистриране...";

        try
        {
            var success = await _authApiService.RegisterAsync(
                firstName,
                lastName,
                email,
                password,
                _accountType);

            if (success)
            {
                await DisplayAlert(
                    "Cutly",
                    "Регистрацията е успешна. Можеш да влезеш в профила си.",
                    "OK");

                await Navigation.PopAsync();
            }
            else
            {
                ShowError("Регистрацията не бе успешна. Провери данните и опитай отново.");
            }
        }
        catch (Exception)
        {
            ShowError("Не успяхме да се свържем със сървъра. Провери дали Cutly API работи.");
        }
        finally
        {
            RegisterButton.IsEnabled = true;
            RegisterButton.Text = "Създай профил";
        }
    }

    private async void OnLoginClicked(
    object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(LoginPage));
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
