#!/usr/bin/env dotnet

#:package Azure.Identity@1.17.0
#:package ModelContextProtocol@0.4.0-preview.1

using Azure.Identity;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

var vscodeAppId = "aebc6443-996d-45c2-90f0-388ff96faa56";
var tenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47"; // Microsoft Tenant
var mcpAuthScope = "api://22bfbae3-f4e7-485f-be43-8cee15065084/MCP.Access";

InteractiveBrowserCredentialOptions options = new()
{
    ClientId = vscodeAppId,
    TenantId = tenantId,
};
InteractiveBrowserCredential credential = new(options); 
var authResult = await credential.GetTokenAsync(new([mcpAuthScope]));

await using var client = await McpClient.CreateAsync(new HttpClientTransport(new()
{
    Endpoint = new Uri("https://mcp.management.azure.com"),
    TransportMode = HttpTransportMode.StreamableHttp,
    AdditionalHeaders = new Dictionary<string, string>
    {
        ["Authorization"] = $"Bearer {authResult.Token}",
    },
}));

var tools = await client.ListToolsAsync();

var response = await client.CallToolAsync("execute_query", new Dictionary<string, object?>
{
    ["query"] = "Resources | where type =~ 'Microsoft.Compute/virtualMachines' | summarize count()",
});

var responseObj = JsonDocument.Parse((response.Content[0] as TextContentBlock)!.Text);
Console.WriteLine($"VM Count: {responseObj.RootElement.GetProperty("results").GetProperty("data")[0].GetProperty("count_")}");