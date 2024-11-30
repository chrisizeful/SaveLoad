#if TOOLS
using Godot;

namespace SaveLoad;

[Tool]
public partial class DefNodeLoader : EditorPlugin
{

	public override void _EnablePlugin()
	{
		AddCustomType("Def2D", "Node2D", ResourceLoader.Load<CSharpScript>("res://addons/Goarch/def/DefNode2D.cs"), null);
		AddCustomType("Def3D", "Node3D", ResourceLoader.Load<CSharpScript>("res://addons/Goarch/def/DefNode3D.cs"), null);
	}

	public override void _DisablePlugin()
	{
		RemoveCustomType("Def2D");
		RemoveCustomType("Def3D");
	}
}
#endif