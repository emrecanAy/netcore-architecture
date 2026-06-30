namespace OrderSystem.Common;

/// <summary>
/// Strongly-typed application configuration. Bound from configuration at startup.
/// Secrets must come from environment/KeyVault, never hard-coded.
/// </summary>
public class AppSettings
{
    public const string SectionName = "App";

    /// <summary>EF Core connection string (SQLite file path for local dev).</summary>
    public string ConnectionString { get; set; } = "Data Source=ordersystem.db";

    /// <summary>Default currency for monetary values created without an explicit currency.</summary>
    public string DefaultCurrency { get; set; } = "USD";

    /// <summary>RAG help-desk settings (AI provider, chunking, retrieval).</summary>
    public RagSettings Rag { get; set; } = new();
}

/// <summary>
/// Configuration for the RAG help-desk. Non-secret knobs live in appsettings
/// (<c>App:Rag</c>); the Gemini API key is injected from the environment at
/// startup and must never be committed.
/// </summary>
public class RagSettings
{
    /// <summary>Gemini API key. Bound from the GEMINI_API_KEY environment variable.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gemini generative-language API base URL.</summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>Model used to generate grounded answers.</summary>
    public string ChatModel { get; set; } = "gemini-2.5-flash";

    /// <summary>Model used to embed chunks and questions.</summary>
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";

    /// <summary>Embedding vector length; must match the vec_chunks virtual table.</summary>
    public int EmbeddingDimensions { get; set; } = 768;

    /// <summary>Target characters per chunk before overlap.</summary>
    public int ChunkSize { get; set; } = 1200;

    /// <summary>Characters of overlap carried between consecutive chunks.</summary>
    public int ChunkOverlap { get; set; } = 200;

    /// <summary>How many chunks to retrieve per question.</summary>
    public int TopK { get; set; } = 5;

    /// <summary>
    /// Minimum cosine similarity for a chunk to count as relevant. Below this for
    /// every hit, the question is treated as out-of-scope and the model is not called.
    /// </summary>
    public double MinScore { get; set; } = 0.6;

    /// <summary>Maximum accepted upload size in bytes.</summary>
    public long MaxUploadBytes { get; set; } = 25 * 1024 * 1024;

    /// <summary>Path/name of the sqlite-vec loadable extension (resolved from the app directory).</summary>
    public string VectorExtensionPath { get; set; } = "vec0";
}
