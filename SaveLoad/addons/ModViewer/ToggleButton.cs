using Godot;

namespace SaveLoad;

public partial class ToggleButton : Button
{

    [Export]
    public Control Toggle;
    [Export]
    public Control Content;

    public override void _Ready()
    {
        Toggled += (toggled) => {
            Icon = ResourceLoader.Load<Texture2D>($"res://addons/ModViewer/assets/{(toggled ? "icon-down" : "icon-expand")}.png");
            Toggle.Visible = toggled && Content != null && Content.GetChildCount() != 0;
        };
    }
}
