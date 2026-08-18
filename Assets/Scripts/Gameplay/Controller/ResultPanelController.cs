using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ResultPanelController : MonoBehaviour
{
    [SerializeField] private BattingModel battingModel;
    [Space]
    [Header("UI Text Elements")] 
    // 유니티 인스펙터에서 텍스트 UI들을 드래그해서 연결할 변수들입니다.
    [SerializeField] private TextMeshProUGUI hitText;
    [SerializeField] private TextMeshProUGUI homerunText;
    [SerializeField] private TextMeshProUGUI foulText;
    [SerializeField] private TextMeshProUGUI groundBallText;
    [Tooltip("요약 카드의 타율. BattingModel에 없는 값이라 여기서 계산한다.")]
    [SerializeField] private TextMeshProUGUI averageText;
    [Tooltip("요약 카드의 총 타격 수(안타+홈런+파울+땅볼).")]
    [SerializeField] private TextMeshProUGUI totalContactText;
    
    [Header("Scenes")] 
    [SerializeField] private AssetReference gameMenu;
    [SerializeField] private AssetReference gameReadyScene;
    
    [Space]
    [Header("Listening to EventChannels")] 
    [SerializeField] private SceneEventSO sceneEvent;

    /// <summary>
    /// BattingModel에서 데이터를 가져와 UI 텍스트에 적용하는 함수
    /// </summary>
    public void UpdateResultUI()
    {
        // 모델이 연결되어 있지 않으면 에러를 방지하기 위해 리턴
        if (battingModel == null) 
        {
            Debug.LogWarning("BattingModel이 연결되어 있지 않습니다!");
            return;
        }

        //항목 이름은 카드 UI가 따로 들고 있으므로 여기서는 숫자만 넣는다
        if (hitText != null)
            hitText.text = $"{battingModel.Hit}";

        if (homerunText != null)
            homerunText.text = $"{battingModel.Homerun}";

        if (foulText != null)
            foulText.text = $"{battingModel.Foul}";

        if (groundBallText != null)
            groundBallText.text = $"{battingModel.GroundBall}";

        UpdateSummary();
    }

    /// <summary>
    /// 요약 카드(타율 / 총 타격). BattingModel에는 없는 파생값이라 여기서 만든다.
    /// ㄴ 타수에서 파울을 빼는 건 야구 규칙과 같다. 파울은 타석을 끝내지 않는다.
    /// ㄴ "총 스윙"이 아니라 "총 타격"인 이유: 헛스윙은 어디에도 기록되지 않아서 셀 수가 없다.
    /// </summary>
    private void UpdateSummary()
    {
        int hits = battingModel.Hit + battingModel.Homerun;
        int atBats = hits + battingModel.GroundBall;
        int contacts = atBats + battingModel.Foul;

        if (averageText != null)
        {
            float average = atBats > 0 ? (float)hits / atBats : 0f;
            averageText.text = average.ToString("#.000");
        }

        if (totalContactText != null)
            totalContactText.text = $"{contacts}";
    }
    
    public void OnGameMenu()
    {
        sceneEvent.RaiseEvent(gameMenu);
    }
    public void OnGameReady()
    {
        sceneEvent.RaiseEvent(gameReadyScene);
    }
    
}
