/// <summary>
/// 🛠️ Unity 에디터 도구 - PitchingSystemManager Inspector 커스텀 UI
/// </summary>

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PitchingModeManager))]
public class PitchingSystemManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GUILayout.Space(10);
        
        PitchingModeManager manager = (PitchingModeManager)target;
        
        // 정보 표시
        EditorGUILayout.LabelField("📊 시스템 정보", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"스트라이크존: {manager.GetStrikeZoneCount()}개");
        EditorGUILayout.LabelField($"볼존: {manager.GetBallZoneCount()}개");
        EditorGUILayout.LabelField($"총 영역: {manager.GetTotalZoneCount()}개");
        
        GUILayout.Space(10);
        
        // 설정 버튼들
        EditorGUILayout.LabelField("🛠️ 시스템 관리", EditorStyles.boldLabel);
        
        if (GUILayout.Button("🚀 시스템 초기화", GUILayout.Height(30)))
        {
            manager.InitializeSystem();
        }
        
        if (GUILayout.Button("👁️ 볼존 가시성 토글", GUILayout.Height(25)))
        {
            manager.ToggleBallZoneVisibility();
        }
        
        GUILayout.Space(10);
        
        // 도움말
        EditorGUILayout.HelpBox(
            "사용법:\n" +
            "1. Strike Zone Parent에 기존 스트라이크존 9개 영역을 설정\n" +
            "2. Homeplate를 홈플레이트 Transform에 연결\n" +
            "3. '시스템 초기화' 버튼 클릭\n" +
            "4. Scene View에서 25개 영역 확인",
            MessageType.Info);
    }
}
#endif
