using Godot;

namespace SaveLoad;

/// <summary>
/// A basic custom Def.
/// </summary>
public record BackgroundDef : Def
{

    public Texture2D Background { get; set; }
}

/// <summary>
/// A basic custom InstanceDef.
/// </summary>
public record CharacterDef : NodeDef
{

    public Texture2D Texture { get; set; }
}