using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DSAapp.Core.Contracts.Services;
using DSAapp.Core.Models;
using Microsoft.UI.Dispatching;

namespace DSAapp.ViewModels;

public partial class EscanerViewModel : ObservableObject
{
    private readonly IScannerService _scannerService;
    private readonly IOcrService _ocrService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SemaphoreSlim _ocrSemaphore = new(3); // Máximo 3 OCR simultáneos
    private CancellationTokenSource? _scanCts;

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private int _totalPagesScanned;
    [ObservableProperty] private ScannedPageViewModel? _selectedPage;

    public ObservableCollection<ScannedPageViewModel> Pages { get; } = new();

    public EscanerViewModel(IScannerService scannerService, IOcrService ocrService)
    {
        _scannerService = scannerService;
        _ocrService = ocrService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (IsScanning) return;

        _scanCts = new CancellationTokenSource();
        IsScanning = true;
        Pages.Clear();
        TotalPagesScanned = 0;

        try
        {
            var config = new ScannerConfig { ResolutionDpi = 300, IsColorEnabled = true };
            var stream = await _scannerService.ScanBatchAsync(config, _scanCts.Token);

            await foreach (var page in stream.WithCancellation(_scanCts.Token))
            {
                var vm = new ScannedPageViewModel(page);
                _dispatcherQueue.TryEnqueue(() => {
                    Pages.Add(vm);
                    TotalPagesScanned++;
                });

                // Disparar OCR en segundo plano
                _ = Task.Run(() => ProcessOcrAsync(vm, _scanCts.Token));
            }
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task ProcessOcrAsync(ScannedPageViewModel pageVm, CancellationToken ct)
    {
        await _ocrSemaphore.WaitAsync(ct);
        try
        {
            if (!pageVm.IsBlank)
            {
                var text = await _ocrService.ExtractTextFromImageAsync(pageVm.ScannedPage.ImageStream, ct);
                _dispatcherQueue.TryEnqueue(() => {
                    pageVm.ExtractedText = text;
                    pageVm.OcrProcessed = true;
                });
            }
        }
        finally { _ocrSemaphore.Release(); }
    }

    [RelayCommand]
    private void DeletePage(ScannedPageViewModel page)
    {
        Pages.Remove(page);
        page.Dispose();
        TotalPagesScanned = Pages.Count;
    }
}