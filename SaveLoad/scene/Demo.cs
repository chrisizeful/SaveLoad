using Godot;
using System.Linq;

namespace SaveLoad;

/// <summary>
/// An example of using Defs and InstanceDefs. Use ESC to toggle the ModViewer UI.
/// </summary>
public partial class Demo : Node
{

    [Export]
    public Node2D Characters { get; private set; }
    [Export]
    public CanvasLayer GUI { get; private set; }
    [Export]
    public ModViewer Viewer { get; private set; }

    public override void _Ready()
    {
        GD.Randomize();
        // Randomly select a background, if any exist
        var backgrounds = SaveLoad.Instance.Get<BackgroundDef>();
        if (backgrounds.Count != 0)
        {
            BackgroundDef def = backgrounds[GD.RandRange(0, backgrounds.Count - 1)];
            Node background = new Sprite2D()
            {
                Texture = def.Background
            };
            AddChild(background);
            MoveChild(background, 0);
        }
        // Randomly add some characters, if any exist
        foreach (InstanceDef def in SaveLoad.Instance.GetInstances<InstanceDef, Sprite2D>())
        {
            Sprite2D sprite = def.Instance<Sprite2D>();
            sprite.Position = new((float) GD.RandRange(-100.0, 100.0), (float) GD.RandRange(-100.0, 100.0));
            Characters.AddChild(sprite);
        }
        // Setup viewer reload
        Viewer.Reload.Pressed += () => {
            GetTree().Root.GetChild(0).QueueFree();
            // Store the list of mods the user enabled, this would normally be stored with
            // some kind of settings system.
            LoadingMods.Mods = Viewer.Enabled.ModIDs;
            // Have to unload all loaded mods and switch back to the loading mods screen so the new mod list
            // can be properly loaded. While you could just check which new mods the user enabled, loading only
            // those wouldn't properly sort their dependencies.
            SaveLoad.Instance.Unload(SaveLoad.Instance.Mods.ToArray());
            GetTree().Root.AddChild(ResourceLoader.Load<PackedScene>("res://scene/LoadingMods.tscn").Instantiate());
        };
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
            GUI.Visible = !GUI.Visible;
    }
}
