using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using translator.Configuration;
using translator.Contracts;
using translator.Controllers.V3;
using translator.Services;

namespace translator.Tests;

public sealed class TranslationControllerV3Tests
{
    [Fact]
    public async Task TranslateBulk_UsesConfiguredBatchSize_AndMapsValuesToOriginalKeys()
    {
        var fakeClient = new FakeLibreTranslateClient(value => $"tx:{value}");
        var controller = CreateController(fakeClient, bulkBatchSize: 2);
        var request = new TranslateRequest
        {
            Target = "fr",
            Data = new Dictionary<string, string>
            {
                ["key-1"] = "Hello world",
                ["key-2"] = "Sample text",
                ["key-3"] = "Another value",
            },
        };

        var result = await controller.TranslateBulk(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<Dictionary<string, string>>(ok.Value);
        Assert.Equal("tx:Hello world", payload["key-1"]);
        Assert.Equal("tx:Sample text", payload["key-2"]);
        Assert.Equal("tx:Another value", payload["key-3"]);
        Assert.Empty(fakeClient.Calls);
        Assert.Equal([2, 1], fakeClient.BulkBatchSizes);
    }

    [Fact]
    public async Task TranslateBulk_UsesDefaultBatchSize_WhenConfiguredValueIsInvalid()
    {
        var fakeClient = new FakeLibreTranslateClient(value => $"tx:{value}");
        var controller = CreateController(fakeClient, bulkBatchSize: 0);
        var request = new TranslateRequest
        {
            Target = "fr",
            Data = Enumerable
                .Range(1, 251)
                .ToDictionary(index => $"key-{index}", index => $"value-{index}"),
        };

        var result = await controller.TranslateBulk(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<Dictionary<string, string>>(ok.Value);
        Assert.Equal(251, payload.Count);
        Assert.Equal([250, 1], fakeClient.BulkBatchSizes);
    }

    private static TranslationControllerV3 CreateController(
        FakeLibreTranslateClient? fakeClient = null,
        int bulkBatchSize = 250
    )
    {
        fakeClient ??= new FakeLibreTranslateClient(value => value);
        var options = Options.Create(
            new TranslationOptions
            {
                SourceLanguage = "en",
                AllowedTargets = ["fr"],
                BulkBatchSize = bulkBatchSize,
            }
        );

        return new TranslationControllerV3(options, fakeClient);
    }

    private sealed class FakeLibreTranslateClient(Func<string, string> translate)
        : ILibreTranslateClient
    {
        public List<TranslationCall> Calls { get; } = [];
        public List<int> BulkBatchSizes { get; } = [];

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
            BulkBatchSizes.Add(texts.Count);
            return Task.FromResult<IReadOnlyList<string>>(texts.Select(translate).ToList());
        }
    }

    private sealed record TranslationCall(string Text, string Source, string Target);
}
