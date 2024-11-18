# SaveLoad

SaveLoad is a C# serialization and modding API for the Godot game engine. It enables you to structure your game's content in a user-friendly way which can be easily expanded or modified.

## Features Overview

#### Def System

#### DependencyGraph
Allows for specifying per-mod dependencies and incompatibilities. Checks for and prevents cyclic dependencies and references. Provided each mod properly configures its list of dependencies and incompatibilities, users don't need to worry about the order of their mod list as it will automatically be ordered correctly.

#### Serialization
JSON converters are provided for every Godot type.

#### ModViewer
A themed UI scene to that allows for managing lists of mods.

#### PCK and DLL Support

#### A/sycnhronous Loading

#### Un/load individual mods

## Usage

The SaveLoad class is a singleton where the majority of your interactions with API will occur.

#### Folder Structure

By default, the mods folder is placed next to the project directory. This is done to 1) prevent your project from importing assets from mods and 2) allow the mods folder to be placed next to the exectuable when exported.

#### Creating Defs

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
Mod C# assemblies are not sandboxed or otherwise prevented from executing arbitrary code. Meaning, mods users download from the internet have the capability to execute malicious code such as deleting user files. Read more on this issue [here](https://github.com/godotengine/godot/issues/7753) and [here](https://github.com/godotengine/godot-proposals/issues/5010).

#### Work in Progress
SaveLoad has been used in numerous personal projects and tested in exported projects on both Windows and Linux. However, it has yet to be thoroughly evaluated in a comercially available game.

## Licensing
SaveLoad is licensed under MIT - you are free to use it however you wish.
