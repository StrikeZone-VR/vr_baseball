using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GamePlayManager : GameManager
{
    [Header("UI")]
    [SerializeField] private UIGameStatus[] _UIGameStatusElements;
    [SerializeField] private TextMeshProUGUI[] _scoreTexts;
    [SerializeField] private TextMeshProUGUI _inningText;
    
    [Header("Objects")]
    [SerializeField] private Defender[] defenders; // pitcher => 0
    [SerializeField] private Transform[] bases;
    

    [Header("Batter")] 
    [SerializeField] private Batter batterPrefab;
    [SerializeField] private Transform batterCreatePosition;
    [SerializeField] private Transform batterPosition;
    [SerializeField] private Batter currentBatter;
    [SerializeField] private GameObject _bat;
    
    [Header("Broadcasting on EventChannels")] 
    [SerializeField] private IntEventSO outBatterEvent; //Defender, Baseman
    
    [Space]
    [SerializeField] private VoidEventSO allTrackingOffEvent; //to baseball
    [SerializeField] private VoidEventSO addScore; //to Batter
    [SerializeField] private IntEventSO addIsBaseStatus; //to Batter
    [SerializeField] private VoidEventSO runSignalEvent;
    
    private int beforeScore = 0;
    private bool [] isBeforeBaseStatus = { false, false, false };
    
    private void OnEnable()
    {
        outBatterEvent.onEventRaised += OutBatter;

        allTrackingOffEvent.onEventRaised += AllTrackingOff;
        addScore.onEventRaised += AddScore;
        runSignalEvent.onEventRaised += RunRunner;

        addIsBaseStatus.onEventRaised += AddIsBaseStatus;

    }

    private void OnDisable()
    {
        outBatterEvent.onEventRaised -= OutBatter;

        allTrackingOffEvent.onEventRaised -= AllTrackingOff;
        addScore.onEventRaised -= AddScore;
        runSignalEvent.onEventRaised -= RunRunner;

        addIsBaseStatus.onEventRaised -= AddIsBaseStatus;
    }

    private void Update()
    {
        //debug
        //to pitcher
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            defenders[0].SetMyBall(_ball);
            //_ball.MyDefender.ThrowBall(defenders[0].transform.position);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
            ThrowToBase(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            ThrowToBase(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            ThrowToBase(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            ThrowToBase(3);

        //has ball and ball batting
        if (_ball.MyDefender && _ball.IsBatTouch)
        {
            //던질 곳 없으면 복귀
            if (!ThrowBallAlgorithm())
            {
                PitcherGetBall();

                //투수일 경우 + currentBatter가 null인 경우
                if (!currentBatter && inning % 2 == 1)
                {
                    CreateBatter();
                }
            }
        }


        if (Input.GetKeyDown(KeyCode.B))
        {
            //_ball.OnTouchBall();
            PitcherGetBall();
        }

        //batter run
        if (Input.GetKeyDown(KeyCode.C))
        {
            MoveOneBase();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Inning++;
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            DebugBaseStatus();
        }

        if (_ball.MyDefender)
        {
            return;
        }

        //tracking => 혹시 포수가 못 잡을 수 있으니 isBatTouch는 넣지말자
        if (!_ball.IsPassing && _ball.IsGroundBall && !_ball.IsThrown)
        {
            int index = FindClosestDefenderIndex();
            AllTrackingOff();
            //closestDefender set tracking
            if (index == -1)
            {
                return;
            }
            
            //Debug.Log("트래킹 잠시 무효화");
            defenders[index].IsTracking = true;
        }
    }

}
