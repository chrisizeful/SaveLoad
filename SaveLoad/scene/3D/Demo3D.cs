using Godot;
using System;
using System.Linq;

namespace SaveLoad;

/// <summary>
/// An example of using Defs and InstanceDefs. Use ESC to toggle the ModViewer UI.
/// </summary>
public partial class Demo3D : Node
{

    [Export]
    public Node3D Characters { get; private set; }
    [Export]
    public ModViewer Viewer { get; private set; }

    public override void _Ready()
    {
        GD.Randomize();
        // Randomly add some characters, if any exist
        foreach (Character3DDef def in SaveLoader.Instance.GetInstances<Character3DDef, Character3D>())
        {
            Character3D character = def.Instance<Character3D>();
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

    void AddCharacter(Character3D character)
    {
        character.Position = new((float)GD.RandRange(-3.0, 3.0), 0, (float)GD.RandRange(-3.0, 3.0));
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
        Character3D dupe = SaveLoader.Instance.LoadJson<Character3D>(Files.GetAsText(path));
        AddCharacter(dupe);
    }
}
