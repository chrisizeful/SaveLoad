using Godot;
using System;

namespace SaveLoad;

public partial class DefNode3D : Node3D
{
    
    [Export]
    public StringName DefName;

	public static void Replace(Node parent, Func<DefNode3D, Node3D> replace)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is DefNode3D d3d)
			{
				Node3D instance = replace.Invoke(d3d);
				instance.Position = d3d.Position;
				parent.AddChild(instance);
				child.QueueFree();
			}
		}
	}

    public static void Replace(Node parent)
	{
		Replace(parent, (def) => ((InstanceDef) SaveLoad.Instance.DefNames[def.DefName]).Instance<Node3D>());
	}
}