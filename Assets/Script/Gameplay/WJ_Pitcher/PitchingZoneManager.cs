using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class PitchingZoneArea
{
    public string areaName;
    public Transform areaTransform;
    public float probability;
    public bool isStrike;
    public Color visualColor;
    public Vector3 areaSize;
    
    public PitchingZoneArea(string name, Transform transform, float prob, bool strike, Color color, Vector3 size)
    {
        areaName = name;
        areaTransform = transform;
        probability = prob;
        isStrike = strike;
        visualColor = color;
        areaSize = size;
    }
}

public class PitchingZoneManager : MonoBehaviour
{
    [Header("🎯 25개 영역 시스템")]
    [Tooltip("스트라이크존(9개) + 볼존(16개) = 총 25개 영역")]
    
    [Header("📊 확률 설정")]
    [Range(40f, 80f)]
    public float strikeZoneProbability = 65f;  // 스트라이크존 전체 확률
    
    [Header("🎯 스트라이크존 내부 확률 분배")]
    [Range(10f, 40f)]
    public float centerProbability = 25f;      // 중앙(MiddleCenter) 확률
    [Range(60f, 90f)]  
    public float edgeProbability = 75f;        // 나머지 8개 영역 확률 합계
    
    [Header("⚾ 볼존 확률 분배")]
    [Range(60f, 90f)]
    public float innerBallProbability = 70f;   // 스트라이크존 바로 인접한 8개 영역
    [Range(10f, 40f)]
    public float outerBallProbability = 30f;   // 가장 바깥쪽 8개 영역
    
    [Header("🎨 시각화 설정")]
    public bool showInSceneView = true;
    public bool showInPlayMode = false;
    
    [Header("📐 영역 크기 설정")]
    public Vector3 strikeAreaSize = new Vector3(0.167f, 0.33f, 0.1f);
    public Vector3 ballAreaSize = new Vector3(0.2f, 0.35f, 0.1f);
    public float areaSpacing = 0.05f;
    
    [Header("🔗 참조")]
    public Transform strikeZoneParent;
    
    // 영역 컨테이너들
    private List<PitchingZoneArea> allZones = new List<PitchingZoneArea>();
    private List<PitchingZoneArea> strikeZones = new List<PitchingZoneArea>();
    private List<PitchingZoneArea> ballZones = new List<PitchingZoneArea>();
    
    // 시각화 컨테이너
    private GameObject ballZoneContainer;
    
    void Start()
    {
        InitializeZoneSystem();
    }
    
    private void InitializeZoneSystem()
    {
        Debug.Log("🏟️ 25개 영역 투수 시스템 초기화 시작...");
        
        // 기존 볼존 정리
        CleanupOldBallZones();
        
        // 스트라이크존 수집 및 설정
        SetupStrikeZones();
        
        // 볼존 생성
        CreateBallZones();
        
        // 확률 정규화
        NormalizeProbabilities();
        
        // 시각화 설정
        SetupVisualization();
        
        Debug.Log($"✅ 초기화 완료! 스트라이크: {strikeZones.Count}개, 볼: {ballZones.Count}개, 총: {allZones.Count}개");
        LogProbabilityDistribution();
    }
    
