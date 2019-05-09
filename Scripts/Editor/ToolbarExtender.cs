//  MIT License

//  Copyright(c) 2018 Marijn Zwemmer

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

// https://github.com/marijnz/unity-toolbar-extender

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if UNITY_2019_1_OR_NEWER
    using UnityEngine.UIElements;
#else
    using UnityEngine.Experimental.UIElements;
#endif

namespace UnityToolbarExtender
{
    public static class ToolbarCallback
    {
        static BindingFlags m_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        static Type m_toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        static Type m_guiViewType = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");
        static PropertyInfo m_viewVisualTree = m_guiViewType.GetProperty("visualTree", m_flags);
        static FieldInfo m_imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler", m_flags);
        static ScriptableObject m_currentToolbar;

        public static Action OnToolbarGUI;

        static ToolbarCallback()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            if (m_currentToolbar == null)
            {
                var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
                if (m_currentToolbar != null)
                {
                    var visualTree = (VisualElement)m_viewVisualTree.GetValue(m_currentToolbar, null);
                    var container = (IMGUIContainer)visualTree[0];
                    var handler = (Action)m_imguiContainerOnGui.GetValue(container);
                    handler -= OnGUI;
                    handler += OnGUI;
                    m_imguiContainerOnGui.SetValue(container, handler);
                }
            }
        }

        static void OnGUI()
        {
            if (OnToolbarGUI != null)
            {
                OnToolbarGUI.Invoke();
            }
        }
    }

    [InitializeOnLoad]
	public static class ToolbarExtender
	{
		static GUIStyle m_commandStyle = null;

		public static readonly List<Action> LeftToolbarGUI = new List<Action>();
		public static readonly List<Action> RightToolbarGUI = new List<Action>();

		static ToolbarExtender()
		{
            ToolbarCallback.OnToolbarGUI -= OnGUI;
			ToolbarCallback.OnToolbarGUI += OnGUI;
        }

		static void OnGUI()
		{
            if (m_commandStyle == null)
            {
                m_commandStyle = new GUIStyle("CommandLeft");
            }

            var screenWidth = EditorGUIUtility.currentViewWidth;

#if UNITY_2019_1_OR_NEWER
            float playButtonsPosition = (screenWidth - 140) / 2;
#else
            float playButtonsPosition = (screenWidth - 100) / 2;
#endif

            Rect leftToolbarRect = new Rect(0, 5, screenWidth, 24);
            leftToolbarRect.xMin += 392;
            leftToolbarRect.xMax = playButtonsPosition - 10;

            Rect rightToolbarRect = new Rect(0, 5, screenWidth, 24);
            rightToolbarRect.xMin = playButtonsPosition + 10 + (m_commandStyle.fixedWidth * 3);
            rightToolbarRect.xMax = screenWidth - 420;

            HandleCustomToolbar(LeftToolbarGUI, leftToolbarRect);
            HandleCustomToolbar(RightToolbarGUI, rightToolbarRect);
		}

        static void HandleCustomToolbar(List<Action> toolbar, Rect rect)
        {
            if (rect.width > 0)
            {
                GUILayout.BeginArea(rect);
                GUILayout.BeginHorizontal();
                foreach (var handler in toolbar)
                {
                    handler();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }
	}
}