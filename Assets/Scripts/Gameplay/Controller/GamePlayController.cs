using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GamePlayController : GameController
{
    [Header("UI")]
    [SerializeField] private UIGameStatus[] _UIGameStatusElements;
    [SerializeField] private TextMeshProUGUI[] _scoreTexts;
    [SerializeField] private TextMeshProUGUI _inningText;

    public void SetInningText(int inning)
    {
        string t = inning % 2 == 0 ? "▲" : "▼";
        t += " " + (inning / 2 + 1) + "이닝";
        _inningText.text = t;
    }

    public void SetUIGameStatusIndex(int index, int value)
    {
        _UIGameStatusElements[index].SetIndex(value);
    }

    public void SetScoreText(int index, int score)
    {
        _scoreTexts[index].text = score.ToString();
    }
}
