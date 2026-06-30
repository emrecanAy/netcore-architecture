using System.Net.Http.Json;
using System.Text.Json;
using OrderSystem.Common;
using OrderSystem.Services.Abstract;

namespace OrderSystem.Services.Concrete;

/// <summary>
/// <see cref="IEmbeddingService"/> backed by Google Gemini's embedding model.
/// Uses <c>:batchEmbedContents</c> for chunk batches and <c>:embedContent</c> for a
/// single question. The output dimensionality is pinned so vectors always match the
/// vec_chunks table width.
/// </summary>
public class GeminiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly RagSettings _settings;

    public GeminiEmbeddingService(HttpClient httpClient, AppSettings appSettings)
    {
        _httpClient = httpClient;
        _settings = appSettings.Rag;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var model = $"models/{_settings.EmbeddingModel}";
        var request = new
        {
            model,
            content = new { parts = new[] { new { text } } },
            outputDimensionality = _settings.EmbeddingDimensions,
        };

        using var response = await GeminiHttp.SendWithRetryAsync(
            ct => _httpClient.PostAsJsonAsync(
                $"models/{_settings.EmbeddingModel}:embedContent?key={_settings.ApiKey}", request, ct),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return ReadVector(doc.RootElement.GetProperty("embedding"));
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
            return Array.Empty<float[]>();

        // gemini-embedding-001 exposes only single embedContent (its batch endpoint is
        // a heavyweight async job), so embed sequentially. Sequential calls also stay
        // gentle on the free-tier rate limit.
        var vectors = new List<float[]>(texts.Count);
        foreach (var text in texts)
            vectors.Add(await EmbedAsync(text, cancellationToken));

        return vectors;
    }

    private static float[] ReadVector(JsonElement embedding)
    {
        var values = embedding.GetProperty("values");
        var vector = new float[values.GetArrayLength()];
        var i = 0;
        foreach (var value in values.EnumerateArray())
            vector[i++] = value.GetSingle();
        return vector;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"Gemini embedding request failed ({(int)response.StatusCode}): {body}");
    }
}
