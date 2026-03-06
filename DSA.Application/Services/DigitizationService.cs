namespace DSA.Application.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Pdfa;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using global::DSA.Domain.Entities;
using global::DSA.Domain.Interfaces;
using global::DSA.Application.Interfaces;
using global::DSA.Application.DTOs;
using global::DSA.Application.Exceptions;

// ─── Servicio Principal ───────────────────────────────────────────────────────

/// <summary>
/// Orquestador del pipeline completo de digitalización institucional segura.
/// Coordina la captura desde hardware, procesamiento de imágenes, generación
/// de PDF/A-1b, sellado criptográfico y actualización del estado lógico.
/// </summary>
public sealed class DigitizationService
{
    private readonly IScannerService          _scannerService;
    private readonly IDocumentoRepository     _documentoRepository;
    private readonly IStorageService          _almacenamiento;
    private readonly ILogger<DigitizationService> _logger;

    public DigitizationService(
        IScannerService          scannerService,
        IDocumentoRepository     documentoRepository,
        IStorageService          almacenamiento,
        ILogger<DigitizationService> logger)
    {
        _scannerService      = scannerService      ?? throw new ArgumentNullException(nameof(scannerService));
        _documentoRepository = documentoRepository ?? throw new ArgumentNullException(nameof(documentoRepository));
        _almacenamiento      = almacenamiento      ?? throw new ArgumentNullException(nameof(almacenamiento));
        _logger              = logger              ?? throw new ArgumentNullException(nameof(logger));
    }

    // ─── Pipeline Principal ───────────────────────────────────────────────────

