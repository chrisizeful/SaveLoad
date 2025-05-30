using Godot;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SaveLoad;

// TODO This should ideally be split into two parts:
// - One that is solely the UI.
// - One that is just the implementation and can be used to implement different UIs.
public partial class ModViewer : Control
{

    List<Mod> loaded = [];
    public IReadOnlyList<Mod> Loaded => loaded;

    [Export]
    public ModPreview Preview { get; private set; }
    [Export]
    public DropList Enabled { get; private set; }
    [Export]
    public DropList Disabled { get; private set; }
    [Export]
    public LineEdit SearchEnabled { get; private set; }
    [Export]
    public LineEdit SearchDisabled { get; private set; }

    [Export]
    public Button Refresh { get; private set; }
    [Export]
    public Button Reload { get; private set; }
    [Export]
    public Button Import { get; private set; }
    [Export]
    public Button Export { get; private set; }

    [ExportGroup("Dialog")]
    [Export]
    public int DialogWidth = 800;
    [Export]
    public int DialogHeight = 600;

    public override void _Ready()
    {
        base._Ready();
        // Connect signals
        SearchEnabled.TextChanged += Enabled.Search;
        SearchDisabled.TextChanged += Disabled.Search;
        Refresh.Pressed += OnRefreshPressed;
        Import.Pressed += OnImportPressed;
        Export.Pressed += OnExportPressed;
        // Refresh, set enabled to currently loaded mods
        foreach (Mod mod in SaveLoader.Instance.Mods)
        {
            loaded.Add(mod);
            ModEntry entry = Entry(mod);
            Enabled.AddChild(entry);
            EmitSignal(SignalName.EntryAdded, entry);
        }
        OnRefreshPressed();
    }

    public void SetEnabled(params Mod[] mods)
    {
        foreach (Node node in Enabled.GetChildren())
            node.Reparent(Disabled, false);
        foreach (Mod mod in mods)
        {
            ModEntry entry = Disabled.Entries.First(e => e.Mod == mod);
            entry.Reparent(Enabled, false);
        }
        Dependencies();
    }

    void OnRefreshPressed()
    {
        // Load mods
        var paths = Files.ListDirs(SaveLoader.ModDir, false);
        paths.RemoveAll(p => loaded.FindIndex(m => m.Directory == p) != -1);
        foreach (string path in paths)
        {
            Mod mod = SaveLoader.Instance.Load<Mod>($"{path}/meta/Metadata.json");
            mod.Directory = path;
            loaded.Add(mod);
        }
        // Free invalid entries
        foreach (ModEntry entry in Enabled.Entries.Concat(Disabled.Entries))
        {
            if (!loaded.Contains(entry.Mod)) entry.QueueFree();
            else paths.Remove(entry.Mod.Directory);
        }
        // Create new entries
        paths.ForEach(p => {
            ModEntry entry = Entry(loaded.Find(m => m.Directory == p));
            Disabled.AddChild(entry);
            EmitSignal(SignalName.EntryAdded, entry);
        });
        // Apply searches
        Disabled.Search(SearchDisabled.Text);
        Enabled.Search(SearchEnabled.Text);
        // Check dependencies and incompatibles
        Dependencies();
    }

    public void Dependencies()
    {
#nullable enable
        IEnumerable<ModEntry> entries = Enabled.Entries.Concat(Disabled.Entries);
        foreach (ModEntry entry in entries)
        {
            entry.Normal();
            // Check game version
            int compare = entry.Mod.VersionCompare;
            if (compare != 0)
                entry.Warn($"Current game version {SaveLoader.Instance.GameVersion} is {(compare < 0 ? "older" : "newer")}");
            // Check dependencies exist, are enabled, and are the correct version
            foreach (Mod.Dependency dependency in entry.Mod.Dependencies)
            {
                ModEntry? found = entries.FirstOrDefault(e => dependency.Match(e.Mod));
                if (found == default(ModEntry))
                {
                    entry.Error($"Dependency {dependency.Name} by {dependency.Creator} is missing");
                }
                else
                {
                    // Dependent is enabled and entry is not
                    if (found.GetParent() != Enabled && entry.GetParent() == Enabled)
                        entry.Error($"Dependency {dependency.Name} by {dependency.Creator} is not enabled");
                    compare = dependency.Version(found.Mod);
                    if (compare != 0)
                        entry.Warn($"Dependency {dependency.Name} by {dependency.Creator} is {(compare < 0 ? "an older" : "a newer")} version ({found.Mod.ModVersion}), requires version " + dependency.ModVersion);
                }
            }
            // Check incompatibles aren't enabled
            foreach (Mod.Dependency incompatible in entry.Mod.Incompatible)
            {
                ModEntry? found = Enabled.Entries.FirstOrDefault(e => incompatible.Match(e.Mod));
                if (found != default(ModEntry))
                    entry.Error($"Incompatible mod {incompatible.Name} by {incompatible.Creator} enabled");
            }
        }
#nullable disable
    }

    ModEntry Entry(Mod mod)
    {
        ModEntry entry = ResourceLoader.Load<PackedScene>("res://addons/ModViewer/ModEntry.tscn").Instantiate<ModEntry>();
        entry.FocusExited += () => entry.ButtonPressed = false;
        entry.Toggled += (toggled) => {
            if (toggled)
                Preview.Set(entry);
        };
        entry.GuiInput += (@event) => {
            if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.DoubleClick)
            {
                Node parent = Enabled.Entries.Contains(entry) ? Disabled : Enabled;
                entry.Reparent(parent, false);
                parent.MoveChild(entry, parent.GetChildCount());
                Dependencies();
            }
        };
        entry.Mod = mod;
        return entry;
    }

    void OnImportPressed()
    {
        FileDialog dialog = GetDialog("Select a mod list", FileDialog.FileModeEnum.OpenFile);
        dialog.FileSelected += (string path) => {
            string[] ids = Mod.LoadList(path);
            List<Mod> enabled = loaded.FindAll(m => Array.FindIndex(ids, id => id == m.ID) != -1);
            SetEnabled(enabled.ToArray());
        };
        dialog.VisibilityChanged += () => { if (!dialog.Visible) dialog.QueueFree(); };
    }

    void OnExportPressed()
    {
        FileDialog dialog = GetDialog("Select a save file", FileDialog.FileModeEnum.SaveFile);
        dialog.FileSelected += (path) => Mod.SaveList(Enabled.Mods, path);
        dialog.VisibilityChanged += () => {
            if (!dialog.Visible)
                dialog.QueueFree();
        };
    }

    FileDialog GetDialog(string title, FileDialog.FileModeEnum mode)
    {
        FileDialog dialog = new()
        {
            Title = title,
            FileMode = mode,
            MinSize = new(DialogWidth, DialogHeight),
            MaxSize = new(DialogWidth, DialogHeight),
            ModeOverridesTitle = false,
            Unresizable = false,
            Access = FileDialog.AccessEnum.Filesystem
        };
        GetParent().AddChild(dialog);
        dialog.PopupCentered();
        return dialog;
    }

    [Signal]
    public delegate void EntryAddedEventHandler(ModEntry entry);
}
