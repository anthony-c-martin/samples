#!/usr/bin/env dotnet

#:package Azure.Bicep.Core@0.39.26
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false

using Bicep.Core;
using Bicep.Core.Extensions;
using Bicep.Core.Text;
using Bicep.IO.Abstraction;
using Bicep.IO.InMemory;
using Microsoft.Extensions.DependencyInjection;

var fileExplorer = new InMemoryFileExplorer();
fileExplorer.GetFile(IOUri.FromFilePath("/main.bicep")).Write("""
module mod 'module.bicep' = {
  name: 'module'
  params: {
    foo: 'foo'
  }
}
""");
fileExplorer.GetFile(IOUri.FromFilePath("/module.bicep")).Write("""
param foo string
output foo string = foo
""");

var services = new ServiceCollection()
    .AddSingleton<IFileExplorer>(fileExplorer)
    .AddBicepCore()
    .BuildServiceProvider();

var compiler = services.GetRequiredService<BicepCompiler>();
var compilation = await compiler.CreateCompilation(IOUri.FromFilePath("/main.bicep"));

var result = compilation.Emitter.Template();

foreach (var (file, diagnostics) in result.Diagnostics)
foreach (var diagnostic in diagnostics)
{
    (var line, var character) = TextCoordinateConverter.GetPosition(file.LineStarts, diagnostic.Span.Position);
    Console.WriteLine($"{file.Uri.LocalPath}({line + 1},{character + 1}) : {diagnostic.Level} {diagnostic.Code}: {diagnostic.Message}");
}

if (!result.Success)
{
    throw new InvalidOperationException("Compilation failed!");
}

Console.Write(result.Template);