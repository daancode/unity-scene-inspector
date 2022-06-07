#if UNITY_2019_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace Daancode.Editor.Injectors
{
    public class VisualElementToolbarInjector : IToolbarInjector
    {
        private const string LEFT_ZONE = "ToolbarZoneLeftAlign";
        private const string RIGHT_ZONE = "ToolbarZoneRightAlign";

        private VisualElement _leftParent = null;
        private VisualElement _rightParent = null;
        private readonly List<ToolbarHook.HookData> _hooks = new List<ToolbarHook.HookData>();
        
        public void InjectGUI(VisualElement root, List<ToolbarHook.HookData> hooks)
        {
            if (root == null)
            {
                return;
            }

            _hooks.Clear();
            _hooks.AddRange(hooks);
            
            _leftParent?.RemoveFromHierarchy();
            _rightParent?.RemoveFromHierarchy();
            
            _leftParent = InjectInternal(root.Q(LEFT_ZONE), _hooks.Where(h => h.Align == ToolbarHook.Alignment.Left));
            _rightParent = InjectInternal(root.Q(RIGHT_ZONE), _hooks.Where(h => h.Align == ToolbarHook.Alignment.Right));
        }

        private VisualElement InjectInternal(VisualElement zone, IEnumerable<ToolbarHook.HookData> hooks)
        {
            var parent = new VisualElement();
            parent.style.flexDirection = FlexDirection.Row;
            parent.style.flexGrow = 1f;
            var container = new IMGUIContainer();
            container.style.flexGrow = 1f;
            container.onGUIHandler += () =>
            {
                EditorGUILayout.BeginHorizontal();
                foreach (var hook in hooks)
                {
                    hook.Callback?.Invoke();
                }
                EditorGUILayout.EndHorizontal();
            };
            parent.Add(container);
            zone?.Add(parent);
            return parent;
        }
    }
}
#endif

