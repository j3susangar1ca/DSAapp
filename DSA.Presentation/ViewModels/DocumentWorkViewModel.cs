namespace DSA.Presentation.ViewModels;

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
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
    private Documento? _documentoActual;

    [ObservableProperty] private MetadatosDto _metadatos = new();
    [ObservableProperty] private Uri? _pdfSourceUri;
    [ObservableProperty] private bool _isLoadingPdf;
    [ObservableProperty] private bool _isAiExtractionComplete;

    public DocumentWorkViewModel(IDocumentWorkflowService workflowService)
    {
        _workflowService = workflowService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    /// <summary>
    /// Propiedad calculada para el estado del botón "Validar y Guardar".
    /// Valida que la IA terminó y que la entidad tiene integridad (D[4] SEAL).
    /// </summary>
    public bool CanExecuteValidation => 
        IsAiExtractionComplete && 
        (_documentoActual?.IsSellado ?? false);

    public async Task CargarDocumentoAsync(Documento documento)
    {
        _documentoActual = documento;
        _dispatcherQueue.TryEnqueue(() => 
        {
            IsLoadingPdf = true;
            PdfSourceUri = new Uri(documento.PathUNC);
            GuardarMetadatosCommand.NotifyCanExecuteChanged();
        });

        await Task.Run(async () =>
        {
            await Task.Delay(1500); // TODO: Reemplazar por invocación real a IIAService

            _dispatcherQueue.TryEnqueue(() =>
            {
                Metadatos.Folio = "001-2026";
                Metadatos.EsUrgente = documento.IsUrgent;
                IsAiExtractionComplete = true;
                IsLoadingPdf = false;
            });
        });
    }

    [RelayCommand(CanExecute = nameof(CanExecuteValidation))]
    private async Task GuardarMetadatosAsync()
    {
        try
        {
            if (_documentoActual == null) return;

            // Transición lógica en la entidad (Máquina de Estados)
            _documentoActual.SetUrgencia(Metadatos.EsUrgente);
            _documentoActual.ValidarClasificacion();

            // Persistencia
            await Task.Run(async () => 
            {
                await _workflowService.ActualizarDocumentoAsync(_documentoActual);
            });

            _dispatcherQueue.TryEnqueue(() =>
            {
                IsAiExtractionComplete = false; 
                // Notificar éxito si es necesario
            });
        }
        catch (InvalidOperationException ex)
        {
            await MostrarDialogoErrorAsync("Error de Integridad Lógica", ex.Message);
        }
        catch (Exception ex)
        {
            await MostrarDialogoErrorAsync("Error Crítico", "No se pudo completar la operación: " + ex.Message);
        }
    }

    [RelayCommand]
    private async Task RechazarDocumentoAsync()
    {
        if (_documentoActual == null) return;

        try 
        {
            await Task.Run(async () =>
            {
                _documentoActual.Rechazar();
                await _workflowService.ActualizarDocumentoAsync(_documentoActual);
            });

            _dispatcherQueue.TryEnqueue(() =>
            {
                PdfSourceUri = null;
            });
        }
        catch (Exception ex)
        {
            await MostrarDialogoErrorAsync("Error al Rechazar", ex.Message);
        }
    }

    private async Task MostrarDialogoErrorAsync(string titulo, string contenido)
    {
        if (App.MainStackVisualRoot == null) return;

        _dispatcherQueue.TryEnqueue(async () =>
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = titulo,
                Content = contenido,
                CloseButtonText = "Entendido",
                XamlRoot = App.MainStackVisualRoot 
            };

            await dialog.ShowAsync();
        });
    }

    partial void OnIsAiExtractionCompleteChanged(bool value) => GuardarMetadatosCommand.NotifyCanExecuteChanged();
}
