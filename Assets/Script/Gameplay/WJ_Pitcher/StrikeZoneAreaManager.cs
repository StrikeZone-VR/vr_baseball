using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StrikeZoneArea
{
    public string areaName;
    public Transform areaTransform;
    public float probability;
    public bool isStrike;

    public StrikeZoneArea(string name, Transform transform, float prob, bool strike)
    {
        areaName = name;
        areaTransform = transform;
        probability = prob;
        isStrike = strike;
    }
}

public class StrikeZoneAreaManager : MonoBehaviour
{
    [Header("스트라이크 존 영역 설정")]
    public Transform strikeZoneParent;

    [Header("�️ 25개 영역 시스템 (9 스트라이크 + 16 볼)")]
    [Range(0f, 100f)]
    public float strikeZoneTotalProbability = 65f;  // 스트라이크존 전체 확률
    [Range(0f, 100f)]
    public float ballZoneTotalProbability = 35f;    // 볼존 전체 확률

    [Header("🎯 스트라이크존 내부 확률 분배")]
    [Range(0f, 50f)]
    public float middleCenterProbability = 20f;     // 정중앙 확률 (스트라이크존 내)
    [Range(0f, 80f)]  
    public float edgeStrikeProbability = 80f;       // 가장자리 8개 확률 (스트라이크존 내)

    [Header("⚾ 볼존 설정")]
    public Vector3 ballAreaSize = new Vector3(0.25f, 0.35f, 0.1f);
    public float ballAreaSpacing = 0.3f;            // 볼 영역 간격
    public bool showBallAreas = true;               // 볼 영역 시각화

    private List<StrikeZoneArea> allAreas = new List<StrikeZoneArea>();
    private List<StrikeZoneArea> strikeAreas = new List<StrikeZoneArea>();
    private List<StrikeZoneArea> ballAreas = new List<StrikeZoneArea>();

    void Start()
    {
        InitializeAreas();
    }

    private void InitializeAreas()
    {
        // 스트라이크 존 자식 오브젝트들 수집
        if (strikeZoneParent != null)
        {
            CollectStrikeZoneAreas();
        }

        // 볼 영역들 생성
        CreateBallAreas();

        // 확률 정규화
        NormalizeProbabilities();

        Debug.Log($"✅ 영역 초기화 완료 - 스트라이크: {strikeAreas.Count}개, 볼: {ballAreas.Count}개, 총: {allAreas.Count}개");
    }

    private void CollectStrikeZoneAreas()
    {
        for (int i = 0; i < strikeZoneParent.childCount; i++)
        {
            Transform child = strikeZoneParent.GetChild(i);
            
            // MiddleCenter인지 확인
            bool isMiddleCenter = child.name.Contains("Center") && child.name.Contains("Middle");
            
            // 새로운 확률 시스템: 스트라이크존 총 확률을 내부적으로 분배
            float strikeZoneIndividualProb;
            if (isMiddleCenter)
            {
                // 정중앙은 middleCenterProbability% (스트라이크존 내)
                strikeZoneIndividualProb = strikeZoneTotalProbability * (middleCenterProbability / 100f);
            }
            else
            {
                // 나머지 8개 영역은 edgeStrikeProbability%를 8개로 분배 (스트라이크존 내)
                strikeZoneIndividualProb = strikeZoneTotalProbability * (edgeStrikeProbability / 100f) / 8f;
            }

            StrikeZoneArea area = new StrikeZoneArea(child.name, child, strikeZoneIndividualProb, true);
            strikeAreas.Add(area);
            allAreas.Add(area);
        }
        
        Debug.Log($"🎯 스트라이크존 초기화: 9개 영역, 총 확률 {strikeZoneTotalProbability}%");
    }

