using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

namespace SaveLoad;

public partial class Character : Node2D
{

    public StringName Definition { get; set; }
    [JsonIgnore]
	public CharacterDef Def => SaveLoader.Instance.GetInstance<CharacterDef, Character>(Definition);

    [Export]
    public Sprite2D Sprite { get; private set; }

    Dictionary<string, string> bogus;

    public override void _Ready()
    {
        Sprite.Texture = Def.Texture;
        // Add a random amount of children to the character
        for (int i = 0; i < GD.RandRange(0, 3); i++)
        {
            string file = "card_diamonds_0" + GD.RandRange(2, 9);
            AddChild(new Sprite2D()
            {
                Name = file,
                Texture = ResourceLoader.Load<Texture2D>($"res://assets/playing-cards-pack/Cards/{file}.png"),
                ShowBehindParent = true,
                Scale = new(.25f, .25f)
            });
        }
        // Testing de/serialization of a data structure within a node
        if (bogus == null)
        {
            bogus = [];
            bogus["hello"] = "world";
            bogus["foo"] = "bar";
            bogus["test"] = "123";
        }
        GD.Print("Bogus:");
        foreach (var kvp in bogus)
            GD.Print($"    {kvp.Key} = {kvp.Value}");
    }
}
