namespace DSA.Presentation.ViewModels;

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using DSA.Domain.Interfaces;
using DSA.Application.Interfaces;

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
    private string _slaColorBrush = "Green"; 

    [RelayCommand]
    private async Task EjecutarCapturaAsync(Guid documentoId)
    {
        IsBusy = true;
        StatusMessage = "Iniciando hardware TWAIN/WIA...";
        SlaColorBrush = "Yellow"; 

        try
        {
            await Task.Run(async () =>
            {
                var rawData = await scannerService.CaptureAsync(documentoId);

                var ocrText = await ocrService.ExtractTextAsync(rawData);
                var metadata = await iaService.AnalyzeTextAsync(ocrText);

                _dispatcherQueue.TryEnqueue(() =>
                {
                    StatusMessage = "Procesamiento cognitivo completado. Procediendo a sellado...";
                    // Simulación de lógica de urgencia basada en el texto analizado
                    SlaColorBrush = ocrText.Contains("URGENTE") ? "Red" : "Green"; 
                });

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
