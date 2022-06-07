using System;
using System.Collections.Generic;
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
        public enum Alignment
        {
            Left,
            Right
        }
        
        public class HookData
        {
            public int Position = 0;
            public Action Callback;
            public Alignment Align;
        }

        public const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        public static readonly Type ToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        
        private static ScriptableObject _toolbar = null;
        private static VisualElement _toolbarRoot = null;
        private static readonly IToolbarInjector _toolbarInjector = null;
        private static readonly List<HookData> _toolbarHooks = new List<HookData>();
        
        public static bool NeedReload { get; private set; } = false;
        
        static ToolbarHook()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

#if UNITY_2021_3_OR_NEWER
            _toolbarInjector = new VisualElementToolbarInjector();
#else
            _toolbarInjector = new LegacyToolbarInjector();
#endif
        }

        public static void Register(Action hook, int position = 0, Alignment alignment = Alignment.Left)
        {
            if (_toolbarHooks.Exists(h => h.Callback == hook))
            {
                return;
            }

            if (position > _toolbarHooks.Count)
            {
                while (_toolbarHooks.Count < position)
                {
                    _toolbarHooks.Add(new HookData());
                }
            }

            NeedReload = true;
            _toolbarHooks?.Insert(position, new HookData
            {
                Position = position, 
                Callback = hook, 
                Align = alignment
            });
        }

        public static void Unregister(Action hook)
        {
            NeedReload = true;
            _toolbarHooks.RemoveAll(h => h.Callback == hook);
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
            
            if (_toolbar == null || (_toolbarRoot != null && !NeedReload))
            {
                return;
            }
            
            _toolbarRoot = ResolveToolbarRoot();
            _toolbarInjector.InjectGUI(_toolbarRoot, _toolbarHooks);
            NeedReload = false;
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
    }
}