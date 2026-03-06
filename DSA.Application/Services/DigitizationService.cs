using DSA.Domain.Entities;

public sealed class DigitizationService(IScannerService scanner, IHashService hasher) 
{
    public async ValueTask ProcessNewDocumentAsync(Documento doc)
    {
        // 1. Captura asíncrona (Fase 2 del Roadmap)
        var rawData = await scanner.CaptureAsync(doc.Id);
        
        // 2. Generación de Hash SHA-256 (Sello Criptográfico)
        var hash = hasher.ComputeHash(rawData);
        doc.SetHash(hash);
        
        // 3. Actualización del Vector de Estado a 'INGR' (Bit 7)
        // Se aplica lógica binaria D[7] = 1
    }
}