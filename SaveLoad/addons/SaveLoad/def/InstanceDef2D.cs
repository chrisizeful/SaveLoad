using Godot;

namespace SaveLoad;

/// <summary>
/// A barebones Node3D that can be used for InstaceDefs to have a Definition property.
/// </summary>
public partial class InstanceDef2D : Node2D
{

    public StringName Definition { get; set; }
	public InstanceDef Def => SaveLoad.Instance.Get<InstanceDef>(Definition);
}