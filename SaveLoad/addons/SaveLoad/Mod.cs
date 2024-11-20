using System.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SaveLoad;

/// <summary>
/// Defines a loaded mod.
/// </summary>
public sealed record Mod
{

    /// <summary>
    /// The root directory.
    /// </summary>
    [JsonIgnore]
    public string Directory;
    /// <summary>
    /// The subdirectory containg any assemblies.
    /// </summary>
    [JsonIgnore]
    public string AssemblyDir => $"{Directory}/assembly";
    /// <summary>
    /// The subdirectory containg any json Defs.
    /// </summary>
    [JsonIgnore]
    public string DefDir => $"{Directory}/defs";
    /// <summary>
    /// The subdirectory containg metadata.
    /// </summary>
    [JsonIgnore]
    public string MetaDir => $"{Directory}/meta";
    /// <summary>
    /// The path to the icon png.
    /// </summary>
    [JsonIgnore]
    public string MetaIcon => $"{MetaDir}/Icon.png";
    /// <summary>
    /// The path to the json Metadata file.
    /// </summary>
    [JsonIgnore]
    public string MetaData => $"{MetaDir}/Metadata.json";

    /// <summary>
    /// A unique string identifier.
    /// </summary>
    public string ID;
    /// <summary>
    /// The display name - does not have to be unique.
    /// </summary>
    public string Name;

    public string Creator;
    public string Description;

    /// <summary>
    /// The current version of the mod.
    /// </summary>
    public Version ModVersion;
    /// <summary>
    /// The version of the game this mod is compatible with.
    /// </summary>
    public Version GameVersion;
    /// <summary>
    /// Other mod requirements for this mod to function properly. While not a strict requirement,
    /// defining dependencies will help users configure a mod list.
    /// </summary>
    public List<Dependency> Dependencies { get; } = new();
    /// <summary>
    /// Mods that this mod does not work with. If a mod list contains any of the mods in this list, this
    /// mod will not be loaded.
    /// </summary>
    public List<Dependency> Incompatible { get; } = new();
    /// <summary>
    /// If this mod was installed locally or from another method (i.e. Steam).
    /// </summary>
    public bool Local;

    /// <summary>
    /// All defs this mod defines.
    /// </summary>
    [JsonIgnore]
    public List<string> Defs { get; } = new();
    /// <summary>
    /// All assemblies this mod loaded.
    /// </summary>
    [JsonIgnore]
    public List<Assembly> Assemblies { get; } = new();

    /// <summary>
    /// An integer comparision between the game version this mod was made for and the
    /// current game version.
    /// </summary>
    public int VersionCompare
    {
        get => SaveLoad.Instance.GameVersion.CompareTo(GameVersion);
    }

    /// <summary>
    /// Loads a list of mod paths from the <see cref="path"/>.
    /// </summary>
    public static string[] LoadList(string path) => FromList(Files.GetAsText(path, false));
    /// <summary>
    /// Given a valid mod list <see cref="text"/>, returns the mod paths as an array.
    /// </summary>
    public static string[] FromList(string text) => text.Split("\n");
    /// <summary>
    /// Save a list of mods to a text file at path.
    /// </summary>
    public static void SaveList(IEnumerable<Mod> mods, string path) => Files.Write(path, FormattedList(mods));
    /// <summary>
    /// Format a list of mods to a string.
    /// </summary>
    public static string FormattedList(IEnumerable<Mod> mods) => string.Join('\n', mods);

    /// <summary>
    /// Defines a mod dependency or incompatibility.
    /// </summary>
    public sealed record Dependency
    {

        public string ID;
        public string Name, Creator;
        public Version ModVersion;

        public bool Match(Mod mod) => mod.Name == Name && mod.Creator == Creator;
        public int Version(Mod mod) => mod.ModVersion.CompareTo(ModVersion);
    }
}
