namespace DSA.Presentation.ViewModels;

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using DSA.Application.Interfaces;
using DSA.Domain.Entities;

public partial class MetadatosDto : ObservableObject
{
    [ObservableProperty] private string _folio = string.Empty;
    [ObservableProperty] private string _remitente = string.Empty;
    [ObservableProperty] private string _asunto = string.Empty;
    [ObservableProperty] private DateTimeOffset _fechaRecepcion = DateTimeOffset.Now;
    [ObservableProperty] private bool _esUrgente;
}

public partial class DocumentWorkViewModel : ObservableObject
{
    private readonly IDocumentWorkflowService _workflowService;
    private readonly DispatcherQueue _dispatcherQueue;
    private Documento? _documentoActual; // Referencia a la entidad de dominio

    [ObservableProperty] private MetadatosDto _metadatos = new();
    [ObservableProperty] private Uri? _pdfSourceUri;
    [ObservableProperty] private bool _isLoadingPdf;
    [ObservableProperty] private bool _isAiExtractionComplete;

    public DocumentWorkViewModel(IDocumentWorkflowService workflowService)
    {
        _workflowService = workflowService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread(); // Asegura el hilo UI
    }

    public async Task CargarDocumentoAsync(Documento documento)
    {
        _documentoActual = documento;
        IsLoadingPdf = true;
        
        PdfSourceUri = new Uri(documento.PathUNC);

        // Se lanza a un hilo secundario para evitar bloqueo de la UI
        await Task.Run(async () =>
        {
            await Task.Delay(1500); // TODO: Reemplazar por invocación real a IIAService

            _dispatcherQueue.TryEnqueue(() =>
            {
                // Mapeo simulado de la IA
                Metadatos.Folio = "001-2026";
                Metadatos.EsUrgente = documento.IsUrgent;
                IsAiExtractionComplete = true;
                IsLoadingPdf = false;
            });
        });
    }

    [RelayCommand]
    private async Task GuardarMetadatosAsync()
    {
        if (_documentoActual == null) return;

        await Task.Run(() =>
        {
            // Mapeo DTO -> Dominio
            _documentoActual.SetUrgencia(Metadatos.EsUrgente);
            _documentoActual.ValidarClasificacion();

            // Persistencia a través del servicio de aplicación
            // await _workflowService.ActualizarDocumentoAsync(_documentoActual);

            _dispatcherQueue.TryEnqueue(() =>
            {
                // Notificar éxito a la UI (puedes agregar un InfoBar de éxito aquí)
                IsAiExtractionComplete = false; 
            });
        });
    }

    [RelayCommand]
    private async Task RechazarDocumentoAsync()
    {
        if (_documentoActual == null) return;

        await Task.Run(() =>
        {
            _documentoActual.Rechazar();
            // await _workflowService.ActualizarDocumentoAsync(_documentoActual);

            _dispatcherQueue.TryEnqueue(() =>
            {
                // Cierra la vista o limpia el formulario
                PdfSourceUri = null;
            });
        });
    }
}

