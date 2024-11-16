using Godot;

namespace SaveLoad;

public class ModPacker
{
    
    private readonly string _mod;
    private PckPacker _packer;

    public ModPacker(string mod) => _mod = mod;

    public bool Start(string name = default)
    {
        DirAccess dir = DirAccess.Open(SaveLoad.PackParentDir);
        if (!Files.MakeDirRecursive(SaveLoad.PackSubdir))
            return false;
        _packer = new PckPacker();
        _packer.PckStart($"{SaveLoad.PackDir}{Name(_mod, name)}.pck");
        return true;
    }
    
    public void AddFolder(string folder)
    {
        DirAccess dir = DirAccess.Open(_mod);
        if (!dir.DirExists(folder))
            return;
        foreach(string file in Files.ListFiles($"{_mod}/{folder}", null, true))
        {
            int idx = file.IndexOf(folder);
            _packer.AddFile($"{SaveLoad.PackParentDir}{file.Substr(idx, file.Length - idx)}", file);
        }
    }

    public void Save() => _packer.Flush();

    public static void Pack(string mod, string filename = default, params string[] folders)
    {
        ModPacker packer = new ModPacker(mod);
        packer.Start(filename);
        foreach (string folder in folders)
            packer.AddFolder(folder);
        packer.Save();
    }

    public static bool IsPacked(string mod, string name = default) => Godot.FileAccess.FileExists($"{SaveLoad.PackDir}{Name(mod, name)}.pck");

    private static string Name(string mod, string name)
    {
        return Equals(name, default(string)) ? System.IO.Path.GetFileName(mod) : name;
    }
}
