#!/usr/bin/env dotnet

#:package Azure.Bicep.Core@0.39.26
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false

using System.Text;
using Bicep.Core;
using Bicep.Core.Extensions;
using Bicep.Core.Parsing;
using Bicep.Core.Semantics;
using Bicep.Core.Syntax;
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