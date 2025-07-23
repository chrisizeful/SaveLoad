using Newtonsoft.Json;
using Godot;
using System;

namespace SaveLoad;

/// <summary>
/// De/serialize a <see cref="Variant"/> via its type and value.
/// </summary>
public class VariantConverter : JsonConverter<Variant>
{

    public override void WriteJson(JsonWriter writer, Variant value, JsonSerializer serializer)
    {
        // Serialize Variant to bytes and encode as base64 string
        var bytes = GD.VarToBytesWithObjects(value);
        writer.WriteValue(Convert.ToBase64String(bytes));
    }

    public override Variant ReadJson(JsonReader reader, Type objectType, Variant existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Read base64 string and decode to bytes, then use GD.BytesToVar
        var base64 = reader.Value as string;
        if (base64 == null && reader.TokenType == JsonToken.String)
            base64 = (string)reader.Value;
        if (base64 == null)
            throw new JsonReaderException("Expected base64 string for Variant");
        return GD.BytesToVarWithObjects(Convert.FromBase64String(base64));
    }
}
