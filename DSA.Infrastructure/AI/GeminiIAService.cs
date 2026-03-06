namespace DSA.Infrastructure.AI;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DSA.Domain.Interfaces;
using System.Text;
using Microsoft.Extensions.Configuration;

public sealed class GeminiIAService : IIAService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;

    public GeminiIAService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // Lectura segura desde IConfiguration (appsettings.json → variables de entorno)
        _apiKey = configuration["AiSettings:GeminiApiKey"]
            ?? throw new InvalidOperationException(
                "CONFIGURACIÓN FALTANTE: La clave 'AiSettings:GeminiApiKey' no fue encontrada. " +
                "Defínala en appsettings.json o como variable de entorno 'AiSettings__GeminiApiKey'.");

        _endpoint = configuration["AiSettings:GeminiEndpoint"]
            ?? "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
    }

    public Task<string> AnalyzeTextAsync(string text)
    {
        return Task.FromResult(text);
    }

    public async Task<DocumentMetadataResult> AnalyzeSemanticsAsync(string ocrText)
    {
        var prompt = $"Analiza este texto OCR y devuelve un JSON con las llaves: folio, remitente, asunto, urgente (boolean). Texto: {ocrText}";

        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { response_mime_type = "application/json" }
        };

        try
        {
            var response = await _httpClient.PostAsync($"{_endpoint}?key={_apiKey}", 
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
            
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            
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
