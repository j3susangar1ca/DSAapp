namespace DSA.Presentation.Services;

using System;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Extensions.Logging;

/// <summary>
/// Servicio de notificaciones nativas de Windows App SDK. C# 12 Primary Constructor.
/// </summary>
public sealed class NativeNotificationService(ILogger<NativeNotificationService> logger)
{
    public void MostrarAlertaUrgente(string folio, string asunto, Guid documentoId)
    {
        try
        {
            // Construcción nativa alineada con las Experiencias de Firma de Windows 11
            var appNotification = new AppNotificationBuilder()
                .AddArgument("action", "viewDocument")
                .AddArgument("docId", documentoId.ToString())
                .SetAudioUri(new Uri("ms-winsoundevent:Notification.Urgent")) // Alerta sonora crítica
                .AddText("🚨 TRÁMITE URGENTE (SLA)")
                .AddText($"Folio: {folio}")
                .AddText(asunto)
                .AddButton(new AppNotificationButton("Atender Inmediatamente")
                    .AddArgument("action", "openWorkPage")
                    .AddArgument("docId", documentoId.ToString()))
                .BuildNotification();

            AppNotificationManager.Default.Show(appNotification);
            logger.LogInformation("Notificación nativa urgente disparada para Folio: {Folio}", folio);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al invocar el AppNotificationManager nativo.");
        }
    }
}
