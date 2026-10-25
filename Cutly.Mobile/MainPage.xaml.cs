namespace Cutly.Mobile;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnCustomerClicked(
        object sender, EventArgs e)
    {
        await DisplayAlert(
            "Cutly",
            "Избра регистрация като клиент.",
            "Продължи");
    }

    private async void OnBarberClicked(
        object sender, EventArgs e)
    {
        await DisplayAlert(
            "Cutly",
            "Избра регистрация като бръснар.",
            "Продължи");
    }
}
