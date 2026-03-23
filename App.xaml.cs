using DSAapp.Activation;
using DSAapp.Contracts.Services;
using DSAapp.Core.Contracts.Services;
using DSAapp.Core.Services;
using DSAapp.Helpers;
using DSAapp.Models;
using DSAapp.Notifications;
using DSAapp.Services;
using DSAapp.ViewModels;
using DSAapp.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace DSAapp;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    public IHost Host
    {
        get;
    }

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar
    {
        get; set;
    }

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<IWebViewService, WebViewService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<ISampleDataService, SampleDataService>();
            services.AddSingleton<IFileService, FileService>();
            // Scanner & OCR services
            services.AddSingleton<IScannerService, ScannerService>();
            services.AddSingleton<IOcrService, OcrService>();

            // ------------------------------------------------------------------
            // [NUEVO] SERVICIOS DE BASE DE DATOS (INYECCIÓN DE DEPENDENCIAS)
            // ------------------------------------------------------------------
            services.AddDbContext<AppDbContext>();

            // Views and ViewModels
            // [NUEVO] Registramos tu TareasViewModel
            services.AddTransient<TareasViewModel>();

            // Scanner viewmodel & page
            services.AddTransient<EscanerViewModel>();
            services.AddTransient<EscanerPage>();

            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<DetallesDeListaViewModel>();
            services.AddTransient<DetallesDeListaPage>();
            services.AddTransient<CuadrículaDeContenidoDetailViewModel>();
            services.AddTransient<CuadrículaDeContenidoDetailPage>();
            services.AddTransient<CuadrículaDeContenidoViewModel>();
            services.AddTransient<CuadrículaDeContenidoPage>();
            services.AddTransient<CuadrículaDeDatosViewModel>();
            services.AddTransient<CuadrículaDeDatosPage>();
            services.AddTransient<VistaWebViewModel>();
            services.AddTransient<VistaWebPage>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();
            services.AddTransient<OficiosViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        App.GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        System.Diagnostics.Debug.WriteLine($"[CRASH]: {e.Message}");
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        // ------------------------------------------------------------------
        // [NUEVO] MIGRACIÓN EN TIEMPO DE EJECUCIÓN (CREA LA DB EN LA RED)
        // ------------------------------------------------------------------
        try
        {
            using (var db = new AppDbContext())
            {
                System.Diagnostics.Debug.WriteLine("[App] Construyendo base de datos en la red institucional...");

                // Esto lee los planos y fabrica el .db físicamente en tu ruta 10.2.1.92
                db.Database.Migrate();

                System.Diagnostics.Debug.WriteLine("[App] Base de datos viva y lista para trabajar.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ALERTA ROJA SMB] No se pudo crear la base de datos: {ex.Message}");
        }
        // ------------------------------------------------------------------

        App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));

        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}