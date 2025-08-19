/// <summary>
/// 🎯 투수 연습 시스템 통합 관리자 - 스트라이크존 9개 + 볼존 16개 (25존 시스템)
/// </summary>

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 🎯 투수 연습 시스템 통합 관리자
/// - 스트라이크존 9개 (3x3 중앙)
/// - 볼존 16개 (5x5 그리드의 바깥쪽)
/// - 확률 기반 타겟팅
/// - 시각적 피드백
/// </summary>
public class PitchingSystemManager : MonoBehaviour
{
    [Header("🎯 존 설정")]
    public Transform strikeZoneParent;
    
    [Header("📊 확률 설정")]
    [Range(0, 100)]
    public float strikeProbability = 60f;
    
    [Header("🎨 시각화")]
    public bool showZonesInEditor = true;
    public bool showZonesInPlay = false;
    public Material strikeZoneMaterial;
    public Material ballZoneMaterial;
    
    [Header("⚙️ 존 크기")]
    public Vector3 zoneSize = new Vector3(0.167f, 0.33f, 0.1f);
    public float zoneSpacing = 0.167f;
    
    // ==============================================
    // 💾 내부 데이터
    // ==============================================
    [System.Serializable]
    public class PitchZone
    {
        public string name;
        public Vector3 position;
        public GameObject zoneObject;
        public bool isStrikeZone;
        public float probability;
        
        public PitchZone(string n, Vector3 pos, bool strike, float prob)
        {
            name = n;
            position = pos;
            isStrikeZone = strike;
            probability = prob;
        }
    }
    
    private List<PitchZone> allZones = new List<PitchZone>();
    private List<PitchZone> strikeZones = new List<PitchZone>();
    private List<PitchZone> ballZones = new List<PitchZone>();
    private Transform ballZoneParent;
    
    // 스트라이크존 중심점과 크기 (기존 9개 영역 기준)
    private Vector3 strikeZoneCenter;
    private Vector3 strikeZoneBounds;
    
    void Start()
    {
        InitializeSystem();
    }
    
    // ==============================================
    // 🚀 시스템 초기화
    // ==============================================
    public void InitializeSystem()
    {
        Debug.Log("🚀 투수 시스템 초기화 시작!");
        
        // 기존 시스템 정리
        ClearExistingSystems();
        
        // 스트라이크존 분석
        AnalyzeExistingStrikeZones();
        
        // 볼존 생성
        CreateBallZones();
        
        // 확률 계산
        CalculateProbabilities();
        
        // 시각화
        UpdateVisualization();
        
        Debug.Log($"✅ 시스템 초기화 완료! 스트라이크: {strikeZones.Count}개, 볼: {ballZones.Count}개, 총: {allZones.Count}개");
    }
    
    private void ClearExistingSystems()
    {
        // 기존 볼존 제거
        if (ballZoneParent != null)
        {
            for (int i = ballZoneParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(ballZoneParent.GetChild(i).gameObject);
            }
        }
        
        allZones.Clear();
        strikeZones.Clear();
        ballZones.Clear();
    }
    
    private void AnalyzeExistingStrikeZones()
    {
        if (strikeZoneParent == null || strikeZoneParent.childCount == 0)
        {
            Debug.LogError("스트라이크존 부모가 없거나 자식 영역이 없습니다!");
            return;
        }
        
        // 스트라이크존 중심점과 경계 계산
        Bounds bounds = new Bounds();
        bool boundsSet = false;
        
        foreach (Transform child in strikeZoneParent)
        {
            if (child.gameObject.activeInHierarchy)
            {
                Collider collider = child.GetComponent<Collider>();
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
                
                // 스트라이크존 리스트에 추가
                PitchZone strikeZone = new PitchZone(
                    child.name,
                    child.position,
                    true,
                    0f // 나중에 계산
                );
                strikeZone.zoneObject = child.gameObject;
                
                strikeZones.Add(strikeZone);
                allZones.Add(strikeZone);
            }
        }
        
        if (boundsSet)
        {
            strikeZoneCenter = bounds.center;
            strikeZoneBounds = bounds.size;
            Debug.Log($"📏 스트라이크존 분석: 중심={strikeZoneCenter}, 크기={strikeZoneBounds}");
        }
    }
    
