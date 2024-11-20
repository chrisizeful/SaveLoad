using Godot;
using System.Collections.Generic;

namespace SaveLoad;

public partial class ModPreview : VBoxContainer
{

    [Export]
    public TextureRect Icon { get; private set; }
    [Export]
    public Label ModName { get; private set; }
    [Export]
    public Label ModCreator { get; private set; }
    [Export]
    public Label ModVersion { get; private set; }
    [Export]
    public TogglePanel Problems { get; private set; }
    [Export]
    public TogglePanel Dependencies { get; private set; }
    [Export]
    public TogglePanel Incompatible { get; private set; }
    [Export]
    public RichTextLabel Description { get; private set; }

	public void Set(ModEntry entry)
	{
        Visible = entry != null;
        foreach (Node child in Dependencies.Button.Content.GetChildren())
            child.QueueFree();
        foreach (Node child in Incompatible.Button.Content.GetChildren())
            child.QueueFree();
        if (entry == null)
            return;
        ModName.Text = entry.Mod.Name;
        ModCreator.Text = entry.Mod.Creator;
        ModVersion.Text = entry.Mod.ModVersion.ToString();
        Build(Dependencies, entry.Mod.Dependencies);
        Build(Incompatible, entry.Mod.Incompatible);
        Description.Text = entry.Mod.Description;
        SetProblems(entry);
        Icon.Visible = FileAccess.FileExists(entry.Mod.MetaIcon);
        if (!Icon.Visible)
            return;
        Image image = new();
        if (image.Load(entry.Mod.MetaIcon) == Error.Ok)
            Icon.Texture = ImageTexture.CreateFromImage(image);
        else
            Icon.Texture = null;
	}

    void SetProblems(ModEntry entry)
    {
        foreach (Node child in Problems.Button.Content.GetChildren())
            child.QueueFree();
        Problems.Visible = entry.Messages.Count != 0;
        VBoxContainer vbox = new();
        foreach (var problem in entry.Messages)
        {
            ModProblem view = ResourceLoader.Load<PackedScene>("res://addons/ModViewer/ModProblem.tscn").Instantiate<ModProblem>();
            view.Set(problem.Item1, problem.Item2);
            vbox.AddChild(view);
        }
        Problems.Button.Content.AddChild(vbox);
    }

    void Build(TogglePanel panel, List<Mod.Dependency> dependencies)
    {
        panel.Button.Disabled = dependencies.Count == 0;
        if (panel.Button.Disabled)
            panel.Button.ButtonPressed = false;
        if (dependencies.Count == 0)
            return;
        VBoxContainer vbox = new();
        foreach(Mod.Dependency dependency in dependencies)
        {
            ModEntry entry = ResourceLoader.Load<PackedScene>("res://addons/ModViewer/ModEntry.tscn").Instantiate<ModEntry>();
            entry.CanDrop = false;
            entry.NameLabel.Text = dependency.Name;
            entry.CreatorLabel.Text = dependency.Creator;
            if (dependency.ModVersion != null)
                entry.VersionLabel.Text = dependency.ModVersion.ToString();
            vbox.AddChild(entry);
        }
        panel.Button.Content.AddChild(vbox);
    }
}
