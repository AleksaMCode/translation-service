namespace translator.Services;

public interface ILibreTranslateClient
{
    Task<string> TranslateAsync(string text, string source, string target, CancellationToken cancellationToken);
}
