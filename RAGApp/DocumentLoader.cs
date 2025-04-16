using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace RAGApp;

public static class DocumentLoader
{
    public static async Task<List<Document>> LoadDocumentsAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Log.Error("La carpeta {FolderPath} no existe.", folderPath);
            throw new DirectoryNotFoundException($"La carpeta {folderPath} no existe.");
        }

        var documents = new List<Document>();
        int id = 1;

        foreach (var file in Directory.GetFiles(folderPath, "*.txt"))
        {
            try
            {
                string content = await File.ReadAllTextAsync(file);
                if (string.IsNullOrWhiteSpace(content))
                {
                    Log.Warning("El archivo {File} está vacío.", file);
                    continue;
                }
                documents.Add(new Document(id.ToString(), content, Array.Empty<float>()));
                Log.Information("Documento cargado: {File}", file);
                id++;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al cargar el archivo {File}", file);
            }
        }

        if (!documents.Any())
        {
            Log.Warning("No se cargaron documentos desde {FolderPath}.", folderPath);
        }

        return documents;
    }
}