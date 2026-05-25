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
        //todo 각자의 구종 가중치값을 곱 할 생각이다.
        //Magnus 모델: 방향은 ForceWeight가 고정, 크기는 수평 속도²로 스케일
        //CalculateVelocity의 보정식과 동일한 수평속도² 기반 → 두 식이 일치해서 타겟 명중
        Vector3 vXZ = new Vector3(velocity.x, 0, velocity.z);
        float vSqHorizontal = vXZ.sqrMagnitude;
        return ForceWeight * vSqHorizontal;
    }
}
