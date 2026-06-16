namespace translator.Contracts;

public sealed class TranslateRequest
{
    public string Target { get; init; } = string.Empty;

    public Dictionary<string, string> Data { get; init; } = [];
}
