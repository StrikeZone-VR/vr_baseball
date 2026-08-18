/// <summary>
/// 🎨 구종 선택 UI 관리자 - 직구, 커브, 슬라이더, 포크볼 선택 인터페이스
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.AddressableAssets;

public class PitchSelectionUI : MonoBehaviour
{
    [SerializeField] private AssetReference menuScene;
    [Header("UI 구성요소")]
    public Canvas pitchSelectionCanvas;
    public Button[] pitchButtons = new Button[4];
    public Image[] pitchButtonImages; 
    [SerializeField] private Color defaultPitchColor = new Color(1f, 1f, 1f, 0.051f);
    [SerializeField] private Color selectedPitchColor = new Color(0.239f, 0.855f, 0.851f, 0.149f);

    [SerializeField] private Color defaultTextColor = new Color(0.9176f, 0.9098f, 0.9686f, 1f);
    [SerializeField] private Color selectedTextColor = new Color(0.2392f, 0.8588f, 0.851f, 1f);

    //버튼별 라벨 캐시. 인스펙터 배열을 또 늘리면 채우는 걸 잊기 쉬워서 SetupUI에서 자식으로부터 1회 수집한다.
    private TextMeshProUGUI[] pitchButtonTexts;

    [Header("실시간 투구 결과 패널")]
    public TextMeshProUGUI strikeCountText;      // "스트라이크: 2"
    public TextMeshProUGUI ballCountText;        // "볼: 1"
    public TextMeshProUGUI lastPitchSpeedText;   // "투구 속도: 145 km/h"
    [SerializeField] private Image [] currentPitchImages; //5개
    [SerializeField] private Color strikeIndicatorColor ;       // 스트라이크 표시 색
    [SerializeField] private Color ballIndicatorColor ;       // 볼 표시 색
    [SerializeField] private Color emptyIndicatorColor = new Color(1f, 1f, 1f, 0.15f); // 빈 슬롯 색

    // 최근 투구 결과 슬라이딩 윈도우: 1~5구는 순서대로 채우고, 6구부터는 한 칸씩 왼쪽으로 밀고 마지막에 새 결과 삽입
    private int currentPitchCount = 0;

    [Header("투구 통계 표시")]
    public TextMeshProUGUI totalPitchesText;     // "총 투구: 15"
    public TextMeshProUGUI strikeRateText;       // "73%"

    [Header("게임 컨트롤")]
    public Button resetGameButton;               // 게임 리셋 버튼

    [Header("구종 설정")]
    [SerializeField] private PitchDataRegistry pitchDataRegistry;

    private PitchType currentSelectedPitch = PitchType.FastBall;
    private Baseball currentBaseball;

    public System.Action<PitchType> OnPitchSelected;

    [Header("Listenin to Events")]
    [SerializeField] private SceneEventSO backMenuSceneEvent;
    [SerializeField] private IntEventSO playAudioClipEvent;
    
    
    void Start()
    {
        SetupUI();
        SetupGameControls();
        SelectPitch(PitchType.FastBall); // 기본 선택
    }

