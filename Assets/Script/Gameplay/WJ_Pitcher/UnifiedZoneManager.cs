using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 🎯 통합 존 관리자
/// 기존 StrikeZone 9개 + 추가 BallZone 16개 = 총 25개 영역 관리
/// </summary>
public class UnifiedZoneManager : MonoBehaviour
{
    [Header("🎯 기존 스트라이크존")]
    public Transform strikeZoneParent;

    [Header("⚾ 볼존 설정")]
    public Transform ballZoneParent;
    public GameObject ballZonePrefab;

    [Header("📊 확률 설정")]
    [Range(0, 100)]
    public float strikeProbability = 65f;

    [Header("🎨 시각화")]
    public bool showBallZones = true;
    public Material ballZoneMaterial;

    // ==============================================
    // 💾 데이터 구조
    // ==============================================
    [System.Serializable]
    public class ZoneData
    {
        public string zoneName;
        public Transform zoneTransform;
        public Vector3 position;
        public float probability;
        public bool isStrikeZone;

        public ZoneData(string name, Transform trans, Vector3 pos, float prob, bool isStrike)
        {
            zoneName = name;
            zoneTransform = trans;
            position = pos;
            probability = prob;
            isStrikeZone = isStrike;
        }
    }

    private List<ZoneData> allZones = new List<ZoneData>();
    private List<ZoneData> strikeZones = new List<ZoneData>();
    private List<ZoneData> ballZones = new List<ZoneData>();

    private Vector3 strikeZoneCenter;
    private Vector3 strikeZoneSize;

    // ==============================================
    // 🏗️ 시스템 초기화
    // ==============================================
    void Start()
    {
        Debug.Log("🏟️ 통합 25구역 시스템 초기화 시작...");

        // 구버전 시스템 비활성화
        DisableOldSystems();

        // 초기화
        InitializeSystem();
    }

    private void DisableOldSystems()
    {
        // PitchingZoneManager 비활성화
        PitchingZoneManager oldPitching = FindObjectOfType<PitchingZoneManager>();
        if (oldPitching != null)
        {
            oldPitching.enabled = false;
            Debug.Log("🔧 구버전 PitchingZoneManager 비활성화");
        }

        // StrikeZoneAreaManager 비활성화
        StrikeZoneAreaManager oldArea = FindObjectOfType<StrikeZoneAreaManager>();
        if (oldArea != null)
        {
            oldArea.enabled = false;
            Debug.Log("🔧 구버전 StrikeZoneAreaManager 비활성화");
        }
    }

    private void InitializeSystem()
    {
        // 1. 기존 스트라이크존 수집
        CollectExistingStrikeZones();

        // 2. 볼존 생성
        CreateBallZones();

        // 3. 확률 설정
        SetupProbabilities();

        // 4. VRBaseball 연결
        ConnectToVRBaseball();

        Debug.Log($"✅ 통합 시스템 초기화 완료! 스트라이크: {strikeZones.Count}개, 볼: {ballZones.Count}개, 총: {allZones.Count}개");
        LogSystemStatus();
    }

    // ==============================================
    // 🎯 기존 스트라이크존 수집
    // ==============================================
    private void CollectExistingStrikeZones()
    {
        strikeZones.Clear();

        if (strikeZoneParent == null)
        {
            Debug.LogError("❌ StrikeZone Parent가 설정되지 않았습니다!");
            return;
        }

        // 스트라이크존 경계 계산
        CalculateStrikeZoneBounds();

        // 기존 스트라이크존 수집
        foreach (Transform child in strikeZoneParent)
        {
            if (child.gameObject.activeSelf)
            {
                float probability = GetStrikeZoneProbability(child.name);
                ZoneData zone = new ZoneData(child.name, child, child.position, probability, true);
                strikeZones.Add(zone);
                allZones.Add(zone);
            }
        }

        Debug.Log($"🎯 기존 스트라이크존 {strikeZones.Count}개 수집 완료");
    }

