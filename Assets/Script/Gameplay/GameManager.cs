using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

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

    private bool[] isBaseStatus = new bool[MAX_BASE_COUNT];
    [SerializeField] private Batter[] runners = new Batter[4]; // 빠따든 주자는 [0]
    [SerializeField] private Batter batterPrefab; 
    
    private TeamStatus []_teamStatus = new TeamStatus[2];

    [Header("Broadcasting on EventChannels")]
    [SerializeField] private IntEventSO outBatterEvent; //Defender, Baseman
    [SerializeField] private VoidEventSO allTrackingOffEvent; //to baseball
    [SerializeField] private VoidEventSO addScore; //to Batter
    [SerializeField] private IntEventSO addIsBaseStatus; //to Batter

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
        
        addIsBaseStatus.onEventRaised += AddIsBaseStatus;
    }
    private void OnDisable()
    {
        outBatterEvent.onEventRaised -= OutBatter;
        allTrackingOffEvent.onEventRaised -= AllTrackingOff;
        addScore.onEventRaised -= AddScore;

        addIsBaseStatus.onEventRaised -= AddIsBaseStatus;
    }

    private void Start()
    {
        BallCount = 0;
        Strike = 0;
        OutCount = 0;

        for (int i = 0; i < MAX_BASE_COUNT; i++)
        {
            isBaseStatus[i] = false;
        }

        SetScore(0, 0);
        SetScore(1, 0);
        Inning = 0;
    }

    private void Update()
    {
        //debug
        if(Input.GetKeyDown(KeyCode.Alpha1))
            ThrowToBase(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            ThrowToBase(1);
        else if(Input.GetKeyDown(KeyCode.Alpha3))
            ThrowToBase(2);
        else if(Input.GetKeyDown(KeyCode.Alpha4))
            ThrowToBase(3);

        //has ball and ball batting
        if (_ball.MyDefender && _ball.IsBatTouch)
        {
            ThrowBallAlgorithm();
        }
        

        if (Input.GetKeyDown(KeyCode.Space))
        {
            DebugBatting();
        }
        
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
            for (int i = 0; i < runners.Length - 1; i++)
            {
                //move
                if (runners[i])
                {
                    runners[i].IsMove = true;
                }
            }
            
            if (!runners[0])
            {
                CreateBatter();
            }
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

    private void AddIsBaseStatus(int index)
    {
        //0 1 2
        if (index == 0)
        {
            isBaseStatus[index] = true;
        }
        else
        {
            isBaseStatus[index - 1] = false;
            
            
            if(index != 3)
                isBaseStatus[index] = true;
        }
        
        runners[index + 1] = runners[index];
        runners[index] = null;
    }

    private void AddScore()
    {
        runners[0].gameObject.SetActive(false);
        SetScore(inning % 2, ++_teamStatus[inning % 2].Score);
    }

    private void SetScore(int teamIndex, int score)
    {
        _teamStatus[teamIndex].Score = score;
        _scoreTexts[teamIndex].text = (_teamStatus[teamIndex].Score).ToString();
    }

    private void AddBaseStatus()
    {
        int i;
        
        for (i = 0; i < 3; i++)
        {
            //is Empty 
            if (isBaseStatus[i] == false)
            {
                isBaseStatus[i] = true;
                return;
            }
            //push base
        }

        //밀어내기 득점
        if (i == MAX_BASE_COUNT)
        {
            AddScore();
        }

    }
    
    // *************************************************************end
    #endregion

    private void ThrowToBase(int index)
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

        _ball.DefenderDis = min;
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

    private void CreateBatter()
    {
        runners[0] = Instantiate(batterPrefab, transform);
        runners[0].SetBall(_ball);
        runners[0].SetBases(bases);
        
        runners[0].transform.position = bases[3].position;
        runners[0].BaseIndex = 0;
        runners[0].IsMove = true;
        
        //runners[0].transform.rotation = Quaternion.LookRotation(bases[2].position);

    }

    private void DebugBatting()
    {
        float x = Random.Range(-1.0f, 0f);
        float z = Random.Range(-1.0f, 0f);
        Vector3 view = new Vector3(x, 1, z).normalized;

        _ball.IsBatTouch = true;
        _ball.IsGroundBall = false;
        _ball.IsPassing = false;

        _ball.RemovePlayer();

        float r = Random.Range(15.0f, 25.0f);
        
        view *= r;
        _ball.transform.position = Vector3.zero;
        _ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        _ball.GetComponent<Rigidbody>().AddForce(view, ForceMode.Impulse);
    }

    #region ALGORITHM
    private void ThrowBallAlgorithm() //SO
    {
        for (int i = 0; i < runners.Length; i++)
        {
            //has runner and run
            if (runners[i] && runners[i].IsMove)
            {
                ThrowToBase(i);
            }
        }
    }
    private void OutBatter(int index)
    {
        if (!runners[index])
        {
            return;
        }
        
        //has runner and don't run
        if (!runners[index].IsMove)
        {
            return;
        }
        
        AddOut();
        
        Destroy(runners[index].gameObject);
        runners[index] = null;
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