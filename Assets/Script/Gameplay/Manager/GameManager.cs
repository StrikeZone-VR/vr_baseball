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

    private Queue<Batter> [] runners = new Queue<Batter>[MAX_BASE_COUNT + 1]; //
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

        for (int i = 0; i < MAX_BASE_COUNT + 1; i++)
        {
            runners[i] = new Queue<Batter>();
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

        if (Input.GetKeyDown(KeyCode.B))
        {
            BallCount++;
        }
        //batter run
        if (Input.GetKeyDown(KeyCode.C))
        {
            MoveOneBase();
        }
        // if (Input.GetKeyDown(KeyCode.D))
        // {
        //     DebugBaseStatus();
        // }
        if (Input.GetKeyDown(KeyCode.A))
        {
            AddOut();
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
            if (value >= MAX_INNING_COUNT)
            {
                Debug.Log("Game Over, back to the menu...");
                //GameEnd
                return;
            }
            inning = value;
            ClearRunners();

            string t = inning % 2 == 0 ? "▲" : "▼";
            t += " " + (inning / 2 + 1) + "이닝";
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
                _ball.IsBatTouch = false;
                
                //AddBaseStatus();
                MoveOneBase();
            }
            _UIGameStatusElements[0].SetIndex(ball_count);
        }
    }

    private void AddIsBaseStatus(int index)
    {
        Batter batter = runners[index].Dequeue();
        runners[index + 1].Enqueue(batter);
    }

    private void AddScore()
    {
        Batter batter = runners[3].Dequeue();
        Destroy(batter.gameObject);
        
        SetScore(inning % 2, ++_teamStatus[inning % 2].Score);
    }

    private void SetScore(int teamIndex, int score)
    {
        _teamStatus[teamIndex].Score = score;
        _scoreTexts[teamIndex].text = (_teamStatus[teamIndex].Score).ToString();
    }

    private void ClearRunners()
    {
        for (int i = 0; i < runners.Length; i++)
        {
            while (runners[i].Count > 0)
            {
                Batter batter = runners[i].Dequeue();
                Destroy(batter.gameObject);
            }
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
        Batter batter = Instantiate(batterPrefab, transform);
        
        runners[0].Enqueue(batter);
        
        batter.SetBall(_ball);
        batter.SetBases(bases);
        
        batter.transform.position = bases[3].position;
        batter.BaseIndex = 0;
        batter.IsMove = true;
        
        //runners[0].transform.rotation = Quaternion.LookRotation(bases[2].position);

    }

    private void DebugBatting()
    {
        float x = Random.Range(-1.0f, 0f);
        float z = Random.Range(-1.0f, 0f);
        Vector3 view = new Vector3(-1, 1, -1).normalized;

        _ball.IsBatTouch = true;
        _ball.IsGroundBall = false;
        _ball.IsPassing = false;

        _ball.RemovePlayer();

        float r = Random.Range(15.0f, 25.0f);
        
        view *= 19;
        _ball.transform.position = Vector3.zero;
        _ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        _ball.GetComponent<Rigidbody>().AddForce(view, ForceMode.Impulse);
        
        MoveBase();
    }

    #region ALGORITHM
    private void ThrowBallAlgorithm() //SO
    {
        for (int i = runners.Length - 1; i >= 0; i--)
        {
            //has runner and run
            if (runners[i].Count > 0 && runners[i].Peek().IsMove)
            {
                ThrowToBase(i);
                break;
            }
        }
    }
    private void OutBatter(int index)
    {
        //don't have runner
        if (runners[index].Count == 0)
        {
            return;
        }

        Batter batter = runners[index].Peek();
        //has runner and don't run
        if (!batter.IsMove)
        {
            return;
        }
        
        AddOut();

        Destroy(batter.gameObject);
        runners[index].Dequeue();
    }

    //move one base
    void MoveOneBase()
    {
        MoveBase();
            
        //don't have Runner
        if (runners[0].Count == 0)
        {
            CreateBatter();
        }
    }

    void MoveBase()
    {
        for (int i = 0; i < runners.Length; i++)
        {
            //HasRunner
            if (runners[i].Count > 0)
            {
                runners[i].Peek().IsMove = true;
            }
        }
    }
    #endregion

    #region DEBUG

    void DebugBaseStatus()
    {
        for (int i = 0; i < runners.Length; i++)
        {
            Debug.Log(i + " : " + runners[i].Count);
        }
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