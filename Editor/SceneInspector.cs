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
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if !UNITY_2019_1_OR_NEWER
using UnityEngine.Experimental.UIElements;
#endif

// ReSharper disable once CheckNamespace
namespace Daancode.Editor
{
    [InitializeOnLoad]
    public class SceneInspector
    {
        [Serializable]
        public class Settings
        {
            public bool OnlyIncludedScenes = false;
            public bool ShowShortcutNames = false;
            public bool RestoreAfterPlay = true;
            public List<string> Shortcuts;
            public string LastOpenedScene;

            public static string Key => $"daancode:{Application.productName}:settings";
            public bool ShortcutsValid => Shortcuts != null && Shortcuts.Count > 0;

            public void Save()
            {
                EditorPrefs.SetString( Key, EditorJsonUtility.ToJson( this ) );
            }

            public void Load()
            {
                if (!EditorPrefs.HasKey( Key ))
                {
                    return;
                }

                JsonUtility.FromJsonOverwrite( EditorPrefs.GetString( Key ), this );
            }
        }

        private static class Styles
        {
            private static GUIContent _playButtonContent;
            private static GUIContent _addSceneContent;
            private static GUIContent _settingsContent;

            public static GUILayoutOption ShortWidth => GUILayout.Width( 25f );
            public static GUILayoutOption Height => GUILayout.Height( 22f );

#if UNITY_2019_3_OR_NEWER
            public static Color ButtonColor = new Color( 1.0f, 1.0f, 1.0f, .5f );
#else
            public static Color ButtonColor = Color.white;
#endif

            public static GUIContent PlaySceneContent => _playButtonContent ?? ( _playButtonContent = new GUIContent
            {
                image = EditorGUIUtility.IconContent( "Animation.Play" ).image,
                tooltip = "Enter play mode from first scene defined in build settings."
            } );

            public static GUIContent ChangeSceneContent => new GUIContent
            {
                text = " " + SceneManager.GetActiveScene().name,
                image = EditorGUIUtility.IconContent( "BuildSettings.Editor.Small" ).image,
                tooltip = "Change active scene"
            };

            public static GUIContent AddSceneContent => _addSceneContent ?? ( _addSceneContent = new GUIContent
            {
                image = EditorGUIUtility.IconContent( "Toolbar Plus More" ).image,
                tooltip = "Open scene in additive mode"
            } );

            public static GUIContent SettingsContent => _settingsContent ?? ( _settingsContent = new GUIContent
            {
                image = EditorGUIUtility.IconContent( "_Popup" ).image,
                tooltip = "Scene inspector settings"
            } );
        }

        private static Settings s_settings;
        private static Settings CurrentSettings => s_settings ?? ( s_settings = new Settings() );

        static SceneInspector()
        {
            CurrentSettings.Load();
            ToolbarHook.OnLeftToolbarGUI += OnToolbarGUI;
            ToolbarHook.OnRightToolbarGUI += OnShortcutsGUI;
            EditorApplication.playModeStateChanged += OnModeChanged;
        }

        private static void OnModeChanged( PlayModeStateChange playModeState )
        {
            CurrentSettings.Load();

            if(!CurrentSettings.RestoreAfterPlay || CurrentSettings.LastOpenedScene == string.Empty)
            {
                return;
            }

            if ( playModeState == PlayModeStateChange.EnteredEditMode)
            {
                EditorSceneManager.OpenScene( CurrentSettings.LastOpenedScene );
                CurrentSettings.LastOpenedScene = string.Empty;
                CurrentSettings.Save();
            }
        }

        private static void OnToolbarGUI(Rect rect)
        {
            GUILayout.FlexibleSpace();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope( EditorApplication.isPlaying ))
                {
                    CreatePlayButton();
                    CreateSceneChangeButton();
                }

                CreateSceneAddButton();

