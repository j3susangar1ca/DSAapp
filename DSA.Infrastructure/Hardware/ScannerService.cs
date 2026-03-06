// ─────────────────────────────────────────────────────────────────────────────
// ScannerService.cs
// DSA.Infrastructure/Hardware/ScannerService.cs
//
// Implementación de IScannerService usando Windows Image Acquisition (WIA) COM.
// Aísla las llamadas COM en hilos STA para no colapsar WinUI 3.
//
// REQUISITO: Las llamadas WIA deben ejecutarse en un hilo STA (Single-Threaded
// Apartment). Task.Run usa hilos MTA del ThreadPool — uso directo causaría
// InvalidCastException en objetos COM. El orquestador de hilos STA está aquí.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WIA;
using DSA.Application.Interfaces;
using DSA.Application.DTOs;

namespace DSA.Infrastructure.Hardware;

[SupportedOSPlatform("windows")]
public sealed class ScannerService(ILogger<ScannerService> logger) : IScannerService, IDisposable
{
    private readonly ILogger<ScannerService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private bool _disposed;

    public bool IsDeviceBusy { get; private set; }

    // ─── CaptureAsync ─────────────────────────────────────────────────────

    public Task<IReadOnlyList<byte[]>> CaptureAsync(
        Guid                         documentoId,
        OpcionesEscaneo?             opciones = null,
        IProgress<ProgresoEscaneo>?  progress = null,
        CancellationToken            ct       = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsDeviceBusy)
            throw new InvalidOperationException(
                "Hardware TWAIN/WIA ocupado: El periférico de captura se encuentra en uso por otra transacción.");

        var tcs = new TaskCompletionSource<IReadOnlyList<byte[]>>();

