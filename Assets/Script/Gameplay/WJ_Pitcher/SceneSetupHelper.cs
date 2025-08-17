using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SceneSetupInstructions : MonoBehaviour
{
    [Header("🏟️ 투수 연습 씬 설정 가이드")]
    [TextArea(10, 30)]
    public string setupInstructions = 
        "🎯 새로운 25구역 투수 시스템 설정 방법:\n\n" +
        "1️⃣ 기존 시스템 확인:\n" +
        "   • StrikeZoneAreaManager가 있다면 삭제하거나 비활성화\n" +
        "   • 기존 Ball_Area 오브젝트들이 있다면 삭제\n\n" +
        "2️⃣ 새로운 시스템 설정:\n" +
        "   • 빈 GameObject 생성하고 'PitchingSystem'으로 이름 변경\n" +
        "   • PitchingZoneManager 스크립트 컴포넌트 추가\n" +
        "   • Strike Zone Parent 필드에 스트라이크존 부모 오브젝트 연결\n\n" +
        "3️⃣ 스트라이크존 설정:\n" +
        "   • 스트라이크존 부모 오브젝트에 9개 영역이 있는지 확인\n" +
        "   • TopLeft, TopCenter, TopRight\n" +
        "   • MiddleLeft, MiddleCenter, MiddleRight  \n" +
        "   • BottomLeft, BottomCenter, BottomRight\n\n" +
        "4️⃣ VRBaseball 설정:\n" +
        "   • VRBaseball 스크립트의 Pitching Zone Manager 필드에\n" +
        "     위에서 만든 PitchingSystem 오브젝트 연결\n\n" +
        "5️⃣ 확률 설정 (Inspector에서):\n" +
        "   • Strike Zone Probability: 65% (스트라이크 확률)\n" +
        "   • Center Probability: 25% (중앙 영역 확률)\n" +
        "   • Edge Probability: 75% (가장자리 8영역 확률)\n" +
        "   • Inner Ball Probability: 70% (내부 볼존 확률)\n" +
        "   • Outer Ball Probability: 30% (외부 볼존 확률)\n\n" +
        "6️⃣ 시각화 설정:\n" +
        "   • Show In Scene View: true (Scene에서 보기)\n" +
        "   • Show In Play Mode: false (플레이 시 숨김)\n\n" +
        "✅ 설정 완료 후 플레이 모드에서 테스트해보세요!\n" +
        "   공이 25개 영역(9 스트라이크 + 16 볼) 중 하나를 선택해서\n" +
        "   확률적으로 날아갑니다.";
    
    [Header("📊 시스템 정보")]
    [TextArea(5, 10)]
    public string systemInfo = 
        "🔥 25구역 투수 시스템 특징:\n\n" +
        "• 스트라이크존 9개 + 볼존 16개 = 총 25개 영역\n" +
        "• 5x5 그리드 배치 (중앙 3x3가 스트라이크존)\n" +
        "• 확률 기반 타겟팅으로 리얼한 투구 연습\n" +
        "• Scene View에서 모든 영역을 시각적으로 확인 가능\n" +
        "• Play 모드에서는 영역이 숨겨져서 깔끔한 화면\n" +
        "• Inspector에서 실시간 확률 조정 가능";

    #if UNITY_EDITOR
    [ContextMenu("자동 시스템 설정")]
    public void AutoSetupSystem()
    {
        // PitchingSystem 오브젝트 찾기 또는 생성
        PitchingZoneManager existingManager = FindObjectOfType<PitchingZoneManager>();
        if (existingManager == null)
        {
            GameObject pitchingSystemObj = new GameObject("PitchingSystem");
            PitchingZoneManager manager = pitchingSystemObj.AddComponent<PitchingZoneManager>();
            
            // 스트라이크존 자동 찾기
            GameObject strikeZoneObj = GameObject.FindGameObjectWithTag("StrikeZone");
            if (strikeZoneObj == null)
            {
                // 이름으로 찾기
                Transform[] allTransforms = FindObjectsOfType<Transform>();
                foreach (Transform t in allTransforms)
                {
                    if (t.name.ToLower().Contains("strikezone") || 
                        (t.childCount >= 9 && t.Find("MiddleCenter") != null))
                    {
                        strikeZoneObj = t.gameObject;
                        break;
                    }
                }
            }
            
            if (strikeZoneObj != null)
            {
                manager.strikeZoneParent = strikeZoneObj.transform;
                Debug.Log($"✅ PitchingSystem 생성 완료! StrikeZone 연결: {strikeZoneObj.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ StrikeZone을 자동으로 찾을 수 없습니다. 수동으로 설정하세요.");
            }
            
            EditorUtility.SetDirty(pitchingSystemObj);
        }
        else
        {
            Debug.Log("✅ PitchingZoneManager가 이미 존재합니다.");
        }
        
        // VRBaseball 설정
        VRBaseball[] baseballs = FindObjectsOfType<VRBaseball>();
        foreach (VRBaseball baseball in baseballs)
        {
            if (baseball.pitchingZoneManager == null)
            {
                baseball.pitchingZoneManager = existingManager ?? FindObjectOfType<PitchingZoneManager>();
                EditorUtility.SetDirty(baseball);
                Debug.Log($"✅ VRBaseball '{baseball.name}' PitchingZoneManager 연결 완료");
            }
        }
        
        // 기존 StrikeZoneAreaManager 비활성화
        StrikeZoneAreaManager oldManager = FindObjectOfType<StrikeZoneAreaManager>();
        if (oldManager != null)
        {
            oldManager.enabled = false;
            Debug.Log("⚠️ 기존 StrikeZoneAreaManager 비활성화됨");
        }
    }
    
    [ContextMenu("기존 Ball_Area 정리")]
    public void CleanupOldBallAreas()
    {
        GameObject[] ballAreas = GameObject.FindGameObjectsWithTag("BallZone");
        int deletedCount = 0;
        
        foreach (GameObject obj in ballAreas)
        {
            DestroyImmediate(obj);
            deletedCount++;
        }
        
        // 이름으로도 찾아서 삭제
        Transform[] allTransforms = FindObjectsOfType<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name.StartsWith("Ball_Area"))
            {
                DestroyImmediate(t.gameObject);
                deletedCount++;
            }
        }
        
        Debug.Log($"🗑️ {deletedCount}개의 기존 Ball_Area 오브젝트 정리 완료");
    }
    #endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(SceneSetupInstructions))]
public class SceneSetupInstructionsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        SceneSetupInstructions setup = (SceneSetupInstructions)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🛠️ 자동 설정 도구", EditorStyles.boldLabel);
        
        if (GUILayout.Button("⚡ 자동 시스템 설정", GUILayout.Height(40)))
        {
            setup.AutoSetupSystem();
        }
        
        if (GUILayout.Button("🗑️ 기존 Ball_Area 정리"))
        {
            setup.CleanupOldBallAreas();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "위 버튼들을 순서대로 클릭하면 자동으로 새로운 시스템이 설정됩니다.\n" +
            "그 후 PitchingZoneManager Inspector에서 확률을 조정하세요.",
            MessageType.Info);
    }
}
#endif
