#!/usr/bin/env dotnet

#:package Azure.Bicep.RpcClient@0.42.1
#:property JsonSerializerIsReflectionEnabledByDefault=true

using Bicep.RpcClient;

IBicepClientFactory clientFactory = new BicepClientFactory();
using var client = await clientFactory.Initialize(new() { BicepVersion = "0.42.1" });
    
var version = await client.GetVersion();
Console.WriteLine($"Bicep version: {version}");

var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.bicep");
File.WriteAllText(tempFile, """
param foo string
output foo string = foo
""");

var result = await client.Compile(new(tempFile));
Console.Write(result.Contents);
