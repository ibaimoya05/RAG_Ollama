using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace RAGApp;

class Program
{
    static async Task Main(string[] args)
    {
        // Configurar logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            // Configurar inyección de dependencias
            var services = new ServiceCollection();
            services.AddHttpClient<OllamaClient>();
            services.AddHttpClient<ChromaClient>();
            var serviceProvider = services.BuildServiceProvider();

            var ollamaClient = serviceProvider.GetRequiredService<OllamaClient>();
            var chromaClient = serviceProvider.GetRequiredService<ChromaClient>();

            // Inicializar Chroma (crear colección si no existe)
            await chromaClient.InitializeAsync();

            // Paso 1: Cargar documentos
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");
            var documents = await DocumentLoader.LoadDocumentsAsync(folderPath);
            if (!documents.Any())
            {
                Log.Error("No se cargaron documentos. Finalizando.");
                return;
            }
            Log.Information("Documentos cargados: {Count}", documents.Count);

            // Paso 2: Generar embeddings
            var updatedDocuments = new List<Document>();
            foreach (var doc in documents)
            {
                try
                {
                    var processedContent = TextPreprocessor.Preprocess(doc.Content);
                    var embedding = await ollamaClient.GenerateEmbeddingAsync(processedContent);
                    updatedDocuments.Add(doc with { Embedding = embedding });
                    Log.Information("Embedding generado para documento {Id}", doc.Id);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al generar embedding para el documento {Id}", doc.Id);
                }
            }

            if (!updatedDocuments.Any())
            {
                Log.Error("No se generaron embeddings. Finalizando.");
                return;
            }

            // Paso 3: Almacenar en Chroma
            try
            {
                await chromaClient.AddDocumentsAsync(updatedDocuments);
                Log.Information("Documentos almacenados en Chroma");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al almacenar documentos en Chroma");
                return;
            }

            // Paso 4: Procesar consulta
            Console.WriteLine("Ingresa tu consulta:");
            string? query = Console.ReadLine();
            if (string.IsNullOrEmpty(query))
            {
                Log.Warning("Consulta vacía");
                return;
            }

            // Paso 5: Generar embedding de la consulta
            float[] queryEmbedding;
            try
            {
                var processedQuery = TextPreprocessor.Preprocess(query);
                queryEmbedding = await ollamaClient.GenerateEmbeddingAsync(processedQuery);
                Log.Information("Embedding generado para la consulta");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al generar embedding de la consulta");
                return;
            }

            // Paso 6: Buscar documentos relevantes
            List<string> relevantDocs;
            try
            {
                relevantDocs = await chromaClient.QueryAsync(queryEmbedding);
                Log.Information("Documentos relevantes encontrados: {Count}", relevantDocs.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al buscar documentos relevantes");
                return;
            }

            // Paso 7: Generar respuesta con contexto
            string context = string.Join("\n", relevantDocs);
            string prompt = $"Contexto:\n{context}\n\nConsulta: {query}\nResponde de forma precisa.";
            string response;
            try
            {
                response = await ollamaClient.GenerateResponseAsync(prompt);
                Log.Information("Respuesta generada");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al generar respuesta");
                return;
            }

            Console.WriteLine("\nRespuesta:");
            Console.WriteLine(response);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Error crítico en la aplicación");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}