    public async Task<ResultadoDigitalizacion> DigitalizarAsync(
        Guid                                  documentoId,
        OpcionesEscaneo?                      opcionesEscaneo = null,
        string                                operador        = "Sistema",
        IProgress<ProgresoDigitalizacion>?    uiProgress      = null,
        CancellationToken                     ct              = default)
    {
        var cronometro = System.Diagnostics.Stopwatch.StartNew();

        // ── Verificar precondiciones del documento ────────────────────────────
        var documento = await _documentoRepository.GetByIdAsync(documentoId, ct)
            ?? throw new InvalidOperationException(
                $"Documento {documentoId} no encontrado en el repositorio.");

        if (documento.IsSellado)
            throw new InvalidOperationException(
                $"El documento '{documento.Nombre}' ya fue sellado. " +
                "La operación de digitalización es idempotente: no puede repetirse.");

        _logger.LogInformation(
            "Iniciando pipeline de digitalización. Documento={Doc} Operador={Op}",
            documento.Nombre, operador);

        byte[]? pdfBytes = null;
        string? hashSha256 = null;
        bool archivoGuardado = false;
        string rutaDestino = string.Empty;

        try
        {
            // ══════════════════════════════════════════════════════════════════
            // FASE 1 — Captura desde Hardware (ScannerService)
            // ══════════════════════════════════════════════════════════════════
            Reportar(uiProgress, "Iniciando escáner...", 5, "CAPTURA");

            var imagenesRaw = await _scannerService.CaptureAsync(
                documentoId,
                opcionesEscaneo,
                new Progress<ProgresoEscaneo>(p => Reportar(uiProgress,
                    p.Mensaje,
                    5 + (int)Math.Min(p.PorcentajeGlobal * 0.3, 30),
                    "CAPTURA")),
                ct);

            if (imagenesRaw.Count == 0)
                throw new InvalidOperationException("El escáner no devolvió ninguna imagen.");

            _logger.LogInformation("{Count} imágenes capturadas desde hardware.", imagenesRaw.Count);

            // ══════════════════════════════════════════════════════════════════
            // FASE 2 — Procesamiento de Imágenes (SkiaSharp)
            // ══════════════════════════════════════════════════════════════════
            Reportar(uiProgress, "Procesando imágenes...", 38, "PROCESAMIENTO");
            ct.ThrowIfCancellationRequested();

            var opcionesActivas = opcionesEscaneo ?? OpcionesEscaneo.Institucional;
            var (imagenesProcesadas, descartadas) = await ProcesarImagenesAsync(
                imagenesRaw, opcionesActivas, ct);

            if (imagenesProcesadas.Count == 0)
                throw new InvalidOperationException(
                    $"Todas las páginas ({imagenesRaw.Count}) fueron detectadas como en blanco.");

            // ══════════════════════════════════════════════════════════════════
            // FASE 3 — Generación de PDF/A-1b (iText7)
            // ══════════════════════════════════════════════════════════════════
            Reportar(uiProgress, "Generando PDF/A-1b conforme ISO 19005-1...", 55, "PDF_A");
            ct.ThrowIfCancellationRequested();

            pdfBytes = await Task.Run(() =>
                GenerarPdfA(imagenesProcesadas, documento.Nombre), ct);

            // ══════════════════════════════════════════════════════════════════
            // FASE 4 — Sello Criptográfico SHA-256
            // ══════════════════════════════════════════════════════════════════
            Reportar(uiProgress, "Calculando sello criptográfico SHA-256...", 70, "HASH");

            hashSha256 = CalcularHashSha256(pdfBytes);
            
            // ══════════════════════════════════════════════════════════════════
            // FASE 5 — Persistencia Física (IStorageService - Taxonomía CADIDO)
            // ══════════════════════════════════════════════════════════════════
            Reportar(uiProgress, "Almacenando PDF/A en repositorio institucional...", 80, "PERSISTENCIA");

            string anio = DateTime.Now.Year.ToString();
            rutaDestino = await _almacenamiento.SaveDocumentAsync(
                documento.Id, 
                pdfBytes, 
                anio, 
                "Gral", 
                "Entrada", 
                ct);
            
            archivoGuardado = true;

            // ══════════════════════════════════════════════════════════════════
            // FASE 6 — Activación del Bit SEAL en la Entidad (CRÍTICO)
            // ══════════════════════════════════════════════════════════════════
            Reportar(uiProgress, "Aplicando sello digital a la entidad...", 90, "SEAL");

            documento.SetPath(rutaDestino);
            documento.SetHash(hashSha256);  // ← Activa IsSellado (D[4])

            // Verificación de invariante de dominio
            if (!documento.IsSellado)
                throw new InvalidOperationException(
                    "INVARIANTE VIOLADA: SetHash fue invocado pero el bit SEAL no está activo.");

            // Persistir el estado actualizado en la base de datos
            await _documentoRepository.SaveChangesAsync(ct);

            cronometro.Stop();

            Reportar(uiProgress,
                $"✓ Digitalización completada. Sello: {hashSha256[..8]}...{hashSha256[^8..]}",
                100, "COMPLETADO");

            return new ResultadoDigitalizacion(
                Exitoso:             true,
                HashSha256:          hashSha256,
                RutaAlmacenada:      rutaDestino,
                PaginasProcesadas:   imagenesProcesadas.Count,
                PaginasDescartadas:  descartadas,
                EstadoVectorFinal:   documento.EstadoVector,
                DuracionTotal:       cronometro.Elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Pipeline cancelado por el usuario. Documento={Doc}", documentoId);
            if (archivoGuardado && !string.IsNullOrEmpty(rutaDestino))
                await RollbackArchivoAsync(rutaDestino);
            throw;
        }
        catch (Exception ex) when (ex is not DigitalizacionException)
        {
            cronometro.Stop();
            _logger.LogError(ex, "Fallo en pipeline de digitalización. Documento={Doc}", documentoId);

            if (archivoGuardado && !string.IsNullOrEmpty(rutaDestino))
                await RollbackArchivoAsync(rutaDestino);

            Reportar(uiProgress, $"✗ Error: {ex.Message}", 0, "ERROR");

            throw new DigitalizacionException(
                DeterminarFase(hashSha256, archivoGuardado),
                documentoId,
                $"Fallo en el pipeline para '{documento?.Nombre}'. Detalle: {ex.Message}",
                ex);
        }
        finally
        {
            if (pdfBytes != null)
            {
                CryptographicOperations.ZeroMemory(pdfBytes);
                pdfBytes = null;
            }
        }
    }

    // ─── Fase 2: Procesamiento de Imágenes con SkiaSharp ─────────────────────
    private static async Task<(List<byte[]> Procesadas, int Descartadas)> ProcesarImagenesAsync(
        IReadOnlyList<byte[]> imagenesRaw, OpcionesEscaneo opciones, CancellationToken ct)
    {
        var tareas = imagenesRaw.Select((imgBytes, idx) =>
            Task.Run(() => ProcesarPagina(imgBytes, idx + 1, opciones), ct));

        var resultados = await Task.WhenAll(tareas);
        var procesadas  = resultados.Where(r => r != null).Cast<byte[]>().ToList();
        int descartadas = resultados.Count(r => r == null);

        return (procesadas, descartadas);
    }

    private static byte[]? ProcesarPagina(byte[] imgBytes, int numeroPagina, OpcionesEscaneo opciones)
    {
        using var stream    = new SKMemoryStream(imgBytes);
        using var codec     = SKCodec.Create(stream);
        if (codec == null) return null;

        var info   = codec.Info;
        using var bitmap = new SKBitmap(info);
        if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success) return null;

        if (EsPaginaEnBlanco(bitmap, opciones.UmbralPaginaBlanco)) return null; 

        using var normalizada = NormalizarImagen(bitmap);
        using var outputStream = new SKDynamicMemoryWStream();
        normalizada.Encode(outputStream, SKEncodedImageFormat.Jpeg, quality: 92);

        return outputStream.DetachAsData().ToArray();
    }

