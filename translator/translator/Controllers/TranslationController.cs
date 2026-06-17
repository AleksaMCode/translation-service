using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using translator.Configuration;
using translator.Contracts;
using translator.Services;

namespace translator.Controllers;

[ApiController]
[Route("")]
public sealed class TranslationController(
    IOptions<TranslationOptions> translationOptions,
    ILibreTranslateClient libreTranslateClient
) : ControllerBase
{
    [HttpPost("translate")]
    public Task<ActionResult<Dictionary<string, string>>> Translate(
        [FromBody] TranslateRequest request,
        CancellationToken cancellationToken
    ) => TranslateInternal(request, isBulkRequest: false, cancellationToken);

    [HttpPost("translate-bulk")]
    public Task<ActionResult<Dictionary<string, string>>> TranslateBulk(
        [FromBody] TranslateRequest request,
        CancellationToken cancellationToken
    ) => TranslateInternal(request, isBulkRequest: true, cancellationToken);

    private async Task<ActionResult<Dictionary<string, string>>> TranslateInternal(
        TranslateRequest request,
        bool isBulkRequest,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.Target))
        {
            return BadRequest(new { error = "Target language is required." });
        }

        if (!IsAllowedTarget(request.Target))
        {
            return BadRequest(
                new { error = $"Target language '{request.Target}' is not allowed." }
            );
        }

        if (request.Data.Count == 0)
        {
            return BadRequest(new { error = "Data cannot be empty." });
        }

        if (!isBulkRequest && request.Data.Count != 1)
        {
            return BadRequest(
                new { error = "The /translate endpoint accepts exactly one key/value pair." }
            );
        }

        var results = new Dictionary<string, string>(request.Data.Count);
        var source = translationOptions.Value.SourceLanguage;

        foreach (var entry in request.Data)
        {
            if (string.IsNullOrWhiteSpace(entry.Value))
            {
                return BadRequest(
                    new { error = $"Value for key '{entry.Key}' must not be empty." }
                );
            }

            var translatedValue = await libreTranslateClient.TranslateAsync(
                entry.Value,
                source,
                request.Target,
                cancellationToken
            );

            results[entry.Key] = translatedValue;
        }

        return Ok(results);
    }

    private bool IsAllowedTarget(string target)
    {
        var allowedTargets = translationOptions.Value.AllowedTargets;
        return allowedTargets.Any(code =>
            string.Equals(code, target, StringComparison.OrdinalIgnoreCase)
        );
    }
}
