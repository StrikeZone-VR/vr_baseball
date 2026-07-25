using UnityEngine;


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
}
