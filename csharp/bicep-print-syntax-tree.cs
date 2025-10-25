#!/usr/bin/env dotnet

#:package Azure.Bicep.Core@0.38.33
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false

using System.IO.Abstractions;
using System.Text;
using Bicep.Core;
using Bicep.Core.Analyzers.Interfaces;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.AzureApi;
using Bicep.Core.Configuration;
using Bicep.Core.Emit;
using Bicep.Core.Extensions;
using Bicep.Core.Features;
using Bicep.Core.Parsing;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Catalog.Implementation;
using Bicep.Core.Semantics;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.SourceGraph;
using Bicep.Core.Syntax;
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

var compiler = services.GetRequiredService<BicepCompiler>();
var compilation = await compiler.CreateCompilation(IOUri.FromFilePath("/main.bicep"));

foreach (var model in compilation.GetAllModels().OfType<SemanticModel>())
{
    Console.WriteLine(model.SourceFile.Uri);
    var syntaxTree = model.SourceFile.ProgramSyntax;
    SyntaxWriter.WriteSyntax(syntaxTree, Console.Out);
    Console.WriteLine("");
}

public static class SyntaxWriter
{
    public static void WriteSyntax(SyntaxBase syntax, TextWriter writer)
    {
        var syntaxList = SyntaxCollectorVisitor.Build(syntax);
        var syntaxByParent = syntaxList.ToLookup(x => x.Parent);

        foreach (var element in syntaxList)
        {
            writer.WriteLine(GetSyntaxLoggingString(syntaxByParent, element));
        }
    }

    private static string GetSyntaxLoggingString(
        ILookup<SyntaxCollectorVisitor.SyntaxItem?, SyntaxCollectorVisitor.SyntaxItem> syntaxByParent,
        SyntaxCollectorVisitor.SyntaxItem syntax)
    {
        // Build a visual graph with lines to help understand the syntax hierarchy
        var graphPrefix = new StringBuilder();

        foreach (var ancestor in syntax.GetAncestors().Reverse().Skip(1))
        {
            var isLast = (ancestor.Depth > 0 && ancestor == syntaxByParent[ancestor.Parent].Last());
            graphPrefix.Append(isLast switch
            {
                true => "  ",
                _ => "| ",
            });
        }

        if (syntax.Depth > 0)
        {
            var isLast = syntax == syntaxByParent[syntax.Parent].Last();
            graphPrefix.Append(isLast switch
            {
                true => "└─",
                _ => "├─",
            });
        }

        return syntax.Syntax switch
        {
            Token token => $"{graphPrefix}Token({token.Type}) |{EscapeWhitespace(token.Text)}|",
            _ => $"{graphPrefix}{syntax.Syntax.GetType().Name}",
        };
    }

    private static string EscapeWhitespace(string input)
        => input
        .Replace("\r", "\\r")
        .Replace("\n", "\\n")
        .Replace("\t", "\\t");
}

public class SyntaxCollectorVisitor : CstVisitor
{
    public record SyntaxItem(SyntaxBase Syntax, SyntaxItem? Parent, int Depth)
    {
        public IEnumerable<SyntaxCollectorVisitor.SyntaxItem> GetAncestors()
        {
            var data = this;
            while (data.Parent is { } parent)
            {
                yield return parent;
                data = parent;
            }
        }
    }

    private readonly IList<SyntaxItem> syntaxList = new List<SyntaxItem>();
    private SyntaxItem? parent = null;
    private int depth = 0;

    private SyntaxCollectorVisitor()
    {
    }

    public static SyntaxItem[] Build(SyntaxBase syntax)
    {
        var visitor = new SyntaxCollectorVisitor();
        visitor.Visit(syntax);

        return [.. visitor.syntaxList];
    }

    protected override void VisitInternal(SyntaxBase syntax)
    {
        var syntaxItem = new SyntaxItem(Syntax: syntax, Parent: parent, Depth: depth);
        syntaxList.Add(syntaxItem);

        var prevParent = parent;
        parent = syntaxItem;
        depth++;
        base.VisitInternal(syntax);
        depth--;
        parent = prevParent;
    }
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