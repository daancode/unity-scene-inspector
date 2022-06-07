# Unity Scene Inspector
Helper package which extends default toolbar with scene utilities.

This tool allows you to switch fast between scenes in your project, play first scene even if isn't active and pin selected scenes to toolbar.

![unity-scene-inspector](https://i.imgur.com/xUwEvZr.png)

#### Tested on:
- Unity 2018.4 LTS
- Unity 2019.4 LTS
- Unity 2020.3 LTS
- Unity 2021.3 LTS

## How to use

### Unity 2018.1 or higher
1. Open your project folder
1. Enter `Packages` directory
1. Open `manifest.json` file
1. Add `"com.daancode.scene-inspector": "https://github.com/daancode/unity-scene-inspector.git"` in `dependencies` section

### Older Unity version
1. Download repository
1. Extract files to any directory in your unity project, eg. `Assets/3rdParty/SceneInspector`

## Adding custom hooks

You can add custom GUI callbacks:
```c#
ToolbarHook.Register(() => { EditorGUILayout.LabelField("Test") }, 0, ToolbarHook.Alignment.Left);
```
> By default scene inspector callbacks are set to 3 position.