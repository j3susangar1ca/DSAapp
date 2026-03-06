using System;
using System.Collections.Generic;
using DSA.Domain.Exceptions;

namespace DSA.Domain.Entities
{
    public sealed class Documento
    {
        public Guid    Id           { get; private set; }
        public string  Nombre       { get; private set; }
        public string? PathUNC      { get; private set; }
        public string? HashSHA256   { get; private set; }
        public ushort  EstadoVector { get; private set; }

        private readonly List<EstadoTransicion> _historial = new();
        public IReadOnlyList<EstadoTransicion> Historial => _historial.AsReadOnly();

        public Documento(Guid id, string nombre)
        {
            Id     = id;
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
        }

        // ─── Mapa de Bits D[11:0] ─────────────────────────────────────────────
        public bool IsSellado     => (EstadoVector & (1 << 4))  != 0;
        public bool IsIngresado   => (EstadoVector & (1 << 7))  != 0;
        public bool IsClasificado => (EstadoVector & (1 << 9))  != 0;
        public bool IsArchivado   => (EstadoVector & (1 << 11)) != 0; // FIX: faltaba
        public bool IsRechazado   => (EstadoVector & (1 << 10)) != 0; // FIX: faltaba
        public bool IsUrgent      { get; set; }

        // ─── Mutaciones ───────────────────────────────────────────────────────
        public void SetPath(string path)
        {
            PathUNC = path ?? throw new ArgumentNullException(nameof(path));
        }

        public void SetHash(string hash)
        {
            HashSHA256 = hash ?? throw new ArgumentNullException(nameof(hash));
            var anterior = EstadoVector;
            EstadoVector |= 1 << 4;
            RegistrarTransicion(anterior, EstadoVector,
                $"Sello digital aplicado. Hash: {HashSHA256[..Math.Min(16, HashSHA256.Length)]}...");
        }

        public void Archivar()
        {
            if (!IsSellado)
                throw new DocumentoInvalidoException(
                    "Invariante Violada: No se puede archivar un documento sin sello digital (D[4]=0).");
            var anterior = EstadoVector;
            EstadoVector |= 1 << 11;
            RegistrarTransicion(anterior, EstadoVector, "Documento archivado definitivamente.");
        }

        // FIX: valor default para que DocumentWorkViewModel pueda llamar Rechazar() sin argumento
        public void Rechazar(string motivo = "Sin motivo especificado")
        {
            var anterior = EstadoVector;
            EstadoVector |= 1 << 10;
            RegistrarTransicion(anterior, EstadoVector, $"Documento rechazado. Motivo: {motivo}");
        }

        // FIX: requerido por DocumentWorkViewModel.GuardarMetadatosAsync
        public void SetUrgencia(bool urgente)
        {
            IsUrgent = urgente;
        }

        // FIX: requerido por DocumentWorkViewModel.GuardarMetadatosAsync
        public void ValidarClasificacion()
        {
            if (!IsClasificado)
                throw new DocumentoInvalidoException(
                    "Infracción Archivística: El documento carece de taxonomía CADIDO (D[9]=0).");
        }

        public void AgregarRelacion(Guid documentoRelacionadoId, string tipoRelacion)
        {
            var anterior = EstadoVector;
            RegistrarTransicion(anterior, EstadoVector,
                $"Relación '{tipoRelacion}' con documento {documentoRelacionadoId} registrada.");
        }

        private void RegistrarTransicion(ushort desde, ushort hacia, string descripcion)
        {
            _historial.Add(new EstadoTransicion(
                Guid.NewGuid(), desde, hacia, descripcion, DateTime.UtcNow));
        }
    }

    public sealed record EstadoTransicion(
        Guid     TransicionId,
        ushort   EstadoDesde,
        ushort   EstadoHacia,
        string   Descripcion,
        DateTime FechaUtc);
}