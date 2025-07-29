using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Godot;
using System;
using System.Reflection;
using SaveLoad;

/// <summary>
/// De/serializes ShaderMaterials
/// </summary>
public class ShaderConverter : JsonConverter<ShaderMaterial>
{

    public override void WriteJson(JsonWriter writer, ShaderMaterial value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("$type");
        writer.WriteValue(value.GetType().AssemblyQualifiedName);
        // Serialize base Resource properties
        foreach (var prop in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!prop.CanRead || NodeConverter.Ignore(prop.Name))
                continue;
            var propValue = prop.GetValue(value);
            writer.WritePropertyName(prop.Name);
            serializer.Serialize(writer, propValue);
        }
        foreach (var field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (NodeConverter.Ignore(field.Name))
                continue;
            var fieldValue = field.GetValue(value);
            writer.WritePropertyName(field.Name);
            serializer.Serialize(writer, fieldValue);
        }
        // Serialize shader uniforms as named properties
        if (value.Shader != null)
        {
            foreach (var uniform in value.Shader.GetShaderUniformList())
            {
                StringName name = uniform.AsStringName();
                writer.WritePropertyName($"uniform:{name}");
                serializer.Serialize(writer, value.GetShaderParameter(name));
            }
        }
        writer.WriteEndObject();
    }

    public override ShaderMaterial ReadJson(JsonReader reader, Type objectType, ShaderMaterial existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        ShaderMaterial mat = (ShaderMaterial)Activator.CreateInstance(objectType);
        using (var jreader = jo.CreateReader())
            serializer.Populate(jreader, mat);
        // Set shader uniforms
        if (mat.Shader != null)
        {
            foreach (var uniform in mat.Shader.GetShaderUniformList())
            {
                StringName name = uniform.AsStringName();
                if (jo.TryGetValue($"uniform:{name}", out var token))
                {
                    Variant value = (Variant) token.ToObject(typeof(Variant), serializer);
                    mat.SetShaderParameter(name, value);
                }
            }
        }
        return mat;
    }
}