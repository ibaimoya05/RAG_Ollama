namespace RAGApp;

public class EmbeddingResponse
{
    public float[] Embedding { get; set; }
}

public class GenerateResponse
{
    public string response { get; set; }
    public bool? done { get; set; }
    public string model { get; set; } // Opcional
    public DateTime? created_at { get; set; } // Opcional
    public int[] context { get; set; } // Opcional
}