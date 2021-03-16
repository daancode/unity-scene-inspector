//  MIT License

//  Copyright(c) 2021 Damian Barczynski

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
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_2019_1_OR_NEWER
    using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Daancode.Utils
{
    // ToolbarExtender based on: https://github.com/marijnz/unity-toolbar-extender.
    [InitializeOnLoad]
    public static class ToolbarExtender
    {
        private const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly Assembly m_assembly = typeof( Editor ).Assembly;
        private static readonly Type m_toolbarType = m_assembly.GetType( "UnityEditor.Toolbar" );
        private static readonly FieldInfo m_imguiContainerOnGui = typeof( IMGUIContainer ).GetField( "m_OnGUIHandler", FLAGS );
        private static ScriptableObject m_currentToolbar;

#if UNITY_2020_1_OR_NEWER
		static Type m_iWindowBackendType = typeof(Editor).Assembly.GetType("UnityEditor.IWindowBackend");
		static PropertyInfo m_windowBackend = m_assembly.GetType( "UnityEditor.GUIView" )
                                                        .GetProperty("windowBackend", FLAGS);
		static PropertyInfo m_viewVisualTree = m_iWindowBackendType.GetProperty("visualTree", FLAGS);
#else
        private static readonly PropertyInfo m_viewVisualTree = m_assembly
                                                                .GetType( "UnityEditor.GUIView" )
                                                                .GetProperty( "visualTree", FLAGS );
#endif

        private static readonly int m_toolCount = GetToolsCount();
        private static GUIStyle m_commandStyle = null;

        public static readonly List<Action> LeftToolbarGUI = new List<Action>();
        public static readonly List<Action> RightToolbarGUI = new List<Action>();

        static ToolbarExtender()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (m_currentToolbar == null)
            {
                var toolbars = Resources.FindObjectsOfTypeAll( m_toolbarType );
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject) toolbars[0] : null;
            }

#if UNITY_2020_1_OR_NEWER
            var windowBackend = m_windowBackend.GetValue(m_currentToolbar);
            var visualTree = (VisualElement) m_viewVisualTree.GetValue(windowBackend, null);
#else
            var visualTree = (VisualElement) m_viewVisualTree.GetValue( m_currentToolbar, null );
#endif

            var container = visualTree[0] as IMGUIContainer;
            var handler = m_imguiContainerOnGui.GetValue( container ) as Action;
            handler -= OnGUI;
            handler += OnGUI;
            m_imguiContainerOnGui.SetValue( container, handler );
        }

        private static void OnGUI()
        {
            if (m_commandStyle == null)
            {
                m_commandStyle = new GUIStyle( "Command" );
            }

            var screenWidth = EditorGUIUtility.currentViewWidth;

#if UNITY_2019_1_OR_NEWER
            var playButtonsPosition = ( screenWidth - 140 ) / 2;
#else
            var playButtonsPosition = ( screenWidth - 100 ) / 2;
#endif

            var leftToolbarRect = new Rect( 0, 4, screenWidth, 24 );
            leftToolbarRect.xMin += 170 + 32 * m_toolCount;
            leftToolbarRect.xMax = playButtonsPosition - 10;

            var rightToolbarRect = new Rect( 0, 4, screenWidth, 24 )
            {
                xMin = playButtonsPosition + 10 + m_commandStyle.fixedWidth * 3,
                xMax = screenWidth - 420
            };

            HandleCustomToolbar( LeftToolbarGUI, leftToolbarRect );
            HandleCustomToolbar( RightToolbarGUI, rightToolbarRect );
        }

        private static void HandleCustomToolbar( IEnumerable<Action> toolbar, Rect rect )
        {
            if (!( rect.width > 0 ))
            {
                return;
            }

            using (new GUILayout.AreaScope( rect ))
            {
                using (new GUILayout.HorizontalScope())
                {
                    foreach (var handler in toolbar)
                    {
                        handler();
                    }
                }
            }
        }

        private static int GetToolsCount()
        {
#if UNITY_2019_1_OR_NEWER
            const string fieldName = "k_ToolCount";
#else
            const string fieldName = "s_ShownToolIcons";
#endif

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var toolIcons = m_toolbarType.GetField( fieldName, flags );

#if UNITY_2019_3_OR_NEWER
            return toolIcons != null ? ( (int) toolIcons.GetValue( null ) ) : 8;
#elif UNITY_2019_1_OR_NEWER
            return toolIcons != null ? ( (int) toolIcons.GetValue( null ) ) : 7;
#elif UNITY_2018_1_OR_NEWER
            return toolIcons != null ? ( (Array) toolIcons.GetValue( null ) ).Length : 6;
#else
			return toolIcons != null ? ( (Array) toolIcons.GetValue( null ) ).Length : 5;
#endif
        }
    }

    [InitializeOnLoad]
    public class SceneInspector
    {
        [Serializable]
        public class Settings
        {
            public bool OnlyIncludedScenes = false;
            public bool EnableShortcuts = false;
            public bool ShowShortcutNames = false;
            public bool RestoreAfterPlay = true;
            public List<string> Shortcuts;
            public string LastOpenedScene;

            public static string Key => $"daancode:{Application.productName}:settings";
            public bool ShortcutsValid => EnableShortcuts && Shortcuts != null && Shortcuts.Count > 0;

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
            ToolbarExtender.LeftToolbarGUI.Add( OnToolbarGUI );
            ToolbarExtender.RightToolbarGUI.Add( OnShortcutsGUI );
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

        private static void OnToolbarGUI()
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

        private static void OnShortcutsGUI()
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

                if (CurrentSettings.EnableShortcuts)
                {
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
                }

                menu.AddSeparator( "" );
                menu.AddDisabledItem( new GUIContent( "Settings" ) );
                menu.AddItem( new GUIContent( "Only build scenes" ), CurrentSettings.OnlyIncludedScenes,
                () =>
                {
                    CurrentSettings.OnlyIncludedScenes = !CurrentSettings.OnlyIncludedScenes;
                    CurrentSettings.Save();
                } );

                menu.AddItem( new GUIContent( "Shortcuts enabled" ), CurrentSettings.EnableShortcuts,
                () =>
                {
                    CurrentSettings.EnableShortcuts = !CurrentSettings.EnableShortcuts;
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
