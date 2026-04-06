using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//---(SO)---> PitchingBallController
//               ---(SO)---> UI
public class PitchingController : GameController
{
    [SerializeField] PitchSelectionUI pitchSelectionUI; // 구종 선택 UI
    [SerializeField] private Baseball _ball;
    
    [Header("게임 설정")]
    [SerializeField] private Transform ballResetPosition;

    private void OnEnable()
    {
        pitchSelectionUI.OnPitchSelected += OnPitchTypeSelected;
    }

    private void OnDisable()
    {
        pitchSelectionUI.OnPitchSelected -= OnPitchTypeSelected;
    }
    
    //pitch start
    public void StartPitchingGame()
    {
        ResetBall();
        
        // UI 초기화
        if (pitchSelectionUI != null)
        {            
            pitchSelectionUI.RegisterBaseball(_ball);
            pitchSelectionUI.ShowUI();
        }

        // 게임 시작 사운드
        //playAudioClipEvent.RaiseEvent(2);
    }

    public void EndPitchingGame()
    {
        pitchSelectionUI.HideUI();
    }

    /// <summary>
    /// init, ball status init 
    /// </summary>
    public void ResetBall()
    {
        _ball.RemoveDefender();

        //맞겠지?
        // XR Grab Interactable 강제 활성화 (새 공이 잡힐 수 있도록)
        //ball.OffBallPhysics();

        Debug.Log("설마 이게 계속 메세지가 나온다고?");
        // init ball
        _ball.SetVelocity(Vector3.zero);
        _ball.SetPosition(ballResetPosition.position);
    }
    
    private void OnPitchTypeSelected(PitchType pitchType)
    {
        if (_ball != null)
            _ball.SetPitchType(pitchType);
    }
    public void SetVelocityUI(float velocity)
    {
        pitchSelectionUI.SetBallVelocityUI(velocity);
    }

    public void SetStrike(int value)
    {
        pitchSelectionUI.SetStrikeUI(value);
    }
    public void SetBallCount(int value)
    {
        pitchSelectionUI.SetBallCountUI(value);
    }
}
