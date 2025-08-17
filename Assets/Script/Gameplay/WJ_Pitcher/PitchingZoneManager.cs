using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 🏟️ 25개 영역 투수 시스템 관리자
/// - 중앙 3x3 (9개) = 스트라이크존
/// - 주변 16개 = 볼존
/// - 확률 기반 타겟팅
/// - Scene View 전용 시각화
/// </summary>
public class PitchingZoneManager : MonoBehaviour
{
    [Header("🎯 기본 설정")]
    public Transform strikeZoneParent;

    [Header("📊 확률 설정")]
    [Range(0, 100)]
    public float strikeProbability = 65f; // 스트라이크 확률

    [Header("🎨 시각화 설정")]
    public bool showZonesInSceneView = true;
    public float zoneSize = 0.5f;

    // ==============================================
    // 💾 내부 데이터
    // ==============================================
    private List<PitchingZone> allZones = new List<PitchingZone>();
    private List<PitchingZone> strikeZones = new List<PitchingZone>();
    private List<PitchingZone> ballZones = new List<PitchingZone>();

    private GameObject visualContainer;
    private Vector3 zoneCenter = Vector3.zero;
    private const float GRID_SIZE = 5; // 5x5 그리드
    private const float ZONE_SPACING = 0.3f;

    // ==============================================
    // 🏗️ 시스템 초기화
    // ==============================================
    void Start()
    {
        Debug.Log("🏟️ 25개 영역 투수 시스템 초기화 시작...");
        InitializeZoneSystem();
    }

    private void InitializeZoneSystem()
    {
        FindStrikeZoneCenter();
        CreateZoneGrid();
        SetupStrikeZones();
        CreateBallZones();
        NormalizeProbabilities();
        CreateVisualization();

        Debug.Log($"✅ 초기화 완료! 스트라이크: {strikeZones.Count}개, 볼: {ballZones.Count}개, 총: {allZones.Count}개");
        LogProbabilityDistribution();
    }

    private void FindStrikeZoneCenter()
    {
        if (strikeZoneParent != null)
        {
            // StrikeZone 중심점 계산
            Bounds bounds = new Bounds();
            bool boundsSet = false;

            foreach (Transform child in strikeZoneParent)
            {
                if (child.gameObject.activeSelf)
                {
                    Renderer renderer = child.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        if (!boundsSet)
                        {
                            bounds = renderer.bounds;
                            boundsSet = true;
                        }
                        else
                        {
                            bounds.Encapsulate(renderer.bounds);
                        }
                    }
                }
            }

            if (boundsSet)
            {
                zoneCenter = bounds.center;
            }
        }

        // 기본값 설정
        if (zoneCenter == Vector3.zero)
        {
            zoneCenter = transform.position + new Vector3(0, 0.5f, -14f);
        }

