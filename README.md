A place to put my reusable code samples for sharing with people

## C# Samples

The majority of these samples can be run directly with `dotnet`, as long as you have .NET 10 installed. For example, run the following in the repo root:
```sh
dotnet csharp/arm-expression-evaluator.cs
```

### Bicep-related Samples
* [bicep-compile.cs](csharp/bicep-compile.cs): Shows how to programmatically compile Bicep files to ARM JSON templates using the Bicep Core library.
* [bicep-jsonrpc.cs](csharp/bicep-jsonrpc.cs): Example of using the Bicep JSON-RPC client to interact with Bicep compiler remotely for compilation tasks.
* [bicep-modify-api.cs](csharp/bicep-modify-api.cs): Illustrates how to parse and modify Bicep syntax trees programmatically using the Bicep Core parsing and rewriting APIs.
* [bicep-print-syntax-tree.cs](csharp/bicep-print-syntax-tree.cs): Demonstrates parsing Bicep files and printing their abstract syntax tree structure for debugging and analysis.
* [bicep-type-generation/](csharp/bicep-type-generation/): Project that converts JSON schema definitions into Bicep type definitions for custom resource providers.
* [quickstart-compile.cs](csharp/quickstart-compile.cs): Bulk compilation tool for compiling all Bicep files in the Azure Quickstart Templates repository.

### Azure Resource Type APIs
* [get-resource-schema.cs](csharp/get-resource-schema.cs): Shows how to retrieve and convert Azure resource type schemas to JSON Schema format using Bicep type libraries.
* [type-navigation.cs](csharp/type-navigation.cs): Demonstrates navigation through Azure resource type definitions to explore properties and structure.

### Other
* [arm-expression-evaluator.cs](csharp/arm-expression-evaluator.cs): Demonstrates how to evaluate ARM template expressions locally, including creating a custom `reverse()` function.
* [cli-boilerplate.cs](csharp/cli-boilerplate.cs): Template for creating command-line applications with argument parsing using CommandLineParser library.
