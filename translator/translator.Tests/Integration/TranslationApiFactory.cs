using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using translator.Services;

namespace translator.Tests.Integration;

public sealed class TranslationApiFactory : WebApplicationFactory<Program>
{
    internal FakeLibreTranslateClient ClientSpy { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ILibreTranslateClient>();
            services.AddSingleton<ILibreTranslateClient>(ClientSpy);
        });
    }

    internal sealed class FakeLibreTranslateClient : ILibreTranslateClient
    {
        public int SingleCalls { get; private set; }
        public int BulkCalls { get; private set; }

        public Task<string> TranslateAsync(
            string text,
            string source,
            string target,
            CancellationToken cancellationToken
        )
        {
            SingleCalls++;
            return Task.FromResult($"{target}:{text}");
        }

        public Task<IReadOnlyList<string>> TranslateManyAsync(
            IReadOnlyList<string> texts,
            string source,
            string target,
            CancellationToken cancellationToken
        )
        {
            BulkCalls++;
            return Task.FromResult<IReadOnlyList<string>>(
                texts.Select(text => $"{target}:{text}").ToList()
            );
        }

        public void Reset()
        {
            SingleCalls = 0;
            BulkCalls = 0;
        }
    }
}
