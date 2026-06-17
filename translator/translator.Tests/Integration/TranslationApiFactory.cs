using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using translator.Services;

namespace translator.Tests.Integration;

public sealed class TranslationApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ILibreTranslateClient>();
            services.AddSingleton<ILibreTranslateClient>(new FakeLibreTranslateClient());
        });
    }

    private sealed class FakeLibreTranslateClient : ILibreTranslateClient
    {
        public Task<string> TranslateAsync(
            string text,
            string source,
            string target,
            CancellationToken cancellationToken
        ) => Task.FromResult($"{target}:{text}");
    }
}
