using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Godot;
using System;
using System.Collections.Generic;

namespace SaveLoad;

public class InstanceDefConverter : JsonConverter
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
        // New instance, populate properties dictionary
        JObject jo = JObject.Load(reader);
        Type defType = Type.GetType((string) jo["$type"]);
        object def = Activator.CreateInstance(defType);
#nullable enable
        JObject? jp = jo.Value<JObject>("Properties");
#nullable disable
        Dictionary<string, object> properties = new();
        if (jp != null)
        {
            jp.Remove("$type");
            Type instanceType = Type.GetType((string) jo["InstanceType"]);
            foreach (var property in jp.Properties())
            {
                PropertyInfo info = instanceType.GetProperty(property.Name);
                if (info == null)
                {
                    GD.PrintErr($"InstanceDefConverter#ReadJson: Matching property not found in type \"{instanceType}\" for property \"{property.Name}\", skipping.");
                    continue;
                }
                var value = property.Value.ToObject(info.PropertyType, serializer);
                properties[property.Name] = value;
            }
        }
        defType.GetProperty("Properties").SetValue(def, properties);
        jo.Remove("Properties");
        // Populate rest of values
        using var jreader = jo.CreateReader();
        serializer.Populate(jreader, def);
        return def;
    }

    public override bool CanConvert(Type objectType) => typeof(InstanceDef).IsAssignableFrom(objectType);

}
