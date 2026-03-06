// ─────────────────────────────────────────────────────────────────────────────
// ScannerService.cs
// DSA.Infrastructure/Hardware/ScannerService.cs
//
// Implementación de IScannerService usando Windows Image Acquisition (WIA) COM.
// Aísla las llamadas COM en hilos STA para no colapsar WinUI 3.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WIA;
using DSA.Application.Interfaces;
using DSA.Application.DTOs;

namespace DSA.Infrastructure.Hardware
{
    public sealed class ScannerService : IScannerService
    {
        private readonly ILogger<ScannerService> _logger;
        private bool _disposed;

        public bool IsDeviceBusy { get; private set; }

        public ScannerService(ILogger<ScannerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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
                var paginas = new List<byte[]>();
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

                    // Configurar propiedades WIA
                    var opts = opciones ?? OpcionesEscaneo.Institucional;
                    SetWiaProperty(item.Properties, 6146, (int)opts.ModoColor);    // Color Intent
                    SetWiaProperty(item.Properties, 6147, opts.ResolucionDpi);      // Horizontal DPI
                    SetWiaProperty(item.Properties, 6148, opts.ResolucionDpi);      // Vertical DPI

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
                            var imageFile = (ImageFile)item.Transfer(
                                FormatID.wiaFormatPNG);

                            byte[] imageBytes = (byte[])imageFile.FileData.get_BinaryData();
                            paginas.Add(imageBytes);

                            Marshal.ReleaseComObject(imageFile);

                            _logger.LogDebug("Página {Pag} capturada ({Kb} KB)",
                                pagina, imageBytes.Length / 1024);
                        }
                        catch (COMException comEx) when (comEx.ErrorCode == unchecked((int)0x80210003))
                        {
                            // WIA_ERROR_PAPER_JAM
                            throw new AtascoPapelException(
                                $"Atasco de papel detectado en la página {pagina}.");
                        }
                        catch (COMException comEx) when (comEx.ErrorCode == unchecked((int)0x80210002))
                        {
                            // WIA_ERROR_PAPER_EMPTY — No hay más hojas en el ADF
                            hasMorePages = false;
                        }
                        catch (COMException)
                        {
                            // Otros errores WIA — detener captura
                            hasMorePages = false;
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
            staThread.Start();

            return tcs.Task;
        }

        // ─── ObtenerDispositivosAsync ─────────────────────────────────────────

        public Task<IReadOnlyList<InfoDispositivo>> ObtenerDispositivosAsync(
            CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var tcs = new TaskCompletionSource<IReadOnlyList<InfoDispositivo>>();

            Thread staThread = new(() =>
            {
                DeviceManager? dm = null;
                try
                {
                    dm = new DeviceManagerClass();
                    var lista = new List<InfoDispositivo>();

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
            staThread.Start();

            return tcs.Task;
        }

        // ─── VerificarDispositivoAsync ────────────────────────────────────────

        public Task<bool> VerificarDispositivoAsync(
            string deviceId, CancellationToken ct = default)
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
                        if (info.Type == WiaDeviceType.ScannerDeviceType &&
                            info.DeviceID == deviceId)
                        {
                            // Intentar conexión para verificar que responde
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
            staThread.Start();

            return tcs.Task;
        }

        // ─── Helpers WIA ──────────────────────────────────────────────────────

        private static void SetWiaProperty(IProperties properties, int propId, int value)
        {
            try
            {
                var prop = properties.get_Item(propId.ToString());
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
}