    private void CreateBallAreas()
    {
        // 🎯 16개 볼존 생성: 5x5 그리드에서 중앙 3x3 스트라이크존 제외
        
        // 스트라이크존 경계 계산
        Bounds strikeBounds = CalculateStrikeZoneBounds();
        
        // 5x5 그리드 생성 (총 25개 위치 중 16개 볼존)
        List<Vector3> ballPositions = Calculate16BallPositions(strikeBounds);
        
        // 각 볼 영역의 확률 (총 ballZoneTotalProbability를 16개로 분배)
        float individualBallProb = ballZoneTotalProbability / 16f;
        
        for (int i = 0; i < ballPositions.Count; i++)
        {
            // 볼 영역 GameObject 생성
            GameObject ballAreaObj = new GameObject($"Ball_Area_{i}");
            ballAreaObj.transform.position = ballPositions[i];
            ballAreaObj.transform.parent = transform;
            
            // BoxCollider 추가 (충돌 감지용)
            BoxCollider collider = ballAreaObj.AddComponent<BoxCollider>();
            collider.size = ballAreaSize;
            collider.isTrigger = true;
            
            // 시각화용 큐브 생성
            if (showBallAreas)
            {
                CreateBallAreaVisual(ballAreaObj, i);
            }

            StrikeZoneArea ballArea = new StrikeZoneArea($"Ball_Area_{i}", ballAreaObj.transform, individualBallProb, false);
            ballAreas.Add(ballArea);
            allAreas.Add(ballArea);
        }
        
        Debug.Log($"✅ 16개 볼존 생성 완료! 각 영역 확률: {individualBallProb:F1}%");
    }
    
    private Bounds CalculateStrikeZoneBounds()
    {
        if (strikeAreas.Count == 0) return new Bounds(strikeZoneParent.position, Vector3.one);
        
        Bounds bounds = new Bounds();
        bool boundsSet = false;
        
        foreach (var area in strikeAreas)
        {
            if (area.areaTransform != null)
            {
                Collider collider = area.areaTransform.GetComponent<Collider>();
                if (collider != null)
                {
                    if (!boundsSet)
                    {
                        bounds = collider.bounds;
                        boundsSet = true;
                    }
                    else
                    {
                        bounds.Encapsulate(collider.bounds);
                    }
                }
            }
        }
        
        return bounds;
    }
    
