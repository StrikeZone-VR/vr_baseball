/// <summary>
/// ⚾ 구종 정의 및 데이터 클래스 - 직구, 커브, 슬라이더, 포크볼 설정
/// </summary>

using UnityEngine;

[System.Serializable]
public enum PitchType
{
    FastBall,    // 직구
    Curve,       // 커브볼
    Slider,      // 슬라이더
    ForkBall     // 포크볼
}

[System.Serializable]
public class PitchData
{
    [Header("구종 기본 정보")]
    public PitchType pitchType;
    public string pitchName;
    public Color pitchColor = Color.white;
    
    [Header("물리 속성")]
    [Range(0.5f, 2.0f)]
    public float speedMultiplier = 1.0f;
    
    [Header("궤도 변화")]
    public Vector3 curveDirection = Vector3.zero;  // 커브 방향
    [Range(0f, 20f)]
    public float curveStrength = 0f;              // 커브 강도
    [Range(0f, 1f)]
    public float curveDelay = 0.3f;               // 커브 시작 지연
    
    [Header("중력 효과")]
    [Range(0.5f, 3.0f)]
    public float gravityMultiplier = 1.0f;
    
    [Header("회전 효과")]
    public Vector3 spinDirection = Vector3.zero;
    [Range(0f, 10f)]
    public float spinStrength = 0f;
    
    [Header("UI 정보")]
    public Sprite pitchIcon;
    
    public static PitchData GetDefaultPitchData(PitchType type)
    {
        PitchData data = new PitchData();
        data.pitchType = type;
        
        switch (type)
        {
            case PitchType.FastBall:
                data.pitchName = "직구";
                data.pitchColor = Color.red;
                data.speedMultiplier = 1.8f;
                data.curveDirection = Vector3.zero;
                data.curveStrength = 0f;
                data.gravityMultiplier = 0.8f;
                data.spinDirection = Vector3.forward;
                data.spinStrength = 1f;
                break;
                
            case PitchType.Curve:
                data.pitchName = "커브볼";
                data.pitchColor = Color.blue;
                data.speedMultiplier = 1.2f;
                data.curveDirection = new Vector3(-2f, -3f, 0f);
                data.curveStrength = 8f;
                data.curveDelay = 0.4f;
                data.gravityMultiplier = 1.5f;
                data.spinDirection = new Vector3(-1f, 0f, 1f);
                data.spinStrength = 3f;
                break;
                
            case PitchType.Slider:
                data.pitchName = "슬라이더";
                data.pitchColor = Color.yellow;
                data.speedMultiplier = 1.5f;
                data.curveDirection = new Vector3(-1.5f, -0.5f, 0f);
                data.curveStrength = 5f;
                data.curveDelay = 0.5f;
                data.gravityMultiplier = 1.0f;
                data.spinDirection = new Vector3(-0.7f, 0f, 0.3f);
                data.spinStrength = 2f;
                break;
                
            case PitchType.ForkBall:
                data.pitchName = "포크볼";
                data.pitchColor = Color.green;
                data.speedMultiplier = 1.0f;
                data.curveDirection = new Vector3(0f, -4f, 0f);
                data.curveStrength = 12f;
                data.curveDelay = 0.6f;
                data.gravityMultiplier = 2.5f;
                data.spinDirection = Vector3.zero;
                data.spinStrength = 0.2f;
                break;
        }
        
        return data;
    }
}
