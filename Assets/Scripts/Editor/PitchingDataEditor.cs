#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// PitcherSO 안의 PitchingPlayerData(구종별 데이터)를 편집하는 인스펙터.
///
/// 쓰는 법
///  - 위쪽 탭으로 구종을 고르고, 그 구종의 구속/존 확률을 편집한다.
///  - 칸에 숫자를 넣고 Enter 치면, "고정"이 안 걸린 나머지 칸들이 기존 비율을 유지한 채
///    합계 100%가 되도록 자동 재분배된다.
///  - 확정한 칸은 "고정"을 켜두면 재분배에서 빠진다 (테두리가 노란색).
///  - 자동 재분배가 거슬리면 토글을 끄고 "지금 재분배" 버튼으로만 맞추면 된다.
///
/// 고정 상태는 게임 데이터가 아니라 편집 편의용이라 PitchingPlayerData에 넣지 않고
/// EditorPrefs에 (에셋 GUID + 구종 인덱스) 키로 25비트 마스크로 저장한다.
/// </summary>
[CustomEditor(typeof(PitcherSO))]
public class PitchingDataEditor : Editor
{
    #region ZONE LAYOUT

    //StrikeZone 프리팹의 zones 배열(25칸)을 화면 배치로 옮긴 표.
    //프리팹의 m_LocalPosition을 그대로 읽어서 만든 것 — x: -0.334~0.334, y: 0.66~-0.66 의 5x5 격자였다.
    //안쪽 3x3(0~8)이 스트라이크존, 바깥 링(9~24)이 볼존.
    //ㄴ 좌우가 뒤집혀 보이면 각 행의 순서만 뒤집으면 된다.
    private const int ZONE_TOTAL = 25;
    private const int ZONE_ROWS = 5;
    private const int ZONE_COLS = 5;
    private const int STRIKE_ZONE_COUNT = 9;

    private static readonly int[,] GRID =
    {
        {  9, 10, 11, 12, 13 },
        { 14,  0,  1,  2, 15 },
        { 16,  3,  4,  5, 17 },
        { 18,  6,  7,  8, 19 },
        { 20, 21, 22, 23, 24 },
    };

    private static readonly string[] ZONE_NAMES =
    {
        "TopLeft", "TopCenter", "TopRight",
        "MiddleLeft", "MiddleCenter", "MiddleRight",
        "BottomLeft", "BottomCenter", "BottomRight",
        "BallZone_TopLeft_Out", "BallZone_TopLeft_Mid", "BallZone_TopCenter_Out",
        "BallZone_TopRight_Mid", "BallZone_TopRight_Out",
        "BallZone_MiddleLeft_Out", "BallZone_MiddleRight_Out",
        "BallZone_CenterLeft_Out", "BallZone_CenterRight_Out",
        "BallZone_BottomLeft_Out", "BallZone_BottomRight_Out",
        "BallZone_BottomLeft_Out2", "BallZone_BottomLeft_Mid", "BallZone_BottomCenter_Out",
        "BallZone_BottomRight_Mid", "BallZone_BottomRight_Out2",
    };

    private static bool IsStrikeZone(int index) => index >= 0 && index < STRIKE_ZONE_COUNT;

    #endregion

    private const float TOTAL_TARGET = 100f;
    private const float CELL_H = 40f;
    private const float PAD = 2f;
    private const string LOCK_PREF = "VRBaseball.PitchingDataEditor.Lock.";

    //인스펙터를 닫았다 열어도 유지되도록 static
    private static bool autoNormalize = true;

    private static readonly GUIContent LOCK_LABEL = new GUIContent("고정");

    private SerializedProperty _pitchingData;
    private int _tab;
    private int _lockMask;         // 25비트. 현재 탭의 고정 상태
    private int _cachedLockTab = -1; // _lockMask가 어느 탭 것인지

