#!/usr/bin/env dotnet

#:package Azure.Bicep.RpcClient@0.39.26
#:property JsonSerializerIsReflectionEnabledByDefault=true

using Bicep.RpcClient;

var clientFactory = new BicepClientFactory(new HttpClient());
using var client = await clientFactory.DownloadAndInitialize(new() { BicepVersion = "0.39.26" }, default);

foreach (var file in Directory.GetFileSystemEntries("/Users/ant/Code/azure-quickstart-templates", "*.bicep", SearchOption.AllDirectories))
{
    var result = await client.Compile(new(file));
    File.WriteAllText(Path.ChangeExtension(file, ".json"), result.Contents);
    Console.WriteLine($"Compiled {file}");
    foreach (var diag in result.Diagnostics)
    {
        Console.WriteLine($"  \e[0;33m{diag.Source}({diag.Range.Start.Line + 1},{diag.Range.Start.Char + 1}) : {diag.Level} {diag.Code}: {diag.Message}\e[0m");
    }
}