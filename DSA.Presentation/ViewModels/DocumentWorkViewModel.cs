namespace DSA.Presentation.ViewModels;

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSA.Application.Services;
using DSA.Application.Interfaces;

/// <summary>
/// DTO Observable para binding bidireccional en la UI.
/// </summary>
public partial class MetadatosDto : ObservableObject
{
    [ObservableProperty] private string _folio = string.Empty;
    [ObservableProperty] private string _remitente = string.Empty;
    [ObservableProperty] private string _asunto = string.Empty;
    [ObservableProperty] private DateTimeOffset _fechaRecepcion = DateTimeOffset.Now;
    [ObservableProperty] private bool _esUrgente;
}

/// <summary>
/// ViewModel que orquesta la vista dividida. C# 12 Primary Constructor.
/// </summary>
public partial class DocumentWorkViewModel(IDocumentWorkflowService workflowService) : ObservableObject
{
    [ObservableProperty]
    private MetadatosDto _metadatos = new();

    [ObservableProperty]
    private Uri? _pdfSourceUri;

    [ObservableProperty]
    private bool _isLoadingPdf;

    [ObservableProperty]
    private bool _isAiExtractionComplete;

    /// <summary>
    /// Carga el documento físico en el WebView2 y simula la extracción de IA.
    /// </summary>
    public async Task CargarDocumentoAsync(string pathUnc)
    {
        IsLoadingPdf = true;
        
        // Mapeo de ruta UNC a un formato URI válido para WebView2
        PdfSourceUri = new Uri(pathUnc);

        // TODO: Invocar a IIAService para extraer semántica real
        await Task.Delay(1500); // Simulando inferencia Llama 3 local

        IsAiExtractionComplete = true;
        IsLoadingPdf = false;
    }

    [RelayCommand]
    private async Task GuardarMetadatosAsync()
    {
        // TODO: Mapear MetadatosDto a Entidad de Dominio y avanzar Vector de Estado a 'VALD' o 'CLAS'
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RechazarDocumentoAsync()
    {
        // TODO: Transición a estado Terminal 'RECH' (D[10]=1)
        await Task.CompletedTask;
    }
}
