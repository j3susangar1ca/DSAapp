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
    private readonly IIAService _iaService; // Motor Gemini/Ollama inyectado
    private readonly IOCRService _ocrService; // Motor Tesseract inyectado
    private readonly DispatcherQueue _dispatcherQueue;
    private Documento? _documentoActual;

    [ObservableProperty] private MetadatosDto _metadatos = new();
    [ObservableProperty] private Uri? _pdfSourceUri;
    [ObservableProperty] private bool _isLoadingPdf;
    [ObservableProperty] private bool _isAiExtractionComplete;

    public DocumentWorkViewModel(
        IDocumentWorkflowService workflowService,
        IIAService iaService,
        IOCRService ocrService)
    {
        _workflowService = workflowService;
        _iaService = iaService;
        _ocrService = ocrService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    /// <summary>
    /// Propiedad calculada para el estado del botón "Validar y Guardar".
    /// Valida que la IA terminó y que la entidad tiene integridad (D[4] SEAL).
    /// </summary>
    public bool CanExecuteValidation => 
        IsAiExtractionComplete && 
        (_documentoActual?.IsSellado ?? false);

    public async Task CargarDocumentoAsync(Documento documento, byte[] rawPdfBytes)
    {
        _documentoActual = documento;
        
        // Actualización inicial de UI
        _dispatcherQueue.TryEnqueue(() => 
        {
            IsLoadingPdf = true;
            IsAiExtractionComplete = false;
            PdfSourceUri = new Uri(documento.PathUNC);
            GuardarMetadatosCommand.NotifyCanExecuteChanged();
        });

        // Procesamiento en hilo de fondo para evitar congelamiento de WinUI 3
        await Task.Run(async () =>
        {
            try 
            {
                // 1. Fase Óptica: Extracción de texto crudo
                string ocrText = await _ocrService.ExtractTextAsync(rawPdfBytes);

                // 2. Fase Cognitiva: Análisis semántico con Gemini
                var aiResult = await _iaService.AnalyzeSemanticsAsync(ocrText);

                // 3. Sincronización con UI Thread para mapeo de MetadatosDto
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Metadatos.Folio = aiResult.Folio;
                    Metadatos.Remitente = aiResult.Remitente;
                    Metadatos.Asunto = aiResult.Asunto;
                    Metadatos.EsUrgente = aiResult.Urgente;
                    
                    IsAiExtractionComplete = true; // Activa InfoBar visual
                    IsLoadingPdf = false;
                    GuardarMetadatosCommand.NotifyCanExecuteChanged(); // Habilita botón Validar
                });
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() => IsLoadingPdf = false);
                await MostrarDialogoErrorAsync("Error de Procesamiento", "No se pudo extraer la información: " + ex.Message);
            }
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
