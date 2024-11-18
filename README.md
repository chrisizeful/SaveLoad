# SaveLoad

SaveLoad is a C# serialization and modding API for the Godot game engine. It enables you to structure your game's content in a user-friendly way which can be easily expanded or modified.

## Features Overview

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

## Warning
TODO

## Licensing
SaveLoad is licensed under MIT - you are free to use it however you wish.