    private static bool EsPaginaEnBlanco(SKBitmap bitmap, double umbralBrillo)
    {
        if (bitmap.Width < 50 || bitmap.Height < 50) return false;
        const int MUESTRAS = 1000;
        var rng = new Random();
        double suma = 0.0, sumaCuadrados = 0.0;

        for (int i = 0; i < MUESTRAS; i++)
        {
            int x = rng.Next(bitmap.Width);
            int y = rng.Next(bitmap.Height);
            var pixel = bitmap.GetPixel(x, y);

            double luminancia = (0.299 * pixel.Red + 0.587 * pixel.Green + 0.114 * pixel.Blue) / 255.0;
            suma           += luminancia;
            sumaCuadrados  += luminancia * luminancia;
        }

        double media    = suma / MUESTRAS;
        double varianza = (sumaCuadrados / MUESTRAS) - (media * media);

        return (media > umbralBrillo) && (varianza < 0.002);
    }

    private static SKBitmap NormalizarImagen(SKBitmap original)
    {
        var colorType = original.ColorType == SKColorType.Gray8 ? SKColorType.Gray8 : SKColorType.Rgba8888;
        var bitmap = new SKBitmap(original.Width, original.Height, colorType, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var paint  = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
        canvas.DrawBitmap(original, SKRect.Create(bitmap.Width, bitmap.Height), paint);
        return bitmap;
    }

    // ─── Fase 3: Generación PDF/A-1b con iText7 ──────────────────────────────
    private byte[] GenerarPdfA(List<byte[]> imagenesJpeg, string nombre)
    {
        using var outputStream = new MemoryStream(capacity: imagenesJpeg.Count * 500_000);
        using var iccStream = ObtenerPerfilIcc();

        var writerProperties = new WriterProperties()
            .SetFullCompression()
            .SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);

        using var pdfWriter = new PdfWriter(outputStream, writerProperties);
        using var pdfDoc    = new PdfADocument(pdfWriter, PdfAConformanceLevel.PDF_A_1B,
            new PdfOutputIntent("Custom", "", "http://www.color.org", "sRGB IEC61966-2.1", iccStream));

        var xmpMeta = pdfDoc.GetXmpMetadata(createNew: true);
        xmpMeta.SetProperty(iText.Kernel.XMP.XMPConst.NS_DC, "title", nombre);
        xmpMeta.SetProperty(iText.Kernel.XMP.XMPConst.NS_DC, "creator", "DSA");
        pdfDoc.SetXmpMetadata(xmpMeta);

        pdfDoc.GetCatalog().SetLang(new PdfString("es-ES"));
        pdfDoc.GetDocumentInfo().SetTitle(nombre).SetCreator("DSA");

        foreach (var jpegBytes in imagenesJpeg)
        {
            var imageData = ImageDataFactory.Create(jpegBytes);
            float anchoPoints  = imageData.GetWidth()  * 72f / (imageData.GetDpiX() > 0 ? imageData.GetDpiX() : 300f);
            float altoPoints   = imageData.GetHeight() * 72f / (imageData.GetDpiY() > 0 ? imageData.GetDpiY() : 300f);

            var pagina = pdfDoc.AddNewPage(new PageSize(anchoPoints, altoPoints));
            var canvas = new PdfCanvas(pagina);
            canvas.AddImageFittedIntoRectangle(imageData, new Rectangle(0, 0, anchoPoints, altoPoints), asInline: false);
        }

        pdfDoc.Close();
        return outputStream.ToArray();
    }

    private static Stream ObtenerPerfilIcc()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("sRGB_v4_ICC_preference.icc", StringComparison.OrdinalIgnoreCase));

        if (resourceName != null) return assembly.GetManifestResourceStream(resourceName)!;
        
        var iccPath = Path.Combine(AppContext.BaseDirectory, "Resources", "sRGB_v4_ICC_preference.icc");
        if (File.Exists(iccPath)) return File.OpenRead(iccPath);

        return new MemoryStream(Array.Empty<byte>());
    }

    // ─── Fase 4: Hash SHA-256 ─────────────────────────────────────────────────
    private static string CalcularHashSha256(byte[] data)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(data, hash);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────
    private async Task RollbackArchivoAsync(string rutaDestino)
    {
        try
        {
            if (await _almacenamiento.FileExistsAsync(rutaDestino))
            {
                File.Delete(rutaDestino);
                _logger.LogWarning("Rollback: archivo eliminado de '{Ruta}'", rutaDestino);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR EN ROLLBACK: No se pudo eliminar el archivo huérfano.");
        }
    }

    private static string DeterminarFase(string? hash, bool archivoGuardado)
    {
        if (hash == null) return archivoGuardado ? "PERSISTENCIA" : "HASH";
        if (!archivoGuardado) return "PERSISTENCIA";
        return "SEAL";
    }

    private static void Reportar(
        IProgress<ProgresoDigitalizacion>? progress, string mensaje, int porcentaje, string fase)
    {
        progress?.Report(new ProgresoDigitalizacion(mensaje, porcentaje, fase));
    }
}