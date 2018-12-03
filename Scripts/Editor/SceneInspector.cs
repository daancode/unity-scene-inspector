using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;

namespace Quario.Tools.Editor
{
    [InitializeOnLoad]
    public class SceneInspector
    {
        static bool ShowAllScenesFromProject = true;

        static SceneInspector()
        {
            if(!EditorPrefs.HasKey("QuarioToolbox:Inspector:ShowAllScenesFromProject"))
            {
                EditorPrefs.SetBool("QuarioToolbox:Inspector:ShowAllScenesFromProject", true);
            }

            ShowAllScenesFromProject = EditorPrefs.GetBool("QuarioToolbox:Inspector:ShowAllScenesFromProject");
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
        }

        static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            EditorGUILayout.BeginHorizontal();
            CreateSettingsButton();
            CreatePlayButton();
            CreateSceneChangeButton();
            EditorGUI.EndDisabledGroup();
            CreateSceneAddButton();
            EditorGUILayout.EndHorizontal();
        }

        static void SwitchScene(object scene)
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene((string) scene);
            }
        }

        static void AddScene(object scene)
        {
            if (EditorApplication.isPlaying)
            {
                EditorSceneManager.LoadScene((string)scene, LoadSceneMode.Additive);
            }
            else
            {
                EditorSceneManager.OpenScene((string)scene, OpenSceneMode.Additive);
            }
        }

        static void CreatePlayButton()
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;

            GUIContent playContent = new GUIContent();
            playContent.image = EditorGUIUtility.IconContent("preAudioPlayOff").image;
            playContent.tooltip = "Play game from first scene";

            if (GUILayout.Button(playContent, GUILayout.Height(23f)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
                    EditorApplication.isPlaying = true;
                }
            }

            GUI.backgroundColor = oldColor;
        }

        static void CreateSceneChangeButton()
        {
            GUIContent changeSceneContent = new GUIContent();
            changeSceneContent.text = " " + SceneManager.GetActiveScene().name;
            changeSceneContent.image = EditorGUIUtility.IconContent("BuildSettings.SelectedIcon").image;
            changeSceneContent.tooltip = "Change active scene";

            if (GUILayout.Button(changeSceneContent, GUILayout.Height(23f)) && !EditorApplication.isPlaying)
            {
                GenericMenu menu = new GenericMenu();
                FillScenesMenu(menu, SwitchScene);
                menu.ShowAsContext();
            }
        }

        static void CreateSceneAddButton()
        {
            GUIContent changeSceneContent = new GUIContent();
            changeSceneContent.image = EditorGUIUtility.IconContent("Toolbar Plus More").image;
            changeSceneContent.tooltip = "Open scene in additive mode";

            if (GUILayout.Button(changeSceneContent, GUILayout.Height(23f)))
            {
                GenericMenu menu = new GenericMenu();
                FillScenesMenu(menu, AddScene);
                menu.ShowAsContext();
            }
        }

        static void FillScenesMenu(GenericMenu menu, GenericMenu.MenuFunction2 callback)
        {
            if (!ShowAllScenesFromProject)
            {
                if (EditorBuildSettings.scenes.Length == 0)
                {
                    Debug.LogWarning("[Quario:SceneInspector] There is no scenes defined in build settings.");
                }
                else foreach (var scene in EditorBuildSettings.scenes)
                {
                    var sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path.Split('/').Last());
                    menu.AddItem(new GUIContent(sceneName), scene.path == SceneManager.GetActiveScene().path, callback, scene.path);
                }
            }
            else
            {
                var scenes = AssetDatabase.FindAssets("t:Scene");
                foreach (var t in scenes)
                {
                    var path = AssetDatabase.GUIDToAssetPath(t);
                    var sceneName = System.IO.Path.GetFileNameWithoutExtension(path.Split('/').Last());
                    menu.AddItem(new GUIContent(sceneName), path == SceneManager.GetActiveScene().path, callback, path);
                }
            }
        }

        static void CreateSettingsButton()
        {
            GUIContent settingsContent = new GUIContent();
            settingsContent.image = EditorGUIUtility.IconContent("_Popup").image;
            settingsContent.tooltip = "Scene inspector settings";

            if (GUILayout.Button(settingsContent, GUILayout.Height(23f)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Show all scenes from project"), ShowAllScenesFromProject, 
                    ToggleOption, !ShowAllScenesFromProject);
                menu.ShowAsContext();
            }
        }

        static void ToggleOption(object option)
        {
            ShowAllScenesFromProject = (bool)option;
            EditorPrefs.SetBool("QuarioToolbox:Inspector:ShowAllScenesFromProject", ShowAllScenesFromProject);
        }
    }
}