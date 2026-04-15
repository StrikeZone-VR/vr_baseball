using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class Baseball : MonoBehaviour
{
    #region VARIABLE
    //public float ballMass = 0.145f; // kg
    //public float ballRadius = 0.037f; // m
    //public float airDensity = 1.225f; // kg/m³
    [SerializeField] private DefenderComponent myDefenderComponent; //handling player
    [SerializeField] private float defenderDis = 0.0f;
    [SerializeField] private Bat bat;
    private XRGrabInteractable grabInteractable;
    
    [Space]
    [Header("Booleans")] 
    [SerializeField] private bool isGroundBall = false; //플라잉 아웃 판별 
    [SerializeField] private bool isZone = false;
    [SerializeField] private bool isStrike = false;
    [SerializeField] private bool isInGamePlay = false; //IsHit


    [SerializeField] private BallState _currentState = default;
    private bool hasPassedStrikeZone = true;

    public event Action<bool> OnIsInGameplayChanged;
    
    //isStrike와 isZone은 킵?

    
    [Space]
    //from GameManager
    [Header("Listening to Events")] 
    [SerializeField] private VoidEventSO allTrackingOffEvent;
    [SerializeField] private VoidEventSO addBallCountEvent;
    [SerializeField] private VoidEventSO addStrikeEvent;
    [SerializeField] private VoidEventSO foulEvent;
    [SerializeField] private VoidEventSO homerunEvent;
    [SerializeField] private VoidEventSO pitchEvent;
    [SerializeField] private VoidEventSO runSignalEvent;
    [SerializeField] private VoidEventSO backToPitcherEvent; //피쳐에게 돌아가라 => PitcherGetBall
    [SerializeField] private VoidEventSO inplayGameEvent; //from BattingSystem
    [SerializeField] private IntEventSO playAudioClipEvent; //from AudioManager

    [Header("구종 설정")] 
    [SerializeField] private PitchType selectedPitchType = PitchType.FastBall;
    private PitchData currentPitchData;

    [Header("이펙트")] 
    [SerializeField] private ParticleSystem trailEffect; // 메인 트레일
    [SerializeField] private ParticleSystem fastBallSpeedLines; // 직구 전용
    [SerializeField] private ParticleSystem curveSpinEffect; // 커브 전용
    [SerializeField] private ParticleSystem sliderSideEffect; // 슬라이더 전용
    [SerializeField] private ParticleSystem forkDropEffect; // 포크볼 전용
    [SerializeField] private LineRenderer trajectoryLine;

    [Header("참조")] 
    [SerializeField] private StrikeZone strikeZone;
    //public PitchingSystemManager pitchingSystemManager;    // 새로운 통합 시스템

    private Vector3 _targetPosition;
    
    [SerializeField] private BaseballPhysics _physics; 
    
    /// <summary> 공이 투구되었는지 확인 </summary>  <returns>투구 상태</returns>


    private float ball_accuracy_weight = 0.0f; //0~1, 1일수록 보정값이 매우 높음
    
    // 속도 추적
    const float targetSpeed = 8.0f; // 8.0 => 25? , 32 => 140
    private float debugShootTime;
    private float debugShootTime2;

    #endregion

    #region EventFunction

    private void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        InitializeComponents();
        UpdatePitchData();
    }


    void FixedUpdate()
    {
        //그냥 파울처리
        if (transform.position.y < -100f)
        {
            //그냥 홈런 범위를 넘어서면
            if (transform.position.x < 0 && transform.position.z < 0 )
            {
                Homerun();
            }
            else
            {
                Foul();
            }
        }
    }



    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnRelease);
            grabInteractable.selectEntered.RemoveListener(OnGrab); // **잡기 이벤트도 해제**
        }
    }

    #endregion

    /// <summary>
    /// 모든 투수, 수비수는 이 공으로 던져야 함
    /// </summary>
    /// <param name="start"></param>
    /// <param name="target"></param>
    /// <param name="velocity_xy"></param>
    public void ThrowBall(Vector3 start, Vector3 target, float velocity_xy, bool isPitcher) //이거 디폴트를 뭐로 할까
    {
        RemoveDefender();

        if (isPitcher)
        {
            CurrentState = BallState.Pitched;
        }
        else
        {
            CurrentState = BallState.Thrown;
        }
        _physics.ThrowBall(start, target, velocity_xy);
        
        playAudioClipEvent.RaiseEvent(0);
        PlayThrowEffects(); // 이펙트
    }
    
    //player
    private void ThrowPlayerBall()
    {
        //경기중에 던지면 알아서 return인데...
        if (_currentState == BallState.Pitched)
        {
            return;
        }
        CurrentState = BallState.Pitched;
        pitchEvent.RaiseEvent();
        
        //todo physics의 던지는 함수
        _physics.ThrowPlayerBall(strikeZone.transform.position, selectedPitchType);
        
        //_physics.PlayerThrowBall();
        
        playAudioClipEvent.RaiseEvent(0);
        PlayThrowEffects(); // 이펙트
    }
    
    
    /// //////////////////////////////////////////////////////////////////////
    //피칭 결과 알려주는 함수
    public void PitchResult()
    {
        //투수가 공을 안 던진경우
        if (_currentState == BallState.Dead || _currentState == BallState.Thrown)
        {
            Debug.Log("설마?");
            return;
        }

        //볼을 맞춘 경우
        if (IsInGamePlay)
        {
            if (!isGroundBall) //groundball or flying ball
            {
                Debug.Log("[Batting] : 안타");
                inplayGameEvent.RaiseEvent();
                IsGroundBall = true;
            }
        }
        //스윙 여부는 방망이의 회전값?
        else if (bat.IsSwing()) //스윙여부 == true => 스윙했는데 방망이를 건들지 않은 경우
        {
            playAudioClipEvent.RaiseEvent(3);
            Debug.Log("[Game] : 스트라이크1");
            addStrikeEvent.RaiseEvent();
            //backToPitcherEvent.RaiseEvent();
        }
        else if(IsStrike) //스윙 안했는데 스트라이크존에 들어간 경우
        {
            playAudioClipEvent.RaiseEvent(3);
            Debug.Log("[Game] : 스트라이크2");
            addStrikeEvent.RaiseEvent();
            //backToPitcherEvent.RaiseEvent();
        }
        else if(IsZone)//스트라이크 존에도 안 닿았고 스윙도 안했다면
        {
            Debug.Log("볼");
            addBallCountEvent.RaiseEvent();
            //backToPitcherEvent.RaiseEvent();
        }
        else
        {
            Debug.LogWarning("견제도 안했는데 이 경고가 뜬다면 문제가 있는거다.");
        }
        
        //돌아가라 => 안타, 돌아가는 상태 제외
        if(_currentState != BallState.Dead && !IsInGamePlay)
        {
            //Debug.Log("[Ball] 볼의 상태 : " + CurrentState);
            CurrentState = BallState.Dead;
        }
        //IsThrown = false;
    }

    #region PROPERTY

    public PitchType SelectPitchType
    {
        get
        {
            return selectedPitchType;
        }
        set
        {
            selectedPitchType = value;
        }
    }

    public BallState CurrentState
    {
        get => _currentState;
        set
        {
            //잡은 상태라면 놓아야 함
            if (_currentState == BallState.Grabbed)
            {
                _physics.DoNotCatchBall();
            }
            _currentState = value;

            //잠만 그러면 
            
            
            switch (_currentState)
            {
                case BallState.Pitched:
                case BallState.Thrown:
                    _physics.SetGravity(true);
                    break;
                case BallState.Grabbed: //
                    DefenderDis = 0;
                    _physics.SetVelocity(Vector3.zero);
                    _physics.SetGravity(false);
                    
                    //todo myDefender에게 frontBall시켜주기
                    _physics.CatchBall();
                    
                    allTrackingOffEvent.RaiseEvent();
                    break;
                case BallState.Dead:
                    RemoveDefender();
                    IsZone = false;
                    IsStrike = false;
                    HasPassedStrikeZone = false;
                    IsGroundBall = false;
                    IsInGamePlay = false;

                    backToPitcherEvent.RaiseEvent();
                    break;
            }
        }
    }

    public bool IsGroundBall
    {
        get => isGroundBall;
        set => isGroundBall = value;
    }

    public DefenderComponent MyDefenderComponent
    {
        get => myDefenderComponent;
        set
        {
            myDefenderComponent = value;
            
            if (myDefenderComponent)
            {
                CurrentState = BallState.Grabbed;
            }
        }
    }

    //isHit
    public bool IsInGamePlay
    {
        get => isInGamePlay;
        set 
        {
            isInGamePlay = value;
            OnIsInGameplayChanged.Invoke(value);
            _physics.PredictTrajectory(transform.position);
            CurrentState = BallState.FreeBall;
        }
    }


    public float DefenderDis
    {
        get => defenderDis;
        set => defenderDis = value;
    }

    ///공식 제거 함수
    public void RemoveDefender()
    {
        //Debug.Log("제거");
        if (!myDefenderComponent)
        {
            return;
        }

        MyDefenderComponent.RemoveBall();
        MyDefenderComponent = null;
    }

    public bool IsZone
    {
        get => isZone;
        set
        {
            isZone = value;
        }
    }

    public bool IsStrike
    {
        get => isStrike;
        set
        {
            //Debug.Log("[Baseball] : 대체 언제 호출" + value);
            isStrike = value;
        }
    }

    public bool HasPassedStrikeZone //디버깅
    {
        get => hasPassedStrikeZone;
        set => hasPassedStrikeZone = value;
    }


    public XRGrabInteractable GrabInteractable
    {
        get => grabInteractable;
    }
    
    public Vector3 GetTargetPosition()
    {
        return _targetPosition;
    }

    public float Ball_Accuracy_Weight
    {
        get
        {
            return ball_accuracy_weight;
        }
        set
        {
            ball_accuracy_weight = value;
        }
    }
    
    #endregion
    
    //구종
    #region PitchType
    public void SetPitchType(PitchType pitchType)
    {
        selectedPitchType = pitchType;
        UpdatePitchData();

        // UI 피드백
        if (trailEffect != null)
        {
            var main = trailEffect.main;
            main.startColor = currentPitchData.pitchColor;
        }
    }

    private void UpdatePitchData()
    {
        currentPitchData = PitchData.GetDefaultPitchData(selectedPitchType);
    }
    
    
    private void PlayThrowEffects()
    {
        try
        {
            // **직구일 때만 빨간색 이펙트, 메인 트레일은 비활성화**
            if (selectedPitchType == PitchType.FastBall)
            {
                // 메인 트레일 비활성화 (흰색 제거)
                if (trailEffect != null)
                {
                    trailEffect.Stop();
                    Debug.Log("🎨 MainTrailEffect(흰색) 비활성화");
                }

                // 빨간색 직구 이펙트만 활성화
                if (fastBallSpeedLines != null)
                {
                    fastBallSpeedLines.Play();
                    Debug.Log("🔥 FastBallSpeedEffect(빨간색)만 활성화");
                }
            }
            else
            {
                // 다른 구종일 때는 메인 트레일 실행
                if (trailEffect != null)
                    trailEffect.Play();
            }

            // 구종별 추가 이펙트 실행 (직구 제외)
            switch (selectedPitchType)
            {
                case PitchType.FastBall:
                    // 이미 위에서 처리됨
                    break;

                case PitchType.Curve:
                    if (curveSpinEffect != null)
                        curveSpinEffect.Play();
                    break;

                case PitchType.Slider:
                    if (sliderSideEffect != null)
                        sliderSideEffect.Play();
                    break;

                case PitchType.ForkBall:
                    if (forkDropEffect != null)
                        forkDropEffect.Play();
                    break;
            }

            // 안전하게 오디오 실행 => 던지는 소리
            
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"이펙트 재생 중 오류 발생: {e.Message}");
        }
    }

    private void StopAllEffects()
    {
        // 모든 파티클 시스템 정지
        ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();
        foreach (var particle in allParticles)
        {
            particle.Stop();
        }
    }
    
    #endregion

    #region PLAYER
    
    private void InitializeComponents()
    {
        // XRGrabInteractable 설정 확인
        if (grabInteractable != null)
        {
            OnTouchBall();
        }

        // XR 이벤트 연결
        
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.selectEntered.AddListener(OnGrab); // **잡을 때 이벤트 추가!**
    }
    
    private void OnRelease(SelectExitEventArgs args)
    {
        //Debug.Log("🎾 공을 놓았습니다! 던지기 시작!");
        Invoke(nameof(ThrowPlayerBall), 0.05f);
        
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        //Debug.Log("✋ 공을 잡았습니다! ");
    }



    public void OnTouchBall()
    {
        grabInteractable.enabled = true;
    }

    public void OffTouchBall()
    {
        grabInteractable.enabled = false;
    }
    
    #endregion
    

    public void Homerun()
    {
        //땅볼인데 홈런 범위로 넘어갔다면
        if (IsInGamePlay && !IsGroundBall)
            homerunEvent.RaiseEvent();
            
        //만약에 홈런의 두 벽을 맞은 경우 => 두 번 호출 될지도
        if(_currentState != BallState.Dead)
        {
            CurrentState = BallState.Dead;
        }
    }

    public void Foul()
    {
        if (IsInGamePlay && !IsGroundBall )
        {
            foulEvent.RaiseEvent();
        }
        if(_currentState != BallState.Dead)
        {
            CurrentState = BallState.Dead;
        }

    }

    public void Hit()
    {
        IsInGamePlay = true;
        runSignalEvent.RaiseEvent();
        playAudioClipEvent.RaiseEvent(1); //hit
        bat.Vibrate();
        
        //debugShootTime2 = Time.time - debugShootTime2;
        //Debug.Log("시간 차이(양수면 실제 시간이 오래 걸린겨) : " + (debugShootTime2 - debugShootTime));
        //Debug.Break();
    }

    public void ResetBallState(Vector3 ballResetPosition)
    {
        RemoveDefender();

        //맞겠지?
        // XR Grab Interactable 강제 활성화 (새 공이 잡힐 수 있도록)
        //ball.OffBallPhysics();

        // init ball
        _physics.SetVelocity(Vector3.zero);
        _physics.SetPosition(ballResetPosition);
    }

    public void DebugHit(Vector3 position, Vector3 velocity)
    {
        //no Defender
        RemoveDefender();
        
        CurrentState = BallState.Pitched;
        
        //친 순간은
        //땅볼과 isBack 제외 모두 체크
        Hit();

        // IsZone = true;
        // IsStrike = true;
        IsGroundBall = false;
        OnTouchBall();
    }

    public void DebugPitching()
    {
        
        if (_currentState == BallState.Pitched) return;
        
        CurrentState = BallState.Pitched;
        HasPassedStrikeZone = false;
        //IsPassing = true;
    
        pitchEvent.RaiseEvent();
        
        //정면
        Vector3 targetPosition = strikeZone.GetZone(4).position;

        IsZone = false;
        IsStrike = false;

        
        //스윙 함수
        // float time = dis / velocity;
        // //Debug.Log("time + time한 후에는 스트라이크가 (" + Time.time+ ") : "+ time);
        //
        // //Debug.Log("예측한 시간대 : " + (time - bat.RotationTime / 2 ));
        // debugShootTime = time - bat.RotationTime / 2f;
        // //0.08
        // debugShootTime2 = Time.time;
        // if(bat)
        //     StartCoroutine(StartSwingAfter(time - bat.RotationTime / 2));
        
        ThrowBall(transform.position, targetPosition, 60f, true); //내부에 계산 함수 있음
    }

}

public enum BallState
{
    Grabbed,    // 투수만 공을 잡는 상태
    Pitched,    // 투수가 던져서 허공을 날아가는 중

    Thrown,     // 수비수가 던져서 허공을 날아가는 중
    FreeBall,   //인 플레이 기능으로 설정
    Dead        // 땅에 닿거나, 파울, 홈런 등으로 플레이가 종료된 상태
                // => 대기동안 주자들은 참는 상황. 돌아갈 준비
}
