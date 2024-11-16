using Godot;
using System.Collections.Generic;
using System.Linq;

namespace SaveLoad;

public partial class DropList : Control
{

    [Export]
    public ModViewer Viewer { get; private set; }

    List<ModEntry> entries = new();
    public IReadOnlyList<ModEntry> Entries => entries;
    public IReadOnlyList<Mod> Mods => Entries.Select(e => e.Mod).ToList();

    public override void _Ready()
    {
        ChildEnteredTree += (child) => entries.Add((ModEntry) child);
        ChildExitingTree += (child) => entries.Remove((ModEntry) child);
    }

    // Search name and creator
    public void Search(string text)
    {
        List<ModEntry> results = entries;
        if (text.Length != 0)
        {
            // Search using mod name and creator name as criteria
            results = entries.FindAll(m => (m.Mod.Name.ToLower() + m.Mod.Creator.ToLower()).Contains(text.ToLower()));
            entries.ForEach(e => e.Visible = false);
        }
        results.ForEach(e => e.Visible = true);
        Viewer.Preview.Set(results.Count > 0 ? results[0] : null);
    }

    public override bool _CanDropData(Vector2 pos, Variant data) => true;
    public override void _DropData(Vector2 pos, Variant data)
    {
        ModEntry entry = (ModEntry) data;
        if (entry.GetParent() == this)
            return;
        entry.Reparent(this, false);
        MoveChild(entry, GetChildCount());
        Viewer.Dependencies();
    }
}