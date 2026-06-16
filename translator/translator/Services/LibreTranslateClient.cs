using System.Net.Http.Json;

namespace translator.Services;

public sealed class LibreTranslateClient(HttpClient httpClient) : ILibreTranslateClient
{
    public async Task<string> TranslateAsync(string text, string source, string target, CancellationToken cancellationToken)
    {
        var request = new LibreTranslateRequest(text, source, target);
        using var response = await httpClient.PostAsJsonAsync("/translate", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"LibreTranslate returned {(int)response.StatusCode}: {errorBody}");
        }

        var payload = await response.Content.ReadFromJsonAsync<LibreTranslateResponse>(cancellationToken);
        return payload?.TranslatedText ?? string.Empty;
    }

    private sealed record LibreTranslateRequest(string Q, string Source, string Target, string Format = "text");

    private sealed record LibreTranslateResponse(string TranslatedText);
}
