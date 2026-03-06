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
using DSA.Presentation.ViewModels;

namespace DSA.Presentation;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        this.InitializeComponent();
        
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        
        Services = serviceCollection.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 1. Persistencia
        services.AddDbContext<SiaDbContext>(options =>
            options.UseInMemoryDatabase("SIA_DB")); 

        // 2. Repositorios
        services.AddScoped<IDocumentoRepository, DocumentoRepository>();
        
        // 3. Servicios
        services.AddSingleton<IScannerService, ScannerService>();
        services.AddSingleton<ISecurityContext, FakeSecurityContext>();
        services.AddScoped<IDocumentWorkflowService, DocumentWorkflowService>();
        services.AddSingleton<DigitizationService>();

        // 4. ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<CapturaViewModel>();
        services.AddTransient<DocumentWorkViewModel>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }

    private Window? m_window;
}

public class FakeSecurityContext : ISecurityContext
{
    public bool CurrentUserHasRole(string roleName) => true;
}
