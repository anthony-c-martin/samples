using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO.Abstractions;
using Bicep.Core;
using Bicep.Core.Analyzers.Interfaces;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Configuration;
using Bicep.Core.Emit;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Text;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Providers;
using Bicep.Core.Utils;
using Bicep.IO.Abstraction;
using Bicep.IO.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Environment = Bicep.Core.Utils.Environment;
using Bicep.Core.SourceGraph;
using Bicep.Core.Extensions;
using Bicep.Core.Registry.Catalog.Implementation;

public class Program
{
    public static async Task Main()
    {
        var fileExplorer = new InMemoryFileExplorer();
        fileExplorer.GetFile(IOUri.FromLocalFilePath("/main.bicep")).Write("""
module mod 'module.bicep' = {
  name: 'module'
  params: {
    foo: 'foo'
  }
}
""");
        fileExplorer.GetFile(IOUri.FromLocalFilePath("/module.bicep")).Write("""
param foo string
output foo string = foo
""");
        
        var services = new ServiceCollection()
            .AddBicepCore(fileExplorer)
            .BuildServiceProvider();
        
        var template = await Compile(services, new Uri("file:///main.bicep"));
        Console.Write(template);
    }
    
    private static async Task<string> Compile(IServiceProvider services, Uri bicepUri)
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
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBicepCore(this IServiceCollection services, IFileExplorer fileExplorer) => services
        .AddSingleton<INamespaceProvider, NamespaceProvider>()
        .AddSingleton<IResourceTypeProviderFactory, ResourceTypeProviderFactory>()
        .AddSingleton<IContainerRegistryClientFactory, ContainerRegistryClientFactory>()
        .AddSingleton<ITemplateSpecRepositoryFactory, TemplateSpecRepositoryFactory>()
        .AddSingleton<IModuleDispatcher, ModuleDispatcher>()
        .AddSingleton<IArtifactRegistryProvider, DefaultArtifactRegistryProvider>()
        .AddSingleton<ITokenCredentialFactory, TokenCredentialFactory>()
        .AddSingleton<IFileResolver, FileResolver>()
        .AddSingleton<IEnvironment, Environment>()
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