        Debug.Log($"🎯 존 중심점: {zoneCenter}");
    }

    private void CreateZoneGrid()
    {
        allZones.Clear();

        // 5x5 그리드 생성
        for (int row = 0; row < GRID_SIZE; row++)
        {
            for (int col = 0; col < GRID_SIZE; col++)
            {
                Vector3 position = CalculateZonePosition(row, col);
                string zoneName = GetZoneName(row, col);
                bool isStrike = IsStrikeZone(row, col);

                PitchingZone zone = new PitchingZone(zoneName, position, isStrike, row, col);
                allZones.Add(zone);
            }
        }

        Debug.Log($"🏗️ {GRID_SIZE}x{GRID_SIZE} 그리드 생성 완료: {allZones.Count}개 영역");
    }

    private Vector3 CalculateZonePosition(int row, int col)
    {
        // 그리드의 중심을 기준으로 위치 계산
        float offsetX = (col - 2) * ZONE_SPACING; // -2부터 +2까지
        float offsetY = (2 - row) * ZONE_SPACING; // 상단이 +2, 하단이 -2

        return zoneCenter + new Vector3(offsetX, offsetY, 0);
    }

    private string GetZoneName(int row, int col)
    {
        if (IsStrikeZone(row, col))
        {
            // 스트라이크존 이름 (3x3 그리드)
            string[] rowNames = { "Top", "Middle", "Bottom" };
            string[] colNames = { "Left", "Center", "Right" };
            return $"{rowNames[row - 1]}{colNames[col - 1]}";
        }
        else
        {
            // 볼존 이름
            return $"BallZone_{row:D2}_{col:D2}";
        }
    }

    private bool IsStrikeZone(int row, int col)
    {
        // 중앙 3x3 영역이 스트라이크존 (인덱스 1,2,3의 행과 열)
        return row >= 1 && row <= 3 && col >= 1 && col <= 3;
    }

    // ==============================================
    // 🎯 스트라이크존 설정
    // ==============================================
    private void SetupStrikeZones()
    {
        strikeZones.Clear();

        foreach (var zone in allZones)
        {
            if (zone.isStrikeZone)
            {
                strikeZones.Add(zone);
            }
        }

        // 기존 StrikeZone 객체들과 연결
        if (strikeZoneParent != null)
        {
            StrikeZone[] existingStrikeZones = strikeZoneParent.GetComponentsInChildren<StrikeZone>();
            for (int i = 0; i < existingStrikeZones.Length && i < strikeZones.Count; i++)
            {
                strikeZones[i].linkedStrikeZone = existingStrikeZones[i];
            }
        }

        Debug.Log($"🎯 스트라이크존 {strikeZones.Count}개 설정 완료");
    }

    // ==============================================
    // ⚾ 볼존 생성
    // ==============================================
    private void CreateBallZones()
    {
        ballZones.Clear();

        foreach (var zone in allZones)
        {
            if (!zone.isStrikeZone)
            {
                ballZones.Add(zone);
            }
        }

        Debug.Log($"⚾ 볼존 {ballZones.Count}개 생성 완료");
    }

    // ==============================================
    // 📊 확률 정규화
    // ==============================================
    private void NormalizeProbabilities()
    {
        float ballProbability = 100f - strikeProbability;

        // 스트라이크존 확률 분배
        if (strikeZones.Count > 0)
        {
            float centerProb = strikeProbability * 0.3f; // 중앙에 30%
            float edgeProb = strikeProbability * 0.7f / 8f; // 나머지 8개에 분배

            foreach (var zone in strikeZones)
            {
                if (zone.zoneName == "MiddleCenter")
                {
                    zone.probability = centerProb;
                }
                else
                {
                    zone.probability = edgeProb;
                }
            }
        }

        // 볼존 확률 분배
        if (ballZones.Count > 0)
        {
            float ballZoneProb = ballProbability / ballZones.Count;
            foreach (var zone in ballZones)
            {
                zone.probability = ballZoneProb;
            }
        }

        // 총합 계산 및 검증
        float totalProb = allZones.Sum(z => z.probability);
        Debug.Log($"📊 확률 정규화 완료: 총합 {totalProb:F1}%");
    }

    private void LogProbabilityDistribution()
    {
        float strikeTotal = strikeZones.Sum(z => z.probability);
        float ballTotal = ballZones.Sum(z => z.probability);

        Debug.Log("📈 확률 분배 현황:");
        Debug.Log($"   🎯 스트라이크존: {strikeTotal:F1}% ({strikeZones.Count}개)");
        Debug.Log($"   ⚾ 볼존 전체: {ballTotal:F1}% ({ballZones.Count}개)");
    }

    // ==============================================
    // 🎯 랜덤 타겟 선택
    // ==============================================
    public Vector3 GetRandomTargetPosition()
    {
        if (allZones.Count == 0)
        {
            Debug.LogWarning("⚠️ 존이 초기화되지 않음!");
            return zoneCenter;
        }

        // 확률 기반 선택
        float randomValue = Random.Range(0f, 100f);
        float currentSum = 0f;

        foreach (var zone in allZones)
        {
            currentSum += zone.probability;
            if (randomValue <= currentSum)
            {
                string zoneType = zone.isStrikeZone ? "⚾ Strike" : "❌ Ball";
                Debug.Log($"🎯 선택: {zone.zoneName} ({zoneType}) - 확률: {zone.probability:F1}%");
                return zone.position;
            }
        }

        // 마지막 존 반환 (폴백)
        var lastZone = allZones.Last();
        string lastType = lastZone.isStrikeZone ? "⚾ Strike" : "❌ Ball";
        Debug.Log($"🎯 폴백 선택: {lastZone.zoneName} ({lastType})");
        return lastZone.position;
    }

    // ==============================================
    // ⚖️ 스트라이크/볼 판정
    // ==============================================
    public bool IsStrikePosition(Vector3 position)
    {
        if (allZones.Count == 0) return false;

        // 가장 가까운 존 찾기
        PitchingZone closestZone = null;
        float minDistance = float.MaxValue;

        foreach (var zone in allZones)
        {
            float distance = Vector3.Distance(position, zone.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestZone = zone;
            }
        }

        return closestZone != null && closestZone.isStrikeZone;
    }

    // ==============================================
    // 🎨 시각화
    // ==============================================
    private void CreateVisualization()
    {
        if (!showZonesInSceneView) return;

        // 기존 시각화 제거
        if (visualContainer != null)
        {
            if (Application.isPlaying)
                Destroy(visualContainer);
            else
                DestroyImmediate(visualContainer);
        }

        // 새 컨테이너 생성
        visualContainer = new GameObject("25Zone_Visualization");
        visualContainer.transform.SetParent(transform);

        foreach (var zone in allZones)
        {
            CreateZoneVisual(zone);
        }

        Debug.Log($"🎨 시각화 생성 완료: {allZones.Count}개 존");
    }

    private void CreateZoneVisual(PitchingZone zone)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = $"Zone_{zone.zoneName}";
        visual.transform.SetParent(visualContainer.transform);
        visual.transform.position = zone.position;
        visual.transform.localScale = Vector3.one * zoneSize;

        // 색상 설정
        Renderer renderer = visual.GetComponent<Renderer>();
        if (zone.isStrikeZone)
        {
            if (zone.zoneName == "MiddleCenter")
                renderer.material.color = Color.yellow; // 중앙
            else
                renderer.material.color = Color.green;  // 스트라이크존
        }
        else
        {
            renderer.material.color = Color.red; // 볼존
        }

        // 반투명 설정
        renderer.material.color = new Color(
            renderer.material.color.r,
            renderer.material.color.g,
            renderer.material.color.b,
            0.6f
        );

        // Collider 제거 (시각화 전용)
        if (visual.GetComponent<Collider>())
        {
            DestroyImmediate(visual.GetComponent<Collider>());
        }
    }

    void OnDrawGizmos()
    {
        if (!showZonesInSceneView || allZones == null) return;

        foreach (var zone in allZones)
        {
            // 색상 설정
            if (zone.isStrikeZone)
            {
                Gizmos.color = zone.zoneName == "MiddleCenter" ? Color.yellow : Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }

            // 반투명 설정
            Color color = Gizmos.color;
            color.a = 0.6f;
            Gizmos.color = color;

            // 박스 그리기
            Gizmos.DrawCube(zone.position, Vector3.one * zoneSize);

            // 테두리 그리기
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(zone.position, Vector3.one * zoneSize);
        }
    }
}

/// <summary>
/// 🎯 투구 존 데이터 클래스
/// </summary>
[System.Serializable]
public class PitchingZone
{
    public string zoneName;
    public Vector3 position;
    public float probability;
    public bool isStrikeZone;
    public int gridRow;
    public int gridCol;
    public StrikeZone linkedStrikeZone; // 기존 StrikeZone 연결

    public PitchingZone(string name, Vector3 pos, bool strike, int row, int col)
    {
        zoneName = name;
        position = pos;
        isStrikeZone = strike;
        gridRow = row;
        gridCol = col;
        probability = 0f;
        linkedStrikeZone = null;
    }
}
