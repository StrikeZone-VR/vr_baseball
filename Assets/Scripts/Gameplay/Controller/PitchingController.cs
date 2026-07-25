using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//pitcher에 관련된 UI 총괄 컨트롤러
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
        PlayerPitcherResetBall();
        
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
    public void PlayerPitcherResetBall()
    {
        _ball.CurrentState = BallState.Idle;
        _ball.ResetBallState(ballResetPosition.position);
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

    public void SetTotalBallStatus(int total, int strike)
    {
        pitchSelectionUI.UpdateStatisticsDisplay(total, strike);
    }

    //최근 5구 결과 슬라이딩 윈도우에 새 결과 추가
    public void AddPitchResultIndicator(bool isStrike)
    {
        pitchSelectionUI.AddPitchResultIndicator(isStrike);
    }
}
