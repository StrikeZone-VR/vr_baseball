using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TeamSelectionManager : MonoBehaviour
{
    [Header("팀 데이터")]
    [SerializeField]
    private List<string> teamNames = new List<string>
    {
        "한화 이글스", "KIA 타이거즈", "LG 트윈스", "두산 베어스", "키움 히어로즈",
        "SSG 랜더스", "롯데 자이언츠", "KT 위즈", "삼성 라이온즈", "NC 다이노스"
    };

    [Header("컴퓨터 팀 선택 UI")]
    [SerializeField] private TextMeshProUGUI computerTeamNameText;
    [SerializeField] private Button computerUpButton;
    [SerializeField] private Button computerDownButton;

    [Header("플레이어 팀 선택 UI")]
    [SerializeField] private TextMeshProUGUI playerTeamNameText;
    [SerializeField] private Button playerUpButton;
    [SerializeField] private Button playerDownButton;

    [Header("게임 시작")]
    [SerializeField] private Button playBallButton;

    private int currentComputerTeamIndex = 0;
    private int currentPlayerTeamIndex = 1;

    void Start()
    {
        SetupButtonEvents();
        UpdateTeamDisplays();
    }

    void SetupButtonEvents()
    {
        // 컴퓨터 팀 선택 버튼
        if (computerUpButton != null)
            computerUpButton.onClick.AddListener(OnComputerTeamUp);

        if (computerDownButton != null)
            computerDownButton.onClick.AddListener(OnComputerTeamDown);

        // 플레이어 팀 선택 버튼
        if (playerUpButton != null)
            playerUpButton.onClick.AddListener(OnPlayerTeamUp);

        if (playerDownButton != null)
            playerDownButton.onClick.AddListener(OnPlayerTeamDown);

        // 플레이 볼 버튼
        if (playBallButton != null)
            playBallButton.onClick.AddListener(OnPlayBallClicked);
    }

    void OnComputerTeamUp()
    {
        currentComputerTeamIndex = (currentComputerTeamIndex - 1 + teamNames.Count) % teamNames.Count;
        UpdateTeamDisplays();
    }

    void OnComputerTeamDown()
    {
        currentComputerTeamIndex = (currentComputerTeamIndex + 1) % teamNames.Count;
        UpdateTeamDisplays();
    }

    void OnPlayerTeamUp()
    {
        currentPlayerTeamIndex = (currentPlayerTeamIndex - 1 + teamNames.Count) % teamNames.Count;
        UpdateTeamDisplays();
    }

    void OnPlayerTeamDown()
    {
        currentPlayerTeamIndex = (currentPlayerTeamIndex + 1) % teamNames.Count;
        UpdateTeamDisplays();
    }

    void UpdateTeamDisplays()
    {
        if (computerTeamNameText != null)
            computerTeamNameText.text = teamNames[currentComputerTeamIndex];

        if (playerTeamNameText != null)
            playerTeamNameText.text = teamNames[currentPlayerTeamIndex];
    }

    void OnPlayBallClicked()
    {
        Debug.Log("게임시작!");
        Debug.Log($"컴퓨터 팀: {teamNames[currentComputerTeamIndex]}");
        Debug.Log($"플레이어 팀: {teamNames[currentPlayerTeamIndex]}");

        // TODO: 게임플레이 씬으로 전환
        // SceneManager.LoadScene("Gameplay");
    }

    // 현재 선택된 팀 정보를 가져오는 메서드들 (다른 스크립트에서 사용 가능)
    public string GetSelectedComputerTeam()
    {
        return teamNames[currentComputerTeamIndex];
    }

    public string GetSelectedPlayerTeam()
    {
        return teamNames[currentPlayerTeamIndex];
    }

    public int GetComputerTeamIndex()
    {
        return currentComputerTeamIndex;
    }

    public int GetPlayerTeamIndex()
    {
        return currentPlayerTeamIndex;
    }
}
