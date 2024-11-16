using System;
using Newtonsoft.Json;
using Godot;

namespace SaveLoad;

// Save/load color as a html string
public class ColorConverter : JsonConverter<Color>
{

    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToHtml());
    }

    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return new Color((string) reader.Value);
    }

    public object Convert(string input) => new Color(input);
}
