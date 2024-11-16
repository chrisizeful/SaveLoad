using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Godot;
using System.Collections.Generic;
using System;

namespace SaveLoad;

// Load resources from a path
public class ResourceConverter : JsonConverter
{

    public static Dictionary<Type, string> Defaults = new()
    {
        { typeof(Texture2D), "res://addons/SaveLoad/assets/default/Missing.png" }
    };

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Resource resource = (Resource) value;
        // Save a string if resource path is valid file path
        if (IsValid(resource.ResourcePath))
        {
            writer.WriteValue(((Resource) value).ResourcePath);
        }
        // Otherwise serialize resource normally
        else
        {
            serializer.Converters.Remove(this);
            JObject jo = JObject.FromObject(resource, serializer);
            jo.Remove("ResourcePath");
            jo.Remove("LoadPath");
            jo.Remove("ResourceName");
            jo.Remove("ResourceLocalToScene");
            jo.WriteTo(writer);
            serializer.Converters.Add(this);
        }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // If token is just a string, load the resource using it as the path
        if (reader.TokenType == JsonToken.String)
        {
            string path = (string) reader.Value;
            if (!ResourceLoader.Exists(path))
            {
                GD.PrintErr($"ResourceConverter#ReadJson: Default resource used in place of missing resource at \"{path}\"");
                return GetDefault(objectType);
            }
            return ResourceLoader.Load(path);
        }
        // Load object from resource path if valid, otherwise create new one; populate w json
        JObject jo = JObject.Load(reader);
        using var jreader = jo.CreateReader();
        string rpath = jo["ResourcePath"]?.Value<string>();
        object resource;
        if (!IsValid(rpath))
            resource = (Resource) Activator.CreateInstance(Type.GetType((string) jo["$type"]));
        else
            resource = ResourceLoader.LoadThreadedGet(rpath);
        serializer.Populate(jreader, resource);
        return resource;
    }

    protected virtual Resource GetDefault(Type type)
    {
        if (Defaults.TryGetValue(type, out var path))
            return ResourceLoader.Load(path);
        return (Resource) Activator.CreateInstance(type);
    }

    protected bool IsValid(string path)
    {
        return !string.IsNullOrEmpty(path) && !path.Contains("::") && ResourceLoader.Exists(path);
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Resource).IsAssignableFrom(objectType) &&
        !typeof(Texture2D).IsAssignableFrom(objectType);
    }
}
