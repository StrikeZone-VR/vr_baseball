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

    [Header("확률 설정")]
    [Range(0f, 100f)]
    public float middleCenterProbability = 10f;    // 한가운데 확률 (낮음)
    [Range(0f, 100f)]
    public float edgeAreaProbability = 22.5f;      // 변두리 확률 (높음) - 8개 영역이므로 각각 22.5%
    [Range(0f, 100f)]
    public float ballAreaProbability = 22.5f;      // 볼 영역 확률 (변두리와 동일)

    [Header("볼 영역 설정")]
    public Vector3 ballAreaSize = new Vector3(1.0f, 1.5f, 0.2f);  // 스트라이크존보다 큰 범위
    public Vector3 ballAreaOffset = Vector3.zero;

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

        // 볼 영역 생성
        CreateBallAreas();

        // 확률 정규화
        NormalizeProbabilities();

        Debug.Log($"스트라이크 영역 {strikeAreas.Count}개, 볼 영역 {ballAreas.Count}개 초기화 완료");
    }

    private void CollectStrikeZoneAreas()
    {
        for (int i = 0; i < strikeZoneParent.childCount; i++)
        {
            Transform child = strikeZoneParent.GetChild(i);

            // 중앙 영역인지 확인 (이름에 "Center"가 들어가는지)
            bool isMiddleCenter = child.name.Contains("Center") &&
                                 (child.name.Contains("Middle") || child.name.Contains("Mid"));

            float probability = isMiddleCenter ? middleCenterProbability : edgeAreaProbability;

            StrikeZoneArea area = new StrikeZoneArea(child.name, child, probability, true);
            strikeAreas.Add(area);
            allAreas.Add(area);
        }
    }

    private void CreateBallAreas()
    {
        // 스트라이크존 주변에 볼 영역들 생성
        Vector3 strikeZonePos = strikeZoneParent.position;

        // 8방향으로 볼 영역 생성
        Vector3[] ballDirections = {
            new Vector3(-1.5f, 1.5f, 0),   // 왼쪽 위
            new Vector3(0, 1.5f, 0),       // 위
            new Vector3(1.5f, 1.5f, 0),    // 오른쪽 위
            new Vector3(-1.5f, 0, 0),      // 왼쪽
            new Vector3(1.5f, 0, 0),       // 오른쪽
            new Vector3(-1.5f, -1.5f, 0),  // 왼쪽 아래
            new Vector3(0, -1.5f, 0),      // 아래
            new Vector3(1.5f, -1.5f, 0)    // 오른쪽 아래
        };

        for (int i = 0; i < ballDirections.Length; i++)
        {
            // 볼 영역 위치 계산
            Vector3 ballPosition = strikeZonePos + ballDirections[i] + ballAreaOffset;

            // 가상의 볼 영역 생성 (실제 GameObject는 만들지 않고 위치만 저장)
            GameObject ballAreaObj = new GameObject($"BallArea_{i}");
            ballAreaObj.transform.position = ballPosition;
            ballAreaObj.transform.parent = transform; // 이 매니저의 자식으로 설정

            StrikeZoneArea ballArea = new StrikeZoneArea($"Ball_Area_{i}", ballAreaObj.transform, ballAreaProbability, false);
            ballAreas.Add(ballArea);
            allAreas.Add(ballArea);
        }
    }

    private void NormalizeProbabilities()
    {
        // 전체 확률의 합을 100%로 정규화
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
        // 확률에 따라 랜덤 영역 선택
        float randomValue = Random.Range(0f, 100f);
        float cumulativeProbability = 0f;

        foreach (var area in allAreas)
        {
            cumulativeProbability += area.probability;
            if (randomValue <= cumulativeProbability)
            {
                // 선택된 영역 내에서 랜덤 위치 생성
                return GetRandomPositionInArea(area);
            }
        }

        // 혹시 모를 경우를 위한 기본값 (스트라이크존 센터)
        return strikeZoneParent.position;
    }

    private Vector3 GetRandomPositionInArea(StrikeZoneArea area)
    {
        // 영역 내에서 랜덤한 위치 생성
        Vector3 basePosition = area.areaTransform.position;

        if (area.isStrike)
        {
            // 스트라이크 영역: 해당 영역 콜라이더 범위 내
            Collider areaCollider = area.areaTransform.GetComponent<Collider>();
            if (areaCollider != null)
            {
                Bounds bounds = areaCollider.bounds;
                Vector3 randomOffset = new Vector3(
                    Random.Range(-bounds.size.x * 0.4f, bounds.size.x * 0.4f),
                    Random.Range(-bounds.size.y * 0.4f, bounds.size.y * 0.4f),
                    0
                );
                return basePosition + randomOffset;
            }
        }
        else
        {
            // 볼 영역: 좀 더 넓은 범위
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.3f, 0.3f),
                0
            );
            return basePosition + randomOffset;
        }

        return basePosition;
    }

    public bool IsStrikePosition(Vector3 position)
    {
        // 해당 위치가 스트라이크 영역인지 확인
        foreach (var area in strikeAreas)
        {
            Collider areaCollider = area.areaTransform.GetComponent<Collider>();
            if (areaCollider != null && areaCollider.bounds.Contains(position))
            {
                return true;
            }
        }
        return false;
    }

    // 디버그용 - 에디터에서 영역들을 시각화
    void OnDrawGizmos()
    {
        if (Application.isPlaying && allAreas != null)
        {
            foreach (var area in allAreas)
            {
                if (area.areaTransform != null)
                {
                    Gizmos.color = area.isStrike ?
                        (area.areaName.Contains("Center") ? Color.yellow : Color.green) :
                        Color.red;

                    Gizmos.DrawWireCube(area.areaTransform.position, Vector3.one * 0.2f);

#if UNITY_EDITOR
                    // 확률 표시 (에디터에서만)
                    UnityEditor.Handles.Label(area.areaTransform.position + Vector3.up * 0.15f,
                        $"{area.areaName}\n{area.probability:F1}%");
#endif
                }
            }
        }
    }
}
