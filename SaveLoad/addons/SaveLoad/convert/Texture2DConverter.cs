using Newtonsoft.Json;
using Godot;
using System;

namespace SaveLoad;

public class Texture2DConverter : ResourceConverter
{

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is PaddedAtlasTexture pad)
        {
            writer.WriteValue($"{pad.Atlas.ResourcePath}:[{pad.Region.Position.X},{pad.Region.Position.Y},{pad.Region.Size.X},{pad.Region.Size.Y}]:[{pad.Padding[0]},{pad.Padding[1]}]");
            return;
        }
        if (value is AtlasTexture atlas)
        {
            writer.WriteValue($"{atlas.Atlas.ResourcePath}:[{atlas.Region.Position.X},{atlas.Region.Position.Y},{atlas.Region.Size.X},{atlas.Region.Size.Y}]");
            return;
        }
        base.WriteJson(writer, value, serializer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
#nullable enable
            string? value = reader.Value?.ToString();
#nullable disable
            if (value != null)
            {
                string[] split = value.Replace("res://", "").Trim().Split(":");
                if (split.Length != 1)
                {
                    int[] @out = GetRect(split[1]);
                    Rect2 rect = new Rect2(@out[0], @out[1], @out[2], @out[3]);
                    string path = "res://" + split[0];
                    if (ResourceLoader.Exists(path))
                    {
                        Texture2D atlas = ResourceLoader.Load<Texture2D>(path);
                        // Region, margin, separation
                        if (split.Length == 2)
                        {
                            return new AtlasTexture()
                            {
                                Atlas = atlas,
                                Region = rect
                            };
                        }
                        PaddedAtlasTexture padded = new()
                        {
                            Atlas = atlas
                        };
                        Rect2 region = rect;
                        @out = GetRect(split[2]);
                        Vector2 padding =  new Vector2(@out[0], @out[1]);
                        padded.SetRegion(region, padding);
                        return padded;
                    }
                }
            }
        }
        return base.ReadJson(reader, objectType, existingValue, serializer);
    }

    // Return integers from format [0,0], [0,0,0,0], etc
    private int[] GetRect(string input)
    {
        string[] size = input.Substr(1, input.Length - 2).Split(",");
        int[] rect = new int[size.Length];
        for (int i = 0; i < size.Length; i++)
            rect[i] = size[i].ToInt();
        return rect;
    }

    public override bool CanConvert(Type objectType) => typeof(Texture2D).IsAssignableFrom(objectType);
}
