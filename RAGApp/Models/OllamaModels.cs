namespace RAGApp;

public class EmbeddingResponse
{
    public float[] Embedding { get; set; }
}

public class GenerateResponse
{
    public string Response { get; set; } 
    public bool? Done { get; set; } 
    public string Model { get; set; } 
    public DateTime? CreatedAt { get; set; } 
    public int[] Context { get; set; } 
}