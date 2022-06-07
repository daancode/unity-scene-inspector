//  MIT License

//  Copyright(c) 2022 Damian Barczynski

//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:

//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.

//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

// Repository: https://github.com/daancode/unity-scene-inspector

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace Daancode.Editor
{
    [InitializeOnLoad]
    public class SceneInspector
    {
        [Serializable]
        public class SettingsData
        {
            public bool OnlyIncludedScenes;
            public bool ShowShortcutNames;
            public bool RestoreAfterPlay = true;
            public List<string> Shortcuts = new List<string>();
            public List<string> LastOpenedScenes = new List<string>();

            public static string Key => $"daancode:{Application.productName}:settings";
            public bool ShortcutsValid => Shortcuts != null && Shortcuts.Count > 0;

            public void Save()
            {
                EditorPrefs.SetString(Key, EditorJsonUtility.ToJson(this));
            }

            public void Load()
            {
                if (!EditorPrefs.HasKey(Key)) return;

                JsonUtility.FromJsonOverwrite(EditorPrefs.GetString(Key), this);
            }

            public void ToggleOnlyBuildOption()
            {
                OnlyIncludedScenes = !OnlyIncludedScenes;
                Settings.Save();
            }

            public void ToggleRestoreScenes()
            {
                Settings.RestoreAfterPlay = !Settings.RestoreAfterPlay;
                Settings.Save();
            }

            public void ToggleShortcutNames()
            {
                Settings.ShowShortcutNames = !Settings.ShowShortcutNames;
                Settings.Save();
            }

            public void ResetShortcuts()
            {
                Shortcuts.Clear();
                Save();
            }
        }

        private static class Layout
        {
            private static GUIContent _currentSceneContent;

            public static GUILayoutOption ShortWidth => GUILayout.Width(25f);
            
#if UNITY_2021_3_OR_NEWER
            public static GUILayoutOption Height => GUILayout.Height(20f);
#else
            public static GUILayoutOption Height => GUILayout.Height(22f);
#endif

            public static readonly Color ButtonColor = new Color(0.76f, 0.76f, 0.76f);

            public static GUIContent PlaySceneContent { get; } = new GUIContent()
            {
                image = EditorGUIUtility.IconContent("Animation.Play").image,
                tooltip = "Enter play mode from first scene defined in build settings."
            };

            public static GUIContent ChangeSceneContent => GetButtonForCurrentScene();

            public static GUIContent AddSceneContent { get; } = new GUIContent()
            {
                image = EditorGUIUtility.IconContent("Toolbar Plus More").image,
                tooltip = "Additional scene options..."
            };

            public static GUIContent ShortcutsShowNames { get; } = new GUIContent("Manage Shortcuts/Show Names");
            public static GUIContent ShortcutsClear { get; } = new GUIContent("Manage Shortcuts/Clear");
            public static GUIContent SettingsOnlyBuild { get; } = new GUIContent("Settings/Show only build scenes");
            public static GUIContent SettingsRestore { get; } = new GUIContent("Settings/Restore scene on play mode exit");
            
            public static GUIContent[] ToolbarContent => new[]
            {
                PlaySceneContent,
                ChangeSceneContent,
                AddSceneContent
            };

            public static GUIContent GetShortcutContent(string sceneName) => new GUIContent("Manage Shortcuts/" + sceneName);

            private static GUIContent GetButtonForCurrentScene()
            {
                if (_currentSceneContent == null)
                {
                    _currentSceneContent = new GUIContent
                    {
                        text = "...",
                        image = EditorGUIUtility.IconContent("BuildSettings.Editor.Small").image,
                        tooltip = "Change active scene"
                    };
                }
                
                var currentScene = SceneManager.GetActiveScene().name;
                if (string.IsNullOrEmpty(currentScene))
                {
                    currentScene = "Untitled";
                }
                _currentSceneContent.text = $" {currentScene}";
                return _currentSceneContent;
            }
        }
        
        private static SettingsData Settings { get; } = new SettingsData();

        static SceneInspector()
        {
            Settings.Load();
            ToolbarHook.Register(OnToolbarGUI, 3, ToolbarHook.Alignment.Left);
            ToolbarHook.Register(OnShortcutsGUI, 3, ToolbarHook.Alignment.Right);
            EditorApplication.playModeStateChanged += OnModeChanged;
        }
        
        private static void OnModeChanged(PlayModeStateChange playModeState)
        {
            Settings.Load();

            if (!Settings.RestoreAfterPlay || Settings.LastOpenedScenes.Count == 0)
            {
                return;
            }

            if (playModeState != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            var anyOpened = false;
            foreach (var scenePath in Settings.LastOpenedScenes.Where(path => !string.IsNullOrEmpty(path)))
            {
                EditorSceneManager.OpenScene(scenePath, anyOpened ? OpenSceneMode.Additive : OpenSceneMode.Single);
                anyOpened = true;
            }

            Settings.LastOpenedScenes.Clear();
            Settings.Save();
        }

        private static void OnToolbarGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var currentColor = GUI.backgroundColor;
                GUI.backgroundColor = Layout.ButtonColor;
                EditorGUI.BeginChangeCheck();
                var selection = GUILayout.Toolbar(-1, Layout.ToolbarContent, GUI.skin.button, GUI.ToolbarButtonSize.FitToContents, Layout.Height);
                if (EditorGUI.EndChangeCheck())
                {
                    switch (selection)
                    {
                        case 0: OnPlayButtonClicked(); break;
                        case 1: OnSceneChangeRequested(); break;
                        case 2: OnSettingsButtonClicked(); break;
                    }
                }
                GUI.backgroundColor = currentColor;
            }
        }

        private static void OnShortcutsGUI()
        {
            if (!Settings.ShortcutsValid)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < Settings.Shortcuts.Count; ++i)
            {
                var isActiveScene = IsActiveScene(Settings.Shortcuts[i]);
                var sceneName = GetSceneNameFromPath(Settings.Shortcuts[i]);

                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = isActiveScene ? Color.cyan : Layout.ButtonColor;

                using (new EditorGUI.DisabledScope(isActiveScene))
                {
                    if (Settings.ShowShortcutNames)
                    {
                        if (GUILayout.Button(SceneButtonContent(i, sceneName), Layout.Height))
                            SwitchScene(Settings.Shortcuts[i]);
                    }
                    else
                    {
                        if (GUILayout.Button(SceneButtonContent(i, sceneName), Layout.ShortWidth, Layout.Height))
                            SwitchScene(Settings.Shortcuts[i]);
                    }
                }

                GUI.backgroundColor = oldColor;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUIContent SceneButtonContent(int index, string sceneName)
            {
                return new GUIContent
                {
                    text = Settings.ShowShortcutNames ? sceneName : $"{index + 1}",
                    tooltip = GetSceneNameFromPath(Settings.Shortcuts[index])
                };
            }
        }
        
        private static void OnPlayButtonClicked()
        {
            if (EditorBuildSettings.scenes.Length == 0)
            {
                Debug.LogError("Unable to play scene, there is no scenes defined in build settings.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var countLoaded = SceneManager.sceneCount;
            for (var i = 0; i < countLoaded; i++)
            {
                Settings.LastOpenedScenes.Add(SceneManager.GetSceneAt(i).path);
            }

            Settings.Save();
            EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
            EditorApplication.isPlaying = true;
        }

        private static void OnSceneChangeRequested()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[SceneInspector] Unable to change scene during runtime.");
                return;
            }

            var menu = new GenericMenu();
            FillScenesMenu(menu, SwitchScene);
            menu.ShowAsContext();
        }

        private static void OnSettingsButtonClicked()
        {
            var menu = new GenericMenu();
            AddNewScene("Empty", NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddNewScene("Empty - Additive", NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            AddNewScene("Default", NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            AddNewScene("Default - Additive", NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            FetchShortcutScenes(menu);
            menu.AddSeparator("Manage Shortcuts/");
            menu.AddItem(Layout.ShortcutsShowNames, Settings.ShowShortcutNames, Settings.ToggleShortcutNames);
            menu.AddItem(Layout.ShortcutsClear, false, Settings.ResetShortcuts);
            menu.AddItem(Layout.SettingsOnlyBuild, Settings.OnlyIncludedScenes, Settings.ToggleOnlyBuildOption);
            menu.AddItem(Layout.SettingsRestore, Settings.RestoreAfterPlay, Settings.ToggleRestoreScenes);
            
            menu.AddSeparator("");
            menu.AddDisabledItem(new GUIContent("Open Additive"));
            FillScenesMenu(menu, AddScene);
            menu.ShowAsContext();

            void AddNewScene(string label, NewSceneSetup setup, NewSceneMode mode)
            {
                menu.AddItem(new GUIContent($"Create Scene/{label}"), false, () => { EditorSceneManager.NewScene(setup, mode); });
            }
        }

        private static void SwitchScene(object scene) => OpenSceneInternal(scene as string, 0);
        private static void AddScene(object scene) => OpenSceneInternal(scene as string, 1);

        private static void OpenSceneInternal(string scenePath, int mode)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                return;
            }
            
            if (EditorApplication.isPlaying)
            {
                SceneManager.LoadScene(scenePath, (LoadSceneMode) mode);
            }
            else
            {
                EditorSceneManager.OpenScene(scenePath, (OpenSceneMode) mode);
            }
        }
        
        private static void FillScenesMenu(GenericMenu menu, GenericMenu.MenuFunction2 callback)
        {
            foreach (var path in GetScenes().Where(path => !IsActiveScene(path)))
            {
                menu.AddItem(new GUIContent(GetSceneNameFromPath(path)), false, callback, path);
            }
        }

        private static IEnumerable<string> GetScenes()
        {
            if (Settings.OnlyIncludedScenes && EditorBuildSettings.scenes.Length != 0)
            {
                return EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            }

            return AssetDatabase.FindAssets("t:Scene")?.Select(AssetDatabase.GUIDToAssetPath).ToArray();
        }
        
        private static void FetchShortcutScenes(GenericMenu menu)
        {
            if (Settings.Shortcuts == null)
            {
                Settings.Shortcuts = new List<string>();
                Settings.Save();
            }

            var scenes = GetScenes();
            foreach (var path in scenes)
            {
                var sceneName = GetSceneNameFromPath(path);
                var isShortcut = Settings.Shortcuts.Contains(path);

                menu.AddItem(Layout.GetShortcutContent(sceneName), isShortcut, () =>
                {
                    if (isShortcut)
                    {
                        Settings.Shortcuts.Remove(path);
                    }
                    else
                    {
                        Settings.Shortcuts.Add(path);
                    }

                    Settings.Save();
                });
            }
        }

        private static string GetSceneNameFromPath(string path)
        {
            return Path.GetFileNameWithoutExtension(path.Split('/').Last());
        }

        private static bool IsActiveScene(string scenePath)
        {
            return scenePath == SceneManager.GetActiveScene().path;
        }
    }
}