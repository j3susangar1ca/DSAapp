namespace DSA.Application.Interfaces;

using System;
using System.Threading.Tasks;

/// <summary>
/// Abstracción para la integración de bajo nivel con hardware TWAIN/WIA.
/// </summary>
public interface IScannerService
{
    bool IsDeviceBusy { get; }

    // Asincronía obligatoria para no bloquear el UI Thread (WinUI 3)
    Task<byte[]> CaptureAsync(Guid documentoId);
}