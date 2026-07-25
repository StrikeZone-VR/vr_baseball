using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public enum PitchType
{
    FastBall,    // 직구
    Curve,       // 커브볼
    Slider,      // 슬라이더
    ForkBall     // 포크볼
}
[CreateAssetMenu(fileName = "PitchTypeSO", menuName = "Baseball/Pitch Type SO")]
public class PitchTypeSO : ScriptableObject
{
    [Header("구종 기본 정보")]
    public PitchType pitchType;
    public Vector3 ForceWeight;

    /// <summary>
    /// enum 기반으로 이 SO가 어떤 구종인지 판단.
    /// 예: if (currentPitchSO.Is(PitchType.Curve)) { ... }
    /// </summary>
    public bool Is(PitchType type) => pitchType == type;

    public Vector3 GetForce(Vector3 velocity)
    {
        Vector3 vXZ = new Vector3(velocity.x, 0, velocity.z);
        float vSqHorizontal = vXZ.sqrMagnitude; //제곱

        //방어: 정지 상태일 때 0벡터 normalize 회피
        if (vSqHorizontal < 0.0001f) return Vector3.zero;

        //ForceWeight를 비행 로컬 좌표계로 해석 → 월드 변환
        //x = 비행 방향 기준 오른쪽, y = 월드 위, z = 비행 방향 전진
        //어떤 방향으로 던지든 슬라이더는 항상 비행 기준 동일한 방향으로 휨
        Vector3 forward = vXZ.normalized;
        Vector3 right = new Vector3(forward.z, 0, -forward.x);

        Vector3 forceWorld = right * ForceWeight.x
                           + Vector3.up * ForceWeight.y
                           + forward * ForceWeight.z;
        return forceWorld * vSqHorizontal;
    }
}
