using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Serialization;

/// 그냥 단순한 VR 피쳐 매니저. Gameplay PitcherScene에서 필요하다.
//pitcherManager ---(SO)---> PitchingBallController
//               ---(SO)---> UI
public class PitchingManager : MonoBehaviour
{
    [Header("게임 오브젝트 참조")]
    public PitchSelectionUI pitchSelectionUI; // 구종 선택 UI
    [SerializeField] private Baseball ball;
    [SerializeField] private TextMeshProUGUI _velocityText;
    
    
    [Header("게임 설정")]
    [SerializeField] private Transform ballResetPosition; 
    public int maxBalls = 10;               // 최대 공 개수 (5에서 10으로 증가)
    public float ballResetDelay = 3.0f;     // 공 리셋 딜레이 (착지 후 3초간 보여줌)

    private int ballsThrown = 0;
    private List<GameObject> thrownBalls = new List<GameObject>();  // 던진 공들 관리

    // 통계
    private int strikes = 0;
    private int balls = 0;

    [Header("broadcasting on Events")]
    public System.Action<int, int> OnCountChanged; // strikes, balls
    public System.Action<bool> OnPitchResult;// isStrike  
    [SerializeField] private FloatEventSO _getVelocityEventSO;
    
    [Header("Listening on Events")]
    [SerializeField] private IntEventSO playAudioClipEvent;

    #region EventFunction

    private void OnEnable()
    {
        pitchSelectionUI.OnPitchSelected += OnPitchTypeSelected;
        _getVelocityEventSO.onEventRaised += SetVelocityUI;
    }

    private void OnDisable()
    {
        pitchSelectionUI.OnPitchSelected -= OnPitchTypeSelected;
        _getVelocityEventSO.onEventRaised -= SetVelocityUI;
    }

    #endregion

    //pitch start
    public void StartPitchingGame()
    {
        ballsThrown = 0; // 기존 공은 카운트하지 않음
        ResetBall();
        
        // UI 초기화
        if (pitchSelectionUI != null)
        {
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
        ball.RemovePlayer();
        ball.IsBatTouch = false;
        ball.IsGroundBall = false;

        // XR Grab Interactable 강제 활성화 (새 공이 잡힐 수 있도록)
        //ball.OffBallPhysics();

        // UI에 공 등록 ★ => 굳이,,,?
        if (pitchSelectionUI != null)
            pitchSelectionUI.RegisterBaseball(ball);

        ballsThrown++;

        // init ball
        Vector3 finalPosition = ballResetPosition.position;
        transform.position = finalPosition;
    }
    
    private void OnPitchTypeSelected(PitchType pitchType)
    {
        if (ball != null)
            ball.SetPitchType(pitchType);
    }

    private void ResetCount()
    {
        strikes = 0;
        balls = 0;
        OnCountChanged?.Invoke(strikes, balls);
    }

    public void ResetGame()
    {
        // 현재 공 제거
        if (ball != null)
            Destroy(ball.gameObject);

        // 통계 리셋
        ballsThrown = 0;
        ResetCount();

        // 새 게임 시작
        ResetBall();
    }

    public void ToggleUI()
    {
        if (pitchSelectionUI != null)
        {
            if (pitchSelectionUI.pitchSelectionCanvas.gameObject.activeInHierarchy)
                pitchSelectionUI.HideUI();
            else
                pitchSelectionUI.ShowUI();
        }
    }

    /// <summary>
    /// 공이 특정 구역에 착지했을 때 호출되는 메서드
    /// </summary>
    /// <param name="isStrike">스트라이크 여부</param>
    /// <param name="zoneName">구역 이름</param>
    public void OnBallResult(bool isStrike, string zoneName)
    {
        Debug.Log($"⚾ 공 결과 수신: {zoneName} - {(isStrike ? "Strike ⚾" : "Ball ❌")}");
        
        // 카운트 업데이트
        if (isStrike)
        {
            strikes++;
            //PlayAudio(strikeSound);
            Debug.Log($"⚾ Strike! 현재 카운트: {balls}-{strikes}");
        }
        else
        {
            balls++;
            //PlayAudio(ballSound);
            Debug.Log($"❌ Ball! 현재 카운트: {balls}-{strikes}");
        }
        
        // 이벤트 발생
        OnCountChanged?.Invoke(strikes, balls);
        OnPitchResult?.Invoke(isStrike);
        
        // 카운트 리셋 체크 (3볼 또는 3스트라이크)
        if (balls >= 3 || strikes >= 3)
        {
            Debug.Log($"🔄 카운트 리셋! (볼: {balls}, 스트라이크: {strikes})");
            ResetCount();
        }
    }

    void SetVelocityUI(float velocity)
    {
        velocity *= 3.6f;
        _velocityText.text = velocity.ToString();
    }

}