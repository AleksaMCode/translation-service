using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace translator.Tests.Integration;

public sealed class TranslationEndpointsTests(TranslationApiFactory factory)
    : IClassFixture<TranslationApiFactory>
{
    [Fact]
    public async Task Translate_ReturnsBadRequest_WhenTargetNotAllowed()
    {
        var client = factory.CreateClient();
        var request = new
        {
            target = "de",
            data = new Dictionary<string, string> { ["key-1"] = "Hello world" },
        };

        var response = await client.PostAsJsonAsync("/api/v1/translate", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Target language 'de' is not allowed.", await ReadErrorMessage(response));
    }

    [Fact]
    public async Task Translate_ReturnsOk_WithTranslatedPayload()
    {
        var client = factory.CreateClient();
        var request = new
        {
            target = "fr",
            data = new Dictionary<string, string> { ["key-1"] = "Hello world" },
        };

        var response = await client.PostAsJsonAsync("/api/v1/translate", request);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("fr:Hello world", payload["key-1"]);
    }

    [Fact]
    public async Task TranslateBulk_ReturnsOk_WithAllTranslatedItems()
    {
        var client = factory.CreateClient();
        factory.ClientSpy.Reset();
        var request = new
        {
            target = "fr",
            data = new Dictionary<string, string>
            {
                ["key-1"] = "Hello world",
                ["key-2"] = "Sample text",
            },
        };

        var response = await client.PostAsJsonAsync("/api/v1/translate-bulk", request);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("fr:Hello world", payload["key-1"]);
        Assert.Equal("fr:Sample text", payload["key-2"]);
        Assert.Equal(2, factory.ClientSpy.SingleCalls);
        Assert.Equal(0, factory.ClientSpy.BulkCalls);
    }

    [Fact]
    public async Task TranslateBulk_V2_UsesSingleBulkCall()
    {
        var client = factory.CreateClient();
        factory.ClientSpy.Reset();
        var request = new
        {
            target = "fr",
            data = new Dictionary<string, string>
            {
                ["key-1"] = "Hello world",
                ["key-2"] = "Sample text",
            },
        };

        var response = await client.PostAsJsonAsync("/api/v2/translate-bulk", request);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("fr:Hello world", payload["key-1"]);
        Assert.Equal("fr:Sample text", payload["key-2"]);
        Assert.Equal(0, factory.ClientSpy.SingleCalls);
        Assert.Equal(1, factory.ClientSpy.BulkCalls);
    }

    private static async Task<string?> ReadErrorMessage(HttpResponseMessage response)
    {
        var document = await response.Content.ReadFromJsonAsync<JsonDocument>();
        if (document is null)
        {
            return null;
        }

        return document.RootElement.TryGetProperty("error", out var errorElement)
            ? errorElement.GetString()
            : null;
    }
}
