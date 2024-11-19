using System.Threading.Tasks;
using Godot;
using System.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace SaveLoad;

/// <summary>
/// Defines content with a unique name.
/// </summary>
public abstract record Def
{

    /// <summary>
    /// Which Mod this Def orginates from.
    /// </summary>
    [JsonIgnore]
    public Mod Owner { get; set; }
    /// <summary>
    /// A unique name used to identify the Def.
    /// </summary>
    public StringName Name { get; set; }
    /// <summary>
    /// An optional Def that this one inherits from.
    /// </summary>
    public string Base { get; set; }
    /// <summary>
    /// If this Def exists only to provide properties to other Defs.
    /// </summary>
    public bool Abstract { get; set; }
}

/// <summary>
/// A type of Def that defines an object of type <see cref="InstanceType"/> which can be instantiated and populated with its data.
/// </summary>
public record InstanceDef : Def
{

    /// <summary>
    /// The type of object to instantiate.
    /// </summary>
    public Type InstanceType { get; set; }

    /// <summary>
    /// The data that an instantiated object will be populated with.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; }

    /// <summary>
    /// Create an instance of this def.
    /// </summary>
    /// <typeparam name="T">The type to cast to.</typeparam>
    /// <param name="parameters">Parameters for the object constructor.</param>
    /// <returns></returns>
    public virtual T Instance<T>(params object[] parameters)
    {
        // Enable duplicate mode
        JsonSerializer serializer = SaveLoad.Instance.CreateDefault();
        DefConverter dc = serializer.Converter<DefConverter>();
        InstanceDefConverter idc = serializer.Converter<InstanceDefConverter>();
        dc.UseCache = idc.UseCache = true;
        // Serialize then derserialize this def to create unique instances of objects
        StringWriter writer = new();
        serializer.Serialize(writer, this);
        StringReader reader = new(writer.ToString());
        InstanceDef deserial = (InstanceDef) serializer.Deserialize(reader, GetType());
        return (T) Create(deserial, parameters);
    }

    /// <summary>
    /// Create an instance of this def.
    /// </summary>
    /// <typeparam name="T">The type to cast to.</typeparam>
    /// <param name="parameters">Parameters for the object constructor.</param>
    /// <returns></returns>
    public async virtual Task<T> InstanceAsync<T>(params object[] parameters)
	{
		return await Task.Run(() => {
			GodotThread.SetThreadSafetyChecksEnabled(false);
			return Instance<T>(parameters);
		});
	}

    /// <summary>
    /// Create a new instance that copies the property layout of @base.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="base"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public virtual T Instance<T>(T @base, params object[] parameters)
    {
        T instance = Instance<T>(parameters);
        foreach (PropertyInfo prop in typeof(T).GetProperties())
        {
            // Non-public or non-existent set/get
            if (prop.GetSetMethod() == null || prop.GetGetMethod() == null)
                continue;
            prop.SetValue(instance, prop.GetValue(@base));
        }
        return instance;
    }

    /// <summary>
    /// Create a new instance that copies the property layout of @base.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="base"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public async virtual Task<T> InstanceAsync<T>(T @base, params object[] parameters)
	{
		return await Task.Run(() => {
			GodotThread.SetThreadSafetyChecksEnabled(false);
			return Instance(@base, parameters);
		});
	}

    /// <summary>
    /// Creates and populates an object of type <see cref="InstanceType"/>.
    /// </summary>
    /// <param name="parameters">Optional parameters for the constructor.</param>
    /// <returns></returns>
    public virtual object Create(params object[] parameters)
    {
        // Copy fields from def to created instance properties
        object instance = Activator.CreateInstance(InstanceType, parameters);
        Populate(instance);
        return instance;
    }

    protected void Populate(object instance)
    {
        if (InstanceType == null)
            return;
        PropertyInfo definition = InstanceType.GetProperty("Definition");
        definition?.SetValue(instance, Name);
        foreach (string prop in Properties.Keys)
        {
            PropertyInfo info = InstanceType.GetProperty(prop);
            if (info == null || info.GetSetMethod() == null)
            {
                GD.PrintErr($"Defs#Populate: Property {prop} not found in type {InstanceType}");
                continue;
            }
            if (!info.PropertyType.IsAssignableFrom(Properties[prop].GetType()))
            {
                GD.PrintErr("Defs#Populate: Instance type \"{info.PropertyType}\" does not match property type \"{Properties[prop].GetType()}\"");
                continue;
            }
            info.SetValue(instance, Properties[prop]);
        }
    }
}

/// <summary>
/// A type of InstanceDef that allows for specifying a scene file and/or a script file.
/// If both are provided, the scene is instantiated and the script attached to it. If
/// only a scene is provided that is what is instantiated. The InstanceType property
/// is used a final fallback for what object to create.
/// </summary>
public record NodeDef : InstanceDef
{

    /// <summary>
    /// Path the a scene file.
    /// </summary>
	public string Scene { get; set; }
    /// <summary>
    /// Path to a C# or GDSCript file.
    /// </summary>
    public string Script { get; set; }

    /// <summary>
    /// Create and populates a node.
    /// </summary>
    /// <param name="def"></param>
    /// <param name="parameters">Only used for the constructor if the node is created through reflection.</param>
    /// <returns></returns>
    public override object Create(params object[] parameters)
    {
        Node result;
		if (Script != null && Scene != null)
            result = Create<Node>(Scene.ToString(), Script.ToString());
		else if (Scene != null)
            result = ResourceLoader.Load<PackedScene>(Scene).Instantiate();
		else
            result = (Node) Activator.CreateInstance(InstanceType, parameters);
        // Set all properties when node is ready
		Populate(result);
		result.Name = Name;
        return result;
    }

    /// <summary>
    /// Create scene at path with script at scriptPath attached.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path">The path to the scene file.</param>
    /// <param name="scriptPath">The path to the C# or GDScript file.</param>
    /// <returns></returns>
	public static T Create<T>(string path, string scriptPath) where T : Node
	{
		Node node = ResourceLoader.Load<PackedScene>(path).Instantiate();
		var id = node.GetInstanceId();
		node.SetScript(ResourceLoader.Load(scriptPath));
		return GodotObject.InstanceFromId(id) as T;
	}
}
