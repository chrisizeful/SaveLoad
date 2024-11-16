using Godot;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SaveLoad;

public partial class ModViewer : Control
{

    List<Mod> loaded = new();
    public IReadOnlyList<Mod> Loaded => loaded;

    public ModPreview Preview => (ModPreview) FindChild("Preview");
    public DropList Enabled => (DropList) FindChild("Enabled");
    public DropList Disabled => (DropList) FindChild("Disabled");
    public LineEdit SearchEnabled => (LineEdit) FindChild("SearchEnabled");
    public LineEdit SearchDisabled => (LineEdit) FindChild("SearchDisabled");

    public override void _Ready()
    {
        base._Ready();
        // Connect signals
        Button apply = new()
        {
            Text = "UI_Save"
        };
        apply.AddThemeStyleboxOverride("normal", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/Green.tres"));
        apply.AddThemeStyleboxOverride("hover", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/GreenHighlight.tres"));
        apply.AddThemeStyleboxOverride("focus", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/GreenHighlight.tres"));
        apply.AddThemeStyleboxOverride("pressed", ResourceLoader.Load<StyleBox>("res://addons/ModViewer/assets/theme/GreenHighlight.tres"));
        apply.Pressed += () => {
            /* TODO
            Settings.Instance.SetImmediate("Game", "Mods", Mod.FormattedList(Enabled.Mods));
            Settings.Instance.Save();
            MainMenu menu = this.FindParent<MainMenu>();
            menu.QueueFree();
            SaveLoad.Instance.Unload(SaveLoad.Instance.Mods.ToArray());
            GetTree().Root.AddChild(ResourceLoader.Load<PackedScene>("res://scenes/ui/loading/LoadingMods.tscn").Instantiate());
            */
        };
        apply.CustomMinimumSize = new Vector2(256, 0);
        // TODO FindChild("HeaderContainer").AddChild(apply);
        SearchEnabled.TextChanged += (text) => Enabled.Search(text);
        SearchDisabled.TextChanged += (text) => Disabled.Search(text);
        ((Button) FindChild("Refresh")).Pressed += OnRefreshPressed;
        ((Button) FindChild("Import")).Pressed += OnImportPressed;
        ((Button) FindChild("Export")).Pressed += OnExportPressed;
        // Refresh, set enabled to currently loaded mods
        foreach (Mod mod in SaveLoad.Instance.Mods)
        {
            loaded.Add(mod);
            Enabled.AddChild(Entry(mod));
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
        var paths = Files.ListDirs(SaveLoad.ModDir, false);
        paths.RemoveAll(p => loaded.FindIndex(m => m.Directory == p) != -1);
        foreach (string path in paths)
        {
            Mod mod = SaveLoad.Instance.Load<Mod>($"{path}/meta/Metadata.json");
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
        paths.ForEach(p => Disabled.AddChild(Entry(loaded.Find(m => m.Directory == p))));
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
            int compare = entry.Mod.Version;
            if (compare != 0)
                entry.Warn($"Current game version {SaveLoad.Instance.GameVersion} is {(compare < 0 ? "older" : "newer")}");
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
        dialog.FileSelected += (string path) => Mod.SaveList(Enabled.Mods, path);
        dialog.VisibilityChanged += () => { if (!dialog.Visible) dialog.QueueFree(); };
    }

    FileDialog GetDialog(string title, FileDialog.FileModeEnum mode)
    {
        FileDialog dialog = new FileDialog();
        dialog.Title = title;
        dialog.FileMode = mode;
        dialog.MinSize = dialog.MaxSize = new Vector2I(1280, 720);
        dialog.ModeOverridesTitle = false;
        dialog.Unresizable = true;
        dialog.Access = FileDialog.AccessEnum.Filesystem;
        GetParent().AddChild(dialog);
        dialog.PopupCentered();
        return dialog;
    }
}