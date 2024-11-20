using Godot;

namespace SaveLoad;

/// <summary>
/// A helper class for packing files for a mod.
/// </summary>
public class ModPacker
{
    
    private readonly string mod;
    private PckPacker packer;

    public ModPacker(string mod) => this.mod = mod;

    /// <summary>
    /// Begin packing the files at into a .pck at the path filename.
    /// </summary>
    /// <param name="filename">The fully qualified path of a .pck to save to.</param>
    /// <returns>If the directory at filename was successfully created.</returns>
    public bool Start(string filename = default)
    {
        if (!Files.MakeDirRecursive(SaveLoad.PackSubdir))
            return false;
        packer = new PckPacker();
        packer.PckStart($"{SaveLoad.PackDir}{Name(mod, filename)}.pck");
        return true;
    }
    
    /// <summary>
    /// Recursively add an entire folder of files to the .pck.
    /// </summary>
    /// <param name="folder"></param>
    public void AddFolder(string folder)
    {
        DirAccess dir = DirAccess.Open(mod);
        if (dir == null || !dir.DirExists(folder))
            return;
        foreach(string file in Files.ListFiles($"{mod}/{folder}", null, true))
        {
            int idx = file.IndexOf(folder);
            packer.AddFile($"{SaveLoad.PackParentDir}{file.Substr(idx, file.Length - idx)}", file);
        }
    }

    /// <summary>
    /// Finish packing files - must be called to save.
    /// </summary>
    public void Save() => packer.Flush();

    /// <summary>
    /// Helper method to quickly pack a mod.
    /// </summary>
    /// <param name="mod">The name of the mod.</param>
    /// <param name="filename">Optionally specify a filename different from the mod name.</param>
    /// <param name="folders">A list of folders to pack.</param>
    // TODO Allow folders to be empty/null to be pack all sub-directories
    public static void Pack(string mod, string filename = default, params string[] folders)
    {
        ModPacker packer = new(mod);
        packer.Start(filename);
        foreach (string folder in folders)
            packer.AddFolder(folder);
        packer.Save();
    }

    /// <summary>
    /// Checks whether a mod has already been packed. 
    /// </summary>
    /// <param name="mod">The name of the mod.</param>
    /// <param name="filename">Optionally specify a filename different from the mod name.</param>
    /// <returns></returns>
    public static bool IsPacked(string mod, string filename = default) => FileAccess.FileExists($"{SaveLoad.PackDir}{Name(mod, filename)}.pck");

    private static string Name(string mod, string filename)
    {
        return Equals(filename, default(string)) ? System.IO.Path.GetFileName(mod) : filename;
    }
}
