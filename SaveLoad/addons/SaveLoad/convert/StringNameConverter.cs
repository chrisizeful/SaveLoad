using Godot;
using Newtonsoft.Json;
using System;

namespace SaveLoad;

/// <summary>
/// De/serializes a <seealso cref="StringName"/> to a string.
/// </summary>
public class StringNameConverter : JsonConverter<StringName>, IStringConverter
{

    public override void WriteJson(JsonWriter writer, StringName value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }

    public override StringName ReadJson(JsonReader reader, Type objectType, StringName existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return new(reader.Value.ToString());
    }
    
    public object Convert(string input) => (StringName) input;
}
