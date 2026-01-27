A place to put my reusable code samples for sharing with people

## Bicep Samples

Examples using Bicep's local deployment feature (`targetScope = 'local'`):
* [local-deploy-azure/](bicep/local-deploy-azure/): Demonstrates local deployment targeting Azure resources.
* [local-deploy-github-oidc/](bicep/local-deploy-github-oidc/): Sets up GitHub OIDC federation with Azure for secure CI/CD authentication.
* [local-deploy-kubernetes/](bicep/local-deploy-kubernetes/): Deploys applications to Kubernetes using local Bicep deployment.

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
* [quickstart-markdown.cs](csharp/quickstart-markdown.cs): Generates markdown documentation from Bicep files, including descriptions, deployment graphs, parameters, and outputs with GitHub-compatible code links.

### Azure Resource Type APIs
* [get-resource-schema.cs](csharp/get-resource-schema.cs): Shows how to retrieve and convert Azure resource type schemas to JSON Schema format using Bicep type libraries.
* [type-navigation.cs](csharp/type-navigation.cs): Demonstrates navigation through Azure resource type definitions to explore properties and structure.

### Other
* [arm-custom-expression.cs](csharp/arm-custom-expression.cs): Demonstrates how to create and register custom ARM template expression functions, including a custom `reverse()` function implementation.
* [arm-eval-expression.cs](csharp/arm-eval-expression.cs): Simple example of evaluating ARM template expressions using the built-in expression evaluator.
* [arm-mcp-server.cs](csharp/arm-mcp-server.cs): Shows how to connect to Azure's ARM Model Context Protocol (MCP) server using interactive browser authentication to execute Azure Resource Graph queries.
* [cli-boilerplate.cs](csharp/cli-boilerplate.cs): Template for creating command-line applications with argument parsing using CommandLineParser library.

## Other Samples

* [armguid/](other/armguid/): Reference implementations of the ARM `guid()` function in C#, Python, and TypeScript for generating deterministic UUIDs.