using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Godot;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace SaveLoad;

/// <summary>
/// De/serializes resources to strings using their ResourcePath. If the resource is not found, a
/// default resource specified in <see cref="Defaults"/> will used (if one exists).
/// </summary>
public class ResourceConverter : JsonConverter
{

    /// <summary>
    /// Resource fallbacks to use when one does not exist at a requested path.
    /// </summary>
    public static Dictionary<Type, string> Defaults = new()
    {
        { typeof(Texture2D), "res://addons/SaveLoad/assets/default/Missing.png" }
    };
    private static readonly Dictionary<Type, object> _instances = [];

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Resource resource = (Resource)value;
        // Save a string if resource path is valid file path
        if (IsValid(resource.ResourcePath))
        {
            writer.WriteValue(resource.ResourcePath);
            return;
        }
        // Get default value
        var type = resource.GetType();
        if (!_instances.TryGetValue(type, out var instance))
            instance = _instances[type] = Activator.CreateInstance(type);
        // Otherwise serialize property-by-property
        writer.WriteStartObject();
        writer.WritePropertyName("$type");
        writer.WriteValue(type.AssemblyQualifiedName);
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!prop.CanRead || NodeConverter.Ignore(prop.Name))
                continue;
            var propValue = prop.GetValue(resource);
            if (propValue == null || propValue.Equals(prop.GetValue(instance)))
                continue;
            writer.WritePropertyName(prop.Name);
            serializer.Serialize(writer, propValue);
        }
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (NodeConverter.Ignore(field.Name))
                continue;
            var fieldValue = field.GetValue(resource);
            if (fieldValue == null || fieldValue.Equals(field.GetValue(instance)))
                continue;
            writer.WritePropertyName(field.Name);
            serializer.Serialize(writer, fieldValue);
        }
        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // If token is just a string, load the resource using it as the path
        if (reader.TokenType == JsonToken.String)
        {
            string path = (string)reader.Value;
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
            resource = (Resource)Activator.CreateInstance(Type.GetType((string)jo["$type"]));
        else
            resource = ResourceLoader.LoadThreadedGet(rpath);
        serializer.Populate(jreader, resource);
        return resource;
    }

    protected virtual Resource GetDefault(Type type)
    {
        if (Defaults.TryGetValue(type, out var path))
            return ResourceLoader.Load(path);
        return (Resource)Activator.CreateInstance(type);
    }

    protected bool IsValid(string path)
    {
        return !string.IsNullOrEmpty(path) && !path.Contains("::") && ResourceLoader.Exists(path);
    }

    public override bool CanConvert(Type objectType)
    {
        // Ensure this converter is only used as fallback if no Resource-specific one exists for the type
        foreach (JsonConverter converter in SaveLoader.Instance.Settings.Converters)
            if (converter != this && converter.CanConvert(objectType))
                return false;
        return typeof(Resource).IsAssignableFrom(objectType);
    }
}
