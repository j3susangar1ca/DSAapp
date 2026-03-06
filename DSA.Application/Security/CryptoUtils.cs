namespace DSA.Application.Security;

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Utilidad estática para operaciones criptográficas de la capa de aplicación.
/// </summary>
public static class CryptoUtils
{
    public static async ValueTask<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        // Procesamiento asíncrono para no bloquear hilos al leer archivos UNC pesados
        byte[] hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes); 
    }
}
