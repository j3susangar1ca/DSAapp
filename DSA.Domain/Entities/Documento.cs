namespace SIA.Domain.Entities;

public sealed class Documento(Guid id, string nombre)
{
    public Guid Id { get; } = id;
    public string Nombre { get; init; } = nombre;
    public string PathUNC { get; private set; } = string.Empty;
    public string HashSHA256 { get; private set; } = string.Empty;
    
    // Vector de Estado D[11:0] inicializado en INGR (Bit 7)
    public ushort EstadoVector { get; private set; } = 0b0000_1000_0000; 

    public void SetPath(string path) => PathUNC = path;
    public void SetHash(string hash) => HashSHA256 = hash;
    
    public bool IsUrgent => (EstadoVector & 0x40) != 0; // Bit 6
}