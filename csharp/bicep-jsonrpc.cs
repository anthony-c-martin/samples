#!/usr/bin/env dotnet

using System.Diagnostics;

var request = """
{
  "jsonrpc": "2.0",
  "id": 0,
  "method": "bicep/version",
  "params": {}
}
""";

using var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "bicep",
        Arguments = "jsonrpc",
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = false,
        UseShellExecute = false,
        CreateNoWindow = true
    },
};

process.Start();

var rawRequest = $"Content-Length: {System.Text.Encoding.UTF8.GetByteCount(request)}\r\n\r\n{request}\r\n\r\n";

await process.StandardInput.WriteAsync(rawRequest);
var rawResponse = await process.StandardOutput.ReadToEndAsync();

Console.WriteLine($"SENT: {rawRequest}");
Console.WriteLine($"RCVD: {rawResponse}");