    private void SetupUI()
    {
        pitchButtonTexts = new TextMeshProUGUI[pitchButtons.Length];

        // 버튼 인덱스 → PitchType 매핑 (enum 선언 순서: FastBall=0, Curve=1, Slider=2, ForkBall=3)
        for (int i = 0; i < pitchButtons.Length; i++)
        {
            if (pitchButtons[i] != null)
            {
                PitchType pitchType = (PitchType)i;

                // 버튼 클릭 이벤트
                pitchButtons[i].onClick.AddListener(() => SelectPitch(pitchType));

                // 버튼 색상 설정
                if (pitchButtonImages[i] != null)
                    pitchButtonImages[i].color = defaultPitchColor;

                // 버튼 라벨 캐시 (자식의 Text (TMP)). 비활성 자식도 잡히도록 includeInactive
                pitchButtonTexts[i] = pitchButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);

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
    }

    private void InitAllUI()
    {
        SetBallCountUI(0);
        SetStrikeUI(0);
        SetBallVelocityUI(0);
        UpdateStatisticsDisplay(0,0);
        ResetPitchResultIndicators();
    }

    /// <summary>
    /// 새 투구 결과를 슬라이딩 윈도우에 추가.
    /// 1~5구: 다음 빈 슬롯을 채움. 6구 이상: 모두 한 칸씩 왼쪽으로 밀고 마지막 슬롯에 새 결과.
    /// </summary>
    public void AddPitchResultIndicator(bool isStrike)
    {
        if (currentPitchImages == null || currentPitchImages.Length == 0) return;

        int capacity = currentPitchImages.Length;
        Color newColor = isStrike ? strikeIndicatorColor : ballIndicatorColor;

        if (currentPitchCount < capacity)
        {
            // 처음 N구: 빈 슬롯에 채워넣기
            if (currentPitchImages[currentPitchCount] != null)
                currentPitchImages[currentPitchCount].color = newColor;
            currentPitchCount++;
        }
        else
        {
            // 풀(full) 상태: 왼쪽으로 시프트 (idx 0 이 가장 오래된 결과 → 버려짐)
            for (int i = 0; i < capacity - 1; i++)
            {
                if (currentPitchImages[i] != null && currentPitchImages[i + 1] != null)
                    currentPitchImages[i].color = currentPitchImages[i + 1].color;
            }
            // 가장 오른쪽 슬롯에 새 결과
            if (currentPitchImages[capacity - 1] != null)
                currentPitchImages[capacity - 1].color = newColor;
        }
    }

    /// <summary>
    /// 모든 슬롯을 비어있음 상태(반투명)로 리셋.
    /// </summary>
    public void ResetPitchResultIndicators()
    {
        currentPitchCount = 0;
        if (currentPitchImages == null) return;
        for (int i = 0; i < currentPitchImages.Length; i++)
        {
            if (currentPitchImages[i] != null)
                currentPitchImages[i].color = emptyIndicatorColor;
        }
    }

    public void SetBallCountUI(float ballCount)
    {
        ballCountText.text = ballCount.ToString();
    }
    public void SetStrikeUI(int strike)
    {
        //playAudioClipEvent.RaiseEvent(3);
        strikeCountText.text = strike.ToString();
    }
    public void SetBallVelocityUI(float velocity)
    {
        lastPitchSpeedText.text = "투구 속도 : " + velocity.ToString("F2") + "km/h";
    }

    //총 갯수와 스트라이크율
    public void UpdateStatisticsDisplay(float totalPitches, float strikeCount)
    {
        if (totalPitchesText != null)
            totalPitchesText.text = $"{totalPitches}";

        //스트라이크율
        float strikeRate = totalPitches > 0 ? (float)(totalPitches > 0 ? strikeCount : 0) / totalPitches * 100f : 0f;
        if (strikeRateText != null)
            strikeRateText.text = $"{strikeRate:F1}%";
        
    }


    public void SelectPitch(PitchType pitchType)
    {
        currentSelectedPitch = pitchType;
        PitchTypeSO selectedData = GetPitchData(pitchType);

        // 버튼 하이라이트 업데이트
        UpdateButtonHighlights(pitchType);

        // 현재 야구공에 구종 적용
        if (currentBaseball != null)
            currentBaseball.SetPitchType(pitchType);

        OnPitchSelected?.Invoke(pitchType);

        //Debug.Log($"구종 선택: {selectedData.pitchName}");
    }


    //reset 
    public void ResetGame()
    {
        playAudioClipEvent.RaiseEvent(0); //play click sound

        //totalPitches = 0;
        //strikeCount = 0;
        //ballCount = 0;
        //lastPitchWasStrike = false;

        //for (int i = 0; i < pitchTypeUsage.Length; i++)
        //    pitchTypeUsage[i] = 0;

        //UpdateAllUI();
        //OnGameReset?.Invoke();
        backMenuSceneEvent.RaiseEvent(menuScene);
        

        // 사운드 재생
        //if (audioSource != null && buttonClickSound != null)
        //    audioSource.PlayOneShot(buttonClickSound);

    }

    public void ToggleUI()
    {
        bool isActive = pitchSelectionCanvas.gameObject.activeInHierarchy;
        pitchSelectionCanvas.gameObject.SetActive(!isActive);

        playAudioClipEvent.RaiseEvent(0);

        Debug.Log($"UI {(!isActive ? "표시" : "숨김")}");
    }

    private void UpdateButtonHighlights(PitchType selectedType)
    {
        for (int i = 0; i < pitchButtons.Length; i++)
        {
            if (pitchButtons[i] != null)
            {
                bool isSelected = (PitchType)i == selectedType;

                // 선택된 버튼 강조
                Transform highlight = pitchButtons[i].transform.Find("Highlight");
                if (highlight != null)
                    highlight.gameObject.SetActive(isSelected);

                // 버튼 크기 조정
                pitchButtons[i].transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;

                //색 변경
                //ㄴ 인스펙터 배열 길이가 어긋나도 예외로 루프가 끊기지 않도록 범위/널 확인
                if (pitchButtonImages != null && i < pitchButtonImages.Length && pitchButtonImages[i] != null)
                    pitchButtonImages[i].color = isSelected ? selectedPitchColor : defaultPitchColor;

                //라벨 색 변경. 배경은 알파가 낮아 틴트가 옅으니 텍스트까지 같이 전환해야 선택이 보인다
                if (pitchButtonTexts != null && i < pitchButtonTexts.Length && pitchButtonTexts[i] != null)
                    pitchButtonTexts[i].color = isSelected ? selectedTextColor : defaultTextColor;
            }
        }
    }

    public void RegisterBaseball(Baseball baseball)
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
            InitAllUI(); // UI 표시할 때 데이터 갱신
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

    public PitchTypeSO GetCurrentPitchData()
    {
        return GetPitchData(currentSelectedPitch);
    }

    private PitchTypeSO GetPitchData(PitchType pitchType)
    {
        if (pitchDataRegistry == null)
        {
            Debug.LogError("[PitchSelectionUI] pitchDataRegistry가 비어있음. 인스펙터 확인 요망.");
            return null;
        }
        return pitchDataRegistry.Get(pitchType);
    }

}
