using Newtonsoft.Json;
using Godot;
using System;

namespace SaveLoad;

/// <summary>
/// Save/load <see cref="Variant"/>.
/// </summary>
public class VariantConverter : JsonConverter<Variant>
{

    public override void WriteJson(JsonWriter writer, Variant value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Obj);
    }

    public override Variant ReadJson(JsonReader reader, Type objectType, Variant existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return Variant.From(reader.Value);
    }
}
