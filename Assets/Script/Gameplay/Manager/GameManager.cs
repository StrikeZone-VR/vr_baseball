using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
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

    [Header("Manager")] [SerializeField] private PitchingManager pitchingManager;


    [SerializeField] private XROrigin playerOrigin;

    [SerializeField] private UIGameStatus[] _UIGameStatusElements;
    [SerializeField] private TextMeshProUGUI[] _scoreTexts;
    [SerializeField] private TextMeshProUGUI _inningText;

    [SerializeField] private Defender[] defenders; // pitcher => 0
    [SerializeField] private Transform[] bases;
    [SerializeField] private Baseball _ball; //일단 PitchingBallController도 여깄음

    private Queue<Batter>[] runners = new Queue<Batter>[MAX_BASE_COUNT + 1];

    [Header("Batter")] 
    [SerializeField] private Batter batterPrefab;
    [SerializeField] private Transform batterCreatePosition;
    [SerializeField] private Transform batterPosition;
    [SerializeField] private Batter currentBatter;
    [SerializeField] private GameObject _bat;

    private TeamStatus[] _teamStatus = new TeamStatus[2];
    private int beforeScore = 0;
    private bool [] isBeforeBaseStatus = { false, false, false };

    [Header("Broadcasting on EventChannels")] [SerializeField]
    private IntEventSO outBatterEvent; //Defender, Baseman

    [SerializeField] private VoidEventSO addBallCountEvent; //to Baseball
    [SerializeField] private VoidEventSO strikeEvent; // toStrikeZone, batter
    [SerializeField] private VoidEventSO paulEvent; // toStrikeZone
    [SerializeField] private VoidEventSO homerunEvent; // toStrikeZone

    
    [Space] [SerializeField] private VoidEventSO allTrackingOffEvent; //to baseball
    [SerializeField] private VoidEventSO addScore; //to Batter
    [SerializeField] private IntEventSO addIsBaseStatus; //to Batter
    [SerializeField] private VoidEventSO runSignalEvent;

    
    [Space] [SerializeField] private VoidEventSO startPitchEvent; //to batter
    [SerializeField] private VoidEventSO swingEvent; //to Pitcher
    [SerializeField] private VoidEventSO pitchEvent; //to PitchingBallController
    [SerializeField] private VoidEventSO backToPitcherEvent; //baseball

    //Define
    private const int MAX_BALL_COUNT = 4;
    private const int MAX_STRIKE_COUNT = 3;
    private const int MAX_OUT_COUNT = 3;
    private const int MAX_INNING_COUNT = 18;
    private const int MAX_BASE_COUNT = 3;

    private void OnEnable()
    {
        outBatterEvent.onEventRaised += OutBatter;

        addBallCountEvent.onEventRaised += AddBallCount;
        strikeEvent.onEventRaised += AddStrike;
        paulEvent.onEventRaised += Paul;
        homerunEvent.onEventRaised += Homerun;

        allTrackingOffEvent.onEventRaised += AllTrackingOff;
        addScore.onEventRaised += AddScore;
        startPitchEvent.onEventRaised += OnTouchBall;
        runSignalEvent.onEventRaised += RunRunner;

        addIsBaseStatus.onEventRaised += AddIsBaseStatus;

        swingEvent.onEventRaised += DebugBatting;
        pitchEvent.onEventRaised += SwingSignalToBatter;
        backToPitcherEvent.onEventRaised += PitcherGetBall;
    }

    private void OnDisable()
    {
        outBatterEvent.onEventRaised -= OutBatter;

        addBallCountEvent.onEventRaised -= AddBallCount;
        strikeEvent.onEventRaised -= AddStrike;
        paulEvent.onEventRaised -= Paul;
        homerunEvent.onEventRaised -= Homerun;


        allTrackingOffEvent.onEventRaised -= AllTrackingOff;
        addScore.onEventRaised -= AddScore;
        startPitchEvent.onEventRaised -= OnTouchBall;
        runSignalEvent.onEventRaised -= RunRunner;

        addIsBaseStatus.onEventRaised -= AddIsBaseStatus;

        swingEvent.onEventRaised -= DebugBatting;
        pitchEvent.onEventRaised -= SwingSignalToBatter;
        backToPitcherEvent.onEventRaised -= PitcherGetBall;
    }

    private void Start()
    {
        Debug.Log("Game start");
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

        //pitcher has ball
        //_ball.RemovePlayer();
        //defenders[0].SetMyBall(_ball);
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
            if (ThrowBallAlgorithm())
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
            if (index == -1)
            {
                return;
            }

            defenders[index].IsTracking = true;
        }
    }

    #region PROPERTY

    //*************************************************************************************** property
    public int OutCount
    {
        get { return out_count; }
        set
        {
            out_count = value;

            BallCount = 0;
            Strike = 0;

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
        get { return inning; }
        set
        {
            if (value >= MAX_INNING_COUNT)
            {
                Debug.Log("Game Over, back to the menu...");

                //GameEnd
                return;
            }

            inning = value;
            InitInning();


            int num = inning % 2;

            //change 
            if (num == 0)
            {
                StartBatter();
            }
            else
            {
                StartPitcher();
            }

            string t = inning % 2 == 0 ? "▲" : "▼";
            t += " " + (inning / 2 + 1) + "이닝";
            _inningText.text = t;
        }
    }

    public int Strike
    {
        get { return strike_count; }
        set
        {
            //상태저장
            strike_count = value;
            
            //out
            if (strike_count >= MAX_STRIKE_COUNT)
            {
                strike_count = 0;

                DeleteRunner();
                OutCount++;
            }

            //ui
            _UIGameStatusElements[1].SetIndex(strike_count);
        }
    }

    public int BallCount
    {
        get { return ball_count; }
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

    private void AddBallCount()
    {
        BallCount++;
    }

    private void AddIsBaseStatus(int index)
    {
        Batter batter = runners[index].Dequeue();
        runners[index + 1].Enqueue(batter);
    }

    void AddStrike()
    {
        Strike++;
    }

    private void AddScore()
    {
        Batter batter = runners[3].Dequeue();
        Destroy(batter.gameObject);

        SetScore(inning % 2, ++_teamStatus[inning % 2].Score);
    }

    private void Paul()
    {
        PitcherGetBall();

        Debug.Log("Paul");
        //만약에 점수를 냈다면?
        RerollBeforeStatus();
        
        //strike == 2
        if (Strike == MAX_STRIKE_COUNT - 1)
        {
            return;
        }

        AddStrike();
    }

    private void Homerun()
    {
        PitcherGetBall();
        SetScore(inning % 2, ++_teamStatus[inning % 2].Score);
        for (int i = 0; i < runners.Length; i++)
        {
            _teamStatus[inning % 2].Score += runners[i].Count;
            SetScore(inning % 2, _teamStatus[inning % 2].Score);
        }

        ClearRunners();
    }


    private void SetScore(int teamIndex, int score)
    {
        _teamStatus[teamIndex].Score = score;
        _scoreTexts[teamIndex].text = (_teamStatus[teamIndex].Score).ToString();
    }

    private void InitInning()
    {
        BallCount = 0;
        Strike = 0;
        OutCount = 0;

        ClearRunners();
    }

    // ********************************************************************************************************************************************end

    #endregion

    private void RerollBeforeStatus()
    {
        //되돌아가자
        for (int i = 1; i < runners.Length; i++)
        {
            while (runners[i].Count > 0)
            {
                Batter batter = runners[i].Peek();
                batter.IsMove = false;
                batter.transform.position = bases[i].position;
            }
        }
        Batter b = runners[0].Dequeue();
        b.IsMove = false;
        b.transform.position = batterPosition.position;
    }
    

    private bool IsCheckBeforeStatus()
    {
        for (int i = 0; i < runners.Length; i++)
        {
            if (isBeforeBaseStatus[i] != (runners[i].Count != 0))
            {
                return true;
            }
        }

        return false;
    }

    private void DeleteRunner()
    {
        Debug.Log("제발 ");
        Destroy(currentBatter.gameObject);
        currentBatter = null;
        CreateBatter();
    }
    
    private void StartBatter()
    {
        Debug.Log("타자 Mode On");

        pitchingManager.EndPitchingGame();
        defenders[0].gameObject.SetActive(true);
        defenders[0].SetMyBall(_ball);

        //방망이 중력, rotation position 풀기
        playerOrigin.MoveCameraToWorldLocation(new Vector3(0, 1.0f, 0)); //시점 타자 시점
    }

    private void StartPitcher()
    {
        Debug.Log("투수 Mode On");

        defenders[0].IsTracking = false;
        pitchingManager.StartPitchingGame();
        defenders[0].gameObject.SetActive(false);

        //방망이 위치 Vector3(-0.660000026,1.37,0.150000006) 여기로
        //방망이 중력, rotation position 얼리기
        CreateBatter();

        playerOrigin.MoveCameraToWorldLocation(new Vector3(-10, 1.0f, -10));
        playerOrigin.MatchOriginUpCameraForward(Vector3.up, new Vector3(1, 0, 1));
    }

    void PitcherGetBall()
    {
        pitchingManager.ResetBall();
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
            if (defenders[i].gameObject.activeSelf)
                defenders[i].IsTracking = false;
        }
    }

    #region BATTER

    /// <summary> runner clear </summary>
    private void ClearRunners()
    {
        for (int i = 0; i < runners.Length; i++)
        {
            while (runners[i].Count > 0)
            {
                Batter batter = runners[i].Dequeue();
                if(!batter)
                    Destroy(batter.gameObject);
            }
        }
    }

    private void RunRunner()
    {
        runners[0].Enqueue(currentBatter);

        currentBatter.SetBases(bases);

        currentBatter.transform.position = bases[3].position;
        currentBatter.BaseIndex = 0;
        currentBatter.IsMove = true;
    }

    private void CreateBatter() //AI
    {
        Debug.Log("엄준식");
        Batter batter = Instantiate(batterPrefab, batterCreatePosition.position, Quaternion.identity);

        batter.SetBall(_ball);
        batter.SetBat(_bat);

        //베트 자리로 이동
        batter.MovePlayer(batterPosition.position);

        currentBatter = batter;
        _ball.OffTouchBall();

        //batter

        //runners[0].transform.rotation = Quaternion.LookRotation(bases[2].position);
    }


    private void OnTouchBall()
    {
        _ball.OnTouchBall();
    }

    private void DebugBatting()
    {
        //batter.DebugHitting();

        // float x = Random.Range(-1.0f, 0f);
        // float z = Random.Range(-1.0f, 0f);
        // Vector3 view = new Vector3(-1, 1, -1).normalized;
        //
        // _ball.IsBatTouch = true;
        // _ball.IsGroundBall = false;
        // _ball.IsPassing = false;
        //
        // _ball.RemovePlayer();
        //
        // float r = Random.Range(15.0f, 25.0f);
        //
        // view *= 19;
        // _ball.transform.position = Vector3.zero;
        // _ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        // _ball.GetComponent<Rigidbody>().AddForce(view, ForceMode.Impulse);
        //
        // MoveBase();
    }

    private void SwingSignalToBatter()
    {
        StartCoroutine(Swing());
    }

    IEnumerator Swing()
    {
        yield return new WaitForSeconds(1.0f);
        currentBatter.StartSwing();
    }

    #endregion


    #region ALGORITHM

    private void ThrowToBase(int index)
    {
        if (_ball.MyDefender)
            _ball.MyDefender.ThrowBall(bases[index].position + new Vector3(0, 0.5f, 0));
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

        if (!defenders[index].gameObject.activeSelf)
        {
            return -1;
        }

        _ball.DefenderDis = min;
        return index;
    }
    
    private bool ThrowBallAlgorithm() //SO
    {
        for (int i = runners.Length - 1; i >= 0; i--)
        {
            //has runner and run
            if (runners[i].Count > 0 && runners[i].Peek().IsMove)
            {
                ThrowToBase(i);
                return true;
            }
        }

        return false;
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

        if (currentBatter == batter)
        {
            currentBatter = null;
        }
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
            RunRunner();
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

    /// <summary>
    ///only AddScore function
    /// </summary>
    public int Score
    {
        get => score;

        set { score = value; }
    }
}