                using (new EditorGUI.DisabledScope( EditorApplication.isPlaying ))
                {
                    CreateSettingsButton();
                }
            }
        }

        private static void OnShortcutsGUI(Rect rect)
        {
            if (EditorApplication.isPlaying || !CurrentSettings.ShortcutsValid)
            {
                return;
            }

            for (var i = 0; i < CurrentSettings.Shortcuts.Count; ++i)
            {
                var isActiveScene = IsActiveScene( CurrentSettings.Shortcuts[i] );
                var sceneName = GetSceneNameFromPath( CurrentSettings.Shortcuts[i] );

                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = isActiveScene ? Color.cyan : Styles.ButtonColor;

                using (new EditorGUI.DisabledScope( isActiveScene ))
                {
                    if (CurrentSettings.ShowShortcutNames)
                    {
                        if (GUILayout.Button( SceneButtonContent( i, sceneName ), Styles.Height ))
                        {
                            SwitchScene( CurrentSettings.Shortcuts[i] );
                        }
                    }
                    else
                    {
                        if (GUILayout.Button( SceneButtonContent( i, sceneName ), Styles.ShortWidth, Styles.Height ))
                        {
                            SwitchScene( CurrentSettings.Shortcuts[i] );
                        }
                    }
                }

                GUI.backgroundColor = oldColor;
            }

            GUIContent SceneButtonContent( int index, string sceneName )
            {
                return new GUIContent
                {
                    text = CurrentSettings.ShowShortcutNames ? sceneName : $"{index + 1}",
                    tooltip = GetSceneNameFromPath( CurrentSettings.Shortcuts[index] )
                };
            }

            GUILayout.FlexibleSpace();
        }

        private static void SwitchScene( object scene )
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene( scene as string );
            }
        }

        private static void AddScene( object scene )
        {
            if (EditorApplication.isPlaying)
            {
                SceneManager.LoadScene( scene as string, LoadSceneMode.Additive );
            }
            else
            {
                EditorSceneManager.OpenScene( scene as string, OpenSceneMode.Additive );
            }
        }

        private static void CreatePlayButton()
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = EditorApplication.isPlaying ? Color.red : Color.green;

            using (new EditorGUI.DisabledScope( EditorBuildSettings.scenes.Length == 0 ))
            {
                if (GUILayout.Button( Styles.PlaySceneContent, Styles.Height ) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    CurrentSettings.LastOpenedScene = SceneManager.GetActiveScene().path;
                    CurrentSettings.Save();
                    EditorSceneManager.OpenScene( EditorBuildSettings.scenes[0].path );
                    EditorApplication.isPlaying = true;
                }
            }

            GUI.backgroundColor = oldColor;
        }

        private static void CreateSceneChangeButton()
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Styles.ButtonColor;
            if (GUILayout.Button( Styles.ChangeSceneContent, Styles.Height ) && !EditorApplication.isPlaying)
            {
                var menu = new GenericMenu();
                FillScenesMenu( menu, SwitchScene );
                menu.ShowAsContext();
            }
            GUI.backgroundColor = oldColor;
        }

        private static void CreateSceneAddButton()
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Styles.ButtonColor;
            if (GUILayout.Button( Styles.AddSceneContent, Styles.Height ))
            {
                var menu = new GenericMenu();
                FillScenesMenu( menu, AddScene, false );
                menu.ShowAsContext();
            }
            GUI.backgroundColor = oldColor;
        }

        private static void FillScenesMenu( GenericMenu menu, GenericMenu.MenuFunction2 callback, bool showActiveScene = true )
        {
            var scenePaths = GetScenes();

            foreach (var path in scenePaths)
            {
                menu.AddItem( SceneNameContent( path ), IsActiveScene( path ) && showActiveScene, callback, path );
            }

            GUIContent SceneNameContent( string path )
            {
                return new GUIContent( GetSceneNameFromPath( path ) );
            }
        }

        private static string[] GetScenes()
        {
            if (CurrentSettings.OnlyIncludedScenes && EditorBuildSettings.scenes.Length != 0)
            {
                return EditorBuildSettings.scenes.Select( s => s.path ).ToArray();
            }

            return AssetDatabase.FindAssets( "t:Scene" )?.Select( AssetDatabase.GUIDToAssetPath ).ToArray();
        }

        private static void CreateSettingsButton()
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Styles.ButtonColor;
            if (GUILayout.Button( Styles.SettingsContent, Styles.Height ))
            {
                var menu = new GenericMenu();

                AddNewScene( menu, "Empty", NewSceneSetup.EmptyScene, NewSceneMode.Single );
                AddNewScene( menu, "Empty (Additive)", NewSceneSetup.EmptyScene, NewSceneMode.Additive );
                AddNewScene( menu, "Default", NewSceneSetup.DefaultGameObjects, NewSceneMode.Single );
                AddNewScene( menu, "Default (Additive)", NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive );

                FetchShortcutScenes( menu );
                menu.AddSeparator( "Shortcuts/" );
                menu.AddItem( new GUIContent( "Shortcuts/Show Names" ), CurrentSettings.ShowShortcutNames, () =>
                {
                    CurrentSettings.ShowShortcutNames = !CurrentSettings.ShowShortcutNames;
                    CurrentSettings.Save();
                } );

                menu.AddItem( new GUIContent( "Shortcuts/Clear" ), false, () =>
                {
                    CurrentSettings.Shortcuts.Clear();
                    CurrentSettings.Save();
                } );

                menu.AddSeparator( "" );
                menu.AddDisabledItem( new GUIContent( "Settings" ) );
                menu.AddItem( new GUIContent( "Only build scenes" ), CurrentSettings.OnlyIncludedScenes,
                () =>
                {
                    CurrentSettings.OnlyIncludedScenes = !CurrentSettings.OnlyIncludedScenes;
                    CurrentSettings.Save();
                } );
                
                menu.AddItem( new GUIContent( "Restore scene on play mode exit" ), CurrentSettings.RestoreAfterPlay,
                () =>
                {
                    CurrentSettings.RestoreAfterPlay = !CurrentSettings.RestoreAfterPlay;
                    CurrentSettings.Save();
                } );

                menu.ShowAsContext();
            }

            void AddNewScene( GenericMenu menu, string label, NewSceneSetup setup, NewSceneMode mode )
            {
                menu.AddItem( new GUIContent( $"Create Scene/{label}" ), false, () =>
                {
                    EditorSceneManager.NewScene( setup, mode );
                } );
            }
            GUI.backgroundColor = oldColor;
        }

        private static void FetchShortcutScenes( GenericMenu menu )
        {
            if (CurrentSettings.Shortcuts == null)
            {
                CurrentSettings.Shortcuts = new List<string>();
                CurrentSettings.Save();
            }

            var scenes = GetScenes();
            foreach (var path in scenes)
            {
                var sceneName = GetSceneNameFromPath( path );
                var isShortcut = CurrentSettings.Shortcuts.Contains( path );

                menu.AddItem( new GUIContent( "Shortcuts/" + sceneName ), isShortcut, () =>
                {
                    if (isShortcut)
                    {
                        CurrentSettings.Shortcuts.Remove( path );
                    }
                    else
                    {
                        CurrentSettings.Shortcuts.Add( path );
                    }

                    CurrentSettings.Save();
                } );
            }
        }

        private static string GetSceneNameFromPath( string path )
        {
            return System.IO.Path.GetFileNameWithoutExtension( path.Split( '/' ).Last() );
        }

        private static bool IsActiveScene( string scenePath )
        {
            return scenePath == SceneManager.GetActiveScene().path;
        }
    }
}
