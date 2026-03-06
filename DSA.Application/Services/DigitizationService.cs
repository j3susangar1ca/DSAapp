namespace DSA.Application.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using DSA.Application.Interfaces;
using DSA.Domain.Entities;
using DSA.Domain.Interfaces;

public sealed class DigitizationService(
    IScannerService scannerService,
    IDocumentoRepository repository)
{
    public async Task<Documento> DigitizarNuevoAsync(string nombre, CancellationToken cancellationToken = default)
    {
        var documento = new Documento(Guid.NewGuid(), nombre);
        
        var content = await scannerService.CaptureAsync(documento.Id);
        
        // Simulación de guardado inicial
        // await repository.AddAsync(documento);
        // await repository.SaveChangesAsync(cancellationToken);
        
        return documento;
    }
}