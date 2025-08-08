using System.Text.Json;
using System.Text.Json.Serialization;

namespace FeeNominalService.Models.ApiKey.Converters;

public class AllowedEndpointsConverter : JsonConverter<string[]>
{
    public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var endpoints = new List<string>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                var endpoint = reader.GetString();
                if (!string.IsNullOrEmpty(endpoint))
                {
                    // Validate endpoint format
                    if (!IsValidEndpoint(endpoint))
                    {
                        throw new JsonException($"Invalid endpoint format: {endpoint}. Endpoints must start with / and can end with * for wildcard.");
                    }
                    endpoints.Add(endpoint);
                }
            }
            return endpoints.ToArray();
        }
        throw new JsonException("Expected array for AllowedEndpoints");
    }

    private bool IsValidEndpoint(string endpoint)
    {
        // Must start with /
        if (!endpoint.StartsWith("/"))
            return false;

        // If it contains *, it must be at the end
        if (endpoint.Contains("*") && !endpoint.EndsWith("*"))
            return false;

        // No consecutive slashes
        if (endpoint.Contains("//"))
            return false;

        return true;
    }

    public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var endpoint in value)
        {
            writer.WriteStringValue(endpoint);
        }
        writer.WriteEndArray();
    }
} 