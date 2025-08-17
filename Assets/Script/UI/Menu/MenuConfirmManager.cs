using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;

public class MenuConfirmManager : MonoBehaviour
{
    [Header("버튼들")]
    [SerializeField] private Button oneOnOneMatchButton;
    [SerializeField] private Button pitchingPracticeButton;
    [SerializeField] private Button hittingPracticeButton;
    [SerializeField] private Button exitButton;
    [Space]

    
    [Header("확인 대화상자")]
    [SerializeField] private GameObject confirmationDialogPanel;
    [SerializeField] private TextMeshProUGUI confirmationMessageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [Space]

    
    [Header("Listening to Event")]
    [SerializeField] private SceneEventSO sceneEventSO;
    
    [Space]
    [Header("Scenes")]
    [SerializeField] private AssetReference gameScene;

    
    private System.Action currentConfirmAction;

    void Start()
    {
        // 버튼 클릭 이벤트 설정
        SetupButtonEvents();

        // 확인 대화상자 버튼 이벤트 설정
        SetupDialogEvents();

        // 대화상자 초기 상태는 비활성화
        if (confirmationDialogPanel != null)
            confirmationDialogPanel.SetActive(false);
    }

    void SetupButtonEvents()
    {
        if (oneOnOneMatchButton != null)
            oneOnOneMatchButton.onClick.AddListener(() => ShowConfirmation("1:1 매치를 시작하시겠습니까?", OnOneOnOneMatch));

        if (pitchingPracticeButton != null)
            pitchingPracticeButton.onClick.AddListener(() => ShowConfirmation("투수 연습을 시작하시겠습니까?", OnPitchingPractice));

        if (hittingPracticeButton != null)
            hittingPracticeButton.onClick.AddListener(() => ShowConfirmation("타자 연습을 시작하시겠습니까?", OnHittingPractice));

        if (exitButton != null)
            exitButton.onClick.AddListener(() => ShowConfirmation("게임을 종료하시겠습니까?", OnExit));
    }

    void SetupDialogEvents()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
    }

    void ShowConfirmation(string message, System.Action onConfirmed)
    {
        if (confirmationMessageText != null)
            confirmationMessageText.text = message;

        currentConfirmAction = onConfirmed;

        if (confirmationDialogPanel != null)
            confirmationDialogPanel.SetActive(true);
    }

    void OnConfirmClicked()
    {
        currentConfirmAction?.Invoke();
        HideConfirmation();
    }

    void OnCancelClicked()
    {
        HideConfirmation();
    }

    void HideConfirmation()
    {
        if (confirmationDialogPanel != null)
            confirmationDialogPanel.SetActive(false);

        currentConfirmAction = null;
    }

    // 각 버튼의 확인 후 실행될 메서드들
    void OnOneOnOneMatch()
    {
        Debug.Log("1:1 매치 시작!");
        sceneEventSO.RaiseEvent(gameScene);
        // TODO: 1:1 매치 씬 로드
    }

    void OnPitchingPractice()
    {
        Debug.Log("투수 연습 시작!");
        // TODO: 투수 연습 씬 로드
    }

    void OnHittingPractice()
    {
        Debug.Log("타자 연습 시작!");
        // TODO: 타자 연습 씬 로드
    }

    void OnExit()
    {
        Debug.Log("게임 종료!");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