    private void CreateBallZones()
    {
        // 볼존 부모 생성
        if (ballZoneParent == null)
        {
            GameObject ballParent = new GameObject("BallZones");
            ballParent.transform.SetParent(transform);
            ballZoneParent = ballParent.transform;
        }
        
        // 5x5 그리드에서 중앙 3x3 제외한 16개 영역 생성
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                // 중앙 3x3 영역은 스트라이크존이므로 제외
                if (row >= 1 && row <= 3 && col >= 1 && col <= 3)
                    continue;
                
                Vector3 ballZonePos = CalculateBallZonePosition(row, col);
                string ballZoneName = $"BallZone_{row:D2}_{col:D2}";
                
                GameObject ballZoneObj = CreateBallZoneObject(ballZoneName, ballZonePos);
                
                PitchZone ballZone = new PitchZone(
                    ballZoneName,
                    ballZonePos,
                    false,
                    0f // 나중에 계산
                );
                ballZone.zoneObject = ballZoneObj;
                
                ballZones.Add(ballZone);
                allZones.Add(ballZone);
            }
        }
        
        Debug.Log($"⚾ 볼존 {ballZones.Count}개 생성 완료");
    }
    
    private Vector3 CalculateBallZonePosition(int row, int col)
    {
        // 5x5 그리드 기준으로 위치 계산
        // (0,0)이 좌상단, (4,4)가 우하단
        float offsetX = (col - 2) * zoneSpacing; // -2 ~ +2
        float offsetY = (2 - row) * zoneSpacing; // +2 ~ -2 (위가 +)
        
        return strikeZoneCenter + new Vector3(offsetX, offsetY, 0);
    }
    
    private GameObject CreateBallZoneObject(string name, Vector3 position)
    {
        // 큐브 생성
        GameObject ballZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ballZone.name = name;
        ballZone.transform.SetParent(ballZoneParent);
        ballZone.transform.position = position;
        ballZone.transform.localScale = zoneSize;
        
        // 트리거 콜라이더로 설정
        Collider collider = ballZone.GetComponent<Collider>();
        collider.isTrigger = true;
        
        // BallZone 스크립트 추가
        BallZone ballZoneScript = ballZone.AddComponent<BallZone>();
        ballZoneScript.SetupBallZone(this);
        
        // 머티리얼 설정
        Renderer renderer = ballZone.GetComponent<Renderer>();
        if (ballZoneMaterial != null)
        {
            renderer.material = ballZoneMaterial;
        }
        else
        {
            // 기본 빨간 반투명 머티리얼 생성
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            SetupTransparentMaterial(material);
            renderer.material = material;
        }
        
        // 태그 설정
        ballZone.tag = "BallZone";
        
        // Play 모드일 때 가시성 설정
        renderer.enabled = Application.isPlaying ? showZonesInPlay : showZonesInEditor;
        
        return ballZone;
    }
    
    private void SetupTransparentMaterial(Material material)
    {
        material.SetFloat("_Mode", 3); // Transparent
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }
    
    private void CalculateProbabilities()
    {
        // 스트라이크존 확률 = 전체 스트라이크 확률 / 9개
        float strikeZoneProbability = strikeProbability / strikeZones.Count;
        
        // 볼존 확률 = 전체 볼 확률 / 16개
        float ballZoneProbability = (100f - strikeProbability) / ballZones.Count;
        
        foreach (var zone in strikeZones)
        {
            zone.probability = strikeZoneProbability;
        }
        
        foreach (var zone in ballZones)
        {
            zone.probability = ballZoneProbability;
        }
        
        Debug.Log($"📊 확률 계산: 스트라이크존={strikeZoneProbability:F1}% 볼존={ballZoneProbability:F1}%");
    }
    
    private void UpdateVisualization()
    {
        if (ballZoneParent == null) return;
        
        foreach (Transform child in ballZoneParent)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = Application.isPlaying ? showZonesInPlay : showZonesInEditor;
            }
        }
    }
    
    // ==============================================
    // 🎯 공 결과 처리
    // ==============================================
    public void OnBallLanded(VRBaseball ball, bool isStrike)
    {
        string resultText = isStrike ? "스트라이크!" : "볼!";
        Debug.Log($"🎾 공 착지 결과: {resultText} 위치: {ball.transform.position}");
    }

    /// <summary>
    /// 특정 위치가 스트라이크존에 있는지 확인
    /// </summary>
    /// <param name="position">확인할 위치</param>
    /// <returns>스트라이크 여부</returns>
    public bool IsStrikePosition(Vector3 position)
    {
        // 스트라이크존들과의 거리를 체크하여 가장 가까운 존 확인
        foreach (var strikeZone in strikeZones)
        {
            if (strikeZone?.zoneObject != null)
            {
                Collider zoneCollider = strikeZone.zoneObject.GetComponent<Collider>();
                if (zoneCollider != null && zoneCollider.bounds.Contains(position))
                {
                    Debug.Log($"✅ 스트라이크! 위치 {position}이 {strikeZone.name}에 포함됨");
                    return true;
                }
            }
        }

        Debug.Log($"❌ 볼! 위치 {position}이 스트라이크존 밖에 있음");
        return false;
    }
    
    // ==============================================
    // 🎯 공 타겟팅
    // ==============================================
    public PitchZone GetRandomTargetZone()
    {
        float randomValue = Random.Range(0f, 100f);
        
        if (randomValue <= strikeProbability)
        {
            // 스트라이크존 선택
            return strikeZones[Random.Range(0, strikeZones.Count)];
        }
        else
        {
            // 볼존 선택
            return ballZones[Random.Range(0, ballZones.Count)];
        }
    }
    
    public Vector3 GetTargetPosition()
    {
        PitchZone targetZone = GetRandomTargetZone();
        
        // 존 내에서 랜덤 위치 (약간의 변화)
        Vector3 randomOffset = new Vector3(
            Random.Range(-zoneSize.x * 0.3f, zoneSize.x * 0.3f),
            Random.Range(-zoneSize.y * 0.3f, zoneSize.y * 0.3f),
            0
        );
        
        Vector3 targetPos = targetZone.position + randomOffset;
        
        Debug.Log($"🎯 타겟: {targetZone.name} ({(targetZone.isStrikeZone ? "스트라이크" : "볼")}) 위치: {targetPos}");
        
        return targetPos;
    }
    
    // ==============================================
    // 🎨 에디터 기즈모
    // ==============================================
    void OnDrawGizmos()
    {
        if (!showZonesInEditor) return;
        
        // 스트라이크존 (녹색)
        Gizmos.color = Color.green;
        foreach (var zone in strikeZones)
        {
            if (zone?.zoneObject != null)
            {
                Gizmos.DrawWireCube(zone.position, zoneSize);
            }
        }
        
        // 볼존 (빨간색)
        Gizmos.color = Color.red;
        foreach (var zone in ballZones)
        {
            if (zone?.zoneObject != null)
            {
                Gizmos.DrawWireCube(zone.position, zoneSize);
            }
        }
        
        // 전체 영역 경계 (파란색)
        if (strikeZoneBounds != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Vector3 fullAreaSize = strikeZoneBounds + Vector3.one * (zoneSpacing * 2);
            Gizmos.DrawWireCube(strikeZoneCenter, fullAreaSize);
        }
    }
    
    // ==============================================
    // 🔧 에디터 도구
    // ==============================================
    [ContextMenu("시스템 재초기화")]
    public void ReinitializeSystem()
    {
        InitializeSystem();
    }
    
    [ContextMenu("볼존 가시성 토글")]
    public void ToggleBallZoneVisibility()
    {
        showZonesInEditor = !showZonesInEditor;
        UpdateVisualization();
    }
    
    // ==============================================
    // 📊 정보 제공
    // ==============================================
    public int GetStrikeZoneCount() => strikeZones.Count;
    public int GetBallZoneCount() => ballZones.Count;
    public int GetTotalZoneCount() => allZones.Count;
    public List<PitchZone> GetStrikeZones() => new List<PitchZone>(strikeZones);
    public List<PitchZone> GetBallZones() => new List<PitchZone>(ballZones);
    public List<PitchZone> GetAllZones() => new List<PitchZone>(allZones);
}
