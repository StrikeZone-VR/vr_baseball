using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
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
    [SerializeField] private Batter currentBatter; //생성되는 위치 : 타석에 넣어야함 
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
        addScore.onEventRaised += IntoHome;
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
        addScore.onEventRaised -= IntoHome;
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
            
            //던질 곳 없으면 다시 투수 복귀
            if (!ThrowBallAlgorithm())
            {
                //위치를 여기다 둔 이유. 피쳐가 수비하면 타자 복귀가 안됨
                //안타를 쳤다면 나중에 복귀
                if (canBackRunner )
                {
                    canBackRunner = false;
                    if (!isFlyingOut)
                    {
                        TransformMyBodyToBatter();
                    }
                    StartCoroutine(TranslateBattingView());
                    DebugBaseStatus();
                }
                
                
                //AI투수가 이미 가지고 있다면
                if (pitcher && _ball.MyDefender == pitcher)
                {
                    return;
                }
                
                //다시 처음
                PitcherGetBall();

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
            OnlyOneTrackingOn(index);
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
            //4볼
            if (value >= BaseballModel.MAX_BALL_COUNT)
            {
                value = 0;

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

    private void IntoHome()
    {
        Debug.Log("[Batter] : 점수점수");
        Batter batter = gamePlayModel.RemoveRunner(3);
        batter.OutPlayer();
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
        OutCount = 0;
        //어차피 OutCount에 Ball, Strike가 초기화...?
        //BallCount = 0;
        //Strike = 0;
        
        ClearRunners();
    }

    //backToPitcherEvent => 이거 던질 곳 없거나 안타치는 순간 겹친다 그냥
    protected override void PitcherGetBall()
    {
        isFlyingOut = false;
        
        Catcher catcher = defenders[4] as Catcher;
        catcher.DefendIndex = 0;

        DebugBaseStatus();
        //batting mode
        if (gamePlayModel.GetTeamIndex() % 2 == 0)
        {
            waitPitcherCoroutine = StartCoroutine(WaitingBackToPitcher());
        }
        //이게 그러니까 pitchermode
        else
        {
            CreateBatter(); //currentBatter에 어차피 들어감
            pitchingController.ResetBall();
        }
    }
    
    IEnumerator WaitingBackToPitcher()
    {
        //StartCoroutine(BackPitching());

        currentBatter = myBody;
        
        //yield return new WaitForSeconds(WAIT_TIME);
        yield return null; //debug
        
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
        //여기에 currentRunner를 하더라. => 어차피 Runners에 다 있는데?
        
        foreach (Batter batter in gamePlayModel.GetRunners())
        {
            //my body는 시점 전환 X
            batter.OutPlayer(true);
        }
        
        gamePlayModel.ClearRunner();
    }

    private void RunRunner()
    {
        //투수는 스위칭
        
        //catcher
        
        Catcher catcher = defenders[4] as Catcher;
        catcher.DefendIndex = 1;
        
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

    //AI, 타자 변환 둘다 이 Batter를 생성
    private Batter CreateBatter(bool isAI = true, int base_index = 0)
    {
        Debug.Log("타자 생성");
        Batter batter = Instantiate(batterPrefab, batterCreatePosition);

        //Set
        batter.SetBases(bases);
        batter.SetBall(_ball);
        batter.SetBat(_bat);


        if (isAI)
        {
            //베트 자리로 이동
            batter.MovePlayer(batterPosition.position);
            currentBatter = batter;
        }
        
        batter.BaseIndex = base_index;
        batter.IsMove = false;

        _ball.OffTouchBall(); //.?

        
        //runners[0].transform.rotation = Quaternion.LookRotation(bases[2].position);
        return batter;
    }
    

    //move one base => 4볼
    void MoveOneBase()
    {
        //transview 뭐시기
        //주자라면 mybody를 먼저 대체하고 해야하나
        gamePlayModel.AddRunner(CreateBatter());
        gamePlayModel.MoveBaseRunner();
        
        
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
            Debug.LogError("[Batter] currentBatter is null");
            return;
        }
        
        currentBatter.OutPlayer(true); 
        //플레이어는 뭐 화면 깜빡거리면 될거같고
        //아니 만약 current가 플레이어면 안 되는 거 아닌가?
        
        
        //타자모드
        // if (Inning % 2 == 0)
        // {
        //     // 굳이? 이미 전에 current에 넣지 않았나?
        //     currentBatter = myBody;
        // }
        // else
        // {
        //     //이거 일단 플레이어 생성은 해야함 => 나중에 out 3개면 지워질거임
        //     currentBatter = CreateBatter();
        // }
    }
    
    #endregion 
    
    #region BATTINGMODE
    //주자 돌아가는 함수 => 이게 가장 문제다.
    private void StartBatter()
    {
        Debug.Log("타자 Mode On");

        pitchingController.EndPitchingGame();
        defenders[0].gameObject.SetActive(true);
        
        //투수 AI 세팅
        pitcher.IsThrowBallStop = false;
        defenders[0].SetMyBall(_ball);
        
        StartCoroutine(TranslateBattingView());
        //TranslateBattingView();
    }

    IEnumerator TranslateBattingView()
    {
        fadeEvent.FadeOut(0.5f); //이동하기 전
        yield return new WaitForSeconds(0.5f); 

        Vector3 movePosition = new Vector3(0, 1.0f, 0);
        Vector3 rotateVector = new Vector3(-1, 0, -1);
        MovePlayer(movePosition);
        RotatePlayer(rotateVector);

        fadeEvent.FadeIn(0.5f); //이동하고 나서
        currentBatter = myBody;
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
        
        //만약 플라잉 아웃이든 뭐든 아웃됐다면 타자를 생성할 이유가 없음
        if (myBody.BaseIndex == 0)
        {
            return;
        }
        Batter batter = CreateBatter(false, myBody.BaseIndex);

        batter.transform.position = myBody.transform.position;//프리펩 정보 이전
        
        gamePlayModel.ReplaceLastRunner(batter);
        //ㄴ 이미 대체 됐으니까 Remove는 필요없음
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
        currentBatter.OutPlayer(true);
        currentBatter = null;
    }

    
    /// <summary>
    /// 아웃하는 함수지만 베이스 아웃 판단하는 것도 넣음
    /// </summary>
    /// <param name="base_index">아웃 될 주자의 base_index임.</param>
    private void OutRunner(int base_index)
    {
        //주자의 base_index0 1 2 3
        //1루가기전 2루가기전 3루가기전 홈으로가기전
        //수비수 : 1 2 3 4
        
        // 기본적으로 공을 가지고있는 상태
        if (!_ball.IsBatTouch || !defenders[base_index + 1].IsInPosition)
        {
            return;
        }
        //debug
        if (base_index > 3)
        {
            Debug.LogError("베이스 index가 넘으면 안된다 : " + base_index);
        }

        //이미 베이스 인덱스는
        if (gamePlayModel.IsEmptyRunner(base_index))
        {
            return;
        }
        
        Batter runner = gamePlayModel.GetRunner(base_index);

        //만약 1루면 1루 전 러너가 있는지 확인. 러너가 달리지 않는다면
        if (!runner.IsMove)
        {
            return;
        }
        //주자를 아웃시켯
        AddOut();
        
        gamePlayModel.RemoveRunner(base_index);
        
        //Destroy();
        runner.OutPlayer();
        DebugBaseStatus();
    }
    
    private void AllTrackingOff()
    {
        OnlyOneTrackingOn();
    }

    private void OnlyOneTrackingOn(int ignore_index = -1)
    {
        for (int i = 0; i < defenders.Length; i++)
        {
            if (ignore_index == i)
            {
                continue;
            }
            if (defenders[i].gameObject.activeSelf)
            {
                defenders[i].IsTracking = false;
                
            }
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
            
            //Debug.Log("[Defender] 후잉 : " + _ball.MyDefender);
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
        
        //던질 곳 없음
        if (index == -1)
        {
            return false;
        }
    
        //던지기
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
        if (Input.GetKeyDown(KeyCode.C))
        {
            //스윙해라
            //currentBatter.Swing();
            MoveOneBase();
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            Debug.Log("투수 스토프");
            pitcher.IsThrowBallStop = !pitcher.IsThrowBallStop;
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            gamePlayModel.DebugPrintBaseStatus();
            //Debug.Log(gamePlayModel.GetRunnerCount());
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
        
        //Debug.Log("던지기 + " + x + ", " + z);
        //공 던지는 코루틴도 제거
        pitcher.StopPitching();
        
        //no Defender
        _ball.RemoveDefender();
        _ball.IsThrown = true;
        _ball.IsPassing = true;
        _ball.SetPosition(batterPosition.position + new Vector3(0, 2.0f, 0));
        _ball.SetVelocity(new Vector3(x, 0.5f, z) * 18f);
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
