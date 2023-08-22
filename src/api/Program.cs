using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Identity;

namespace Api;
class Program
{
    static async Task Main(string[] args)
    {
        using var cosmosClient = new CosmosClient(
            accountEndpoint: Environment.GetEnvironmentVariable("AZURE_COSMOSDB_ENDPOINT"),
            tokenCredential: new DefaultAzureCredential());
        var cosmosDatabase = cosmosClient.GetDatabase("AZURE_COSMOSDB_DATABASE_ID");

        var credential = new DefaultAzureCredential();
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices((config, services) =>
            {
                services.AddSingleton(cosmosDatabase);
            })
        .Build();
        
        await host.RunAsync();
    }
}