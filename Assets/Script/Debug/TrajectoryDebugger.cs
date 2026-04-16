using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TrajectoryDebugger : MonoBehaviour
{
    [SerializeField] private TrajectoryBaseBallData _trajectoryData;
    [SerializeField] private StrikeZone strikeZone;
    private void OnDrawGizmos()
    {
        DrawTrajectory();
    }

    void DrawTrajectory()
    {
        float dashLength = 0.1f; // 그려지는 짧은 선 길이
        float gapLength  = 0.1f; // 대시 사이 공백
        
        Gizmos.color = Color.yellow;

        float stepLen = dashLength + Mathf.Max(0f, gapLength);
        List<Vector3> list = _trajectoryData.GetPathPoints();
        for (int i = 0; i < list.Count - 1; i++)
        {
            DrawDashedSegment(list[i], list[i + 1], dashLength, stepLen);
        }

        if (_trajectoryData.GetHasPassedStrikeZone())
        {
            DebugDrawSp(_trajectoryData.GetStrikeZonePoint());
        }
        
        if (_trajectoryData.GetHasLand())
        {
            Gizmos.DrawWireSphere(_trajectoryData.GetLandingPoint(), 0.2f);
        }
    }
    
    void DrawDashedSegment(Vector3 a, Vector3 b, float dashLen, float stepLen)
    {
        Vector3 ab = b - a;
        float len = ab.magnitude;
        if (len < 0.00001f) return;

        Vector3 dir = ab / len;

        for (float t = 0f; t < len; t += stepLen)
        {
            float t0 = t;
            float t1 = Mathf.Min(t + dashLen, len);
            Gizmos.DrawLine(a + dir * t0, a + dir * t1);
        }
    }

    
    //스트라이크존에 닿을때
    private void DebugDrawSp(Vector3 point)
    {
        // 충돌 지점을 중심으로 평면에 평행한 아주 짧은 선 두 개를 그립니다.
        float crossSize = 0.05f; // 십자선 크기
        Color hitColor = Color.yellow; // 충돌 시 색상
        float duration = 100f; // 시각화 지속 시간
        
        
        // 평면의 로컬 좌표계 기준 방향 벡터 가져오기
        Vector3 right = strikeZone.transform.right;
        Vector3 up = strikeZone.transform.up;
        
        //Debug.Log("[Baseball] position : " + point);
        
        // 수평선 그리기
        Debug.DrawLine(point - right * crossSize, point + right * crossSize, hitColor, duration);
        // 수직선 그리기
        Debug.DrawLine(point - up * crossSize, point + up * crossSize, hitColor, duration);
    }
}
