using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 🔧 Unity 씬에서 자동으로 UnifiedZoneManager 설정
/// 기존 시스템을 새로운 통합 시스템으로 마이그레이션
/// </summary>
public class ZoneSystemMigrator : MonoBehaviour
{
    [Header("🔧 자동 설정")]
    [TextArea(5, 10)]
    public string instructions =
        "🎯 UnifiedZoneManager 자동 설정:\n\n" +
        "1. 'Auto Setup' 버튼 클릭\n" +
        "2. 기존 StrikeZone 9개 + 새로운 BallZone 16개 생성\n" +
        "3. 구버전 시스템 자동 비활성화\n" +
        "4. VRBaseball과 자동 연결\n\n" +
        "✅ 완전히 자동화된 설정입니다!";

    [Header("⚾ 볼존 설정")]
    public Material ballZoneMaterial;
    public Color ballZoneColor = new Color(1f, 0f, 0f, 0.3f); // 반투명 빨강

    // ==============================================
    // 🔧 자동 설정
    // ==============================================
    [ContextMenu("Auto Setup UnifiedZoneManager")]
    public void AutoSetupUnifiedSystem()
    {
        Debug.Log("🔧 UnifiedZoneManager 자동 설정 시작...");

        // 1. UnifiedZoneManager 생성
        CreateUnifiedZoneManager();

        // 2. 기존 시스템 비활성화
        DisableOldSystems();

        // 3. VRBaseball 연결
        ConnectVRBaseballs();

        Debug.Log("✅ UnifiedZoneManager 자동 설정 완료!");
    }

    private void CreateUnifiedZoneManager()
    {
        // 기존 UnifiedZoneManager 확인
        UnifiedZoneManager existing = FindObjectOfType<UnifiedZoneManager>();
        if (existing != null)
        {
            Debug.Log("✅ UnifiedZoneManager가 이미 존재합니다.");
            return;
        }

        // 새로운 GameObject 생성
        GameObject unifiedSystemObj = new GameObject("UnifiedZoneSystem");
        UnifiedZoneManager manager = unifiedSystemObj.AddComponent<UnifiedZoneManager>();

        // StrikeZone 부모 찾기
        Transform strikeZoneParent = FindStrikeZoneParent();
        if (strikeZoneParent != null)
        {
            manager.strikeZoneParent = strikeZoneParent;
            Debug.Log($"✅ StrikeZone 부모 연결: {strikeZoneParent.name}");
        }

        // 볼존 머티리얼 설정
        if (ballZoneMaterial != null)
        {
            manager.ballZoneMaterial = ballZoneMaterial;
        }
        else
        {
            // 기본 머티리얼 생성
            Material defaultMaterial = CreateDefaultBallZoneMaterial();
            manager.ballZoneMaterial = defaultMaterial;
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(unifiedSystemObj);
#endif
        Debug.Log("🎯 UnifiedZoneManager 생성 완료!");
    }

    private Transform FindStrikeZoneParent()
    {
        // 태그로 찾기
        GameObject strikeZoneObj = GameObject.FindGameObjectWithTag("StrikeZone");
        if (strikeZoneObj != null)
        {
            return strikeZoneObj.transform;
        }

        // 이름으로 찾기
        Transform[] allTransforms = FindObjectsOfType<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name.ToLower().Contains("strikezone"))
            {
                // 9개 자식이 있는지 확인
                if (t.childCount >= 9)
                {
                    return t;
                }
            }
        }

        // Hierarchy 탐색 (MainZoneVisual 등)
        foreach (Transform t in allTransforms)
        {
            if (t.Find("MiddleCenter") != null && t.Find("TopLeft") != null)
            {
                return t;
            }
        }

