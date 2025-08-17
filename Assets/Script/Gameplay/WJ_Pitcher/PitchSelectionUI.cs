/// <summary>
/// 🎨 구종 선택 UI 관리자 - 직구, 커브, 슬라이더, 포크볼 선택 인터페이스
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class PitchSelectionUI : MonoBehaviour
{
    [Header("UI 구성요소")]
    public Canvas pitchSelectionCanvas;
    public Button[] pitchButtons = new Button[4];
    public TextMeshProUGUI selectedPitchText;
    public Image selectedPitchIcon;
    public Image[] pitchButtonImages;

    [Header("실시간 투구 결과 패널")]
    public TextMeshProUGUI strikeCountText;      // "스트라이크: 2"
    public TextMeshProUGUI ballCountText;        // "볼: 1"  
    public TextMeshProUGUI lastPitchSpeedText;   // "투구 속도: 145 km/h"
    public TextMeshProUGUI lastPitchResultText;  // "결과: 스트라이크!"
    public Image resultBackground;               // 결과 배경 (스트라이크=빨강, 볼=파랑)

    [Header("투구 통계 표시")]
    public TextMeshProUGUI totalPitchesText;     // "총 투구: 15"
    public TextMeshProUGUI strikeRateText;       // "스트라이크율: 73%"
    public TextMeshProUGUI[] pitchTypeCountTexts = new TextMeshProUGUI[4]; // 구종별 사용 횟수

    [Header("게임 컨트롤")]
    public Button resetGameButton;               // 게임 리셋 버튼
    public Button toggleUIButton;                // UI 토글 버튼
    public TextMeshProUGUI gameStatusText;       // "게임 진행 중..."

    [Header("구종 설정")]
    public PitchData[] pitchDataArray = new PitchData[4];

    [Header("VR 상호작용")]
    public XRRayInteractor leftRayInteractor;
    public XRRayInteractor rightRayInteractor;

    [Header("오디오")]
    public AudioClip buttonClickSound;
    public AudioClip strikeSound;
    public AudioClip ballSound;

    private AudioSource audioSource;
    private PitchType currentSelectedPitch = PitchType.FastBall;
    private VRBaseball currentBaseball;

    // 통계 데이터
    private int totalPitches = 0;
    private int strikeCount = 0;
    private int ballCount = 0;
    private int[] pitchTypeUsage = new int[4]; // 구종별 사용 횟수
    private float lastPitchSpeed = 0f;
    private bool lastPitchWasStrike = false;

    public System.Action<PitchType> OnPitchSelected;
    public System.Action OnGameReset;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        InitializePitchData();
        SetupUI();
        SetupGameControls();
        SelectPitch(PitchType.FastBall); // 기본 선택
        UpdateAllUI();
    }

    private void InitializePitchData()
    {
        pitchDataArray[0] = PitchData.GetDefaultPitchData(PitchType.FastBall);
        pitchDataArray[1] = PitchData.GetDefaultPitchData(PitchType.Curve);
        pitchDataArray[2] = PitchData.GetDefaultPitchData(PitchType.Slider);
        pitchDataArray[3] = PitchData.GetDefaultPitchData(PitchType.ForkBall);
    }

    private void SetupUI()
    {
        for (int i = 0; i < pitchButtons.Length; i++)
        {
            if (pitchButtons[i] != null)
            {
                int index = i; // 클로저 문제 해결
                PitchType pitchType = pitchDataArray[i].pitchType;

                // 버튼 클릭 이벤트
                pitchButtons[i].onClick.AddListener(() => SelectPitch(pitchType));

                // 버튼 텍스트 설정
                TextMeshProUGUI buttonText = pitchButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = pitchDataArray[i].pitchName;

                // 버튼 색상 설정
                if (pitchButtonImages[i] != null)
                    pitchButtonImages[i].color = pitchDataArray[i].pitchColor;

                // XR Interactable 추가 (VR 버튼 상호작용을 위해)
                XRSimpleInteractable xrInteractable = pitchButtons[i].GetComponent<XRSimpleInteractable>();
                if (xrInteractable == null)
                {
                    xrInteractable = pitchButtons[i].gameObject.AddComponent<XRSimpleInteractable>();
                }

                // VR 선택 이벤트
                xrInteractable.selectEntered.AddListener((args) => SelectPitch(pitchType));
            }
        }
    }

    private void SetupGameControls()
    {
        // 리셋 버튼 설정
        if (resetGameButton != null)
        {
            resetGameButton.onClick.AddListener(ResetGame);

            // VR 상호작용 추가
            XRSimpleInteractable resetXR = resetGameButton.GetComponent<XRSimpleInteractable>();
            if (resetXR == null)
                resetXR = resetGameButton.gameObject.AddComponent<XRSimpleInteractable>();
            resetXR.selectEntered.AddListener((args) => ResetGame());
        }

        // UI 토글 버튼 설정
        if (toggleUIButton != null)
        {
            toggleUIButton.onClick.AddListener(ToggleUI);

            // VR 상호작용 추가
            XRSimpleInteractable toggleXR = toggleUIButton.GetComponent<XRSimpleInteractable>();
            if (toggleXR == null)
                toggleXR = toggleUIButton.gameObject.AddComponent<XRSimpleInteractable>();
            toggleXR.selectEntered.AddListener((args) => ToggleUI());
        }
    }

    private void UpdateAllUI()
    {
        UpdateCountDisplay();
        UpdateStatisticsDisplay();
        UpdateGameStatusDisplay();
    }

    private void UpdateCountDisplay()
    {
        if (strikeCountText != null)
            strikeCountText.text = $"스트라이크: {strikeCount}";

        if (ballCountText != null)
            ballCountText.text = $"볼: {ballCount}";

        if (lastPitchSpeedText != null)
            lastPitchSpeedText.text = $"투구 속도: {lastPitchSpeed:F1} km/h";

        if (lastPitchResultText != null)
        {
            string resultText = totalPitches == 0 ? "투구 대기 중..." :
                               (lastPitchWasStrike ? "결과: 스트라이크!" : "결과: 볼!");
            lastPitchResultText.text = resultText;
        }

        // 결과 배경 색상 변경
        if (resultBackground != null && totalPitches > 0)
        {
            resultBackground.color = lastPitchWasStrike ?
                new Color(1f, 0.2f, 0.2f, 0.8f) : // 빨간색 (스트라이크)
                new Color(0.2f, 0.2f, 1f, 0.8f);   // 파란색 (볼)
        }
    }

    private void UpdateStatisticsDisplay()
    {
        if (totalPitchesText != null)
            totalPitchesText.text = $"총 투구: {totalPitches}";

        float strikeRate = totalPitches > 0 ? (float)(strikeCount + ballCount > 0 ? strikeCount : 0) / totalPitches * 100f : 0f;
        if (strikeRateText != null)
            strikeRateText.text = $"스트라이크율: {strikeRate:F1}%";

        // 구종별 사용 횟수 업데이트
        for (int i = 0; i < pitchTypeCountTexts.Length && i < pitchDataArray.Length; i++)
        {
            if (pitchTypeCountTexts[i] != null)
            {
                pitchTypeCountTexts[i].text = $"{pitchDataArray[i].pitchName}: {pitchTypeUsage[i]}회";
            }
        }
    }

    private void UpdateGameStatusDisplay()
    {
        if (gameStatusText != null)
        {
            if (strikeCount >= 3)
                gameStatusText.text = "삼진아웃!";
            else if (ballCount >= 4)
                gameStatusText.text = "포볼!";
            else
                gameStatusText.text = $"게임 진행 중... ({strikeCount}S-{ballCount}B)";
        }
    }

    public void SelectPitch(PitchType pitchType)
    {
        currentSelectedPitch = pitchType;
        PitchData selectedData = GetPitchData(pitchType);

        // UI 업데이트
        if (selectedPitchText != null)
            selectedPitchText.text = $"선택된 구종: {selectedData.pitchName}";

        if (selectedPitchIcon != null)
        {
            selectedPitchIcon.color = selectedData.pitchColor;
            if (selectedData.pitchIcon != null)
                selectedPitchIcon.sprite = selectedData.pitchIcon;
        }

        // 버튼 하이라이트 업데이트
        UpdateButtonHighlights(pitchType);

        // 현재 야구공에 구종 적용
        if (currentBaseball != null)
            currentBaseball.SetPitchType(pitchType);

        // 사운드 재생
        if (audioSource != null && buttonClickSound != null)
            audioSource.PlayOneShot(buttonClickSound);

        OnPitchSelected?.Invoke(pitchType);

        // 구종 사용 통계 업데이트
        int pitchIndex = System.Array.FindIndex(pitchDataArray, data => data.pitchType == pitchType);
        if (pitchIndex >= 0 && pitchIndex < pitchTypeUsage.Length)
        {
            pitchTypeUsage[pitchIndex]++;
            UpdateStatisticsDisplay();
        }

        Debug.Log($"구종 선택: {selectedData.pitchName}");
    }

    // 투구 결과 처리 (VRPitchingManager에서 호출)
    public void OnPitchResult(bool isStrike, float pitchSpeed)
    {
        totalPitches++;
        lastPitchSpeed = pitchSpeed;
        lastPitchWasStrike = isStrike;

        if (isStrike)
            strikeCount++;
        else
            ballCount++;

        // 사운드 재생
        if (audioSource != null)
        {
            if (isStrike && strikeSound != null)
                audioSource.PlayOneShot(strikeSound);
            else if (!isStrike && ballSound != null)
                audioSource.PlayOneShot(ballSound);
        }

        UpdateAllUI();

        Debug.Log($"투구 결과: {(isStrike ? "스트라이크" : "볼")}, 속도: {pitchSpeed:F1} km/h");
    }

    public void ResetGame()
    {
        totalPitches = 0;
        strikeCount = 0;
        ballCount = 0;
        lastPitchSpeed = 0f;
        lastPitchWasStrike = false;

        for (int i = 0; i < pitchTypeUsage.Length; i++)
            pitchTypeUsage[i] = 0;

        UpdateAllUI();
        OnGameReset?.Invoke();

        // 사운드 재생
        if (audioSource != null && buttonClickSound != null)
            audioSource.PlayOneShot(buttonClickSound);

        Debug.Log("게임 리셋!");
    }

    public void ToggleUI()
    {
        bool isActive = pitchSelectionCanvas.gameObject.activeInHierarchy;
        pitchSelectionCanvas.gameObject.SetActive(!isActive);

        // 사운드 재생
        if (audioSource != null && buttonClickSound != null)
            audioSource.PlayOneShot(buttonClickSound);

        Debug.Log($"UI {(!isActive ? "표시" : "숨김")}");
    }

    private void UpdateButtonHighlights(PitchType selectedType)
    {
        for (int i = 0; i < pitchButtons.Length; i++)
        {
            if (pitchButtons[i] != null)
            {
                bool isSelected = pitchDataArray[i].pitchType == selectedType;

                // 선택된 버튼 강조
                Transform highlight = pitchButtons[i].transform.Find("Highlight");
                if (highlight != null)
                    highlight.gameObject.SetActive(isSelected);

                // 버튼 크기 조정
                pitchButtons[i].transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
            }
        }
    }

    public void RegisterBaseball(VRBaseball baseball)
    {
        currentBaseball = baseball;
        // 현재 선택된 구종을 야구공에 적용
        if (baseball != null)
            baseball.SetPitchType(currentSelectedPitch);
    }

    public void ShowUI()
    {
        if (pitchSelectionCanvas != null)
        {
            pitchSelectionCanvas.gameObject.SetActive(true);
            UpdateAllUI(); // UI 표시할 때 데이터 갱신
        }
    }

    public void HideUI()
    {
        if (pitchSelectionCanvas != null)
            pitchSelectionCanvas.gameObject.SetActive(false);
    }

    public PitchType GetCurrentSelectedPitch()
    {
        return currentSelectedPitch;
    }

    public PitchData GetCurrentPitchData()
    {
        return GetPitchData(currentSelectedPitch);
    }

    // 게임 통계 정보 반환
    public int GetTotalPitches() => totalPitches;
    public int GetStrikeCount() => strikeCount;
    public int GetBallCount() => ballCount;
    public float GetStrikeRate() => totalPitches > 0 ? (float)strikeCount / totalPitches * 100f : 0f;
    public int[] GetPitchTypeUsage() => pitchTypeUsage;
    public float GetLastPitchSpeed() => lastPitchSpeed;
    public bool GetLastPitchResult() => lastPitchWasStrike;

    private PitchData GetPitchData(PitchType pitchType)
    {
        for (int i = 0; i < pitchDataArray.Length; i++)
        {
            if (pitchDataArray[i].pitchType == pitchType)
                return pitchDataArray[i];
        }
        return pitchDataArray[0]; // 기본값
    }

    // 키보드 단축키 (에디터 테스트용)
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectPitch(PitchType.FastBall);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectPitch(PitchType.Curve);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectPitch(PitchType.Slider);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectPitch(PitchType.ForkBall);
#endif
    }
}
