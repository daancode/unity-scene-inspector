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
        static SceneInspector()
        {
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
        }

        static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            EditorGUILayout.BeginHorizontal();

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

                var scenes = AssetDatabase.FindAssets("t:Scene");
                foreach (var t in scenes)
                {
                    var path = AssetDatabase.GUIDToAssetPath(t);
                    menu.AddItem(new GUIContent(path.Split('/').Last()), path == SceneManager.GetActiveScene().path, SwitchScene, path);
                }

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

                var scenes = AssetDatabase.FindAssets("t:Scene");
                foreach (var t in scenes)
                {
                    var path = AssetDatabase.GUIDToAssetPath(t);
                    menu.AddItem(new GUIContent(path.Split('/').Last()), path == SceneManager.GetActiveScene().path, AddScene, path);
                }

                menu.ShowAsContext();
            }
        }
    }
}