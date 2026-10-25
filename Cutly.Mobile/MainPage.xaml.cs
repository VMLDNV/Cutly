using Cutly.Mobile.Pages;
using Cutly.Mobile.Services;

namespace Cutly.Mobile;

public partial class MainPage : ContentPage
{
    private readonly AuthApiService _authApiService;

    public MainPage(AuthApiService authApiService)
    {
        InitializeComponent();
        _authApiService = authApiService;
    }

    private async void OnCustomerClicked(
    object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(RegisterPage),
            new Dictionary<string, object>
            {
                ["AccountType"] = "Customer"
            });
    }

    private async void OnBarberClicked(
        object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(RegisterPage),
            new Dictionary<string, object>
            {
                ["AccountType"] = "Barber"
            });
    }
}
