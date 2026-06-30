using System.Net;

namespace OrderSystem.Services.Concrete;

/// <summary>
/// Shared Gemini HTTP helper. Retries transient failures (429 rate limit, 503
/// overload) with exponential backoff; other responses are returned as-is for the
/// caller to handle. Free-tier Gemini returns these transients routinely.
/// </summary>
internal static class GeminiHttp
{
    private static readonly int[] BackoffMs = { 1000, 2000, 4000, 8000 };

    public static async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var response = await send(cancellationToken);

            if (response.StatusCode is not (HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                || attempt >= BackoffMs.Length)
                return response;

            response.Dispose();
            await Task.Delay(BackoffMs[attempt], cancellationToken);
        }
    }
}
