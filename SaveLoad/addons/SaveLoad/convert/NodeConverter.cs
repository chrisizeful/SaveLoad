using Godot;
using Newtonsoft.Json;
using System.Reflection;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;

namespace SaveLoad;

/// <summary>
/// De/serializes a node and its children.
/// </summary>
public class NodeConverter : JsonConverter
{

    // Properties to ignore
    private static readonly HashSet<string> _ignore = [
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
        "ZAsRelative",
        "NativePtr"
    ];
    // Default value instances
    private static readonly Dictionary<Type, object> _instances = [];

    public override bool CanWrite => false;

    /// <summary>
    /// Write JSON not supported as the written object should be a Dictionary<string, object>.
    /// </summary>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotSupportedException("NodeConverter does not support writing JSON.");
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // Read a scene path
        if (reader.TokenType == JsonToken.String)
            return Create((string)reader.Value);
        // Deserialize dictionary
        var dict = serializer.Deserialize<Dictionary<string, object>>(reader);
        if (!dict.TryGetValue("$nodeType", out object typeName))
            return dict; // Not a Node, just a normal dictionary
        var type = Type.GetType((string)typeName);
        if (type == null || !typeof(Node).IsAssignableFrom(type))
            return dict; // Not a Node, just a normal dictionary
        return Create(type, serializer, dict);
    }

    private Node Create(string path) => ResourceLoader.Load<PackedScene>(path).Instantiate();
    private Node Create(Type type, JsonSerializer serializer, Dictionary<string, object> dict)
    {
        // Create node using activator or scene
        Node node = dict.TryGetValue("SceneFilePath", out var path)
            ? Create((string)path)
            : (Node)Activator.CreateInstance(Type.GetType((string)dict["$nodeType"]));
        // Assure valid name
        if (dict.TryGetValue("Name", out object value))
        {
            node.Name = new StringName((string)value);
            dict.Remove("Name");
        }
        node.Name ??= new StringName(node.GetType().Name);
        // Set properties and fields
        foreach (var kvp in dict)
        {
            if (kvp.Key == "$nodeType" || kvp.Key == "Children")
                continue;
            var prop = type.GetProperty(kvp.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite)
            {
                // Convert object to the property type
                var dvalue = JToken.FromObject(kvp.Value).ToObject(prop.PropertyType, serializer);
                prop.SetValue(node, dvalue);
                continue;
            }
            var field = type.GetField(kvp.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                var dvalue = JToken.FromObject(kvp.Value).ToObject(field.FieldType, serializer);
                field.SetValue(node, dvalue);
            }
        }
        // If the node is a scene, remove all children to avoid duplicates
        if (dict.ContainsKey("SceneFilePath"))
        {
            foreach (Node child in node.GetChildren())
            {
                node.RemoveChild(child);
                child.QueueFree();
            }
        }
        // Add children, if any
        if (dict.TryGetValue("Children", out object cdict))
            foreach (var child in (List<Dictionary<string, object>>)cdict)
                node.AddChild(Create(Type.GetType((string)child["$nodeType"]), serializer, child));
        return node;
    }

    public override bool CanConvert(Type objectType)
    {
        // Ensure this converter is only used as fallback if no Node-specific one exists for the type
        foreach (JsonConverter converter in SaveLoader.Instance.Settings.Converters)
            if (converter != this && converter.CanConvert(objectType))
                return false;
        return typeof(Node).IsAssignableFrom(objectType);
    }

    /// <summary>
    /// Stores the properties of a node in a dictionary for serialization. This is
    /// required as Nodes are not thread-safe and cannot be serialized directly on
    /// a separate thread.
    /// </summary>
    public static Dictionary<string, object> Store(Node node)
    {
        var dict = new Dictionary<string, object>();
        var type = node.GetType();
        // Get default value
        if (!_instances.TryGetValue(type, out var instance))
            instance = _instances[type] = Activator.CreateInstance(type);
        // Always write node type
        dict["$nodeType"] = type.AssemblyQualifiedName;
        // Write name, if valid
        string name = node.Name.ToString();
        if (!string.IsNullOrEmpty(name) && !name.StartsWith('@'))
            dict["Name"] = name;
        // Store properties
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            // Skip JsonIgnore properties
            if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                continue;
            // Skip exported properties
            if (prop.GetCustomAttribute<ExportAttribute>() != null)
                continue;
            // Skip ignored and read-only properties
            if (_ignore.Contains(prop.Name) || prop.SetMethod == null)
                continue;
            object propValue = prop.GetValue(node);
            // Skip null and default properties
            if (propValue == null || propValue.Equals(prop.GetValue(instance)))
                continue;
            dict[prop.Name] = propValue;
        }
        // Store fields
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            // Skip JsonIgnore fields
            if (field.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                continue;
            // Skip ignored and backing fields
            if (_ignore.Contains(field.Name) || field.Name.Contains("k__BackingField"))
                continue;
            var fieldValue = field.GetValue(node);
            // Skip null and default fields
            if (fieldValue == null || fieldValue.Equals(field.GetValue(instance)))
                continue;
            dict[field.Name] = fieldValue;
        }
        // Store children
        if (node.GetChildCount() != 0)
        {
            var children = new List<Dictionary<string, object>>();
            foreach (Node child in node.GetChildren())
            {
                var childDict = Store(child);
                if (childDict.Count > 0)
                    children.Add(childDict);
            }
            if (children.Count > 0)
                dict["Children"] = children;
        }
        return dict;
    }
}
