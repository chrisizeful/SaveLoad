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

public class SaveLoad
{

    public static SaveLoad Instance
    {
        get
        {
            _instance ??= new SaveLoad();
            return _instance;
        }
    }
    private static SaveLoad _instance;

    public JsonSerializer Serializer { get; private set; }
    public JsonSerializerSettings Settings { get; private set; }

    private Dictionary<Type, List<Def>> defTypes { get; } = new();
    public IReadOnlyDictionary<Type, List<Def>> DefTypes => defTypes;

    private Dictionary<string, Def> defNames { get; } = new();
    public IReadOnlyDictionary<string, Def> DefNames => defNames;

    private List<Mod> mods { get; } = new();
    public IReadOnlyList<Mod> Mods => mods;

    Version gameVersion;
    public Version GameVersion
    {
        get
        {
            if (gameVersion != null)
                return gameVersion;
            string current = ProjectSettings.GetSetting("config/Version", "1.0.0").AsString();
            gameVersion = new(current);
            return gameVersion;
        }
    }

    public static string PackParentDir
    {
#if DEBUG
        get => "res://";
#else
        get => Path.GetDirectoryName(OS.GetExecutablePath()).Replace('\\', '/') + "/";
#endif
    }

    public static string PackSubdir => "mods/packed/";
    public static string PackDir => PackParentDir + PackSubdir;

    public static string ModDir
    {
#if DEBUG
        get => Directory.GetParent(Directory.GetParent(ProjectSettings.GlobalizePath("res://")).ToString()).ToString().Replace("\\", "/") + "/Mods/";
#else
        get => Path.GetDirectoryName(OS.GetExecutablePath()).Replace('\\', '/') + "/Mods/";
#endif
    }

    private SaveLoad()
    {
        JsonConvert.DefaultSettings = () => Settings;
        Settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Converters = new List<JsonConverter> {
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
            },
            ContractResolver = new IgnoreResolver(new List<string> {
                // Object
                "DynamicObject",
                "NativeInstance"
            })
        };
        Serializer = JsonSerializer.CreateDefault();
    }
    
    public static JsonSerializer CreateDefault()
    {
        _instance ??= new SaveLoad();
        return JsonSerializer.CreateDefault();
    }

    public async Task<string> Save(object data, Formatting formatting = Formatting.None)
    {
        return await Task.Run(() => JsonConvert.SerializeObject(data, formatting));
    }

    public async void Save(string path, object data, Formatting formatting = Formatting.None)
    {
        if (Files.MakeDirRecursive(path))
            Files.Write(path, await Save(data, formatting));
    }

    public async Task<T> LoadAsync<T>(string path) => await Task.Run(() => Load<T>(path));
    public T Load<T>(string path, bool cache = false) => JsonConvert.DeserializeObject<T>(Files.GetAsText(path, cache));
    public T LoadJson<T>(string json) => JsonConvert.DeserializeObject<T>(json);

    public Def LoadDef(string json)
    {
        using JsonTextReader reader = new(new StringReader(json));
        if (reader.Read())
            return LoadDef(JObject.Load(reader));
        return null;
    }

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

    public async Task<Def> LoadDefAsync(string json)
    {
        using JsonTextReader reader = new JsonTextReader(new StringReader(json));
        if (reader.Read())
            return await LoadDefAsync(JObject.Load(reader));
        return null;
    }

    // Load single def using a JObject
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

    // Load all enabled mods
    public async void Load(ISaveLoadListener listener, string[] folders, params string[] enabled)
    {
        // Pack mods (including internal)
#if DEBUG
        ModPacker.Pack(PackParentDir, "Internal", folders);
        foreach (string modPath in enabled)
            ModPacker.Pack($"{ModDir}{modPath}", modPath, folders);
        // Load mod packs, internal last
        foreach (string mod in enabled)
            await AssetLoad.Instance.LoadResourcePack($"{PackDir}{mod}.pck");
        await AssetLoad.Instance.LoadResourcePack($"{PackDir}Internal.pck");
#else
        // Load packs recursively from mod folder
        await AssetLoad.Instance.LoadResourcePacks(ModDir);
#endif
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
                .Select(method => (method, method.GetCustomAttribute<StartupAttribute>()))
                .Where(pair => pair.Item2 != null).ToList()
                .ForEach(pair => pair.method.Invoke(null, pair.Item2.Parameters));         
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

    // Unload mod by path
    public void Unload(params Mod[] unload)
    {
        foreach (Mod mod in unload)
        {
            mod.Defs.ForEach(d => defNames.Remove(d));
            mods.Remove(mod);
            // TODO await SteamWorkshop.StopTrack(mod);
        }
        foreach (Type t in defTypes.Keys.Where(t => defTypes[t].Count == 0))
            defTypes.Remove(t);
    }

    public void Unload() => Unload(mods.ToArray());

    // Get def of type T with name
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get<T>(string name) where T : Def => (T) defNames[name];

    // Get list of defs of type T
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T> Get<T>() where T : Def 
    {
        if (!defTypes.TryGetValue(typeof(T), out var defs))
            return new List<T>();
        return defs.Cast<T>().ToList();
    }
 
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public I GetInstance<I, T>(string name) where I : InstanceDef => GetInstances<I, T>().Find(i => i.Name == name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<I> GetInstances<I, T>() where I : InstanceDef
    {
        List<I> defs = Get<I>();
        defs.RemoveAll(d => !typeof(T).IsAssignableFrom(d.InstanceType));
        return defs;
    }
    
    // Create an InstanceDef 
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Create<I, T>(string name, params object[] parameters) where I : InstanceDef => GetInstance<I, T>(name).Instance<T>(parameters);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Create<I, T>(string name, T @base, params object[] parameters) where I : InstanceDef => GetInstance<I, T>(name).Instance<T>(@base, parameters);
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

public interface ISaveLoadListener
{

    public void Loading(string def) {}
    public void Progress(int loaded, int total) {}
    public void Complete() {}
}