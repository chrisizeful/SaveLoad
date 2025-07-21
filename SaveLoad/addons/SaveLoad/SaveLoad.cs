using Godot;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;

namespace SaveLoad;

/// <summary>
/// The core singleton class for the SaveLoad API.
/// </summary>
public class SaveLoader
{

    /// <summary>
    /// The singleton instance.
    /// </summary>
    public static SaveLoader Instance
    {
        get
        {
            _instance ??= new SaveLoader();
            return _instance;
        }
    }
    private static SaveLoader _instance;

    /// <summary>
    /// Pre-configured JsonSerializer instance and settings will all default converters.
    /// </summary>
    public JsonSerializer Serializer { get; private set; }
    public JsonSerializerSettings Settings { get; private set; }

    private Dictionary<Type, List<Def>> defTypes { get; } = [];
    /// <summary>
    /// Loaded mods by type.
    /// </summary>
    public IReadOnlyDictionary<Type, List<Def>> DefTypes => defTypes;

    private Dictionary<string, Def> defNames { get; } = [];
    /// <summary>
    /// Loaded mods by their unique name.
    /// </summary>
    public IReadOnlyDictionary<string, Def> DefNames => defNames;

    private List<Mod> mods { get; } = [];
    /// <summary>
    /// All currently loaded mods.
    /// </summary>
    public IReadOnlyList<Mod> Mods => mods;

    Version gameVersion;
    /// <summary>
    /// The current version of the game - used to detect whether mods are compatible. Automatically
    /// set from the ProjectSettings "application/config/version" setting.
    /// </summary>
    public Version GameVersion
    {
        get
        {
            gameVersion ??= new(ProjectSettings.GetSetting("application/config/version", "1.0.0.0").AsString());
            return gameVersion;
        }
    }

    /// <summary>
    /// The parent directory where mod .pck files will be packed to. In the editor, this in in the project (res://)
    /// folder, and in exported projects this is next to the running executable.
    /// </summary>
    public static string PackParentDir
    {
        get
        {
            if (OS.HasFeature("editor"))
                return "res://";
            return Path.GetDirectoryName(OS.GetExecutablePath()).Replace('\\', '/') + "/";
        }
    }

    /// <summary>
    /// The directory relative to <see cref="PackParentDir"/> where mod .pck files will be packed to.
    /// </summary>
    public static string PackSubdir => "mods/packed/";
    /// <summary>
    /// The fully qualified directory to pack .pck files into.
    /// </summary>
    public static string PackDir => PackParentDir + PackSubdir;

    /// <summary>
    /// The directory where mods are stored. In the editor, this is in the parent directory alongside the project. In an
    /// exported project this is next to th erunning executable. In both cases, the folder name is "Mods".
    /// </summary>
    public static string ModDir
    {
        get
        {
            if (OS.HasFeature("editor"))
                return Directory.GetParent(Directory.GetParent(ProjectSettings.GlobalizePath("res://")).ToString()).ToString().Replace("\\", "/") + "/Mods/";
            return Path.GetDirectoryName(OS.GetExecutablePath()).Replace('\\', '/') + "/Mods/";
        }
    }

