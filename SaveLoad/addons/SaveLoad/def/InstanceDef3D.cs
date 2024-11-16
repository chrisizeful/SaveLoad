using Godot;

namespace SaveLoad;

public partial class InstanceDef3D : Node3D
{

    public StringName Definition { get; set; }
	public InstanceDef Def => SaveLoad.Instance.Get<InstanceDef>(Definition);
}