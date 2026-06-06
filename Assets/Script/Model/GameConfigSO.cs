using UnityEngine;

/// <summary>
/// 게임 설정값을 씬 간에 전달하기 위한 SO
/// GameReady에서 설정 → Gameplay에서 읽음
/// </summary>
[CreateAssetMenu(fileName = "GameConfigSO", menuName = "Model/GameConfig")]
public class GameConfigSO : ScriptableObject
{
    //config가 없을 때(직접 Gameplay 씬 단독 실행 등)의 내부 이닝 카운터 max (초/말 0~17 → 18)
    public const int DEFAULT_MAX_INNING_COUNT = 18;

    [Header("이닝 설정")]
    [Tooltip("플레이 가능한 총 이닝 수 (1, 3, 6, 9)")]
    [SerializeField] private int selectedInnings = 9;

    public int SelectedInnings
    {
        get => selectedInnings;
        set => selectedInnings = value;
    }

    /// <summary>
    /// 내부 이닝 카운터 최댓값. (초/말 각각 1단위라 × 2)
    /// GamePlayManager의 Inning setter에서 종료 판정에 사용.
    /// </summary>
    public int MaxInningCount => selectedInnings * 2;
}
