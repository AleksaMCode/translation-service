namespace translator.Services;

public interface ILibreTranslateClient
{
    Task<string> TranslateAsync(
        string text,
        string source,
        string target,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<string>> TranslateManyAsync(
        IReadOnlyList<string> texts,
        string source,
        string target,
        CancellationToken cancellationToken
    );
}
