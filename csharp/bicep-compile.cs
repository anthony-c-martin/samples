#!/usr/bin/env dotnet

#:package Azure.Bicep.Core@0.38.33
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false

using System.IO.Abstractions;
using Bicep.Core;
using Bicep.Core.Analyzers.Interfaces;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.AzureApi;
using Bicep.Core.Configuration;
using Bicep.Core.Emit;
using Bicep.Core.Extensions;
using Bicep.Core.Features;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Catalog.Implementation;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.SourceGraph;
using Bicep.Core.Text;
using Bicep.Core.TypeSystem.Providers;
using Bicep.Core.Utils;
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
    .AddBicepCore(fileExplorer)
    .BuildServiceProvider();

var template = await Compile(services, IOUri.FromFilePath("/main.bicep"));
Console.Write(template);

static async Task<string> Compile(IServiceProvider services, IOUri bicepUri)
{
    var compiler = services.GetRequiredService<BicepCompiler>();
    var compilation = await compiler.CreateCompilation(bicepUri);
    var model = compilation.GetEntrypointSemanticModel();

    using var stream = new MemoryStream();
    var emitter = new TemplateEmitter(model);
    var emitResult = emitter.Emit(stream);

    foreach (var (file, diagnostics) in compilation.GetAllDiagnosticsByBicepFile())
    foreach (var diagnostic in diagnostics)
    {
        (var line, var character) = TextCoordinateConverter.GetPosition(file.LineStarts, diagnostic.Span.Position);
        Console.WriteLine($"{file.Uri.LocalPath}({line + 1},{character + 1}) : {diagnostic.Level} {diagnostic.Code}: {diagnostic.Message}");
    }

    if (emitResult.Status == EmitStatus.Failed)
    {            
        throw new InvalidOperationException("Compilation failed!");
    }

    stream.Position = 0;
    return await new StreamReader(stream).ReadToEndAsync();
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBicepCore(this IServiceCollection services, IFileExplorer fileExplorer) => services
        .AddSingleton<INamespaceProvider, NamespaceProvider>()
        .AddSingleton<IResourceTypeProviderFactory, ResourceTypeProviderFactory>()
        .AddSingleton<IContainerRegistryClientFactory, ContainerRegistryClientFactory>()
        .AddSingleton<ITemplateSpecRepositoryFactory, TemplateSpecRepositoryFactory>()
        .AddSingleton<IArmClientProvider, ArmClientProvider>()
        .AddSingleton<IModuleDispatcher, ModuleDispatcher>()
        .AddSingleton<IArtifactRegistryProvider, DefaultArtifactRegistryProvider>()
        .AddSingleton<ITokenCredentialFactory, TokenCredentialFactory>()
        .AddSingleton<IEnvironment, Bicep.Core.Utils.Environment>()
        .AddSingleton<IFileSystem, FileSystem>()
        .AddSingleton<IFileExplorer>(fileExplorer)
        .AddSingleton<IAuxiliaryFileCache, AuxiliaryFileCache>()
        .AddSingleton<IConfigurationManager, ConfigurationManager>()
        .AddSingleton<IBicepAnalyzer, LinterAnalyzer>()
        .AddSingleton<IFeatureProviderFactory, FeatureProviderFactory>()
        .AddSingleton<ILinterRulesProvider, LinterRulesProvider>()
        .AddSingleton<ISourceFileFactory, SourceFileFactory>()
        .AddRegistryCatalogServices()
        .AddSingleton<BicepCompiler>();
}