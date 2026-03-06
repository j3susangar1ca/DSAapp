namespace DSA.Application.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using DSA.Application.Interfaces;
using DSA.Domain.Entities;
using DSA.Domain.Interfaces;

public sealed class DigitizationService(
    IScannerService scannerService,
    IDocumentoRepository repository,
    IStorageService storageService)
{
    public async Task<(Documento Documento, byte[] Bytes)> DigitizarNuevoAsync(string nombre, CancellationToken ct = default)
    {
        // 1. Instanciación en estado INGR (D[7])
        var documento = new Documento(Guid.NewGuid(), nombre);
        
        // 2. Captura de Hardware (Scanner)
        byte[] pdfBytes = await scannerService.CaptureAsync(documento.Id);
        
        // 3. Persistencia Física (UNC Taxonomy)
        string anio = DateTime.Now.Year.ToString();
        string path = await storageService.SaveDocumentAsync(
            documento.Id, 
            pdfBytes, 
            anio, 
            "Gral", 
            "Entrada", 
            ct);
            
        documento.SetPath(path);

        // 4. Integridad Inicial (Criptografía)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(pdfBytes);
        documento.SetHash(Convert.ToHexString(hashBytes));

        // 5. Registro en Base de Datos (PostgreSQL)
        await repository.AddAsync(documento, ct);
        await repository.SaveChangesAsync(ct);
        
        return (documento, pdfBytes);
    }
}