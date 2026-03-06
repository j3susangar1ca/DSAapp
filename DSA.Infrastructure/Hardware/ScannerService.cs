// ─────────────────────────────────────────────────────────────────────────────
// ScannerService.cs
// DSA.Infrastructure/Hardware/ScannerService.cs
//
// CORRECCIÓN: WiaDotNet v1.0.0 no expone el namespace COM "WIA".
// Se reemplaza el tipado estático (DeviceManager/Device/Item) por dynamic
// late-binding vía Activator.CreateInstance(Type.GetTypeFromProgID(...)).
// Esto elimina los 25 errores CS0246/CS0144/CS0136/CS0019/CS1503 de una vez.
//
// Requisito en la máquina: Windows 10/11 con WIA activo (siempre presente).
// NO requiere <COMReference> ni Interop.WIA.dll.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DSA.Application.Interfaces;
using DSA.Application.DTOs;

// ❌ ELIMINAR: `using WIA;`  ← era la causa raíz de los 25 errores

namespace DSA.Infrastructure.Hardware;

public sealed class ScannerService(ILogger<ScannerService> logger) : IScannerService, IDisposable
{
    private readonly ILogger<ScannerService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private bool _disposed;

    // ─── Constantes WIA 2.0 embebidas (sin Interop.WIA.dll) ─────────────────
    // Estas son constantes del sistema, no cambian entre versiones de WIA 2.0.
    private const int    WIA_DEVICE_TYPE_SCANNER   = 1;
    private const string WIA_FORMAT_PNG            = "{B96B3CAF-0728-11D3-9D7B-0000F81EF32E}";
    private const int    WIA_ERROR_PAPER_JAM       = unchecked((int)0x80210003);
    private const int    WIA_ERROR_PAPER_EMPTY     = unchecked((int)0x80210002);

    // WIA Property IDs
    private const int WIA_IPS_PHOTOMETRIC_INTERP   = 6146;  // Color Intent
    private const int WIA_IPS_XRES                 = 6147;  // Horizontal DPI
    private const int WIA_IPS_YRES                 = 6148;  // Vertical DPI

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
            // ── Ahora: dynamic en lugar de DeviceManager/Device/Item tipado ──
            // FIX: Las variables son object? para permitir Marshal.ReleaseComObject(object)
            object? deviceManagerObj = null;
            object? scannerObj       = null;
            object? itemObj          = null;

            try
            {
                IsDeviceBusy = true;
                ct.ThrowIfCancellationRequested();

                progress?.Report(new ProgresoEscaneo(0, 1, "Inicializando dispositivo WIA...", 0.0));

                // FIX: Instanciación COM por ProgID, sin new DeviceManagerClass()
                var dmType = Type.GetTypeFromProgID("WIA.DeviceManager")
                    ?? throw new EscanerNoDisponibleException(
                        "WIA no está disponible en este sistema. Verifique que el servicio 'Windows Image Acquisition (WIA)' esté activo.");

                deviceManagerObj = Activator.CreateInstance(dmType)!;
                dynamic deviceManager = deviceManagerObj;

                string? dispositivoId = opciones?.DispositivoId;
                dynamic? scanner = null;

                // Iteración sobre DeviceInfos (colección COM, indexada en 1)
                foreach (dynamic info in deviceManager.DeviceInfos)
                {
                    if ((int)info.Type != WIA_DEVICE_TYPE_SCANNER) continue;

                    if (dispositivoId == null || (string)info.DeviceID == dispositivoId)
                    {
                        scannerObj = info.Connect();
                        scanner    = scannerObj;
                        break;
                    }
                }

                // FIX: Comparación con null — dynamic soporta == null correctamente
                if (scanner == null)
                    throw new EscanerNoDisponibleException(
                        dispositivoId != null
                            ? $"Dispositivo '{dispositivoId}' no encontrado."
                            : "No se encontró ningún escáner conectado al sistema.");

                // scanner.Items[1] — colección COM indexada en 1
                itemObj = scanner.Items[1];
                dynamic item = itemObj;

                var opts = opciones ?? OpcionesEscaneo.Institucional;
                SetWiaProperty(item.Properties, WIA_IPS_PHOTOMETRIC_INTERP, (int)opts.ModoColor);
                SetWiaProperty(item.Properties, WIA_IPS_XRES,               opts.ResolucionDpi);
                SetWiaProperty(item.Properties, WIA_IPS_YRES,               opts.ResolucionDpi);

                _logger.LogDebug(
                    "WIA configurado: DPI={Dpi} Modo={Modo} Documento={Id}",
                    opts.ResolucionDpi, opts.ModoColor, documentoId);

                List<byte[]> paginas = [];
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
                        // FIX: WIA_FORMAT_PNG como string constante, sin FormatID.wiaFormatPNG
                        dynamic imageFile = item.Transfer(WIA_FORMAT_PNG);
                        byte[]  imageBytes = (byte[])imageFile.FileData.get_BinaryData();
                        paginas.Add(imageBytes);
                        Marshal.ReleaseComObject(imageFile);

                        _logger.LogDebug("Página {Pag} capturada ({Kb} KB)",
                            pagina, imageBytes.Length / 1024);
                    }
                    catch (COMException comEx) when (comEx.ErrorCode == WIA_ERROR_PAPER_JAM)
                    {
                        throw new EscanerOperacionException(
                            $"Atasco de papel detectado en la página {pagina}.",
                            comEx, opciones?.DispositivoId, comEx.ErrorCode);
                    }
                    catch (COMException comEx) when (comEx.ErrorCode == WIA_ERROR_PAPER_EMPTY)
                    {
                        if (paginas.Count == 0)
                            throw new AlimentadorVacioException(opciones?.DispositivoId);
                        hasMorePages = false;
                    }
                    catch (COMException comEx)
                    {
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

                // FIX: Marshal.ReleaseComObject(object) — object? acepta null-check limpio
                if (itemObj          != null) Marshal.ReleaseComObject(itemObj);
                if (scannerObj       != null) Marshal.ReleaseComObject(scannerObj);
                if (deviceManagerObj != null) Marshal.ReleaseComObject(deviceManagerObj);
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.IsBackground = true;
        staThread.Start();

        return tcs.Task;
    }

