/// <summary>
/// 🎯 투수 연습 시스템 통합 관리자 - 스트라이크존 9개 + 볼존 16개 (25존 시스템)
/// </summary>

using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Collections;
using TMPro;
/// <summary>
/// pitcher mode. 되도록 많은 부분을 없애면 된다.
/// </summary>
public class PitchingModeManager : MonoBehaviour
{
    [SerializeField] private Batter batter;
    [SerializeField] private Baseball baseball;

    private int strike = 0;
    private int ball_count = 0;
    
    [SerializeField] private PitchingManager pitchingManager;
    [SerializeField] private PitchSelectionUI pitchSelectionUI;

    private Transform ballZoneParent;
    
    // 스트라이크존 중심점과 크기 (기존 9개 영역 기준)
    private Vector3 strikeZoneCenter;
    private Vector3 strikeZoneBounds;
    
    [Header("Events")] 
    [SerializeField] private VoidEventSO strikeEventSO;
    [SerializeField] private VoidEventSO ballEventSO;
    
    [SerializeField] private VoidEventSO backToPitcherEvent; //baseball
    [SerializeField] private VoidEventSO pitchEvent;
    [SerializeField] private Vector3EventSO moveOriginEvent;
    [SerializeField] private Vector3EventSO rotateOriginEvent;

    const float WAIT_TIME = 2.0f; //원래 7임
    
    private void OnEnable()
    {
        ballEventSO.onEventRaised += AddBallCount;
        strikeEventSO.onEventRaised += AddStrike;
        
        backToPitcherEvent.onEventRaised += BackPitcherBall;
        pitchEvent.onEventRaised += WaitingSwing;
    }

    private void OnDisable()
    {
        ballEventSO.onEventRaised -= AddBallCount;
        strikeEventSO.onEventRaised -= AddStrike;
        
        backToPitcherEvent.onEventRaised -= BackPitcherBall;
        pitchEvent.onEventRaised -= WaitingSwing;
    }

    void Start()
    {       
        moveOriginEvent.RaiseEvent(new Vector3(0.6f, 1.3f, -0.98f));
        rotateOriginEvent.RaiseEvent(new Vector3(0, -135f, 0));

        Strike = 0;
        BallCount = 0;
        InitializeSystem();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            pitchingManager.ResetBall();
        }
    }
    
    /// <summary>
    /// 시스템 초기화
    /// </summary>
    public void InitializeSystem()
    {
        pitchingManager.StartPitchingGame();
    }
    
    private void BackPitcherBall()
    {
        StartCoroutine(WaitingBackToPitcher());
    }
    
    IEnumerator WaitingBackToPitcher()
    {
        yield return new WaitForSeconds(WAIT_TIME);
        pitchingManager.ResetBall();

    }

    private void WaitingSwing()
    {
        Debug.Log("타자 필요 없을지도?");
        //StartCoroutine(StartSwing());
    }


    private IEnumerator StartSwing()
    {
        yield return new WaitForSeconds(1.5f);
        batter.StartSwing();
        
    }

    private void AddBallCount()
    {
        BallCount++;
    }
    
    private void AddStrike()
    {
        Strike++;
    }

    public int Strike
    {
        get => strike;
        set
        {
            strike = value;
            pitchSelectionUI.SetStrikeUI(strike);
        }
    }
    public int BallCount
    {
        get => ball_count;
        set
        {
            ball_count = value;
            pitchSelectionUI.SetBallCountUI(ball_count);
        }
    }
}
