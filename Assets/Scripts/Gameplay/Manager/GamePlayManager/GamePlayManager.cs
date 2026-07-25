using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public partial class GamePlayManager : GameManager
{
    #region VARIABLES
    [Header("Debug")]
    [SerializeField] protected ActionBasedContinuousMoveProvider moveProvider; //debug용
    [SerializeField] private Color _myTeamColor;
    [SerializeField] private Color _yourTeamColor;
    [SerializeField] private Vector3 debugShootVector;
    [Tooltip("50이 홈런, 25가 안타, 10이 내야 땅볼")]
    [SerializeField] private float debugShootPower;
    private float player_y = 1.7f; //1.7
    
    [Space]
    
    [Header("Objects")]
    [SerializeField] private Player[] defenders; // pitcher => 0
    [SerializeField] private Transform[] bases;
    [SerializeField] private Transform init_base;
    [SerializeField] private MyBody myBody; //플레이어 타자
    [SerializeField] private Transform[] rightBatterPositionTemp; //4개
    
    private PitcherComponent _aiPitcherComponent;
    
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
    [SerializeField] private VoidEventSO walkEvent;
    [SerializeField] private VoidEventSO addOutEvent;

    [SerializeField] private bool canBackRunner = false; //이거 안타 말고도 그냥 파울이나 돌아가는 것도 이거 true임
    
    [SerializeField] private GamePlayModel gamePlayModel;
    [SerializeField] private BattingModel battingModel;

    private Coroutine waitPitcherCoroutine;
    
    private bool isFlyingOut = false;
    private bool canGetBall = true; //호출 위치를 어디에 해야할지 모르겠네
    
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

        //씬 언로드 후에도 레일이 남아 스테일 좌표로 플레이어를 끌지 않게 해제
        //(MyXROriginManager는 Persistent 씬이라 이 매니저보다 오래 산다)
        if (_xrOriginManager != null) _xrOriginManager.ClearMoveRail();
    }

    #endregion

    //todo 저거 프로퍼티에서 함수 꼬이는 것좀 어떻게 개선좀 시키자
    //여기라인부터는 메인함수만 Start, Update, Dead함수 이후
    //내가 이 Start, Update만 봐도 구조를 이해하게 코드를 짜야한다.
    protected override void Start()
    {
        base.Start();
        
        Init();
        Inning = 0; //changed
    }

    #region START

    /// <summary>
    /// 초기 설정
    /// </summary>
    void Init()
    {
        gamePlayModel.Init();
        battingModel.Init();
        
        _aiPitcherComponent = GetDefenderComponent(0) as PitcherComponent;
        
        SetScore(0, 0);
        SetScore(1, 0);

        SetMyBodyCamera();

        gamePlayModel.SetPanel(_baseStatusPanel);
        
        //루수(1·2·3루)의 수비 앵커를 해당 베이스 정중앙에 고정한다(B-2).
        //ㄴ 송구는 항상 베이스 중심(bases[b])으로 오는데, 루수가 큰 베이스 트리거 가장자리에 서서
        //   못 받던 버그. 서는 곳=던지는 곳이 되도록 앵커를 베이스로 맞춘다. (판정은 BasemanComponent가 거리로)
        //ㄴ defenders[b+1] = base_index b의 루수 (ThrowToBase 매핑과 동일). 포수(index 4)는 공받기/수비 두 위치라 제외.
        // for (int b = 0; b <= 2; b++)
        // {
        //     DefenderComponent dc = GetDefenderComponent(b + 1);
        //     if (dc != null) dc.SetDefenseAnchor(bases[b]);
        // }
    }

    #endregion
    
    private void Update()
    {
#if UNITY_EDITOR
        DebugInput();
#endif
        
        //수비
        BeforePitcherGetBall(); 
        TrackingBall(); 
            
        //(GamePlayManager.BatterBox.cs)
        UpdateBatterBoxSwitch(); //타자 모드: 왼손 조이스틱 플릭으로 좌/우 타석 전환 
        UpdateRunnerRail();      //주자 달리기: XR 오리진을 베이스라인 복도에 구속
    }
    
    //Dead 함수
    //backToPitcherEvent => 이거 던질 곳 없거나 안타치는 순간 겹친다 그냥
    /// <summary>
    /// 애초에 공을 받는 함수. **직접 호출하지 마라** _ball.Dead함수로 호출해라
    /// 메인함수
    /// </summary>
    protected override void PitcherGetBall()
    {
        canGetBall = false;
        isFlyingOut = false;
        GameLog.Defend($"[Game] : ResetBall");
        
        //포수 위치 변환
        CatcherComponent catcherComponent = GetDefenderComponent(4) as CatcherComponent;
        catcherComponent.DefendIndex = 0;

        //디버그 베이스 세팅보여주기
        DebugBaseStatus();
        gamePlayModel.SaveBeforeStatus();
        
        if (gamePlayModel.PlayerIsBatterMode())
            PitcherGetBall_BatterMode();
        else
            PitcherGetBall_PitcherMode();
    }
    
    //플레이어 주자가 베이스를 밟은 경우 =>
    //todo : 최대한 발생하는 함수를 여기에 적자. ex) 방망이로 히트한 경우, 
    //! Defense의 OutRunner => 베이스 아웃 기준 (116줄)
    
    
    //근데 파울마냥 너무 짜잘한 거는 아래에 넣고

    //아래 함수는 최대한 안 보는게 목적 느낌으로 => 이름만 봐도 구조를 알수있게
    #region GAMEPLAY
    
    /// <summary>
    /// 매 이닝 초기화될 때마다 실행되는 함수
    /// </summary>
    private void ChangedInning()
    {
        //어차피 OutCount에 Ball, Strike가 초기화...?
        OutCount = 0;
        //ㄴ BallCount = 0;
        //ㄴ Strike = 0;

        ClearRunners();

        //지난 이닝의 복귀 플래그가 새 이닝에서 오발되지 않게 초기화.
        //반드시 ClearRunners "뒤"에서: ClearRunners → 플레이어 MyBatterComponent.OutPlayer(true)
        //→ moveBatterEvent → OnCanBackRunner가 canBackRunner를 다시 true로 세운다.
        canBackRunner = false;

        ChangedColor();
        
        //false
        //change 
        if (gamePlayModel.PlayerIsBatterMode())
        {
            StartBatterMode();
        }
        else
        {
            StartPitcherMode();
        }
    }

    
    /// <summary>
    /// 이닝 바뀔때 색 설정
    /// </summary>
    private void ChangedColor()
    {
        //색 설정
        Color defend_color;
        
        //타자
        if (gamePlayModel.PlayerIsBatterMode())
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

    void MovePlayer(Vector3 position)
    {
        moveOriginEvent.RaiseEvent(position);
    }
    
    void RotatePlayer(Vector3 rotate)
    {
        rotateOriginEvent.RaiseEvent(rotate);
    }
    
    private void OnTouchBall()
    {
        _ball.OnTouchBall();
    }

    private void SetPlayerMoveMode(bool isMove)
    {
        //GameLog.RunnerLog($"Player  : isMove = " + isMove);

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
        _setPlayerMoveMode.RaiseEvent(isMove);
    }
    

    #endregion
    
    #region BATTER

    /// <summary>
    /// 주자/타자 파괴 단일 창구: 리스트 제거 → current 참조 해제 → 파괴.
    /// Destroy는 프레임 끝까지 지연되므로 파괴 '전에' 참조를 정리해야
    /// 같은 프레임의 PitcherGetBall_PitcherMode 타자 생성 판단이 안 깨진다. (ReplaySO_1_6 66.024초 타자 미생성 버그)
    /// ㄴ currentBatterComponent == batter 비교가 "타석 출신 주자인가" 판단을 겸한다.
    ///    전제: currentBatterComponent는 항상 가장 최근 타자를 가리킨다 (RunRunner가 리스트에 넣어도 유지).
    /// </summary>
    private void RetireBatter(BatterComponent batter, bool isMove = true)
    {
        gamePlayModel.RemoveRunnerInstance(batter); //파괴된 참조가 리스트에 남아있는 경우도 여기서 정리
        if (currentBatterComponent == batter) currentBatterComponent = null;
        if (!batter) return; //이미 파괴됐으면 참조 정리까지만 (이중 파괴 예외 → 후속 AddOut 유실 방지)
        batter.OutPlayer(isMove);
    }

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
        //ㄴ 원래는 !=로 해야하지만 inning이 바뀐 후라 !를 안 썼다.  &&  !gamePlayModel.PlayerIsBatterMode()
        if (currentBatterComponent)
        {
            //현재 타석 제거
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
        
        MoveBase();


        //타자모드
        //주자들 달리는 신호
        if (!gamePlayModel.PlayerIsBatterMode())
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
    /// "다음 타자 등장" = 한 타석 시작. 양 모드 공통으로 카운트를 초기화한다.
    /// 타자모드: 다음 타자 = 플레이어 본인 → 타석으로 시점 복귀(TranslateBattingView).
    /// 투수모드: 타석에 서있는 AI 타자 생성. 아웃, 홈런, 사구 각각 있음 (ㄴ 파울은 아님)
    /// </summary>
    /// <param name="isAI"></param>
    /// <param name="base_index"></param>
    /// <returns></returns>
    private BatterComponent NextBatter(int base_index = 0)
    {
        //어차피 아웃되거나 안타 확정될때 초기화 해야함
        ResetCount();

        //타자모드: AI를 생성하지 않고 플레이어 시점만 타석으로 복귀
        if (gamePlayModel.PlayerIsBatterMode())
        {
            StartCoroutine(TranslateBattingView()); //끝에서 currentBatterComponent = myBody 배터로 세팅됨
            return myBody.GetMyBatterComponent();
        }

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
        Debug.LogWarning("이게 플레이어가 아웃될때 생성하면 안된다.");
        Player batter = Instantiate(batterPrefab, batterCreatePosition);
        BatterComponent batterComponent = batter.GetComponent<BatterComponent>();
        
        //Set
        batterComponent.SetBases(bases, init_base);
        batter.SetBall(_ball);
        batterComponent.SetBat(_bat);

        //타자모드
        if (gamePlayModel.PlayerIsBatterMode())
        {
            batter.SetShirtColor(_myTeamColor);
        }
        else //투수모드
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
        
        //batting mode
        if(gamePlayModel.PlayerIsBatterMode())
        {
            //그냥 AI를 생성해서 저쪽에 넣는다.
            gamePlayModel.AddRunner(CreateBatter());
            Strike = 0;
            gamePlayModel.MoveBaseRunner();
        }
        else //pitchermode
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
        
        //되돌아가는데 점수를 얻은 경우 => 웬만하면 안 생기긴 하는데
        if (gamePlayModel.BeforeScore != gamePlayModel.GetScore())
        {
            if (!gamePlayModel.PlayerIsBatterMode())
            {
                //runners의 insert 맨 앞 
                gamePlayModel.InsertRunner(CreateBatter(0));
                //어차피 baseindex는 나중에 설정할거임
            }
        }
        
        currentBatterComponent.IsMove = false;
        gamePlayModel.FoulRollbackBeforeStatus(); //정보만 바뀜

        if (gamePlayModel.PlayerIsBatterMode())
        {
            StartCoroutine(TranslateBattingView());
            canBackRunner = true;
        }
        else //AI타자가 파울이면?
        {
            gamePlayModel.GetLastRunner().SetBaseIndexPosition(0); //홈으로 이동
        }

        //맨 뒤 주자 제거
        gamePlayModel.RemoveLastRunner();
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
        if (!gamePlayModel.PlayerIsBatterMode())
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
        GameLog.Log("타자 Mode On");

        pitchingController.EndPitchingGame();
        defenders[0].gameObject.SetActive(true); //pitcher로 하면 mybody도 true가 될 수 있으니까
        
        //투수 AI 세팅
        _aiPitcherComponent.IsThrowBallStop = false;

        //GetDefenderComponent(0).SetMyBall(_ball);
        //_ball.CurrentState = Dead가 backToPitcherEvent→PitcherGetBall→WaitingBackToPitcher를 '동기적으로' 호출하고,
        //그 코루틴이 myBody.GetMyBatterComponent()를 쓴다. 그래서 Dead로 만들기 전에 먼저 타자 역할로 스위칭해야 null(NRE)이 안 난다.
        myBody.SetBatterComponent();
        _ball.CurrentState = BallState.Dead;

        SetPlayerMoveMode(true); //StartPitcherMode에서 false로 잠갔던 걸 복원 (비대칭 방지)

        StartCoroutine(TranslateBattingView());
        //TranslateBattingView();
    }
    
    //battingmode
    private void PitcherGetBall_BatterMode()
    {
        //안전망: 플레이가 비정상 경로(땅볼 홈런존, 디버그 킬, 이벤트 미발송 홈런 등)로 죽어
        //정상 복귀 루트(BeforePitcherGetBall의 canBackRunner 블록)를 우회하면 플레이어가 주자로 남는다.
        //ㄴ 리셋 시점 불변식: "플레이어는 주자가 아니다". 깨져 있으면 가던 베이스 안착 인정 후 타석 복귀. (ReplaySO_1_6 145.8초 버그)
        MyBatterComponent myBatter = myBody.GetMyBatterComponent();
        if (gamePlayModel.GetRunners().Contains(myBatter))
        {
            myBatter.IsMove = false;
            int settledBase = myBatter.BaseIndex + 1; //가던 베이스 안착 인정
            myBatter.BaseIndex = settledBase; //4(홈인)면 setter가 득점 처리 (IntoHome→RetireBatter가 리스트 정리까지)
            if (settledBase < 4)
            {
                TransformMyBodyToBatter(); //AI 대체 주자 생성 + 리스트 교체 + myBatter.BaseIndex=0
                //대체 주자는 플레이어의 현재 위치(베이스라인 중간)가 아니라 안착한 베이스 위로
                gamePlayModel.GetLastRunner().SetBaseIndexPosition(settledBase);
            }
            StartCoroutine(TranslateBattingView()); //타석 시점 복귀
        }

        waitPitcherCoroutine = StartCoroutine(WaitingBackToAIPitcher());
        //안에 canGetBall 변수가 있음.
    }

    /// <summary>
    /// BatterMode에서 공 가져오는 함수
    /// </summary>
    /// <returns></returns>
    IEnumerator WaitingBackToAIPitcher()
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
    

    //타석 이동
    IEnumerator TranslateBattingView()
    {
        fadeEvent.FadeOut(FADE_WAIT_TIME); //이동하기 전
        yield return new WaitForSeconds(FADE_WAIT_TIME);

        //Vector3 movePosition = new Vector3(0, player_y, 0);
        //좌/우 타석 선택(_isLeftBatterBox)을 반영해 위치·시선 계산 (GamePlayManager.BatterBox.cs)
        //ㄴ 우타석이면 기존과 동일하게 rightBatterPositionTemp 평균, 좌타석이면 홈→2루 축 반사
        GetBatterBoxPose(_isLeftBatterBox, out Vector3 movePosition, out Vector3 rotateVector);
        MovePlayer(movePosition);
        RotatePlayer(rotateVector);

        fadeEvent.FadeIn(FADE_WAIT_TIME); //이동하고 나서
        currentBatterComponent = myBody.GetMyBatterComponent();

        //타석에는 움직이지 마라 todo
        //SetPlayerMoveMode(false);
        SetPlayerMoveMode(false); //↑ todo 구현: 타석 도착 → 자유 이동 잠금. 타구가 인플레이 되면
                                  //  OnIsInGameplayChanged → SetPlayerMoveMode(true)로 자동 해제된다.
                                  //  타석 전환은 왼손 조이스틱 플릭(UpdateBatterBoxSwitch)으로만.
    }
    
    //override
    protected override void SetVelocityToText(float velocity)
    {
        battingController.SetVelocityToText(velocity);
        pitchingController.SetVelocityUI(velocity); 
    }
    
    protected override void WaitPitchingToText(int time)
    {
        battingController.WaitPitchingToText(time);
        if (time == 3)
        {
            playAudioClipEvent.RaiseEvent(2);
        }
    }
    
    //주자가 돌아오는 함수
    void TransformMyBodyToBatter()
    {
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
        _aiPitcherComponent.IsThrowBallStop = true;
        pitchingController.StartPitchingGame();
        
        _ball.CurrentState = BallState.Dead;
        
        //todo
        //SetPlayerMoveMode(false);
            
        defenders[0].gameObject.SetActive(false);
        myBody.SetPitcherComponent();
        
        //일단 주자가 두 번 생기는 오류때문에 막아놓음
        //currentBatterComponent = NextBatter();

        //방망이 위치 Vector3(-0.660000026,1.37,0.150000006) 여기로
        //방망이 중력, rotation position 얼리기
        //; //OutCount에 넣었으면 안 넣어도 됨
        
        StartCoroutine(TranslatePitchingView());
    }
    
    //pitchermode
    private void PitcherGetBall_PitcherMode()
    {
        //안전망: 비정상 경로(땅볼 홈런존, 디버그 킬 등)로 플레이가 죽어 달리던 AI 주자가 남아있으면 가던 베이스에 안착 인정
        //ㄴ 복사본 순회: 홈 안착(득점)이면 BaseIndex setter→IntoHome→RetireBatter가 원본 리스트를 수정하기 때문
        //ㄴ 아래 타자 생성 판단보다 먼저: 타자 주자가 여기서 안착(IsMove=false, BaseIndex>=1)되면 새 타자 생성 조건에 걸린다
        foreach (BatterComponent r in new List<BatterComponent>(gamePlayModel.GetRunners()))
        {
            if (r && r.IsMove)
            {
                r.SetBaseIndexPosition(r.BaseIndex + 1);
            }
        }

        //주자 생성
        //기본 조건 : 투수모드
        //그냥 주자가 안 움직이고 index가 1 이상이면 Next? => 타석 생성
        //또는 current가 Null이면
        if (!currentBatterComponent
            || (!currentBatterComponent.IsMove && currentBatterComponent.BaseIndex >= 1)) 
            //파울이나 스트라이크 방지용 (즉, 초반이나 아웃일때만 생성)
        {
            currentBatterComponent = NextBatter();
        }
        else
        {
            GameLog.RunnerLog("주자가 남아있! : " + currentBatterComponent);
        }
        pitchingController.PlayerPitcherResetBall(); //AI투수에게 공 주는 함수
        canGetBall = true;
    }
    
    IEnumerator TranslatePitchingView()
    {
        fadeEvent.FadeOut(FADE_WAIT_TIME);
        yield return new WaitForSeconds(FADE_WAIT_TIME);
        
        Vector3 movePosition = new Vector3(-13.46f, player_y, -13.46f);
        Vector3 rotateVector = new Vector3(1, 0, 1);
        MovePlayer(movePosition);
        RotatePlayer(rotateVector);
        
        fadeEvent.FadeIn(FADE_WAIT_TIME);
    }
    
    #endregion 
    
    #region PROPERTY
    public int Inning
    {
        get { return gamePlayModel.Inning; }
        set
        {
            //GameReady에서 선택한 이닝 수가 있으면 그걸 우선, 아니면 기본 9이닝(DEFAULT_MAX_INNING_COUNT=18)
            int maxInning = gamePlayModel.MaxInning; 
            if (value >= maxInning)
            {
                sceneEventSO.RaiseEvent(gameResultScene); //최종 결과씬 이동
                //todo 정보 전달해야함 => 어 근데 이미 전달한 거 아닌가?
                return;
            }
            
            gamePlayModel.Inning = value;
            ChangedInning();

            gamePlayController.SetInningText(value);
        }
    }
    
    public int OutCount
    {
        get { return gamePlayModel.OutCount; }
        set
        {
            GameLog.BallLog("아웃");
            ResetCount();
            
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
    
    private void AddOut()
    {
        OutCount++;
        if (addOutEvent != null) addOutEvent.RaiseEvent();
    }
    
    public override int Strike //나중에 battingSystem에서는 override
    {
        get { return baseballModel.Strike; }
        set
        {
            //out
            if (value >= BaseballModel.MAX_STRIKE_COUNT)
            {
                //판독용 카운트 전이 로그: 리플레이에서 "왜 아웃?"이 로그만으로 읽히게
                GameLog.BallLog("삼진 아웃");
                value = 0;

                DeleteRunner();
                AddOut();

                //batter
                //투수모드 삼진 때 플레이어(투수)가 타자 시점으로 끌려가는 버그라 제거.
                //ㄴ 다음 AI 타자는 공 리셋 → PitcherGetBall → NextBatter()가 생성한다.
                //ㄴ TranslateBattingView는 끝에서 currentBatterComponent를 플레이어로 바꿔버려
                //   NextBatter 생성 조건(!currentBatterComponent)까지 깨뜨리고 있었다.
                // if (!gamePlayModel.PlayerIsBatterMode())
                // {
                //     StartCoroutine(TranslateBattingView());
                // }
            }
            
            GameLog.BallLog("스트라이크");
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
                GameLog.BallLog("볼넷");
                value = 0;

                //AddBaseStatus();
                MoveOneBase();
                gamePlayModel.SaveBeforeStatus();
                if (walkEvent != null) walkEvent.RaiseEvent();
            }
            
            GameLog.BallLog("볼.");

            baseballModel.BallCount = value;
            gamePlayController.SetUIGameStatusIndex(0, value);
            battingController.SetBallCountToText(value);
        }
    }


    /// <summary>
    /// 새 타석 카운트(볼/스트라이크) 초기화. NextBatter("다음 타자 등장")가 양 모드 공통으로 부른다.
    /// (TranslateBattingView에 넣지 말 것: 파울 롤백도 그 함수를 쓰는데 파울은 카운트를 유지해야 함)
    /// </summary>
    private void ResetCount()
    {
        BallCount = 0;
        Strike = 0;
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
        GameLog.RunnerLog("밟아서 홈인, 점수 얻음");
        BatterComponent batterComponent = gamePlayModel.RemoveRunner(3);
        RetireBatter(batterComponent); //current 참조 해제 + null 가드 포함 (파괴 단일 창구)
        AddScore(1);
    }

    protected override void Foul()
    {
        //만약에 점수를 냈다면?
        RollbackBeforeStatus();
        ++FoulCount;

        GameLog.HitLog("파울");
        
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
        GameLog.HitLog("홈런");
        AddScore(gamePlayModel.GetRunnerCount());
        ClearRunners();

        //타자모드면 시점 복귀, 투수모드면 AI 타자 생성(주자 나와야 함). 둘 다 카운트 초기화 포함
        currentBatterComponent = NextBatter();
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
        _setBodyEvent.RaiseEvent(myBody);
    }

    #endregion
    
}
