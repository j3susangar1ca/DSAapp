// 1. APLICACIÓN: DSA.Application/Services/RelationService.cs
namespace DSA.Application.Services;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSA.Domain.Entities;
using dsApp.Domain.Interfaces; // Repositorios y Storage
using DSA.Application.Security; // CryptoUtils

public interface IRelationService
{
    ValueTask VincularDocumentosAsync(Guid oficioId, Guid anexoId, string tipoRelacion, CancellationToken cancellationToken = default);
}

/// <summary>
/// C# 12 Primary Constructor para el motor de relaciones.
/// </summary>
public sealed class RelationService(
    IDocumentoRepository repository,
    IStorageService storageService) : IRelationService
{
    public async ValueTask VincularDocumentosAsync(Guid oficioId, Guid anexoId, string tipoRelacion, CancellationToken cancellationToken = default)
    {
        // 1. Recuperación de Entidades desde EF Core (I/O RAM)
        var oficio = await repository.GetByIdAsync(oficioId, cancellationToken)
            ?? throw new ArgumentException($"Oficio origen {oficioId} inexistente en el acervo.");
        
        var anexo = await repository.GetByIdAsync(anexoId, cancellationToken)
            ?? throw new ArgumentException($"Documento anexo {anexoId} inexistente en el acervo.");

        // 2. Compuerta de Estado Lógico (Vector D[11:0])
        if (!oficio.IsSellado || !anexo.IsSellado)
        {
            throw new InvalidOperationException("Infracción de Integridad: No se pueden relacionar documentos sin Sello SHA-256 previo (D[4]=0).");
        }

        // 3. Compuerta de Existencia Física y Auditoría Criptográfica (I/O Red UNC)
        await ValidarIntegridadFisicaAsync(oficio, cancellationToken);
        await ValidarIntegridadFisicaAsync(anexo, cancellationToken);

        // 4. Mutación Atómica del Dominio
        // (Requiere que Documento.cs implemente el agregado de relaciones)
        oficio.AgregarRelacion(anexoId, tipoRelacion);

        // 5. Persistencia Transaccional Estricta
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async ValueTask ValidarIntegridadFisicaAsync(Documento doc, CancellationToken cancellationToken)
    {
        // 3.1 Verificación de existencia en ruta UNC protegida
        if (!await storageService.FileExistsAsync(doc.PathUNC, cancellationToken))
        {
            throw new FileNotFoundException($"Discrepancia Fatal: El archivo físico de {doc.Id} no se encuentra en el repositorio UNC.");
        }

        // 3.2 Recálculo "Fail-Safe" del Hash SHA-256 para evitar vinculación de archivos corruptos
        await using var fileStream = await storageService.GetFileStreamAsync(doc.PathUNC, cancellationToken);
        string currentHash = await CryptoUtils.ComputeSha256Async(fileStream, cancellationToken);

        if (!currentHash.Equals(doc.HashSHA256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Corrupción Detectada (OWASP/Art. 210-A): El hash físico del archivo {doc.Id} no coincide con el sello criptográfico en base de datos.");
        }
    }
}

// 2. APLICACIÓN/SEGURIDAD: DSA.Application/Security/CryptoUtils.cs
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