using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using KBO.Data;

/// <summary>
/// KBO 정보를 표시하는 UI 매니저
/// 팀 순위, 타자, 투수 정보를 API에서 가져와서 현대적인 테이블 형태로 표시
/// </summary>
public class KBOInfoManager : MonoBehaviour
{
    [Header("KBO 정보 UI 컨테이너")]
    [SerializeField] private GameObject kboInfoPanel;
    [SerializeField] private Button kboToggleButton;

    [Header("탭 버튼들")]
    [SerializeField] private Button teamRankingTabButton;
    [SerializeField] private Button hittersTabButton;
    [SerializeField] private Button pitchersTabButton;

    [Header("데이터 표시 영역")]
    [SerializeField] private GameObject contentArea;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("테이블 헤더 (동적 생성용)")]
    [SerializeField] private Transform headerContainer;
    [SerializeField] private GameObject headerCellPrefab;

    [Header("테이블 행 (동적 생성용)")]
    [SerializeField] private Transform rowContainer;
    [SerializeField] private GameObject rowPrefab;

    [Header("새로고침")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private float refreshCooldown = 5f;

    [Header("한글 폰트 설정")]
    [SerializeField] private TMP_FontAsset koreanFontAsset;

    // API 설정
    private const string BASE_URL = "http://15.164.96.52:8080/api";
    private const string HITTERS_ENDPOINT = "/hitters/top";
    private const string PITCHERS_ENDPOINT = "/pitchers/top";
    private const string TEAM_RANKINGS_ENDPOINT = "/team-rankings";

    // 현재 상태
    private KBODataType currentDataType = KBODataType.TeamRanking;
    private bool isLoading = false;
    private bool canRefresh = true;

    // 데이터 캐시
    private List<TeamRankingData> teamRankings = new List<TeamRankingData>();
    private List<HitterData> hitters = new List<HitterData>();
    private List<PitcherData> pitchers = new List<PitcherData>();

    // 색상 설정
    [Header("UI 색상 설정")]
    [SerializeField] private Color selectedTabColor = new Color(0.2f, 0.6f, 1f, 1f);
    [SerializeField] private Color defaultTabColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color headerColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color rowColor1 = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color rowColor2 = new Color(0.95f, 0.95f, 0.95f, 1f);

    void Start()
    {
        InitializeUI();
        SetupEventHandlers();

        // 초기 데이터 로드 (팀 순위)
        LoadData(KBODataType.TeamRanking);
    }

    void InitializeUI()
    {
        // 초기 상태 설정
        if (kboInfoPanel != null)
            kboInfoPanel.SetActive(true);  // 항상 활성화 상태로 표시

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        if (errorPanel != null)
            errorPanel.SetActive(false);

        // 한글 폰트 적용
        ApplyKoreanFont();

        // 탭 버튼 초기 색상 설정
        UpdateTabButtons();
    }

    void SetupEventHandlers()
    {
        // KBO 정보 패널 토글
        if (kboToggleButton != null)
            kboToggleButton.onClick.AddListener(ToggleKBOPanel);

        // 탭 버튼들
        if (teamRankingTabButton != null)
            teamRankingTabButton.onClick.AddListener(() => SwitchTab(KBODataType.TeamRanking));

        if (hittersTabButton != null)
            hittersTabButton.onClick.AddListener(() => SwitchTab(KBODataType.Hitters));

        if (pitchersTabButton != null)
            pitchersTabButton.onClick.AddListener(() => SwitchTab(KBODataType.Pitchers));

        // 새로고침 버튼
        if (refreshButton != null)
            refreshButton.onClick.AddListener(() => RefreshCurrentData());
    }

    public void ToggleKBOPanel()
    {
        if (kboInfoPanel != null)
        {
            bool isActive = kboInfoPanel.activeInHierarchy;
            kboInfoPanel.SetActive(!isActive);

            if (!isActive && ShouldRefreshData())
            {
                LoadData(currentDataType);
            }
        }
    }

    public void SwitchTab(KBODataType dataType)
    {
        if (isLoading) return;

        currentDataType = dataType;
        UpdateTabButtons();
        LoadData(dataType);
    }

    void UpdateTabButtons()
    {
        // 팀 순위 탭
        if (teamRankingTabButton != null)
        {
            var image = teamRankingTabButton.GetComponent<Image>();
            if (image != null)
                image.color = currentDataType == KBODataType.TeamRanking ? selectedTabColor : defaultTabColor;
        }

        // 타자 탭
        if (hittersTabButton != null)
        {
            var image = hittersTabButton.GetComponent<Image>();
            if (image != null)
                image.color = currentDataType == KBODataType.Hitters ? selectedTabColor : defaultTabColor;
        }

        // 투수 탭
        if (pitchersTabButton != null)
        {
            var image = pitchersTabButton.GetComponent<Image>();
            if (image != null)
                image.color = currentDataType == KBODataType.Pitchers ? selectedTabColor : defaultTabColor;
        }
    }

    void LoadData(KBODataType dataType)
    {
        if (isLoading) return;

        string endpoint = "";
        string title = "";

        switch (dataType)
        {
            case KBODataType.TeamRanking:
                endpoint = TEAM_RANKINGS_ENDPOINT;
                title = "KBO 팀 순위";
                break;
            case KBODataType.Hitters:
                endpoint = HITTERS_ENDPOINT;
                title = "KBO 상위 타자";
                break;
            case KBODataType.Pitchers:
                endpoint = PITCHERS_ENDPOINT;
                title = "KBO 상위 투수";
                break;
        }

        if (titleText != null)
            titleText.text = title;

        StartCoroutine(FetchDataCoroutine(endpoint, dataType));
    }

    IEnumerator FetchDataCoroutine(string endpoint, KBODataType dataType)
    {
        isLoading = true;
        ShowLoading(true);
        HideError();

        string url = BASE_URL + endpoint;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            isLoading = false;
            ShowLoading(false);

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;

                    // API 응답 데이터 콘솔 출력
                    Debug.Log($"==================== API 응답 데이터 [{dataType}] ====================");
                    Debug.Log($"URL: {url}");
                    Debug.Log($"응답 길이: {jsonResponse.Length} characters");
                    Debug.Log($"JSON 데이터: {jsonResponse}");
                    Debug.Log("================================================================");

                    ProcessAPIResponse(jsonResponse, dataType);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"데이터 처리 중 오류 발생: {e.Message}");
                    Debug.LogError($"Stack Trace: {e.StackTrace}");
                    ShowError($"데이터 처리 중 오류가 발생했습니다: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"API 요청 실패 - URL: {url}, Error: {request.error}, Response Code: {request.responseCode}");
                ShowError($"서버 연결 실패: {request.error}");
            }
        }
    }

    void ProcessAPIResponse(string jsonResponse, KBODataType dataType)
    {
        Debug.Log($"=============== 데이터 처리 시작: {dataType} ===============");

        switch (dataType)
        {
            case KBODataType.TeamRanking:
                teamRankings = JsonHelper.FromJson<TeamRankingData>(jsonResponse);
                Debug.Log($"팀 순위 데이터 파싱 완료: {teamRankings?.Count ?? 0}개 팀");
                if (teamRankings != null && teamRankings.Count > 0)
                {
                    Debug.Log($"첫 번째 팀 예시: {teamRankings[0].rank}위 - {KBOTeamHelper.GetTeamFullName(teamRankings[0].id.team)} (승률: {teamRankings[0].pct})");
                }
                DisplayTeamRankings();
                break;

            case KBODataType.Hitters:
                hitters = JsonHelper.FromJson<HitterData>(jsonResponse);
                Debug.Log($"타자 데이터 파싱 완료: {hitters?.Count ?? 0}명");
                if (hitters != null && hitters.Count > 0)
                {
                    Debug.Log($"첫 번째 타자 예시: {hitters[0].id.playerName} ({hitters[0].id.team}) - 타율: {hitters[0].avg}");
                }
                DisplayHitters();
                break;

            case KBODataType.Pitchers:
                pitchers = JsonHelper.FromJson<PitcherData>(jsonResponse);
                Debug.Log($"투수 데이터 파싱 완료: {pitchers?.Count ?? 0}명");
                if (pitchers != null && pitchers.Count > 0)
                {
                    Debug.Log($"첫 번째 투수 예시: {pitchers[0].id.playerName} ({pitchers[0].id.team}) - 평균자책점: {pitchers[0].era}");
                }
                DisplayPitchers();
                break;
        }

        Debug.Log($"=============== 데이터 처리 완료: {dataType} ===============");
    }

    void DisplayTeamRankings()
    {
        Debug.Log("========== 팀 순위 UI 표시 시작 ==========");
        ClearTable();

        // 헤더 생성
        string[] headers = { "순위", "팀", "경기", "승", "패", "무", "승률", "게임차", "연속", "최근10경기" };
        Debug.Log($"헤더 생성: {string.Join(", ", headers)}");
        CreateHeaders(headers);

        // 데이터 행 생성
        Debug.Log($"팀 순위 데이터 행 생성 시작: {teamRankings.Count}개 팀");
        for (int i = 0; i < teamRankings.Count; i++)
        {
            var team = teamRankings[i];
            string teamFullName = KBOTeamHelper.GetTeamFullName(team.id.team);
            string[] rowData = {
                team.rank.ToString(),
                teamFullName,
                team.games.ToString(),
                team.wins.ToString(),
                team.losses.ToString(),
                team.draws.ToString(),
                team.pct.ToString("F3"),
                team.gb.ToString("F1"),
                team.streak,
                team.last10
            };

            Debug.Log($"행 {i + 1}: {team.rank}위 {teamFullName} ({team.wins}승 {team.losses}패, 승률 {team.pct:F3})");

            // 순위에 따른 배경 색상 적용
            Color backgroundColor = i % 2 == 0 ? rowColor1 : rowColor2;

            // 상위 5팀은 특별한 색상으로 구분
            if (team.rank <= 5)
            {
                Color rankColor = KBOTeamHelper.GetRankColor(team.rank);
                backgroundColor = Color.Lerp(backgroundColor, rankColor, 0.3f);
            }

            CreateDataRow(rowData, backgroundColor, team.id.team, team.rank);
        }

        Debug.Log("========== 팀 순위 UI 표시 완료 ==========");
    }

    void DisplayHitters()
    {
        Debug.Log("========== 타자 순위 UI 표시 시작 ==========");
        ClearTable();

        // 헤더 생성
        string[] headers = { "순위", "선수명", "팀", "타율", "경기", "타석", "안타", "홈런", "타점", "득점" };
        Debug.Log($"타자 헤더 생성: {string.Join(", ", headers)}");
        CreateHeaders(headers);

        // 데이터 행 생성
        Debug.Log($"타자 데이터 행 생성 시작: {hitters.Count}명");
        for (int i = 0; i < hitters.Count; i++)
        {
            var hitter = hitters[i];
            string teamFullName = KBOTeamHelper.GetTeamFullName(hitter.id.team);
            string[] rowData = {
                (i + 1).ToString(),
                hitter.id.playerName,
                teamFullName,
                hitter.avg.ToString("F3"),
                hitter.g.ToString(),
                hitter.pa.ToString(),
                hitter.h.ToString(),
                hitter.hr.ToString(),
                hitter.rbi.ToString(),
                hitter.r.ToString()
            };

            Debug.Log($"타자 행 {i + 1}: {hitter.id.playerName} ({teamFullName}) - 타율 {hitter.avg:F3}, 홈런 {hitter.hr}, 타점 {hitter.rbi}");

            CreateDataRowWithTeam(rowData, i % 2 == 0 ? rowColor1 : rowColor2, hitter.id.team, i + 1);
        }

        Debug.Log("========== 타자 순위 UI 표시 완료 ==========");
    }

    void DisplayPitchers()
    {
        Debug.Log("========== 투수 순위 UI 표시 시작 ==========");
        ClearTable();

        // 헤더 생성
        string[] headers = { "순위", "선수명", "팀", "ERA", "이닝", "승", "패", "세이브", "삼진", "볼넷" };
        Debug.Log($"투수 헤더 생성: {string.Join(", ", headers)}");
        CreateHeaders(headers);

        // 데이터 행 생성
        Debug.Log($"투수 데이터 행 생성 시작: {pitchers.Count}명");
        for (int i = 0; i < pitchers.Count; i++)
        {
            var pitcher = pitchers[i];
            string teamFullName = KBOTeamHelper.GetTeamFullName(pitcher.id.team);
            string[] rowData = {
                (i + 1).ToString(),
                pitcher.id.playerName,
                teamFullName,
                pitcher.era.ToString("F2"),
                pitcher.ip.ToString("F1"),
                pitcher.w.ToString(),
                pitcher.l.ToString(),
                pitcher.sv.ToString(),
                pitcher.so.ToString(),
                pitcher.bb.ToString()
            };

            Debug.Log($"투수 행 {i + 1}: {pitcher.id.playerName} ({teamFullName}) - ERA {pitcher.era:F2}, 승 {pitcher.w}, 세이브 {pitcher.sv}");

            CreateDataRowWithTeam(rowData, i % 2 == 0 ? rowColor1 : rowColor2, pitcher.id.team, i + 1);
        }

        Debug.Log("========== 투수 순위 UI 표시 완료 ==========");
    }

    void CreateHeaders(string[] headers)
    {
        if (headerContainer == null || headerCellPrefab == null) return;

        foreach (string header in headers)
        {
            GameObject headerCell = Instantiate(headerCellPrefab, headerContainer);

            // 생성된 UI 요소 강제 활성화
            headerCell.SetActive(true);

            // 최소 크기 설정
            var rectTransform = headerCell.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(100, 30); // 최소 크기 설정
            }

            var text = headerCell.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = header;
                text.fontStyle = FontStyles.Bold;
                text.fontSize = 16f; // 헤더는 조금 더 크게

                // Canvas 안에 있는 TextMeshPro의 RectTransform 올바르게 설정
                var textRect = text.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    // 부모 Canvas의 크기에 맞게 꽉 채우도록 설정
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    textRect.anchoredPosition = Vector2.zero;

                    // 텍스트 정렬 설정
                    text.alignment = TextAlignmentOptions.Center;
                }

                // Canvas 컴포넌트가 있다면 설정 확인
                var canvas = text.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.gameObject != text.gameObject)
                {
                    // 부모 Canvas 설정 확인
                    canvas.overrideSorting = false;
                    canvas.sortingOrder = 0;
                }

                // 헤더에도 한글 폰트 적용
                ApplyKoreanFontToText(text);
            }

            var image = headerCell.GetComponent<Image>();
            if (image != null)
                image.color = headerColor;
        }

        // Layout 강제 갱신
        StartCoroutine(RefreshLayoutAfterFrame());
    }

    void CreateDataRow(string[] data, Color backgroundColor, string teamName = "", int rank = 0)
    {
        if (rowContainer == null || rowPrefab == null)
        {
            Debug.LogError("CreateDataRow: rowContainer 또는 rowPrefab이 null입니다!");
            return;
        }

        Debug.Log($"데이터 행 생성: {string.Join(" | ", data)}");
        GameObject row = Instantiate(rowPrefab, rowContainer);

        // 생성된 UI 요소 강제 활성화
        row.SetActive(true);

        // 디버깅: 생성된 오브젝트의 구조 확인
        Debug.Log($"생성된 행 오브젝트: {row.name}, 활성 상태: {row.activeInHierarchy}");

        // 최소 크기 설정
        var rectTransform = row.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(100, 30); // 최소 크기 설정
        }

        var rowImage = row.GetComponent<Image>();
        if (rowImage != null)
            rowImage.color = backgroundColor;

        var textComponents = row.GetComponentsInChildren<TextMeshProUGUI>(true);

        Debug.Log($"찾은 텍스트 컴포넌트 수: {textComponents.Length}");

        for (int i = 0; i < data.Length && i < textComponents.Length; i++)
        {
            textComponents[i].text = data[i];

            // 텍스트 크기 줄이기
            textComponents[i].fontSize = 14f;

            Debug.Log($"텍스트 {i} 설정: '{data[i]}', 오브젝트: {textComponents[i].gameObject.name}, 활성: {textComponents[i].gameObject.activeInHierarchy}");

            // Canvas 안에 있는 TextMeshPro의 RectTransform 올바르게 설정
            var textRect = textComponents[i].GetComponent<RectTransform>();
            if (textRect != null)
            {
                // 부모 Canvas의 크기에 맞게 꽉 채우도록 설정
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;

                // 텍스트 정렬 설정
                textComponents[i].alignment = TextAlignmentOptions.Center;
            }

            // Canvas 컴포넌트가 있다면 설정 확인
            var canvas = textComponents[i].GetComponentInParent<Canvas>();
            if (canvas != null && canvas.gameObject != textComponents[i].gameObject)
            {
                // 부모 Canvas 설정 확인
                canvas.overrideSorting = false;
                canvas.sortingOrder = 0;
            }

            // 한글 폰트 적용
            ApplyKoreanFontToText(textComponents[i]);

            // 팀 이름 컬럼에 팀 색상 적용
            if (i == 1 && !string.IsNullOrEmpty(teamName))
            {
                textComponents[i].color = KBOTeamHelper.GetTeamColor(teamName);
                textComponents[i].fontStyle = FontStyles.Bold;
            }

            // 순위 컬럼에 특별 색상 적용
            if (i == 0 && rank > 0 && rank <= 3)
            {
                textComponents[i].color = KBOTeamHelper.GetRankColor(rank);
                textComponents[i].fontStyle = FontStyles.Bold;
            }
        }

        // Layout 강제 갱신
        StartCoroutine(RefreshLayoutAfterFrame());
    }

    // 기존 메서드 오버로드
    void CreateDataRow(string[] data, Color backgroundColor)
    {
        CreateDataRow(data, backgroundColor, "", 0);
    }

    void CreateDataRowWithTeam(string[] data, Color backgroundColor, string teamName, int rank)
    {
        if (rowContainer == null || rowPrefab == null) return;

        GameObject row = Instantiate(rowPrefab, rowContainer);
        var rowImage = row.GetComponent<Image>();
        if (rowImage != null)
            rowImage.color = backgroundColor;

        var textComponents = row.GetComponentsInChildren<TextMeshProUGUI>();

        for (int i = 0; i < data.Length && i < textComponents.Length; i++)
        {
            textComponents[i].text = data[i];

            // 팀 이름 컬럼 (3번째 컬럼)에 팀 색상 적용
            if (i == 2 && !string.IsNullOrEmpty(teamName))
            {
                textComponents[i].color = KBOTeamHelper.GetTeamColor(teamName);
                textComponents[i].fontStyle = FontStyles.Bold;
            }

            // 순위 컬럼에 특별 색상 적용
            if (i == 0 && rank > 0 && rank <= 3)
            {
                textComponents[i].color = KBOTeamHelper.GetRankColor(rank);
                textComponents[i].fontStyle = FontStyles.Bold;
            }
        }
    }

    void ClearTable()
    {
        // 헤더 클리어
        if (headerContainer != null)
        {
            foreach (Transform child in headerContainer)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
            }
        }

        // 행 클리어
        if (rowContainer != null)
        {
            foreach (Transform child in rowContainer)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
            }
        }
    }

    void ShowLoading(bool show)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(show);
    }

    void ShowError(string message)
    {
        if (errorPanel != null)
            errorPanel.SetActive(true);

        if (errorText != null)
            errorText.text = message;

        Debug.LogError($"KBO Info Error: {message}");
    }

    void HideError()
    {
        if (errorPanel != null)
            errorPanel.SetActive(false);
    }

    public void RefreshCurrentData()
    {
        if (!canRefresh || isLoading) return;

        LoadData(currentDataType);
        StartCoroutine(RefreshCooldownCoroutine());
    }

    IEnumerator RefreshCooldownCoroutine()
    {
        canRefresh = false;
        yield return new WaitForSeconds(refreshCooldown);
        canRefresh = true;
    }

    /// <summary>
    /// Layout을 강제로 갱신하여 UI가 올바르게 표시되도록 합니다
    /// </summary>
    IEnumerator RefreshLayoutAfterFrame()
    {
        yield return new WaitForEndOfFrame();

        // ScrollView Content의 Layout Group 강제 갱신
        if (headerContainer != null)
        {
            var layoutGroup = headerContainer.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(headerContainer.GetComponent<RectTransform>());
            }
        }

        if (rowContainer != null)
        {
            var layoutGroup = rowContainer.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowContainer.GetComponent<RectTransform>());
            }

            // ScrollView의 Content 영역도 갱신
            Transform content = rowContainer.parent;
            if (content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
            }
        }

        Debug.Log("Layout 강제 갱신 완료");
    }

    bool ShouldRefreshData()
    {
        // 데이터가 없거나 오래된 경우 새로고침 필요
        switch (currentDataType)
        {
            case KBODataType.TeamRanking:
                return teamRankings.Count == 0;
            case KBODataType.Hitters:
                return hitters.Count == 0;
            case KBODataType.Pitchers:
                return pitchers.Count == 0;
            default:
                return true;
        }
    }

    /// <summary>
    /// 모든 TextMeshPro 컴포넌트에 한글 폰트를 적용합니다
    /// </summary>
    void ApplyKoreanFont()
    {
        // 한글 폰트 에셋이 할당되지 않은 경우 경고 메시지
        if (koreanFontAsset == null)
        {
            Debug.LogError("==================== 중요한 알림 ====================");
            Debug.LogError("한글 폰트 에셋이 할당되지 않았습니다!");
            Debug.LogError("다음 단계를 따라 한글 폰트를 설정해주세요:");
            Debug.LogError("1. Window → TextMeshPro → Font Asset Creator 열기");
            Debug.LogError("2. Source Font File: Arial 또는 시스템 한글 폰트 선택");
            Debug.LogError("3. Character Set: Custom Characters 선택");
            Debug.LogError("4. Custom Character List에 입력: 0x0020-0x007F,0xAC00-0xD7AF,0x3131-0x318E");
            Debug.LogError("5. Generate Font Atlas 클릭");
            Debug.LogError("6. Save로 폰트 에셋 저장");
            Debug.LogError("7. KBOInfoManager의 Korean Font Asset 필드에 할당");
            Debug.LogError("================================================");

            // 임시방편: 기본 폰트라도 할당해보기
            TMP_FontAsset defaultFont = Resources.GetBuiltinResource<TMP_FontAsset>("LiberationSans SDF.asset");
            if (defaultFont != null)
            {
                koreanFontAsset = defaultFont;
                Debug.LogWarning("임시로 기본 폰트를 사용합니다. 한글은 □로 표시될 수 있습니다.");
            }
            else
            {
                return;
            }
        }

        // 모든 하위 TextMeshPro 컴포넌트 찾기
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI textComponent in allTexts)
        {
            if (textComponent != null)
            {
                textComponent.font = koreanFontAsset;
            }
        }

        Debug.Log($"폰트가 {allTexts.Length}개의 텍스트 컴포넌트에 적용되었습니다.");
    }

    /// <summary>
    /// 특정 TextMeshPro 컴포넌트에 한글 폰트를 적용합니다
    /// </summary>
    void ApplyKoreanFontToText(TextMeshProUGUI textComponent)
    {
        if (koreanFontAsset != null && textComponent != null)
        {
            textComponent.font = koreanFontAsset;
        }
    }
}

/// <summary>
/// JSON 배열을 List로 변환하는 유틸리티 클래스
/// </summary>
public static class JsonHelper
{
    public static List<T> FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>("{\"Items\":" + json + "}");
        return wrapper.Items;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public List<T> Items;
    }
}