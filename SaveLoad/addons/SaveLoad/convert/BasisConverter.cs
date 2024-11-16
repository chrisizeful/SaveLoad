using System;
using Godot;
using Newtonsoft.Json;

namespace SaveLoad;

public class BasisConverter : JsonConverter<Basis>
{

    public override void WriteJson(JsonWriter writer, Basis value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        serializer.Serialize(writer, value.Column0);
        serializer.Serialize(writer, value.Column1);
        serializer.Serialize(writer, value.Column2);
        writer.WriteEndArray();
    }

    public override Basis ReadJson(JsonReader reader, Type objectType, Basis existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        Basis basis = new();
        if (reader.TokenType == JsonToken.StartArray)
        {
            reader.Read();
            basis.Column0 = serializer.Deserialize<Vector3>(reader);
            reader.Read();
            basis.Column1 = serializer.Deserialize<Vector3>(reader);
            reader.Read();
            basis.Column2 = serializer.Deserialize<Vector3>(reader);
            reader.Read();
        }
        return basis;
    }
}