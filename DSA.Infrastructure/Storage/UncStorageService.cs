namespace DSA.Infrastructure.Storage;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSA.Domain.Interfaces;

/// <summary>
/// Implementación física para el manejo de archivos en red institucional.
/// C# 12 Primary Constructor sin dependencias adicionales.
/// </summary>
public sealed class UncStorageService : IStorageService
{
    // Ruta estática base definida para el servidor de archivos (SMB/UNC)
    private const string BaseUncPath = @"\\10.2.1.92\FAA_divserv_admvos\Nueva Carpeta\Oficios";

    public async ValueTask<string> SaveDocumentAsync(Guid documentoId, byte[] pdfBytes, string anio, string serie, string lefort, CancellationToken cancellationToken = default)
    {
        // 1. Construcción taxonómica de la ruta CADIDO (Ej. \2026\Serie_A\Lefort_1)
        string targetFolder = Path.Combine(BaseUncPath, anio, serie, lefort);
        string fileName = $"{documentoId}.pdf";
        string fullPath = Path.Combine(targetFolder, fileName);

        // 2. Aislamiento estricto de I/O de red para no bloquear el Dispatcher de WinUI 3
        await Task.Run(async () =>
        {
            if (!Directory.Exists(targetFolder))
            {
                // Directory.CreateDirectory crea la cadena completa de carpetas si no existen
                Directory.CreateDirectory(targetFolder);
            }

            // 3. Persistencia física atómica de los bytes capturados por el WIA/TWAIN
            await File.WriteAllBytesAsync(fullPath, pdfBytes, cancellationToken);
            
        }, cancellationToken);

        return fullPath; // Se retorna la ruta para ser inyectada en Documento.PathUNC
    }

    public ValueTask<bool> FileExistsAsync(string pathUnc, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(File.Exists(pathUnc));
    }

    public ValueTask<Stream> GetFileStreamAsync(string pathUnc, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(pathUnc))
        {
            throw new FileNotFoundException($"Falla de Integridad: El archivo físico no se encuentra en el repositorio UNC: {pathUnc}");
        }

        // Se exige el uso de FileShare.Read para permitir consultas concurrentes (dashboard, IA, otros operadores)
        Stream stream = new FileStream(pathUnc, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        
        return ValueTask.FromResult(stream);
    }

    public ValueTask EliminarSiExisteAsync(string pathUnc, CancellationToken cancellationToken = default)
    {
        if (File.Exists(pathUnc))
        {
            File.Delete(pathUnc);
        }
        
        return ValueTask.CompletedTask;
    }
}