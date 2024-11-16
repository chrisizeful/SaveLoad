using System.Threading.Tasks;
using Godot;
using System.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace  SaveLoad;

public abstract record Def
{

    [JsonIgnore]
    public Mod Owner { get; set; }
    public StringName Name { get; set; }
    public string Base { get; set; }
    public bool Abstract { get; set; }
}

public record InstanceDef : Def
{

    public Type InstanceType { get; set; }

    public Dictionary<string, object> Properties { get; set; }

    // Create an instance of this def
    public virtual T Instance<T>(params object[] parameters)
    {
        // Enable duplicate mode
        JsonSerializer serializer = SaveLoad.CreateDefault();
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

    public async virtual Task<T> InstanceAsync<T>(params object[] parameters)
	{
		return await Task.Run(() => {
			GodotThread.SetThreadSafetyChecksEnabled(false);
			return Instance<T>(parameters);
		});
	}

    // Create a new instance that copies property layout of @base
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

    public async virtual Task<T> InstanceAsync<T>(T @base, params object[] parameters)
	{
		return await Task.Run(() => {
			GodotThread.SetThreadSafetyChecksEnabled(false);
			return Instance(@base, parameters);
		});
	}

    public virtual object Create(InstanceDef def,  params object[] parameters)
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

public record NodeDef : InstanceDef
{

	public string Scene { get; set; }
    public string Script { get; set; }

    public override object Create(InstanceDef def, params object[] parameters)
    {
        NodeDef nd = (NodeDef) def;
        Node result;
		if (nd.Script != null && nd.Scene != null)
            result = Create<Node>(nd.Scene.ToString(), nd.Script.ToString());
		else if (nd.Scene != null)
            result = ResourceLoader.Load<PackedScene>(nd.Scene).Instantiate();
		else
            result = (Node) Activator.CreateInstance(nd.InstanceType, parameters);
        // Set all properties when node is ready
		Populate(result);
		result.Name = def.Name;
        return result;
    }

    // Create scene at path with script at scriptPath attached
	public static T Create<T>(string path, string scriptPath) where T : Node
	{
		Node node = ResourceLoader.Load<PackedScene>(path).Instantiate();
		var id = node.GetInstanceId();
		node.SetScript(ResourceLoader.Load<CSharpScript>(scriptPath));
		return GodotObject.InstanceFromId(id) as T;
	}
}
