namespace DSA.Application.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::DSA.Application.Interfaces;
using global::DSA.Domain.Entities;
using global::DSA.Domain.Interfaces;

public sealed class DocumentWorkflowService(
    IDocumentoRepository repository,
    ISecurityContext securityContext,
    IScannerService scannerService) : IDocumentWorkflowService
{
    public async ValueTask EjecutarTransicionArchivadoAsync(Guid documentoId, CancellationToken cancellationToken = default)
    {
        if (!securityContext.CurrentUserHasRole("Archivista_Operador"))
        {
            throw new UnauthorizedAccessException("Violación de Acceso: El usuario no posee permisos para archivar.");
        }

        var doc = await repository.GetByIdAsync(documentoId, cancellationToken) 
            ?? throw new ArgumentException($"Documento {documentoId} no localizado en el acervo.");

        if (scannerService.IsDeviceBusy)
        {
            throw new InvalidOperationException("Bloqueo de Hardware: El periférico de captura se encuentra en uso.");
        }
        
        if (!doc.IsIngresado)
        {
            throw new InvalidOperationException("Secuencia Inválida: El documento físico no ha sido ingresado (escaneado) al sistema.");
        }

        if (!doc.IsSellado || string.IsNullOrWhiteSpace(doc.HashSHA256))
        {
            throw new InvalidOperationException("Infracción de Integridad: Sello SHA-256 ausente.");
        }

        if (!doc.IsClasificado)
        {
            throw new InvalidOperationException("Infracción Archivística: El documento carece de taxonomía CADIDO asignada.");
        }

        doc.Archivar();

        await repository.SaveChangesAsync(cancellationToken);
    }
}
