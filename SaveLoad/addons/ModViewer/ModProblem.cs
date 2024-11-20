using Godot;

namespace SaveLoad;

/// <summary>
/// A PanelContainer that changes its theme based on a <see cref="ModEntry.Conflict"/> type.
/// </summary>
public partial class ModProblem : PanelContainer
{

	[Export]
	public Label ProblemLabel { get; private set; }

	/// <summary>
	/// Update the theme to match the conflict level.
	/// </summary>
	/// <param name="conflict">The severity of the issue.</param>
	/// <param name="message">A message describing the issue to the user.</param>
	public void Set(ModEntry.Conflict conflict, string message)
	{
		if (conflict == ModEntry.Conflict.None)
        	AddThemeStyleboxOverride("panel", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Blue.tres"));
		else if (conflict == ModEntry.Conflict.Warning)
			AddThemeStyleboxOverride("panel", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Yellow.tres"));
		else if (conflict == ModEntry.Conflict.Error)
			AddThemeStyleboxOverride("panel", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Red.tres"));
		ProblemLabel.Text = message;
	}
}
