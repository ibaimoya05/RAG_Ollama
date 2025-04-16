namespace RAGApp;

public class QueryResponse
{
    public List<List<Metadata>> Metadatas { get; set; }
}

public class Metadata
{
    public string Content { get; set; }
}