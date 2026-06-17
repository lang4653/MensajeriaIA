using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MensajeriaIA.Infrastructure.Services;

public class SimuladorIAService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public SimuladorIAService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public async IAsyncEnumerable<string> GenerarRespuestaStreamAsync(string prompt, string modeloId)
    {
        string apiKey = modeloId switch
        {
            "gpt-4o" => _config["GeminiKeys:Key1"] ?? "",
            "claude-3-5" => _config["GeminiKeys:Key2"] ?? "",
            _ => _config["GeminiKeys:Key3"] ?? ""
        };

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("TU_API_KEY") || apiKey.Contains("censurada"))
        {
            string nombreSimulado = modeloId == "gpt-4o" ? "ChatGPT 4o" : (modeloId == "claude-3-5" ? "Claude 3.5 Sonnet" : "Google Gemini");
            string respuestaFalsa = $"[Modo Simulación Activo]\nHola, soy {nombreSimulado}. Has configurado el enrutador correctamente, pero necesitas colocar una API Key válida en el appsettings.json del ChatService.\n\nTu prompt fue: {prompt}";
            foreach (var palabra in respuestaFalsa.Split(' ')) { yield return palabra + " "; await Task.Delay(50); }
            yield break;
        }

        var client = _httpClientFactory.CreateClient();
        var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        
        // ¡LA SOLUCIÓN! Actualizamos el modelo a "gemini-2.5-flash" para evitar el error 404
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:streamGenerateContent?alt=sse&key={apiKey}";

        var request = new HttpRequestMessage(HttpMethod.Post, url) { 
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json") 
        };
        
        HttpResponseMessage? response = null;
        string? errorMessage = null;

        try {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        } catch (Exception ex) {
            errorMessage = $"[Error crítico de red en el backend]: {ex.Message}";
        }

        if (errorMessage != null)
        {
            yield return errorMessage;
            yield break;
        }

        if (!response!.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            yield return $"[Error de Gemini API ({response.StatusCode})]: Revisa tus credenciales. Detalle: {errorContent}";
            yield break;
        }
        
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            if (line.StartsWith("data: "))
            {
                var json = line.Substring(6);
                if (json == "[DONE]") continue;

                JsonDocument doc;
                try { doc = JsonDocument.Parse(json); } catch { continue; }
                
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var parts = candidates[0].GetProperty("content").GetProperty("parts");
                    if (parts.GetArrayLength() > 0 && parts[0].TryGetProperty("text", out var textProp))
                    {
                        yield return textProp.GetString() ?? "";
                    }
                }
            }
        }
    }
}
