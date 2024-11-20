using Godot;
using System;

namespace SaveLoad;

/// <summary>
/// Used to place InstanceDefs in the editor. To use it add a DefNode2D to you scene and specify a DefName. Then,
/// call <see cref="Replace"/> on the root node to replace all DefNode2Ds with InstanceDef instances.
/// </summary>
public partial class DefNode2D : Node2D
{
    
    [Export]
    public StringName DefName;

	/// <summary>
	/// Iterates through the children of parent, replacing any DefNode2Ds with with InstanceDef
	/// instances. The position of the DefNode2D is applied to the created instance.
	/// </summary>
	/// <param name="parent">The root node to check the children of.</param>
	/// <param name="replace">A function that replaces a DefNode2D with a Node2D.</param>
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

	/// <summary>
	/// Iterates through the children of parent, replacing any DefNode2Ds with with InstanceDef
	/// instances. The position of the DefNode2D is applied to the created instance.
	/// </summary>
	/// <param name="parent">The root node to check the children of.</param>
    public static void Replace(Node parent)
	{
		Replace(parent, (def) => ((InstanceDef) SaveLoader.Instance.DefNames[def.DefName]).Instance<Node2D>());
	}
}