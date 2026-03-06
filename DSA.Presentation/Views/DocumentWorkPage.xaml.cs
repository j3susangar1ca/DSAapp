namespace DSA.Presentation.Views;

using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection; // Para GetRequiredService
using DSA.Presentation.ViewModels;

/// <summary>
/// Code-Behind de la vista de trabajo dividida. Maneja exclusivamente el ciclo de vida del UI Control.
/// </summary>
public sealed partial class DocumentWorkPage : Page
{
    public DocumentWorkViewModel ViewModel { get; }

    public DocumentWorkPage()
    {
        this.InitializeComponent();
        
        // Resolución del ViewModel inyectado en App.xaml.cs
        ViewModel = App.RootScope.ServiceProvider.GetRequiredService<DocumentWorkViewModel>();
        
        // Suscripción segura a eventos del ciclo de vida visual
        this.Loaded += OnPageLoaded;
        this.Unloaded += OnPageUnloaded;
    }

    private async void OnPageLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            await InicializarWebView2Async();
        }
        catch (Exception ex)
        {
            ViewModel.Metadatos.Asunto = $"ERROR CRÍTICO AL CARGAR LA PÁGINA: {ex.Message}";
        }
    }

    private async Task InicializarWebView2Async()
    {
        try
        {
            // Creación de entorno aislado para caché de Edge, requerido por WinUI 3
            string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SIA_WebView2_Data");
            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            
            // Inicialización asíncrona atada al UI Thread
            await PdfViewer.EnsureCoreWebView2Async(environment);

            // Virtual Host Mapping: Transforma la ruta de red UNC a un dominio virtual seguro
            // Permite renderizar "\\SERVER_SIA\Acervo_SIA" como "http://sia.local" esquivando el bloqueo "file://"
            PdfViewer.CoreWebView2.SetVirtualHostNameToFolderMapping(
                hostName: "sia.local", 
                folderPath: @"\\SERVER_SIA\Acervo_SIA", // Taxonomía CADIDO
                accessKind: CoreWebView2HostResourceAccessKind.Allow);
        }
        catch (Exception ex)
        {
            // Fallback visual en caso de que WebView2 Runtime no esté instalado
            ViewModel.Metadatos.Asunto = $"FALLA CRÍTICA DE RENDERIZADO: {ex.Message}";
        }
    }

    private void OnPageUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Prevención estricta de Memory Leaks: Cierre explícito del proceso subyacente msedgewebview2.exe
        PdfViewer?.Close();
    }
}