    private SaveLoader()
    {
        JsonConvert.DefaultSettings = () => Settings;
        Settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Converters = [
                new Int32Converter(),
                new AabbConverter(),
                new BasisConverter(),
                new ColorConverter(),
                new Texture2DConverter(),
                new ResourceConverter(),
                new StringNameConverter(),
                new NodeConverter(),
                new InstanceDefConverter(),
                new DefConverter(),
                new StringEnumConverter(),
                new VariantConverter(),
                new Vector2Converter(), new Vector2IConverter(),
                new Vector3Converter(), new Vector3IConverter(),
                new Vector4Converter(), new Vector4IConverter()
            ],
            ContractResolver = new IgnoreResolver([
                // Object
                "DynamicObject",
                "NativeInstance"
            ])
        };
        Serializer = JsonSerializer.CreateDefault();
    }
    
    /// <summary>
    /// Create a JsonSerializer with the default settings and converters.
    /// </summary>
    /// <returns></returns>
    public JsonSerializer CreateDefault()
    {
        _instance ??= new SaveLoader();
        return JsonSerializer.CreateDefault();
    }

    /// <summary>
    /// Serialize an object to json.
    /// </summary>
    /// <param name="data">The object to serialize.</param>
    /// <param name="formatting">Optional formatting specification.</param>
    /// <returns>A JSON representation of the object.</returns>
    public async Task<string> Save(object data, Formatting formatting = Formatting.None)
    {
        return await Task.Run(() => JsonConvert.SerializeObject(data, formatting));
    }

    /// <summary>
    /// Serailize an object to json and save it to a file.
    /// </summary>
    /// <param name="path">The path to save to.</param>
    /// <param name="data">The object to serialize.</param>
    /// <param name="formatting">Optional formatting specification.</param>
    public async void Save(string path, object data, Formatting formatting = Formatting.None)
    {
        if (Files.MakeDirRecursive(path))
            Files.Write(path, await Save(data, formatting));
    }

    /// <summary>
    /// Load an object from a path asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="path">The path of the JSON to load.</param>
    /// <returns>The deserialized object.</returns>
    public async Task<T> LoadAsync<T>(string path) => await Task.Run(() => Load<T>(path));

    /// <summary>
    /// Load an object from a path synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="path">The path of the JSON to load.</param>
    /// <returns>The deserialized object.</returns>
    public T Load<T>(string path, bool cache = false) => JsonConvert.DeserializeObject<T>(Files.GetAsText(path, cache));
    
    /// <summary>
    /// Load an object from a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="json">The JSON representation of an object.</param>
    /// <returns>The deserialized object.</returns>
    public T LoadJson<T>(string json) => JsonConvert.DeserializeObject<T>(json);

    /// <summary>
    /// Load a single def from json.
    /// </summary>
    /// <param name="json">A JSON string.</param>
    /// <returns></returns>
    public Def LoadDef(string json)
    {
        using JsonTextReader reader = new(new StringReader(json));
        if (reader.Read())
            return LoadDef(JObject.Load(reader));
        return null;
    }

    /// <summary>
    /// Load single def using a JObject synchronously.
    /// </summary>
    /// <param name="jo">The JObject storing the Def data.</param>
    /// <returns>The deserialized Def.</returns>
    public Def LoadDef(JObject jo)
    {
        Def def = null;
        try 
        {
            using JsonReader jreader = jo.CreateReader();
            def = (Def) Serializer.Deserialize(jreader, Type.GetType(jo["$type"].Value<string>()));
        }
        catch (Exception e)
        {
#nullable enable
            string? name = jo["Name"]?.Value<string?>();
#nullable disable
            throw new Exception($"Error loading def \"{name}\": {e.Message}");
        }
        Type type = def.GetType();
        // Check if def has a base
        if (def.Base != null)
        {
            // Verify def base exists
            if (!defNames.TryGetValue(def.Base, out var defBase))
                throw new Exception($"Database does not contain def for base of \"{def.Name}\" with the name \"{def.Base}\"");
            // Copy data from base def
            foreach (PropertyInfo baseProp in defBase.GetType().GetProperties())
            {
                PropertyInfo defProp = type.GetProperty(baseProp.Name);
                // Skip copying abstract property
                if (defProp.Name == "Abstract")
                    continue;
                // Skip setting prop if the def doesn't have it or if it is set in the def json
                if (defProp == null || jo.ContainsKey(defProp.Name))
                    continue;
                defProp.SetValue(def, baseProp.GetValue(defBase));
            }
        }
        // Add def to database
        if (!defNames.TryAdd(def.Name, def))
            throw new Exception($"Database already contains a def with the name \"{def.Name}\"");
        if (!defTypes.TryGetValue(type, out var defs))
            defs = defTypes[type] = new();
        defs.Add(def);
        return def;
    }

    /// <summary>
    /// Load a single Def asynchronously.
    /// </summary>
    /// <param name="json"></param>
    /// <returns>The deserialized Def.</returns>
    public async Task<Def> LoadDefAsync(string json)
    {
        using JsonTextReader reader = new JsonTextReader(new StringReader(json));
        if (reader.Read())
            return await LoadDefAsync(JObject.Load(reader));
        return null;
    }

    /// <summary>
    /// Load single def using a JObject asynchronously.
    /// </summary>
    /// <param name="jo">The JObject storing the Def data.</param>
    /// <returns>The deserialized Def.</returns>
    public async Task<Def> LoadDefAsync(JObject jo)
    {
        return await Task.Run(() => LoadDef(jo));
    }

    private List<ModObject> LoadJO(Mod mod, string file, bool cache = false)
    {
        List<ModObject> jos = new();
        using (var reader = new JsonTextReader(new StringReader(Files.GetAsText(file, cache))))
        {
            reader.SupportMultipleContent = true;
            while (reader.Read())
                jos.Add(new() { Owner = mod, Obj = JObject.Load(reader)});
        }
        return jos;
    }

    /// <summary>
    /// Loads all specified mods, or if enabled is not specified loads all mods in the mods folder.
    /// </summary>
    /// <param name="listener">Optional ISaveLoadListener for listening to callbacks.</param>
    /// <param name="folders">The list of sub-directories to include for each mod.</param>
    /// <param name="enabled">The list of mods to load, or nothing for all mods.</param>
    /// <exception cref="Exception">Thrown if a mods DLL failed to load.</exception>
    public async void Load(ISaveLoaderListener listener, string[] folders, params string[] enabled)
    {
        // Use all mods if enabled isn't specified
        if (enabled.Length == 0)
        {
            var dirs = Files.ListDirs(ModDir, false);
            for (int i = 0; i < dirs.Count; i++)
                dirs[i] = Path.GetFileName(dirs[i]);
            enabled = dirs.ToArray();
        }
        if (OS.HasFeature("editor"))
        {
            // Begin packing
            foreach (string mod in enabled)
                ModPacker.Pack($"{ModDir}{mod}", mod, folders);
            // Load mod packs
            foreach (string mod in enabled)
                await AssetLoad.Instance.LoadResourcePack($"{PackDir}{mod}.pck");
        }
        else
        {
            // Load packs recursively from mod folder
            await AssetLoad.Instance.LoadResourcePacks(ModDir);
        }
        // Load mod types
        foreach (string modPath in enabled)
        {
            string directory = $"{ModDir}{modPath}";
            Mod mod = await LoadAsync<Mod>($"{directory}/meta/Metadata.json");
            mod.Directory = directory;
            mods.Add(mod);
        }
        // Collect, order, and load defs
        await Task.Run(() => {
            List<ModObject> jos = new();
            foreach (Mod mod in mods)
            {
                if (!DirAccess.DirExistsAbsolute(mod.DefDir))
                {
                    GD.Print($"SaveLoad#Load: Def folder does not exist at \"{mod.DefDir}\", continuing.");
                    continue;
                }
                foreach (string file in Files.ListFiles(mod.DefDir, ".json", true))
                    jos.AddRange(LoadJO(mod, file));
            }
            var order = DependencyOrder(jos);
            for (int i = 0; i < order.Count; i++)
            {
                ModObject mo = order[i];
                listener?.Loading((string) mo.Obj["Name"]);
                Def def = LoadDef(mo.Obj);
                def.Owner = mo.Owner;
                def.Owner.Defs.Add(def.Name);
                listener?.Progress(i, order.Count);
            }
        });
        listener?.Complete();
        // Discard abstract defs and empty type lists
        foreach (Def def in defNames.Values.ToList().FindAll(d => d.Abstract))
        {
            defNames.Remove(def.Name);
            defTypes[def.GetType()].Remove(def);
        }
        foreach (Type type in defTypes.Keys.ToList().FindAll(t => defTypes[t].Count == 0))
            defTypes.Remove(type);
        // Load and startup assemblies
        foreach (Mod mod in mods)
        {
            // TODO await SteamWorkshop.StartTrack(mod);
            // Load and startup assemblies
            if (!DirAccess.DirExistsAbsolute(mod.AssemblyDir))
                continue;
            var files = Files.ListFiles(mod.AssemblyDir, ".dll", true);
            files.ForEach(async f => {
                try
                {
                    Assembly assembly = await AssetLoad.Instance.LoadAssembly(f);
                    mod.Assemblies.Add(assembly);
                }
                catch (Exception e)
                {
                    throw new Exception("AssetLoad: Failed to load the DLL assembly file ", e);
                }
            });
            // Invoke all static methods with StartupAttribute
            mod.Assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                .Select(method => (method, attribute: method.GetCustomAttribute<StartupAttribute>()))
                .Where(pair => pair.attribute != null)
                .ToList()
                .ForEach(pair => pair.method.Invoke(null, pair.attribute.Parameters));
        }
    }

    private List<ModObject> DependencyOrder(List<ModObject> jos)
    {
        JObjectGraph<ModObject> graph = new();
        foreach (ModObject mo in jos)
        {
            string name = (string) mo.Obj["Name"];
            graph.AddNode(name, mo);
            graph.CheckProperties(mo.Obj, (prop) => {
                if (!prop.Name.Contains("Def"))
                    return;
                if (prop.Value.Type == JTokenType.String)
                    graph.AddDependency(name, prop.Value.ToString());
                if (prop.Value.Type == JTokenType.Array)
                    foreach (JToken item in prop.Value.Children())
                        if (item.Type == JTokenType.String)
                            graph.AddDependency(name, item.Value<string>());
            });
        }
        return graph.DataOrder();
    }

    /// <summary>
    /// Unload mods.
    /// </summary>
    /// <param name="unload"></param>
    public void Unload(params Mod[] unload)
    {
        foreach (Mod mod in unload)
        {
            mod.Defs.ForEach(d => {
                Def def = defNames[d];
                Type type = def.GetType();
                defNames.Remove(d);
                if (defTypes.TryGetValue(type, out var defs))
                    defs.Remove(def);
            });
            mods.Remove(mod);
            // TODO await SteamWorkshop.StopTrack(mod);
        }
        foreach (Type t in defTypes.Keys.Where(t => defTypes[t].Count == 0))
            defTypes.Remove(t);
    }

    /// <summary>
    /// Unload all currently loaded mods.
    /// </summary>
    public void Unload() => Unload(mods.ToArray());

    /// <summary>
    /// Get def of type T with name.
    /// </summary>
    /// <typeparam name="T">The type to cast the returned Def to.</typeparam>
    /// <param name="name">The name of the Def.</param>
    /// <returns>The loaded Def.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get<T>(string name) where T : Def => (T) defNames[name];

    /// <summary>
    /// Get all loaded defs of type T.
    /// </summary>
    /// <typeparam name="T">The type to cast the returned list of Defs to.</typeparam>
    /// <returns>A list of all loaded defs of type T.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T> Get<T>() where T : Def 
    {
        if (!defTypes.TryGetValue(typeof(T), out var defs))
            return [];
        return defs.Cast<T>().ToList();
    }
 
    /// <summary>
    /// Return the InstanceDef matching name and the generic paramters.
    /// </summary>
    /// <typeparam name="I">The type of the InstanceDef.</typeparam>
    /// <typeparam name="T"></typeparam>
    /// <param name="name">The type the object the InstanceDef instantiates to.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public I GetInstance<I, T>(string name) where I : InstanceDef => GetInstances<I, T>().Find(i => i.Name == name);

    /// <summary>
    /// Return a list of all InstanceDefs that meet the generic parameters.
    /// </summary>
    /// <typeparam name="I">The type of the InstanceDef.</typeparam>
    /// <typeparam name="T">The type the object the InstanceDef instantiates to.</typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<I> GetInstances<I, T>() where I : InstanceDef
    {
        List<I> defs = Get<I>();
        defs.RemoveAll(d => !typeof(T).IsAssignableFrom(d.InstanceType));
        return defs;
    }
    
    /// <summary>
    /// Helper function for quickly instantiating an InstanceDef.
    /// </summary>
    /// <typeparam name="I">The type of the InstanceDef.</typeparam>
    /// <typeparam name="T">The type the object the InstanceDef instantiates to.</typeparam>
    /// <param name="name">The name of the Def.</param>
    /// <param name="parameters">Optional paramters to use for a constructor.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Create<I, T>(string name, params object[] parameters) where I : InstanceDef => GetInstance<I, T>(name).Instance<T>(parameters);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Create<I, T>(string name, T @base, params object[] parameters) where I : InstanceDef => GetInstance<I, T>(name).Instance(@base, parameters);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<T> CreateAsync<I, T>(string name, params object[] parameters) where I : InstanceDef => GetInstance<I, T>(name).InstanceAsync<T>(parameters);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<T> CreateAsync<I, T>(string name, T @base, params object[] parameters) where I : InstanceDef => GetInstance<I, T>(name).InstanceAsync<T>(@base, parameters);

    private struct ModObject
    {
        public Mod Owner;
        public JObject Obj;
    }
}

/// <summary>
/// Used by <see cref="SaveLoader.Load"/> to monitor progress.
/// </summary>
public interface ISaveLoaderListener
{

    public void Loading(string def) {}
    public void Progress(int loaded, int total) {}
    public void Complete() {}
}