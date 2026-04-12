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

        if (hitText != null) 
            hitText.text = $"안타: {battingModel.Hit}";
            
        if (homerunText != null) 
            homerunText.text = $"홈런: {battingModel.Homerun}";
            
        if (foulText != null) 
            foulText.text = $"파울: {battingModel.Foul}";
            
        if (groundBallText != null) 
            groundBallText.text = $"땅볼: {battingModel.GroundBall}";
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
