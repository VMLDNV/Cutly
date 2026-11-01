using Microsoft.Extensions.Logging;
using Cutly.Mobile.Services;
using Cutly.Mobile.Pages;

namespace Cutly.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<AuthApiService>();
            builder.Services.AddSingleton<SalonApiService>();
            builder.Services.AddSingleton<FavoritesApiService>();
            builder.Services.AddSingleton<AppointmentApiService>();


            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<CustomerHomePage>();
            builder.Services.AddTransient<SalonDetailsPage>();
            builder.Services.AddTransient<FavoritesPage>();
            builder.Services.AddTransient<BookingPage>();
            builder.Services.AddTransient<ServiceEmployeesPage>();
            builder.Services.AddTransient<MyAppointmentsPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif
            

            return builder.Build();
        }
    }
}
