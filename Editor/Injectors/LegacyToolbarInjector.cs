using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if !UNITY_2019_1_OR_NEWER
using UnityEngine.Experimental.UIElements;
#else
using UnityEngine.UIElements;
#endif

// ReSharper disable once CheckNamespace
namespace Daancode.Editor.Injectors
{
    public class LegacyToolbarInjector  : IToolbarInjector
    {
        private readonly FieldInfo _imguiHandlerFieldInfo = typeof(IMGUIContainer).GetField("m_OnGUIHandler", ToolbarHook.FLAGS);

        private GUIStyle _commandStyle;
        private List<ToolbarHook.HookData> _hooks = new List<ToolbarHook.HookData>();
        
        public void InjectGUI(VisualElement root, List<ToolbarHook.HookData> hooks)
        {
            _hooks.Clear();
            _hooks.AddRange(hooks);
            
            var container = root[0] as IMGUIContainer;
            var handler = _imguiHandlerFieldInfo.GetValue(container) as Action;
            handler -= OnGUI;
            handler += OnGUI;
            _imguiHandlerFieldInfo.SetValue(container, handler);
        }

        private void OnGUI()
        {
            if (_commandStyle == null)
            {
                _commandStyle = new GUIStyle("Command");
            }
            
            var screenWidth = EditorGUIUtility.currentViewWidth;

#if UNITY_2019_1_OR_NEWER
            var playButtonsPosition = ( screenWidth - 140 ) / 2;
#else
            var playButtonsPosition = ( screenWidth - 100 ) / 2;
#endif

            var leftToolbarRect = new Rect( 0, 4, screenWidth, 24 );
            leftToolbarRect.xMin += 170 + 32 * GetToolsCount();
            leftToolbarRect.xMax = playButtonsPosition - 10;

            var rightToolbarRect = new Rect( 0, 4, screenWidth, 24 )
            {
                xMin = playButtonsPosition + 10 + _commandStyle.fixedWidth * 3,
                xMax = screenWidth - 420
            };
            
            HandleToolbar(leftToolbarRect, _hooks.Where(h => h.Align == ToolbarHook.Alignment.Left));
            HandleToolbar(rightToolbarRect, _hooks.Where(h => h.Align == ToolbarHook.Alignment.Right));
        }

        private static void HandleToolbar(Rect rect, IEnumerable<ToolbarHook.HookData> hooks)
        {
            using (new GUILayout.AreaScope(rect))
            {
                using (new GUILayout.HorizontalScope())
                {
                    foreach (var hook in hooks)
                    {
                        hook.Callback?.Invoke();
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

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var toolIcons = ToolbarHook.ToolbarType.GetField( fieldName, flags );

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
}
