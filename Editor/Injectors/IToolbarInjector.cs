using System;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace Daancode.Editor.Injectors
{
    public interface IToolbarInjector
    {
        void InjectGUI(VisualElement root, Action<Rect> onLeftGUI, Action<Rect> onRightGUI);
    }
}