    private List<Vector3> Calculate16BallPositions(Bounds strikeBounds)
    {
        List<Vector3> positions = new List<Vector3>();
        Vector3 center = strikeBounds.center;
        Vector3 size = strikeBounds.size;
        
        // 5x5 그리드의 셀 크기 계산
        float cellWidth = (size.x + ballAreaSpacing * 2) / 4f;   // 5칸이므로 4개 간격
        float cellHeight = (size.y + ballAreaSpacing * 2) / 4f;  // 5칸이므로 4개 간격
        
        // 그리드 시작점 (왼쪽 위)
        Vector3 gridStart = center + new Vector3(-cellWidth * 2, cellHeight * 2, 0);
        
        // 5x5 그리드에서 중앙 3x3 제외하고 16개 위치 계산
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                // 중앙 3x3 영역 건너뛰기 (스트라이크존 위치)
                if (row >= 1 && row <= 3 && col >= 1 && col <= 3)
                    continue;
                
                Vector3 position = gridStart + new Vector3(
                    col * cellWidth, 
                    -row * cellHeight, 
                    0
                );
                positions.Add(position);
            }
        }
        
        return positions;
    }
    
    private void CreateBallAreaVisual(GameObject ballArea, int index)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = $"Ball_Visual_{index}";
        visual.transform.parent = ballArea.transform;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = ballAreaSize;
        
        // 빨간색 반투명 머티리얼
        Renderer renderer = visual.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        material.SetFloat("_Mode", 3); // Transparent
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        renderer.material = material;
        
        // Play 모드에서 렌더러 비활성화 (Scene View에서만 보이도록)
        if (Application.isPlaying)
            renderer.enabled = false;
    }

    private void NormalizeProbabilities()
    {
        // 확률 정규화 (총 100%가 되도록)
        float totalProbability = 0f;
        foreach (var area in allAreas)
        {
            totalProbability += area.probability;
        }

        if (totalProbability > 0)
        {
            foreach (var area in allAreas)
            {
                area.probability = (area.probability / totalProbability) * 100f;
            }
        }
    }

    public Vector3 GetRandomTargetPosition()
    {
        if (allAreas.Count == 0)
        {
            Debug.LogError("영역이 초기화되지 않았습니다!");
            return strikeZoneParent.position;
        }

        // 누적 확률 기반 선택
        float randomValue = Random.Range(0f, 100f);
        float cumulativeProbability = 0f;

        foreach (var area in allAreas)
        {
            cumulativeProbability += area.probability;
            if (randomValue <= cumulativeProbability)
            {
                Vector3 targetPos = GetRandomPositionInArea(area);
                
                string strikeOrBall = area.isStrike ? "스트라이크" : "볼";
                Debug.Log($"🎯 선택된 영역: {area.areaName} ({strikeOrBall}) - 확률: {area.probability:F1}%");
                
                return targetPos;
            }
        }

        // 만약을 위한 기본값
        return allAreas[allAreas.Count - 1].areaTransform.position;
    }

    Vector3 GetRandomPositionInArea(StrikeZoneArea area)
    {
        Vector3 basePosition = area.areaTransform.position;
        
        if (area.isStrike)
        {
            // 스트라이크존 내 랜덤 위치
            Collider areaCollider = area.areaTransform.GetComponent<Collider>();
            if (areaCollider != null)
            {
                Bounds bounds = areaCollider.bounds;
                Vector3 randomOffset = new Vector3(
                    Random.Range(-bounds.size.x / 2f, bounds.size.x / 2f),
                    Random.Range(-bounds.size.y / 2f, bounds.size.y / 2f),
                    Random.Range(-bounds.size.z / 2f, bounds.size.z / 2f)
                );
                return basePosition + randomOffset;
            }
        }
        else
        {
            // 볼 영역 내 랜덤 위치
            Vector3 randomOffset = new Vector3(
                Random.Range(-ballAreaSize.x / 2f, ballAreaSize.x / 2f),
                Random.Range(-ballAreaSize.y / 2f, ballAreaSize.y / 2f),
                Random.Range(-ballAreaSize.z / 2f, ballAreaSize.z / 2f)
            );
            return basePosition + randomOffset;
        }
        
        return basePosition;
    }

    public bool IsStrikePosition(Vector3 position)
    {
        foreach (var area in strikeAreas)
        {
            Collider collider = area.areaTransform.GetComponent<Collider>();
            if (collider != null && collider.bounds.Contains(position))
                return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        // Scene View에서만 25개 영역 시각화
        if (allAreas != null && allAreas.Count > 0)
        {
            foreach (var area in allAreas)
            {
                if (area.areaTransform != null)
                {
                    // 영역별 색상 설정
                    if (area.isStrike)
                    {
                        // 스트라이크존: 중앙은 노란색, 나머지는 초록색
                        Gizmos.color = area.areaName.Contains("Center") ? Color.yellow : Color.green;
                    }
                    else
                    {
                        // 볼존: 빨간색
                        Gizmos.color = Color.red;
                    }

                    // 영역 박스 그리기
                    Vector3 boxSize = area.isStrike ? Vector3.one * 0.2f : ballAreaSize;
                    Gizmos.DrawWireCube(area.areaTransform.position, boxSize);

                    // 확률 라벨 표시 (Unity Editor에서만)
                    #if UNITY_EDITOR
                    string label = $"{area.areaName}\n{area.probability:F1}%";
                    UnityEditor.Handles.Label(area.areaTransform.position + Vector3.up * 0.15f, label);
                    #endif
                }
            }
            
            // 시스템 정보 표시
            #if UNITY_EDITOR
            if (strikeZoneParent != null)
            {
                Vector3 infoPos = strikeZoneParent.position + Vector3.up * 2f;
                string systemInfo = $"🏟️ 25개 영역 시스템\n" +
                                  $"✅ 스트라이크: {strikeAreas.Count}개 ({strikeZoneTotalProbability}%)\n" +
                                  $"❌ 볼: {ballAreas.Count}개 ({ballZoneTotalProbability}%)\n" +
                                  $"📊 총: {allAreas.Count}개 영역";
                UnityEditor.Handles.Label(infoPos, systemInfo);
            }
            #endif
        }
    }
}
