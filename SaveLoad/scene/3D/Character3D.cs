using Godot;
using Newtonsoft.Json;

namespace SaveLoad;

public partial class Character3D : CharacterBody3D
{

    public StringName Definition { get; set; }
    [JsonIgnore]
	public Character3DDef Def => SaveLoader.Instance.GetInstance<Character3DDef, Character3D>(Definition);

    [Export]
    public MeshInstance3D Sprite { get; private set; }

    Variant foo = Vector3.One;

    public override void _Ready()
    {
        StandardMaterial3D mat = (StandardMaterial3D) Sprite.MaterialOverride;
        mat.AlbedoTexture = Def.Texture;
        if (foo.AsVector3() == Vector3.One)
            foo = new Vector3(2, 2, 2);
        GD.Print(foo);
    }
}
