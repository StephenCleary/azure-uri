using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Api;
public class Functions
{
    private readonly ILogger _logger;

    public Functions(ILogger<Functions> logger)
    {
        _logger = logger;
    }

    [Function(nameof(Redirect))]
    public async Task<HttpResponseData> Redirect(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{slug}")]
        HttpRequestData req, string slug)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.WriteString("Hi");
        return response;
    }
}