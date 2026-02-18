using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class GamePlayManager : GameManager
{
    #region VARIABLES
    [Header("Debug")]
    [SerializeField] protected XROrigin playerOrigin; //debug용
    
    [Header("Objects")]
    [SerializeField] private Defender[] defenders; // pitcher => 0
    [SerializeField] private Transform[] bases;
    [SerializeField] private MyBody myBody; //플레이어 타자
    private Pitcher pitcher;
    
    [Header("Controllers")]
    [SerializeField] private GamePlayController gamePlayController;
    [SerializeField] private PitchingController pitchingController;
    [SerializeField] private BattingController battingController;
    
    [Header("Batter")] 
    [SerializeField] private Batter batterPrefab;
    [SerializeField] private Transform batterCreatePosition;
    [SerializeField] private Transform batterPosition;
    [SerializeField] private Batter currentBatter;
    [SerializeField] private Bat _bat;
    [SerializeField] private GameObject _axis;
    [SerializeField] private BaseStatusPanel _baseStatusPanel; //debug

    
    //A =>

    [Header("Listening to")] 
    [SerializeField] private VoidEventSO flyingOutEvent; //Defender, Baseman
    [SerializeField] private IntEventSO outRunnerEvent; //Defender, Baseman
    
    [SerializeField] private VoidEventSO startPitcherModeEvent; //to batter
    [SerializeField] private VoidEventSO swingEvent; //to Pitcher, auto swing
    [SerializeField] private VoidEventSO pitchEvent; //to PitchingBallController
    [SerializeField] private VoidEventSO onCanBackBatterEvent;

    [Space]
    [SerializeField] private VoidEventSO allTrackingOffEvent; //to baseball
    [SerializeField] private VoidEventSO addScore; //to Batter
    [SerializeField] private IntEventSO addIsBaseStatus; //to Batter
    [SerializeField] private VoidEventSO runSignalEvent;
    [SerializeField] private VoidEventSO changedBaseStatus;
    
    // => A
    [Header("Broadcasting on")]
    [SerializeField] private FadeChannelSO fadeEvent;

    private bool [] isBeforeBaseStatus = { false, false, false };
    private bool canBackRunner = false;
    private bool isFlyingOut = false;
    private int beforeScore = 0;
    
    private GamePlayModel gamePlayModel = new GamePlayModel();
    private BattingModel battingModel = new BattingModel();

    private Coroutine waitPitcherCoroutine;
    private bool isPrint = false; //debug
    const float WAIT_TIME = 7.0f; 
    
    #endregion
    #region SO
    protected override void OnEnable()
    {
        base.OnEnable();
        flyingOutEvent.onEventRaised += FlyingOut;
        outRunnerEvent.onEventRaised += OutRunner;
        //OutRunner

        allTrackingOffEvent.onEventRaised += AllTrackingOff;
        addScore.onEventRaised += thirdRunnerintoHome;
        runSignalEvent.onEventRaised += RunRunner;

        addIsBaseStatus.onEventRaised += AddIsBaseStatus;
        startPitcherModeEvent.onEventRaised += OnTouchBall;
        swingEvent.onEventRaised += DebugBatting;
        //pitchEvent.onEventRaised += SwingSignalToBatter;

        onCanBackBatterEvent.onEventRaised += OnCanBackRunner;
        changedBaseStatus.onEventRaised += DebugBaseStatus;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        flyingOutEvent.onEventRaised -= FlyingOut;
        outRunnerEvent.onEventRaised -= OutRunner;

        allTrackingOffEvent.onEventRaised -= AllTrackingOff;
        addScore.onEventRaised -= thirdRunnerintoHome;
        runSignalEvent.onEventRaised -= RunRunner;

        addIsBaseStatus.onEventRaised -= AddIsBaseStatus;
        startPitcherModeEvent.onEventRaised -= OnTouchBall;
        swingEvent.onEventRaised -= DebugBatting;
        //pitchEvent.onEventRaised -= SwingSignalToBatter;
        
        onCanBackBatterEvent.onEventRaised -= OnCanBackRunner;
        changedBaseStatus.onEventRaised -= DebugBaseStatus;
    }
    #endregion

    protected override void Start()
    {
        base.Start();
        pitcher = defenders[0] as Pitcher; 
        SetScore(0, 0);
        SetScore(1, 0);
        
        gamePlayModel.SetPanel(_baseStatusPanel);
        Inning = 0;
    }

    private void Update()
    {
        DebugInput();
        
        //수비 알고리즘
        //has ball and ball batting
        if (_ball.MyDefender && _ball.IsBatTouch)
        {
            //isFlyingOut이면 던지는 알고리즘도 바꿔야 함
            
            //isFlyingOut이면 되돌아가라
            
            //던질 곳 없으면 복귀
            if (!ThrowBallAlgorithm())
            {
                //안타를 쳤다면 나중에 복귀
                if (canBackRunner && !isFlyingOut)
                {
                    canBackRunner = false;
                    TransformMyBodyToBatter();
                    StartCoroutine(TranslateBattingView());
                    DebugBaseStatus();
                }
                
                //AI투수가 이미 가지고 있다면
                if (pitcher && _ball.MyDefender == pitcher)
                {
                    return;
                }
                
                PitcherGetBall();

                //투수일 경우 + currentBatter가 null인 경우
                if (!currentBatter && gamePlayModel.Inning % 2 == 1)
                {
                    CreateBatter();
                }
            }
        }
        //to pitcher
        
        if (_ball.MyDefender)
        {
            return;
        }

        //트래킹 알고리즘
        //tracking => 혹시 포수가 못 잡을 수 있으니 isBatTouch는 넣지말자
        //if (!_ball.IsPassing && _ball.IsGroundBall && !_ball.IsThrown)
        if(_ball.IsBatTouch)
        {
            int index = FindClosestDefenderIndex();
            AllTrackingOff();
            //Debug.Log(index);
            
            //closestDefender set tracking
            if (index == -1)
            {
                return;
            }
            
            //Debug.Log("트래킹 잠시 무효화");
            defenders[index].IsTracking = true;
        }
    }
    
    
    /// ////////////////////////////////////////////
    
    
    #region PROPERTY
    public int Inning
    {
        get { return gamePlayModel.Inning; }
        set
        {
            if (value >= GamePlayModel.MAX_INNING_COUNT)
            {
                Debug.Log("Game Over, back to the menu...");

                //GameEnd
                return;
            }

            gamePlayModel.Inning = value;
            InitInning();


            int num = value % 2;

            //change 
            if (num == 0)
            {
                StartBatter();
            }
            else
            {
                StartPitcher();
            }
            
            gamePlayController.SetInningText(value);
        }
    }
    
    public int OutCount
    {
        get { return gamePlayModel.OutCount; }
        set
        {
            BallCount = 0;
            Strike = 0;

            //change inning
            if (value >= GamePlayModel.MAX_OUT_COUNT)
            {
                value = 0;
                Inning++;
            }
            gamePlayModel.OutCount = value;

            gamePlayController.SetUIGameStatusIndex(2, value);
            DebugBaseStatus();
        }
    }
    
    public override int Strike //나중에 battingSystem에서는 override
    {
        get { return baseballModel.Strike; }
        set
        {
            //out
            if (value >= BaseballModel.MAX_STRIKE_COUNT)
            {
                value = 0;

                DeleteRunner();
                AddOut();
            }
            else
            {
                //아니 근데 여기서 달리기를 되돌리는건 좀
            }
            //상태저장
            baseballModel.Strike = value;
            
            //ui
            gamePlayController.SetUIGameStatusIndex(1, value);
            battingController.SetStrikeToText(value);
        }
    }
    
    public override int BallCount //나중에 battingSystem에서는 override
    {
        get { return baseballModel.BallCount; }
        set
        {
            if (value >= BaseballModel.MAX_BALL_COUNT)
            {
                value = 0;
                _ball.IsBatTouch = false;

                //AddBaseStatus();
                MoveOneBase();
            }
            baseballModel.BallCount = value;
            gamePlayController.SetUIGameStatusIndex(0, value);
            battingController.SetBallCountToText(value);
        }
    }

    private void AddOut()
    {
        OutCount++;
    }

    //대체로 볼넷으로 준 경우 or 주자가 자연스럽게 옮긴 경우 (이거만 유일한 주자 조정 함수임)
    private void AddIsBaseStatus(int index)
    {
        //for문으로 BaseIndex 한 칸식 올려라
        gamePlayModel.MoveBase();
        DebugBaseStatus();
    }

    private void thirdRunnerintoHome()
    {
        Batter batter = gamePlayModel.RemoveRunner(3);
        Destroy(batter.gameObject); //pooling?
        AddScore(1);
    }

    protected override void Foul()
    {
        //만약에 점수를 냈다면?
        RerollBeforeStatus();
        ++FoulCount;

        Debug.Log("파울");
        //strike == 2
        if (Strike == BaseballModel.MAX_STRIKE_COUNT - 1)
        {
            return;
        }
        
        AddStrike();
    }

    protected override void Homerun()
    {
        Debug.Log("홈런");
        AddScore(gamePlayModel.GetRunnerCount());
        ClearRunners();
        ++HomerunCount;
        CreateBatter(); //주자는 없으니까
    }

    private void AddScore(int value)
    {
        int teamIndex = gamePlayModel.GetTeamIndex();
        SetScore(teamIndex, gamePlayModel.AddScore(value));
    }
    
    private void SetScore(int teamIndex, int score)
    {
        gamePlayController.SetScoreText(teamIndex, score);
    }
    public int HomerunCount
    {
        get { return battingModel.Homerun; }
        set
        {
            battingModel.Homerun = value;
            battingController.SetHomerunToText(value);
        }
    }

    public int FoulCount
    {
        get { return battingModel.Foul; }
        set
        {
            battingModel.Foul = value;
            battingController.SetFoulToText(value);
        }
    }
    public int GroundBallCount
    {
        get { return battingModel.GroundBall; }
        set
        {
            battingModel.GroundBall = value;
            battingController.SetGroundballToText(value);
        }
    }
    void OnCanBackRunner()
    {
        canBackRunner = true;
        DebugBaseStatus();
        //StartCoroutine(TranslateBattingView()); => canBackRunner로 해결
    }


    #endregion

    #region GAMEPLAY
    private void InitInning()
    {
        BallCount = 0;
        Strike = 0;
        OutCount = 0;

        ClearRunners();
    }

    //backToPitcherEvent => 이거 던질 곳 없거나 안타치는 순간 겹친다 그냥
    protected override void PitcherGetBall()
    {
        isFlyingOut = false;
        DebugBaseStatus();
        //batting mode
        if (gamePlayModel.GetTeamIndex() % 2 == 0)
        {
            waitPitcherCoroutine = StartCoroutine(WaitingBackToPitcher());
        }
        //이게 그러니까 pitchermode
        else
        {
            pitchingController.ResetBall();
        }
    }
    
    IEnumerator WaitingBackToPitcher()
    {
        //StartCoroutine(BackPitching());

        //yield return new WaitForSeconds(WAIT_TIME);
        yield return null;
        
        //만약 돌아올때 비활성화 된 경우
        if (defenders[0].gameObject.activeSelf)
        {
            battingController.PitcherGetBall();
        }
    }
    
    void MovePlayer(Vector3 position)
    {
        //debug
        if (playerOrigin.gameObject.activeSelf)
        {
            //방망이 중력, rotation position 풀기
            playerOrigin.MoveCameraToWorldLocation(position); //시점 타자 시점
        }
        else
        {
            moveOriginEvent.RaiseEvent(position);
        }
    }
    
    void RotatePlayer(Vector3 rotate)
    {
        if (playerOrigin.gameObject.activeSelf)
        {
            playerOrigin.MatchOriginUpCameraForward(Vector3.up, rotate);
        }
        else
        {
            rotateOriginEvent.RaiseEvent(rotate);
        }
    }
    
    private void OnTouchBall()
    {
        _ball.OnTouchBall();
    }
    
    #endregion
    
    #region BATTER
    
    /// <summary> runner clear </summary>
    private void ClearRunners()
    {
        if (currentBatter)
        {
            if (currentBatter.gameObject)
            {
                Destroy(currentBatter.gameObject);
                currentBatter = null;
            }
        }

        foreach (Batter batter in gamePlayModel.GetRunners())
        {
            //근데 이러면 my body는 시점이 초기화 되는 거 아닌가?
            batter.OutPlayer();
        }
        
        gamePlayModel.ClearRunner();
    }

    private void RunRunner()
    {
        //타자모드
        //주자들 달리는 신호
        MoveBase();
        
        if (gamePlayModel.Inning % 2 == 0)
        {
            //DebugMoveBase(1);
            
            //todo : 컨트롤러 이동 허용시키게 해주는 기능
            //debug
#if  UNITY_EDITOR
            XRDeviceSimulator xr = Object.FindAnyObjectByType<XRDeviceSimulator>();
            if (xr)
            {
                xr.keyboardXTranslateSpeed = 1.5f;
                xr.keyboardZTranslateSpeed = 1.5f;
            }
            
#endif
            currentBatter = myBody;
            currentBatter.SetBases(bases);
            
            //안타 두 번 이상 친 경우 (Debug Hitting 여러번 예방) => 비어있으면 어차피 처음임
            if (gamePlayModel.GetRunners().Count != 0 && myBody == gamePlayModel.GetLastRunner())
            {
                //isMove도 이미 움직였을 테이니
                return;
            }
            gamePlayModel.AddRunner(currentBatter);
            
            currentBatter.IsMove = true;
            return;
        }
        gamePlayModel.AddRunner(currentBatter);

        currentBatter.SetBases(bases);

        currentBatter.transform.position = bases[3].position;
        currentBatter.BaseIndex = 0;
        currentBatter.IsMove = true;
    }

    private void CreateBatter() //AI
    {
        Debug.Log("타자 생성");
        Batter batter = Instantiate(batterPrefab, batterCreatePosition.position, Quaternion.identity);

        batter.SetBall(_ball);
        batter.SetBat(_bat);
        batter.transform.parent = batterCreatePosition;

        //베트 자리로 이동
        batter.MovePlayer(batterPosition.position);

        currentBatter = batter;
        _ball.OffTouchBall();

        //batter

        //runners[0].transform.rotation = Quaternion.LookRotation(bases[2].position);
    }
    

    //move one base => 4볼
    void MoveOneBase()
    {
        Debug.Log("수정해야함! - 4볼 구현안함");
        //그냥 Batter MoveBase같은 함수 쓰면 되지 않을까?
        //MoveBase();
        //일단 신호 줘야할듯 => 던지지 말라고?
    }

    void MoveBase()
    {
        gamePlayModel.RunSignal();
    }
    
    private void RerollBeforeStatus()
    {
        Debug.Log("파울이라 돌아감 - 주자들이 되돌아 가는 기능은 안 넣음");

        //되돌아가자
        
        //내가 타자라면 그냥 페이드아웃
         if (gamePlayModel.Inning % 2 == 0)
         {
             StartCoroutine(TranslateBattingView());
             return;
         }
    }
    
    private void DeleteRunner()
    {
        if (!currentBatter)
        {
            return;
        }
        Destroy(currentBatter.gameObject);
        currentBatter = null;
        CreateBatter();
    }
    
    #endregion 
    
    #region BATTINGMODE
    //주자 돌아가는 함수 => 이게 가장 문제다.
    private void StartBatter()
    {
        Debug.Log("타자 Mode On");

        pitchingController.EndPitchingGame();
        defenders[0].gameObject.SetActive(true);
        
        pitcher.IsThrowBallStop = false;
        defenders[0].SetMyBall(_ball);
        
        StartCoroutine(TranslateBattingView());
        //TranslateBattingView();
    }

    IEnumerator TranslateBattingView()
    {
        fadeEvent.FadeOut(0.2f); //이동하기 전
        yield return new WaitForSeconds(0.2f); 

        Vector3 movePosition = new Vector3(0, 1.0f, 0);
        Vector3 rotateVector = new Vector3(-1, 0, -1);
        MovePlayer(movePosition);
        RotatePlayer(rotateVector);

        fadeEvent.FadeIn(0.2f); //이동하고 나서
    }
    
    //override
    protected override void SetVelocityToText(float velocity)
    {
        battingController.SetVelocityToText(velocity);
    }
    
    protected override void WaitPitchingToText(int time)
    {
        battingController.WaitPitchingToText(time);
        if (time == 3)
        {
            playAudioClipEvent.RaiseEvent(2);
        }
    }
    
    void TransformMyBodyToBatter()
    {
        Debug.Log("몸 체인지");
        Batter batter = Instantiate(batterPrefab.gameObject, batterCreatePosition).GetComponent<Batter>();

        batter.transform.position = myBody.transform.position;//프리펩 정보 이전
        batter.SetBases(bases);
        
        batter.BaseIndex = myBody.BaseIndex;
        batter.IsMove = false;

        
        gamePlayModel.ReplaceLastRunner(batter);
        //ㄴ 이미 대체 됐으니까
        //gamePlayModel.RemoveRunner(myBody.BaseIndex);
        myBody.BaseIndex = 0;
        
        //어차피 안타치면 runner는 생성된다?
    }
    #endregion 
    
    #region PITCHINGMODE
    private void StartPitcher()
    {
        Debug.Log("투수 Mode On");

        defenders[0].IsTracking = false;
        pitcher.IsThrowBallStop = true;
        pitchingController.StartPitchingGame();
        defenders[0].gameObject.SetActive(false);

        //방망이 위치 Vector3(-0.660000026,1.37,0.150000006) 여기로
        //방망이 중력, rotation position 얼리기
        CreateBatter();
        
        StartCoroutine(TranslatePitchingView());
    }
    
    IEnumerator TranslatePitchingView()
    {
        fadeEvent.FadeOut(0.2f);
        yield return new WaitForSeconds(0.2f);
        
        Vector3 movePosition = new Vector3(-13.46f, 1.0f, -13.46f);
        Vector3 rotateVector = new Vector3(1, 0, 1);
        MovePlayer(movePosition);
        RotatePlayer(rotateVector);
        
        fadeEvent.FadeIn(0.2f);
    }
    
    #endregion 
    
    #region DEFENSE
    
    private void FlyingOut()
    {
        AddOut();
        isFlyingOut = true;
        gamePlayModel.RemoveRunner(currentBatter.BaseIndex);
        
        //Destroy();
        currentBatter.OutPlayer();
        currentBatter = null;
    }

    private void OutRunner(int base_index)
    {
        AddOut();

        if (gamePlayModel.IsEmptyRunner(base_index))
        {
            return;
        }
        
        Batter batter = gamePlayModel.GetRunner(base_index);
        
        gamePlayModel.RemoveRunner(base_index);
        
        //Destroy();
        batter.OutPlayer();
    }
    
    private void AllTrackingOff()
    {
        for (int i = 0; i < defenders.Length; i++)
        {
            if (defenders[i].gameObject.activeSelf)
                defenders[i].IsTracking = false;
        }
    }
    
    private bool ThrowToBase(int index)
    {
        if (_ball.MyDefender)
        {
            if (_ball.MyDefender == defenders[index + 1] && (0 <= index && index < 4) ) //1루수 ~ 4루수
            {
                return false;
            }
            
            _ball.MyDefender.ThrowBall(bases[index].position + new Vector3(0, 0.5f, 0));
            return true;
        }

        return false;
    }

    
    /// <summary>
    /// 가장 가까운 수비수를 찾아라
    /// </summary>
    /// <returns></returns>
    private int FindClosestDefenderIndex()
    {
        float min = float.MaxValue;
        int index = -1;
        
        for (int i = 0; i < defenders.Length; i++)
        {
            float dis = GetDistanceBetween(_ball.GetTargetPosition(), defenders[i].transform.position);
            
            if (min > dis)
            {
                min = dis;
                index = i;
            }
        }

        //투수모드인 경우
        if (!defenders[index].gameObject.activeSelf)
        {
            return -1;
        }

        _ball.DefenderDis = min;
        return index;
    }
    
    private bool ThrowBallAlgorithm() //SO
    {
        int index = gamePlayModel.RunningIndex();
        if (index == -1)
        {
            return false;
        }
    
        if (ThrowToBase(index))
        {
            return true;
        }
        return false;
    }
    
    private float GetDistanceBetween(Vector3 a, Vector3 b)
    {
        float result = Vector3.Distance(a, b);
        return result;
    }
    #endregion 
    
    #region DEBUG

    void DebugInput()
    {
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



        // if (Input.GetKeyDown(KeyCode.B))
        // {
        //     //_ball.OnTouchBall();
        //     PitcherGetBall();
        // }
        //
        if (Input.GetKeyDown(KeyCode.C))
        {
            //isPrint = !isPrint;
            //MoveOneBase();        //batter run
            DebugBaseStatus();
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            DebugHitting();
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            Inning++;
        }
        // if (Input.GetKeyDown(KeyCode.C))
        // {
        //     //스윙해라
        //     currentBatter.Swing();
        // }
        if (Input.GetKeyDown(KeyCode.V))
        {
            Debug.Log("투수 스토프");
            pitcher.IsThrowBallStop = !pitcher.IsThrowBallStop;
        }

    }
    
    void DebugBaseStatus()
    {
        gamePlayModel.DebugBaseStatus();
        
    }

    void DebugHitting()
    {
        Debug.Log("디버깅용 타자 안타 함수 - player는 타자");
        //공을 던지면 isPassing, isThrown

        float x = Random.Range(-1.0f, 0f);
        float z = Random.Range(-1.0f, 0f);
        
        //공 던지는 코루틴도 제거
        pitcher.StopPitching();
        
        //no Defender
        _ball.RemovePlayer();
        _ball.IsThrown = true;
        _ball.IsPassing = true;
        _ball.SetPosition(batterPosition.position + new Vector3(0, 2.0f, 0));
        _ball.SetVelocity(new Vector3(x, 0.5f, z) * 20f);
        //10 : 내야 땅볼?
        //20 : 뜬 공
        
        //백 코루틴 제거?
        if(waitPitcherCoroutine != null)
            StopCoroutine(waitPitcherCoroutine);
        
        //친 순간은
        //땅볼과 isBack 제외 모두 체크
        _ball.IsBatTouch = true;
        _ball.IsZone = true;
        _ball.IsStrike = true;
        _ball.IsThrown = true;
        _ball.IsGroundBall = false;

        //_ball
        //batterPosition
        //속력 추가
        
        //만약 파울이면? => isPass와 isThrown 제거되는 듯 => 이거는 그냥 볼 필요는 없다.
    }
    
    //베이스 이동 디버그
    void DebugMoveBase(int index)
    {
        //RunRunner();
        MovePlayer(bases[index].position + new Vector3(0, 1.0f, 0));
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
    
    #endregion

}
