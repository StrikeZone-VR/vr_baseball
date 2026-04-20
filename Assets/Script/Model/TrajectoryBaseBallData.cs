using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTrajectoryBaseBallData", menuName = "Model/Trajectory BaseBall Data")]
public class TrajectoryBaseBallData : ScriptableObject
{
    [SerializeField] private List<Vector3> pathPoints = new List<Vector3>(); // 날아가는 궤적 점들

    //배트 예상 스윙을 위한 변수들
    [SerializeField] private bool hasPassedStrikeZone = false;
    [SerializeField] private Vector3 StrikeZonePoint; // 스트라이크 존 관통 좌표

    [SerializeField] private bool hasLanded = false; //필요할지도
    [SerializeField] private Vector3 landingPoint; // 최종 바닥/벽 충돌 좌표

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

    public void SetStrikeZonePoint(Vector3 point)
    {
        this.StrikeZonePoint = point;
    }
    public Vector3 GetStrikeZonePoint()
    {
        return StrikeZonePoint;
    }
    
    public void SetLandingPoint(Vector3 point)
    {
        landingPoint = point;
    }
    public Vector3 GetLandingPoint()
    {
        return landingPoint;
    }
    
    public bool GetHasPassedStrikeZone()
    {
        return hasPassedStrikeZone;
    }

    public void SetHasPassedStrikeZone(bool hasPassedStrikeZone)
    {
        this.hasPassedStrikeZone = hasPassedStrikeZone;
    }
    public bool GetHasLand()
    {
        return hasLanded;
    }

    public void SetHasLand(bool hasLanded)
    {
        this.hasLanded = hasLanded;
    }

}