    private void CleanupOldBallZones()
    {
        // 태그 대신 이름으로만 검색해서 기존 Ball_Area 오브젝트들 삭제
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int cleanedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("Ball_Area") || obj.name.StartsWith("BallZone_"))
            {
                DestroyImmediate(obj);
                cleanedCount++;
            }
        }
        
        // 자식 오브젝트들도 확인
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Ball_Area_") || child.name.StartsWith("BallZone_"))
            {
                DestroyImmediate(child.gameObject);
                cleanedCount++;
                i--; // 인덱스 조정
            }
        }
        
        if (cleanedCount > 0)
        {
            Debug.Log($"🗑️ {cleanedCount}개의 기존 Ball_Area 오브젝트 정리 완료");
        }
    }
    
    private void SetupStrikeZones()
    {
        if (strikeZoneParent == null)
        {
            Debug.LogError("❌ StrikeZone Parent가 설정되지 않았습니다!");
            return;
        }
        
        strikeZones.Clear();
        
        for (int i = 0; i < strikeZoneParent.childCount; i++)
        {
            Transform child = strikeZoneParent.GetChild(i);
            
            // 중앙인지 확인
            bool isCenter = child.name.ToLower().Contains("center") && child.name.ToLower().Contains("middle");
            
            // 확률 계산
            float individualProb;
            if (isCenter)
            {
                individualProb = strikeZoneProbability * (centerProbability / 100f);
            }
            else
            {
                individualProb = strikeZoneProbability * (edgeProbability / 100f) / 8f; // 8개로 나눔
            }
            
            // 색상 설정
            Color zoneColor = isCenter ? Color.yellow : Color.green;
            
            PitchingZoneArea strikeZone = new PitchingZoneArea(
                child.name, 
                child, 
                individualProb, 
                true, 
                zoneColor, 
                strikeAreaSize
            );
            
            strikeZones.Add(strikeZone);
            allZones.Add(strikeZone);
        }
        
        Debug.Log($"🎯 스트라이크존 {strikeZones.Count}개 설정 완료");
    }
    
    private void CreateBallZones()
    {
        ballZones.Clear();
        
        // 볼존 컨테이너 생성
        if (ballZoneContainer != null) DestroyImmediate(ballZoneContainer);
        ballZoneContainer = new GameObject("BallZones_Container");
        ballZoneContainer.transform.parent = transform;
        ballZoneContainer.transform.localPosition = Vector3.zero;
        
        // 스트라이크존 경계 계산
        Bounds strikeBounds = CalculateStrikeZoneBounds();
        
        // 5x5 그리드에서 16개 볼존 위치 계산
        List<BallZoneData> ballPositions = Calculate25GridPositions(strikeBounds);
        
        foreach (var ballData in ballPositions)
        {
            CreateBallZoneAt(ballData);
        }
        
        Debug.Log($"⚾ 볼존 {ballZones.Count}개 생성 완료");
    }
    
    [System.Serializable]
    public class BallZoneData
    {
        public Vector3 position;
        public string zoneName;
        public bool isInnerBall; // 스트라이크존 바로 인접한 8개 영역
        
        public BallZoneData(Vector3 pos, string name, bool inner)
        {
            position = pos;
            zoneName = name;
            isInnerBall = inner;
        }
    }
    
    private List<BallZoneData> Calculate25GridPositions(Bounds strikeBounds)
    {
        List<BallZoneData> ballData = new List<BallZoneData>();
        
        Vector3 center = strikeBounds.center;
        Vector3 strikeSize = strikeBounds.size;
        
        // 5x5 그리드 셀 크기 (스트라이크존 + 여유공간 포함)
        float cellWidth = (strikeSize.x + areaSpacing * 2) / 3f;   // 3x3 스트라이크존 기준
        float cellHeight = (strikeSize.y + areaSpacing * 2) / 3f;
        
        // 그리드 시작점 (5x5 그리드의 왼쪽 위)
        Vector3 gridOrigin = center + new Vector3(-cellWidth * 2, cellHeight * 2, 0);
        
        int ballIndex = 0;
        
        // 5x5 그리드 순회
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                // 중앙 3x3 영역(스트라이크존) 건너뛰기
                if (row >= 1 && row <= 3 && col >= 1 && col <= 3)
                    continue;
                
                Vector3 position = gridOrigin + new Vector3(col * cellWidth, -row * cellHeight, center.z);
                
                // 내부/외부 볼존 구분
                bool isInnerBall = IsInnerBallZone(row, col);
                
                string zoneName = $"BallZone_{ballIndex:D2}_{GetZoneLocationName(row, col)}";
                
                ballData.Add(new BallZoneData(position, zoneName, isInnerBall));
                ballIndex++;
            }
        }
        
        return ballData;
    }
    
    private bool IsInnerBallZone(int row, int col)
    {
        // 스트라이크존(1-3, 1-3) 바로 인접한 영역들
        // 즉, 한 칸 간격으로 둘러싸는 영역들
        
        // 상단 중앙 (row=0, col=1,2,3)
        if (row == 0 && col >= 1 && col <= 3) return true;
        
        // 하단 중앙 (row=4, col=1,2,3)  
        if (row == 4 && col >= 1 && col <= 3) return true;
        
        // 좌측 중앙 (col=0, row=1,2,3)
        if (col == 0 && row >= 1 && row <= 3) return true;
        
        // 우측 중앙 (col=4, row=1,2,3)
        if (col == 4 && row >= 1 && row <= 3) return true;
        
        return false; // 나머지는 외부 볼존
    }
    
    private string GetZoneLocationName(int row, int col)
    {
        if (row == 0)
        {
            if (col == 0) return "TopLeft";
            if (col == 1) return "TopCenterLeft";
            if (col == 2) return "TopCenter";
            if (col == 3) return "TopCenterRight";
            if (col == 4) return "TopRight";
        }
        else if (row == 1)
        {
            if (col == 0) return "MiddleLeft";
            if (col == 4) return "MiddleRight";
        }
        else if (row == 2)
        {
            if (col == 0) return "CenterLeft";
            if (col == 4) return "CenterRight";
        }
        else if (row == 3)
        {
            if (col == 0) return "LowerMiddleLeft";
            if (col == 4) return "LowerMiddleRight";
        }
        else if (row == 4)
        {
            if (col == 0) return "BottomLeft";
            if (col == 1) return "BottomCenterLeft";
            if (col == 2) return "BottomCenter";
            if (col == 3) return "BottomCenterRight";
            if (col == 4) return "BottomRight";
        }
        
        return $"Row{row}Col{col}";
    }
    
    private void CreateBallZoneAt(BallZoneData ballData)
    {
        // 볼존 GameObject 생성
        GameObject ballZoneObj = new GameObject(ballData.zoneName);
        ballZoneObj.transform.parent = ballZoneContainer.transform;
        ballZoneObj.transform.position = ballData.position;
        // 태그는 설정하지 않음 (오류 방지)
        
        // Collider 추가
        BoxCollider collider = ballZoneObj.AddComponent<BoxCollider>();
        collider.size = ballAreaSize;
        collider.isTrigger = true;
        
        // 확률 계산
        float ballZoneTotalProb = 100f - strikeZoneProbability;
        float individualProb;
        
        if (ballData.isInnerBall)
        {
            // 내부 볼존 8개: 70% 확률을 8개로 분배
            individualProb = ballZoneTotalProb * (innerBallProbability / 100f) / 8f;
        }
        else
        {
            // 외부 볼존 8개: 30% 확률을 8개로 분배  
            individualProb = ballZoneTotalProb * (outerBallProbability / 100f) / 8f;
        }
        
        // 시각화 색상 설정
        Color zoneColor = ballData.isInnerBall ? 
            new Color(1f, 0.5f, 0f, 0.7f) :  // 주황색 (내부)
            new Color(1f, 0.2f, 0.2f, 0.7f); // 빨간색 (외부)
        
        // 볼존 데이터 생성
        PitchingZoneArea ballZone = new PitchingZoneArea(
            ballData.zoneName,
            ballZoneObj.transform,
            individualProb,
            false,
            zoneColor,
            ballAreaSize
        );
        
        ballZones.Add(ballZone);
        allZones.Add(ballZone);
        
        // 시각화 큐브 생성
        CreateZoneVisual(ballZoneObj, ballZone);
    }
    
    private void CreateZoneVisual(GameObject parentObj, PitchingZoneArea zone)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = $"Visual_{zone.areaName}";
        visual.transform.parent = parentObj.transform;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = zone.areaSize;
        
        // Collider 제거 (부모에 이미 있음)
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null) DestroyImmediate(visualCollider);
        
        // 머티리얼 설정
        Renderer renderer = visual.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = zone.visualColor;
        
        // 반투명 설정
        material.SetFloat("_Mode", 3); // Transparent
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        
        renderer.material = material;
        
        // Play 모드에서 렌더링 제어
        renderer.enabled = showInPlayMode || !Application.isPlaying;
    }
    
    private Bounds CalculateStrikeZoneBounds()
    {
        if (strikeZones.Count == 0 || strikeZoneParent == null)
        {
            return new Bounds(transform.position, Vector3.one);
        }
        
        Bounds totalBounds = new Bounds();
        bool boundsInitialized = false;
        
        foreach (var zone in strikeZones)
        {
            if (zone.areaTransform != null)
            {
                Collider collider = zone.areaTransform.GetComponent<Collider>();
                if (collider != null)
                {
                    if (!boundsInitialized)
                    {
                        totalBounds = collider.bounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        totalBounds.Encapsulate(collider.bounds);
                    }
                }
            }
        }
        
        return totalBounds;
    }
    
    private void NormalizeProbabilities()
    {
        float totalProb = allZones.Sum(zone => zone.probability);
        
        if (totalProb > 0)
        {
            foreach (var zone in allZones)
            {
                zone.probability = (zone.probability / totalProb) * 100f;
            }
        }
        
        Debug.Log($"📊 확률 정규화 완료: 총합 {totalProb:F1}% -> 100%");
    }
    
    private void SetupVisualization()
    {
        // 모든 시각화 오브젝트의 렌더링 상태 설정
        foreach (var zone in allZones)
        {
            if (zone.areaTransform != null)
            {
                Transform visualChild = zone.areaTransform.Find($"Visual_{zone.areaName}");
                if (visualChild != null)
                {
                    Renderer renderer = visualChild.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = showInPlayMode || (!Application.isPlaying && showInSceneView);
                    }
                }
            }
        }
    }
    
    private void LogProbabilityDistribution()
    {
        float strikeTotal = strikeZones.Sum(z => z.probability);
        float ballTotal = ballZones.Sum(z => z.probability);
        float innerBallTotal = ballZones.Where(z => z.areaName.Contains("Middle") || z.areaName.Contains("Center")).Sum(z => z.probability);
        
        Debug.Log($"📈 확률 분배 현황:");
        Debug.Log($"   🎯 스트라이크존: {strikeTotal:F1}% ({strikeZones.Count}개)");
        Debug.Log($"   ⚾ 볼존 전체: {ballTotal:F1}% ({ballZones.Count}개)");
        Debug.Log($"   🔸 내부 볼존: {innerBallTotal:F1}%");
        Debug.Log($"   🔹 외부 볼존: {(ballTotal - innerBallTotal):F1}%");
    }
    
    // 공개 메서드: 랜덤 타겟 위치 반환
    public Vector3 GetRandomTargetPosition()
    {
        if (allZones.Count == 0)
        {
            Debug.LogError("❌ 영역이 초기화되지 않았습니다!");
            return transform.position;
        }
        
        // 누적 확률 기반 선택
        float randomValue = Random.Range(0f, 100f);
        float cumulativeProb = 0f;
        
        foreach (var zone in allZones)
        {
            cumulativeProb += zone.probability;
            if (randomValue <= cumulativeProb)
            {
                Vector3 targetPos = GetRandomPositionInZone(zone);
                
                string zoneType = zone.isStrike ? "⚾ Strike" : "❌ Ball";
                Debug.Log($"🎯 선택: {zone.areaName} ({zoneType}) - 확률: {zone.probability:F1}%");
                
                return targetPos;
            }
        }
        
        // 마지막 영역 반환 (안전장치)
        return GetRandomPositionInZone(allZones.Last());
    }
    
    private Vector3 GetRandomPositionInZone(PitchingZoneArea zone)
    {
        Vector3 basePos = zone.areaTransform.position;
        Vector3 size = zone.areaSize;
        
        Vector3 randomOffset = new Vector3(
            Random.Range(-size.x / 2f, size.x / 2f),
            Random.Range(-size.y / 2f, size.y / 2f),
            Random.Range(-size.z / 2f, size.z / 2f)
        );
        
        return basePos + randomOffset;
    }
    
    public bool IsStrikePosition(Vector3 position)
    {
        foreach (var zone in strikeZones)
        {
            if (zone.areaTransform != null)
            {
                Collider collider = zone.areaTransform.GetComponent<Collider>();
                if (collider != null && collider.bounds.Contains(position))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    // 통계 정보 반환
    public (int strikes, int balls, int total) GetZoneStatistics()
    {
        return (strikeZones.Count, ballZones.Count, allZones.Count);
    }
    
    // Inspector에서 설정 변경 시 실시간 업데이트
    void OnValidate()
    {
        if (Application.isPlaying && allZones.Count > 0)
        {
            // 확률 재계산
            SetupStrikeZones();
            CreateBallZones();
            NormalizeProbabilities();
            SetupVisualization();
        }
    }
    
    // Scene View Gizmos
    void OnDrawGizmos()
    {
        if (!showInSceneView || allZones == null || allZones.Count == 0) return;
        
        foreach (var zone in allZones)
        {
            if (zone.areaTransform != null)
            {
                Gizmos.color = zone.visualColor;
                Gizmos.DrawWireCube(zone.areaTransform.position, zone.areaSize);
                
                #if UNITY_EDITOR
                Vector3 labelPos = zone.areaTransform.position + Vector3.up * (zone.areaSize.y / 2f + 0.1f);
                string label = $"{zone.areaName}\n{zone.probability:F1}%";
                UnityEditor.Handles.Label(labelPos, label);
                #endif
            }
        }
        
        // 시스템 정보 표시
        #if UNITY_EDITOR
        if (strikeZoneParent != null)
        {
            Vector3 infoPos = strikeZoneParent.position + Vector3.up * 2.5f;
            string info = $"🏟️ 25구역 투수 시스템\n" +
                         $"🎯 스트라이크: {strikeZones.Count}개 ({strikeZoneProbability}%)\n" +
                         $"⚾ 볼: {ballZones.Count}개 ({(100 - strikeZoneProbability)}%)\n" +
                         $"📊 총 {allZones.Count}개 영역";
            UnityEditor.Handles.Label(infoPos, info);
        }
        #endif
    }
}
