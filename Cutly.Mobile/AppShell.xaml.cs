using Cutly.Mobile.Pages;

namespace Cutly.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(CustomerHomePage), typeof(CustomerHomePage));
        Routing.RegisterRoute(nameof(SalonDetailsPage), typeof(SalonDetailsPage));
        Routing.RegisterRoute(nameof(FavoritesPage), typeof(FavoritesPage));
        Routing.RegisterRoute(nameof(BookingPage), typeof(BookingPage));
        Routing.RegisterRoute(nameof(ServiceEmployeesPage), typeof(ServiceEmployeesPage));
        Routing.RegisterRoute(nameof(MyAppointmentsPage), typeof(MyAppointmentsPage));
        Routing.RegisterRoute(nameof(BusinessAppointmentsPage), typeof(BusinessAppointmentsPage));
        Routing.RegisterRoute(nameof(MySchedulePage), typeof(MySchedulePage));
    }
}
