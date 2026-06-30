using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using OrderSystem.Common;
using OrderSystem.Services.Abstract;

namespace OrderSystem.Services.Concrete;

/// <summary>
/// <see cref="IChatCompletionService"/> backed by Google Gemini's
/// <c>generateContent</c> endpoint. The system instruction carries the grounding
/// rules; the user prompt carries the question plus retrieved context. Temperature
/// is kept low so answers stay faithful to the supplied context.
/// </summary>
public class GeminiChatCompletionService : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly RagSettings _settings;

    public GeminiChatCompletionService(HttpClient httpClient, AppSettings appSettings)
    {
        _httpClient = httpClient;
        _settings = appSettings.Rag;
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
            generationConfig = new { temperature = 0.2 },
        };

        using var response = await GeminiHttp.SendWithRetryAsync(
            ct => _httpClient.PostAsJsonAsync(
                $"models/{_settings.ChatModel}:generateContent?key={_settings.ApiKey}", request, ct),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Gemini chat request failed ({(int)response.StatusCode}): {error}");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return ReadAnswer(doc.RootElement);
    }

    private static string ReadAnswer(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return string.Empty;

        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts))
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var text))
                builder.Append(text.GetString());
        }

        return builder.ToString().Trim();
    }
}
