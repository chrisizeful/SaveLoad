using Godot;

namespace SaveLoad;

public partial class TogglePanel : VBoxContainer
{

	[Export]
	public string Text;
	public ToggleButton Button => (ToggleButton) FindChild("ToggleButton");

	public override void _Ready() => Button.Text = Text;
	
}
