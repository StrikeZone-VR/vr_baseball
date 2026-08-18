#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// StrikeZone 인스펙터.
/// zones 배열을 "실제 좌표 배치" 그대로 격자에 그려서, 배열 순서가 화면 배치와
/// 맞는지 눈으로 확인하고 버튼 한 번으로 정렬할 수 있게 한다.
///
/// 칸 안의 큰 숫자 = 그 위치에 있는 존의 현재 배열 인덱스.
/// 정렬이 끝나면 0,1,2,... 순서대로 읽히고, 그때 index = 행*5 + 열 이 성립한다.
/// ㄴ 좌표가 격자로 안 떨어지면(행마다 개수가 다르면) 정렬하지 않고 경고만 띄운다.
/// </summary>
[CustomEditor(typeof(StrikeZone))]
public class StrikeZoneEditor : Editor
{
    //같은 행으로 볼 y 오차. 존 간격이 0.33이라 0.01이면 충분히 안전하다.
    private const float ROW_TOLERANCE = 0.01f;

    private const float CELL_H = 34f;
    private const float PAD = 2f;

    private SerializedProperty _zones;

    private void OnEnable()
    {
        _zones = serializedObject.FindProperty("zones");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script", "zones");

        if (_zones == null)
        {
            EditorGUILayout.HelpBox("zones 배열을 찾지 못했습니다.", MessageType.Error);
            return;
        }

        //원본 배열도 접어서 볼 수 있게 남겨둔다
        EditorGUILayout.Space(6);
        EditorGUILayout.PropertyField(_zones, new GUIContent("Zones (원본 배열)"), true);

        EditorGUILayout.Space(10);
        DrawSortPanel();

        serializedObject.ApplyModifiedProperties();
    }

    #region SORT PANEL

