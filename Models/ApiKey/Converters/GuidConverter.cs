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