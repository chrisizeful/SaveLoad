using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SaveLoad;

/// <summary>
/// De/serializes a <see cref="Def"/> using its name if <see cref="UseCache"/> is enabled.
/// Otherwise, creates a new instance and populates it.
/// </summary>
public class DefConverter : JsonConverter
{

    public bool UseCache { get; set; } = true;

    public override bool CanWrite => UseCache;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteValue(((Def) value).Name);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // Caching
        if (UseCache)
        {
            var defNames = SaveLoad.Instance.DefNames;
            if (defNames != null && reader.TokenType == JsonToken.String)
            {
                string name = (string) reader.Value;
                if (!defNames.ContainsKey(name))
                    throw new JsonException($"The def \"{name}\" does not exist in the names dictionary.");
                return defNames[name];
            }
        }
        // New instance
        if (reader.TokenType != JsonToken.StartObject)
            return null;
        JObject jo = JObject.Load(reader);
        using var jreader = jo.CreateReader();
        object def = Activator.CreateInstance(Type.GetType((string) jo["$type"]));
        serializer.Populate(jreader, def);
        return def;
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Def).IsAssignableFrom(objectType) &&
        !typeof(InstanceDef).IsAssignableFrom(objectType);
    }
}
