using UnityEngine;


//custom UI : PitchingDataEditor
[CreateAssetMenu(fileName = "NewPitcherSO", menuName = "Player/PitcherData")]
public class PitcherSO : ScriptableObject
{
    //todo 나중에 playerSO 만들어야 할듯
    [SerializeField] private string player_name;
    
    [SerializeField] private PitchingPlayerData [] pitchingData;

    public string PlayerName => player_name;

    //todo 구종 비율 / 구속 분포 / 멘탈 보정
    //todo 가중치 랜덤으로 존 인덱스 뽑는 함수
    //     ㄴ 합이 100이 아니어도 되게 "총합으로 나누는" 방식으로 짜두면 나중에 안 깨진다

    public int DataCount => pitchingData != null ? pitchingData.Length : 0;

    /// <summary>
    /// weight 가중치로 이번에 던질 구종 데이터를 하나 뽑는다.
    /// 존 인덱스와 구속은 뽑힌 데이터에게 다시 물어본다(PickZoneIndex / PickVelocity).
    /// </summary>
    /// <returns>뽑힌 구종 데이터. 등록된 게 없으면 null</returns>
    public PitchingPlayerData PickPitchingData()
    {
        if (pitchingData == null || pitchingData.Length == 0)
        {
            return null;
        }

        int index = WeightedRandom.Pick(
            pitchingData.Length,
            i => pitchingData[i] != null ? pitchingData[i].Weight : 0f);

        if (index < 0 || index >= pitchingData.Length)
        {
            return null;
        }
        return pitchingData[index];
    }
}
