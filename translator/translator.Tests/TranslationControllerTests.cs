using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using translator.Configuration;
using translator.Contracts;
using translator.Controllers.V1;
using translator.Services;

namespace translator.Tests;

public sealed class TranslationControllerTests
{
    [Fact]
    public async Task Translate_ReturnsBadRequest_WhenTargetMissing()
    {
        var controller = CreateController();
        var request = new TranslateRequest
        {
            Target = "",
            Data = new Dictionary<string, string> { ["key-1"] = "Hello world" },
        };

        var result = await controller.Translate(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Target language is required.", ReadErrorMessage(badRequest));
    }

    [Fact]
    public async Task Translate_ReturnsBadRequest_WhenTargetNotAllowed()
    {
        var controller = CreateController();
        var request = new TranslateRequest
        {
            Target = "de",
            Data = new Dictionary<string, string> { ["key-1"] = "Hello world" },
        };

        var result = await controller.Translate(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Target language 'de' is not allowed.", ReadErrorMessage(badRequest));
    }

    [Fact]
    public async Task Translate_ReturnsBadRequest_WhenMoreThanOneEntryProvided()
    {
        var controller = CreateController();
        var request = new TranslateRequest
        {
            Target = "fr",
            Data = new Dictionary<string, string>
            {
                ["key-1"] = "Hello world",
                ["key-2"] = "Sample text",
            },
        };

        var result = await controller.Translate(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(
            "The /translate endpoint accepts exactly one key/value pair.",
            ReadErrorMessage(badRequest)
        );
    }

    [Fact]
    public async Task TranslateBulk_ReturnsBadRequest_WhenValueEmpty()
    {
        var controller = CreateController();
        var request = new TranslateRequest
        {
            Target = "fr",
            Data = new Dictionary<string, string> { ["key-1"] = "" },
        };

        var result = await controller.TranslateBulk(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Value for key 'key-1' must not be empty.", ReadErrorMessage(badRequest));
    }

    [Fact]
    public async Task Translate_ReturnsTranslatedDictionary_ForSingleItem()
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
        Assert.Equal("Hello world", fakeClient.Calls[0].Text);
        Assert.Equal("en", fakeClient.Calls[0].Source);
        Assert.Equal("fr", fakeClient.Calls[0].Target);
    }

    [Fact]
    public async Task TranslateBulk_ReturnsTranslatedDictionary_ForAllItems()
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
        Assert.Equal(2, fakeClient.Calls.Count);
        Assert.Empty(fakeClient.BulkCalls);
    }

    private static TranslationControllerV1 CreateController(
        FakeLibreTranslateClient? fakeClient = null
    )
    {
        fakeClient ??= new FakeLibreTranslateClient(value => value);

        var options = Options.Create(
            new TranslationOptions { SourceLanguage = "en", AllowedTargets = ["fr"] }
        );

        return new TranslationControllerV1(options, fakeClient);
    }

    private static string? ReadErrorMessage(BadRequestObjectResult badRequest)
    {
        var value = badRequest.Value;
        if (value is null)
        {
            return null;
        }

        var property = value.GetType().GetProperty("error");
        return property?.GetValue(value)?.ToString();
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
