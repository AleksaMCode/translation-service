namespace translator.Configuration;

public sealed class TranslationOptions
{
    public const string SectionName = "Translation";
    public const int DefaultBulkBatchSize = 250;

    public string SourceLanguage { get; init; } = "en";

    public string LibreTranslateUrl { get; init; } = "http://localhost:5000";

    public IReadOnlyList<string> AllowedTargets { get; init; } = ["fr"];

    public int BulkBatchSize { get; init; } = DefaultBulkBatchSize;
}
