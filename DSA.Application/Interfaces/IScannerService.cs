using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DSA.Application.DTOs;

namespace DSA.Application.Interfaces
{
    public interface IScannerService : IDisposable
    {
        bool IsDeviceBusy { get; }

        Task<IReadOnlyList<byte[]>> CaptureAsync(
            Guid                        documentoId,
            OpcionesEscaneo?            opciones  = null,
            IProgress<ProgresoEscaneo>? progress  = null,
            CancellationToken           ct        = default);

        Task<IReadOnlyList<InfoDispositivo>> ObtenerDispositivosAsync(
            CancellationToken ct = default);

        Task<bool> VerificarDispositivoAsync(
            string deviceId,
            CancellationToken ct = default);
    }
}