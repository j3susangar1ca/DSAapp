namespace DSA.Application.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DSA.Application.DTOs;

public interface IScannerService : IDisposable
{
    /// <summary>Indica si el dispositivo está actualmente en uso.</summary>
    bool IsDeviceBusy { get; }

    /// <summary>Captura múltiples páginas desde el ADF o Cama Plana.</summary>
    Task<IReadOnlyList<byte[]>> CaptureAsync(
        Guid documentoId,
        OpcionesEscaneo? opciones = null,
        IProgress<ProgresoEscaneo>? progress = null,
        CancellationToken ct = default);

    /// <summary>Devuelve la lista de escáneres disponibles en el sistema.</summary>
    Task<IReadOnlyList<InfoDispositivo>> ObtenerDispositivosAsync(CancellationToken ct = default);

    /// <summary>Verifica si un dispositivo específico está conectado y listo.</summary>
    Task<bool> VerificarDispositivoAsync(string deviceId, CancellationToken ct = default);
}