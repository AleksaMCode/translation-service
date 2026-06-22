using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using translator.Configuration;
using translator.Contracts;
using translator.Services;

namespace translator.Controllers.V3;

[ApiController]
[ApiVersion("3.0")]
[Route("api/v3")]
public sealed class TranslationControllerV3(
    IOptions<TranslationOptions> translationOptions,
    ILibreTranslateClient libreTranslateClient
) : ControllerBase
{
    [HttpPost("translate")]
    [MapToApiVersion("3.0")]
    public Task<ActionResult<Dictionary<string, string>>> Translate(
        [FromBody] TranslateRequest request,
        CancellationToken cancellationToken
    ) => TranslateInternal(request, isBulkRequest: false, cancellationToken);

    [HttpPost("translate-bulk")]
    [MapToApiVersion("3.0")]
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

        foreach (var entry in request.Data)
        {
            if (string.IsNullOrWhiteSpace(entry.Value))
            {
                return BadRequest(
                    new { error = $"Value for key '{entry.Key}' must not be empty." }
                );
            }
        }

        var source = translationOptions.Value.SourceLanguage;
        if (!isBulkRequest)
        {
            var singleEntry = request.Data.Single();
            var translatedValue = await libreTranslateClient.TranslateAsync(
                singleEntry.Value,
                source,
                request.Target,
                cancellationToken
            );

            return Ok(new Dictionary<string, string> { [singleEntry.Key] = translatedValue });
        }

        var entries = request.Data.ToList();
        var batchSize = translationOptions.Value.BulkBatchSize;
        if (batchSize <= 0)
        {
            batchSize = TranslationOptions.DefaultBulkBatchSize;
        }

        var batchTasks = new List<Task<IReadOnlyList<string>>>();
        for (var start = 0; start < entries.Count; start += batchSize)
        {
            var size = Math.Min(batchSize, entries.Count - start);
            var batchValues = entries.GetRange(start, size).Select(entry => entry.Value).ToList();

            batchTasks.Add(
                libreTranslateClient.TranslateManyAsync(
                    batchValues,
                    source,
                    request.Target,
                    cancellationToken
                )
            );
        }

        var translatedBatches = await Task.WhenAll(batchTasks);
        var results = new Dictionary<string, string>(entries.Count);

        var offset = 0;
        foreach (var translatedBatch in translatedBatches)
        {
            for (var i = 0; i < translatedBatch.Count; i++)
            {
                results[entries[offset + i].Key] = translatedBatch[i];
            }

            offset += translatedBatch.Count;
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
