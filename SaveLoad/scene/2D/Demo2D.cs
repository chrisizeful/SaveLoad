using Godot;
using System.Linq;

namespace SaveLoad;

/// <summary>
/// An example of using Defs and InstanceDefs. Use ESC to toggle the ModViewer UI.
/// </summary>
public partial class Demo2D : Node
{

    [Export]
    public Node2D Characters { get; private set; }
    [Export]
    public ModViewer Viewer { get; private set; }

    public override void _Ready()
    {
        GD.Randomize();
        // Randomly select a background, if any exist
        var backgrounds = SaveLoader.Instance.Get<BackgroundDef>();
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
        foreach (Character2DDef def in SaveLoader.Instance.GetInstances<Character2DDef, Character2D>())
        {
            Character2D character = def.Instance<Character2D>();
            AddCharacter(character);
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
            SaveLoader.Instance.Unload(SaveLoader.Instance.Mods.ToArray());
            GetTree().Root.AddChild(ResourceLoader.Load<PackedScene>("res://scene/LoadingMods.tscn").Instantiate());
        };
    }

    void AddCharacter(Character2D character)
    {
        character.Rotation = (float)GD.RandRange(-Mathf.Pi, Mathf.Pi);
        character.Scale = GD.Randf() < .5f ? Vector2.One : new(2.0f, 2.0f);
        character.Position = new((float)GD.RandRange(-100.0, 100.0), (float)GD.RandRange(-100.0, 100.0));
        Characters.AddChild(character);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
            Viewer.Visible = !Viewer.Visible;
        if (@event.IsActionPressed("ui_accept") && Characters.GetChildCount() != 0)
            SaveCharacter();
    }

    // Test saving and loading a node to JSON
    async void SaveCharacter()
    {
        // The node to serialize and path to save it to
        string path = "res://scene/save/character.json";
        Node character = Characters.GetChild(GD.RandRange(0, Characters.GetChildCount() - 1));
        // Store the node properties in a dictionary before serialization, on the main thread
        var stored = NodeConverter.Store(character);
        // Serialize + save to file
        string json = await SaveLoader.Instance.Save(stored, Newtonsoft.Json.Formatting.Indented);
		Files.Write(path, json);
        // Load from file + deserialize
        Character2D dupe = SaveLoader.Instance.LoadJson<Character2D>(Files.GetAsText(path));
        AddCharacter(dupe);
    }
}
