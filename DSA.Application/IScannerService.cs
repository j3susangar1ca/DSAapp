namespace DSA.Application.Interfaces;

/// <summary>
/// Abstracción para la integración de bajo nivel con hardware TWAIN/WIA.
/// </summary>
public interface IScannerService
{
    // Asincronía obligatoria para no bloquear el UI Thread (WinUI 3)
    Task<byte[]> CaptureAsync(Guid documentoId);
}