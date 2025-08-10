using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[System.Serializable]
public class StrikeZoneAreaData
{
    public string areaName;
    public Vector3 localPosition;
    public Vector3 colliderSize;

    public StrikeZoneAreaData(string name, Vector3 pos, Vector3 size)
    {
        areaName = name;
        localPosition = pos;
        colliderSize = size;
    }
}

public class StrikeZoneSetupHelper : MonoBehaviour
{
    [Header("스트라이크존 설정")]
    public Transform strikeZoneParent;

    [Header("영역 크기 설정")]
    public Vector3 areaSize = new Vector3(0.167f, 0.33f, 0.1f);
    public float areaSpacing = 0.167f;

    [Header("자동 생성")]
    [SerializeField] private bool showPreview = true;

    private StrikeZoneAreaData[] areaDefinitions = {
        new StrikeZoneAreaData("TopLeft", new Vector3(-0.167f, 0.33f, 0), new Vector3(0.167f, 0.33f, 0.1f)),
        new StrikeZoneAreaData("TopCenter", new Vector3(0, 0.33f, 0), new Vector3(0.167f, 0.33f, 0.1f)),
        new StrikeZoneAreaData("TopRight", new Vector3(0.167f, 0.33f, 0), new Vector3(0.167f, 0.33f, 0.1f)),
        new StrikeZoneAreaData("MiddleLeft", new Vector3(-0.167f, 0, 0), new Vector3(0.167f, 0.33f, 0.1f)),
        new StrikeZoneAreaData("MiddleCenter", new Vector3(0, 0, 0), new Vector3(0.167f, 0.33f, 0.1f)),
        new StrikeZoneAreaData("MiddleRight", new Vector3(0.167f, 0, 0), new Vector3(0.167f, 0.33f, 0.1f)),
        new StrikeZoneAreaData("BottomLeft", new Vector3(-0.167f, -0.33f, 0), new Vector3(0.167f, 0.33f, 0.1f)),
        new StrikeZoneAreaData("BottomCenter", new Vector3(0, -0.33f, 0), new Vector3(0.167f, 0.33f, 0.1f)),
        new StrikeZoneAreaData("BottomRight", new Vector3(0.167f, -0.33f, 0), new Vector3(0.167f, 0.33f, 0.1f))
    };

    void Start()
    {
        if (strikeZoneParent == null)
        {
            strikeZoneParent = transform;
        }
    }

    [ContextMenu("Create Missing Strike Zone Areas")]
    public void CreateMissingAreas()
    {
        if (strikeZoneParent == null)
        {
            Debug.LogError("Strike Zone Parent가 설정되지 않았습니다!");
            return;
        }

        int createdCount = 0;

        foreach (var areaDef in areaDefinitions)
        {
            // 해당 이름의 자식이 이미 있는지 확인
            Transform existingArea = strikeZoneParent.Find(areaDef.areaName);

            if (existingArea == null)
            {
                // 영역 생성
                GameObject newArea = new GameObject(areaDef.areaName);
                newArea.transform.parent = strikeZoneParent;
                newArea.transform.localPosition = areaDef.localPosition;
                newArea.transform.localRotation = Quaternion.identity;
                newArea.transform.localScale = Vector3.one;

                // BoxCollider 추가
                BoxCollider collider = newArea.AddComponent<BoxCollider>();
                collider.size = areaDef.colliderSize;
                collider.isTrigger = true;

                // 레이어 설정 (필요시)
                newArea.layer = strikeZoneParent.gameObject.layer;

                createdCount++;
                Debug.Log($"스트라이크존 영역 생성: {areaDef.areaName}");
            }
            else
            {
                Debug.Log($"이미 존재함: {areaDef.areaName}");
            }
        }

        Debug.Log($"총 {createdCount}개의 새로운 스트라이크존 영역이 생성되었습니다.");

#if UNITY_EDITOR
        // 에디터에서 변경사항 저장
        EditorUtility.SetDirty(strikeZoneParent.gameObject);
#endif
    }

