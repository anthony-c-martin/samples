#!/usr/bin/env dotnet

#:package Azure.Bicep.Types.Az@0.2.756

using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Az;

var loader = new AzTypeLoader();

var index = loader.LoadTypeIndex();
var allTypes = index.Resources;

Console.WriteLine($"VM types:\n{string.Join("\n", allTypes.Keys.Where(x => x.StartsWith("Microsoft.Compute/virtualMachines@")))}\n");

var vm = loader.LoadResourceType(allTypes["Microsoft.Compute/virtualMachines@2020-06-01"]);
var vmProps = (vm.Body.Type as ObjectType)!.Properties;

Console.WriteLine($"VM top-level properties:\n{string.Join("\n", vmProps.Select(x => x.Key))}\n");