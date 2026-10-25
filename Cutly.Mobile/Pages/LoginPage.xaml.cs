using Cutly.Mobile.Services;

namespace Cutly.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthApiService _authApiService;

    public LoginPage(AuthApiService authApiService)
    {
        InitializeComponent();
        _authApiService = authApiService;
    }

    private async void OnLoginClicked(
        object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            ShowError("Въведи имейл и парола.");
            return;
        }

        LoginButton.IsEnabled = false;
        LoginButton.Text = "Влизане...";

        try
        {
            var success = await _authApiService.LoginAsync(
                email,
                password);

            if (success)
            {
                await DisplayAlert(
                    "Cutly",
                    "Входът е успешен!",
                    "OK");

                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                ShowError(
                    "Входът не бе успешен. Провери имейла и паролата.");
            }
        }
        catch (Exception)
        {
            ShowError(
                "Не успяхме да се свържем със сървъра.");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = "Вход";
        }
    }

    private async void OnBackClicked(
        object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
