namespace DSA.Infrastructure.AI;

using System.Threading.Tasks;
using DSA.Domain.Interfaces;

public sealed class StubOcrService : IOCRService
{
    public Task<string> ExtractTextAsync(byte[] content) =>
        Task.FromResult("[OCR pendiente de implementación — Tesseract no configurado]");
}
