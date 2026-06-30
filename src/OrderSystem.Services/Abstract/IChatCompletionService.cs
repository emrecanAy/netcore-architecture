namespace OrderSystem.Services.Abstract;

/// <summary>
/// Generates a grounded answer from a system instruction and a user prompt.
/// Provider-agnostic; the help-desk passes retrieved context inside the prompt.
/// </summary>
public interface IChatCompletionService
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
