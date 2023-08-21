using Microsoft.Extensions.Hosting;
using Azure.Identity;

namespace Api;
class Program
{
    static async Task Main(string[] args)
    {
        var credential = new DefaultAzureCredential();
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices((config, services) =>
            {
            })
        .Build();
        
        await host.RunAsync();
    }
}