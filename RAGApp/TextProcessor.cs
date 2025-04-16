namespace RAGApp;

public static class TextPreprocessor
{
    public static string Preprocess(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Convertir a minúsculas
        text = text.ToLowerInvariant();

        // Eliminar puntuación y caracteres especiales
        text = new string(text.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());

        return text.Trim();
    }
}