using System;
using Newtonsoft.Json;
using Godot;

namespace SaveLoad;

/// <summary>
/// Converts a <see cref="Color"/> to JSON. The color is de/serialized as an HTML string.
/// </summary>
// TODO Allow nullable colors
public class ColorConverter : JsonConverter<Color>, IStringConverter
{

    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToHtml());
    }

    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return new((string) reader.Value);
    }

    public object Convert(string input) => new Color(input);
}
