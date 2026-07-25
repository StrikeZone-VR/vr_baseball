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

    [Header("테이블 컨테이너")]
    [SerializeField] private Transform tableContainer;
    [SerializeField] private ScrollRect scrollRect;

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

        // 헤더 정의
        string[] headers = { "순위", "팀", "경기", "승", "패", "무", "승률", "게임차", "연속", "최근10경기" };

        // 데이터 준비
        List<string[]> tableData = new List<string[]>();

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

            tableData.Add(rowData);
            Debug.Log($"팀 데이터 추가: {team.rank}위 {teamFullName}");
        }

        // 새로운 테이블 시스템으로 생성
        CreateTable(headers, tableData);

        Debug.Log("========== 팀 순위 UI 표시 완료 ==========");
    }

    void DisplayHitters()
    {
        Debug.Log("========== 타자 순위 UI 표시 시작 ==========");

        // 헤더 정의
        string[] headers = { "순위", "선수명", "팀", "타율", "경기", "타석", "안타", "홈런", "타점", "득점" };

        // 데이터 준비
        List<string[]> tableData = new List<string[]>();

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

            tableData.Add(rowData);
            Debug.Log($"타자 데이터 추가: {i + 1}위 {hitter.id.playerName} ({teamFullName})");
        }

        // 새로운 테이블 시스템으로 생성
        CreateTable(headers, tableData);

        Debug.Log("========== 타자 순위 UI 표시 완료 ==========");
    }

    void DisplayPitchers()
    {
        Debug.Log("========== 투수 순위 UI 표시 시작 ==========");

        // 헤더 정의
        string[] headers = { "순위", "선수명", "팀", "ERA", "이닝", "승", "패", "세이브", "삼진", "볼넷" };

        // 데이터 준비
        List<string[]> tableData = new List<string[]>();

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

            tableData.Add(rowData);
            Debug.Log($"투수 데이터 추가: {i + 1}위 {pitcher.id.playerName} ({teamFullName})");
        }

        // 새로운 테이블 시스템으로 생성
        CreateTable(headers, tableData);

        Debug.Log("========== 투수 순위 UI 표시 완료 ==========");
    }

    /// <summary>
    /// 새로운 깔끔한 테이블을 생성합니다
    /// </summary>
    void CreateTable(string[] headers, List<string[]> data)
    {
        ClearTable();

        if (tableContainer == null)
        {
            Debug.LogError("TableContainer가 할당되지 않았습니다!");
            return;
        }

        // 메인 테이블 컨테이너 생성
        GameObject table = new GameObject("KBO_Table");
        table.transform.SetParent(tableContainer);

        var tableRect = table.AddComponent<RectTransform>();
        tableRect.anchorMin = new Vector2(0, 1);
        tableRect.anchorMax = new Vector2(1, 1);
        tableRect.pivot = new Vector2(0.5f, 1);

        // Vertical Layout Group으로 헤더와 데이터 행들을 세로 배치
        var verticalLayout = table.AddComponent<VerticalLayoutGroup>();
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.spacing = 1f;

        // Content Size Fitter 추가
        var contentSizeFitter = table.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 헤더 생성
        CreateTableHeader(table.transform, headers);

        // 데이터 행들 생성
        for (int i = 0; i < data.Count; i++)
        {
            CreateTableRow(table.transform, data[i], i);
        }

        Debug.Log($"테이블 생성 완료: 헤더 1개, 데이터 행 {data.Count}개");

        // 테이블 생성 완료 후 다음 프레임에서 RectTransform 강제 Reset
        StartCoroutine(ForceResetAfterLayout(tableRect));
    }

    /// <summary>
    /// 테이블 헤더를 생성합니다
    /// </summary>
    void CreateTableHeader(Transform parent, string[] headers)
    {
        GameObject headerRow = new GameObject("TableHeader");
        headerRow.transform.SetParent(parent);

        var headerRect = headerRow.AddComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(0, 50); // 헤더 높이

        // 배경
        var headerBg = headerRow.AddComponent<Image>();
        headerBg.color = new Color(0.2f, 0.3f, 0.5f, 0.8f); // 진한 파란색

        // Grid Layout으로 정확한 컬럼 정렬
        var gridLayout = headerRow.AddComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = headers.Length;
        gridLayout.cellSize = new Vector2(120, 45);
        gridLayout.spacing = new Vector2(2, 0);
        gridLayout.padding = new RectOffset(5, 5, 2, 2);
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // 헤더 셀들 생성
        for (int i = 0; i < headers.Length; i++)
        {
            CreateHeaderCell(headerRow.transform, headers[i]);
        }
    }

    /// <summary>
    /// 헤더 셀 생성
    /// </summary>
    void CreateHeaderCell(Transform parent, string headerText)
    {
        GameObject cell = new GameObject($"Header_{headerText}");
        cell.transform.SetParent(parent);

        var cellRect = cell.AddComponent<RectTransform>();

        // 텍스트 컴포넌트
        var text = cell.AddComponent<TextMeshProUGUI>();
        text.text = headerText;
        text.fontSize = 14f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        ApplyKoreanFontToText(text);
    }

    /// <summary>
    /// 데이터 행을 생성합니다
    /// </summary>
    void CreateTableRow(Transform parent, string[] rowData, int rowIndex)
    {
        GameObject dataRow = new GameObject($"DataRow_{rowIndex}");
        dataRow.transform.SetParent(parent);

        var rowRect = dataRow.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0, 40); // 행 높이

        // 배경 (짝수/홀수 행 구분)
        var rowBg = dataRow.AddComponent<Image>();
        rowBg.color = rowIndex % 2 == 0 ?
            new Color(1f, 1f, 1f, 0.1f) :
            new Color(0.9f, 0.9f, 0.9f, 0.2f);

        // Grid Layout - 헤더와 동일한 설정
        var gridLayout = dataRow.AddComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = rowData.Length;
        gridLayout.cellSize = new Vector2(120, 35);
        gridLayout.spacing = new Vector2(2, 0);
        gridLayout.padding = new RectOffset(5, 5, 2, 2);
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // 데이터 셀들 생성
        for (int i = 0; i < rowData.Length; i++)
        {
            CreateDataCell(dataRow.transform, rowData[i], i, rowIndex);
        }
    }

    /// <summary>
    /// 데이터 셀 생성
    /// </summary>
    void CreateDataCell(Transform parent, string cellData, int columnIndex, int rowIndex)
    {
        GameObject cell = new GameObject($"Cell_{rowIndex}_{columnIndex}");
        cell.transform.SetParent(parent);

        var cellRect = cell.AddComponent<RectTransform>();

        // 텍스트 컴포넌트
        var text = cell.AddComponent<TextMeshProUGUI>();
        text.text = cellData;
        text.fontSize = 12f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;

        // 특별한 컬럼 스타일링
        if (columnIndex == 1) // 팀 이름 컬럼
        {
            text.fontStyle = FontStyles.Bold;
            // 팀 색상 적용 (필요시)
        }
        else if (columnIndex == 0) // 순위 컬럼
        {
            text.fontStyle = FontStyles.Bold;
            int rank = int.TryParse(cellData, out int r) ? r : 0;
            if (rank <= 3)
            {
                text.color = rank == 1 ? Color.red :
                           rank == 2 ? new Color(1f, 0.5f, 0f) :
                           new Color(0.8f, 0.6f, 0f);
            }
        }

        ApplyKoreanFontToText(text);
    }

    void ClearTable()
    {
        if (tableContainer != null)
        {
            foreach (Transform child in tableContainer)
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

        // 테이블 컨테이너의 Layout Group 강제 갱신
        if (tableContainer != null)
        {
            var layoutGroup = tableContainer.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(tableContainer.GetComponent<RectTransform>());
            }

            // ScrollView의 Content 영역도 갱신
            if (scrollRect != null && scrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }
        }

        Debug.Log("Layout 강제 갱신 완료");
    }

    /// <summary>
    /// 테이블 생성 완료 후 Layout 계산을 기다린 다음 RectTransform을 Reset합니다
    /// </summary>
    IEnumerator ForceResetAfterLayout(RectTransform tableRect)
    {
        // Layout 계산을 위해 2프레임 대기
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (tableRect != null)
        {
            // Unity Inspector의 Reset과 동일한 효과
            tableRect.localPosition = Vector3.zero;
            tableRect.localRotation = Quaternion.identity;
            tableRect.localScale = Vector3.one;
            tableRect.anchorMin = Vector2.zero;
            tableRect.anchorMax = Vector2.one;
            tableRect.anchoredPosition = Vector2.zero;
            tableRect.sizeDelta = Vector2.zero;
            tableRect.pivot = new Vector2(0.5f, 0.5f);

            Debug.Log("테이블 RectTransform 자동 Reset 완료 - 이제 테이블이 올바르게 표시됩니다!");

            // Reset 후 추가 Layout 갱신
            LayoutRebuilder.ForceRebuildLayoutImmediate(tableRect);
        }
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