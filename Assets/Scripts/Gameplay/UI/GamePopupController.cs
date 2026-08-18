using DG.Tweening;
using TMPro;
using UnityEngine;

public class GamePopupController : MonoBehaviour
{
    [Header("Event 구독")]
    [SerializeField] private VoidEventSO addOutEvent;
    [SerializeField] private VoidEventSO homerunEvent;
    [SerializeField] private VoidEventSO walkEvent;

    [Header("UI")]
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private TMP_Text popupText;

    [Header("Animation")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float holdDuration = 0.6f;
    [SerializeField] private float hideDuration = 0.35f;
    [SerializeField] private float overshootScale = 1.3f;
    [SerializeField] private float settleScale = 1.0f;

    [Header("Messages")]
    [SerializeField] private string outText = "OUT!";
    [SerializeField] private Color outColor = new Color(1f, 0.15f, 0.1f, 1f);
    [SerializeField] private string homerunText = "HOMERUN!";
    [SerializeField] private Color homerunColor = new Color(1f, 0.8f, 0.1f, 1f);
    [SerializeField] private string walkText = "BALL FOUR!";
    [SerializeField] private Color walkColor = new Color(0.3f, 1f, 0.4f, 1f);

    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;
    [SerializeField] private KeyCode debugOutKey = KeyCode.O;
    [SerializeField] private KeyCode debugHomerunKey = KeyCode.H;
    [SerializeField] private KeyCode debugWalkKey = KeyCode.W;

    private Sequence _activeSequence;

    private void Awake()
    {
        if (popupText == null) popupText = GetComponentInChildren<TMP_Text>(true);
        if (popupRoot == null && popupText != null) popupRoot = popupText.rectTransform;
    }

    private void OnEnable()
    {
        if (addOutEvent != null) addOutEvent.onEventRaised += OnOut;
        if (homerunEvent != null) homerunEvent.onEventRaised += OnHomerun;
        if (walkEvent != null) walkEvent.onEventRaised += OnWalk;
    }

    private void OnDisable()
    {
        if (addOutEvent != null) addOutEvent.onEventRaised -= OnOut;
        if (homerunEvent != null) homerunEvent.onEventRaised -= OnHomerun;
        if (walkEvent != null) walkEvent.onEventRaised -= OnWalk;
    }

    private void Start()
    {
        if (popupText != null)
        {
            Color c = popupText.color;
            c.a = 0f;
            popupText.color = c;
        }
        if (popupRoot != null) popupRoot.localScale = Vector3.zero;
    }

    private void OnOut() => Show(outText, outColor);
    private void OnHomerun() => Show(homerunText, homerunColor);
    private void OnWalk() => Show(walkText, walkColor);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (!enableDebugKeys) return;
        if (Input.GetKeyDown(debugOutKey)) OnOut();
        else if (Input.GetKeyDown(debugHomerunKey)) OnHomerun();
        else if (Input.GetKeyDown(debugWalkKey)) OnWalk();
    }
#endif

    public void Show(string message, Color color)
    {
        if (popupRoot == null || popupText == null) return;

        _activeSequence?.Kill();
        popupRoot.localScale = Vector3.zero;
        popupText.text = message;
        Color start = color;
        start.a = 0f;
        popupText.color = start;

        _activeSequence = DOTween.Sequence()
            .Append(popupText.DOFade(1f, showDuration))
            .Join(popupRoot.DOScale(overshootScale, showDuration).SetEase(Ease.OutBack))
            .Append(popupRoot.DOScale(settleScale, 0.15f).SetEase(Ease.InOutSine))
            .AppendInterval(holdDuration)
            .Append(popupText.DOFade(0f, hideDuration))
            .Join(popupRoot.DOScale(0f, hideDuration).SetEase(Ease.InBack));
    }
}
