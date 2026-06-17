using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace translator.Services;

public sealed class LibreTranslateClient(HttpClient httpClient) : ILibreTranslateClient
{
    public async Task<string> TranslateAsync(
        string text,
        string source,
        string target,
        CancellationToken cancellationToken
    )
    {
        var request = new LibreTranslateRequest(text, source, target);
        using var response = await httpClient.PostAsJsonAsync(
            "/translate",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"LibreTranslate returned {(int)response.StatusCode}: {errorBody}"
            );
        }

        var payload = await response.Content.ReadFromJsonAsync<LibreTranslateResponse>(
            cancellationToken
        );
        return payload?.TranslatedText ?? string.Empty;
    }

    public async Task<IReadOnlyList<string>> TranslateManyAsync(
        IReadOnlyList<string> texts,
        string source,
        string target,
        CancellationToken cancellationToken
    )
    {
        if (texts.Count == 0)
        {
            return [];
        }

        var request = new LibreTranslateBulkRequest(texts, source, target);
        using var response = await httpClient.PostAsJsonAsync(
            "/translate",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"LibreTranslate returned {(int)response.StatusCode}: {errorBody}"
            );
        }

        var payload = await response.Content.ReadFromJsonAsync<LibreTranslateBulkResponse>(
            cancellationToken
        );

        var translated = payload?.TranslatedText ?? [];
        if (translated.Count != texts.Count)
        {
            throw new InvalidOperationException(
                "LibreTranslate returned a different number of translations than requested."
            );
        }

        return translated;
    }

    private sealed record LibreTranslateRequest(
        string Q,
        string Source,
        string Target,
        string Format = "text"
    );

    private sealed record LibreTranslateBulkRequest(
        IReadOnlyList<string> Q,
        string Source,
        string Target,
        string Format = "text"
    );

    private sealed record LibreTranslateResponse(string TranslatedText);

    private sealed record LibreTranslateBulkResponse(
        [property: JsonPropertyName("translatedText")] IReadOnlyList<string> TranslatedText
    );
}
