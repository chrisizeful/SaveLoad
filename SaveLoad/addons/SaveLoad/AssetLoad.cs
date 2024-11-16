using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using Godot;
using System;

namespace SaveLoad;

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

    // Load all assets in directory path asynchronously
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

    public void LoadFile(string file)
    {
        Error error = ResourceLoader.LoadThreadedRequest(file, "", true);
        if (error != Error.Ok)
            GD.PrintErr($"AssetLoad#LoadFile: Failed to load file at {file} ({error})");
    }

    public async Task<bool> LoadResourcePack(string file)
    {
        return await Task.Run(() => {
            bool loaded = ProjectSettings.LoadResourcePack(file);
            if (!loaded)
                GD.PrintErr($"AssetLoad#LoadResourcePack: Failed to load PCK file at {file}");
            return loaded;
        });
    }

    public async Task<bool[]> LoadResourcePacks(string folder)
    {
        List<string> files = Files.ListFiles(folder, ".pck", true);
        List<Task<bool>> tasks = new();
        foreach (string file in files)
            tasks.Add(LoadResourcePack(file));
        return await Task.WhenAll(tasks.ToArray());
    }

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

public interface IAssetLoadListener
{

    public void AssetProgress(int loaded, int total, float progress) {}
    public void AssetComplete() {}
}
