/// <summary>
/// 🎯 VR 투수 게임 메인 매니저 - 공 생성, 던지기, 카운트 관리
/// </summary>

using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;


//pitcherManager ---(SO)---> PitchingBallController
//               ---(SO)---> UI
public class PitchingManager : MonoBehaviour
{
    [Header("게임 오브젝트 참조")]
    public PitchSelectionUI pitchSelectionUI; // 구종 선택 UI
    [SerializeField] private Baseball ball;

    
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
    
    [Header("Listening on Events")]
    [SerializeField] private IntEventSO playAudioClipEvent;

    #region EventFunction

    private void OnEnable()
    {
        pitchSelectionUI.OnPitchSelected += OnPitchTypeSelected;
    }

    private void OnDisable()
    {
        pitchSelectionUI.OnPitchSelected -= OnPitchTypeSelected;
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
        
        // **한 프레임 뒤에 물리 설정 - VRBaseball Start() 후에 실행되도록!**
        //StartCoroutine(SetupBallAfterFrame());

        // XR Grab Interactable 강제 활성화 (새 공이 잡힐 수 있도록)
        ball.OffBallPhysics();

        // UI에 공 등록 ★ => 굳이,,,?
        if (pitchSelectionUI != null)
            pitchSelectionUI.RegisterBaseball(ball);

        ballsThrown++;

        // init ball
        Vector3 finalPosition = ballResetPosition.position;
        ball.ResetBall(finalPosition);
    }
    //
    private void SetupExistingBall()
    {
        if (ball == null) return;

        ball.OffBallPhysics();

        // XR Grab Interactable 강제 활성화 및 설정
        XRGrabInteractable grabComponent = ball.GetComponent<XRGrabInteractable>();
        if (grabComponent != null)
        {
            grabComponent.enabled = true;
            // 첫 번째 공은 제대로 동작하므로 기본 설정 유지
            // 씬에 있는 초기 공은 throwOnDetach가 올바르게 설정되어 있을 것임
        }


        // UI에 공 등록
        if (pitchSelectionUI != null)
            pitchSelectionUI.RegisterBaseball(ball);
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

    public Baseball GetCurrentBall() => ball;

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

}