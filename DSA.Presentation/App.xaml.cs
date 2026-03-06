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
using DSA.Presentation.ViewModels;

namespace DSA.Presentation;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    // Raíz visual para diálogos (ContentDialog)
    public static Microsoft.UI.Xaml.XamlRoot? MainStackVisualRoot { get; set; }

    public App()
    {
        this.InitializeComponent();
        
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        
        Services = serviceCollection.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 1. Persistencia: PostgreSQL configurado
        services.AddDbContext<SiaDbContext>(options =>
            options.UseNpgsql("Host=localhost;Database=dsa_db;Username=postgres;Password=tu_password"));

        // 2. Repositorios
        services.AddScoped<IDocumentoRepository, DocumentoRepository>();
        
        // 3. Servicios de Aplicación e Infraestructura
        services.AddSingleton<IScannerService, ScannerService>();
        services.AddSingleton<ISecurityContext, FakeSecurityContext>();
        services.AddScoped<IDocumentWorkflowService, DocumentWorkflowService>();
        services.AddSingleton<AnalyticsService>();
        services.AddSingleton<DigitizationService>();
        services.AddSingleton<IStorageService, UncStorageService>();
        services.AddHttpClient();
        services.AddSingleton<IIAService, GeminiIAService>();

        // 4. ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<CapturaViewModel>();
        services.AddTransient<DocumentWorkViewModel>();
        services.AddTransient<DashboardViewModel>();
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
