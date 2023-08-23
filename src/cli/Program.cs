using Microsoft.Azure.Cosmos;
using Azure.Identity;
using System.CommandLine;
using System.Diagnostics;
using System.Text;

var envValues = LoadAzdEnvFile();
DotNetEnv.Env.LoadContents(envValues);

var rootCommand = new RootCommand("azure-uri cli");
var slugArgument = new Argument<string>("slug", "The short url.");
var urlArgument = new Argument<Uri>("url", "The target url.");
var setCommand = new Command("set", "Forward a short url to a target url.")
{
    slugArgument,
    urlArgument,
};
rootCommand.AddCommand(setCommand);
setCommand.SetHandler(async (slug, url) =>
{
    using var cosmosClient = new CosmosClient(
        accountEndpoint: Environment.GetEnvironmentVariable("AZURE_COSMOSDB_ENDPOINT"),
        tokenCredential: new DefaultAzureCredential());
    var slugsContainer = cosmosClient.GetContainer(Environment.GetEnvironmentVariable("AZURE_COSMOSDB_DATABASE_NAME"), "slugs");
    await slugsContainer.UpsertItemAsync(new SlugEntity(
        id: slug,
        slug: slug,
        url: url.ToString()
    ));
}, slugArgument, urlArgument);

return await rootCommand.InvokeAsync(args);

static string LoadAzdEnvFile()
{
    var sb = new StringBuilder();
    using var process = new Process();
    process.StartInfo.FileName = "azd";
    process.StartInfo.Arguments = "env get-values";
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.CreateNoWindow = true;
    process.StartInfo.RedirectStandardOutput = true;
    process.OutputDataReceived += (_, args) => sb.Append(args.Data + Environment.NewLine);
    process.Start();
    process.BeginOutputReadLine();
    process.WaitForExit();
    return sb.ToString();
}

#pragma warning disable IDE1006

public record SlugEntity(
    string id,
    string slug,
    string url);
