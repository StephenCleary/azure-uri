using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;

namespace Api;
public class Functions
{
    private readonly ILogger _logger;
    private readonly Container _slugsContainer;
    private readonly Container _logsContainer;

    public Functions(ILogger<Functions> logger, Database cosmosDatabase)
    {
        _logger = logger;
        _slugsContainer = cosmosDatabase.GetContainer("slugs");
        _logsContainer = cosmosDatabase.GetContainer("logs");
    }

    [Function(nameof(Redirect))]
    public async Task<HttpResponseData> Redirect(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{slug}")]
        HttpRequestData req, string slug)
    {
        var (slugLookup, statusCode) = await TryLookupSlugAsync();
        if (statusCode == HttpStatusCode.OK)
        {
            var result = req.CreateResponse(HttpStatusCode.Found);
            result.Headers.Add("Location", slugLookup!.uri);
            return result;
        }
        if (statusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Slug lookup not found for slug {slug}", slug);
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        _logger.LogError("Slug lookup failed for slug {slug} with status code {statusCode}", slug, statusCode);
        return req.CreateResponse(HttpStatusCode.InternalServerError);

        async Task<(Slug? Slug, HttpStatusCode Status)> TryLookupSlugAsync()
        {
            try
            {
                var slugLookupResponse = await _slugsContainer.ReadItemAsync<Slug>("", new PartitionKey(slug));
                return (slugLookupResponse.Resource, slugLookupResponse.StatusCode);
            }
            catch (CosmosException ex)
            {
                return (null, ex.StatusCode);
            }
        }
    }
}