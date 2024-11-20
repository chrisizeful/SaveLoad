using System.Collections.Generic;
using Godot;

namespace SaveLoad;

/// <summary>
/// An example of loading mods asynchronously and displaying progress via a <see cref="Godot.ProgressBar"/>
/// and a <see cref="Label"/>.
/// </summary>
public partial class LoadingMods : Control, ISaveLoadListener
{

	/// <summary>
	/// A list of mods the user has enabled. In a normal project, this should be stored with some
	/// kind of settings system - and a loading mods screen would fetch that saved value. It's here
	/// so that the ModViewer UI can easily set it.
	/// </summary>
	public static List<string> Mods = new()
	{
		"Characters",
		"EpicBackgrounds"
	};

	[Export]
	public ProgressBar ProgressBar { get; private set; }
	[Export]
	public Label Message { get; private set; }

	public override void _Ready()
	{
		// Nothing to load...
		if (Mods.Count == 0)
		{
			Complete();
			return;
		}
		// Load mods and specify folders to include
		string[] folders = { "assets", "scripts", "addons", "scenes", ".godot/imported" };
		SaveLoad.Instance.Load(this, folders, Mods.ToArray());
	}

	// These methods are called asynchronously by SaveLoad, so to keep Godot happy any interaction with the SceneTree must
	// be done with the deferred methods.
	public void Loading(string def) => Message.SetDeferred("text", $"Loading \"{def}");
	public void Progress(int loaded, int total) => ProgressBar.SetDeferred("value", (loaded / (float) total) * ProgressBar.MaxValue);
	public void Complete()
	{
		// Switch to main demo scene
		CallDeferred(Node.MethodName.QueueFree);
		Node demo = ResourceLoader.Load<PackedScene>("res://scene/Demo.tscn").Instantiate();
		GetTree().Root.CallDeferred(Node.MethodName.AddChild, demo);
	}
}
