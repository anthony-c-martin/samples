using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Az;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

public class Program
{
    public static void Main()
    {
        var loader = new AzTypeLoader();

        var index = loader.LoadTypeIndex();
        var allTypes = index.Resources;

        var vm = loader.LoadResourceType(allTypes["Microsoft.KeyVault/managedHSMs@2023-07-01"]);

        Console.WriteLine(ToJsonSchemaRecursive(vm.Body.Type).ToString());
    }

    private static JsonObject ToJsonSchemaRecursive(TypeBase type)
    {
        // TODO handle cycles!
        RuntimeHelpers.EnsureSufficientExecutionStack();

        switch (type)
        {
            case StringLiteralType _:
            case StringType _:
            case UnionType _:
                return new JsonObject
                {
                    ["type"] = "string"
                };
            case IntegerType _:
                return new JsonObject
                {
                    ["type"] = "number"
                };
            case BooleanType _:
                return new JsonObject
                {
                    ["type"] = "boolean"
                };
            case ArrayType arrayType:
                return new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = ToJsonSchemaRecursive(arrayType.ItemType.Type)
                };
            case ObjectType objectType:
                var writableProps = objectType.Properties.Where(x => !x.Value.Flags.HasFlag(ObjectTypePropertyFlags.ReadOnly));
                var requiredProps = writableProps.Where(x => x.Value.Flags.HasFlag(ObjectTypePropertyFlags.Required));

                var properties = writableProps.Select(x => new KeyValuePair<string, JsonNode?>(x.Key, ToJsonSchemaRecursive(x.Value.Type.Type)));
                return new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject(properties),
                    ["required"] = new JsonArray([.. requiredProps.Select(x => JsonValue.Create(x.Key))]),
                };
            default:
                // TODO discriminated object support
                throw new NotImplementedException($"{type.GetType()}");
        }
    }
}