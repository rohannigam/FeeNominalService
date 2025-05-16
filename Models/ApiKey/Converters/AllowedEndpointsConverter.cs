using System.Text.Json;
using System.Text.Json.Serialization;

namespace FeeNominalService.Models.ApiKey.Converters;

public class AllowedEndpointsConverter : JsonConverter<string[]>
{
    public override string[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return string.IsNullOrEmpty(value) 
                ? Array.Empty<string>() 
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(e => e.Trim())
                       .ToArray();
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<string>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    list.Add(reader.GetString()!);
                }
            }
            return list.ToArray();
        }

        throw new JsonException("Expected string or array for AllowedEndpoints");
    }

    public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item);
        }
        writer.WriteEndArray();
    }
} 