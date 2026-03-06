// 1. INFRAESTRUCTURA: DSA.Infrastructure/Hardware/ScannerService.cs
namespace DSA.Infrastructure.Hardware;

using System;
using System.Threading.Tasks;
using DSA.Application.Interfaces; //

/// <summary>
/// Wrapper para Windows Image Acquisition (WIA) o TWAIN.
/// </summary>
public sealed class ScannerService : IScannerService
{
    // Bandera volátil para evaluación de cortocircuito
    public bool IsDeviceBusy { get; private set; }

    public async Task<byte[]> CaptureAsync(Guid documentoId)
    {
        if (IsDeviceBusy)
        {
            throw new InvalidOperationException("Hardware TWAIN/WIA ocupado por otro subproceso.");
        }

        IsDeviceBusy = true;

        try
        {
            // Aislamiento estricto de llamadas COM síncronas para evitar bloqueos en WinUI 3
            return await Task.Run(() =>
            {
                // TODO: Implementación nativa Interop COM (Ej. WIA.DeviceManager)
                // Se simula la latencia mecánica del escáner
                Task.Delay(2500).Wait(); 
                
                // Retorno de bytes empaquetados (Ej. Magic Bytes PDF %PDF-)
                return [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34]; 
            });
        }
        finally
        {
            IsDeviceBusy = false;
        }
    }
}

// 2. INFRAESTRUCTURA: DSA.Infrastructure/Storage/UncStorageWatcherService.cs
namespace DSA.Infrastructure.Storage;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Servicio Singleton de vigilancia de directorios UNC compartidos.
/// C# 12 Primary Constructor.
/// </summary>
public sealed class UncStorageWatcherService(ILogger<UncStorageWatcherService> logger) : IHostedService, IDisposable
{
    private readonly FileSystemWatcher _watcher = new()
    {
        Path = @"\\SERVER_SIA\Acervo_SIA", // Ruta UNC estandarizada
        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
        Filter = "*.pdf",
        IncludeSubdirectories = true
    };

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _watcher.Created += OnDocumentoRecibido;
        _watcher.EnableRaisingEvents = true;
        
        logger.LogInformation("Watcher UNC iniciado en {Path}", _watcher.Path);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnDocumentoRecibido;
        
        logger.LogInformation("Watcher UNC detenido.");
        return Task.CompletedTask;
    }

    private void OnDocumentoRecibido(object sender, FileSystemEventArgs e)
    {
        // El evento se dispara en un Thread de I/O (ThreadPool)
        logger.LogInformation("Nuevo archivo detectado vía red: {FileName}", e.Name);
        
        // TODO: Encolar en canal (Channel<T>) para procesado asíncrono (Hash SHA-256 + OCR) sin saturar el FileSystemWatcher.
    }

    public void Dispose() => _watcher.Dispose();
}