namespace DSA.Application.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DSA.Application.DTOs;

/// <summary>
/// Abstracción para la integración de bajo nivel con hardware TWAIN/WIA.
/// </summary>
public interface IScannerService
{
    bool IsDeviceBusy { get; }

    /// <summary>
    /// Captura una o más imágenes desde el hardware de escaneo.
    /// Retorna una lista de arreglos de bytes (una entrada por página escaneada).
    /// </summary>
    Task<List<byte[]>> CaptureAsync(
        Guid                          documentoId,
        OpcionesEscaneo?              opciones  = null,
        IProgress<ProgresoEscaneo>?   progress  = null,
        CancellationToken             ct        = default);
}