using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

//게임 시작할때 실행되는 GameManager
public class GameManager : MonoBehaviour
{
    //0 1 => 1이닝 공격 수비, => 0~17 => 짝수면 원정, 홀수면 홈 
    private int inning = 0;

    private int ball_count = 0;
    private int strike_count = 0;
    private int out_count = 0;
    
    [SerializeField] private UIGameStatus[] _UIGameStatusElements;
    [SerializeField] private TextMeshProUGUI [] _scoreTexts ;
    [SerializeField] private TextMeshProUGUI _inningText ;
    
    [SerializeField] private Defender[] defenders;
    [SerializeField] private Transform[] bases;
    [SerializeField] private Baseball _ball;
    
    private int isBaseStatus = 0; //bit mask
    [SerializeField]private Batter[] runners = new Batter[4]; // 빠따든 주자는 [0]
    
    private TeamStatus []_teamStatus = new TeamStatus[2];

    [Header("Broadcasting on EventChannels")]
    [SerializeField] private IntEventSO outBatterEvent; //Defender, Baseman
    [SerializeField] private VoidEventSO allTrackingOffEvent; //to baseball
    [SerializeField] private VoidEventSO addScore; //to Batter
    
    //Define
    private const int MAX_BALL_COUNT = 4; 
    private const int MAX_STRIKE_COUNT = 3; 
    private const int MAX_OUT_COUNT = 3; 
    private const int MAX_INNING_COUNT = 18; 
    private const int MAX_BASE_COUNT = 3;

    private void OnEnable()
    {
        outBatterEvent.onEventRaised += OutBatter;
        allTrackingOffEvent.onEventRaised += AllTrackingOff;
        addScore.onEventRaised += AddScore;
    }
    private void OnDisable()
    {
        outBatterEvent.onEventRaised -= OutBatter;
        allTrackingOffEvent.onEventRaised -= AllTrackingOff;
        addScore.onEventRaised -= AddScore;

    }

    private void Start()
    {
        BallCount = 0;
        Strike = 0;
        OutCount = 0;

        SetScore(0, 0);
        SetScore(1, 0);
        Inning = 0;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
            ThrowToBase(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            ThrowToBase(1);
        else if(Input.GetKeyDown(KeyCode.Alpha3))
            ThrowToBase(2);
        else if(Input.GetKeyDown(KeyCode.Alpha4))
            ThrowToBase(3);

        if (Input.GetKeyDown(KeyCode.V))
        {
            _ball.RemovePlayer();
            AllTrackingOff();
            _ball.IsGroundBall = false;
            _ball.IsPassing = false;
            defenders[0].SetMyBall(_ball);
        }
        //batter run
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!runners[0].gameObject.activeSelf)
            {
                runners[0].gameObject.SetActive(true);

                runners[0].BaseIndex = 0;
                runners[0].transform.position = bases[3].position;
                //runners[0].transform.rotation = Quaternion.LookRotation(bases[2].position);
            }
            runners[0].IsMove = !runners[0].IsMove;
            
            
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
            defenders[index].IsTracking = true;
        }
    }

    #region PROPERTY
    //*************************************************************************************** property
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
            _UIGameStatusElements[2].SetIndex(out_count);
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

            string t = inning % 2 == 0 ? "▲" : "▼";
            t += " " + inning / 2 + "이닝";
            if (inning >= MAX_INNING_COUNT)
            {
                //GameEnd
            }
            _inningText.text = t;
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
            
            //ui
            _UIGameStatusElements[1].SetIndex(strike_count);
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
            _UIGameStatusElements[0].SetIndex(ball_count);
        }
    }

    private void AddScore()
    {
        runners[0].gameObject.SetActive(false);
        SetScore(inning % 2, ++_teamStatus[inning % 2].Score);
    }

    public void SetScore(int teamIndex, int score)
    {
        _teamStatus[teamIndex].Score = score;
        _scoreTexts[teamIndex].text = (_teamStatus[teamIndex].Score).ToString();
        
        
    }

    public void AddBaseStatus()
    {
        int i;
        for (i = 1; i < 8; i <<= 2)
        {
            int num = i & isBaseStatus;
            //is Empty 
            if (num == 0)
            {
                isBaseStatus |= i;
                return;
            }
            //밀어내기
        }

        //밀어내기 득점
        if (i == MAX_BASE_COUNT)
        {
            AddScore();
        }

    }
    
    // *************************************************************end
    #endregion

    public void ThrowToBase(int index)
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
            defenders[i].IsTracking = false;
        }
    }

    #region ALGORITHM
    private void ThrowBallAlgorithm() //SO
    {
        //주자가 없는데 타자가 있다면 => 1루
        if(isBaseStatus == 0)
        {
            ThrowToBase(0);
        }
    }
    private void OutBatter(int index) 
    {
        
        //don't run
        if (!runners[index].IsMove)
        {
            Debug.Log("b" + runners[index].BaseIndex);
            Debug.Log("아웃");

            return;
        }

        //init
        runners[index].IsMove = false;
        AddOut();
        
        
        runners[index].gameObject.SetActive(false);
    }

    #endregion
}

struct TeamStatus
{
    private int score;
    
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

    public int Score
    {
        get => score;
        
        //only AddScore function
        set
        {
            score = value;
            
        }
    }

}