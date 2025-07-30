using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

public class TeamSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class TeamData
    {
        public string teamName;
        public Sprite teamLogo;
    }

    [Header("팀 데이터")]
    [SerializeField] private List<TeamData> teams = new List<TeamData>();

    [Header("컴퓨터 팀 선택 UI")]
    [SerializeField] private Image computerCurrentTeamLogo;      // 현재 선택된 팀
    [SerializeField] private Image computerPreviousTeamLogo;    // 이전 팀 
    [SerializeField] private Image computerNextTeamLogo;        // 다음 팀 
    [SerializeField] private TextMeshProUGUI computerTeamNameText;
    [SerializeField] private Button computerUpButton;
    [SerializeField] private Button computerDownButton;
    [SerializeField] private ScrollRect computerScrollRect;

    [Header("플레이어 팀 선택 UI")]
    [SerializeField] private Image playerCurrentTeamLogo;       // 현재 선택된 팀 
    [SerializeField] private Image playerPreviousTeamLogo;      // 이전 팀 
    [SerializeField] private Image playerNextTeamLogo;          // 다음 팀 
    [SerializeField] private TextMeshProUGUI playerTeamNameText;
    [SerializeField] private Button playerUpButton;
    [SerializeField] private Button playerDownButton;
    [SerializeField] private ScrollRect playerScrollRect;

    [Header("팀 교체")]
    [SerializeField] private Button teamSwapButton;

    [Header("게임 시작")]
    [SerializeField] private Button playBallButton;

    [Header("애니메이션 설정")]
    [SerializeField] private float scrollAnimationDuration = 0.3f;
    [SerializeField] private AnimationCurve scrollAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private int currentComputerTeamIndex = 0;
    private int currentPlayerTeamIndex = 1;
    private bool isAnimating = false;

    void Start()
    {
        InitializeTeams();
        SetupButtonEvents();
        SetupScrollEvents();
        UpdateTeamDisplays();
    }

    void InitializeTeams()
    {
        // 팀 데이터가 비어있으면 기본값으로 초기화
        if (teams.Count == 0)
        {
            teams = new List<TeamData>
            {
                new TeamData { teamName = "한화 이글스", teamLogo = null },
                new TeamData { teamName = "KIA 타이거즈", teamLogo = null },
                new TeamData { teamName = "LG 트윈스", teamLogo = null },
                new TeamData { teamName = "두산 베어스", teamLogo = null },
                new TeamData { teamName = "키움 히어로즈", teamLogo = null },
                new TeamData { teamName = "SSG 랜더스", teamLogo = null },
                new TeamData { teamName = "롯데 자이언츠", teamLogo = null },
                new TeamData { teamName = "KT 위즈", teamLogo = null },
                new TeamData { teamName = "삼성 라이온즈", teamLogo = null },
                new TeamData { teamName = "NC 다이노스", teamLogo = null }
            };
        }
    }

    void SetupButtonEvents()
    {
        // 컴퓨터 팀 선택 버튼
        if (computerUpButton != null)
            computerUpButton.onClick.AddListener(() => ChangeTeam(true, -1));

        if (computerDownButton != null)
            computerDownButton.onClick.AddListener(() => ChangeTeam(true, 1));

        // 플레이어 팀 선택 버튼
        if (playerUpButton != null)
            playerUpButton.onClick.AddListener(() => ChangeTeam(false, -1));

        if (playerDownButton != null)
            playerDownButton.onClick.AddListener(() => ChangeTeam(false, 1));

        // 팀 교체 버튼
        if (teamSwapButton != null)
            teamSwapButton.onClick.AddListener(SwapTeams);

        // 플레이 볼 버튼
        if (playBallButton != null)
            playBallButton.onClick.AddListener(OnPlayBallClicked);
    }

    void SetupScrollEvents()
    {
        // 컴퓨터 팀 스크롤
        if (computerScrollRect != null)
        {
            computerScrollRect.onValueChanged.AddListener((Vector2 value) => OnScrollValueChanged(true, value));
        }

        // 플레이어 팀 스크롤
        if (playerScrollRect != null)
        {
            playerScrollRect.onValueChanged.AddListener((Vector2 value) => OnScrollValueChanged(false, value));
        }
    }

    void ChangeTeam(bool isComputer, int direction)
    {
        if (isAnimating) return;

        if (isComputer)
        {
            currentComputerTeamIndex = (currentComputerTeamIndex + direction + teams.Count) % teams.Count;
        }
        else
        {
            currentPlayerTeamIndex = (currentPlayerTeamIndex + direction + teams.Count) % teams.Count;
        }

        StartCoroutine(AnimateTeamChange(isComputer));
    }

    void OnScrollValueChanged(bool isComputer, Vector2 scrollValue)
    {
        if (isAnimating) return;

        // 스크롤 값에 따라 팀 변경
        float normalizedScroll = isComputer ? computerScrollRect.verticalNormalizedPosition : playerScrollRect.verticalNormalizedPosition;

        int newIndex = Mathf.RoundToInt(normalizedScroll * (teams.Count - 1));

        if (isComputer && newIndex != currentComputerTeamIndex)
        {
            currentComputerTeamIndex = newIndex;
            UpdateTeamDisplays();
        }
        else if (!isComputer && newIndex != currentPlayerTeamIndex)
        {
            currentPlayerTeamIndex = newIndex;
            UpdateTeamDisplays();
        }
    }

    IEnumerator AnimateTeamChange(bool isComputer)
    {
        isAnimating = true;

        Image currentLogo = isComputer ? computerCurrentTeamLogo : playerCurrentTeamLogo;
        Image previousLogo = isComputer ? computerPreviousTeamLogo : playerPreviousTeamLogo;
        Image nextLogo = isComputer ? computerNextTeamLogo : playerNextTeamLogo;
        TextMeshProUGUI targetText = isComputer ? computerTeamNameText : playerTeamNameText;

        // 모든 로고들 페이드 아웃
        float startAlpha = 1f;
        for (float t = 0; t < scrollAnimationDuration / 2; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(startAlpha, 0f, scrollAnimationCurve.Evaluate(t / (scrollAnimationDuration / 2)));

            SetImageAlpha(currentLogo, alpha);
            SetImageAlpha(previousLogo, alpha * 0.5f);  // 이전/다음은 더 흐리게
            SetImageAlpha(nextLogo, alpha * 0.5f);

            if (targetText != null)
            {
                Color textColor = targetText.color;
                textColor.a = alpha;
                targetText.color = textColor;
            }

            yield return null;
        }

        // 팀 정보 업데이트
        UpdateTeamDisplays();

        // 모든 로고들 페이드 인
        for (float t = 0; t < scrollAnimationDuration / 2; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(0f, 1f, scrollAnimationCurve.Evaluate(t / (scrollAnimationDuration / 2)));

            SetImageAlpha(currentLogo, alpha);
            SetImageAlpha(previousLogo, alpha * 0.5f);  // 이전/다음은 더 흐리게
            SetImageAlpha(nextLogo, alpha * 0.5f);

            if (targetText != null)
            {
                Color textColor = targetText.color;
                textColor.a = alpha;
                targetText.color = textColor;
            }

            yield return null;
        }

        // 최종 알파값 설정
        SetImageAlpha(currentLogo, 1f);
        SetImageAlpha(previousLogo, 0.5f); // 이전/다음은 반투명
        SetImageAlpha(nextLogo, 0.5f);

        if (targetText != null)
        {
            Color textColor = targetText.color;
            textColor.a = 1f;
            targetText.color = textColor;
        }

        isAnimating = false;
    }

    void SetImageAlpha(Image image, float alpha)
    {
        if (image != null)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    void SwapTeams()
    {
        if (isAnimating) return;

        // 팀 인덱스 교체
        int temp = currentComputerTeamIndex;
        currentComputerTeamIndex = currentPlayerTeamIndex;
        currentPlayerTeamIndex = temp;

        // 양쪽 모두 애니메이션으로 업데이트
        StartCoroutine(AnimateTeamChange(true));
        StartCoroutine(AnimateTeamChange(false));
    }

    void UpdateTeamDisplays()
    {
        UpdateTeamLogos(true);   // 컴퓨터 팀
        UpdateTeamLogos(false);  // 플레이어 팀
    }

    void UpdateTeamLogos(bool isComputer)
    {
        int currentIndex = isComputer ? currentComputerTeamIndex : currentPlayerTeamIndex;
        Image currentLogo = isComputer ? computerCurrentTeamLogo : playerCurrentTeamLogo;
        Image previousLogo = isComputer ? computerPreviousTeamLogo : playerPreviousTeamLogo;
        Image nextLogo = isComputer ? computerNextTeamLogo : playerNextTeamLogo;
        TextMeshProUGUI teamNameText = isComputer ? computerTeamNameText : playerTeamNameText;

        // 현재 팀 로고 (크고 선명)
        SetTeamLogo(currentLogo, currentIndex, 1f, Vector3.one);

        // 이전 팀 로고 (작고 흐림)
        int previousIndex = (currentIndex - 1 + teams.Count) % teams.Count;
        SetTeamLogo(previousLogo, previousIndex, 0.5f, Vector3.one * 0.8f);

        // 다음 팀 로고 (작고 흐림)
        int nextIndex = (currentIndex + 1) % teams.Count;
        SetTeamLogo(nextLogo, nextIndex, 0.5f, Vector3.one * 0.8f);

        // 팀 이름 텍스트 업데이트
        if (teamNameText != null)
            teamNameText.text = teams[currentIndex].teamName;
    }

    void SetTeamLogo(Image logoImage, int teamIndex, float alpha, Vector3 scale)
    {
        if (logoImage != null && teamIndex < teams.Count)
        {
            // 로고 스프라이트 설정
            logoImage.sprite = teams[teamIndex].teamLogo;
            logoImage.enabled = teams[teamIndex].teamLogo != null;

            // 알파값 설정
            Color color = logoImage.color;
            color.a = alpha;
            logoImage.color = color;

            // 크기 설정
            logoImage.transform.localScale = scale;
        }
    }

    void OnPlayBallClicked()
    {
        Debug.Log("게임시작!");
        Debug.Log($"컴퓨터 팀: {teams[currentComputerTeamIndex].teamName}");
        Debug.Log($"플레이어 팀: {teams[currentPlayerTeamIndex].teamName}");

        // TODO: 게임플레이 씬으로 전환
        // SceneManager.LoadScene("Gameplay");
    }

    // 현재 선택된 팀 정보를 가져오는 메서드들 
    public string GetSelectedComputerTeam()
    {
        return teams[currentComputerTeamIndex].teamName;
    }

    public string GetSelectedPlayerTeam()
    {
        return teams[currentPlayerTeamIndex].teamName;
    }

    public int GetComputerTeamIndex()
    {
        return currentComputerTeamIndex;
    }

    public int GetPlayerTeamIndex()
    {
        return currentPlayerTeamIndex;
    }

    public TeamData GetComputerTeamData()
    {
        return teams[currentComputerTeamIndex];
    }

    public TeamData GetPlayerTeamData()
    {
        return teams[currentPlayerTeamIndex];
    }
}
