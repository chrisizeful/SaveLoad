<p align="center">
  <img src="https://i.imgur.com/OfBpO05.png" alt="Banner" />
</p>

[![NuGet](https://img.shields.io/nuget/v/SaveLoad.svg)](https://www.nuget.org/packages/SaveLoad/)

SaveLoad is a C# serialization, modding, and game content API for the Godot game engine. It enables you to structure your game's content in a user-friendly way which can be easily expanded or modified. See below for a list of features and a basic API overview. Addiontionally, it is recommended to look at the demo project to see how a typical game may setup folders and mods.

SaveLoad can be used as an addon by copying the folder located in addons (SaveLoad/addons/SaveLoad). It's also available as a Nuget package which can be viewed with the button above. The ModViewer folder in addons can optionally be copied as well - note it isn't in the Nuget package since it has assets that must be imported. Additionally, optional DefNode and InstanceDef node types can be used by including the DefNode addon folder.

## Features Overview

- **Def System:** Inspiried by the modding system of [Rimworld](https://rimworldgame.com/), every piece of game content is defined in JSON as a 'Def'. Allows for the easy creation, extension, organization, and updating of game content without the need for code.
- **DependencyGraph:** Allows for specifying per-mod dependencies and incompatibilities. Checks for and prevents cyclic dependencies and references. Provided each mod properly configures its list of dependencies and incompatibilities, users don't need to worry about the order of their mod list as it will automatically be ordered correctly. Additionally, this allows Defs to safely reference other Defs.
- **Serialization:** JSON converters are provided for every Godot type. Easily serialize and deserialize entire Nodes and their children, or other types.
- **Automatic PCK Packing:** Mod creators do not have to use Godot to manually pack their mods. Instead, the API packs all mods when the game is first run.
- **DLL Support:** Load C# assemblies that can hook into the game via StartupAttribute.
- **A/sycnhronous Loading:** Easily load assets asychronously via AssetLoad. Load mods synchronously or asynchronously via SaveLoader. Both provide callbacks allowing you to update a loading screen.
- **Un/load individual mods** Load entire directories of mods or load and unload individual mods. Enable users to create lists of mods which they can de/activate one by one or entirely.
- **ModViewer (UNFINISHED):** A themed UI scene that allows for managing, activating/deactivating lists of mods and individual mods.

## Installation

#### Addons
SaveLoad has three addons. The first, 'SaveLoad', is the main library. There are two optional ones: DefNode for using Defs in the Godot editor, and ModViewer for using the themed mod UI. To install addons, copy the appropriate folders from SaveLoad/addons into the addons folder of your poject. Read more about installing and enabling addons [here](https://docs.godotengine.org/en/stable/tutorials/plugins/editor/installing_plugins.html). 

#### Folder Structure

By default, the mods folder is placed next to the project directory. This is done to 1) prevent your project from importing assets from mods and 2) allow the mods folder to be placed next to the exectuable when exported. An additional directory (mods/packed) is created to store packed .pck files. See SaveLoader.PackDir and SaveLoader.ModDir.

The folder structure of mods is up to you. You can either enforce they be setup a specific way, or load all files in each mod folder. See the loading mods section for more information. Mods are usually their own Godot projects. However, that is only required if they use assets (i.e. textures) since Godot requires they have an .import file. If a mod only defines Defs, it does not have to be a Godot project.

## Loading Mods
The SaveLoader class is a singleton where the majority of your interactions with API will occur. It is recommended you load a list of mods (instead of each individually) so their load order can be correctly resolved. To load a list of mods you can specify the names of the mods and the sub-directories to include (the defs sub-directory is automatically included).
```C#
using namespace SaveLoad;

string[] mods = { "CoolTheme", "EpicBackground" };
string[] folders = { "assets", "scripts", "addons", "scenes", ".godot/imported" };
SaveLoader.Instance.Load(this, folders, mods);
```

Or, you can not specify a mods list to load every mod the user has in their mods folder:
```C#
string[] folders = ...;
SaveLoader.Instance.Load(this, folders);
```

The prior code loads mods asychronously. The first parameter in the Load() function is an optional ISaveLoaderListener for listening to callbacks. Note that since these methods may be called asynchronously, all interactions with the SceneTree should be done with SetDeferred() and CallDeferred().
```C#
// Optionally update a ProgressBar's text with the asset currently being loaded...
public void Loading(string def) {}    

// Update a ProgressBar or other UI here...
public void Progress(int loaded, int total) {}

// Done loading, switch scenes...
public void Complete() {}
```

You can also load and unload individual mods:
```C#
// Individual
SaveLoader.Unload(mod1, mod2, ...);
// ...or all currently loaded mods
SaveLoader.Unload();
```

## Creating Defs

First, you must create a record that extends Def. Then, you can create JSON files using that type. Defs can safely reference other Defs - the DefConverter resolves them by name and the DependencyGraph resolves the order to load them in. Note that a single file can contain any number of defs.
```C#
namespace MyGame;

public record ShirtDef : Def
{

    public Texture2D Texture { get; set; }
    public List<ShirtDef> Alternatives { get; set; } 
}
```
```JSON
{
    "$type": "MyGame.ShirtDef, MyGame",
    "Name": "DressShirt",
    "Texture": "res://assets/shirt/DressShirt.png",
    "Alternatives": [
        "FancyShirt",
        "FancierShirt"
    ]
}
```

#### InstanceDef and NodeDef

There are two built-in types that extend Def and can be instantiated: InstanceDef and NodeDef. An InstanceDef defines data that populates an object. The data is duplicated per instance so that it is unique. These Defs require that an InstanceType be defined and an optional dictionary of Properties to populate the object with. If your object is a simple Node2D or Node3D, you can choose to set the Type to one of the built-in InstanceDef2D/3D classes. A NodeDef is similar to an InstanceDef, but can take an optional Scene path to load/instantiate and an optional Script path to attach to the scene.

If the Type has a StringName Definition property (like InstanceDef2D/3D), it will be set to the name of the Def used to create it. For one, this allows you to fetch data from the Def (see the IsCool property below). It exists in the Def but is not set on the instantiated object.

The source for InstanceDef3D for reference:
```C#
public class InstanceDef3D : Node3D
{

	public StringName Definition { get; set; }
	public InstanceDef Def => SaveLoader.Instance.Get<InstanceDef>(Definition);
}
```

An example of defining a custom InstanceDef that has two Properties which are not set on instantiated objects:
```C#
namespace MyGame;

public record EnemyDef : InstanceDef
{

	public bool IsCool { get; set; }
	public Texture2D Texture { get; set; }
}
```

An example of JSON that defines InstanceDefs:
```JSON
{
    "$type": "MyGame.EnemyDef, MyGame",
    "Name": "DressShirt",
    "InstanceType": "Godot.Node3D, GodotSharp",
    "Texture": "res://assets/Enemy.png",
    "Properties": {
        "Position": "1.0,2.0,3.0",
    }
}
{
    "$type": "SaveLoad.InstanceDef, SaveLoad",
    "Name": "EvilCharacter",
    "InstanceType": "SaveLoad.InstanceDef3D, SaveLoad",
    "Scene": "res://scenes/Character.tcsn",
    "Script": "res://scenes/EvilCharacter.tscn",
    "Texture": "res://assets/EvilCharacter.png",
    "Properties": {
        "Position": "1.0,2.0,3.0",
    }
}
{
    "$type": "SaveLoad.CharacterDef, SaveLoad",
    "Name": "GoodCharacter",
    "InstanceType": "Godot.Node3D, GodotSharp",
    "Scene": "res://scenes/Character.tcsn",
    "Script": "res://scenes/GoodCharacter.tscn",
    "Texture": "res://assets/GoodCharacter.png",
    "Properties": {
        "Position": "3.0,1.0,0.0",
        "Scale": "2.0,2.0,2.0"
    }
}
```

## Using Defs

SaveLoader provides numerous methods to get Defs of a certain type. Additionally, Defs can be fetched by name:
```C#
// All of type
foreach (CharacterDef def in SaveLoader.Instance.Get<CharacterDef>())
	// Add to a character select screen, maybe...
// Single of type
CharacterDef def = SaveLoader.Instance.Get<CharacterDef>("OrcDef");
```

Fetching InstanceDefs is slighly different, as the InstanceType needs to be specified alongside the Def Type:
```C#
// All
foreach (CharacterInstanceDef def in SaveLoader.Instance.GetInstance<CharacterInstanceDef, InstanceDef>())
	// Add to a character select screen, maybe...
// Single, by name
CharacterInstanceDef def = SaveLoader.Instance.GetInstance<CharacterInstanceDef, Character2D>("OrcInstanceDef");
```

You can also use the DefNames and DefTypes dictionaries to access Defs directly:
```C#
CharacterInstanceDef def = (CharacterInstanceDef) SaveLoader.Instance.DefNames["OrcInstanceDef"];
// or to get a list
List<CharacterInstanceDef> defs = (CharacterInstanceDef) SaveLoader.Instance.DefTypes[typeof(CharacterInstanceDef)];
```

To create an object from an InstanceDef you can use the Instance() method or use SaveLoader.Create() for easy creation:
```C#
CharacterInstanceDef def = SaveLoader.Instance.GetInstance<CharacterInstanceDef, Character2D>("OrcInstanceDef");
Character2D character = def.Instance<CharacterInstanceDef>();
// or if you don't have a reference to the def...
character = SaveLoader.Instance.Create<CharacterInstanceDef, Character2D>("OrcInstanceDef");
```

## Mod DLL Usage

A mod can hook into your game by using StartupAttribute on a static method. This method is automatically called when the mod is loaded. Note that depending on a how a mod is loaded, it could be called synchronously or asynchronously. Thus, all interactions with the SceneTree should be done with SetDeferred() and CallDeferred().
```C#
[Startup]
public static void OnStartup()
{
    SceneTree tree = (SceneTree) Engine.GetMainLoop();
    // Modify SceneTree, add custom nodes, etc...
}
```

#### Object Serialization

SaveLoader provides methods for easily saving and loading any object:
```C#
Node node = ...
string json = SaveLoader.Save(node);
// or save directly to a file
SaveLoader.Save("res://save.json", node);

Node loaded = SaveLoader.LoadJson(json);
// or load directly from a file
loaded = SaveLoader.Load("res://save.json");
```

## Warnings

#### Arbitrary Code Execution
Mod C# assemblies are not sandboxed or otherwise prevented from executing arbitrary code. Meaning, mods users download from the internet have the capability to execute malicious code such as deleting user files. Deploy at your own risk and provide a warning to users. Read more on this issue [here](https://github.com/godotengine/godot/issues/7753) and [here](https://github.com/godotengine/godot-proposals/issues/5010).

#### Work in Progress
SaveLoad has been used in numerous personal projects and tested in exported projects on both Windows and Linux. However, it has yet to be thoroughly evaluated in a comercially available game. While the core concepts are soldified, some aspects may be broken or partially functional. Any issues and pull requests are much appreciated.

## Licensing
SaveLoad is licensed under MIT - you are free to use it however you wish.

The demo uses assets from Kenney's CC0 licensed [Tiny Dungeon](https://kenney.nl/assets/tiny-dungeon) asset pack. It also uses Nidhoggn's CC0 licensed [Backgrounds](https://opengameart.org/content/backgrounds-3). The ModViewer scene uses assets from Kenney's CC0 licensed [Game Icons](https://kenney.nl/assets/game-icons). The banner logo uses the [Ubuntu Title](https://www.dafont.com/ubuntu-title.font) font.