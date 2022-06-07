#if UNITY_EDITOR
using System;
using System.Reflection;
using Daancode.Editor.Injectors;
using UnityEditor;
using UnityEngine;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

// ReSharper disable once CheckNamespace
namespace Daancode.Editor
{
    // ToolbarExtender based on: https://github.com/marijnz/unity-toolbar-extender.
    [InitializeOnLoad]
    public static class ToolbarHook
    {
        public const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        public static readonly Type ToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        
        private static ScriptableObject _toolbar = null;
        private static VisualElement _toolbarRoot = null;
        private static readonly IToolbarInjector _toolbarInjector = null;
        
        public static event Action<Rect> OnLeftToolbarGUI;
        public static event Action<Rect> OnRightToolbarGUI;

        public static bool ShowDebugRect { get; set; } = true;

        static ToolbarHook()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

#if UNITY_2019_4_OR_NEWER
            _toolbarInjector = new VisualElementToolbarInjector();
#else
            _toolbarInjector = new LegacyToolbarInjector();
#endif
            if (ShowDebugRect)
            {
                OnLeftToolbarGUI += OnDebugGUI;
                OnRightToolbarGUI += OnDebugGUI;
            }
        }

        private static void OnUpdate()
        {
            if (_toolbarInjector == null)
            {
                Debug.LogError("[SceneInspector] Unsupported unity editor.");
                EditorApplication.update -= OnUpdate;
                return;
            }
            
            if (_toolbar == null)
            {
                var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
                _toolbar = toolbars.Length > 0 ? (ScriptableObject) toolbars[0] : null;
            }

            if (_toolbar == null)
            {
                Debug.LogError("[SceneInspector] Unable to locate toolbar asset.");
                return;
            }

            if (_toolbarRoot != null)
            {
                return;
            }
            
            _toolbarRoot = ResolveToolbarRoot();
            _toolbarInjector.InjectGUI(_toolbarRoot, OnLeftToolbarGUI, OnRightToolbarGUI);
            EditorApplication.update -= OnUpdate;
        }
        
        private static VisualElement ResolveToolbarRoot()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            
            object visualTree = null;
            PropertyInfo visualTreeProperty = null;
            
#if UNITY_2020_1_OR_NEWER
            var windowBackendType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.IWindowBackend");
            visualTreeProperty = windowBackendType.GetProperty("visualTree", flags);
            visualTree = ToolbarType.GetProperty("windowBackend", flags)?.GetValue(_toolbar);
#else
            visualTreeProperty = ToolbarType.GetProperty("visualTree", flags);
            visualTree = _toolbar;
#endif
            
            return visualTree != null ? visualTreeProperty?.GetValue(visualTree) as VisualElement : null;
        }
        
        private static void OnDebugGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, Color.red);
        }
    }
}
#endif