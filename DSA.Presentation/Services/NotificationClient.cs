// 1. PRESENTACIÓN: DSA.Presentation/Services/NotificationClient.cs
namespace DSA.Presentation.Services;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Toolkit.Uwp.Notifications; // NuGet: Microsoft.Toolkit.Uwp.Notifications
using Microsoft.UI.Dispatching;
using Microsoft.Extensions.Logging;

/// <summary>
/// Cliente SignalR para sincronización de mesas de trabajo en tiempo real.
/// </summary>
public sealed class NotificationClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<NotificationClient> _logger;
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public NotificationClient(ILogger<NotificationClient> logger)
    {
        _logger = logger;

        // Configuración del Hub (Endpoint definido en infraestructura de red)
        _connection = new HubConnectionBuilder()
            .WithUrl("https://sia-server.local/hubs/documentos")
            .WithAutomaticReconnect()
            .Build();

        // Registro de manejadores de eventos distribuidos
        _connection.On<string, Guid>("NuevoOficioRecibido", (folio, id) =>
        {
            MostrarNotificacionNativa(folio, id);
        });
    }

    public async Task IniciarAsync()
    {
        try
        {
            await _connection.StartAsync();
            _logger.LogInformation("Conexión SignalR establecida con éxito.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al conectar con el Hub de Notificaciones.");
        }
    }

    /// <summary>
    /// Lanza una Toast Notification nativa de Windows 10/11.
    /// </summary>
    private void MostrarNotificacionNativa(string folio, Guid documentoId)
    {
        // Uso de Microsoft.Toolkit.Uwp.Notifications para una experiencia fluida
        new ToastContentBuilder()
            .AddArgument("action", "viewDocument")
            .AddArgument("docId", documentoId.ToString())
            .AddText("📦 Nuevo Oficio Detectado")
            .AddText($"Folio: {folio}")
            .AddAttributionText("SIA - Sistema Institucional de Archivos")
            .SetToastDuration(ToastDuration.Long)
            .Show();
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}