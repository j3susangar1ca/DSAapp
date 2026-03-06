using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using System;

// Espacios de nombres del proyecto SIA
using dsApp.Domain.Interfaces;
using dsApp.Domain.Services;
// using dsApp.Infrastructure.Data; // Asumiendo el namespace de tu DbContext

namespace dsApp.Presentation;

public partial class App : Application
{
    // Propiedad estática para acceder al contenedor desde cualquier parte de la app
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        this.InitializeComponent();
        
        // Inicialización del contenedor de servicios
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        
        Services = serviceCollection.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 1. Persistencia: PostgreSQL con soporte para JSONB y GIN (Optimizado para SIA)
        /* services.AddDbContext<SiaDbContext>(options =>
            options.UseNpgsql(
                "Host=localhost;Database=SIA_DB;Username=admin;Password=secret",
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
            )); 
        */

        // 2. Repositorios e Infraestructura
        services.AddScoped<IDocumentoRepository, DocumentoRepository>();
        
        // 3. Servicios de Hardware y Procesamiento (Singletons o Scoped)
        services.AddSingleton<IScannerService, ScannerService>(); // Wrapper TWAIN/WIA
        services.AddSingleton<IOCRService, TesseractOCRService>();
        services.AddSingleton<IIAService, OllamaService>(); // Cliente Llama 3 local
        services.AddSingleton<IStorageService, UncStorageService>(); // Gestión rutas UNC

        // 4. ViewModels (Presentación)
        services.AddTransient<MainViewModel>();
        services.AddTransient<CapturaViewModel>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Resolución de la ventana principal mediante DI
        m_window = new MainWindow();
        m_window.Activate();
    }

    private Window? m_window;
}