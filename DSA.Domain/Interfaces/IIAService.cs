namespace DSA.Domain.Interfaces;

using System.Threading.Tasks;

public record DocumentMetadataResult(string Folio, string Remitente, string Asunto, bool Urgente);

public interface IIAService
{
    Task<string> AnalyzeTextAsync(string text);
    Task<DocumentMetadataResult> AnalyzeSemanticsAsync(string ocrText);
}
