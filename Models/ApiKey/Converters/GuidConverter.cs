using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FeeNominalService.Models.ApiKey.Converters;

/// <summary>
/// Custom JSON converter for handling GUID serialization and deserialization
/// </summary>
public class GuidConverter : JsonConverter<Guid>
{
    /// <summary>
    /// Reads a GUID from JSON
    /// </summary>
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return Guid.Empty;
        }

        return Guid.Parse(value);
    }

    /// <summary>
    /// Writes a GUID to JSON
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("D"));
    }
}

/// <summary>
/// Custom JSON converter for handling nullable GUID serialization and deserialization
/// </summary>
public class NullableGuidConverter : JsonConverter<Guid?>
{
    /// <summary>
    /// Reads a nullable GUID from JSON
    /// </summary>
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return Guid.Parse(value);
    }

    /// <summary>
    /// Writes a nullable GUID to JSON
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("D"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
} 