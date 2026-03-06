namespace DSA.Application.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using DSA.Domain.Entities;
// Utilizando interfaces inferidas de la inyección de dependencias
using dsApp.Domain.Interfaces; 

/// <summary>
/// Interfaz para orquestación de ciclo de vida documental.
/// </summary>
public interface IDocumentWorkflowService
{
    ValueTask EjecutarTransicionArchivadoAsync(Guid documentoId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Servicio de aplicación con jerarquía matemática de validación (C# 12 Primary Constructor).
/// </summary>
public sealed class DocumentWorkflowService(
    IDocumentoRepository repository,
    ISecurityContext securityContext, // Abstracción de permisos/UI
    IScannerService scannerService) : IDocumentWorkflowService
{
    public async ValueTask EjecutarTransicionArchivadoAsync(Guid documentoId, CancellationToken cancellationToken = default)
    {
        // 1. Compuerta de Seguridad (Permisos/UI) - RAM (Microsegundos)
        if (!securityContext.CurrentUserHasRole("Archivista_Operador"))
        {
            throw new UnauthorizedAccessException("Violación de Acceso: El usuario no posee permisos para archivar.");
        }

        // Recuperación de la entidad (I/O)
        var doc = await repository.GetByIdAsync(documentoId, cancellationToken) 
            ?? throw new ArgumentException($"Documento {documentoId} no localizado en el acervo.");

        // 2. Compuerta de Hardware (WIA/TWAIN) - RAM
        // Asegurar que el escáner no tenga un bloqueo activo sobre este documento
        if (scannerService.IsDeviceBusy)
        {
            throw new InvalidOperationException("Bloqueo de Hardware: El periférico de captura se encuentra en uso.");
        }
        
        if (!doc.IsIngresado) // D[7]
        {
            throw new InvalidOperationException("Secuencia Inválida: El documento físico no ha sido ingresado (escaneado) al sistema.");
        }

        // 3. Compuerta Criptográfica (Hash SHA-256) - RAM
        // Validación de inmutabilidad probatoria (Art. 210-A)
        if (!doc.IsSellado || string.IsNullOrWhiteSpace(doc.HashSHA256)) // D[4]
        {
            throw new InvalidOperationException("Infracción de Integridad: Sello SHA-256 ausente.");
        }

        // 4. Compuerta Normativa (Reglas CADIDO/Lefort) - RAM
        if (!doc.IsClasificado) // D[3]
        {
            throw new InvalidOperationException("Infracción Archivística: El documento carece de taxonomía CADIDO asignada.");
        }

        // Ejecución de la mutación de estado en la Entidad (Máquina de Estados D[11:0])
        doc.Archivar();

        // 5. Compuerta de Persistencia (PostgreSQL) - I/O Costoso (Milisegundos)
        // Se ejecuta únicamente si todas las compuertas lógicas previas (RAM) retornaron TRUE
        await repository.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Interfaz simulada para la compuerta de seguridad (Paso 1).
/// </summary>
public interface ISecurityContext
{
    bool CurrentUserHasRole(string roleName);
}