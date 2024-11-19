# SaveLoad

SaveLoad is a C# serialization and modding API for the Godot game engine. It enables you to structure your game's content in a user-friendly way which can be easily expanded or modified. See below for a list of features and a basic API overview. Addiontionally, it is recommended to look at the demo project to see how a typical game may setup folders and mods.

## Features Overview

- **Def System:** Inspiried by the modding system of [Rimworld](https://rimworldgame.com/), every piece of game content is defined in JSON as a 'Def'. Allows for the easy creation, extension, organization, and updating of game content without the need for code.
- **DependencyGraph:** Allows for specifying per-mod dependencies and incompatibilities. Checks for and prevents cyclic dependencies and references. Provided each mod properly configures its list of dependencies and incompatibilities, users don't need to worry about the order of their mod list as it will automatically be ordered correctly. Additionally, this allows defs to safely reference other defs.
- **Serialization:** JSON converters are provided for every Godot type.
- **ModViewer:** A themed UI scene that allows for managing lists of mods.
- **Automatic PCK Packing:** Mod creators do not have to use Godot to manually pack their mods. Instead, the API packs all mods when the game is run.
- **DLL Support:** Load C# assemblies that can hook into the game via StartupAttribute.
- **A/sycnhronous Loading:** Easily load assets asychronously via AssetLoad. Load mods synchronously or asynchronously via SaveLoad. Both provide callbacks allowing you to update a loading screen.
- **Un/load individual mods** Load entire directories of mods or load and unload individual mods. Enable users to create lists of mods which they can de/activate one by one or entirely.

## Usage

The SaveLoad class is a singleton where the majority of your interactions with API will occur.

#### Folder Structure

By default, the mods folder is placed next to the project directory. This is done to 1) prevent your project from importing assets from mods and 2) allow the mods folder to be placed next to the exectuable when exported. An additional directory (mods/packed) is created to store packed .pck files. See SaveLoad.PackDir and SaveLoad.ModDir.

The folder structure of mods is up to you. You can either enforce they be setup a specific way, or load all files in each mod folder. See the loading mods section for more information.

#### Creating Defs

First, you must create a record that extends Def. Then, you can create JSON files using that type. Note that Defs can safely reference other Defs - the DefConverter resolves them by name and the DependencyGraph resolves the order to load them in.
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

#### Loading Mods
To load a list of mods you can specify the names of the mods and the sub-folders to include:
```C#
string[] mods = { "CoolTheme", "EpicBackground" };
string[] folders = { "assets", "scripts", "addons", "scenes", ".godot/imported" };
SaveLoad.Instance.Load(this, folders, mods);
```

The prior code loads mods asychronously. The first parameter in the Load() function is an optional ISaveLoadListener for listening to callbacks:
```C#
// Optionally update ProgressBar text with the asset currently being loaded...
public void Loading(string def) {}    

// Update a ProgressBar or other UI here...
public void Progress(int loaded, int total) {}

// Done loading, switch scenes...
public void Complete() {}
```

## Warnings

#### Arbitrary Code Execution
Mod C# assemblies are not sandboxed or otherwise prevented from executing arbitrary code. Meaning, mods users download from the internet have the capability to execute malicious code such as deleting user files. Deploy at your own risk and provide a warning to users. Read more on this issue [here](https://github.com/godotengine/godot/issues/7753) and [here](https://github.com/godotengine/godot-proposals/issues/5010).

#### Work in Progress
SaveLoad has been used in numerous personal projects and tested in exported projects on both Windows and Linux. However, it has yet to be thoroughly evaluated in a comercially available game. Any issues and pull requests are much appreciated.

## Licensing
SaveLoad is licensed under MIT - you are free to use it however you wish.
