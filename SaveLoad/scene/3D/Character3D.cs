using Godot;
using Newtonsoft.Json;

namespace SaveLoad;

public partial class Character3D : CharacterBody3D
{

    public StringName Definition { get; set; }
    [JsonIgnore]
	public Character3DDef Def => SaveLoader.Instance.GetInstance<Character3DDef, Character3D>(Definition);

    [Export]
    public Sprite3D Sprite { get; private set; }

    Variant foo = Vector3.One;

    public override void _Ready()
    {
        Sprite.Texture = Def.Texture;
        if (foo.AsVector3() == Vector3.One)
            foo = new Vector3(2, 2, 2);
        GD.Print(foo);
    }
}
