using System.Net.Http.Json;
using Serilog;

namespace RAGApp;

public class ChromaClient
{
    private readonly HttpClient _httpClient;
    private const string Tenant = "default_tenant";
    private const string Database = "default_database";
    private const string CollectionName = "rag_collection";
    private string? _collectionId;

    public ChromaClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://localhost:8000/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task InitializeAsync()
    {
        // Verificar si la colección existe, y crearla si no
        try
        {
            var collections = await _httpClient.GetFromJsonAsync<List<Collection>>(
                $"/api/v2/tenants/{Tenant}/databases/{Database}/collections?tenant={Tenant}&database={Database}");

            var collection = collections?.FirstOrDefault(c => c.Name == CollectionName);
            if (collection != null)
            {
                _collectionId = collection.Id;
                Log.Information("Colección {CollectionName} encontrada con ID {CollectionId}", CollectionName, _collectionId);
            }
            else
            {
                // Crear la colección
                var createRequest = new { name = CollectionName };
                var createResponse = await _httpClient.PostAsJsonAsync(
                    $"/api/v2/tenants/{Tenant}/databases/{Database}/collections", createRequest);
                createResponse.EnsureSuccessStatusCode();

                var createdCollection = await createResponse.Content.ReadFromJsonAsync<Collection>();
                _collectionId = createdCollection?.Id;
                Log.Information("Colección {CollectionName} creada con ID {CollectionId}", CollectionName, _collectionId);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al inicializar la colección en Chroma");
            throw;
        }
    }

    public async Task AddDocumentsAsync(IEnumerable<Document> documents)
    {
        if (_collectionId == null)
        {
            await InitializeAsync();
        }

        if (!documents.Any())
        {
            Log.Warning("No hay documentos para almacenar en Chroma.");
            return;
        }

        try
        {
            var requestBody = new
            {
                ids = documents.Select(d => d.Id).ToArray(),
                embeddings = documents.Select(d => d.Embedding).ToArray(),
                metadatas = documents.Select(d => new { content = d.Content }).ToArray()
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v2/tenants/{Tenant}/databases/{Database}/collections/{_collectionId}/add", requestBody);
            response.EnsureSuccessStatusCode();
            Log.Information("Documentos almacenados en Chroma: {Count}", documents.Count());
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "Error al conectar con Chroma para almacenar documentos.");
            throw;
        }
    }

    public async Task<List<string>> QueryAsync(float[] queryEmbedding)
    {
        if (_collectionId == null)
        {
            await InitializeAsync();
        }

        if (queryEmbedding == null || queryEmbedding.Length == 0)
        {
            Log.Error("El embedding de la consulta está vacío.");
            throw new ArgumentException("El embedding de la consulta no puede estar vacío.");
        }

        try
        {
            var requestBody = new
            {
                query_embeddings = new[] { queryEmbedding },
                n_results = 2,
                include = new[] { "metadatas" }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v2/tenants/{Tenant}/databases/{Database}/collections/{_collectionId}/query", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<QueryResponse>();
            var contents = result?.Metadatas?.FirstOrDefault()?.Select(m => m?.Content ?? string.Empty).ToList() ?? new List<string>();

            if (!contents.Any())
            {
                Log.Warning("No se encontraron documentos relevantes para la consulta.");
            }

            return contents;
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "Error al conectar con Chroma para buscar documentos.");
            throw;
        }
    }
}

public class Collection
{
    public string? Database { get; set; }
    public int? Dimension { get; set; } // Cambiado a int? para permitir null
    public string? Id { get; set; }
    public long? LogPosition { get; set; } // Cambiado a long? para permitir null
    public Dictionary<string, object>? Metadata { get; set; }
    public string? Name { get; set; }
    public string? Tenant { get; set; }
    public int? Version { get; set; } // Cambiado a int? para permitir null
}