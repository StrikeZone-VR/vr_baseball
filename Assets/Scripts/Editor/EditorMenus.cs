#if UNITY_EDITOR

using UnityEditor;

static class EditorMenus
{
    [MenuItem("Tools/Toggle Inspector Lock %l")] // Ctrl + L
    static void ToggleInspectorLock()
    {
        ActiveEditorTracker.sharedTracker.isLocked = !ActiveEditorTracker.sharedTracker.isLocked;
        ActiveEditorTracker.sharedTracker.ForceRebuild();
    }
    
}
#endif
