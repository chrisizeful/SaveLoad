using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace SaveLoad;

/// <summary>
/// De/serializes a node and its children.
/// </summary>
public class NodeConverter : JsonConverter
{

    // Properties to ignore
    private readonly HashSet<string> _ignore = [
        // Node
        "Owner",
        "NativeObject",
        "_ImportPath",
        "EditorDescription",
        "Filename",
        "Name",
        // Node2D / Node3D
        "Transform",
        "Rotation",
        "RotationDegrees",
        "RotationOrder",
        "GlobalBasis",
        "GlobalPosition",
        "GlobalRotation",
        "GlobalRotationDegrees",
        "GlobalScale",
        "GlobalTransform",
        "GlobalSkew",
        "Quaternion",
        "ZAsRelative"
    ];
    // Default value instances
    private readonly Dictionary<Type, object> _instances = [];

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Node node = (Node) value;
        if (!string.IsNullOrEmpty(node.SceneFilePath))
        {
            writer.WriteValue(node.SceneFilePath);
            return;
        }
        Type type = node.GetType();
        // Get default value
        if (!_instances.TryGetValue(type, out var instance))
            instance = _instances[type] = Activator.CreateInstance(type);
        // Start node
        writer.WriteStartObject();
        // Always write node type
        writer.WritePropertyName("$type");
        serializer.Serialize(writer, node.GetType());
        // Write name, if valid
        string name = node.Name.ToString();
        if (!string.IsNullOrEmpty(name) && !name.StartsWith("@"))
        {
            writer.WritePropertyName("Name");
            serializer.Serialize(writer, name);
        }
        // Write node properties
        foreach (PropertyInfo prop in node.GetType().GetProperties())
        {
            // Skip JsonIgnore properties
            if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                continue;
            // Skip ignored and read-only properties
            if (_ignore.Contains(prop.Name) || prop.SetMethod == null)
                continue;
            object propValue = prop.GetValue(node);
            // Skip null properties
            if (propValue == null)
                continue;
            // Skip default properties
            if (propValue.Equals(prop.GetValue(instance)))
                continue;
            // Write property
            writer.WritePropertyName(prop.Name);
            serializer.Serialize(writer, propValue);
        }
        // Write children
        if (node.GetChildCount() != 0)
        {
            writer.WritePropertyName("Children");
            writer.WriteStartArray();
            foreach (Node child in node.GetChildren())
                serializer.Serialize(writer, child);
            writer.WriteEndArray();
        }
        // End node
        writer.WriteEndObject(); 
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // Instance using resource loader if token is a string, otherwise assume it's an object
        if (reader.TokenType == JsonToken.String)
            return Create((string) reader.Value);
        return Create(JObject.Load(reader), serializer);
    }

    private Node Create(string path)
    {
        return ResourceLoader.Load<PackedScene>(path).Instantiate();
    }

    private Node Create(JObject jo, JsonSerializer serializer)
    {
        // Create node using activator or scene
        Node node = jo.TryGetValue("SceneFilePath", out var path)
            ? ResourceLoader.Load<PackedScene>((string) path).Instantiate<Node>()
            : (Node) Activator.CreateInstance(Type.GetType((string) jo["$type"]));
        // Assure valid name
        if (!jo.ContainsKey("Name"))
        {
            node.Name = new StringName(node.GetType().Name);
        }
        else
        {
            node.Name = new StringName(jo["Name"].ToString());
            jo.Remove("Name");
        }
        // Populate object
        serializer.Populate(jo.CreateReader(), node);
        // Add children, if any
        if (jo.ContainsKey("Children"))
        {
            foreach (var child in jo["Children"])
            {
                if (child is JObject joc)
                    node.AddChild(Create(joc, serializer));
                else if (child is JValue value)
                    node.AddChild(Create((string) value));
            }
        }
        return node;
    }

    public override bool CanConvert(Type objectType)
    {
        // Ensure this converter is only used as fallback if no Node-specific one exist for the type
        foreach (JsonConverter converter in SaveLoader.Instance.Settings.Converters)
            if (converter != this && converter.CanConvert(objectType))
                return false;
        return typeof(Node).IsAssignableFrom(objectType);
    }
}
