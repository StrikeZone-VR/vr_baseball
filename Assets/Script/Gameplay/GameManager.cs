using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

//게임 시작할때 실행되는 GameManager
public class GameManager : MonoBehaviour
{
    [SerializeField] private VoidEventSO outEvent;
    [SerializeField] private VoidEventSO allTrackingOffEvent; //to baseball
    //0 1 => 1이닝 공격 수비, => 0~17 => 짝수면 원정, 홀수면 홈 
    private int inning = 0;

    private int ball_count = 0;
    private int strike_count = 0;
    private int out_count = 0;
    
    //1 2 3 포수
    
    
    [SerializeField] private Defender[] defenders;
    [SerializeField] private Transform[] bases;
    [SerializeField] private Baseball _ball;
    
    private bool [] isBaseStatus = { false, false, false };
    
    private TeamStatus []_teamStatus = new TeamStatus[2];

    //Define
    private const int MAX_BALL_COUNT = 4; 
    private const int MAX_STRIKE_COUNT = 3; 
    private const int MAX_OUT_COUNT = 3; 
    private const int MAX_INNING_COUNT = 18; 
    private const int MAX_BASE_COUNT = 3;

    private void OnEnable()
    {
        outEvent.onEventRaised += AddOut;
        allTrackingOffEvent.onEventRaised += AllTrackingOff;
    }
    private void OnDisable()
    {
        outEvent.onEventRaised -= AddOut;
        allTrackingOffEvent.onEventRaised -= AllTrackingOff;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
            DebugBase(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            DebugBase(1);
        else if(Input.GetKeyDown(KeyCode.Alpha3))
            DebugBase(2);
        else if(Input.GetKeyDown(KeyCode.Alpha4))
            DebugBase(3);

        if (Input.GetKeyDown(KeyCode.V))
        {
            _ball.RemovePlayer();
            AllTrackingOff();
            _ball.IsGroundBall = false;
            _ball.IsPassing = false;
            defenders[0].SetMyBall(_ball);
        }
        if (_ball.MyDefender)
        {
            return;
        }
        //tracking
        if (!_ball.IsPassing && _ball.IsGroundBall)
        {
            int index = FindClosestDefenderIndex();
            AllTrackingOff();
            //closestDefender set tracking
            defenders[FindClosestDefenderIndex()].IsTracking = true;
        }
    }

    //property
    public int OutCount
    {
        get
        {
            return out_count;
        }
        set
        {
            out_count = value;
            
            if (out_count >= MAX_OUT_COUNT)
            {
                out_count = 0;
                Inning++;
            }
        }
    }
    
    private void AddOut()
    {
        OutCount++;
    }

    public int Inning
    {
        get
        {
            return inning;
        }
        set
        {
            inning = value;
            
            if (inning >= MAX_INNING_COUNT)
            {
                //게임 종료
            }
        }
    }

    public int Strike
    {
        get
        {
            return strike_count;
        }
        set
        {
            strike_count = value;
            if (strike_count >= MAX_STRIKE_COUNT)
            {
                strike_count = 0;
                OutCount++;
            }
        }
    }

    public int BallCount
    {
        get
        {
            return ball_count;
        }
        set
        {
            ball_count = value;
            if (ball_count >= MAX_BALL_COUNT)
            {
                ball_count = 0;
                AddBaseStatus();
                
            }
        }
    }

    public void AddBaseStatus()
    {
        int i;
        for (i = 0; i < MAX_BASE_COUNT; i++)
        {
            if (!isBaseStatus[i])
            {
                isBaseStatus[i] = true;
                return;
            }
            //밀어내기
        }

        //밀어내기 득점
        if (i == MAX_BASE_COUNT)
        {
            _teamStatus[inning % 2].score++;
        }

    }

    public void DebugBase(int index)
    {
        if(_ball.MyDefender)
            _ball.MyDefender.ThrowBall(bases[index].position + new Vector3(0,0.5f,0));
    }

    public int FindClosestDefenderIndex()
    {
        float min = float.MaxValue;
        int index = -1;
        for (int i = 0; i < defenders.Length; i++)
        {
            float dis = GetDistanceBetween(_ball.transform.position, defenders[i].transform.position);
            if (min > dis)
            {
                min = dis;
                index = i;
            }
        }

        return index;
    }


    private float GetDistanceBetween(Vector3 a, Vector3 b)
    {
        float result = Vector3.Distance(a, b);
        return result;
    }

    private void AllTrackingOff()
    {
        for (int i = 0; i < defenders.Length; i++)
        {
            defenders[FindClosestDefenderIndex()].IsTracking = false;
        }
    }
}

struct TeamStatus
{
    public int score;
    
    //타순 0 ~ 8
    public int batting_order;

    //Define
    private const int MAX_BATTING_ORDER = 9; 

    public int BattingOrder
    {
        get => batting_order;
        set
        {
            batting_order = value;
            if (batting_order >= MAX_BATTING_ORDER)
            {
                batting_order = 0;
            }
        }
    }

}