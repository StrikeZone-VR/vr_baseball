using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScoreboardDisplay : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private BaseballModel baseballModel;
    [SerializeField] private GamePlayModel gamePlayModel;

    [Header("Event 구독")]
    [SerializeField] private VoidEventSO addBallCountEvent;
    [SerializeField] private VoidEventSO addStrikeEvent;
    [SerializeField] private VoidEventSO flyingOutEvent;
    [SerializeField] private IntEventSO outRunnerEvent;

    [Header("UI Dots")]
    [SerializeField] private Image[] ballDots = new Image[4];
    [SerializeField] private Image[] strikeDots = new Image[2];
    [SerializeField] private Image[] outDots = new Image[2];

    [Header("Team / LineScore")]
    [SerializeField] private string[] teamNames = { "Red", "Blue" }; // 0:원정(▲) 1:홈(▼)
    [SerializeField] private TMP_Text awayTeamText;  // 팀0 이름
    [SerializeField] private TMP_Text homeTeamText;  // 팀1 이름
    // 라인스코어 셀: index 0~8 = 1~9이닝, 마지막(9) = 총점(R). 각 칸이 독립 텍스트라
    // 두 자리 이상이어도 칸 안에서 가운데 정렬 + 자동축소로 들어가고, 세로 정렬은 칸 위치가 보장
    [SerializeField] private TMP_Text[] awayCells; // 팀0 (길이 = 이닝수+1)
    [SerializeField] private TMP_Text[] homeCells; // 팀1

    [Header("Runner Diamond (1,2,3루)")]
    [SerializeField] private Image[] runnerBases = new Image[3]; // 0:1루 1:2루 2:3루

    [Header("Colors")]
    [SerializeField] private Color ballLitColor = Color.green;
    [SerializeField] private Color strikeLitColor = Color.yellow;
    [SerializeField] private Color outLitColor = Color.red;
    [SerializeField] private Color unlitColor = new Color(0.18f, 0.18f, 0.2f, 1f);
    [SerializeField] private Color battingTeamColor = new Color(1f, 0.95f, 0.4f, 1f); // 공격중 팀 강조
    [SerializeField] private Color idleTeamColor = new Color(0.78f, 0.80f, 0.85f, 1f);
    [SerializeField] private Color baseLitColor; //흰색 위에거
    [SerializeField] private Color baseEmptyColor = new Color(0.22f, 0.22f, 0.25f, 1f);

    private const int DISPLAY_INNING = GamePlayModel.MAX_DISPLAY_INNING;

    private int _lastHash = int.MinValue;

    private void OnEnable()
    {
        if (addBallCountEvent != null) addBallCountEvent.onEventRaised += RefreshAll;
        if (addStrikeEvent != null) addStrikeEvent.onEventRaised += RefreshAll;
        if (flyingOutEvent != null) flyingOutEvent.onEventRaised += RefreshAll;
        if (outRunnerEvent != null) outRunnerEvent.onEventRaised += RefreshFromInt;
    }

    private void OnDisable()
    {
        if (addBallCountEvent != null) addBallCountEvent.onEventRaised -= RefreshAll;
        if (addStrikeEvent != null) addStrikeEvent.onEventRaised -= RefreshAll;
        if (flyingOutEvent != null) flyingOutEvent.onEventRaised -= RefreshAll;
        if (outRunnerEvent != null) outRunnerEvent.onEventRaised -= RefreshFromInt;
    }

    private void Start() => RefreshAll();

    private void Update()
    {
        // 리셋(예: 4볼/3스트라이크 후 0)이나 점수/이닝/주자 변화는 별도 이벤트가 없을 수 있어서 변화 감지로 보완
        int hash = ComputeStateHash();
        if (hash != _lastHash) RefreshAll();
    }

    private void RefreshFromInt(int _) => RefreshAll();

    // 점수/이닝/주자/카운트 상태를 한 번에 비교하기 위한 가벼운 해시(매 프레임 문자열 할당 방지)
    private int ComputeStateHash()
    {
        if (baseballModel == null || gamePlayModel == null) return _lastHash;

        int h = 17;
        h = h * 31 + baseballModel.BallCount;
        h = h * 31 + baseballModel.Strike;
        h = h * 31 + gamePlayModel.OutCount;
        h = h * 31 + gamePlayModel.Inning;
        h = h * 31 + gamePlayModel.GetTeamScore(0);
        h = h * 31 + gamePlayModel.GetTeamScore(1);
        for (int b = 1; b <= 3; b++)
            h = h * 31 + (gamePlayModel.IsEmptyRunner(b) ? 0 : 1);
        for (int t = 0; t < 2; t++)
            for (int n = 0; n < DISPLAY_INNING; n++)
                h = h * 31 + gamePlayModel.GetInningScore(t, n);
        return h;
    }

    private void RefreshAll()
    {
        if (baseballModel == null || gamePlayModel == null) return;

        // B/S/O 카운트 점
        UpdateDots(ballDots, baseballModel.BallCount, ballLitColor);
        UpdateDots(strikeDots, baseballModel.Strike, strikeLitColor);
        UpdateDots(outDots, gamePlayModel.OutCount, outLitColor);

        RefreshTeams();
        RefreshLineScore();
        RefreshRunners();

        _lastHash = ComputeStateHash();
    }

    private void UpdateDots(Image[] dots, int count, Color litColor)
    {
        if (dots == null) return;
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] != null) dots[i].color = i < count ? litColor : unlitColor;
        }
    }

    // 팀 이름 + 누가 공격중인지(▲/▼ + 강조색)
    private void RefreshTeams()
    {
        int batting = gamePlayModel.GetTeamIndex();
        SetTeam(awayTeamText, 0, batting);
        SetTeam(homeTeamText, 1, batting);
    }

    private void SetTeam(TMP_Text t, int team, int batting)
    {
        if (t == null) return;
        string name = (teamNames != null && team < teamNames.Length) ? teamNames[team] : ("Team" + team);
        string arrow = team == 0 ? "▲" : "▼"; // ▲ / ▼
        bool isBatting = team == batting;
        t.text = (isBatting ? arrow + " " : "    ") + name;
        t.color = isBatting ? battingTeamColor : idleTeamColor;
    }

    // 라인스코어: 팀별 이닝 셀 + 총점 셀 채우기 (헤더 셀은 고정 라벨이라 갱신 불필요)
    private void RefreshLineScore()
    {
        int curInning = gamePlayModel.Inning / 2; // 0-based 현재 이닝
        FillCells(awayCells, 0, curInning);
        FillCells(homeCells, 1, curInning);
    }

    private void FillCells(TMP_Text[] cells, int team, int curInning)
    {
        if (cells == null) return;
        for (int n = 0; n < DISPLAY_INNING && n < cells.Length; n++)
        {
            if (cells[n] == null) continue;
            // 진행된(또는 진행중인) 이닝만 숫자, 미래 이닝은 빈칸
            cells[n].text = (n <= curInning) ? gamePlayModel.GetInningScore(team, n).ToString() : "";
        }
        // 마지막 칸 = 총점(R)
        int totalIdx = DISPLAY_INNING;
        if (totalIdx < cells.Length && cells[totalIdx] != null)
            cells[totalIdx].text = gamePlayModel.GetTeamScore(team).ToString();
    }

    // 주자 다이아몬드: 1/2/3루 점유 여부
    private void RefreshRunners()
    {
        if (runnerBases == null) return;
        for (int b = 1; b <= 3 && b - 1 < runnerBases.Length; b++)
        {
            Image img = runnerBases[b - 1];
            if (img == null) continue;
            bool occupied = !gamePlayModel.IsEmptyRunner(b);
            img.color = occupied ? baseLitColor : baseEmptyColor;
        }
    }