    private void OnEnable()
    {
        _pitchingData = serializedObject.FindProperty("pitchingData");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //pitchingData 말고 나머지 필드는 기본 방식으로 (player_name 등)
        DrawPropertiesExcluding(serializedObject, "m_Script", "pitchingData");

        if (_pitchingData == null)
        {
            EditorGUILayout.HelpBox("pitchingData 필드를 찾지 못했습니다. PitcherSO의 필드명을 확인하세요.", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("구종별 피칭 데이터", EditorStyles.boldLabel);

        if (_pitchingData.arraySize == 0)
        {
            EditorGUILayout.HelpBox("등록된 구종이 없습니다. 아래 버튼으로 추가하세요.", MessageType.Info);
            if (GUILayout.Button("구종 추가")) AddPitchingData();
            serializedObject.ApplyModifiedProperties();
            return;
        }

        DrawTabs();

        _tab = Mathf.Clamp(_tab, 0, _pitchingData.arraySize - 1);
        SerializedProperty element = _pitchingData.GetArrayElementAtIndex(_tab);
        SerializedProperty zone = element.FindPropertyRelative("correctZone");

        if (zone == null)
        {
            EditorGUILayout.HelpBox(
                "correctZone을 찾지 못했습니다. PitchingPlayerData의 필드에 [SerializeField]가 붙어 있는지 확인하세요.",
                MessageType.Error);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EnsureZoneSize(zone);
        LoadLocksIfNeeded();

        EditorGUILayout.Space(4);
        DrawBasicFields(element);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("존별 투구 확률 (%)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("가운데 3x3 = 스트라이크존 / 바깥 링 = 볼존", EditorStyles.miniLabel);
        EditorGUILayout.Space(2);

        DrawGrid(zone);

        EditorGUILayout.Space(8);
        DrawSummary(zone);

        EditorGUILayout.Space(4);
        DrawButtons(zone);

        EditorGUILayout.Space(8);
        DrawArrayButtons();

        serializedObject.ApplyModifiedProperties();
    }

    #region TAB / BASIC

    private void DrawTabs()
    {
        string[] labels = new string[_pitchingData.arraySize];
        for (int i = 0; i < labels.Length; i++)
        {
            SerializedProperty pt = _pitchingData.GetArrayElementAtIndex(i).FindPropertyRelative("pitchType");
            if (pt != null && pt.enumDisplayNames.Length > 0)
            {
                int e = Mathf.Clamp(pt.enumValueIndex, 0, pt.enumDisplayNames.Length - 1);
                labels[i] = pt.enumDisplayNames[e];
            }
            else
            {
                labels[i] = "#" + i;
            }
        }

        int next = GUILayout.Toolbar(_tab, labels);
        if (next != _tab)
        {
            _tab = next;
            _cachedLockTab = -1; // 탭이 바뀌면 고정 마스크를 다시 읽는다
        }
    }

    private void DrawBasicFields(SerializedProperty element)
    {
        SerializedProperty pitchType = element.FindPropertyRelative("pitchType");
        SerializedProperty min = element.FindPropertyRelative("min_velocity");
        SerializedProperty max = element.FindPropertyRelative("max_velocity");
        SerializedProperty weight = element.FindPropertyRelative("weight");

        if (pitchType != null) EditorGUILayout.PropertyField(pitchType, new GUIContent("구종"));
        if (min != null) EditorGUILayout.PropertyField(min, new GUIContent("구속 최소 (km/h)"));
        if (max != null) EditorGUILayout.PropertyField(max, new GUIContent("구속 최대 (km/h)"));
        if (weight != null) EditorGUILayout.PropertyField(weight, new GUIContent("확률 가중치"));

        if (min != null && max != null && min.floatValue > max.floatValue)
        {
            EditorGUILayout.HelpBox("구속 최소값이 최대값보다 큽니다.", MessageType.Warning);
        }
    }

    #endregion

    #region GRID

    private void DrawGrid(SerializedProperty zone)
    {
        float maxWeight = 0f;
        for (int i = 0; i < ZONE_TOTAL; i++)
        {
            maxWeight = Mathf.Max(maxWeight, zone.GetArrayElementAtIndex(i).floatValue);
        }

        //인스펙터 폭에 맞춰 칸 크기를 줄인다 (좁은 창에서 잘리지 않게)
        float avail = EditorGUIUtility.currentViewWidth - 40f;
        float cellW = Mathf.Clamp((avail - PAD * (ZONE_COLS - 1)) / ZONE_COLS, 42f, 78f);

        float gridW = cellW * ZONE_COLS + PAD * (ZONE_COLS - 1);
        float gridH = CELL_H * ZONE_ROWS + PAD * (ZONE_ROWS - 1);
        Rect block = GUILayoutUtility.GetRect(gridW, gridH);

        for (int row = 0; row < ZONE_ROWS; row++)
        {
            for (int col = 0; col < ZONE_COLS; col++)
            {
                Rect cell = new Rect(
                    block.x + col * (cellW + PAD),
                    block.y + row * (CELL_H + PAD),
                    cellW, CELL_H);

                DrawCell(cell, zone, GRID[row, col], maxWeight);
            }
        }
    }

    private void DrawCell(Rect cell, SerializedProperty zone, int index, float maxWeight)
    {
        SerializedProperty weight = zone.GetArrayElementAtIndex(index);

        //값이 클수록 진하게 (히트맵). 스트라이크존은 초록, 볼존은 파랑 계열로 구분.
        bool dark = EditorGUIUtility.isProSkin;
        Color empty = dark ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.86f, 0.86f, 0.86f);
        Color hot = IsStrikeZone(index)
            ? (dark ? new Color(0.18f, 0.48f, 0.30f) : new Color(0.55f, 0.86f, 0.63f))
            : (dark ? new Color(0.20f, 0.34f, 0.56f) : new Color(0.62f, 0.77f, 0.96f));

        float t = maxWeight > 0.0001f ? Mathf.Clamp01(weight.floatValue / maxWeight) : 0f;
        EditorGUI.DrawRect(cell, Color.Lerp(empty, hot, t));

        if (IsLocked(index))
        {
            DrawBorder(cell, new Color(1f, 0.78f, 0.2f));
        }

        Rect fieldRect = new Rect(cell.x + 3f, cell.y + 3f, cell.width - 6f, 18f);
        Rect lockRect = new Rect(cell.x + 3f, cell.y + 22f, cell.width - 6f, 15f);

        //DelayedFloatField: 타이핑 도중이 아니라 Enter/포커스 이동 시점에만 반영된다.
        //ㄴ 즉시 반영이면 "1"을 치는 순간 재분배가 돌아서 값을 못 넣는다.
        EditorGUI.BeginChangeCheck();
        float typed = EditorGUI.DelayedFloatField(fieldRect, weight.floatValue);
        if (EditorGUI.EndChangeCheck())
        {
            weight.floatValue = Mathf.Clamp(typed, 0f, TOTAL_TARGET);
            if (autoNormalize)
            {
                Normalize(zone, index); //방금 고친 칸은 그대로 두고 나머지를 맞춘다
            }
        }

        EditorGUI.BeginChangeCheck();
        bool locked = EditorGUI.ToggleLeft(lockRect, LOCK_LABEL, IsLocked(index), EditorStyles.miniLabel);
        if (EditorGUI.EndChangeCheck())
        {
            SetLocked(index, locked);
        }

        //마우스를 올리면 존 이름이 뜨도록 (빈 라벨이라 그려지는 건 없음)
        GUI.Label(cell, new GUIContent(string.Empty, $"[{index}] {ZONE_NAMES[index]}"));
    }

    private static void DrawBorder(Rect r, Color color)
    {
        const float t = 2f;
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), color);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), color);
        EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), color);
        EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), color);
    }

    #endregion

    #region NORMALIZE

    /// <summary>
    /// 고정되지 않은 칸들을 기존 비율대로 늘리거나 줄여서 전체 합을 100%로 맞춘다.
    /// </summary>
    /// <param name="justEditedIndex">방금 손으로 고친 칸. 고정 취급해서 값을 보존한다. 없으면 -1</param>
    private void Normalize(SerializedProperty zone, int justEditedIndex)
    {
        float pinnedSum = 0f;
        List<int> free = new List<int>();

        for (int i = 0; i < ZONE_TOTAL; i++)
        {
            if (IsLocked(i) || i == justEditedIndex)
            {
                pinnedSum += zone.GetArrayElementAtIndex(i).floatValue;
            }
            else
            {
                free.Add(i);
            }
        }

        //전부 고정이면 재분배할 대상이 없다 (합계 경고만 뜬다)
        if (free.Count == 0) return;

        float remaining = Mathf.Max(0f, TOTAL_TARGET - pinnedSum);

        float freeSum = 0f;
        for (int k = 0; k < free.Count; k++)
        {
            freeSum += zone.GetArrayElementAtIndex(free[k]).floatValue;
        }

        for (int k = 0; k < free.Count; k++)
        {
            SerializedProperty p = zone.GetArrayElementAtIndex(free[k]);

            //기존 비율 유지. 전부 0이면 균등하게 나눈다.
            float v = freeSum > 0.0001f
                ? p.floatValue / freeSum * remaining
                : remaining / free.Count;

            p.floatValue = Round2(v);
        }
    }

    private void DistributeEvenly(SerializedProperty zone)
    {
        float pinnedSum = 0f;
        int freeCount = 0;

        for (int i = 0; i < ZONE_TOTAL; i++)
        {
            if (IsLocked(i)) pinnedSum += zone.GetArrayElementAtIndex(i).floatValue;
            else freeCount++;
        }

        if (freeCount == 0) return;

        float each = Round2(Mathf.Max(0f, TOTAL_TARGET - pinnedSum) / freeCount);
        for (int i = 0; i < ZONE_TOTAL; i++)
        {
            if (!IsLocked(i)) zone.GetArrayElementAtIndex(i).floatValue = each;
        }
    }

    #endregion

    #region UI PARTS

    private void DrawSummary(SerializedProperty zone)
    {
        float strikeSum = 0f;
        float ballSum = 0f;
        float lockedSum = 0f;

        for (int i = 0; i < ZONE_TOTAL; i++)
        {
            float v = zone.GetArrayElementAtIndex(i).floatValue;
            if (IsStrikeZone(i)) strikeSum += v;
            else ballSum += v;

            if (IsLocked(i)) lockedSum += v;
        }

        float total = strikeSum + ballSum;

        EditorGUILayout.LabelField($"스트라이크존 합계 : {strikeSum:F1} %   (= 이 구종의 제구력)");
        EditorGUILayout.LabelField($"볼존 합계 : {ballSum:F1} %");

        Color prev = GUI.color;
        GUI.color = Mathf.Abs(total - TOTAL_TARGET) < 0.05f
            ? new Color(0.4f, 1f, 0.5f)
            : new Color(1f, 0.55f, 0.45f);
        EditorGUILayout.LabelField($"전체 합계 : {total:F1} %", EditorStyles.boldLabel);
        GUI.color = prev;

        if (lockedSum > TOTAL_TARGET + 0.05f)
        {
            EditorGUILayout.HelpBox(
                $"고정된 칸의 합이 {lockedSum:F1}% 로 100%를 넘습니다. 고정을 일부 풀어야 재분배가 됩니다.",
                MessageType.Warning);
        }
    }

    private void DrawButtons(SerializedProperty zone)
    {
        // ── 확률 분배 ─────────────────────────────
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        SectionHeader("확률 분배");

        autoNormalize = EditorGUILayout.ToggleLeft(
            new GUIContent("값을 고칠 때마다 자동으로 100% 맞추기",
                "끄면 아래 '지금 재분배'를 눌렀을 때만 합계를 맞춥니다."),
            autoNormalize);

        EditorGUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();
        if (ActionButton("지금 재분배",
                "고정 안 된 칸들의 비율을 유지한 채 합계를 100%로 맞춥니다.",
                "Refresh", NEUTRAL_TINT))
        {
            Normalize(zone, -1);
        }
        if (ActionButton("균등 분배",
                "고정 안 된 칸을 전부 같은 값으로 덮어씁니다. 처음 세팅할 때 쓰세요.",
                null, NEUTRAL_TINT))
        {
            DistributeEvenly(zone);
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4);

        // ── 칸 고정 ───────────────────────────────
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        SectionHeader($"칸 고정      {LockedCount()} / {ZONE_TOTAL} 칸");

        EditorGUILayout.BeginHorizontal();
        if (ActionButton("전체 고정",
                "25칸을 모두 고정합니다. 이 상태에선 재분배가 아무것도 못 바꿉니다.",
                null, LOCK_TINT))
        {
            SetAllLocked(true);
        }
        if (ActionButton("전체 해제",
                "모든 고정을 풉니다.",
                null, NEUTRAL_TINT))
        {
            SetAllLocked(false);
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawArrayButtons()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        SectionHeader($"구종 관리      {_pitchingData.arraySize}개 등록됨");

        EditorGUILayout.BeginHorizontal();

        if (ActionButton("구종 추가",
                "새 구종 데이터를 만들고 그 탭으로 이동합니다.",
                "Toolbar Plus", ADD_TINT))
        {
            AddPitchingData();
        }

        GUI.enabled = _pitchingData.arraySize > 0;
        if (ActionButton("현재 구종 삭제",
                "지금 선택된 탭의 구종 데이터를 지웁니다.",
                "TreeEditor.Trash", DELETE_TINT, 132f)) //글자가 길어서 조금 넓게
        {
            if (EditorUtility.DisplayDialog(
                    "구종 삭제",
                    $"'{CurrentTabLabel()}' 구종 데이터를 삭제할까요?\n\n되돌리려면 Ctrl+Z 를 누르세요.",
                    "삭제", "취소"))
            {
                RemovePitchingData();
            }
        }
        GUI.enabled = true;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void AddPitchingData()
    {
        _pitchingData.arraySize++;
        _tab = _pitchingData.arraySize - 1;
        _cachedLockTab = -1;
    }

    private void RemovePitchingData()
    {
        _pitchingData.DeleteArrayElementAtIndex(_tab);
        _tab = Mathf.Clamp(_tab, 0, Mathf.Max(0, _pitchingData.arraySize - 1));
        _cachedLockTab = -1;
    }

    #endregion

    #region UI STYLE HELPER

    private const float BTN_H = 24f;
    private const float BTN_W = 116f; //인스펙터 폭 전체로 늘어나지 않게 고정 폭을 준다

    private static readonly Color NEUTRAL_TINT = Color.white;
    private static readonly Color ADD_TINT = new Color(0.62f, 0.95f, 0.68f);
    private static readonly Color DELETE_TINT = new Color(1f, 0.58f, 0.52f);
    private static readonly Color LOCK_TINT = new Color(1f, 0.86f, 0.45f);

    /// <summary> 박스 안 소제목 + 밑줄 </summary>
    private static void SectionHeader(string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);

        Rect line = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(line, EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.12f)
            : new Color(0f, 0f, 0f, 0.15f));

        EditorGUILayout.Space(3);
    }

    /// <summary>
    /// 아이콘 + 툴팁 + 색조가 붙은 버튼.
    /// 유니티 버전마다 내장 아이콘 이름이 달라서, 못 찾으면 조용히 글자만 있는 버튼이 된다.
    /// </summary>
    private static bool ActionButton(string text, string tooltip, string iconName, Color tint, float width = BTN_W)
    {
        GUIContent content = new GUIContent(text, tooltip);

        if (!string.IsNullOrEmpty(iconName))
        {
            try
            {
                GUIContent icon = EditorGUIUtility.IconContent(iconName);
                if (icon != null && icon.image != null)
                {
                    content = new GUIContent(" " + text, icon.image, tooltip);
                }
            }
            catch
            {
                //무시하고 글자 버튼으로 폴백
            }
        }

        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = tint;
        bool clicked = GUILayout.Button(content, GUILayout.Height(BTN_H), GUILayout.Width(width));
        GUI.backgroundColor = prev;

        return clicked;
    }

    private int LockedCount()
    {
        int n = 0;
        for (int i = 0; i < ZONE_TOTAL; i++)
        {
            if (IsLocked(i)) n++;
        }
        return n;
    }

    private string CurrentTabLabel()
    {
        if (_pitchingData == null || _pitchingData.arraySize == 0) return "-";

        SerializedProperty pt = _pitchingData
            .GetArrayElementAtIndex(Mathf.Clamp(_tab, 0, _pitchingData.arraySize - 1))
            .FindPropertyRelative("pitchType");

        if (pt != null && pt.enumDisplayNames.Length > 0)
        {
            int e = Mathf.Clamp(pt.enumValueIndex, 0, pt.enumDisplayNames.Length - 1);
            return pt.enumDisplayNames[e];
        }
        return "#" + _tab;
    }

    #endregion

    #region LOCK (EditorPrefs 25비트 마스크)

    private string LockKey()
    {
        string path = AssetDatabase.GetAssetPath(target);
        string id = string.IsNullOrEmpty(path)
            ? target.GetInstanceID().ToString()
            : AssetDatabase.AssetPathToGUID(path);
        return LOCK_PREF + id + "." + _tab;
    }

    private void LoadLocksIfNeeded()
    {
        if (_cachedLockTab == _tab) return;
        _lockMask = EditorPrefs.GetInt(LockKey(), 0);
        _cachedLockTab = _tab;
    }

    private bool IsLocked(int zoneIndex)
    {
        return (_lockMask & (1 << zoneIndex)) != 0;
    }

    private void SetLocked(int zoneIndex, bool value)
    {
        if (value) _lockMask |= (1 << zoneIndex);
        else _lockMask &= ~(1 << zoneIndex);

        EditorPrefs.SetInt(LockKey(), _lockMask);
    }

    private void SetAllLocked(bool value)
    {
        _lockMask = value ? (1 << ZONE_TOTAL) - 1 : 0;
        EditorPrefs.SetInt(LockKey(), _lockMask);
    }

    #endregion

    #region HELPER

    private static void EnsureZoneSize(SerializedProperty zone)
    {
        if (zone.arraySize != ZONE_TOTAL) zone.arraySize = ZONE_TOTAL;
    }

    private static float Round2(float v)
    {
        return Mathf.Round(v * 100f) / 100f;
    }

    #endregion
}

#endif
