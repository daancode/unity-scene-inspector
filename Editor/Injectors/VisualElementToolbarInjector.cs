#if UNITY_EDITOR && UNITY_2019_1_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace Daancode.Editor.Injectors
{
    public class VisualElementToolbarInjector : IToolbarInjector
    {
        private const string LEFT_ZONE = "ToolbarZoneLeftAlign";
        private const string RIGHT_ZONE = "ToolbarZoneRightAlign";
        
        public void InjectGUI(VisualElement root, Action<Rect> onLeftGUI, Action<Rect> onRightGUI)
        {
            if (root == null)
            {
                return;
            }
            
            InjectInternal(root.Q(LEFT_ZONE), onLeftGUI);
            InjectInternal(root.Q(RIGHT_ZONE), onRightGUI);
        }

        private void InjectInternal(VisualElement zone, Action<Rect> onGUI)
        {
            var parent = new VisualElement();
            parent.style.flexGrow = 1f;
            parent.style.flexDirection = FlexDirection.Row;

            var container = new IMGUIContainer();
            container.style.flexGrow = 1f;
            if (onGUI != null)
            {
                container.onGUIHandler += () => onGUI(container.contentRect);
            }
            
            parent.Add(container);
            zone?.Add(parent);
        }
    }
}
#endif

