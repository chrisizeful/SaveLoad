using Godot;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SaveLoad;

/// <summary>
/// Contains a variety of utility functions for easing interactions with files.
/// </summary>
public static class Files
{

    private static readonly Dictionary<string, string> _cache = new();

    /// <summary>
    /// Returns the contents of a text file.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="cacheText">If the result should be cached for future use.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetAsText(string path, bool cacheText = true)
    {
        if (_cache.TryGetValue(path, out var text))
            return text;
        FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
            GD.PrintErr($"Files#GetAsText: Failed to open file at \"{path}\" ({FileAccess.GetOpenError()})");
        text = file.GetAsText();
        if (cacheText)
            _cache[path] = text;
        file.Dispose();
        return text;
    }

    /// <summary>
    /// Removes text that was previously cached by <see cref="GetAsText"/>.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>If the text existed and was removed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove(string path) => _cache.Remove(path);

    /// <summary>
    /// Clear all text previously cached by <see cref="GetAsText"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearCache() => _cache.Clear();

    /// <summary>
    /// Fetch the fully qualified path to all files in a folder, optionally specifying a type.
    /// </summary>
    /// <param name="path">The path of the root directory to check.</param>
    /// <param name="type">The type of files to check for. Can include '.' or just be the extension.</param>
    /// <param name="recursive">Whether to rescursively check all sub directories.</param>
    /// <returns></returns>
    public static List<string> ListFiles(string path, string type = null, bool recursive = false)
    {
        List<string> files = new();
        DirAccess dir = DirAccess.Open(path);
        if (dir == null)
        {
            GD.PrintErr($"Files#ListFiles: Failed to open directory at \"{path}\" ({DirAccess.GetOpenError()})");
            return files;
        }
        dir.IncludeHidden = false;
        dir.IncludeNavigational = false;
        AddFiles(dir, type, recursive, files);
        dir.Dispose();
        return files;
    }

    private static void AddFiles(DirAccess dir, string type, bool recursive, List<string> files)
    {
        if (dir.ListDirBegin() != Error.Ok)
            return;
        string next = dir.GetNext();
        while (next != "")
        {
            string path = dir.GetCurrentDir() + "/" + next;
            // Check for directories
            if (recursive && dir.CurrentIsDir())
            {
                DirAccess sub = DirAccess.Open(path);
                sub.IncludeHidden = false;
                sub.IncludeNavigational = false;
                AddFiles(sub, type, recursive, files);
                sub.Dispose();
            }
            // Add relevant files
            else if (!dir.CurrentIsDir())
            {
#if !DEBUG
                // Trim export .remap extension
                if (path.EndsWith(".remap"))
                    path = path.TrimSuffix(".remap");
                if (path.EndsWith(".import"))
                    path = path.TrimSuffix(".import");
#endif
                if (type == null || path.EndsWith(type))
                    files.Add(path);
            }
            next = dir.GetNext();
        }
        dir.ListDirEnd();
    }

    /// <summary>
    /// Return all sub directories in a folder.
    /// </summary>
    /// <param name="path">The path of the root directory to check.</param>
    /// <param name="recursive">Whether to rescursively check all sub directories.</param>
    /// <returns></returns>
    public static List<string> ListDirs(string path, bool recursive = true)
    {
        List<string> directories = new();
        DirAccess dir = DirAccess.Open(path);
        if (dir == null)
        {
            GD.PrintErr($"Files#ListDirs: Failed to open directory at \"{path}\" ({DirAccess.GetOpenError()})");
            return directories;
        }
        dir.IncludeHidden = false;
        dir.IncludeNavigational = false;
        AddDirs(dir, directories, recursive);
        dir.Dispose();
        return directories;
    }

    private static void AddDirs(DirAccess dir, List<string> directories, bool recursive)
    {
        if (dir.ListDirBegin() != Error.Ok)
            return;
        string next = dir.GetNext();
        while (next != "")
        {
            string path = $"{dir.GetCurrentDir()}/{next}";
            // Only care for directories
            if (dir.CurrentIsDir())
            {
                directories.Add(path);
                if (recursive)
                {
                    DirAccess sub = DirAccess.Open(path);
                    sub.IncludeHidden = false;
                    sub.IncludeNavigational = false;
                    AddDirs(sub, directories, recursive);
                    sub.Dispose();
                }
            }
            next = dir.GetNext();
        }
        dir.ListDirEnd();
    }

    /// <summary>
    /// Deletes a directory and all of it's contents, optioanlly keeping the root folder.
    /// </summary>
    /// <param name="path">The path of the directory to delete.</param>
    /// <param name="self">Whether or not to delete the root folder.</param>
    public static void DeleteDir(string path, bool self = true)
    {
        if (!DirAccess.DirExistsAbsolute(path))
            return;
        DirAccess dir = DirAccess.Open(path);
        foreach (string file in ListFiles(path, null, true))
            dir.Remove(file);
        foreach (string folder in ListDirs(path))
            dir.Remove(folder);
        if (self)
            dir.Remove(path);
        dir.Dispose();
    }

    /// <summary>
    /// Write the text to the file at path, overwriting any existing content in the file.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="text"></param>
    public static void Write(string path, string text)
    {
        if (!MakeDirRecursive(path))
            return;
        FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
            GD.PrintErr($"Files#Write", $"Failed to open file at \"{path}\" ({FileAccess.GetOpenError()})");
        file.StoreString(text);
        file.Dispose();
    }

    /// <summary>
    /// Safely wraps the DirAcess.MakeDirRecursiveAbsolute method.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static bool MakeDirRecursive(string path)
    {
        string dir = path.GetBaseDir();
        if (DirAccess.DirExistsAbsolute(dir))
            return true;
        Error err = DirAccess.MakeDirRecursiveAbsolute(dir);
        if (err != Error.Ok)
            GD.PrintErr("Files#Write", $"Failed to create directory at \"{path}\" ({err})");
        return err == Error.Ok;
    }
}
