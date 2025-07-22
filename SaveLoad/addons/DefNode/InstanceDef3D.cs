using Godot;
using Newtonsoft.Json;

namespace SaveLoad;

/// <summary>
/// A barebones Node3D that can be used for InstaceDefs to have a Definition property.
/// </summary>
public partial class InstanceDef3D : Node3D
{

    public StringName Definition { get; set; }
    [JsonIgnore]
	public InstanceDef Def => SaveLoader.Instance.Get<InstanceDef>(Definition);
}