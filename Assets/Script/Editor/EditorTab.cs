#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

public class EditorTab
{
    
    //
    // % : Ctrl, # : Shift, & : Alt
    //유니티 닫는 창 기능
    [MenuItem("Custom Shortcuts/Close Focused Window %w")] //ctrl w
    private static void CloseFocusedWindow()
    {
        if (EditorWindow.focusedWindow != null)
        {
            EditorWindow.focusedWindow.Close();
        }
        else
        {
            Debug.LogWarning("No window is currently focused.");
        }
    }
    
    //새로운 Project창
    [MenuItem("Custom Shortcuts/Open Project %#5")]
    private static void OpenProjectWindow()
    {
        
        EditorWindow focusedWindow = EditorWindow.focusedWindow;
        Type projectBrowserType = Type.GetType("UnityEditor.ProjectBrowser, UnityEditor");
        if (projectBrowserType != null)
        {
            EditorWindow projectWindow = ScriptableObject.CreateInstance(projectBrowserType) as EditorWindow;
            projectWindow.Show();
        }
    }
}
#endif
