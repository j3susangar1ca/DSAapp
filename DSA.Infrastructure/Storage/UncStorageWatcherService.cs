namespace DSA.Infrastructure.Storage;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class UncStorageWatcherService(ILogger<UncStorageWatcherService> logger) : IHostedService, IDisposable
{
    private readonly FileSystemWatcher _watcher = new()
    {
        Path = @"\\SERVER_SIA\Acervo_SIA",
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
        logger.LogInformation("Nuevo archivo detectado vía red: {FileName}", e.Name);
    }

    public void Dispose() => _watcher.Dispose();
}
