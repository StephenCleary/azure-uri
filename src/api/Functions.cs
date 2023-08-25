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
        var log = new LogEntity(
            id: Guid.NewGuid().ToString("N"),
            timestamp: DateTimeOffset.UtcNow,
            slug: slug,
            requestUrl: req.Url.ToString(),
            requestHeaders: req.Headers.ToString(),
            clientIpAddress: ClientIpAddress(),
            resultStatusCode: 0,
            resultUrl: "");

        var (slugLookup, statusCode) = await TryLookupSlugAsync();
        if (statusCode == HttpStatusCode.OK)
        {
            await _logsContainer.CreateItemAsync(
                log with {
                    resultStatusCode = (int)HttpStatusCode.Found,
                    resultUrl = slugLookup!.url,
                },
                partitionKey: new PartitionKey(slug),
                requestOptions: new() { EnableContentResponseOnWrite = false });

            var result = req.CreateResponse(HttpStatusCode.Found);
            result.Headers.Add("Location", slugLookup!.url);

            // No caching. For serious.
            result.Headers.Add("Cache-Control", "no-store, no-cache, private, max-age=0, s-maxage=0, must-revalidate, proxy-revalidate");
            result.Headers.Add("Pragma", "no-cache");
            result.Headers.Add("Expires", "Mon, 01 Jan 1990 00:00:00 GMT");
            
            return result;
        }
        if (statusCode == HttpStatusCode.NotFound)
        {
            await _logsContainer.CreateItemAsync(
                log with {
                    resultStatusCode = (int)HttpStatusCode.NotFound,
                },
                partitionKey: new PartitionKey(slug),
                requestOptions: new() { EnableContentResponseOnWrite = false });
            
            _logger.LogInformation("Slug lookup not found for slug {slug}", slug);
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        _logger.LogError("Slug lookup failed for slug {slug} with status code {statusCode}", slug, statusCode);
        return req.CreateResponse(HttpStatusCode.InternalServerError);

        string ClientIpAddress()
        {
            var headerValue = req.Headers.TryGetValues("X-Forwarded-For", out var values) ? values.FirstOrDefault()?.Split(',').FirstOrDefault()?.Split(':').FirstOrDefault() : "" ?? "";
            if (IPAddress.TryParse(headerValue, out _))
                return headerValue;
            return "";
        }

        async Task<(SlugEntity? Slug, HttpStatusCode Status)> TryLookupSlugAsync()
        {
            try
            {
                var slugLookupResponse = await _slugsContainer.ReadItemAsync<SlugEntity>(slug, new PartitionKey(slug));
                return (slugLookupResponse.Resource, slugLookupResponse.StatusCode);
            }
            catch (CosmosException ex)
            {
                return (null, ex.StatusCode);
            }
        }
    }
}