using System.Net.Http.Json;
using Serilog;

namespace RAGApp;

public class OllamaClient
{
    private readonly HttpClient _httpClient;

    public OllamaClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://localhost:11434/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            Log.Error("El contenido para generar embedding está vacío.");
            throw new ArgumentException("El contenido no puede estar vacío.");
        }

        try
        {
            var requestBody = new { model = "all-minilm:l6-v2", prompt = content };
            var response = await _httpClient.PostAsJsonAsync("api/embeddings", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
            if (result?.Embedding == null || result.Embedding.Length == 0)
            {
                Log.Error("Respuesta de embedding vacía o inválida para el contenido: {Content}", content);
                throw new InvalidOperationException("Respuesta de embedding vacía.");
            }

            return result.Embedding;
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "Error al conectar con Ollama para generar embedding.");
            throw;
        }
    }

    public async Task<string> GenerateResponseAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Log.Error("El prompt para generar respuesta está vacío.");
            throw new ArgumentException("El prompt no puede estar vacío.");
        }

        try
        {
            var requestBody = new { model = "llama3", prompt = prompt };
            var response = await _httpClient.PostAsJsonAsync("api/generate", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GenerateResponse>();
            if (string.IsNullOrWhiteSpace(result?.Response))
            {
                Log.Error("Respuesta de generación vacía o inválida para el prompt: {Prompt}", prompt);
                throw new InvalidOperationException("Respuesta de generación vacía.");
            }

            return result.Response;
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "Error al conectar con Ollama para generar respuesta.");
            throw;
        }
    }
}