using Microsoft.Azure.Cosmos;
using Azure.Identity;
using System.CommandLine;
using System.Diagnostics;
using System.Text;
using CsvHelper;
using System.Globalization;

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

var exportFileParameter = new Argument<FileInfo>("csvfile", "The file to export to.");
var exportCommand = new Command("export", "Export all slugs to a file.")
{
    exportFileParameter,
};
exportCommand.SetHandler(async (FileInfo file) =>
{
    using var cosmosClient = new CosmosClient(
        accountEndpoint: Environment.GetEnvironmentVariable("AZURE_COSMOSDB_ENDPOINT"),
        tokenCredential: new DefaultAzureCredential());
    var slugsContainer = cosmosClient.GetContainer(Environment.GetEnvironmentVariable("AZURE_COSMOSDB_DATABASE_NAME"), "slugs");
    using var iterator = slugsContainer.GetItemQueryIterator<SlugEntity>();
    using var writer = new StreamWriter(file.FullName, append: false, Encoding.UTF8);
    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
    while (iterator.HasMoreResults)
    {
        var items = await iterator.ReadNextAsync();
        csv.WriteRecords(items.Select(item => new { item.slug, item.url }));
    }
}, exportFileParameter);

rootCommand.AddCommand(setCommand);
rootCommand.AddCommand(exportCommand);
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
