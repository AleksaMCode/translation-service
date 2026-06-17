var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
});
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<translator.Configuration.TranslationOptions>(
    builder.Configuration.GetSection(translator.Configuration.TranslationOptions.SectionName)
);
builder.Services.AddHttpClient<
    translator.Services.ILibreTranslateClient,
    translator.Services.LibreTranslateClient
>(
    (serviceProvider, httpClient) =>
    {
        var options = serviceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<translator.Configuration.TranslationOptions>>()
            .Value;
        httpClient.BaseAddress = new Uri(options.LibreTranslateUrl);
    }
);

var app = builder.Build();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

public partial class Program { }
