using Godot;

namespace SaveLoad;

public partial class ModProblem : PanelContainer
{
	
	public void Set(ModEntry.Conflict conflict, string message)
	{
		if (conflict == ModEntry.Conflict.None)
        	AddThemeStyleboxOverride("panel", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Blue.tres"));
		else if (conflict == ModEntry.Conflict.Warning)
			AddThemeStyleboxOverride("panel", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Yellow.tres"));
		else if (conflict == ModEntry.Conflict.Error)
			AddThemeStyleboxOverride("panel", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Red.tres"));
		((Label) FindChild("Problem")).Text = message;
	}
}
