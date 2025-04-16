namespace RAGApp;

public record Document(
    string Id, 
    string Content, 
    float[] Embedding    
);