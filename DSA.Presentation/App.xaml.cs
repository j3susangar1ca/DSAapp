using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using System;
using DSA.Domain.Interfaces;
using DSA.Domain.Entities;
using DSA.Application.Interfaces;
using DSA.Application.Services;
using DSA.Infrastructure.Persistence;
using DSA.Infrastructure.Hardware;
using DSA.Infrastructure.Storage;
using DSA.Infrastructure.AI;
using Microsoft.Windows.AppNotifications;
using DSA.Presentation.Services;

using Microsoft.Extensions.Configuration;
using System.IO;

namespace DSA.Presentation;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public IConfiguration Configuration { get; }

    // Raíz visual para diálogos (ContentDialog)
    public static Microsoft.UI.Xaml.XamlRoot? MainStackVisualRoot { get; set; }

    public App()
    {
        // Carga jerárquica de configuración (Fase 5 del Roadmap)
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables(); // Prioriza variables de entorno en producción

        Configuration = builder.Build();

        this.InitializeComponent();
        
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        
        Services = serviceCollection.BuildServiceProvider();

        // Registro e Inicialización de Notificaciones Nativas (Windows App SDK)
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
        AppNotificationManager.Default.Register();
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        // Lógica para manejar clics en notificaciones (ej. navegar a la página del documento)
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Inyección de ConnectionString desde el gestor de configuración
        string connectionString = Configuration.GetConnectionString("PostgreSql")!;
        
        // 1. Persistencia: PostgreSQL configurado
        services.AddDbContext<SiaDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 2. Repositorios
        services.AddScoped<IDocumentoRepository, DocumentoRepository>();
        
        // 3. Servicios de Aplicación e Infraestructura
        services.AddSingleton(Configuration); // Registro de IConfiguration
        services.AddSingleton<IScannerService, ScannerService>();
        services.AddSingleton<ISecurityContext, FakeSecurityContext>();
        services.AddSingleton<IStorageService, UncStorageService>();
        services.AddScoped<IDocumentWorkflowService, DocumentWorkflowService>();
        services.AddScoped<DigitizationService>();
        services.AddScoped<RelationService>();
        services.AddSingleton<AnalyticsService>();
        services.AddHttpClient();
        services.AddSingleton<IIAService, GeminiIAService>();
        services.AddSingleton<NativeNotificationService>(); // Servicio nativo de alertas

        // 4. ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<CapturaViewModel>();
        services.AddTransient<DocumentWorkViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<BusquedaViewModel>(); // ViewModel de Búsqueda
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
        
        // Captura de la raíz visual para diálogos
        MainStackVisualRoot = m_window.Content.XamlRoot;
    }

    private Window? m_window;
}

public class FakeSecurityContext : ISecurityContext
{
    public bool CurrentUserHasRole(string roleName) => true;
}