        Thread staThread = new(() =>
        {
            List<byte[]> paginas = [];
            DeviceManager? deviceManager = null;
            Device? scanner = null;
            Item? item = null;

            try
            {
                IsDeviceBusy = true;
                ct.ThrowIfCancellationRequested();

                progress?.Report(new ProgresoEscaneo(0, 1, "Inicializando dispositivo WIA...", 0.0));

                deviceManager = new DeviceManagerClass();

                // Buscar el dispositivo solicitado o el primero disponible
                string? dispositivoId = opciones?.DispositivoId;
                foreach (DeviceInfo info in deviceManager.DeviceInfos)
                {
                    if (info.Type != WiaDeviceType.ScannerDeviceType) continue;

                    if (dispositivoId == null || info.DeviceID == dispositivoId)
                    {
                        scanner = info.Connect();
                        break;
                    }
                }

                if (scanner == null)
                    throw new EscanerNoDisponibleException(
                        dispositivoId != null
                            ? $"Dispositivo '{dispositivoId}' no encontrado."
                            : "No se encontró ningún escáner conectado al sistema.");

                item = scanner.Items[1];

                // Configurar propiedades WIA mediante interfaz IProperties
                var opts = opciones ?? OpcionesEscaneo.Institucional;
                SetWiaProperty((IProperties)item.Properties, 6146, (int)opts.ModoColor);    // Color Intent
                SetWiaProperty((IProperties)item.Properties, 6147, opts.ResolucionDpi);      // Horizontal DPI
                SetWiaProperty((IProperties)item.Properties, 6148, opts.ResolucionDpi);      // Vertical DPI

                _logger.LogDebug(
                    "WIA configurado: DPI={Dpi} Modo={Modo} Documento={Id}",
                    opts.ResolucionDpi, opts.ModoColor, documentoId);

                // Captura — intenta ADF (alimentación múltiple), cae a cama plana si no hay ADF
                int pagina = 0;
                bool hasMorePages = true;

                while (hasMorePages)
                {
                    ct.ThrowIfCancellationRequested();
                    pagina++;

                    progress?.Report(new ProgresoEscaneo(
                        pagina, pagina, $"Escaneando página {pagina}...",
                        Math.Min(pagina * 0.1, 0.9)));

                    try
                    {
                        var imageFile = (ImageFile)item.Transfer(FormatID.wiaFormatPNG);
                        // get_BinaryData() devuelve object — extraemos de forma segura
                        var rawData = imageFile.FileData.get_BinaryData();
                        byte[] imageBytes = rawData as byte[]
                            ?? throw new EscanerOperacionException(
                                $"WIA devolvió datos binarios en formato inesperado ({rawData?.GetType().Name ?? "null"}).",
                                null, opciones?.DispositivoId, 0);
                        paginas.Add(imageBytes);

                        Marshal.ReleaseComObject(imageFile);

                        _logger.LogDebug("Página {Pag} capturada ({Kb} KB)",
                            pagina, imageBytes.Length / 1024);
                    }
                    catch (COMException comEx) when (comEx.ErrorCode == unchecked((int)0x80210003))
                    {
                        // WIA_ERROR_PAPER_JAM
                        throw new EscanerOperacionException(
                            $"Atasco de papel detectado en la página {pagina}.",
                            comEx, opciones?.DispositivoId, comEx.ErrorCode);
                    }
                    catch (COMException comEx) when (comEx.ErrorCode == unchecked((int)0x80210002))
                    {
                        // WIA_ERROR_PAPER_EMPTY — No hay más hojas en el ADF
                        if (paginas.Count == 0)
                            throw new AlimentadorVacioException(opciones?.DispositivoId);
                        hasMorePages = false;
                    }
                    catch (COMException comEx)
                    {
                        // Otros errores WIA
                        throw new EscanerOperacionException(
                            $"Error WIA en página {pagina}: {comEx.Message}",
                            comEx, opciones?.DispositivoId, comEx.ErrorCode);
                    }
                }

                progress?.Report(new ProgresoEscaneo(
                    pagina, pagina,
                    $"{paginas.Count} página(s) capturada(s) correctamente.", 1.0));

                tcs.SetResult(paginas);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                IsDeviceBusy = false;

                if (item != null) Marshal.ReleaseComObject(item);
                if (scanner != null) Marshal.ReleaseComObject(scanner);
                if (deviceManager != null) Marshal.ReleaseComObject(deviceManager);
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.IsBackground = true;
        staThread.Name = $"WIA-Capture-STA-{documentoId:N}";
        staThread.Start();

        return tcs.Task;
    }

    // ─── ObtenerDispositivosAsync ─────────────────────────────────────────

    public Task<IReadOnlyList<InfoDispositivo>> ObtenerDispositivosAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tcs = new TaskCompletionSource<IReadOnlyList<InfoDispositivo>>();

        Thread staThread = new(() =>
        {
            DeviceManager? dm = null;
            try
            {
                dm = new DeviceManagerClass();
                List<InfoDispositivo> lista = [];

                foreach (DeviceInfo info in dm.DeviceInfos)
                {
                    if (info.Type != WiaDeviceType.ScannerDeviceType) continue;

                    lista.Add(new InfoDispositivo(
                        info.DeviceID,
                        info.Properties["Name"]?.get_Value()?.ToString() ?? "Desconocido",
                        info.Properties["Description"]?.get_Value()?.ToString() ?? ""));
                }

                tcs.SetResult(lista);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                if (dm != null) Marshal.ReleaseComObject(dm);
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.IsBackground = true;
        staThread.Name = "WIA-Enum-STA";
        staThread.Start();

        return tcs.Task;
    }

    // ─── VerificarDispositivoAsync ────────────────────────────────────────

    public Task<bool> VerificarDispositivoAsync(string deviceId, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tcs = new TaskCompletionSource<bool>();

        Thread staThread = new(() =>
        {
            DeviceManager? dm = null;
            try
            {
                dm = new DeviceManagerClass();

                foreach (DeviceInfo info in dm.DeviceInfos)
                {
                    if (info.Type == WiaDeviceType.ScannerDeviceType && info.DeviceID == deviceId)
                    {
                        var dev = info.Connect();
                        Marshal.ReleaseComObject(dev);
                        tcs.SetResult(true);
                        return;
                    }
                }

                tcs.SetResult(false);
            }
            catch (Exception)
            {
                tcs.TrySetResult(false);
            }
            finally
            {
                if (dm != null) Marshal.ReleaseComObject(dm);
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.IsBackground = true;
        staThread.Name = $"WIA-Verify-STA-{deviceId}";
        staThread.Start();

        return tcs.Task;
    }

    // ─── Helpers WIA ──────────────────────────────────────────────────────

    /// <summary>
    /// Establece una propiedad WIA en el Item del escáner de forma segura.
    /// IMPORTANTE: Debe llamarse desde un hilo STA; de lo contrario COM lanzará
    /// InvalidCastException. No usa <c>dynamic</c> para evitar dispatch tardío en MTA.
    /// </summary>
    private static void SetWiaProperty(IProperties properties, int propId, int value)
    {
        try
        {
            // El indexador de WIA.IProperties acepta object (VARIANT)
            object key = propId;
            IProperty prop = properties.get_Item(ref key);
            prop.set_Value(value);
        }
        catch (COMException)
        {
            // Propiedad no soportada por el hardware — se omite silenciosamente
        }
        catch (InvalidCastException)
        {
            // Garantía extra: si el COM RCW falla en MTA (no debería en STA) — ignorar
        }
    }

    // ─── IDisposable ──────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}