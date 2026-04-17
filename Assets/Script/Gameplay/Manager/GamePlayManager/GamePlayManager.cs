using System.Collections;
using System.Runtime.CompilerServices;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class GamePlayManager : GameManager
{
    #region VARIABLES
    [Header("Debug")]
    [SerializeField] protected XROrigin playerOrigin; //debug용
    [SerializeField] protected ActionBasedContinuousMoveProvider moveProvider; //debug용
    [SerializeField] private Color _myTeamColor;
    [SerializeField] private Color _yourTeamColor;
    
    [Space]
    
    [Header("Objects")]
    [SerializeField] private Player[] defenders; // pitcher => 0
    [SerializeField] private Transform[] bases;
    [SerializeField] private Transform init_base;
    [SerializeField] private MyBody myBody; //플레이어 타자 <= 이거를 event로 통신해야하는데
    private PitcherComponent _pitcherComponent;
    
    [Space]

    [Header("Controllers")]
    [SerializeField] private GamePlayController gamePlayController;
    [SerializeField] private PitchingController pitchingController;
    [SerializeField] private BattingController battingController;
    
    [Space]
    
    [Header("Batter")] 
    [SerializeField] private Player batterPrefab;
    [SerializeField] private Transform batterCreatePosition;
    [SerializeField] private Transform batterPosition;
    [SerializeField] private BatterComponent currentBatterComponent; //생성되는 위치 : 타석에 넣어야함 
    [SerializeField] private Bat _bat;
    [SerializeField] private GameObject _axis;
    [SerializeField] private BaseStatusPanel _baseStatusPanel; //debug
    [SerializeField] private TrajectoryBaseBallData _trajectoryBaseBallData;

    [Space]
    [SerializeField] private AssetReference gameMenu;
    [SerializeField] private AssetReference gameResultScene;
    [Space]
    

    //A =>

    [Header("Listening to")] 
    [SerializeField] private VoidEventSO flyingOutEvent; //Defender, Baseman
    [SerializeField] private IntEventSO outRunnerEvent; //Defender, Baseman
    
    [SerializeField] private VoidEventSO startPitcherModeEvent; //to batter
    [SerializeField] private VoidEventSO swingEvent; //to Pitcher, auto swing
    [SerializeField] private VoidEventSO pitchEvent; //to PitchingBallController
    [SerializeField] private VoidEventSO onCanBackBatterEvent; //
    
    [Space]
    [SerializeField] private VoidEventSO allTrackingOffEvent; //to baseball
    [SerializeField] private VoidEventSO addScore; //to Batter
    [SerializeField] private IntEventSO addIsBaseStatus; //to Batter
    [SerializeField] private VoidEventSO runSignalEvent;
    [SerializeField] private VoidEventSO changedBaseStatus;
    [Space]
    // => A
    [Header("Broadcasting on")]
    [SerializeField] private FadeChannelSO fadeEvent;
    [SerializeField] private MyBodyEventSO _setBodyEvent ;
    [SerializeField] private BoolEventSO _setPlayerMoveMode;
    [SerializeField] private SceneEventSO sceneEvent;

    [SerializeField] private bool canBackRunner = false;
    
    [SerializeField] private GamePlayModel gamePlayModel;
    [SerializeField] private BattingModel battingModel;

    private Coroutine waitPitcherCoroutine;
    
    private bool isFlyingOut = false;
    private bool canGetBall = true; //호출 위치를 어디에 해야할지 모르겠네
    private bool isPrint = false; //debug
    
    //define
    const float WAIT_TIME = 7.0f;  //투수 던지기 전 대기 상태
    const float FADE_WAIT_TIME = 0.5f;  
    
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
        swingEvent.onEventRaised += DebugSwing;
        //pitchEvent.onEventRaised += SwingSignalToBatter;

        onCanBackBatterEvent.onEventRaised += OnCanBackRunner;
        changedBaseStatus.onEventRaised += DebugBaseStatus;
        
        _ball.OnIsInGameplayChanged += SetPlayerMoveMode;
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
        swingEvent.onEventRaised -= DebugSwing;
        //pitchEvent.onEventRaised -= SwingSignalToBatter;
        
        onCanBackBatterEvent.onEventRaised -= OnCanBackRunner;
        changedBaseStatus.onEventRaised -= DebugBaseStatus;
        
        _ball.OnIsInGameplayChanged -= SetPlayerMoveMode;
    }

    #endregion

    protected override void Start()
    {
        base.Start();
        
        
        Init();
       
        Inning = 0;
    }

    private void Update()
    {
        DebugInput();
        
        BeforePitcherGetBall(); //갑자기 공 받는데 트래킹 될 수도 있다.
        TrackingBall();
    }

    #region GAMEPLAY
    
    //1Function
    void Init()
    { 
        gamePlayModel.Init();
        battingModel.Init();
        
        _pitcherComponent = GetDefenderComponent(0) as PitcherComponent; 
        SetScore(0, 0);
        SetScore(1, 0);
        
        SetMyBodyCamera();
        
        gamePlayModel.SetPanel(_baseStatusPanel);
    }
    
    //2 function
    /// <summary>
    /// 매 이닝 초기화될 때마다 실행되는 함수
    /// </summary>
    private void ChangedInning()
    {
        OutCount = 0;
        //어차피 OutCount에 Ball, Strike가 초기화...?
        //BallCount = 0;
        //Strike = 0;
        
        ClearRunners();
        SetColor();
    }

    
    /// <summary>
    /// 이닝 바뀔때 색 설정
    /// </summary>
    private void SetColor()
    {
        //색 설정
        Color defend_color;
        
        //타자
        if (gamePlayModel.IsMyTeamBatting())
        {
            defend_color = _yourTeamColor;
        }
        else //수비
        {
            defend_color = _myTeamColor;
        }

        //red
        //for defenders
        for (int i = 0; i < defenders.Length; i++)
        {
            defenders[i].SetShirtColor(defend_color);
        }
    }

    //backToPitcherEvent => 이거 던질 곳 없거나 안타치는 순간 겹친다 그냥
    /// <summary>
    /// 애초에 공을 받는 함수. **직접 호출하지 마라** _ball.Dead함수로 호출해라
    /// 이거 제외한 함수에 투수에게 직접 공 주는 함수가 있다면 제거해라
    /// </summary>
    protected override void PitcherGetBall()
    {
        canGetBall = false;
        isFlyingOut = false;
        
        Debug.Log("[Game] : ResetBall");
        //포수 위치 변환
        CatcherComponent catcherComponent = GetDefenderComponent(4) as CatcherComponent;
        catcherComponent.DefendIndex = 0;

        //디버그 베이스 세팅보여주기
        DebugBaseStatus();
        
        //batting mode
        if (gamePlayModel.IsMyTeamBatting())
        {
            waitPitcherCoroutine = StartCoroutine(WaitingBackToPitcher());
            //안에 canGetBall 변수가 있음. 
        }
        //이게 그러니까 pitchermode
        else
        {
            //주자 생성
            //기본 조건 : 투수모드
            //그냥 주자가 안 움직이고 index가 1 이상이면 Next?
            //또는 current가 Null이면
            if (!currentBatterComponent 
                || (!currentBatterComponent.IsMove && currentBatterComponent.BaseIndex >= 1))
            {
                currentBatterComponent = NextBatter();
            }
            pitchingController.PlayerPitcherResetBall();
            canGetBall = true;
        }
    }
    
    //공 가져오는 함수
    IEnumerator WaitingBackToPitcher()
    {
        //StartCoroutine(BackPitching());

        //배터
        currentBatterComponent = myBody.GetMyBatterComponent();
        myBody.GetMyBatterComponent().IsOut = false;
        
        //yield return new WaitForSeconds(WAIT_TIME);
        yield return null; //빠른 테스트를 위해
        canGetBall = true;
        
        //만약 돌아올때 비활성화 된 경우
        if (defenders[0].gameObject.activeSelf)
        {
            //canBackRunner
            battingController.PitcherGetBall(); //투수가 공 받는 함수
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
            Debug.Log("AAA");
            playerOrigin.MatchOriginUpCameraForward(Vector3.up, rotate);
        }
        else
        {
            Debug.Log("BBB");
            rotateOriginEvent.RaiseEvent(rotate);
        }
    }
    
    private void OnTouchBall()
    {
        _ball.OnTouchBall();
    }

    private void SetPlayerMoveMode(bool isMove)
    {
        Debug.Log("[Player] : isMove = " + isMove);

        if (playerOrigin.gameObject.activeSelf)
        {
#if  UNITY_EDITOR
            XRDeviceSimulator xr = Object.FindAnyObjectByType<XRDeviceSimulator>();
            if (xr)
            {
                float speed = 0f;
                if (isMove)
                {
                    speed = 1.5f;
                }
                xr.keyboardXTranslateSpeed = speed;
                xr.keyboardYTranslateSpeed = speed;
                xr.keyboardZTranslateSpeed = speed;
            }
#endif
            moveProvider.enabled = isMove;
        }
        else
        {
            _setPlayerMoveMode.RaiseEvent(isMove);
        }
    }
    

    #endregion
    
    #region BATTER
    
    /// <summary> runner clear </summary>
    private void ClearRunners()
    {
        //여기에 currentRunner를 하더라. => 어차피 Runners에 다 있는데?
        
        foreach (BatterComponent batter in gamePlayModel.GetRunners())
        {
            //my body는 시점 전환 X
            batter.OutPlayer(true);
        }
        
        //debug로 Inning을 넘길 시 AI타자가 돌아다니는 버그
        //만약 current가 AI인 경우
        //ㄴ 원래는 !=로 해야하지만 inning이 바뀐 후라 !를 안 썼다.
        if (currentBatterComponent && gamePlayModel.IsMyTeamBatting())
        {
            currentBatterComponent.OutPlayer();
            currentBatterComponent = null;
        }
        gamePlayModel.ClearRunner();
    }

    private void RunRunner()
    {
        //투수는 스위칭
        
        //catcher 베이스 안으로
        CatcherComponent catcherComponent = GetDefenderComponent(4) as CatcherComponent;
        catcherComponent.DefendIndex = 1;
        
        //타자모드
        //주자들 달리는 신호
        
        MoveBase();
        
        if (gamePlayModel.IsMyTeamBatting())
        {
            //DebugMoveBase(1);

            currentBatterComponent.SetBases(bases, init_base);
            
            //안타 두 번 이상 친 경우 (Debug Hitting 여러번 예방) => 비어있으면 어차피 처음임
            if (gamePlayModel.GetRunners().Count != 0 && myBody == gamePlayModel.GetLastRunner())
            {
                //isMove도 이미 움직였을 테이니
                return;
            }
            gamePlayModel.AddRunner(currentBatterComponent);
            
            currentBatterComponent.IsMove = true;
            return;
        }
        gamePlayModel.AddRunner(currentBatterComponent);

        currentBatterComponent.SetBases(bases, init_base);

        Debug.LogWarning("주석을 해야할지도 아닐지도? : 일단 함");
        //currentBatter.transform.position = bases[3].position;
        
        currentBatterComponent.BaseIndex = 0;
        currentBatterComponent.IsMove = true;
    }

    /// <summary>
    /// "투수모드"에서 타석에 서있는 AI 타자 생성.
    /// 아웃, 홈런, 사구 각각 있음
    /// ㄴ 파울은 아님
    /// </summary>
    /// <param name="isAI"></param>
    /// <param name="base_index"></param>
    /// <returns></returns>
    private BatterComponent NextBatter(int base_index = 0)
    {
        //Debug.Log("[Batter] 생성");
        //어차피 아웃되거나 안타 확정될때 초기화 해야함
        BallCount = 0;
        Strike = 0;
        
        BatterComponent batter = CreateBatter(base_index);
        
        //타석인가?
        // IsMove뒤에 둔 이유 : IsMove에 StopMove이 있음
        //create 석으로 이동?
        batter.transform.position = batterCreatePosition.position;

        //베트 자리로 이동
        batter.gameObject.GetComponent<Player>().MovePlayer(batterPosition.position);
        currentBatterComponent = batter;
        
        return batter;
    }
    
    /// <summary>
    /// AI, 타자 변환, 파울이나 플라잉아웃시 되돌아오는 것  모두 Batter를 생성
    /// </summary>
    /// <param name="isStartBatter"> true : 타석, false 나머지 </param>
    /// <param name="base_index"></param>
    /// <returns></returns>
    private BatterComponent CreateBatter(int base_index = 0)
    {
        Debug.Log("[Batter]  타자 생성");
        Player batter = Instantiate(batterPrefab, batterCreatePosition);
        BatterComponent batterComponent = batter.GetComponent<BatterComponent>();
        
        //Set
        batterComponent.SetBases(bases, init_base);
        batter.SetBall(_ball);
        batterComponent.SetBat(_bat);

        //0 == 0, 타자모드
        if (gamePlayModel.IsMyTeamBatting())
        {
            batter.SetShirtColor(_myTeamColor);
        }
        else
        {
            batter.SetShirtColor(_yourTeamColor);
        }
        
        gamePlayModel.SaveBeforeStatus();
        
        batterComponent.SetBaseIndexPosition(base_index);
        batterComponent.IsMove = false;

        _ball.OffTouchBall(); //.?

        //runners[0].transform.rotation = Quaternion.LookRotation(bases[2].position);
        return batterComponent;
    }
    

    //move one base => 4볼
    void MoveOneBase()
    {
        //transview 뭐시기
        //주자라면 mybody를 먼저 대체하고 해야하나

        if(gamePlayModel.IsMyTeamBatting())
        {
            //그냥 AI를 생성해서 저쪽에 넣는다.
            gamePlayModel.AddRunner(CreateBatter());
            Strike = 0;
            gamePlayModel.MoveBaseRunner();
        }
        else
        {
            gamePlayModel.AddRunner(currentBatterComponent);
            gamePlayModel.MoveBaseRunner(); // 왜 안움직이지
            currentBatterComponent = NextBatter();
        }

        //그냥 Batter MoveBase같은 함수 쓰면 되지 않을까?
        //MoveBase();
        //일단 신호 줘야할듯 => 던지지 말라고?
    }

    void MoveBase()
    {
        gamePlayModel.RunSignal();
    }
    
    private void RollbackBeforeStatus()
    {
        gamePlayModel.DebugBeforeStatus();
        //되돌아가는데 점수를 얻은 경우
        if (gamePlayModel.BeforeScore != gamePlayModel.GetScore())
        {
            //runners의 insert 맨 앞 
            gamePlayModel.InsertRunner(CreateBatter(0));
            //어차피 baseindex는 나중에 설정할거임
        }
        
        currentBatterComponent.IsMove = false;
        gamePlayModel.FoulRollbackBeforeStatus(); //정보만 바뀜

        if (gamePlayModel.IsMyTeamBatting())
        {
            StartCoroutine(TranslateBattingView());
        }
        else //AI타자가 파울이면?
        {
            gamePlayModel.GetLastRunner().SetBaseIndexPosition(0);
            //맨 뒤 주자 제거
            gamePlayModel.RemoveLastRunner();
        }

        //내가 타자라면 그냥 페이드아웃
         if (gamePlayModel.IsMyTeamBatting())
         {
             canBackRunner = true;
             return;
         }
         
         DebugBaseStatus();
    }
    
    
    //주자 아웃
    private void DeleteRunner()
    {
        if (!currentBatterComponent)
        {
            Debug.LogError("[Batter] currentBatter is null");
            return;
        }
        
        currentBatterComponent.OutPlayer(false); //따로 true때 발동하는 기능은 Strike++에 넣어놨음
        
        //투수모드
        if (!gamePlayModel.IsMyTeamBatting())
        {
            currentBatterComponent = null;
        }
        
        //플레이어는 뭐 화면 깜빡거리면 될거같고
        //아니 만약 current가 플레이어면 안 되는 거 아닌가?
        
        
        //타자모드
        // if ()
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
    
    /// ////////////////////////////////////////////
    // 플라잉 아웃되면 되돌아 가는 기능 
    public void ReverseMoveBase()
    {
        int n = gamePlayModel.GetScore() - gamePlayModel.BeforeScore;
        for (int i = 0; i < n; i++)
        {
            gamePlayModel.InsertRunner(CreateBatter(0));
        }
        
        gamePlayModel.FlyingOutRollbackBeforeStatus();
    }
    #endregion 
    
    #region BATTINGMODE
    //주자 돌아가는 함수 => 이게 가장 문제다.
    private void StartBatterMode()
    {
        Debug.Log("<color=green>[GamePlay] : 타자 Mode On</color>");

        pitchingController.EndPitchingGame();
        defenders[0].gameObject.SetActive(true); //pitcher로 하면 mybody도 true가 될 수 있으니까
        
        //투수 AI 세팅
        _pitcherComponent.IsThrowBallStop = false;
        _ball.CurrentState = BallState.Dead;
        
        //GetDefenderComponent(0).SetMyBall(_ball);
        myBody.SetMode(true);
        
        
        StartCoroutine(TranslateBattingView());
        //TranslateBattingView();
    }

    IEnumerator TranslateBattingView()
    {
        fadeEvent.FadeOut(FADE_WAIT_TIME); //이동하기 전
        yield return new WaitForSeconds(FADE_WAIT_TIME);

        Vector3 movePosition = new Vector3(0, 1.0f, 0);
        Vector3 rotateVector = new Vector3(-1, 0, -1);
        MovePlayer(movePosition);
        RotatePlayer(rotateVector);

        fadeEvent.FadeIn(FADE_WAIT_TIME); //이동하고 나서
        currentBatterComponent = myBody.GetMyBatterComponent();
        
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
        if (myBody.GetMyBatterComponent().BaseIndex == 0)
        {
            return;
        }
        BatterComponent batterComponent = CreateBatter(myBody.GetMyBatterComponent().BaseIndex);
        BallCount = 0;
        Strike = 0;

        batterComponent.transform.position = myBody.transform.position;//프리펩 정보 이전
        
        gamePlayModel.ReplaceLastRunner(batterComponent);
        //ㄴ 이미 대체 됐으니까 Remove는 필요없음
        //gamePlayModel.RemoveRunner(myBody.BaseIndex);
        myBody.GetMyBatterComponent().BaseIndex = 0;
        
        //어차피 안타치면 runner는 생성된다?
    }
    #endregion 
    
    #region PITCHINGMODE
    private void StartPitcherMode()
    {
        Debug.Log("<color=green>[GamePlay] : 투수 Mode On</color>");

        GetDefenderComponent(0).IsTracking = false;
        _pitcherComponent.IsThrowBallStop = true;
        pitchingController.StartPitchingGame();
        
        _ball.CurrentState = BallState.Dead;
        SetPlayerMoveMode(false);
            
        defenders[0].gameObject.SetActive(false);
        myBody.SetMode(false);
        
        currentBatterComponent = NextBatter();

        //방망이 위치 Vector3(-0.660000026,1.37,0.150000006) 여기로
        //방망이 중력, rotation position 얼리기
        //; //OutCount에 넣었으면 안 넣어도 됨
        
        StartCoroutine(TranslatePitchingView());
    }
    
    IEnumerator TranslatePitchingView()
    {
        fadeEvent.FadeOut(FADE_WAIT_TIME);
        yield return new WaitForSeconds(FADE_WAIT_TIME);
        
        Vector3 movePosition = new Vector3(-13.46f, 1.0f, -13.46f);
        Vector3 rotateVector = new Vector3(1, 0, 1);
        MovePlayer(movePosition);
        RotatePlayer(rotateVector);
        
        fadeEvent.FadeIn(FADE_WAIT_TIME);
    }
    
    #endregion 
    
    #region DEFENSE
    /// <summary>
    /// 트래킹 알고리즘 
    /// </summary>
    private void TrackingBall()
    {
        if (_ball.MyDefenderComponent)
        {
            return;
        }
        //tracking => 혹시 포수가 못 잡을 수 있으니 isBatTouch는 넣지말자
        //if (!_ball.IsPassing && _ball.IsGroundBall && !_ball.IsThrown)
        
        //인 게임 플레이
        if(_ball.IsInGamePlay)
        {
            //떨어지는 공 위치중에서 가장 가까운 수비수
            int index = FindClosestDefenderIndex();
            OnlyOneTrackingOn(index);
            
            //Debug.Log("엄준식2 + " + GetDefenderComponent(index).name);

            //closestDefender set tracking
            if (index == -1)
            {
                return;
            }
            
            //Debug.Log("트래킹 잠시 무효화");
            GetDefenderComponent(index).IsTracking = true;
        }
    }
    
    
    private void BeforePitcherGetBall()
    {
        //Debug.LogWarning("이러면 1루 견제를 하면 못 돌아옴");
        if (!_ball.IsInGamePlay)
        {
            return;
        }
        
        //수비 알고리즘
        //has ball and ball batting => 포수 방지용으로 존에 들어간 순간부터 하는게 낫지 않을까?
        if (_ball.MyDefenderComponent && _ball.IsZone)
        {
            //던질 곳 없으면 다시 투수 복귀
            if (!ThrowBallAlgorithm())
            {
                //어차피 근데 타자모드일때만 이라고 해도 canBackRunner자체가 여기에서만 나올듯
                //위치를 여기다 둔 이유. 피쳐가 수비하면 타자 복귀가 안됨
                //안타나 플라잉아웃이면 나중에 복귀
                if (canBackRunner) 
                {
                    canBackRunner = false;
                    
                    //안타
                    if (!isFlyingOut && !myBody.GetMyBatterComponent().IsOut)
                    {
                        TransformMyBodyToBatter();
                    }
                    StartCoroutine(TranslateBattingView());
                    DebugBaseStatus();
                }
                
                
                //AI투수가 공을 가지고 있다면
                if (_pitcherComponent && _ball.MyDefenderComponent == _pitcherComponent)
                {
                    return;
                }
        
                
                if(canGetBall)
                {
                    Debug.Log("[Baseball] 상태 : " + _ball.CurrentState);
                    _ball.CurrentState = BallState.Dead; //죽으면 결국 다시 생기지 않나
                    //PitcherGetBall();
                }
            }
            //플레이어 투수가 공을 잡은 경우
        }
    }
    
    private void FlyingOut()
    {
        Debug.Log("[Batting] : 플라잉 아웃");

        //여기에 AddOut을 넣으면 이닝이 바뀌었는데 주자를 제거하고 있음
        isFlyingOut = true;
        gamePlayModel.RemoveLastRunner(); //RemoveRunner로 하면 baseindex = 1이 두명이고 앞선 주자가 아웃이 된다. 
        
        //Destroy();
        currentBatterComponent.OutPlayer(true);
        currentBatterComponent = null;

        //되돌아가자
        ReverseMoveBase();
        AddOut(); 
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
        if (!_ball.IsInGamePlay || !GetDefenderComponent(base_index + 1).IsInPosition)
        {
            //Debug.LogError("1탄 : " + _ball.IsBatTouch + ", " + GetDefenderComponent(base_index + 1).IsInPosition);
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
            //Debug.LogError("2탄");
            return;
        }
        
        BatterComponent runner = gamePlayModel.GetRunner(base_index);

        //만약 1루면 1루 전 러너가 있는지 확인. 러너가 달리지 않는다면
        if (!runner.IsMove)
        {
            //Debug.LogError("3탄");
            return;
        }
        //Debug.Log("4탄");
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
                GetDefenderComponent(i).IsTracking = false;
            }
        }
    }
    
    private bool ThrowToBase(int index)
    {
        //0 1 2 3
        //1루까지 2루까지 3루까지 홈까지
        
        if (_ball.MyDefenderComponent)
        {
            if (index < 0 && 4 <= index) //1루수 ~ 4루수
            {
                return false;
            }
            
            //내 베이스맨이 주자가 있는 상태에서 공을 가진 경우. => 트래킹
            //1 2 3 4
            if (_ball.MyDefenderComponent != defenders[index + 1].GetPlayerComponent())
            {
                _ball.MyDefenderComponent.ThrowBall(bases[index].position + new Vector3(0, 0.5f, 0));
            }
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
            //투수모드인 경우
            if (!gamePlayModel.IsMyTeamBatting())
            {
                continue;
            }
            float dis = GetDistanceBetween(_trajectoryBaseBallData.GetLandingPoint(), defenders[i].transform.position);
            
            if (min > dis)
            {
                min = dis;
                index = i;
            }
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

        //플레이어 투수인 경우. 어차피 자기가 던지니까
        if (_ball.MyDefenderComponent == myBody.GetMyPitcherComponent())
        {
            return true;
        }
    
        //타자의 0 1 2 3
        //던지기 => 만약 공 mydefender와 index가 같은 경우 => 무조건 자기 베이스로 돌아가야함
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
    
    #region PROPERTY
    public int Inning
    {
        get { return gamePlayModel.Inning; }
        set
        {
            if (value >= GamePlayModel.MAX_INNING_COUNT)
            {
                Debug.Log("Game Over, back to the menu...");

                sceneEventSO.RaiseEvent(gameResultScene);
                
                //정보 전달해야함
                return;
            }
            
            gamePlayModel.Inning = value;
            ChangedInning();

            int num = value % 2;

            //change 
            if (num == gamePlayModel.MyTeamIndex)
            {
                StartBatterMode();
            }
            else
            {
                StartPitcherMode();
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

            // //debug
            // if (currentBatterComponent)
            // {
            //     Debug.Log("[Batter] 아웃 주자 있음 : " + currentBatterComponent.name);
            // }
            // else
            // {
            //     Debug.Log("[Batter] 주자 없음 : ");
            // }
            
            
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
                //batter
                if (gamePlayModel.IsMyTeamBatting())
                {
                    StartCoroutine(TranslateBattingView());
                }
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
                gamePlayModel.SaveBeforeStatus();
            }

            baseballModel.BallCount = value;
            gamePlayController.SetUIGameStatusIndex(0, value);
            battingController.SetBallCountToText(value);
        }
    }

    private void AddOut()
    {
        Debug.Log("[Batter] : out");
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
        BatterComponent batterComponent = gamePlayModel.RemoveRunner(3);
        batterComponent.OutPlayer();
        AddScore(1);
    }

    protected override void Foul()
    {
        //만약에 점수를 냈다면?
        RollbackBeforeStatus();
        ++FoulCount;

        Debug.Log("파울");
        
        //strike == 2
        if (Strike == BaseballModel.MAX_STRIKE_COUNT - 1)
        {
            return;
        }
        
        AddStrike();
    }

    /// <summary>
    /// 홈런치면 뒤에
    /// </summary>
    protected override void Homerun()
    {
        Debug.Log("홈런");
        AddScore(gamePlayModel.GetRunnerCount());
        ClearRunners();

        if (gamePlayModel.IsMyTeamBatting())
        {
            StartCoroutine(TranslateBattingView());
        }
        else
        {
            currentBatterComponent = NextBatter(); //주자 나와야 함
        }
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

    DefenderComponent GetDefenderComponent(int index)
    {
        return defenders[index].GetPlayerComponent() as DefenderComponent;
    }

    private void SetMyBodyCamera()
    {
        //debug
        if (playerOrigin.gameObject.activeSelf)
        {
            myBody.SetCamera(playerOrigin.Camera);
        }
        else
        {
            _setBodyEvent.RaiseEvent(myBody);
        }
    }

    #endregion
    
    #region DEBUG

    void DebugInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("1루 던지기");
            myBody.GetMyPitcherComponent().ThrowBall(
                bases[0].position + new Vector3(0, 0.5f, 0)
            );
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("2루 던지기");
            myBody.GetMyPitcherComponent().ThrowBall(
                bases[1].position + new Vector3(0, 0.5f, 0)
            );
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("3루 던지기");
            myBody.GetMyPitcherComponent().ThrowBall(
                bases[2].position + new Vector3(0, 0.5f, 0)
            );
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (gamePlayModel.IsMyTeamBatting())
                DebugHitting();
            else
                DebugThrowBall();
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            Inning++;
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            //BallCount++;
            OutCount--;
            //AddOut();

            //DebugSwing();


            //스윙해라
            //currentBatter.Swing();
            //MoveOneBase();
        }
        if (Input.GetKeyDown(KeyCode.V))
        {

            if (gamePlayModel.IsMyTeamBatting())
            {
                Debug.Log("투수 스토프");
                _pitcherComponent.IsThrowBallStop = !_pitcherComponent.IsThrowBallStop;
            }
            else
                DebugHitting();
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (!gamePlayModel.IsMyTeamBatting())
            {
                //Player 투수 공 받기
                myBody.GetMyPitcherComponent().ForceGrab();
            }
        }

    }
    
    void DebugBaseStatus()
    {
        gamePlayModel.DebugBaseStatus(isFlyingOut);
        
    }
    void DebugHitting()
    {
        Debug.Log("디버깅용 타자 안타 함수 - 강제 타격 실행!");

        // 1. 랜덤 속력 계산
        float x = Random.Range(-1.0f, 0f);
        float y = 0.5f;
        float z = Random.Range(-1.0f, 0f);
        float power = Random.Range(5f, 5f);  //50이 홈런

        // 2. 기존 매니저의 투수 및 코루틴 제어 (이건 매니저의 일이 맞음!)
        _pitcherComponent.StopPitching();
        _ball.RemoveDefender();

        if(waitPitcherCoroutine != null)
            StopCoroutine(waitPitcherCoroutine);

        Vector3 targetSpawnPos = batterPosition.position + new Vector3(0, 2.0f, 0);
        Vector3 targetVelocity = new Vector3(x, y, z) * power;

        _ball.DebugHit(targetSpawnPos, targetVelocity);
    }
    
    private void DebugThrowBall()
    {
        _ball.CurrentState = BallState.Dead; //PitcherGetBall(); //공을 가져옴 근데 가져오는 시간이 꽤 될텐데
        _ball.DebugPitching();
    }
    
    //베이스 이동 디버그
    void DebugMoveBase(int index)
    {
        RunRunner();
        MovePlayer(bases[index].position + new Vector3(0, 1.0f, 0));
    }

    private void DebugSwing()
    {
        currentBatterComponent.Swing();
    }
    
    #endregion

}
