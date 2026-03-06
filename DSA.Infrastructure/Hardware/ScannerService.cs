// INFRAESTRUCTURA: DSA.Infrastructure/Hardware/ScannerService.cs
namespace DSA.Infrastructure.Hardware;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DSA.Application.DTOs;
using DSA.Application.Interfaces;

/// <summary>
/// Wrapper de producción para hardware de captura nativo (WIA).
/// Requiere agregar la referencia COM: "Microsoft Windows Image Acquisition Library v2.0" en el .csproj.
/// </summary>
public sealed class ScannerService : IScannerService
{
    // Bandera volátil para evaluación de cortocircuito (Karnaugh/Hardware)
    public bool IsDeviceBusy { get; private set; }

    public Task<List<byte[]>> CaptureAsync(
        Guid                          documentoId,
        OpcionesEscaneo?              opciones  = null,
        IProgress<ProgresoEscaneo>?   progress  = null,
        CancellationToken             ct        = default)
    {
        if (IsDeviceBusy)
        {
            throw new InvalidOperationException("Hardware TWAIN/WIA ocupado: El periférico de captura se encuentra en uso por otra transacción.");
        }

        IsDeviceBusy = true;
        var tcs = new TaskCompletionSource<List<byte[]>>();

        // Aislamiento estricto de llamadas COM en un hilo STA para evitar colapsar WinUI 3
        Thread staThread = new(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                progress?.Report(new ProgresoEscaneo("Inicializando dispositivo WIA...", 0.0));

                // Inicialización del diálogo o conexión silenciosa WIA
                dynamic wiaDialog = Activator.CreateInstance(Type.GetTypeFromProgID("WIA.CommonDialog") 
                                    ?? throw new NotSupportedException("WIA no está soportado en este sistema operativo."))!;
                
                progress?.Report(new ProgresoEscaneo("Capturando imagen...", 0.2));

                // Parámetros mágicos de WIA: ColorIntent, MaximizeQuality, Formato PNG
                dynamic image = wiaDialog.ShowAcquireImage(
                    1, // ScannerDeviceType
                    1, // ColorIntent
                    1, // MaximizeQuality
                    "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}", // ID de formato (PNG genérico)
                    false, // AlwaysSelectDevice
                    false, // UseCommonUI
                    false  // CancelError
                );

                if (image != null)
                {
                    dynamic vector = image.FileData;
                    byte[] imageBytes = (byte[])vector.get_BinaryData();

                    progress?.Report(new ProgresoEscaneo("Imagen capturada correctamente.", 1.0));

                    tcs.SetResult(new List<byte[]> { imageBytes });
                }
                else
                {
                    tcs.SetException(new OperationCanceledException("La captura fue abortada por el periférico o el usuario."));
                }
            }
            catch (COMException comEx)
            {
                tcs.SetException(new InvalidOperationException($"Error de bajo nivel en el bus WIA/TWAIN: {comEx.Message}", comEx));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                IsDeviceBusy = false;
            }
        });

        // Configuración innegociable para interoperabilidad COM de WIA
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.IsBackground = true;
        staThread.Start();

        return tcs.Task;
    }
}