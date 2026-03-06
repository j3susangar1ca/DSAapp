using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DSA.Application.Interfaces;
using DSA.Application.Services;
using DSA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DSA.Presentation;

public partial class App : Microsoft.UI.Xaml.Application
{
    public static IHost Host { get; private set; }

    public App()
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Capa de Infraestructura (DSA.Infrastructure)
                services.AddDbContext<SiaDbContext>(options =>
                    options.UseNpgsql("Host=localhost;Database=dsa_db;Username=postgres;Password=tu_password"));

                // Capa de Aplicación (DSA.Application)
                services.AddScoped<DigitizationService>();
                
                // Registro de dependencias para el procesamiento de documentos
                // services.AddSingleton<IScannerService, ScannerService>();
                // services.AddSingleton<IHashService, HashService>();

                // ViewModels y UI
                services.AddTransient<MainWindow>();
            })
            .Build();

        this.InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = Host.Services.GetRequiredService<MainWindow>();
        m_window.Activate();
    }

    private Microsoft.UI.Xaml.Window m_window;
}