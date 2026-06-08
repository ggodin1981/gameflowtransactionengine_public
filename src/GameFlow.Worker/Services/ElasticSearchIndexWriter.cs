using System.Net.Http.Json;
using GameFlow.Shared.Search;
using GameFlow.Worker.Options;
using Microsoft.Extensions.Options;

namespace GameFlow.Worker.Services;

public sealed class ElasticSearchIndexWriter(
    HttpClient httpClient,
    IOptions<ElasticSearchOptions> options,
    ILogger<ElasticSearchIndexWriter> logger) : ISearchIndexWriter
{
    private readonly ElasticSearchOptions _options = options.Value;

    public async Task IndexAsync(IndexedTransactionDocument document, CancellationToken cancellationToken)
    {
        var endpoint = $"{_options.IndexName}/_doc/{document.TransactionId}";
        var response = await httpClient.PutAsJsonAsync(endpoint, document, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            logger.LogDebug("Indexed transaction {ExternalTransactionId} into Elasticsearch index {IndexName}.", document.ExternalTransactionId, _options.IndexName);
            return;
        }

        logger.LogWarning("Failed to index transaction {ExternalTransactionId} in Elasticsearch. StatusCode: {StatusCode}", document.ExternalTransactionId, response.StatusCode);
    }
}
