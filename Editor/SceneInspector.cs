//  MIT License

//  Copyright(c) 2018 Damian Barczynski

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

// https://github.com/daancode/unity-scene-inspector

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

namespace DCTools
{
    [Serializable]
    public class SceneInspectorSettings
    {
        public bool OnlyIncludedScenes = false;
        public bool RestoreAfterPlay = true;
        public string[] scenePaths;
    }

    [InitializeOnLoad]
    public static class ToolbarExtender
    {
        static BindingFlags m_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        static Assembly m_assembly = typeof( Editor ).Assembly;
        static Type m_toolbarType = m_assembly.GetType( "UnityEditor.Toolbar" );
        static PropertyInfo m_viewVisualTree = m_assembly.GetType( "UnityEditor.GUIView" ).GetProperty( "visualTree", m_flags );
        static FieldInfo m_imguiContainerOnGui = typeof( IMGUIContainer ).GetField( "m_OnGUIHandler", m_flags );
        static ScriptableObject m_currentToolbar;

        static int m_toolCount = GetToolsCount();
        static GUIStyle m_commandStyle = null;

        public static readonly List<Action> LeftToolbarGUI = new List<Action>();
        public static readonly List<Action> RightToolbarGUI = new List<Action>();

        static ToolbarExtender()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            if (m_currentToolbar == null)
            {
                var toolbars = Resources.FindObjectsOfTypeAll( m_toolbarType );
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject) toolbars[0] : null;
                if (m_currentToolbar != null)
                {
                    var element = m_viewVisualTree.GetValue( m_currentToolbar, null ) as VisualElement;
                    var container = element[0] as IMGUIContainer;
                    var handler = m_imguiContainerOnGui.GetValue( container ) as Action;
                    handler -= OnGUI;
                    handler += OnGUI;
                    m_imguiContainerOnGui.SetValue( container, handler );
                }
            }
        }

        static void OnGUI()
        {
            if (m_commandStyle == null)
            {
                m_commandStyle = new GUIStyle( "CommandLeft" );
            }

            var screenWidth = EditorGUIUtility.currentViewWidth;

#if UNITY_2019_1_OR_NEWER
            float playButtonsPosition = ( screenWidth - 140 ) / 2;
#else
            float playButtonsPosition = (screenWidth - 100) / 2;
#endif

            Rect leftToolbarRect = new Rect( 0, 4, screenWidth, 24 );
            leftToolbarRect.xMin += 170 + 32 * m_toolCount;
            leftToolbarRect.xMax = playButtonsPosition - 10;

            Rect rightToolbarRect = new Rect( 0, 4, screenWidth, 24 );
            rightToolbarRect.xMin = playButtonsPosition + 10 + ( m_commandStyle.fixedWidth * 3 );
            rightToolbarRect.xMax = screenWidth - 420;

            HandleCustomToolbar( LeftToolbarGUI, leftToolbarRect );
            HandleCustomToolbar( RightToolbarGUI, rightToolbarRect );
        }

        static void HandleCustomToolbar( List<Action> toolbar, Rect rect )
        {
            if (rect.width > 0)
            {
                GUILayout.BeginArea( rect );
                GUILayout.BeginHorizontal();
                foreach (var handler in toolbar)
                {
                    handler();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }

        static int GetToolsCount()
        {
#if UNITY_2019_1_OR_NEWER
            string fieldName = "k_ToolCount";
#else
			string fieldName = "s_ShownToolIcons";
#endif

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var toolIcons = m_toolbarType.GetField( fieldName, flags ) as FieldInfo;

#if UNITY_2019_1_OR_NEWER
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
        static float Height = 22f;
        static SceneInspectorSettings Settings;
        static HashSet<string> Shortcuts;

        static SceneInspector()
        {
            LoadSettings();
            if (Settings == null)
            {
                Settings = new SceneInspectorSettings();
            }

            ToolbarExtender.LeftToolbarGUI.Add( OnToolbarGUI );
            ToolbarExtender.RightToolbarGUI.Add( OnShortcutsGUI );
        }

        static void SaveSettings()
        {
            EditorPrefs.SetString( GetEditorSettingsKey(), JsonUtility.ToJson( Settings ) );
        }

        static void LoadSettings()
        {
            if (Settings == null)
            {
                Settings = new SceneInspectorSettings();
            }

            var settingsKeyData = EditorPrefs.GetString( GetEditorSettingsKey() );
            if (settingsKeyData == "")
            {
                SaveSettings();
            }

            JsonUtility.FromJsonOverwrite( EditorPrefs.GetString( GetEditorSettingsKey() ), Settings );
            Shortcuts = new HashSet<string>( Settings.scenePaths.ToList() );
        }

        static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup( EditorApplication.isPlaying );
            CreatePlayButton();
            CreateSceneChangeButton();
            EditorGUI.EndDisabledGroup();

            CreateSceneAddButton();

            EditorGUI.BeginDisabledGroup( EditorApplication.isPlaying );
            CreateSettingsButton();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        static void OnShortcutsGUI()
        {
            if (!EditorApplication.isPlaying && Shortcuts.Count > 0)
            {
                var scenes = Shortcuts.ToArray();
                string[] sceneNames = scenes.ToArray();

                for (int i = 0; i < sceneNames.Length; ++i)
                {
                    sceneNames[i] = GetSceneNameFromPath( sceneNames[i] );
                }

                int selection = GUILayout.Toolbar( -1, sceneNames, GUILayout.Height( Height ) );
                if (selection != -1)
                {
                    SwitchScene( scenes[selection] );
                }

                GUILayout.FlexibleSpace();
            }
        }

        static void SwitchScene( object scene )
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene( (string) scene );
            }
        }

        static void AddScene( object scene )
        {
            if (EditorApplication.isPlaying)
            {
                EditorSceneManager.LoadScene( (string) scene, LoadSceneMode.Additive );
            }
            else
            {
                EditorSceneManager.OpenScene( (string) scene, OpenSceneMode.Additive );
            }
        }

        static void CreatePlayButton()
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = EditorApplication.isPlaying ? Color.red : Color.green;

            GUIContent playContent = new GUIContent();

            if (EditorApplication.isPlaying)
            {
                playContent.text = "Play Mode";
            }
            else
            {
                playContent.image = EditorGUIUtility.IconContent( "Animation.Play" ).image;
            }

            playContent.tooltip = "Play game from first scene";

            if (GUILayout.Button( playContent, GUILayout.Height( Height ) ))
            {
                if (!EditorApplication.isPlaying)
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene( EditorBuildSettings.scenes[0].path );
                        EditorApplication.isPlaying = true;
                    }
                }
            }

            GUI.backgroundColor = oldColor;
        }

        static void CreateSceneChangeButton()
        {
            GUIContent changeSceneContent = new GUIContent();
            changeSceneContent.text = " " + SceneManager.GetActiveScene().name;
            changeSceneContent.image = EditorGUIUtility.IconContent( "BuildSettings.Editor.Small" ).image;
            changeSceneContent.tooltip = "Change active scene";

            if (GUILayout.Button( changeSceneContent, GUILayout.Height( Height ) ) && !EditorApplication.isPlaying)
            {
                GenericMenu menu = new GenericMenu();
                FillScenesMenu( menu, SwitchScene );
                menu.ShowAsContext();
            }
        }

        static void CreateSceneAddButton()
        {
            GUIContent changeSceneContent = new GUIContent();
            changeSceneContent.image = EditorGUIUtility.IconContent( "Toolbar Plus More" ).image;
            changeSceneContent.tooltip = "Open scene in additive mode";

            if (GUILayout.Button( changeSceneContent, GUILayout.Height( Height ) ))
            {
                GenericMenu menu = new GenericMenu();
                FillScenesMenu( menu, AddScene, false );
                menu.ShowAsContext();
            }
        }

        static void FillScenesMenu( GenericMenu menu, GenericMenu.MenuFunction2 callback, bool showActiveScene = true )
        {
            if (Settings.OnlyIncludedScenes)
            {
                if (EditorBuildSettings.scenes.Length == 0)
                {
                    Debug.LogWarning( "[DCTools:SceneInspector] There is no scenes defined in build settings." );
                }
                else foreach (var scene in EditorBuildSettings.scenes)
                {
                    menu.AddItem( new GUIContent( GetSceneNameFromPath( scene.path ) ),
                        scene.path == SceneManager.GetActiveScene().path && showActiveScene,
                        callback,
                        scene.path );
                }
            }
            else
            {
                var scenes = AssetDatabase.FindAssets( "t:Scene" );
                foreach (var t in scenes)
                {
                    var path = AssetDatabase.GUIDToAssetPath( t );
                    menu.AddItem( new GUIContent( GetSceneNameFromPath( path ) ),
                        path == SceneManager.GetActiveScene().path && showActiveScene,
                        callback,
                        path );
                }
            }
        }

        static void CreateSettingsButton()
        {
            GUIContent settingsContent = new GUIContent();
            settingsContent.image = EditorGUIUtility.IconContent( "_Popup" ).image;
            settingsContent.tooltip = "Scene inspector settings";

            if (GUILayout.Button( settingsContent, GUILayout.Height( Height ) ))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem( new GUIContent( "Create new scene/Empty" ), false, () =>
                {
                    EditorSceneManager.NewScene( NewSceneSetup.EmptyScene, NewSceneMode.Single );
                } );

                menu.AddItem( new GUIContent( "Create new scene/Empty - Additive" ), false, () =>
                {
                    EditorSceneManager.NewScene( NewSceneSetup.EmptyScene, NewSceneMode.Additive );
                } );

                menu.AddItem( new GUIContent( "Create new scene/Default" ), false, () =>
                {
                    EditorSceneManager.NewScene( NewSceneSetup.DefaultGameObjects, NewSceneMode.Single );
                } );

                menu.AddItem( new GUIContent( "Create new scene/Default - Additive" ), false, () =>
                {
                    EditorSceneManager.NewScene( NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive );
                } );

                menu.AddItem( new GUIContent( "Show only scenes included in build" ), Settings.OnlyIncludedScenes, () =>
                {
                    Settings.OnlyIncludedScenes = !Settings.OnlyIncludedScenes;
                    SaveSettings();
                } );

                menu.AddSeparator( "/" );
                FetchShortcutScenes( menu );
                menu.AddSeparator( "Pin scene to toolbar/" );

                menu.AddItem( new GUIContent( "Pin scene to toolbar/Clear" ), false, () =>
                {
                    Shortcuts.Clear();
                    Settings.scenePaths = Shortcuts.ToArray();
                    SaveSettings();
                } );

                menu.ShowAsContext();
            }
        }

        static public void FetchShortcutScenes( GenericMenu menu )
        {
            if (Settings.OnlyIncludedScenes)
            {
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    var path = scene.path;
                    var sceneName = System.IO.Path.GetFileNameWithoutExtension( path.Split( '/' ).Last() );
                    menu.AddItem( new GUIContent( "Pin scene to toolbar/" + sceneName ), Shortcuts.Contains( path ), () =>
                    {
                        if (!Shortcuts.Add( path ))
                        {
                            Shortcuts.Remove( path );
                        }

                        Settings.scenePaths = Shortcuts.ToArray();
                        SaveSettings();
                    } );
                }
            }
            else
            {
                var scenes = AssetDatabase.FindAssets( "t:Scene" );
                foreach (var t in scenes)
                {
                    var path = AssetDatabase.GUIDToAssetPath( t );
                    var sceneName = System.IO.Path.GetFileNameWithoutExtension( path.Split( '/' ).Last() );
                    menu.AddItem( new GUIContent( "Pin scene to toolbar/" + sceneName ), Shortcuts.Contains( path ), () =>
                    {
                        if (!Shortcuts.Add( path ))
                        {
                            Shortcuts.Remove( path );
                        }

                        Settings.scenePaths = Shortcuts.ToArray();
                        SaveSettings();
                    } );
                }
            }
        }

        static public string GetSceneNameFromPath( string path )
        {
            return System.IO.Path.GetFileNameWithoutExtension( path.Split( '/' ).Last() );
        }

        public static string GetEditorSettingsKey()
        {
            return "DCTools:" + Application.productName + ":Settings";
        }
    }
}
