using Microsoft.Extensions.Hosting;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Api;
class Program
{
    static async Task Main(string[] args)
    {
        var credential = new DefaultAzureCredential();
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureAppConfiguration(config => 
                config.AddAzureKeyVault(new Uri(Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT")!), credential))
            .ConfigureServices((config, services) =>
            {
            })
        .Build();
        
        // await using (var scope = host.Services.CreateAsyncScope())
        // {
        //     var db = scope.ServiceProvider.GetRequiredService<TodoDb>();
        //     await db.Database.EnsureCreatedAsync();
        // }
        await host.RunAsync();
    }
}