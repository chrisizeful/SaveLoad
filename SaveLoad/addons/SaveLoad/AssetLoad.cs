using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using Godot;
using System;

namespace SaveLoad;

/// <summary>
/// A singleton helper class for asynchronously loading assets.
/// </summary>
public class AssetLoad
{

    public static AssetLoad Instance
    {
        get
        {
            _instance ??= new AssetLoad();
            return _instance;
        }
    }
    private static AssetLoad _instance;

    private readonly Godot.Collections.Array _progress = new();

    private AssetLoad() {}

    /// <summary>
    /// Load all assets in a directory path asynchronously.
    /// </summary>
    /// <param name="path">The path of the directory.</param>
    /// <returns></returns>
    public async Task<List<string>> Load(string path)
    {
        return await Task.Run(() => {
            var files = Files.ListFiles(path, null, true);
            files.ForEach(async f => {
                string ext = f.GetExtension();
                if (ext == "dll")
                    await LoadAssembly(f);
                else if (ext == "pck")
                    await LoadResourcePack(f);
                else
                    LoadFile(f);
            });
            files.RemoveAll(f => f.GetExtension() == "pck");
            files.RemoveAll(f => f.GetExtension() == "pck");
            return files;
        });
    }

    /// <summary>
    /// Load an asset at a path asynchronously.
    /// </summary>
    /// <param name="file">The path of the file.</param>
    public void LoadFile(string file)
    {
        Error error = ResourceLoader.LoadThreadedRequest(file, "", true);
        if (error != Error.Ok)
            GD.PrintErr($"AssetLoad#LoadFile: Failed to load file at {file} ({error})");
    }

    /// <summary>
    /// Load an asset pack (.pck) at a path asynchronously.
    /// </summary>
    /// <param name="file">The path to the .pck file.</param>
    /// <returns></returns>
    public async Task<bool> LoadResourcePack(string file)
    {
        return await Task.Run(() => {
            bool loaded = ProjectSettings.LoadResourcePack(file);
            if (!loaded)
                GD.PrintErr($"AssetLoad#LoadResourcePack: Failed to load PCK file at {file}");
            return loaded;
        });
    }

    /// <summary>
    /// Load all asset packs (.pck) in a folder asynchronously.
    /// </summary>
    /// <param name="folder">The path to the root directory.</param>
    /// <returns></returns>
    public async Task<bool[]> LoadResourcePacks(string folder)
    {
        List<string> files = Files.ListFiles(folder, ".pck", true);
        List<Task<bool>> tasks = new();
        foreach (string file in files)
            tasks.Add(LoadResourcePack(file));
        return await Task.WhenAll(tasks.ToArray());
    }

    /// <summary>
    /// Loads an assembly asynchronously.
    /// </summary>
    /// <param name="file">The fullly qualified path to an assembly (.dll).</param>
    /// <returns></returns>
    public async Task<Assembly> LoadAssembly(string file)
    {
        return await Task.Run(() => {
            try
            {
                return Assembly.LoadFile(file);
            }
            catch (Exception e)
            {
                GD.PrintErr($"AssetLoad#LoadAssembly: Failed to load Assembly at {file} ({e.Message})");
            }
            return null;
        });
    }

    /// <summary>
    /// Can be called each frame to (i.e. in Process()) to hook into callbacks and update a loading screen.
    /// </summary>
    /// <param name="files">The list of files that were requested to be loaded.</param>
    /// <param name="listener">An IAssetLoadListener that listens to callbacks.</param>
    public void GetStatus(List<string> files, IAssetLoadListener listener)
    {
        if (files == null || files.Count == 0)
        {
            listener.AssetComplete();
            return;
        }
        int loaded = 0;
        float current = 0.0f;
        for (int i = files.Count - 1; i >= 0; i--)
        {
            string file = files[i];
            var status = ResourceLoader.LoadThreadedGetStatus(file, _progress);
            if (status == ResourceLoader.ThreadLoadStatus.InvalidResource  ||
                status == ResourceLoader.ThreadLoadStatus.Failed)
            {
                files.RemoveAt(i);
                GD.PrintErr($"AssetLoad#GetStatus: Failed loading asset at {files[i]}");
            }
            else if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                loaded++;
            current += (float) _progress[0];
        }
        listener.AssetProgress(loaded, files.Count, current);
        if (current == files.Count)
            listener.AssetComplete();
    }
}

/// <summary>
/// Used by <see cref="AssetLoad.GetStatus(List{string}, IAssetLoadListener)"/> to get the status
/// of asynchronously loaded assets.
/// </summary>
public interface IAssetLoadListener
{

    /// <summary>
    /// Called continuously.
    /// </summary>
    /// <param name="loaded">The number of assets that have finished loading.</param>
    /// <param name="total">The total number of assets that were requested to load.</param>
    /// <param name="progress">The percent of assets that have finished loading.</param>
    public void AssetProgress(int loaded, int total, float progress) {}
    /// <summary>
    /// Called when all assets have finished loading.
    /// </summary>
    public void AssetComplete() {}
}
