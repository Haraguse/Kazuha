using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;

namespace Luminalium.Wps;

public sealed class WpsProtocol
{
    private const string CommonSchemaName = "common.schema.json";
    private static readonly Uri SchemaBaseUri = new("https://luminalium.local/wps/protocol/");
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private readonly IReadOnlyDictionary<string, JsonSchema> _messageSchemas;
    private readonly EvaluationOptions _evaluationOptions = new()
    {
        OutputFormat = OutputFormat.List,
    };

    public WpsProtocol()
        : this(LoadEmbeddedSchemas())
    {
    }

    public WpsProtocol(IReadOnlyDictionary<string, string> schemaTexts)
    {
        ArgumentNullException.ThrowIfNull(schemaTexts);

        var parsedSchemas = BuildSchemas(schemaTexts);
        _messageSchemas = BuildMessageSchemas(schemaTexts, parsedSchemas);
        MessageTypes = _messageSchemas.Keys.Order(StringComparer.Ordinal).ToArray();
    }

    public IReadOnlyList<string> MessageTypes { get; }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The protocol API mirrors the legacy instance codec.")]
    public string NewMessageId() => Guid.NewGuid().ToString("N");

    public WpsMessage MakeMessage(
        string messageType,
        object? payload = null,
        string? messageId = null,
        long? ts = null)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        var message = new WpsMessage(
            messageType,
            messageId ?? NewMessageId(),
            ts ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            JsonSerializer.SerializeToElement(payload ?? new Dictionary<string, object?>(), SerializerOptions).Clone());
        Validate(message);
        return message;
    }

    public string Encode(
        string messageType,
        object? payload = null,
        string? messageId = null,
        long? ts = null) =>
        Encode(MakeMessage(messageType, payload, messageId, ts));

    public string Encode(WpsMessage message)
    {
        Validate(message);
        var envelope = new EnvelopeDto(message.MessageType, message.MessageId, message.Ts, message.Payload);
        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }

    public WpsMessage Decode(string rawText)
    {
        ArgumentNullException.ThrowIfNull(rawText);

        using var document = ParseDocument(rawText);
        var root = document.RootElement;
        if (root.ValueKind is not JsonValueKind.Object)
        {
            throw new WpsProtocolException("Message must be a JSON object");
        }

        ValidateElement(root);
        var messageId = root.TryGetProperty("message_id", out var messageIdElement)
            && messageIdElement.ValueKind is JsonValueKind.String
                ? messageIdElement.GetString() ?? string.Empty
                : string.Empty;
        var ts = root.TryGetProperty("ts", out var tsElement)
            && tsElement.ValueKind is JsonValueKind.Number
            && tsElement.TryGetInt64(out var parsedTs)
                ? parsedTs
                : 0;

        return new WpsMessage(
            root.GetProperty("message_type").GetString()!,
            messageId,
            ts,
            root.GetProperty("payload").Clone());
    }

    public void Validate(WpsMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        using var document = JsonDocument.Parse(EncodeUnchecked(message));
        ValidateElement(document.RootElement);
    }

    private static Dictionary<string, string> LoadEmbeddedSchemas()
    {
        var assembly = typeof(WpsProtocol).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Protocol.", StringComparison.Ordinal)
                && name.EndsWith(".schema.json", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (resourceNames.Length is 0)
        {
            throw new WpsProtocolException("No embedded WPS bridge protocol schemas were found");
        }

        var schemas = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var resourceName in resourceNames)
        {
            var fileName = resourceName[(resourceName.LastIndexOf(".Protocol.", StringComparison.Ordinal) + ".Protocol.".Length)..];
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new WpsProtocolException($"Failed to load embedded schema {resourceName}");
            using var reader = new StreamReader(stream);
            schemas[fileName] = reader.ReadToEnd();
        }

        return schemas;
    }

    private static Dictionary<string, JsonSchema> BuildSchemas(IReadOnlyDictionary<string, string> schemaTexts)
    {
        if (schemaTexts.Count is 0)
        {
            throw new WpsProtocolException("No WPS bridge protocol schemas were provided");
        }

        var registry = new SchemaRegistry();
        var buildOptions = new BuildOptions
        {
            Dialect = Dialect.Draft202012,
            SchemaRegistry = registry,
        };
        var schemas = new Dictionary<string, JsonSchema>(StringComparer.Ordinal);

        foreach (var (name, text) in schemaTexts.OrderBy(item => SchemaLoadOrder(item.Key), StringComparer.Ordinal))
        {
            JsonSchema schema;
            try
            {
                schema = JsonSchema.FromText(text, buildOptions, SchemaUri(name));
            }
            catch (JsonException exc)
            {
                throw new WpsProtocolException($"Failed to load schema {name}: {exc.Message}");
            }

            registry.Register(SchemaUri(name), schema);
            schemas[name] = schema;
        }

        return schemas;
    }

    private static Dictionary<string, JsonSchema> BuildMessageSchemas(
        IReadOnlyDictionary<string, string> schemaTexts,
        Dictionary<string, JsonSchema> parsedSchemas)
    {
        var messageSchemas = new Dictionary<string, JsonSchema>(StringComparer.Ordinal);
        foreach (var (name, text) in schemaTexts.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            if (string.Equals(name, CommonSchemaName, StringComparison.Ordinal))
            {
                continue;
            }

            var messageTypes = DiscoverMessageTypes(name, text).Order(StringComparer.Ordinal).Distinct(StringComparer.Ordinal);
            foreach (var messageType in messageTypes)
            {
                if (messageSchemas.ContainsKey(messageType))
                {
                    throw new WpsProtocolException($"Duplicate schema for message_type {messageType}");
                }

                messageSchemas[messageType] = parsedSchemas[name];
            }
        }

        if (messageSchemas.Count is 0)
        {
            throw new WpsProtocolException("No message schemas could be discovered");
        }

        return messageSchemas;
    }

    private static IEnumerable<string> DiscoverMessageTypes(string schemaName, string schemaText)
    {
        using var document = ParseSchemaDocument(schemaName, schemaText);
        foreach (var messageType in EnumerateMessageTypes(document.RootElement))
        {
            yield return messageType;
        }
    }

    private static JsonDocument ParseSchemaDocument(string schemaName, string schemaText)
    {
        try
        {
            var document = JsonDocument.Parse(schemaText);
            if (document.RootElement.ValueKind is not JsonValueKind.Object)
            {
                throw new WpsProtocolException($"Schema must be a JSON object: {schemaName}");
            }

            return document;
        }
        catch (JsonException exc)
        {
            throw new WpsProtocolException($"Failed to parse schema {schemaName}: {exc.Message}");
        }
    }

    private static IEnumerable<string> EnumerateMessageTypes(JsonElement node)
    {
        if (node.ValueKind is JsonValueKind.Object)
        {
            if (node.TryGetProperty("properties", out var properties)
                && properties.ValueKind is JsonValueKind.Object
                && properties.TryGetProperty("message_type", out var messageType)
                && messageType.ValueKind is JsonValueKind.Object)
            {
                if (messageType.TryGetProperty("const", out var constElement)
                    && constElement.ValueKind is JsonValueKind.String)
                {
                    yield return constElement.GetString()!;
                }

                if (messageType.TryGetProperty("enum", out var enumElement)
                    && enumElement.ValueKind is JsonValueKind.Array)
                {
                    foreach (var item in enumElement.EnumerateArray())
                    {
                        if (item.ValueKind is JsonValueKind.String)
                        {
                            yield return item.GetString()!;
                        }
                    }
                }
            }

            foreach (var property in node.EnumerateObject())
            {
                foreach (var nestedMessageType in EnumerateMessageTypes(property.Value))
                {
                    yield return nestedMessageType;
                }
            }
        }
        else if (node.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
            {
                foreach (var nestedMessageType in EnumerateMessageTypes(item))
                {
                    yield return nestedMessageType;
                }
            }
        }
    }

    private static JsonDocument ParseDocument(string rawText)
    {
        try
        {
            return JsonDocument.Parse(rawText);
        }
        catch (Exception exc) when (exc is JsonException or NotSupportedException)
        {
            throw new WpsProtocolException($"Invalid JSON: {exc.Message}");
        }
    }

    private void ValidateElement(JsonElement root)
    {
        if (!root.TryGetProperty("message_type", out var messageTypeElement)
            || messageTypeElement.ValueKind is not JsonValueKind.String)
        {
            throw new WpsProtocolException("Message is missing string field 'message_type'");
        }

        var messageType = messageTypeElement.GetString()!;
        if (!_messageSchemas.TryGetValue(messageType, out var schema))
        {
            throw new WpsProtocolException($"Unknown message_type: {messageType}");
        }

        var results = schema.Evaluate(root, _evaluationOptions);
        if (!results.IsValid)
        {
            throw new WpsProtocolException(FormatErrors(results));
        }
    }

    private static string FormatErrors(EvaluationResults results)
    {
        var issues = FlattenResults(results)
            .Where(result => !result.IsValid && result.Errors is not null && result.Errors.Count > 0)
            .SelectMany(result => result.Errors!.Values.Select(message => new ValidationIssue(
                PointerToJsonPath(result.InstanceLocation.ToString()),
                message)))
            .OrderBy(issue => issue.Path, StringComparer.Ordinal)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ToArray();

        var parts = issues.Take(5)
            .Select(issue => $"{issue.Path}: {issue.Message}")
            .ToList();
        if (issues.Length > 5)
        {
            parts.Add($"... {issues.Length - 5} more validation errors");
        }

        return string.Join("; ", parts);
    }

    private static IEnumerable<EvaluationResults> FlattenResults(EvaluationResults results)
    {
        yield return results;
        foreach (var detail in results.Details ?? [])
        {
            foreach (var child in FlattenResults(detail))
            {
                yield return child;
            }
        }
    }

    private static string PointerToJsonPath(string pointer)
    {
        if (string.IsNullOrEmpty(pointer))
        {
            return "$";
        }

        var path = "$";
        foreach (var rawSegment in pointer.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var segment = rawSegment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
            if (int.TryParse(segment, out _))
            {
                path += $"[{segment}]";
            }
            else if (IsJsonPathIdentifier(segment))
            {
                path += "." + segment;
            }
            else
            {
                path += "['" + segment.Replace("'", "\\'", StringComparison.Ordinal) + "']";
            }
        }

        return path;
    }

    private static bool IsJsonPathIdentifier(string value) =>
        value.Length > 0
        && (char.IsLetter(value[0]) || value[0] is '_')
        && value.All(character => char.IsLetterOrDigit(character) || character is '_');

    private static Uri SchemaUri(string schemaName) => new(SchemaBaseUri, schemaName);

    private static string SchemaLoadOrder(string schemaName) =>
        string.Equals(schemaName, CommonSchemaName, StringComparison.Ordinal)
            ? string.Empty
            : schemaName;

    private static string EncodeUnchecked(WpsMessage message)
    {
        var envelope = new EnvelopeDto(message.MessageType, message.MessageId, message.Ts, message.Payload);
        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }

    private sealed record EnvelopeDto(
        [property: JsonPropertyName("message_type")] string MessageType,
        [property: JsonPropertyName("message_id")] string MessageId,
        [property: JsonPropertyName("ts")] long Ts,
        [property: JsonPropertyName("payload")] JsonElement Payload);

    private sealed record ValidationIssue(string Path, string Message);
}
