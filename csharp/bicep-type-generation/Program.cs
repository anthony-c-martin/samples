using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Serialization;
using System.Text;
using System.Text.Json.Nodes;

class Program
{
    static void Main(string[] args)
    {
        var factory = new TypeFactory([]);
        var jsonContent = JsonNode.Parse(File.ReadAllText("input.json"))!;

        var typeName = jsonContent["type"]!.ToString();
        var version = jsonContent["version"]!.ToString();

        var bodyType = Convert(factory, jsonContent["schema"]!["embedded"]!);

        var resourceType = factory.Create(() => new ResourceType(
            $"{typeName}@{version}",
            ScopeType.Unknown,
            null,
            factory.GetReference(bodyType),
            ResourceFlags.None,
            null));

        TypeSettings settings = new(
            name: "Testing",
            version: "0.0.1",
            isSingleton: false,
            configurationType: null!);

        var resourceTypes = new[] {
            resourceType,
        };

        var index = new TypeIndex(
            resourceTypes.ToDictionary(x => x.Name, x => new CrossFileTypeReference("types.json", factory.GetIndex(x))),
            new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<CrossFileTypeReference>>>(),
            settings,
            null);

        var files = new Dictionary<string, string>{
            ["index.json"] = GetString(stream => TypeSerializer.SerializeIndex(stream, index)),
            ["types.json"] = GetString(stream => TypeSerializer.Serialize(stream, factory.GetTypes())),
        };

        foreach (var file in files)
        {
            File.WriteAllText($"output/{file.Key}", file.Value);
        }
    }

    static TypeBase Convert(TypeFactory factory, JsonNode schema)
    {
        switch (schema["type"]!.ToString())
        {
            case "object":
                var title = schema["title"]?.ToString() ?? "object";
                var properties = (schema["properties"] as JsonObject)!
                    .ToDictionary(x => x.Key, x => new ObjectTypeProperty(factory.GetReference(Convert(factory, x.Value!)), ObjectTypePropertyFlags.None, null));

                return factory.Create(() => new ObjectType(title, properties, null));
            case "array":
                var itemsType = Convert(factory, schema["items"]!);
                return factory.Create(() => new ArrayType(factory.GetReference(itemsType)));
            case "integer":
            case "number":
                return factory.Create(() => new IntegerType());
            case "string":
                return factory.Create(() => new StringType());
            case "boolean":
                return factory.Create(() => new BooleanType());
            default:
                throw new NotSupportedException($"Unsupported type: {schema["type"]}");
        }
    }

    static string GetString(Action<Stream> streamWriteFunc)
    {
        using var memoryStream = new MemoryStream();
        streamWriteFunc(memoryStream);

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }
}