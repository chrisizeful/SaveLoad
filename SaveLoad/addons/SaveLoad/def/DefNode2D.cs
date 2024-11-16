using Godot;
using System;

namespace SaveLoad;

public partial class DefNode2D : Node2D
{
    
    [Export]
    public StringName DefName;

	public static void Replace(Node parent, Func<DefNode2D, Node2D> replace)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is DefNode2D d2d)
			{
				Node2D instance = replace.Invoke(d2d);
				instance.Position = d2d.Position;
				parent.AddChild(instance);
				child.QueueFree();
			}
		}
	}

    public static void Replace(Node parent)
	{
		Replace(parent, (def) => ((InstanceDef) SaveLoad.Instance.DefNames[def.DefName]).Instance<Node2D>());
	}
}