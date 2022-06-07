using System.Collections.Generic;


#if !UNITY_2019_1_OR_NEWER
using UnityEngine.Experimental.UIElements;
#else
using UnityEngine.UIElements;
#endif

// ReSharper disable once CheckNamespace
namespace Daancode.Editor.Injectors
{
    public interface IToolbarInjector
    {
        void InjectGUI(VisualElement root, List<ToolbarHook.HookData> hooks);
    }
}