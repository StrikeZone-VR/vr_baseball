using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoryBaseBallData : MonoBehaviour
{
    private List<Vector3> pathPoints = new List<Vector3>(); // 날아가는 궤적 점들

    private bool hasPassedStrikeZone = false;
    //public Vector3 StrikeZonePoint; // 스트라이크 존 관통 좌표

    //public bool HasLanded = false; //필요할지도
    private Vector3 landingPoint; // 최종 바닥/벽 충돌 좌표

    public void Init()
    {
        pathPoints.Clear();
        hasPassedStrikeZone = false;
    }
    
    
    public List<Vector3> GetPathPoints()
    {
        return pathPoints;
    }
    public void AddPathPoint(Vector3 point)
    {
        pathPoints.Add(point);
    }

    public bool GetHasPassedStrikeZone()
    {
        return hasPassedStrikeZone;
    }
}
