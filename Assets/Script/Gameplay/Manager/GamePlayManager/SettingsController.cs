using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SettingsController : MonoBehaviour
{
    [Header("UI 패널")]
    [SerializeField] private GameObject settingsPanel;

    //메뉴로 되돌아가기
    //- [ ] 소리
    //- [ ] 언어
    ///- [ ] 컨트롤러 진동 세기
    [Header("인풋 액션 연결")]
    
    //Input Action Asset의 하위 버전 : XRI : UI / Click 이런거
    [SerializeField] private InputActionReference toggleMenuAction;

        //canceled
        //performed
    private void OnEnable()
    {
        toggleMenuAction.action.Enable();
        toggleMenuAction.action.started += TogglePanel;
        //started
    }

    private void OnDisable()
    {
        toggleMenuAction.action.started -= TogglePanel;
        toggleMenuAction.action.Disable();
    }

    private void TogglePanel(InputAction.CallbackContext context)
    {
        // 패널 껐다 켜기
        settingsPanel.SetActive(!settingsPanel.activeSelf);

        if (settingsPanel.activeSelf)
        {
            Debug.Log("환경설정 창 켜짐!");
        }
    }
}
