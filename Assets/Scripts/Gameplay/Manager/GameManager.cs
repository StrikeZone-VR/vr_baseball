using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Serialization;

//게임 시작할때 실행되는 GameManager
public class GameManager : MonoBehaviour
{
    [SerializeField] protected Baseball _ball; //일단 PitchingBallController도 여깄음

    [Header("Broadcasting on EventChannels")] 
    [SerializeField] private VoidEventSO addBallCountEvent; //to Baseball
    [SerializeField] private VoidEventSO strikeEvent; // toStrikeZone, batter
    [SerializeField] private VoidEventSO foulEvent; // toStrikeZone
    [SerializeField] private VoidEventSO homerunEvent; // toStrikeZone

    [Space]
    [SerializeField] private VoidEventSO backToPitcherEvent; //?
    [SerializeField] private FloatEventSO getVelocityEvent; //ball?
    [SerializeField] private IntEventSO waitPitcherEvent;

    //가져오기
    [Header("Listening to EventChannels")] 
    [SerializeField] protected Vector3EventSO moveOriginEvent;
    [SerializeField] protected Vector3EventSO rotateOriginEvent;
    
    [SerializeField] protected SceneEventSO sceneEventSO;
    [SerializeField] protected IntEventSO playAudioClipEvent;

    [SerializeField] protected BaseballModel baseballModel;

    
    protected virtual void OnEnable()
    {
        addBallCountEvent.onEventRaised += AddBallCount;
        strikeEvent.onEventRaised += AddStrike;
        foulEvent.onEventRaised += Foul;
        homerunEvent.onEventRaised += Homerun;

        backToPitcherEvent.onEventRaised += PitcherGetBall;
        getVelocityEvent.onEventRaised += SetVelocityToText;
        waitPitcherEvent.onEventRaised += WaitPitchingToText;
    }

    protected virtual void OnDisable()
    {
        addBallCountEvent.onEventRaised -= AddBallCount;
        strikeEvent.onEventRaised -= AddStrike;
        foulEvent.onEventRaised -= Foul;
        homerunEvent.onEventRaised -= Homerun;

        backToPitcherEvent.onEventRaised -= PitcherGetBall;
        getVelocityEvent.onEventRaised -= SetVelocityToText;
        waitPitcherEvent.onEventRaised -= WaitPitchingToText;
    }

    protected virtual void Start()
    {
        baseballModel.Init();
    }

    #region PROPERTY

    public virtual int Strike //나중에 battingSystem에서는 override
    {
        get => baseballModel.Strike;
        set => baseballModel.Strike = value;
    }
    
    public virtual int BallCount //나중에 battingSystem에서는 override
    {
        get => baseballModel.BallCount;
        set => baseballModel.BallCount = value;
    }

    protected virtual void AddBallCount()
    {
        BallCount++;
    }

    protected virtual void AddStrike()
    {
        Strike++;
    }

    //pitcher에서는 안 쓰일거다. batter와 gameplay에서 쓰인다
    protected virtual void Foul() { }
    protected virtual void Homerun() {}

    #endregion

    //Gameplay에서는 알아서 사용
    protected virtual void PitcherGetBall() { }
    protected virtual void SetVelocityToText(float velocity) {}

    //PitchingManager에서는 안 쓰일 예정. 투수 대기 함수
    protected virtual void WaitPitchingToText(int time) { }


    #region REPLAY

    //ReplayRecorder가 매니저 종류(GamePlay / Batting / Pitching)와 무관하게 녹화할 수 있도록
    //공통 진입점을 여기 둔다. 예전엔 레코더가 GamePlayManager를 직접 찾아서, 단독 씬에서는
    //EnsureResolved()가 계속 false → 한 프레임도 녹화되지 않았다.
    //ㄴ GamePlayManager는 주자/수비/전광판까지 있으니 아래를 전부 override 한다.
    //ㄴ 단독 씬은 기본 구현만으로 공·배트·양손·머리·볼카운트가 녹화된다.

    private static readonly Transform[] EMPTY_TRANSFORMS = new Transform[0];
    private Bat _cachedBat;

    public virtual Baseball Ball => _ball;

    /// <summary>
    /// 배트. 단독 씬 매니저는 배트를 필드로 안 들고 있어서 씬에서 한 번 찾아 캐시한다.
    /// (못 찾으면 null이고, 레코더가 null을 이미 걸러낸다)
    /// </summary>
    public virtual Bat Bat
    {
        get
        {
            if (_cachedBat == null) _cachedBat = FindAnyObjectByType<Bat>();
            return _cachedBat;
        }
    }

    /// <summary> 주자 transform. 단독 씬엔 주자 개념이 없어서 빈 배열. </summary>
    public virtual Transform[] GetRunnerTransforms() => EMPTY_TRANSFORMS;

    /// <summary> 수비수 transform. 단독 씬엔 고정 수비 배열이 없어서 빈 배열. </summary>
    public virtual Transform[] GetDefenderTransforms() => EMPTY_TRANSFORMS;

    /// <summary>
    /// 상태 스냅샷. 기본 구현은 공과 볼/스트라이크 카운트만 채운다.
    ///
    /// 반환 타입을 GamePlayManager.StatusSnapshot 그대로 쓰는 이유:
    /// 이미 녹화된 .asset들이 이 타입 이름으로 직렬화돼 있어서, 타입을 밖으로 옮기면
    /// 기존 리플레이 파일을 못 읽게 된다. 부모가 자식 타입을 참조하는 모양이 되지만
    /// 기록 호환성을 깨는 것보다 낫다고 판단했다.
    /// </summary>
    public virtual GamePlayManager.StatusSnapshot CaptureStatus()
    {
        GamePlayManager.StatusSnapshot s = new GamePlayManager.StatusSnapshot();

        s.hasBall = _ball != null;
        if (s.hasBall)
        {
            s.ballState    = _ball.CurrentState;
            s.isInGamePlay = _ball.IsInGamePlay;
            s.isGroundBall = _ball.IsGroundBall;
            s.isZone       = _ball.IsZone;
            s.isStrike     = _ball.IsStrike;
            s.defenderDis  = _ball.DefenderDis;
            s.defenderName = $"{_ball.MyDefenderComponent}";
            s.ballPos      = _ball.PhysicsPosition;
            s.ballVel      = _ball.Velocity;
            s.useGravity   = _ball.UseGravity;
        }

        //단독 씬에도 있는 값
        s.ballCount   = baseballModel != null ? baseballModel.BallCount : 0;
        s.strikeCount = baseballModel != null ? baseballModel.Strike : 0;

        //재생 쪽 포맷 함수가 null 배열을 건드리지 않도록 빈 값으로 채워둔다
        s.awayInnings   = new int[GamePlayModel.MAX_DISPLAY_INNING];
        s.homeInnings   = new int[GamePlayModel.MAX_DISPLAY_INNING];
        s.runners       = new GamePlayManager.RunnerSnapshot[0];
        s.defenders     = new GamePlayManager.DefenderStateSnapshot[0];
        s.throwBallStop = "null";
        s.batterName    = "-";
        if (s.defenderName == null) s.defenderName = "-";

        return s;
    }

    #endregion
}