        Debug.LogWarning("⚠️ StrikeZone 부모를 자동으로 찾을 수 없습니다. 수동으로 설정하세요.");
        return null;
    }

    private Material CreateDefaultBallZoneMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = "BallZone_Material";
        mat.color = ballZoneColor;

        // 투명도 설정
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        return mat;
    }

    private void DisableOldSystems()
    {
        // PitchingZoneManager 비활성화
        PitchingZoneManager[] pitchingManagers = FindObjectsOfType<PitchingZoneManager>();
        foreach (var manager in pitchingManagers)
        {
            manager.enabled = false;
            Debug.Log($"🔧 PitchingZoneManager 비활성화: {manager.name}");
        }

        // StrikeZoneAreaManager 비활성화
        StrikeZoneAreaManager[] areaManagers = FindObjectsOfType<StrikeZoneAreaManager>();
        foreach (var manager in areaManagers)
        {
            manager.enabled = false;
            Debug.Log($"🔧 StrikeZoneAreaManager 비활성화: {manager.name}");
        }

        // PitchingSystem GameObject 제거
        GameObject pitchingSystem = GameObject.Find("PitchingSystem");
        if (pitchingSystem != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(pitchingSystem);
#else
            Destroy(pitchingSystem);
#endif
            Debug.Log("🔧 중복된 PitchingSystem 제거");
        }
    }

    private void ConnectVRBaseballs()
    {
        VRBaseball[] baseballs = FindObjectsOfType<VRBaseball>();
        UnifiedZoneManager manager = FindObjectOfType<UnifiedZoneManager>();

        if (manager == null)
        {
            Debug.LogError("❌ UnifiedZoneManager를 찾을 수 없습니다!");
            return;
        }

        foreach (VRBaseball baseball in baseballs)
        {
            baseball.unifiedZoneManager = manager;
#if UNITY_EDITOR
            EditorUtility.SetDirty(baseball);
#endif
            Debug.Log($"🔗 VRBaseball '{baseball.name}' 연결 완료");
        }
    }

    // ==============================================
    // 🧹 정리 도구
    // ==============================================
    [ContextMenu("Clean Old Ball Areas")]
    public void CleanOldBallAreas()
    {
        // Ball_Area로 시작하는 모든 오브젝트 제거
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int removedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("Ball_Area"))
            {
#if UNITY_EDITOR
                DestroyImmediate(obj);
#else
                Destroy(obj);
#endif
                removedCount++;
            }
        }

        Debug.Log($"🧹 기존 볼 영역 {removedCount}개 정리 완료");
    }

    [ContextMenu("Reset All Systems")]
    public void ResetAllSystems()
    {
        Debug.Log("🔄 모든 시스템 리셋 시작...");

        // 기존 UnifiedZoneManager 제거
        UnifiedZoneManager[] managers = FindObjectsOfType<UnifiedZoneManager>();
        foreach (var manager in managers)
        {
#if UNITY_EDITOR
            DestroyImmediate(manager.gameObject);
#else
            Destroy(manager.gameObject);
#endif
        }

        // 기존 볼존 정리
        CleanOldBallAreas();

        // 구버전 시스템 재활성화
        PitchingZoneManager[] pitchingManagers = FindObjectsOfType<PitchingZoneManager>();
        foreach (var manager in pitchingManagers)
        {
            manager.enabled = true;
        }

        StrikeZoneAreaManager[] areaManagers = FindObjectsOfType<StrikeZoneAreaManager>();
        foreach (var manager in areaManagers)
        {
            manager.enabled = true;
        }

        Debug.Log("🔄 시스템 리셋 완료");
    }
}

#if UNITY_EDITOR
/// <summary>
/// 에디터 UI 개선
/// </summary>
[CustomEditor(typeof(ZoneSystemMigrator))]
public class ZoneSystemMigratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        ZoneSystemMigrator migrator = (ZoneSystemMigrator)target;

        // 큰 버튼들
        if (GUILayout.Button("🚀 Auto Setup UnifiedZoneManager", GUILayout.Height(40)))
        {
            migrator.AutoSetupUnifiedSystem();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("🧹 Clean Old Ball Areas", GUILayout.Height(30)))
        {
            migrator.CleanOldBallAreas();
        }

        if (GUILayout.Button("🔄 Reset All Systems", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("시스템 리셋",
                "모든 UnifiedZoneManager를 제거하고 구버전으로 되돌리시겠습니까?",
                "예", "아니오"))
            {
                migrator.ResetAllSystems();
            }
        }

        // 현재 상태 표시
        GUILayout.Space(10);
        GUILayout.Label("📊 현재 상태:", EditorStyles.boldLabel);

        UnifiedZoneManager unified = FindObjectOfType<UnifiedZoneManager>();
        GUILayout.Label($"UnifiedZoneManager: {(unified != null ? "✅ 활성" : "❌ 없음")}");

        PitchingZoneManager pitching = FindObjectOfType<PitchingZoneManager>();
        GUILayout.Label($"PitchingZoneManager: {(pitching != null && pitching.enabled ? "🔄 활성" : "❌ 비활성")}");

        StrikeZoneAreaManager area = FindObjectOfType<StrikeZoneAreaManager>();
        GUILayout.Label($"StrikeZoneAreaManager: {(area != null && area.enabled ? "🔄 활성" : "❌ 비활성")}");
    }
}
#endif