    [ContextMenu("Validate Existing Areas")]
    public void ValidateExistingAreas()
    {
        if (strikeZoneParent == null)
        {
            Debug.LogError("Strike Zone Parent가 설정되지 않았습니다!");
            return;
        }

        Debug.Log("=== 스트라이크존 영역 검증 ===");

        int validAreas = 0;
        int totalChildren = strikeZoneParent.childCount;

        foreach (var areaDef in areaDefinitions)
        {
            Transform existingArea = strikeZoneParent.Find(areaDef.areaName);

            if (existingArea != null)
            {
                BoxCollider collider = existingArea.GetComponent<BoxCollider>();
                if (collider != null && collider.isTrigger)
                {
                    Debug.Log($"✅ {areaDef.areaName}: 정상 (위치: {existingArea.localPosition})");
                    validAreas++;
                }
                else
                {
                    Debug.LogWarning($"⚠️ {areaDef.areaName}: BoxCollider가 없거나 Trigger가 아님");
                }
            }
            else
            {
                Debug.LogWarning($"❌ {areaDef.areaName}: 누락됨");
            }
        }

        Debug.Log($"검증 완료: {validAreas}/9개 영역이 올바르게 설정됨 (총 자식: {totalChildren}개)");

        if (validAreas < 9)
        {
            Debug.Log("누락된 영역이 있습니다. 'Create Missing Strike Zone Areas'를 실행하세요.");
        }
    }

    [ContextMenu("Reset All Areas")]
    public void ResetAllAreas()
    {
        if (strikeZoneParent == null)
        {
            Debug.LogError("Strike Zone Parent가 설정되지 않았습니다!");
            return;
        }

        if (!EditorUtility.DisplayDialog("영역 리셋",
            "모든 스트라이크존 영역을 삭제하고 다시 생성하시겠습니까?",
            "예", "아니오"))
        {
            return;
        }

        // 기존 영역들 삭제
        for (int i = strikeZoneParent.childCount - 1; i >= 0; i--)
        {
            Transform child = strikeZoneParent.GetChild(i);
            if (System.Array.Exists(areaDefinitions, area => area.areaName == child.name))
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // 새로 생성
        CreateMissingAreas();

        Debug.Log("모든 스트라이크존 영역이 리셋되었습니다.");
    }

    void OnDrawGizmos()
    {
        if (!showPreview || strikeZoneParent == null) return;

        Gizmos.color = Color.green;

        foreach (var areaDef in areaDefinitions)
        {
            Vector3 worldPos = strikeZoneParent.TransformPoint(areaDef.localPosition);

            // 영역이 이미 존재하는지 확인
            bool exists = strikeZoneParent.Find(areaDef.areaName) != null;

            if (exists)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }

            Gizmos.DrawWireCube(worldPos, areaDef.colliderSize);

#if UNITY_EDITOR
            Handles.Label(worldPos + Vector3.up * 0.1f, areaDef.areaName);
#endif
        }
    }
}

// 에디터 창용 스크립트
#if UNITY_EDITOR
[CustomEditor(typeof(StrikeZoneSetupHelper))]
public class StrikeZoneSetupHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StrikeZoneSetupHelper helper = (StrikeZoneSetupHelper)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("스트라이크존 설정 도구", EditorStyles.boldLabel);

        if (GUILayout.Button("현재 영역 검증"))
        {
            helper.ValidateExistingAreas();
        }

        if (GUILayout.Button("누락된 영역 생성"))
        {
            helper.CreateMissingAreas();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("모든 영역 리셋", GUILayout.Height(30)))
        {
            helper.ResetAllAreas();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1. Strike Zone Parent를 설정하세요\n" +
            "2. '현재 영역 검증'으로 상태 확인\n" +
            "3. '누락된 영역 생성'으로 영역 자동 생성\n" +
            "4. Scene 뷰에서 미리보기를 확인하세요",
            MessageType.Info);
    }
}
#endif

#endif
