// ─────────────────────────────────────────────────────────────────────────────
// UncStorageService.cs
// DSA.Infrastructure/Storage/UncStorageService.cs
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DSA.Domain.Interfaces;

namespace DSA.Infrastructure.Storage
{
    /// <summary>
    /// Implementación de IStorageService para almacenamiento en red institucional (UNC/NAS).
    /// Usa escritura atómica (.tmp → rename) para evitar lecturas de archivos incompletos.
    /// </summary>
    public sealed class UncStorageService : IStorageService
    {
        private readonly ILogger<UncStorageService> _logger;

        // Ruta base del servidor de archivos institucional (SMB/UNC)
        private const string BaseUncPath = @"\\10.2.1.92\FAA_divserv_admvos\Nueva Carpeta\Oficios";

        public UncStorageService(ILogger<UncStorageService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Construye la ruta taxonómica CADIDO:
        ///   \\servidor\Oficios\{anio}\{serie}\{lefort}\{documentoId}.pdf
        /// </remarks>
        public async ValueTask<string> SaveDocumentAsync(
            Guid              documentoId,
            byte[]            pdfBytes,
            string            anio,
            string            serie,
            string            lefort,
            CancellationToken cancellationToken = default)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
                throw new ArgumentException("El contenido del PDF no puede estar vacío.", nameof(pdfBytes));

            // 1. Construcción taxonómica de la ruta CADIDO
            string targetFolder = Path.Combine(BaseUncPath, anio, serie, lefort);
            string fileName     = $"{documentoId:N}.pdf";
            string fullPath     = Path.Combine(targetFolder, fileName);

            // 2. Operación de I/O en hilo de fondo para no bloquear el UI Thread
            await Task.Run(async () =>
            {
                // Crear estructura de carpetas si no existe
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                    _logger.LogDebug("Directorio creado: {Dir}", targetFolder);
                }

                // 3. Escritura atómica: .tmp → rename
                //    Evita que otros procesos (dashboard, IA, visor) lean un PDF incompleto
                var rutaTemporal = fullPath + ".tmp";

                try
                {
                    await using var fs = new FileStream(
                        rutaTemporal,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 81920,
                        useAsync: true);

                    await fs.WriteAsync(pdfBytes, cancellationToken);
                    await fs.FlushAsync(cancellationToken);
                }
                catch
                {
                    // Limpiar el .tmp si la escritura falló — nunca dejar archivos huérfanos
                    if (File.Exists(rutaTemporal))
                        try { File.Delete(rutaTemporal); } catch { /* ignorar error de limpieza */ }
                    throw;
                }

                // Renombrado atómico — en Windows sobre la misma unidad es una operación indivisible
                File.Move(rutaTemporal, fullPath, overwrite: false);

            }, cancellationToken);

            _logger.LogInformation(
                "Archivo guardado: {Ruta} ({Kb} KB)",
                fullPath, pdfBytes.Length / 1024);

            return fullPath;
        }

        /// <inheritdoc/>
        public ValueTask<bool> FileExistsAsync(
            string            pathUnc,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(File.Exists(pathUnc));
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Usado por el rollback del pipeline de digitalización cuando falla SetHash.
        /// Si el archivo no existe no lanza excepción — operación idempotente.
        /// </remarks>
        public ValueTask EliminarSiExisteAsync(
            string            pathUnc,
            CancellationToken cancellationToken = default)
        {
            if (File.Exists(pathUnc))
            {
                File.Delete(pathUnc);
                _logger.LogWarning("Archivo eliminado (rollback): {Ruta}", pathUnc);
            }

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask<Stream> GetFileStreamAsync(
            string            pathUnc,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(pathUnc))
                throw new FileNotFoundException(
                    $"El archivo físico no se encuentra en el repositorio UNC: {pathUnc}");

            // FileShare.Read permite lecturas concurrentes desde dashboard, IA y otros operadores
            Stream stream = new FileStream(
                pathUnc,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            return ValueTask.FromResult(stream);
        }
    }
}