    private void DrawSortPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("존 배치 확인 / 정렬", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("칸 안의 숫자 = 그 자리에 있는 존의 현재 배열 인덱스", EditorStyles.miniLabel);
        EditorGUILayout.Space(3);

        List<List<int>> rows = BuildRowsByPosition();

        if (rows.Count == 0)
        {
            EditorGUILayout.HelpBox("zones 배열이 비어 있습니다.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        DrawPreviewGrid(rows);

        EditorGUILayout.Space(4);

        bool uniform = IsUniformGrid(rows, out int cols);
        bool sorted = IsAlreadySorted(rows);

        if (!uniform)
        {
            //여기가 사수님이 걱정하신 "좌표대로 하면 이상해지는" 경우
            EditorGUILayout.HelpBox(
                "좌표가 반듯한 격자로 떨어지지 않습니다.\n" +
                "행마다 존 개수가 다릅니다 — 위 미리보기에서 어긋난 줄을 확인하세요.\n" +
                "이 상태로 정렬하면 인덱스가 뒤죽박죽이 되므로 정렬 버튼을 막아둡니다.",
                MessageType.Warning);
        }
        else if (sorted)
        {
            EditorGUILayout.HelpBox(
                $"이미 정렬되어 있습니다. (가로 {cols}칸 기준 index = 행*{cols} + 열)",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "배열 순서가 화면 배치와 다릅니다. 아래 버튼으로 맞출 수 있습니다.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(2);

        GUI.enabled = uniform && !sorted;
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.62f, 0.95f, 0.68f);
        if (GUILayout.Button(
                new GUIContent("좌표 순서로 정렬", "위→아래, 왼→오른 순으로 zones 배열을 다시 담습니다. Ctrl+Z로 되돌릴 수 있습니다."),
                GUILayout.Height(24f), GUILayout.Width(140f)))
        {
            ApplySort(rows);
        }
        GUI.backgroundColor = prev;
        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewGrid(List<List<int>> rows)
    {
        int maxCols = 0;
        for (int r = 0; r < rows.Count; r++)
        {
            maxCols = Mathf.Max(maxCols, rows[r].Count);
        }
        if (maxCols == 0) return;

        float avail = EditorGUIUtility.currentViewWidth - 50f;
        float cellW = Mathf.Clamp((avail - PAD * (maxCols - 1)) / maxCols, 40f, 90f);

        float gridW = cellW * maxCols + PAD * (maxCols - 1);
        float gridH = CELL_H * rows.Count + PAD * (rows.Count - 1);
        Rect block = GUILayoutUtility.GetRect(gridW, gridH);

        int expected = 0;
        bool dark = EditorGUIUtility.isProSkin;

        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < rows[r].Count; c++)
            {
                int arrayIndex = rows[r][c];

                Rect cell = new Rect(
                    block.x + c * (cellW + PAD),
                    block.y + r * (CELL_H + PAD),
                    cellW, CELL_H);

                //제자리면 초록, 어긋났으면 빨강 — 정렬이 필요한지 한눈에 보이게
                bool inPlace = arrayIndex == expected;
                Color bg = inPlace
                    ? (dark ? new Color(0.18f, 0.40f, 0.26f) : new Color(0.70f, 0.90f, 0.74f))
                    : (dark ? new Color(0.45f, 0.22f, 0.20f) : new Color(0.96f, 0.74f, 0.70f));
                EditorGUI.DrawRect(cell, bg);

                string full = ZoneName(arrayIndex);
                string shortName = full.StartsWith("BallZone_") ? full.Substring(9) : full;

                Rect numRect = new Rect(cell.x + 2f, cell.y + 2f, cell.width - 4f, 15f);
                Rect nameRect = new Rect(cell.x + 2f, cell.y + 17f, cell.width - 4f, 14f);

                GUI.Label(numRect, arrayIndex.ToString(), EditorStyles.boldLabel);
                GUI.Label(nameRect, new GUIContent(shortName, $"[{arrayIndex}] {full}"), EditorStyles.miniLabel);

                expected++;
            }
        }
    }

    #endregion

    #region ANALYSIS

    /// <summary>
    /// 실제 좌표로 행을 만든다. 위→아래 순의 행 목록이고, 각 행은 왼→오른 정렬된 배열 인덱스들.
    /// </summary>
    private List<List<int>> BuildRowsByPosition()
    {
        List<List<int>> rows = new List<List<int>>();

        StrikeZone sz = target as StrikeZone;
        if (sz == null) return rows;

        Transform origin = sz.transform;

        //유효한 항목만 모은다 (빈 칸 무시)
        List<int> valid = new List<int>();
        for (int i = 0; i < _zones.arraySize; i++)
        {
            if (_zones.GetArrayElementAtIndex(i).objectReferenceValue as Transform != null)
            {
                valid.Add(i);
            }
        }
        if (valid.Count == 0) return rows;

        valid.Sort((a, b) =>
        {
            Vector3 pa = LocalPos(origin, a);
            Vector3 pb = LocalPos(origin, b);

            if (Mathf.Abs(pa.y - pb.y) > ROW_TOLERANCE)
            {
                return pb.y.CompareTo(pa.y); // y가 큰 쪽(위)이 앞
            }
            return pa.x.CompareTo(pb.x);     // x가 작은 쪽(왼쪽)이 앞
        });

        float currentY = 0f;
        for (int k = 0; k < valid.Count; k++)
        {
            float y = LocalPos(origin, valid[k]).y;

            if (rows.Count == 0 || Mathf.Abs(y - currentY) > ROW_TOLERANCE)
            {
                rows.Add(new List<int>());
                currentY = y;
            }
            rows[rows.Count - 1].Add(valid[k]);
        }
        return rows;
    }

    /// <summary> 모든 행의 칸 수가 같은지 (= 반듯한 격자인지) </summary>
    private static bool IsUniformGrid(List<List<int>> rows, out int cols)
    {
        cols = rows.Count > 0 ? rows[0].Count : 0;
        if (cols == 0) return false;

        for (int r = 1; r < rows.Count; r++)
        {
            if (rows[r].Count != cols) return false;
        }
        return true;
    }

    /// <summary> 좌표 순서대로 읽었을 때 배열 인덱스가 0,1,2,... 인지 </summary>
    private static bool IsAlreadySorted(List<List<int>> rows)
    {
        int expected = 0;
        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < rows[r].Count; c++)
            {
                if (rows[r][c] != expected) return false;
                expected++;
            }
        }
        return true;
    }

    private Vector3 LocalPos(Transform origin, int arrayIndex)
    {
        Transform t = _zones.GetArrayElementAtIndex(arrayIndex).objectReferenceValue as Transform;
        return t != null ? origin.InverseTransformPoint(t.position) : Vector3.zero;
    }

    private string ZoneName(int arrayIndex)
    {
        Transform t = _zones.GetArrayElementAtIndex(arrayIndex).objectReferenceValue as Transform;
        return t != null ? t.name : "(비어 있음)";
    }

    #endregion

    private void ApplySort(List<List<int>> rows)
    {
        //현재 참조를 좌표 순서대로 뽑아서 그대로 다시 담는다
        List<Object> ordered = new List<Object>();
        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < rows[r].Count; c++)
            {
                ordered.Add(_zones.GetArrayElementAtIndex(rows[r][c]).objectReferenceValue);
            }
        }

        _zones.arraySize = ordered.Count;
        for (int i = 0; i < ordered.Count; i++)
        {
            _zones.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
        }

        serializedObject.ApplyModifiedProperties();
        Debug.Log($"[StrikeZone] zones {ordered.Count}개를 좌표 순서로 정렬했습니다.", target);
    }
}

#endif