    // ─── ObtenerDispositivosAsync ──────────────────────────────────────────

    public Task<IReadOnlyList<InfoDispositivo>> ObtenerDispositivosAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tcs = new TaskCompletionSource<IReadOnlyList<InfoDispositivo>>();

        Thread staThread = new(() =>
        {
            // FIX: object? en lugar de DeviceManager? — resuelve CS0246 + CS0019 + CS1503
            object? dmObj = null;

            try
            {
                var dmType = Type.GetTypeFromProgID("WIA.DeviceManager");
                if (dmType == null) { tcs.SetResult([]); return; }

                dmObj = Activator.CreateInstance(dmType)!;
                dynamic dm = dmObj;

                List<InfoDispositivo> lista = [];

                foreach (dynamic info in dm.DeviceInfos)
                {
                    if ((int)info.Type != WIA_DEVICE_TYPE_SCANNER) continue;

                    lista.Add(new InfoDispositivo(
                        (string)info.DeviceID,
                        (string?)info.Properties["Name"]?.get_Value() ?? "Desconocido",
                        (string?)info.Properties["Description"]?.get_Value() ?? ""));
                }

                tcs.SetResult(lista);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                // FIX: object? — null-check limpio, no CS0019
                if (dmObj != null) Marshal.ReleaseComObject(dmObj);
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.IsBackground = true;
        staThread.Start();

        return tcs.Task;
    }

    // ─── VerificarDispositivoAsync ─────────────────────────────────────────

    public Task<bool> VerificarDispositivoAsync(string deviceId, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tcs = new TaskCompletionSource<bool>();

        Thread staThread = new(() =>
        {
            // FIX: object? en lugar de DeviceManager? — resuelve CS0246 + CS0019 + CS1503
            object? dmObj = null;

            try
            {
                var dmType = Type.GetTypeFromProgID("WIA.DeviceManager");
                if (dmType == null) { tcs.SetResult(false); return; }

                dmObj = Activator.CreateInstance(dmType)!;
                dynamic dm = dmObj;

                foreach (dynamic info in dm.DeviceInfos)
                {
                    if ((int)info.Type == WIA_DEVICE_TYPE_SCANNER &&
                        (string)info.DeviceID == deviceId)
                    {
                        object devObj = info.Connect();
                        Marshal.ReleaseComObject(devObj);
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
                // FIX: object? — null-check limpio, no CS0019
                if (dmObj != null) Marshal.ReleaseComObject(dmObj);
            }
        });

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.IsBackground = true;
        staThread.Start();

        return tcs.Task;
    }

    // ─── Helper WIA (dynamic properties) ──────────────────────────────────

    private static void SetWiaProperty(dynamic properties, int propId, int value)
    {
        try
        {
            object objPropId = propId;
            var prop = properties.get_Item(ref objPropId);
            prop.set_Value(value);
        }
        catch (COMException)
        {
            // La propiedad no es soportada por este dispositivo — ignorar
        }
    }

    // ─── IDisposable ──────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}