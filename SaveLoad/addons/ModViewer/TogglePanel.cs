using Godot;

namespace SaveLoad;

/// <summary>
/// A control that works in conjuguntion with <see cref="ToggleButton"/> to show/hide
/// itself when pressed.
/// </summary>
public partial class TogglePanel : VBoxContainer
{

	[Export]
	public string Text { get; set; }
	[Export]
	public ToggleButton Button { get; private set; }

	public override void _Ready() => Button.Text = Text;
}
