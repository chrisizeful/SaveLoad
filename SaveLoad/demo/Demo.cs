using Godot;

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
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
            GUI.Visible = !GUI.Visible;
    }
}
