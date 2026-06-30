using Microsoft.Extensions.DependencyInjection;
using OrderSystem.Common;
using OrderSystem.Repositories.Abstract;
using OrderSystem.Repositories.Concrete;
using OrderSystem.Services.Abstract;
using OrderSystem.Services.Concrete;
using OrderSystem.Services.Rag;

namespace OrderSystem.Services;

/// <summary>
/// Registers the RAG help-desk integrations (AI provider clients, document
/// processing and the ingestion worker). Kept beside the other service
/// registrations so the composition root stays a thin aggregator.
/// </summary>
public static class RagServiceCollectionExtensions
{
    public static IServiceCollection AddRag(this IServiceCollection services, AppSettings appSettings)
    {
        var baseAddress = new Uri(appSettings.Rag.BaseUrl.TrimEnd('/') + "/");

        services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>(client => client.BaseAddress = baseAddress);
        services.AddHttpClient<IChatCompletionService, GeminiChatCompletionService>(client => client.BaseAddress = baseAddress);

        services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
        services.AddSingleton<ITextChunker, TextChunker>();
        services.AddScoped<IVectorStore, SqliteVecStore>();

        services.AddHostedService<DocumentIngestionWorker>();

        return services;
    }
}
