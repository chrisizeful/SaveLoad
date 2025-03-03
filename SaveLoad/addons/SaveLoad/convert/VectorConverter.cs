using Godot;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace SaveLoad;

/// <summary>
/// De/serializes a <see cref="Vector2"/> to a string, i.e. "0.0,0.0" or "0.0, 0.0".
/// </summary>
// TODO Allow nullable vectors
public class Vector2Converter : JsonConverter, IStringConverter
{

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Vector2 vec = (Vector2) value;
        writer.WriteValue(vec.X + "," + vec.Y);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return (Vector2) Convert(reader.Value.ToString());
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector2) || objectType == typeof(Vector2?);
    }
    
    public object Convert(string input)
    {
        string[] split = Regex.Split(input, ",\\s*");
        return new Vector2(float.Parse(split[0]), float.Parse(split[1]));
    }
}

/// <summary>
/// De/serializes a <see cref="Vector2I"/> to a string, i.e. "0,0" or "0, 0".
/// </summary>
public class Vector2IConverter : JsonConverter, IStringConverter
{

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Vector2I vec = (Vector2I) value;
        writer.WriteValue(vec.X + "," + vec.Y);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return (Vector2I) Convert(reader.Value.ToString());
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector2I) || objectType == typeof(Vector2I?);
    }

    public object Convert(string input)
    {
        string[] split = Regex.Split(input, ",\\s*");
        return new Vector2I(int.Parse(split[0]), int.Parse(split[1]));
    }
}

/// <summary>
/// De/serializes a <see cref="Vector3"/> to a string, i.e. "0.0,0.0,0.0" or "0.0, 0.0, 0.0".
/// </summary>
public class Vector3Converter : JsonConverter, IStringConverter
{

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Vector3 vec = (Vector3) value;
        writer.WriteValue(vec.X + "," + vec.Y + "," + vec.Z);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return (Vector3) Convert(reader.Value.ToString());
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector3) || objectType == typeof(Vector3?);
    }

    public object Convert(string input)
    {
        string[] split = Regex.Split(input, ",\\s*");
        return new Vector3(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
    }
}

/// <summary>
/// De/serializes a <see cref="Vector3I"/> to a string, i.e. "0,0,0" or "0, 0, 0".
/// </summary>
public class Vector3IConverter : JsonConverter, IStringConverter
{

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Vector3I vec = (Vector3I) value;
        writer.WriteValue(vec.X + "," + vec.Y + "," + vec.Z);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return (Vector3I) Convert(reader.Value.ToString());
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector3I) || objectType == typeof(Vector3I?);
    }

    public object Convert(string input)
    {
        string[] split = Regex.Split(input, ",\\s*");
        return new Vector3I(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]));
    }
}

/// <summary>
/// De/serializes a <see cref="Vector4"/> to a string, i.e. "0.0,0.0,0.0,0.0" or "0.0, 0.0, 0.0, 0.0".
/// </summary>
public class Vector4Converter : JsonConverter, IStringConverter
{

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Vector4 vec = (Vector4) value;
        writer.WriteValue(vec.X + "," + vec.Y + "," + vec.Z + "," + vec.W);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return (Vector4) Convert(reader.Value.ToString());
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector4) || objectType == typeof(Vector4?);
    }
    
    public object Convert(string input)
    {
        string[] split = Regex.Split(input, ",\\s*");
        return new Vector4(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
    }
}

/// <summary>
/// De/serializes a <see cref="Vector4I"/> to a string, i.e. "0,0,0,0" or "0, 0, 0, 0".
/// </summary>
public class Vector4IConverter : JsonConverter, IStringConverter
{

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Vector4I vec = (Vector4I) value;
        writer.WriteValue(vec.X + "," + vec.Y + "," + vec.Z + "," + vec.W);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return (Vector4I) Convert(reader.Value.ToString());
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector4I) || objectType == typeof(Vector4I?);
    }
    
    public object Convert(string input)
    {
        string[] split = Regex.Split(input, ",\\s*");
        return new Vector4I(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]));
    }
}
