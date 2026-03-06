// 1. VIEWMODEL: DSA.Presentation/ViewModels/CapturaViewModel.cs
namespace dsApp.Presentation.ViewModels;

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using dsApp.Domain.Interfaces; // OCR, IA
using DSA.Application.Interfaces; // IScannerService
using DSA.Application.Services;

/// <summary>
/// ViewModel de Captura. C# 12 Primary Constructor inyectando dependencias.
/// </summary>
public partial class CapturaViewModel(
    IScannerService scannerService,
    IOCRService ocrService,
    IIAService iaService,
    IDocumentWorkflowService workflowService) : ObservableObject
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Listo para escanear";

    [ObservableProperty]
    private string _slaColorBrush = "Green"; // Verde (Normal), Amarillo (Advertencia), Rojo (Crítico)

    [RelayCommand]
    private async Task EjecutarCapturaAsync(Guid documentoId)
    {
        IsBusy = true;
        StatusMessage = "Iniciando hardware TWAIN/WIA...";
        SlaColorBrush = "Yellow"; 

        try
        {
            // Aislamiento de carga pesada en subproceso para no bloquear la UI
            await Task.Run(async () =>
            {
                // 1. Captura asíncrona de hardware
                var rawData = await scannerService.CaptureAsync(documentoId);

                // 2. Extracción Cognitiva (Tesseract + Ollama)
                var ocrText = await ocrService.ExtractTextAsync(rawData);
                var metadata = await iaService.AnalyzeSemanticsAsync(ocrText);

                // 3. Evaluación SLA y actualización de UI vía DispatcherQueue
                _dispatcherQueue.TryEnqueue(() =>
                {
                    StatusMessage = "Procesamiento cognitivo completado. Procediendo a sellado...";
                    SlaColorBrush = metadata.IsUrgent ? "Red" : "Green"; // Actualización visual del SLA
                });

                // 4. Orquestación y Cortocircuito (Validación Hash y Archivo)
                await workflowService.EjecutarTransicionArchivadoAsync(documentoId);
            });

            StatusMessage = "Documento archivado y sellado correctamente.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error crítico: {ex.Message}";
            SlaColorBrush = "Red";
        }
        finally
        {
            IsBusy = false;
        }
    }
}