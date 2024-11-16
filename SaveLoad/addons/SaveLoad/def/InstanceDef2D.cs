using Godot;
using System;

namespace SaveLoad;

public partial class InstanceDef2D : Node2D
{

    public StringName Definition { get; set; }
	public InstanceDef Def => SaveLoad.Instance.Get<InstanceDef>(Definition);
}