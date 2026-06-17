using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using translator.Configuration;
using translator.Contracts;
using translator.Controllers.V2;
using translator.Services;

namespace translator.Tests;

public sealed class TranslationControllerV2Tests
{
    [Fact]
    public async Task TranslateBulk_ReturnsTranslatedDictionary_UsingSingleBulkCall()
    {
        var fakeClient = new FakeLibreTranslateClient(value => $"tx:{value}");
        var controller = CreateController(fakeClient);
        var request = new TranslateRequest
        {
            Target = "fr",
            Data = new Dictionary<string, string>
            {
                ["key-1"] = "Hello world",
                ["key-2"] = "Sample text",
            },
        };

        var result = await controller.TranslateBulk(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<Dictionary<string, string>>(ok.Value);
        Assert.Equal("tx:Hello world", payload["key-1"]);
        Assert.Equal("tx:Sample text", payload["key-2"]);
        Assert.Empty(fakeClient.Calls);
        Assert.Single(fakeClient.BulkCalls);
        Assert.Equal(["Hello world", "Sample text"], fakeClient.BulkCalls[0].Texts);
        Assert.Equal("en", fakeClient.BulkCalls[0].Source);
        Assert.Equal("fr", fakeClient.BulkCalls[0].Target);
    }

    [Fact]
    public async Task Translate_ReturnsTranslatedDictionary_UsingSingleCall()
    {
        var fakeClient = new FakeLibreTranslateClient(value => $"fr:{value}");
        var controller = CreateController(fakeClient);
        var request = new TranslateRequest
        {
            Target = "fr",
            Data = new Dictionary<string, string> { ["key-1"] = "Hello world" },
        };

        var result = await controller.Translate(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<Dictionary<string, string>>(ok.Value);
        Assert.Equal("fr:Hello world", payload["key-1"]);
        Assert.Single(fakeClient.Calls);
        Assert.Empty(fakeClient.BulkCalls);
    }

    private static TranslationControllerV2 CreateController(
        FakeLibreTranslateClient? fakeClient = null
    )
    {
        fakeClient ??= new FakeLibreTranslateClient(value => value);

        var options = Options.Create(
            new TranslationOptions { SourceLanguage = "en", AllowedTargets = ["fr"] }
        );

        return new TranslationControllerV2(options, fakeClient);
    }

    private sealed class FakeLibreTranslateClient(Func<string, string> translate)
        : ILibreTranslateClient
    {
        public List<TranslationCall> Calls { get; } = [];
        public List<BulkTranslationCall> BulkCalls { get; } = [];

        public Task<string> TranslateAsync(
            string text,
            string source,
            string target,
            CancellationToken cancellationToken
        )
        {
            Calls.Add(new TranslationCall(text, source, target));
            return Task.FromResult(translate(text));
        }

        public Task<IReadOnlyList<string>> TranslateManyAsync(
            IReadOnlyList<string> texts,
            string source,
            string target,
            CancellationToken cancellationToken
        )
        {
            BulkCalls.Add(new BulkTranslationCall(texts.ToList(), source, target));
            return Task.FromResult<IReadOnlyList<string>>(texts.Select(translate).ToList());
        }
    }

    private sealed record TranslationCall(string Text, string Source, string Target);

    private sealed record BulkTranslationCall(
        IReadOnlyList<string> Texts,
        string Source,
        string Target
    );
}
