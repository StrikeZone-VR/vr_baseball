using UnityEngine;
using UnityEngine.UI;

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

    [Header("Colors")]
    [SerializeField] private Color ballLitColor = Color.green;
    [SerializeField] private Color strikeLitColor = Color.yellow;
    [SerializeField] private Color outLitColor = Color.red;
    [SerializeField] private Color unlitColor = new Color(0.18f, 0.18f, 0.2f, 1f);

    private int _lastBall = -1, _lastStrike = -1, _lastOut = -1;

    private void OnEnable()
    {
        if (addBallCountEvent != null) addBallCountEvent.onEventRaised += Refresh;
        if (addStrikeEvent != null) addStrikeEvent.onEventRaised += Refresh;
        if (flyingOutEvent != null) flyingOutEvent.onEventRaised += Refresh;
        if (outRunnerEvent != null) outRunnerEvent.onEventRaised += RefreshFromInt;
    }

    private void OnDisable()
    {
        if (addBallCountEvent != null) addBallCountEvent.onEventRaised -= Refresh;
        if (addStrikeEvent != null) addStrikeEvent.onEventRaised -= Refresh;
        if (flyingOutEvent != null) flyingOutEvent.onEventRaised -= Refresh;
        if (outRunnerEvent != null) outRunnerEvent.onEventRaised -= RefreshFromInt;
    }

    private void Start() => Refresh();

    private void Update()
    {
        // 리셋(예: 4볼/3스트라이크 후 0)은 별도 이벤트가 없을 수 있어서 변화 감지로 보완
        if (HasCountChanged()) Refresh();
    }

    private bool HasCountChanged()
    {
        if (baseballModel == null || gamePlayModel == null) return false;
        return baseballModel.BallCount != _lastBall
            || baseballModel.Strike != _lastStrike
            || gamePlayModel.OutCount != _lastOut;
    }

    private void RefreshFromInt(int _) => Refresh();

    private void Refresh()
    {
        if (baseballModel == null || gamePlayModel == null) return;

        int balls = baseballModel.BallCount;
        int strikes = baseballModel.Strike;
        int outs = gamePlayModel.OutCount;

        UpdateDots(ballDots, balls, ballLitColor);
        UpdateDots(strikeDots, strikes, strikeLitColor);
        UpdateDots(outDots, outs, outLitColor);

        _lastBall = balls;
        _lastStrike = strikes;
        _lastOut = outs;
    }

    private void UpdateDots(Image[] dots, int count, Color litColor)
    {
        if (dots == null) return;
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] != null) dots[i].color = i < count ? litColor : unlitColor;
        }
    }
}
