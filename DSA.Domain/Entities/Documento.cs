namespace DSA.Domain.Entities;

using System;

/// <summary>
/// Entidad principal regida por el Vector de Estado D[11:0] para control estricto de ciclo de vida.
/// C# 12: Primary Constructor implementado.
/// </summary>
public sealed class Documento(Guid id, string nombre)
{
    public Guid Id { get; } = id;
    public string Nombre { get; init; } = nombre;
    
    public string PathUNC { get; private set; } = string.Empty;
    public string HashSHA256 { get; private set; } = string.Empty;
    
    // Vector de Estado D[11:0] inicializado en INGR (Bit 7 = 1)
    public ushort EstadoVector { get; private set; } = 0b0000_1000_0000; 

    // Mapeo inmutable de Bits D[11:0]
    public bool IsArchivado => (EstadoVector & (1 << 11)) != 0;   // D[11] ARCH
    public bool IsRechazado => (EstadoVector & (1 << 10)) != 0;   // D[10] RECH
    public bool IsEnValidacion => (EstadoVector & (1 << 9)) != 0; // D[9] VALD
    public bool IsEnProceso => (EstadoVector & (1 << 8)) != 0;    // D[8] PROC
    public bool IsIngresado => (EstadoVector & (1 << 7)) != 0;    // D[7] INGR
    public bool IsUrgent => (EstadoVector & (1 << 6)) != 0;       // D[6] URG
    public bool IsOcrCompletado => (EstadoVector & (1 << 5)) != 0;// D[5] OCR
    public bool IsSellado => (EstadoVector & (1 << 4)) != 0;      // D[4] SEAL
    public bool IsClasificado => (EstadoVector & (1 << 3)) != 0;  // D[3] CLAS

    public void SetPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        PathUNC = path;
    }

    public void SetHash(string hash) 
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        HashSHA256 = hash;
        EstadoVector |= 1 << 4; // Activar el bit SEAL (D[4])
    }

    public void SetUrgencia(bool esUrgente)
    {
        if (esUrgente)
            EstadoVector |= 1 << 6; // Enciende D[6]
        else
            EstadoVector &= unchecked((ushort)~(1 << 6)); // Apaga D[6]
    }

    public void ValidarClasificacion()
    {
        if (IsArchivado || IsRechazado)
            throw new InvalidOperationException("El documento está en un estado terminal.");
            
        EstadoVector |= 1 << 9; // Activa VALD (D[9])
        EstadoVector |= 1 << 3; // Activa CLAS (D[3])
    }

    public void Rechazar()
    {
        if (IsArchivado)
            throw new InvalidOperationException("No se puede rechazar un documento ya archivado.");
            
        EstadoVector |= 1 << 10; // Activa RECH (D[10])
        EstadoVector &= unchecked((ushort)~((1 << 8) | (1 << 9))); // Apaga PROC y VALD
    }

    public void Archivar()
    {
        if (!IsSellado || string.IsNullOrWhiteSpace(HashSHA256))
            throw new InvalidOperationException("Infracción de Integridad (Código 210-A): Falla Sello SHA-256.");
        
        if (IsArchivado || IsRechazado)
            throw new InvalidOperationException("El documento ya se encuentra en un estado terminal.");

        EstadoVector |= 1 << 11; // Activa ARCH (D[11])
        EstadoVector &= unchecked((ushort)~((1 << 8) | (1 << 9))); // Apaga PROC y VALD
    }
}
