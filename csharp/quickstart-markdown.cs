#!/usr/bin/env dotnet

#:package Azure.Bicep.RpcClient@0.39.26
#:property JsonSerializerIsReflectionEnabledByDefault=true

using System.Text;
using Bicep.RpcClient;
using Bicep.RpcClient.Models;

var clientFactory = new BicepClientFactory(new HttpClient());
using var client = await clientFactory.DownloadAndInitialize(new() { BicepVersion = "0.39.26" }, default);

foreach (var file in Directory.GetFileSystemEntries("/Users/ant/Code/azure-quickstart-templates", "*.bicep", SearchOption.AllDirectories))
{
    var metadata = await client.GetMetadata(new(file));
    var graph = await client.GetDeploymentGraph(new(file));
    
    var markdown = FormatMarkdown(metadata, graph, Path.GetFileName(file));
    await File.WriteAllTextAsync(Path.ChangeExtension(file, ".md"), markdown);
}

string FormatMarkdown(GetMetadataResponse metadata, GetDeploymentGraphResponse graph, string fileName)
{
    var sb = new StringBuilder();
    
    var description = metadata.Metadata.FirstOrDefault(x => x.Name == "description")?.Value;
    if (!string.IsNullOrEmpty(description))
    {
        sb.AppendLine("## Description");
        sb.AppendLine();
        sb.AppendLine(description);
        sb.AppendLine();
    }
    
    if (graph.Nodes.Any())
    {
        sb.AppendLine("## Graph");
        sb.AppendLine();
        sb.AppendLine("```mermaid");
        sb.AppendLine("flowchart LR;");
        
        foreach (var node in graph.Nodes)
        {
            var existingLabel = node.IsExisting ? " (existing)" : "";
            sb.AppendLine($"    {node.Name}[\"{node.Name}{existingLabel}");
            sb.AppendLine($"    {node.Type}\"]");
        }
        
        foreach (var edge in graph.Edges)
        {
            sb.AppendLine($"    {edge.Source}-->{edge.Target};");
        }
        
        sb.AppendLine("```");
        sb.AppendLine();
    }
    
    if (metadata.Parameters.Any())
    {
        sb.AppendLine("## Parameters");
        sb.AppendLine();
        sb.AppendLine("| Name | Type | Description |");
        sb.AppendLine("| -- | -- | -- |");
        
        foreach (var param in metadata.Parameters)
        {
            var (name, type, desc) = GetFormattedRow(param, fileName);
            sb.AppendLine($"| {name} | {type} | {desc} |");
        }
        
        sb.AppendLine();
    }
    
    if (metadata.Outputs.Any())
    {
        sb.AppendLine("## Outputs");
        sb.AppendLine();
        sb.AppendLine("| Name | Type | Description |");
        sb.AppendLine("| -- | -- | -- |");
        
        foreach (var output in metadata.Outputs)
        {
            var (name, type, desc) = GetFormattedRow(output, fileName);
            sb.AppendLine($"| {name} | {type} | {desc} |");
        }
        
        sb.AppendLine();
    }
    
    return sb.ToString();
}

(string name, string type, string description) GetFormattedRow(GetMetadataResponse.SymbolDefinition symbol, string fileName)
{
    var name = FormatCodeLink(symbol.Name, symbol.Range, fileName);
    var type = symbol.Type != null 
        ? FormatCodeLink(symbol.Type.Name, symbol.Type.Range, fileName) 
        : "";
    var description = symbol.Description ?? "";
    
    return (name, type, description);
}

string FormatCodeLink(string contents, Bicep.RpcClient.Models.Range? range, string fileName)
{
    if (range == null)
    {
        return $"`{contents}`";
    }
    
    var startLine = range.Start.Line + 1;
    var startChar = range.Start.Char + 1;
    var endLine = range.End.Line + 1;
    var endChar = range.End.Char + 1;
    
    return $"[`{contents}`](./{fileName}#L{startLine}C{startChar}-L{endLine}C{endChar})";
}