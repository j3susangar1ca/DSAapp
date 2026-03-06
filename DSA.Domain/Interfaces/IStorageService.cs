namespace DSA.Domain.Interfaces;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public interface IStorageService
{
    /// <summary>
    /// Guarda el arreglo de bytes del documento en la ruta UNC taxonómica y retorna la ruta absoluta.
    /// </summary>
    ValueTask<string> SaveDocumentAsync(Guid documentoId, byte[] pdfBytes, string anio, string serie, string lefort, CancellationToken cancellationToken = default);
    
    ValueTask<bool> FileExistsAsync(string pathUnc, CancellationToken cancellationToken = default);
    
    ValueTask<Stream> GetFileStreamAsync(string pathUnc, CancellationToken cancellationToken = default);

    ValueTask EliminarSiExisteAsync(string pathUnc, CancellationToken cancellationToken = default);
}
