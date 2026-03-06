using System;
using System.Collections.Generic;
using DSA.Domain.Exceptions;

namespace DSA.Domain.Entities
{
    public sealed class Documento
    {
        public Guid Id { get; private set; }
        public string Nombre { get; private set; }
        public string? PathUNC { get; private set; }
        public string? HashSHA256 { get; private set; }
        public ushort EstadoVector { get; private set; }

        // 1 — El Audit Trail
        private readonly List<EstadoTransicion> _historial = new();
        public IReadOnlyList<EstadoTransicion> Historial => _historial.AsReadOnly();

        public Documento(Guid id, string nombre)
        {
            Id = id;
            Nombre = nombre;
            // Configuración inicial de tu vector (ej. INGR D[7])
        }

        public bool IsSellado    => (EstadoVector & (1 << 4))  != 0;
        public bool IsIngresado  => (EstadoVector & (1 << 7))  != 0;
        public bool IsClasificado => (EstadoVector & (1 << 9)) != 0;
        public bool IsUrgent { get; set; }

        public void SetPath(string path)
        {
            PathUNC = path;
        }

        public void SetHash(string hash)
        {
            HashSHA256 = hash;
            
            // 4 — Llamar a RegistrarTransicion (SEAL)
            var estadoAnterior = EstadoVector;
            EstadoVector |= 1 << 4; // Bit SEAL
            
            // Usamos Math.Min de forma segura en caso de que el hash sea más corto por alguna anomalía
            RegistrarTransicion(
                estadoAnterior, 
                EstadoVector, 
                $"Sello digital aplicado. Hash: {HashSHA256[..Math.Min(16, HashSHA256.Length)]}...");
        }

        public void Archivar()
        {
            if (!IsSellado)
                throw new DocumentoInvalidoException("Invariante Violada: No se puede archivar un documento sin sello digital.");

            // 4 — Llamar a RegistrarTransicion (Archivar)
            var estadoAnterior = EstadoVector;
            EstadoVector |= 1 << 11; // Bit IsArchivado
            
            RegistrarTransicion(estadoAnterior, EstadoVector, "Documento archivado definitivamente.");
        }

        public void Rechazar(string motivo)
        {
            // 4 — Llamar a RegistrarTransicion (Rechazar)
            var estadoAnterior = EstadoVector;
            
            // Lógica de bits de rechazo según tu mapa real (ej. IsRechazado)
            // EstadoVector |= 1 << X; 
            
            RegistrarTransicion(estadoAnterior, EstadoVector, $"Documento rechazado. Motivo: {motivo}");
        }

        /// <summary>
        /// Registra una relación lógica con otro documento.
        /// </summary>
        public void AgregarRelacion(Guid documentoRelacionadoId, string tipoRelacion)
        {
            // TODO: Implementar colección de relaciones cuando se defina la entidad Relacion
            var estadoAnterior = EstadoVector;
            RegistrarTransicion(estadoAnterior, EstadoVector,
                $"Relación '{tipoRelacion}' con documento {documentoRelacionadoId} registrada.");
        }

        // 3 — El método RegistrarTransicion
        private void RegistrarTransicion(ushort desde, ushort hacia, string descripcion)
        {
            _historial.Add(new EstadoTransicion(
                Guid.NewGuid(), 
                desde, 
                hacia, 
                descripcion, 
                DateTime.UtcNow));
        }
    }

    // 2 — El record EstadoTransicion al final del archivo
    public sealed record EstadoTransicion(
        Guid     TransicionId,
        ushort   EstadoDesde,
        ushort   EstadoHacia,
        string   Descripcion,
        DateTime FechaUtc);
}
