using Godot;
using System.Collections.Generic;

namespace SaveLoad;

public partial class ModEntry : Button
{
    
    public Mod Mod
    {
        get => mod;
        set
        {
            mod = value;
            NameLabel.Text = Mod.Name;
            CreatorLabel.Text = Mod.Creator;
            VersionLabel.Text = Mod.ModVersion.ToString();
            External.Visible = !Mod.Local;
        }
    }
    Mod mod;

    public bool CanDrop { get; set; } = true;

    [Export]
    public Label NameLabel { get; private set; }
    [Export]
    public Label CreatorLabel { get; private set; }
    [Export]
    public Label VersionLabel { get; private set; }
    [Export]
    public TextureRect External { get; private set; }

    public List<(Conflict, string)> messages = new();
    public IReadOnlyList<(Conflict, string)> Messages => messages;

    public Conflict ConflictType { get; private set; }

    public void Normal()
    {
        messages.Clear();
        ConflictType = Conflict.None;
        AddThemeStyleboxOverride("focus", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/BlueHighlight.tres"));
        AddThemeStyleboxOverride("hover", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/BlueHighlight.tres"));
        AddThemeStyleboxOverride("normal", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Blue.tres"));
        AddThemeStyleboxOverride("pressed", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Blue.tres"));
    }

    public void Warn(string message)
    {
        messages.Add((Conflict.Warning, message));
        if ((int) ConflictType > (int) Conflict.Warning)
            return;
        ConflictType = Conflict.Warning;
        AddThemeStyleboxOverride("focus", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/YellowHighlight.tres"));
        AddThemeStyleboxOverride("hover", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/YellowHighlight.tres"));
        AddThemeStyleboxOverride("normal", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Yellow.tres"));
        AddThemeStyleboxOverride("pressed", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Yellow.tres"));
    }

    public void Error(string message)
    {
        messages.Add((Conflict.Error, message));
        ConflictType = Conflict.Error;
        AddThemeStyleboxOverride("focus", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/RedHighlight.tres"));
        AddThemeStyleboxOverride("hover", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/RedHighlight.tres"));
        AddThemeStyleboxOverride("normal", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Red.tres"));
        AddThemeStyleboxOverride("pressed", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Red.tres"));
    }

    public override Variant _GetDragData(Vector2 pos)
    {
        // Data is all pressed mods in viewer
        ModEntry dupe = (ModEntry) Duplicate();
        dupe.ButtonPressed = false;
        dupe.CustomMinimumSize = Size;
        SetDragPreview(dupe);
        return this;
    }

    public override bool _CanDropData(Vector2 pos, Variant data) => CanDrop;
    public override void _DropData(Vector2 pos, Variant data)
    {
        ModEntry entry = (ModEntry) data;
        if (entry.GetParent() != GetParent())
            GetParent<DropList>()._DropData(Position, data);
        Node parent = entry.GetParent();
        parent.MoveChild(entry, GetIndex());
    }

    public override string ToString() => Mod.ID;

    public enum Conflict
    {
        None = 0,
        Warning = 1,
        Error = 2
    }
}
