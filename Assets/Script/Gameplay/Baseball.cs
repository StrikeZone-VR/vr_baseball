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
    //public float ballMass = 0.145f; // kg
    //public float ballRadius = 0.037f; // m
    //public float airDensity = 1.225f; // kg/m³

    [SerializeField] private Defender myDefender; //handling player
    private Rigidbody _rigidbody;
    private XRGrabInteractable grabInteractable;

    [SerializeField] private float defenderDis = 0.0f;
    [SerializeField] private Bat bat;
    
    [Space]
    [Header("Booleans")] 
    [SerializeField] private bool isGroundBall = false;
    [SerializeField] private bool isBatTouch = false;
    [SerializeField] private bool isPassing = false;
    [SerializeField] private bool isZone = false;
    [SerializeField] private bool isStrike = false;
    [SerializeField] private bool isThrown = false;
    [SerializeField] private bool isBack = false;
    
    
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
    [SerializeField] private VoidEventSO backToPitcherEvent; //
    [SerializeField] private VoidEventSO inplayGameEvent; //from BattingSystem
    [SerializeField] private FloatEventSO getVelocityEventSO; //from BattingSystem
    [SerializeField] private IntEventSO playAudioClipEvent; //from AudioManager

    [Header("물리 설정")] 
    public AnimationCurve throwSmoothingCurve = AnimationCurve.Linear(0, 0, 1, 1);

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

    [Header("참조")] [SerializeField] private StrikeZone strikeZone;
    //public PitchingSystemManager pitchingSystemManager;    // 새로운 통합 시스템

    [Header("Debug")] 
    [SerializeField] private float _debugVelocity;
    private Vector3 _targetPosition;

    /// <summary> 공이 투구되었는지 확인 </summary>  <returns>투구 상태</returns>

    // 사용하지 않는 변수들 제거: isCurveActive, throwTime, curveTimer
    private Vector3 velocityXY; // 실제 목표 위치

    public readonly float MAGNUS = 60.0f; //100 기준
    private float ball_accuracy_weight = 0.0f; //0~1, 1일수록 보정값이 매우 높음
    
    // 속도 추적
    const float targetSpeed = 8.0f; // 8.0 => 25? , 32 => 140
    private float beforeTime = 0f;

    #region EventFunction

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        InitializeComponents();
        UpdatePitchData();
    }

    private void Update()
    {
        CalTrajectory();
    }

    void FixedUpdate()
    {
        //커브
        if (_rigidbody != null)
        {
            //스트라이크 판정
            float dis = _rigidbody.velocity.magnitude * Time.deltaTime;
            Ray ray = new Ray(this.transform.position, _rigidbody.velocity);

            if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit rayHit, dis))
            {
                //Debug.DrawRay(ray.origin, ray.direction, Color.red, 0.5f);
                if(rayHit.transform.CompareTag("StrikeZone"))
                {
                    IsStrike = true;
                }
            }

            //curve
            if (isThrown && SelectPitchType == PitchType.Curve)
            {
                float deltaTime = Time.time - beforeTime;
                beforeTime = Time.time;
                _rigidbody.velocity += new Vector3(0, -deltaTime * velocityXY.magnitude / 100 * MAGNUS,0);
            }
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("VelocityZone"))
        {
            PrintBallVelocity();
        }

        //into Strike Zone
        if (collider.gameObject.CompareTag("StrikeZone"))
        {
            IsZone = true;
            IsStrike = true;
            //Debug.Log("스트라이크 : " + IsStrike);
            //addStrikeEvent.RaiseEvent();
        }
        //Debug.Log("건드린 물체 : " + collider.gameObject.name + " - " + collider.gameObject.tag);
        //Debug.Log("모드 : " + _rigidbody.collisionDetectionMode);
        //foul
        if (collider.CompareTag("Foul"))
        {
            if (IsBatTouch && isThrown && !IsGroundBall )
            {
                foulEvent.RaiseEvent();
            }
            if(!isBack)
            {
                isBack = true;
                backToPitcherEvent.RaiseEvent();
            }
        }

        //homerun
        if (collider.CompareTag("Homerun"))
        {
            if (isBatTouch && !IsGroundBall)
                homerunEvent.RaiseEvent();
            
            //만약에 홈런의 두 벽을 맞은 경우 => 두 번 호출 될지도
            if(!isBack)
            {
                isBack = true;
                backToPitcherEvent.RaiseEvent();
            }
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.CompareTag("BallZone") && !IsZone)
        {
            IsZone = true;
            //addBallCountEvent.RaiseEvent();
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Ground") || collision.collider.CompareTag("Base"))
        {
            //공 잡으면 계속 땅을 터치함. => 왜 그런지는 모름
            if (myDefender == null)
            {
                PitchResult();
                
                IsPassing = false;
                IsThrown = false;

                if (isBatTouch)
                {
                    IsGroundBall = true;
                    //throw ball but swing miss
                }
            }
        }

        //in play game
        if (collision.collider.CompareTag("Bat"))
        {
            //두번 건드리는거 방지
            if (IsBatTouch)
            {
                return;
            }
            
            playAudioClipEvent.RaiseEvent(1); //hit
            
            //Debug.Log("인 플레이 게임");
            IsBatTouch = true;
            //IsThrown = true;

            // 공의 Rigidbody 컴포넌트 가져오기
            Rigidbody batRb = collision.gameObject.GetComponent<Rigidbody>();

            //force ball
            if (batRb != null)
            {
                //계산이 잘못된 듯?
                // 충돌 방향 계산 (배트에서 공으로의 방향)
                Vector3 hitDirection = (collision.GetContact(0).point - transform.position).normalized;
                Bat bat = collision.transform.GetComponent<Bat>();
                
                //배트 터치
                float speed = bat.GetSwingSpeed();
                bat.Vibrate();
                

                //가중치 4배 * speed * 4f
                this._rigidbody.AddForce(hitDirection * speed * 4, ForceMode.Impulse);
                this._rigidbody.useGravity = true;
            }

            //signal => IsBatTouch로 옮김
            //runSignalEvent.RaiseEvent();
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.collider.CompareTag("Bat"))
        {
            Debug.Log("히토! 공 속도 :" + transform.GetComponent<Rigidbody>().velocity);
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

    public void ThrowBall(Vector3 force)
    {
        RemovePlayer();
        IsPassing = true;

        //rotation zero
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.velocity = force;
        beforeTime = Time.time;
        velocityXY = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z);
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
    public bool IsThrown
    {
        get => isThrown;
        set
        {
            isThrown = value;
            
            //true면 던지는 경우.
            if (isThrown)
            {
                isBack = false;
            }
            //Debug.Log(isThrown);
        }
    }

    public bool IsPassing
    {
        get => isPassing;
        set => isPassing = value;
    }

    public bool IsGroundBall
    {
        get => isGroundBall;
        set => isGroundBall = value;
    }

    public bool IsBatTouch
    {
        get => isBatTouch;
        set
        {
            Debug.Log("isBatTouch: " + value);
            isBatTouch = value;
            if(isBatTouch)
                runSignalEvent.RaiseEvent();

        } 
            
    }

    public Defender MyDefender
    {
        get => myDefender;
        set
        {
            myDefender = value;
            if (myDefender)
            {
                DefenderDis = 0;
                IsPassing = false;
                allTrackingOffEvent.RaiseEvent();
            }
        }
    }

    public float DefenderDis
    {
        get => defenderDis;
        set => defenderDis = value;
    }

    //공식 제거 함수
    public void RemovePlayer()
    {
        if (!myDefender)
        {
            return;
        }

        myDefender.RemoveBall();
        myDefender = null;
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
            isStrike = value;
        }
    }

    public void SetVelocity(Vector3 velocity)
    {
        if (_rigidbody != null)
        {
            _rigidbody.velocity = velocity;
            _rigidbody.angularVelocity = velocity;
        }
    }
    public void SetPosition(Vector3 position)
    {
        if (_rigidbody != null)
        {
            _rigidbody.position = position;
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

        // **충돌 감지 강화 설정**
        //_rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous; // 바닥 뚫림 방지
        
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 움직임 => 이거 안하면 오류 생기는 듯
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 충돌 감지 개선
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

    
    //player
    private void ThrowPlayerBall()
    {
        if (IsThrown) return;

        IsStrike = false;
        isBatTouch = false;
        IsZone = false;
        IsThrown = true;
        IsPassing = true;

        pitchEvent.RaiseEvent();

        //int index = Random.Range(0, 25);
        
        Vector3 targetPosition = strikeZone.GetZone(4).position;
        
        //아래는 직구
        //OnBallPhysics();

        Vector3 targetVector = (targetPosition - transform.position);
        float dis = targetVector.magnitude;
        // **정확한 직선 투구 - 스트라이크존 (0, 0.605, -14.06) 조준**
        Vector3 direction = targetVector.normalized;

        float time = dis / _rigidbody.velocity.magnitude;
        //Debug.Log("time + time한 후에는 스트라이크가 (" + Time.time+ ") : "+ time);
        if(bat)
            StartCoroutine(StartSwingAfter(time - bat.RotationTime / 2));
        
        //time - bat.RotationTime / 2)
        float ac = Mathf.Abs(Physics.gravity.y) * time / 2;
        //_rigidbody.velocity = ( direction) * _rigidbody.velocity.magnitude;
        _rigidbody.velocity = ((1.0f - ball_accuracy_weight) * _rigidbody.velocity.normalized
                                + ball_accuracy_weight * direction)
            * _rigidbody.velocity.magnitude + new Vector3(0, ac, 0) * ball_accuracy_weight;

        playAudioClipEvent.RaiseEvent(0);
        
        // 이펙트
        PlayThrowEffects();
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
    
    //피칭 결과 알려주는 함수
    private void PitchResult()
    {
        //투수가 공을 안 던진경우
        if (!IsThrown)
        {
            return;
        }

        if(isBack)//홈런이나 파울맞음
        {
            return;
        }
        
        //볼을 맞춘 경우
        if (IsBatTouch)
        {
            //흐음
            if (this.transform.position.x > -0.5f || this.transform.position.z > -0.5f)
            {
                Debug.Log("파울");
                foulEvent.RaiseEvent();
            }
            //in play
            else if (!isGroundBall) //groundball or flying ball
            {
                Debug.Log("안타");
                inplayGameEvent.RaiseEvent();
                IsGroundBall = true;
            }
        }
        //스윙 여부는 방망이의 회전값?
        else if (bat.IsSwing()) //스윙여부 == true => 스윙했는데 방망이를 건들지 않은 경우
        {
            playAudioClipEvent.RaiseEvent(3);
            Debug.Log("스트라이크1");
            addStrikeEvent.RaiseEvent();
            //backToPitcherEvent.RaiseEvent();
        }
        else if(IsStrike) //스윙 안했는데 스트라이크존에 들어간 경우
        {
            playAudioClipEvent.RaiseEvent(3);
            Debug.Log("스트라이크2");
            addStrikeEvent.RaiseEvent();
            //backToPitcherEvent.RaiseEvent();
        }
        else //스트라이크 존에도 안 닿았고 스윙도 안했다면
        {
            Debug.Log("볼");
            addBallCountEvent.RaiseEvent();
            //backToPitcherEvent.RaiseEvent();
        }
        
        //돌아가라 => 안타 제외
        if(!isBack && !isBatTouch)
        {
            isBack = true;
            //임시 막기
            backToPitcherEvent.RaiseEvent(); //이게 IsBatTouch를 false로 만듬
        }
        IsThrown = false;
    }

    /// <summary>
    /// 볼 속력 출력
    /// </summary>
    private void PrintBallVelocity()
    {
        if(IsZone)
        {
            return;
        }
        if (!_rigidbody)
        {
            Debug.Log("rigidbdoy 없음");
            return;
        }
        Vector3 v = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z);
        float velocity = v.magnitude * 3.6f; 
        getVelocityEventSO.RaiseEvent(velocity);
        //Debug.Log(velocity+ "km/h"); //수치 재미를 위해 * 4.5 할듯
    }
    
    
    public bool GetIsSwing()
    {
        //Debug.Log("회전한 값 : " + bat.transform.rotation.eulerAngles.y);
        if (bat == null)
        {
            return false;
        }
        return bat.IsSwing();
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
    
    IEnumerator StartSwingAfter(float delay)
    {
        //Debug.Log("시간 (음수면 안된다): " + delay); //3.3
        yield return new WaitForSeconds(delay);
        //Debug.Log("cal hit time (" + Time.time+ ") : "+ delay);
        bat.StartSwing();
    }

    void OnDrawGizmos()
    {
        CalTrajectory(true);
    }

    void OnDrawGizmosSelected()
    {
        CalTrajectory(true);
    }
    void CalTrajectory(bool isDebug = false)
    {
        float dashLength = 0.3f; // 그려지는 짧은 선 길이
        float gapLength  = 0.2f; // 대시 사이 공백
        
        int steps = 160;
        float dt = 0.05f;
        
        if(isDebug)
            Gizmos.color = Color.yellow;

        Vector3 p = transform.position;
        
        Vector3 g = Physics.gravity;
        Vector3 v;
        if (_rigidbody == null)
        {
            v = new Vector3(1, 0, 1).normalized * _debugVelocity / 3.6f;
        }
        else
        {
            v = _rigidbody.velocity;
        }
        
        float stepLen = dashLength + Mathf.Max(0f, gapLength);

        for (int i = 0; i < steps; i++)
        {
            //중력 적용
            v += g * dt;
            Vector3 nextP = p + v * dt;

            // p -> nextP 구간을 대시로 쪼개서 그리기
            if(isDebug)
                DrawDashedSegment(p, nextP, dashLength, stepLen);

            // 물체 충돌 => 원형
            if (Physics.Linecast(p, nextP, out var hit))
            {
                if(isDebug) 
                    Gizmos.DrawWireSphere(hit.point, 0.2f);
                _targetPosition = hit.point;
                break;
            }

            p = nextP;
        }
        
    }

    void DrawDashedSegment(Vector3 a, Vector3 b, float dashLen, float stepLen)
    {
        Vector3 ab = b - a;
        float len = ab.magnitude;
        if (len < 0.00001f) return;

        Vector3 dir = ab / len;

        for (float t = 0f; t < len; t += stepLen)
        {
            float t0 = t;
            float t1 = Mathf.Min(t + dashLen, len);
            Gizmos.DrawLine(a + dir * t0, a + dir * t1);
        }
    }

    public Vector3 GetTargetPosition()
    {
        return _targetPosition;
    }
}
