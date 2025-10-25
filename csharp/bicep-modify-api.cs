#!/usr/bin/env dotnet

#:package Azure.Bicep.Core@0.38.33
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false

using Bicep.Core.Parsing;
using Bicep.Core.PrettyPrintV2;
using Bicep.Core.Syntax;
using Bicep.Core.Syntax.Rewriters;

var input = """
resource functionAppAppSettings 'Microsoft.Web/sites/config@2018-02-01' = {
  parent: functionApp
  name: 'appsettings'
  properties: {
    SETTING_1_FROM_US: 'some value'
    SETTING_2_FROM_US: 'some value'
    CUSTOMIZED_SETTING_1: 'some value'
  }  
}
""";

var syntaxTree = new Parser(input).Program();

syntaxTree = CallbackRewriter.Rewrite(syntaxTree, syntax =>
{
    if (syntax is ResourceDeclarationSyntax resource &&
        resource.Name.NameEquals("functionAppAppSettings") &&
        resource.Value is ObjectSyntax objectSyntax)
    {
        return new ResourceDeclarationSyntax(
            resource.LeadingNodes,
            resource.Keyword,
            resource.Name,
            resource.Type,
            resource.ExistingKeyword,
            resource.Assignment,
            resource.Newlines,
            new ObjectSyntax(objectSyntax.OpenBrace, objectSyntax.Properties.Select(property =>
            {
                if (property.HasPropertyName("properties") && property.Value is ObjectSyntax propertiesObject)
                {
                    var newChildren = propertiesObject.Children
                        .Append(SyntaxFactory.CreateObjectProperty(
                            "SETTING_3_FROM_US",
                            SyntaxFactory.CreateStringLiteral("some value")));

                    return new ObjectPropertySyntax(
                        property.Key,
                        property.Colon,
                        new ObjectSyntax(
                            propertiesObject.OpenBrace,
                            newChildren,
                            propertiesObject.CloseBrace));
                }

                return property;
            }), objectSyntax.CloseBrace));
    }

    return syntax;
});

var output = PrettyPrinterV2.PrintValid(syntaxTree, PrettyPrinterV2Options.Default);

Console.Write(output);