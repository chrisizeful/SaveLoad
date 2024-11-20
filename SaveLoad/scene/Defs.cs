using Godot;

namespace SaveLoad;

/// <summary>
/// A basic custom Def.
/// </summary>
public record BackgroundDef : Def
{

    public Texture2D Background { get; set; }
}