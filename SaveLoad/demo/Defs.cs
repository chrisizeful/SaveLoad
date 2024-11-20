using Godot;

namespace SaveLoad;

public record BackgroundDef : Def
{

    public Texture2D Background { get; set; }
}