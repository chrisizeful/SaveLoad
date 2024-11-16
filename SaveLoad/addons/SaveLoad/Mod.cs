using System.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SaveLoad;

public sealed record Mod
{

    // Paths
    [JsonIgnore]
    public string Directory;
    [JsonIgnore]
    public string AssemblyDir => $"{Directory}/assembly";
    [JsonIgnore]
    public string DefDir => $"{Directory}/defs";
    [JsonIgnore]
    public string MetaDir => $"{Directory}/meta";
    [JsonIgnore]
    public string MetaIcon => $"{MetaDir}/Icon.png";
    [JsonIgnore]
    public string MetaData => $"{MetaDir}/Metadata.json";

    // Metadata
    public string ID;
    public string Name, Creator;
    public string Description;
    public Version ModVersion, GameVersion;
    public List<Dependency> Dependencies { get; } = new();
    public List<Dependency> Incompatible { get; } = new();
    public bool SteamMod;

    // Loaded
    [JsonIgnore]
    public List<string> Defs { get; } = new();
    [JsonIgnore]
    public List<Assembly> Assemblies { get; } = new();

    public int Version
    {
        get => GameVersion.CompareTo(SaveLoad.Instance.GameVersion);
    }

    public static string[] LoadList(string path) => FromList(Files.GetAsText(path, false));
    public static string[] FromList(string text) => text.Split("\n");
    public static void SaveList(IEnumerable<Mod> mods, string path) => Files.Write(path, FormattedList(mods));
    public static string FormattedList(IEnumerable<Mod> mods) => string.Join('\n', mods);

    public sealed record Dependency
    {

        public string Name, Creator;
        public Version ModVersion;

        public bool Match(Mod mod) => mod.Name == Name && mod.Creator == Creator;
        public int Version(Mod mod) => mod.ModVersion.CompareTo(ModVersion);
    }
}
