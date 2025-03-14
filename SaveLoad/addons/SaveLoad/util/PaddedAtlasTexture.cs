using Godot;

namespace SaveLoad;

/// <summary>
/// An atlas texture that contains padding on all sides of it.
/// </summary>
public partial class PaddedAtlasTexture : AtlasTexture
{

    /// <summary>
    /// The excluded pixels surrouding around the texture.
    /// </summary>
    public Vector2 Padding { get; set; } = Vector2.Zero;

    public void SetRegion(Rect2 region, Vector2 padding)
    {
        // Offset is tile coord times twice padding
        float xo = (region.Position.X / region.Size.X) * padding[0] * 2;
        float yo = (region.Position.Y / region.Size.Y) * padding[1] * 2;
        Region = new Rect2(padding[0] + region.Position.X + xo, padding[1] + region.Position.Y + yo, region.Size.X, region.Size.Y);
        Padding = padding;
    }
}
