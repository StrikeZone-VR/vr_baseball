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
        return ForceWeight * vSqHorizontal;
    }
}
