using Newtonsoft.Json;
using System;

namespace SaveLoad;

// Convert Int64 to Int32
public class Int32Converter : JsonConverter
{

    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return (reader.TokenType == JsonToken.Integer) ? Convert.ToInt32(reader.Value) : serializer.Deserialize(reader);
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(int) || objectType == typeof(long) || objectType == typeof(object);
    }
}