    private void CalculateStrikeZoneBounds()
    {
        if (strikeZoneParent.childCount == 0) return;

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
            strikeZoneCenter = bounds.center;
            strikeZoneSize = bounds.size;
            Debug.Log($"📐 스트라이크존 경계: 중심 {strikeZoneCenter}, 크기 {strikeZoneSize}");
        }
    }

    private float GetStrikeZoneProbability(string zoneName)
    {
        // 중앙 영역은 높은 확률
        if (zoneName.Contains("Center") && zoneName.Contains("Middle"))
            return strikeProbability * 0.3f; // 30%

        // 나머지 8개 영역은 균등 분배
        return strikeProbability * 0.7f / 8f; // 나머지 70%를 8개로 분배
    }

    // ==============================================
    // ⚾ 볼존 생성
    // ==============================================
    private void CreateBallZones()
    {
        ballZones.Clear();

        // BallZone 부모 생성
        if (ballZoneParent == null)
        {
            GameObject ballParent = new GameObject("BallZones");
            ballParent.transform.parent = transform;
            ballZoneParent = ballParent.transform;
        }

        // 기존 볼존 삭제
        for (int i = ballZoneParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(ballZoneParent.GetChild(i).gameObject);
        }

        // 5x5 그리드에서 중앙 3x3 제외한 16개 영역 생성
        float spacing = Mathf.Max(strikeZoneSize.x, strikeZoneSize.y) / 2f; // 스트라이크존 크기 기반

        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                // 중앙 3x3 영역 제외 (스트라이크존)
                if (row >= 1 && row <= 3 && col >= 1 && col <= 3)
                    continue;

                Vector3 ballZonePos = CalculateBallZonePosition(row, col, spacing);
                string ballZoneName = $"BallZone_{row:D2}_{col:D2}";

                GameObject ballZoneObj = CreateBallZoneObject(ballZoneName, ballZonePos);

                float ballProb = (100f - strikeProbability) / 16f; // 볼 확률을 16개로 균등 분배
                ZoneData ballZone = new ZoneData(ballZoneName, ballZoneObj.transform, ballZonePos, ballProb, false);

                ballZones.Add(ballZone);
                allZones.Add(ballZone);
            }
        }

        Debug.Log($"⚾ 볼존 {ballZones.Count}개 생성 완료");
    }

    private Vector3 CalculateBallZonePosition(int row, int col, float spacing)
    {
        // 5x5 그리드의 위치 계산
        float offsetX = (col - 2) * spacing; // -2부터 +2까지
        float offsetY = (2 - row) * spacing; // 상단이 +2, 하단이 -2

        return strikeZoneCenter + new Vector3(offsetX, offsetY, 0);
    }

    private GameObject CreateBallZoneObject(string name, Vector3 position)
    {
        GameObject ballZone;

        if (ballZonePrefab != null)
        {
            ballZone = Instantiate(ballZonePrefab, ballZoneParent);
        }
        else
        {
            ballZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ballZone.transform.parent = ballZoneParent;

            // 기본 설정
            ballZone.transform.localScale = Vector3.one * 0.3f;

            // 콜라이더 설정
            BoxCollider collider = ballZone.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            // BallZone 스크립트 추가
            BallZone ballZoneScript = ballZone.AddComponent<BallZone>();
            ballZoneScript.SetupBallZone(this);

            // 머티리얼 설정
            if (ballZoneMaterial != null)
            {
                Renderer renderer = ballZone.GetComponent<Renderer>();
                renderer.material = ballZoneMaterial;
            }

            // 태그 설정
            ballZone.tag = "BallZone";
        }

        ballZone.name = name;
        ballZone.transform.position = position;
        ballZone.SetActive(showBallZones);

        return ballZone;
    }

    // ==============================================
    // 📊 확률 설정
    // ==============================================
    private void SetupProbabilities()
    {
        // 총 확률 검증
        float totalProb = allZones.Sum(z => z.probability);
        Debug.Log($"📊 총 확률: {totalProb:F1}%");

        // 정규화 (필요시)
        if (Mathf.Abs(totalProb - 100f) > 0.1f)
        {
            float factor = 100f / totalProb;
            foreach (var zone in allZones)
            {
                zone.probability *= factor;
            }
            Debug.Log($"📊 확률 정규화 완료");
        }
    }

    // ==============================================
    // 🔗 VRBaseball 연결
    // ==============================================
    private void ConnectToVRBaseball()
    {
        VRBaseball[] baseballs = FindObjectsOfType<VRBaseball>();
        foreach (VRBaseball baseball in baseballs)
        {
            // UnifiedZoneManager 연결
            baseball.unifiedZoneManager = this;
            Debug.Log($"🔗 VRBaseball '{baseball.name}' 연결 완료");
        }
    }

    // ==============================================
    // 🎯 랜덤 타겟 선택
    // ==============================================
    public Vector3 GetRandomTargetPosition()
    {
        if (allZones.Count == 0)
        {
            Debug.LogWarning("⚠️ 존이 초기화되지 않음!");
            return strikeZoneCenter;
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

        // 폴백
        var lastZone = allZones.Last();
        string lastType = lastZone.isStrikeZone ? "⚾ Strike" : "❌ Ball";
        Debug.Log($"🎯 폴백: {lastZone.zoneName} ({lastType})");
        return lastZone.position;
    }

    // ==============================================
    // ⚖️ 판정 시스템
    // ==============================================
    public bool IsStrikePosition(Vector3 position)
    {
        if (allZones.Count == 0) return false;

        // 가장 가까운 존 찾기
        ZoneData closestZone = null;
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

    public void HandleBallHit(Vector3 hitPosition, VRBaseball baseball)
    {
        bool isStrike = IsStrikePosition(hitPosition);

        if (isStrike)
        {
            Debug.Log("⚾ 스트라이크! 좋은 투구입니다!");
        }
        else
        {
            Debug.Log("❌ 볼! 아쉬운 투구네요.");
        }

        // 공 제거 및 새 공 스폰
        StartCoroutine(RespawnBall(baseball));
    }

    private System.Collections.IEnumerator RespawnBall(VRBaseball baseball)
    {
        yield return new WaitForSeconds(1f);
        
        // VRPitchingManager에 새 공 요청
        VRPitchingManager pitchingManager = FindObjectOfType<VRPitchingManager>();
        if (pitchingManager != null)
        {
            // public 메서드가 있다면 사용, 없으면 직접 처리
            Debug.Log("🎾 새 공 스폰 요청");
        }
    }    // ==============================================
    // 📈 디버깅 및 로그
    // ==============================================
    private void LogSystemStatus()
    {
        float strikeTotal = strikeZones.Sum(z => z.probability);
        float ballTotal = ballZones.Sum(z => z.probability);

        Debug.Log("📈 통합 시스템 현황:");
        Debug.Log($"   🎯 스트라이크존: {strikeTotal:F1}% ({strikeZones.Count}개)");
        Debug.Log($"   ⚾ 볼존: {ballTotal:F1}% ({ballZones.Count}개)");
    }

    // ==============================================
    // 🎨 에디터 기즈모
    // ==============================================
    void OnDrawGizmos()
    {
        if (ballZones == null) return;

        // 볼존 시각화
        Gizmos.color = Color.red;
        foreach (var ballZone in ballZones)
        {
            if (ballZone.zoneTransform != null)
            {
                Gizmos.DrawWireCube(ballZone.position, Vector3.one * 0.3f);
            }
        }

        // 스트라이크존 경계
        if (strikeZoneSize != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(strikeZoneCenter, strikeZoneSize);
        }
    }

    // ==============================================
    // 🔧 에디터 도구
    // ==============================================
    [ContextMenu("시스템 재초기화")]
    public void ReinitializeSystem()
    {
        allZones.Clear();
        strikeZones.Clear();
        ballZones.Clear();

        InitializeSystem();
    }

    [ContextMenu("볼존 가시성 토글")]
    public void ToggleBallZoneVisibility()
    {
        showBallZones = !showBallZones;

        foreach (var ballZone in ballZones)
        {
            if (ballZone.zoneTransform != null)
            {
                ballZone.zoneTransform.gameObject.SetActive(showBallZones);
            }
        }
    }
}
