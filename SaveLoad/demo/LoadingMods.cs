using Godot;

namespace SaveLoad;

/// <summary>
/// An example of loading mods asynchronously and displaying progress via a <see cref="Godot.ProgressBar"/>
/// and a <see cref="Label"/>.
/// </summary>
public partial class LoadingMods : Control, ISaveLoadListener
{

	[Export]
	public ProgressBar ProgressBar { get; private set; }
	[Export]
	public Label Message { get; private set; }

	public override void _Ready()
	{
		// Load mods
		string[] mods = { "Base" };
		string[] folders = { "assets", "scripts", "addons", "scenes", ".godot/imported" };
		SaveLoad.Instance.Load(this, folders, mods);
	}

	// These methods are called asynchronously by SaveLoad, so to keep Godot happy any interaction with the SceneTree must
	// be done with the deferred methods.
	public void Loading(string def) => Message.SetDeferred("text", $"Loading \"{def}");
	public void Progress(int loaded, int total) => ProgressBar.SetDeferred("value", (loaded / (float) total) * ProgressBar.MaxValue);
	public void Complete()
	{
		// Switch to main demo scene
		CallDeferred(Node.MethodName.QueueFree);
		Node demo = ResourceLoader.Load<PackedScene>("res://demo/Demo.tscn").Instantiate();
		GetTree().Root.CallDeferred(Node.MethodName.AddChild, demo);
	}
}
