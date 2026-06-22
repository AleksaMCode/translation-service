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
        private readonly object syncRoot = new();
        public int SingleCalls { get; private set; }
        public int BulkCalls { get; private set; }
        public List<int> BulkBatchSizes { get; } = [];

        public Task<string> TranslateAsync(
            string text,
            string source,
            string target,
            CancellationToken cancellationToken
        )
        {
            lock (syncRoot)
            {
                SingleCalls++;
            }
            return Task.FromResult($"{target}:{text}");
        }

        public Task<IReadOnlyList<string>> TranslateManyAsync(
            IReadOnlyList<string> texts,
            string source,
            string target,
            CancellationToken cancellationToken
        )
        {
            lock (syncRoot)
            {
                BulkCalls++;
                BulkBatchSizes.Add(texts.Count);
            }
            return Task.FromResult<IReadOnlyList<string>>(
                texts.Select(text => $"{target}:{text}").ToList()
            );
        }

        public void Reset()
        {
            lock (syncRoot)
            {
                SingleCalls = 0;
                BulkCalls = 0;
                BulkBatchSizes.Clear();
            }
        }
    }
}
