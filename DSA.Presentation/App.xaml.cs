using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using System;
using DSA.Domain.Interfaces;
using DSA.Application.Interfaces;
using DSA.Application.Services;
using DSA.Infrastructure.Persistence;
using DSA.Infrastructure.Hardware;
using DSA.Infrastructure.Storage;
using DSA.Infrastructure.AI;
using Microsoft.Windows.AppNotifications;
using DSA.Presentation.Services;
using DSA.Presentation.ViewModels;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.Logging;

namespace DSA.Presentation;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    // FIX: Scope raíz para resolver servicios Scoped (DbContext, Repos, ViewModels)
    // desde las Pages de WinUI 3 sin un HTTP request scope.
    public static IServiceScope RootScope { get; private set; } = null!;

    public IConfiguration Configuration { get; }
    public static Microsoft.UI.Xaml.XamlRoot? MainStackVisualRoot { get; set; }

    public App()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        Configuration = builder.Build();
        this.InitializeComponent();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        Services  = serviceCollection.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = false, ValidateOnBuild = false });

        // FIX: Scope único de aplicación — todas las Pages usan RootScope.ServiceProvider
        RootScope = Services.CreateScope();

        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
        AppNotificationManager.Default.Register();
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args) { }

    private void ConfigureServices(IServiceCollection services)
    {
        string connectionString = Configuration.GetConnectionString("PostgreSql")!;

        // 1. Persistencia
        services.AddDbContextPool<SiaDbContext>(options =>
            options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure(3)));

        // 2. Repositorios (Scoped — ligados al DbContext)
        services.AddScoped<IDocumentoRepository, DocumentoRepository>();

        // 3. Infraestructura Singleton
        services.AddSingleton(Configuration);
        services.AddSingleton<IScannerService, ScannerService>();
        services.AddSingleton<ISecurityContext, FakeSecurityContext>();
        services.AddSingleton<IStorageService, UncStorageService>();
        services.AddSingleton<NativeNotificationService>();

        // FIX: IOCRService no estaba registrado → DocumentWorkViewModel fallaba al resolver
        services.AddSingleton<IOCRService, StubOcrService>();

        // 4. Servicios de Aplicación (Scoped)
        services.AddScoped<IDocumentWorkflowService, DocumentWorkflowService>();
        services.AddScoped<DigitizationService>();
        services.AddScoped<RelationService>();

        // FIX: AnalyticsService era Singleton consumiendo IDocumentoRepository Scoped → captive dependency
        services.AddScoped<AnalyticsService>();

        // FIX: GeminiIAService necesita HttpClient → usar cliente tipado, no AddSingleton directo
        services.AddHttpClient<GeminiIAService>();
        services.AddSingleton<IIAService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client  = factory.CreateClient(nameof(GeminiIAService));
            var config  = sp.GetRequiredService<IConfiguration>();
            return new GeminiIAService(client, config);
        });

        // 5. ViewModels (Scoped — resueltos desde RootScope en las Pages)
        // FIX: eran AddTransient pero consumen servicios Scoped;
        // resolver desde root container lanzaba InvalidOperationException.
        services.AddScoped<MainViewModel>();
        services.AddScoped<CapturaViewModel>();
        services.AddScoped<DocumentWorkViewModel>();
        services.AddScoped<DashboardViewModel>();
        services.AddScoped<BusquedaViewModel>();

        // 6. Logging
        services.AddLogging(logging =>
        {
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Debug);
        });
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();

        if (m_window.Content is FrameworkElement rootElement)
            rootElement.Loaded += (s, e) => MainStackVisualRoot = rootElement.XamlRoot;
    }

    private Window? m_window;
}

public class FakeSecurityContext : ISecurityContext
{
    public bool CurrentUserHasRole(string roleName) => true;
}

// FIX: placeholder hasta integrar Tesseract en DSA.Infrastructure
public sealed class StubOcrService : IOCRService
{
    public Task<string> ExtractTextAsync(byte[] content) =>
        Task.FromResult("[OCR pendiente — integrar Tesseract.Net]");
}