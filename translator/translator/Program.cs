var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<translator.Configuration.TranslationOptions>(
    builder.Configuration.GetSection(translator.Configuration.TranslationOptions.SectionName));
builder.Services.AddHttpClient<translator.Services.ILibreTranslateClient, translator.Services.LibreTranslateClient>(
    (serviceProvider, httpClient) =>
    {
        var options = serviceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<translator.Configuration.TranslationOptions>>()
            .Value;
        httpClient.BaseAddress = new Uri(options.LibreTranslateUrl);
    });

var app = builder.Build();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
