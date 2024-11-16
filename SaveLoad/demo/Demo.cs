using Godot;

namespace SaveLoad;

public partial class Demo : Node
{

    [Export]
    public CanvasLayer GUI { get; private set; }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
            GUI.Visible = !GUI.Visible;
    }
}
