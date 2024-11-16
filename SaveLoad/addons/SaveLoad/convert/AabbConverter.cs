using System;
using Godot;
using Newtonsoft.Json;

namespace SaveLoad;

public class AabbConverter : JsonConverter<Aabb>
{

    public override void WriteJson(JsonWriter writer, Aabb value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        serializer.Serialize(writer, value.Position);
        serializer.Serialize(writer, value.Size);
        writer.WriteEndArray();
    }

    public override Aabb ReadJson(JsonReader reader, Type objectType, Aabb existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        Aabb aabb = new();
        if (reader.TokenType == JsonToken.StartArray)
        {
            reader.Read();
            aabb.Position = serializer.Deserialize<Vector3>(reader);
            reader.Read();
            aabb.Size = serializer.Deserialize<Vector3>(reader);
            reader.Read();
        }
        return aabb;
    }
}