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
    [SerializeField] private Color defaultPitchColor; 
    [SerializeField] private Color selectedPitchColor; 

    [Header("실시간 투구 결과 패널")]
    public TextMeshProUGUI strikeCountText;      // "스트라이크: 2"
    public TextMeshProUGUI ballCountText;        // "볼: 1"  
    public TextMeshProUGUI lastPitchSpeedText;   // "투구 속도: 145 km/h"
    [SerializeField] private Image [] currentPitchImages; //5개

    [Header("투구 통계 표시")]
    public TextMeshProUGUI totalPitchesText;     // "총 투구: 15"
    public TextMeshProUGUI strikeRateText;       // "73%"

    [Header("게임 컨트롤")]
    public Button resetGameButton;               // 게임 리셋 버튼

    [Header("구종 설정")]
    public PitchData[] pitchDataArray = new PitchData[4];

    private PitchType currentSelectedPitch = PitchType.FastBall;
    private Baseball currentBaseball;

    public System.Action<PitchType> OnPitchSelected;

    [Header("Listenin to Events")]
    [SerializeField] private SceneEventSO backMenuSceneEvent;
    [SerializeField] private IntEventSO playAudioClipEvent;
    
    
    void Start()
    {
        InitializePitchData();
        SetupUI();
        SetupGameControls();
        SelectPitch(PitchType.FastBall); // 기본 선택
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
                PitchType pitchType = pitchDataArray[i].pitchType;

                // 버튼 클릭 이벤트
                pitchButtons[i].onClick.AddListener(() => SelectPitch(pitchType));

                // 버튼 색상 설정
                if (pitchButtonImages[i] != null)
                    pitchButtonImages[i].color = defaultPitchColor;
                
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
        PitchData selectedData = GetPitchData(pitchType);

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
                bool isSelected = pitchDataArray[i].pitchType == selectedType;

                // 선택된 버튼 강조
                Transform highlight = pitchButtons[i].transform.Find("Highlight");
                if (highlight != null)
                    highlight.gameObject.SetActive(isSelected);

                // 버튼 크기 조정
                pitchButtons[i].transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
                
                //색 변경
                pitchButtonImages[i].color = isSelected ? selectedPitchColor : defaultPitchColor; 
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

    public PitchData GetCurrentPitchData()
    {
        return GetPitchData(currentSelectedPitch);
    }

    private PitchData GetPitchData(PitchType pitchType)
    {
        for (int i = 0; i < pitchDataArray.Length; i++)
        {
            if (pitchDataArray[i].pitchType == pitchType)
                return pitchDataArray[i];
        }
        return pitchDataArray[0]; // 기본값
    }

}
