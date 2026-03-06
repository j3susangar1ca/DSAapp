namespace DSA.Infrastructure.AI;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DSA.Domain.Interfaces;
using System.Text;

public sealed class GeminiIAService(HttpClient httpClient) : IIAService
{
    // API KEY HARDCOREADA (LM STUDIO / GEMINI COMPATIBLE ENDPOINT)
    private const string ApiKey = "YOUR_GEMINI_API_KEY"; 
    private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

    public Task<string> AnalyzeTextAsync(string text)
    {
        // Implementación básica o delegación
        return Task.FromResult(text);
    }

    public async Task<DocumentMetadataResult> AnalyzeSemanticsAsync(string ocrText)
    {
        var prompt = $"Analiza este texto OCR y devuelve un JSON con las llaves: folio, remitente, asunto, urgente (boolean). Texto: {ocrText}";

        // Estructura de petición para Google Gemini API
        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { response_mime_type = "application/json" }
        };

        try
        {
            var response = await httpClient.PostAsync($"{Endpoint}?key={ApiKey}", 
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
            
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            
            // Extracción del contenido desde el formato anidado de Gemini
            using var doc = JsonDocument.Parse(jsonString);
            var textResponse = doc.RootElement.GetProperty("candidates")[0]
                                .GetProperty("content").GetProperty("parts")[0]
                                .GetProperty("text").GetString();

            var result = JsonSerializer.Deserialize<GeminiRawResult>(textResponse!, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new DocumentMetadataResult(
                result?.Folio ?? "PENDIENTE",
                result?.Remitente ?? "DESCONOCIDO",
                result?.Asunto ?? "SIN ASUNTO",
                result?.Urgente ?? false
            );
        }
        catch (Exception ex)
        {
            return new DocumentMetadataResult("ERR_GEMINI", "FALLA_CONEXION", ex.Message, false);
        }
    }

    private record GeminiRawResult(string Folio, string Remitente, string Asunto, bool Urgente);
}
