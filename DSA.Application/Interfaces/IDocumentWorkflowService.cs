namespace DSA.Application.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;
using DSA.Domain.Entities;

public interface IDocumentWorkflowService
{
    ValueTask EjecutarTransicionArchivadoAsync(Guid documentoId, CancellationToken cancellationToken = default);
    ValueTask ActualizarDocumentoAsync(Documento documento, CancellationToken cancellationToken = default);
}
