#!/usr/bin/env dotnet

#:package Azure.Bicep.RpcClient@0.38.33
#:property JsonSerializerIsReflectionEnabledByDefault=true

using Bicep.RpcClient;

var clientFactory = new BicepClientFactory(new HttpClient());

using var client = await clientFactory.DownloadAndInitialize(
    new BicepClientConfiguration
    {
        BicepVersion = "0.38.33",
    }, default);

var version = await client.GetVersion();
Console.WriteLine($"Bicep version: {version}");

var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.bicep");
File.WriteAllText(tempFile, """
param foo string
output foo string = foo
""");

var result = await client.Compile(new(tempFile));
Console.Write(result.Contents);