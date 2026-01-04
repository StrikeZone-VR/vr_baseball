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
    [SerializeField] private VoidEventSO backToPitcherEvent; //? => 일단 안쓰임
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


    /// <summary> 공이 투구되었는지 확인 </summary>  <returns>투구 상태</returns>

    // 사용하지 않는 변수들 제거: isCurveActive, throwTime, curveTimer
    private Vector3 targetPosition; // 실제 목표 위치
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
            if (isBatTouch && isThrown && !IsGroundBall )
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
                

                //4
                this._rigidbody.AddForce(hitDirection * speed * 4f, ForceMode.Impulse);
                this._rigidbody.useGravity = true;
            }

            //signal
            runSignalEvent.RaiseEvent();
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.collider.CompareTag("Bat"))
        {
            Debug.Log("공 속도 :" + transform.GetComponent<Rigidbody>().velocity);
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
            isBatTouch = value;
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
        Debug.Log("🎾 공을 놓았습니다! 던지기 시작!");
        Invoke(nameof(ThrowPlayerBall), 0.05f);
        
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        Debug.Log("✋ 공을 잡았습니다! ");
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
        
        targetPosition = strikeZone.GetZone(4).position;
        
        //아래는 직구
        //OnBallPhysics();

        // **정확한 직선 투구 - 스트라이크존 (0, 0.605, -14.06) 조준**
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        //_rigidbody.velocity = ( direction) * _rigidbody.velocity.magnitude;
        _rigidbody.velocity = ((1.0f - ball_accuracy_weight) * _rigidbody.velocity.normalized
                               + ball_accuracy_weight * direction)
                              * _rigidbody.velocity.magnitude;

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

    // 포물선 계산으로 정확한 투구 속도 계산
    private Vector3 CalculateVelocityForTarget(Vector3 startPos, Vector3 targetPos, float speed)
    {
        Vector3 direction = targetPos - startPos;
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
        float verticalDistance = direction.y;

        // 거리가 너무 가까우면 직진
        if (horizontalDistance < 1.0f)
        {
            return direction.normalized * speed;
        }

        // 포물선 운동 공식을 사용하여 각도 계산
        float gravity = Physics.gravity.magnitude;

        // 안전한 계산을 위해 최소각도 보장
        float discriminant = speed * speed * speed * speed - gravity * (gravity * horizontalDistance * horizontalDistance + 2 * verticalDistance * speed * speed);

        float angle;
        if (discriminant < 0)
        {
            // 계산 불가능하면 45도 각도 사용
            angle = Mathf.PI / 4;
            Debug.Log($"⚠️ 포물선 계산 불가! 45도 각도 사용. 거리: {horizontalDistance:F2}m, 높이차: {verticalDistance:F2}m");
        }
        else
        {
            angle = Mathf.Atan((speed * speed + Mathf.Sqrt(discriminant)) / (gravity * horizontalDistance));

            // 각도가 너무 높으면 45도로 제한
            if (angle > Mathf.PI / 4)
            {
                angle = Mathf.PI / 4;
                Debug.Log($"⚠️ 각도 제한! 45도로 설정. 거리: {horizontalDistance:F2}m");
            }
        }

        // 속도 벡터 계산
        Vector3 horizontalDirection = new Vector3(direction.x, 0, direction.z).normalized;
        float horizontalSpeed = speed * Mathf.Cos(angle);
        float verticalSpeed = speed * Mathf.Sin(angle);

        Vector3 finalVelocity = horizontalDirection * horizontalSpeed + Vector3.up * verticalSpeed;

        Debug.Log($"🎯 포물선 계산: 거리={horizontalDistance:F2}m, 각도={angle * Mathf.Rad2Deg:F1}°, 최종속도={finalVelocity}");

        return finalVelocity;
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

        if(!isBack)
        {
            isBack = true;
            backToPitcherEvent.RaiseEvent();
        }
        
        //볼을 맞춘 경우
        if (IsBatTouch)
        {
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
        else if (GetIsSwing()) //스윙여부 == true => 스윙했는데 방망이를 건들지 않은 경우
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
}