#if UNITY_EDITOR
    // ───────────────────────────────────────────────────────────────
    // 에디터 빌더: Stadium 프리펩에 라인스코어/팀이름/주자 다이아몬드 오브젝트를
    // 실제로 생성·배치하고 위 참조들을 자동 연결한다.
    // 프리펩 모드(Stadium)에서 ScoreboardDisplay 우클릭 → 이 메뉴 실행.
    // 배치 후 위치/크기는 에디터에서 자유롭게 다듬으면 됨.
    // ───────────────────────────────────────────────────────────────
    // Stadium 프리펩을 직접 열어서 빌드 후 저장 (메뉴: Tools/Scoreboard/...)
    // → 사용자가 프리펩 모드를 직접 열지 않아도 됨.
    private const string STADIUM_PREFAB_PATH = "Assets/Res/Stadium/Stadium.prefab";

    [MenuItem("Tools/Scoreboard/전광판 레이아웃 빌드 (Stadium Prefab)")]
    private static void BuildInStadiumPrefab()
    {
        string path = STADIUM_PREFAB_PATH;
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogError($"[Scoreboard] 프리펩을 열 수 없습니다: {path}");
            return;
        }

        try
        {
            ScoreboardDisplay sb = root.GetComponentInChildren<ScoreboardDisplay>(true);
            if (sb == null)
            {
                Debug.LogError("[Scoreboard] 프리펩에서 ScoreboardDisplay를 찾을 수 없습니다.");
                return;
            }

            sb.BuildScoreboardLayout();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("[Scoreboard] Stadium 프리펩에 전광판 레이아웃을 빌드하고 저장했습니다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [ContextMenu("전광판 레이아웃 빌드 (이미지 스타일)")]
    private void BuildScoreboardLayout()
    {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            Debug.LogError("[Scoreboard] Canvas를 찾을 수 없습니다. Scoreboard 하위에 WorldSpace Canvas가 있어야 합니다.");
            return;
        }
        RectTransform root = canvas.GetComponent<RectTransform>();
        Transform rootT = root.transform;

        // 수동 배치를 위해 Canvas의 자동 세로 레이아웃 제거
        VerticalLayoutGroup vlg = canvas.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) DestroyImmediate(vlg);

        // 폰트는 기존 라벨에서 가져와 통일
        TMP_FontAsset font = null;
        TMP_Text anyText = GetComponentInChildren<TMP_Text>(true);
        if (anyText != null) font = anyText.font;

        // 보드 좌표: anchor/pivot = (0,1) 좌상단 기준. x:0→500(우), y:0→-300(아래)
        Vector2 TL = new Vector2(0, 1);

        // ── 배경: 보드 전체를 채우는 어두운 패널 ──
        Transform bgT = rootT.Find("Background");
        if (bgT != null)
        {
            RectTransform bg = bgT as RectTransform;
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
            bg.pivot = new Vector2(0.5f, 0.5f);
            bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
            LayoutElement bgle = bg.GetComponent<LayoutElement>();
            if (bgle != null) bgle.ignoreLayout = true;
        }

        // ── 팀 이름 (좌측) ──
        awayTeamText = EnsureText(rootT, "AwayTeam", font);
        ConfigText(awayTeamText, 28, TextAlignmentOptions.MidlineLeft);
        SetRect(awayTeamText.rectTransform, TL, TL, TL, new Vector2(20, -86), new Vector2(140, 40));

        homeTeamText = EnsureText(rootT, "HomeTeam", font);
        ConfigText(homeTeamText, 28, TextAlignmentOptions.MidlineLeft);
        SetRect(homeTeamText.rectTransform, TL, TL, TL, new Vector2(20, -120), new Vector2(140, 40));

        // ── 라인스코어 격자 (가운데) ──
        // 각 칸이 독립 셀(가운데정렬 + 자동축소)이라 두 자리 이상 득점도 칸 안에서 알아서 맞춰지고
        // 세로 정렬은 칸의 고정 X 위치가 보장한다. (구버전 단일 텍스트 잔재 제거)
        DeleteChild(rootT, "LineHeader");
        DeleteChild(rootT, "AwayLine");
        DeleteChild(rootT, "HomeLine");

        RectTransform grid = Ensure(rootT, "LineGrid");
        SetRect(grid, TL, TL, TL, Vector2.zero, new Vector2(500, 300));

        const float gridX = 165f, colW = 21f, cellH = 30f;
        const float headerY = -52f, awayY = -86f, homeY = -120f;
        Color headerColor = new Color(0.95f, 0.85f, 0.35f, 1f); // 헤더(이닝번호) 노란빛

        awayCells = new TMP_Text[DISPLAY_INNING + 1];
        homeCells = new TMP_Text[DISPLAY_INNING + 1];
        for (int c = 0; c <= DISPLAY_INNING; c++)
        {
            float x = gridX + c * colW;
            string label = (c < DISPLAY_INNING) ? (c + 1).ToString() : "R"; // 마지막 칸은 총점 R

            TMP_Text hc = MakeCell(grid, "H" + c, x, headerY, colW, cellH, font);
            hc.text = label;
            hc.color = headerColor;

            awayCells[c] = MakeCell(grid, "A" + c, x, awayY, colW, cellH, font);
            homeCells[c] = MakeCell(grid, "M" + c, x, homeY, colW, cellH, font);
        }

        // ── B/S/O 점들: 우측 상단으로 재배치 + 작게 ──
        RelayoutCountRow(ballDots, -54f);
        RelayoutCountRow(strikeDots, -88f);
        RelayoutCountRow(outDots, -122f);

        // ── 주자 다이아몬드 (우측 하단) ──
        RectTransform diamond = Ensure(rootT, "RunnerDiamond");
        SetRect(diamond, TL, TL, TL, Vector2.zero, new Vector2(500, 300));
        float cx = 430f, cy = -225f, r = 26f, s = 20f;
        // 0:1루(우) 1:2루(상) 2:3루(좌)
        runnerBases = new Image[3];
        runnerBases[0] = MakeBase(diamond, "Base1", cx + r, cy, s);       // 1루
        runnerBases[1] = MakeBase(diamond, "Base2", cx, cy + r, s);       // 2루
        runnerBases[2] = MakeBase(diamond, "Base3", cx - r, cy, s);       // 3루
        Image homeMark = MakeBase(diamond, "HomePlate", cx, cy - r, s);   // 홈(표시용, 고정색)
        homeMark.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        // 초기 색/텍스트 한 번 채워줌(에디터 프리뷰 — 플레이 시 RefreshAll이 실제 값으로 갱신)
        foreach (Image b in runnerBases) if (b != null) b.color = baseEmptyColor;

        string name0 = (teamNames != null && teamNames.Length > 0) ? teamNames[0] : "Red";
        string name1 = (teamNames != null && teamNames.Length > 1) ? teamNames[1] : "Blue";
        if (awayTeamText != null) { awayTeamText.text = "▲ " + name0; awayTeamText.color = battingTeamColor; }
        if (homeTeamText != null) { homeTeamText.text = "    " + name1; homeTeamText.color = idleTeamColor; }
        // 라인스코어 프리뷰: 전부 0 (플레이 시 RefreshAll이 실제 값으로 갱신)
        for (int c = 0; c <= DISPLAY_INNING; c++)
        {
            if (awayCells[c] != null) awayCells[c].text = "0";
            if (homeCells[c] != null) homeCells[c].text = "0";
        }

        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(gameObject);
        Debug.Log("[Scoreboard] 레이아웃 빌드 완료. 위치/크기는 에디터에서 다듬은 뒤 프리펩을 저장하세요.");
    }

    // B/S/O 한 줄을 우측으로 재배치(라벨 + 점들), 점은 작게
    private void RelayoutCountRow(Image[] dots, float y)
    {
        if (dots == null || dots.Length == 0 || dots[0] == null) return;

        Vector2 TL = new Vector2(0, 1);
        Transform row = dots[0].transform.parent;

        // 행 컨테이너를 보드 전체 크기 투명 컨테이너로 만들어 자식 좌표를 절대좌표처럼 사용
        RectTransform rowRt = row as RectTransform;
        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) DestroyImmediate(hlg);
        LayoutElement rowLe = row.GetComponent<LayoutElement>();
        if (rowLe != null) DestroyImmediate(rowLe);
        SetRect(rowRt, TL, TL, TL, Vector2.zero, new Vector2(500, 300));

        // 라벨(B/S/O)
        Transform labelT = row.Find("Label");
        if (labelT != null)
        {
            TMP_Text lt = labelT.GetComponent<TMP_Text>();
            if (lt != null) lt.fontSize = 20;
            LayoutElement le = labelT.GetComponent<LayoutElement>();
            if (le != null) DestroyImmediate(le);
            SetRect(labelT as RectTransform, TL, TL, TL, new Vector2(384, y + 2), new Vector2(24, 22));
        }

        // 점들
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null) continue;
            LayoutElement le = dots[i].GetComponent<LayoutElement>();
            if (le != null) DestroyImmediate(le);
            SetRect(dots[i].rectTransform, TL, TL, TL, new Vector2(410 + i * 22, y), new Vector2(18, 18));
            dots[i].color = unlitColor;
        }
    }

    // 라인스코어 셀 하나 생성(가운데정렬 + 자동축소)
    private TMP_Text MakeCell(Transform parent, string name, float x, float y, float w, float h, TMP_FontAsset font)
    {
        Vector2 TL = new Vector2(0, 1);
        TMP_Text t = EnsureText(parent, name, font);
        ConfigCell(t);
        SetRect(t.rectTransform, TL, TL, TL, new Vector2(x, y), new Vector2(w, h));
        return t;
    }

    private void ConfigCell(TMP_Text t)
    {
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.color = Color.white;
        t.richText = false;
        t.enableAutoSizing = true; // 칸보다 큰 숫자(세 자리 등)는 자동으로 줄어듦
        t.fontSizeMin = 8f;
        t.fontSizeMax = 20f;
    }

    private void DeleteChild(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) DestroyImmediate(t.gameObject);
    }

    private Image MakeBase(Transform parent, string name, float cx, float cy, float s)
    {
        Vector2 TL = new Vector2(0, 1);
        Image img = EnsureImage(parent, name);
        SetRect(img.rectTransform, TL, TL, TL, new Vector2(cx - s / 2f, cy + s / 2f), new Vector2(s, s));
        img.rectTransform.localEulerAngles = new Vector3(0, 0, 45); // 다이아몬드 모양
        return img;
    }

    private RectTransform Ensure(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            t = go.transform;
            t.SetParent(parent, false);
        }
        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt == null) rt = t.gameObject.AddComponent<RectTransform>();
        return rt;
    }

    private TMP_Text EnsureText(Transform parent, string name, TMP_FontAsset font)
    {
        RectTransform rt = Ensure(parent, name);
        TMP_Text tmp = rt.GetComponent<TMP_Text>();
        if (tmp == null) tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        return tmp;
    }

    private Image EnsureImage(Transform parent, string name)
    {
        RectTransform rt = Ensure(parent, name);
        Image img = rt.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        return img;
    }

    private void ConfigText(TMP_Text t, float fontSize, TextAlignmentOptions align)
    {
        t.fontSize = fontSize;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        t.color = Color.white;
        t.richText = true;
    }

    private void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }
#endif
}
