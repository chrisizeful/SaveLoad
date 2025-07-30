using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Godot;
using System;
using System.Reflection;
using SaveLoad;

/// <summary>
/// De/serializes ShaderMaterials, persisting shader uniforms.
/// </summary>
public class ShaderConverter : ResourceConverter
{

    protected override void WriteObject(JsonWriter writer, object value, JsonSerializer serializer)
    {
        base.WriteObject(writer, value, serializer);
        // Serialize shader uniforms as named properties
        ShaderMaterial mat = (ShaderMaterial)value;
        if (mat.Shader == null)
            return;
        foreach (var uniform in mat.Shader.GetShaderUniformList())
        {
            var dict = uniform.AsGodotDictionary();
            StringName name = dict["name"].AsStringName();
            writer.WritePropertyName($"uniform:{name}");
            serializer.Serialize(writer, mat.GetShaderParameter(name));
        }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    { 
        if (reader.TokenType == JsonToken.String)
            return base.ReadJson(reader, objectType, existingValue, serializer);
        JObject jo = JObject.Load(reader);
        ShaderMaterial mat = (ShaderMaterial)Activator.CreateInstance(objectType);
        using (var jreader = jo.CreateReader())
            serializer.Populate(jreader, mat);
        // Set shader uniforms
        if (mat.Shader != null)
        {
            foreach (var uniform in mat.Shader.GetShaderUniformList())
            {
                var dict = uniform.AsGodotDictionary();
                StringName name = dict["name"].AsStringName();
                if (jo.TryGetValue($"uniform:{name}", out var token))
                {
                    Variant value = (Variant)token.ToObject(typeof(Variant), serializer);
                    mat.SetShaderParameter(name, value);
                }
            }
        }
        return mat;
    }
    
    public override bool CanConvert(Type objectType) => typeof(ShaderMaterial).IsAssignableFrom(objectType);
}