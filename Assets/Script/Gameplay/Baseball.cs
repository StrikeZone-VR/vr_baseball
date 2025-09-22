using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent( typeof(Rigidbody), typeof(XRGrabInteractable))]
public class Baseball : MonoBehaviour
{
    [SerializeField] private Defender myDefender; //handling player
    private Rigidbody _rigidbody;
    private XRGrabInteractable grabInteractable;

    [SerializeField] bool isGroundBall = false; 
    [SerializeField] bool isBatTouch = false;
    [SerializeField] bool isPassing = false;
    bool isZone = false;
    [SerializeField] private float defenderDis = 0.0f;
    
    //from GameManager
    [Header("Listening to Events")]
    [SerializeField] private VoidEventSO allTrackingOffEvent;
    [SerializeField] private VoidEventSO addBallCountEvent;
    [SerializeField] private VoidEventSO addStrikeEvent;
    [SerializeField] private VoidEventSO paulEvent;
    [SerializeField] private VoidEventSO homerunEvent;
    [SerializeField] private VoidEventSO pitchEvent; 
    [SerializeField] private VoidEventSO runSignalEvent; 

    
    [Header("물리 설정")]
    public float baseThrowForce = 1f;      // Inspector 덮어쓰기 방지: 1f로 더 낮춤
    public float maxThrowForce = 2f;       // Inspector 덮어쓰기 방지: 2f로 더 낮춤
    public AnimationCurve throwSmoothingCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("구종 설정")]
    public PitchType selectedPitchType = PitchType.FastBall;
    private PitchData currentPitchData;

    [Header("이펙트")]
    public ParticleSystem trailEffect;                  // 메인 트레일
    public ParticleSystem fastBallSpeedLines;          // 직구 전용
    public ParticleSystem curveSpinEffect;             // 커브 전용
    public ParticleSystem sliderSideEffect;            // 슬라이더 전용
    public ParticleSystem forkDropEffect;              // 포크볼 전용
    public LineRenderer trajectoryLine;

    [Header("오디오")]
    public AudioSource audioSource;
    public AudioClip throwSound;
    public AudioClip bounceSound;

    [Header("참조")]
    public Transform strikeZone;
    //public PitchingSystemManager pitchingSystemManager;    // 새로운 통합 시스템

    [Header("투구 보정 설정")]
    [Range(0f, 1f)]
    public float aimAssistStrength = 0.8f;     // 보정 강도 높임 (0=보정없음, 1=완전보정)
    public bool enableRandomTargeting = true;   // 랜덤 타겟팅 활성화

    // is Pitcher throw?
    private bool isThrown = false;
    
    /// <summary> 공이 투구되었는지 확인 </summary>  <returns>투구 상태</returns>
    
    // 사용하지 않는 변수들 제거: isCurveActive, throwTime, curveTimer
    private Vector3 targetPosition;             // 실제 목표 위치

    // 속도 추적
    private Vector3 throwVelocity;
    private Vector3 lastPosition;
    private Vector3 originalGravity;

    // 이벤트 한 번만 발생시키기 위한 플래그
    private bool eventFired = false;
    
    
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
        if (isThrown)
        {
            // 안전장치: 공이 너무 아래로 떨어지면 강제 멈춤 => ball
            if (transform.position.y < -2.0f) // Y=-2 이하로 떨어지면
            {
                Debug.LogWarning($"⚠️ 공이 바닥을 뚫고 떨어짐! Y위치: {transform.position.y} - 볼 처리합니다.");

                // 강제로 바닥에 착지한 것으로 처리
                if (_rigidbody != null && !_rigidbody.isKinematic)
                {
                    _rigidbody.velocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                    _rigidbody.useGravity = false;
                    _rigidbody.isKinematic = true;
                }

                // 던지기 상태 종료
                isThrown = false;
                return;
            }

            ApplyPitchPhysics();
            UpdateTrajectoryEffect();

            // 디버그 로그 제거 - 렉 방지
            // 성능 향상을 위해 콘솔 출력 완전 제거
        }
        else
        {
            // 던지기 전 속도 추적
            throwVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
            lastPosition = transform.position;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        //Strike Zone
        if (collider.gameObject.CompareTag("StrikeZone") && !IsZone)
        {
            IsZone = true;
            //addStrikeEvent.RaiseEvent();
        }
        //paul
        if (collider.CompareTag("Paul"))
        {
            if(isBatTouch)
                paulEvent.RaiseEvent();
        }
        //homerun
        if (collider.CompareTag("Homerun"))
        {
            if(isBatTouch)
                homerunEvent.RaiseEvent();

        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.CompareTag("BallZone") && !IsZone)
        {
            IsZone = true;
            addBallCountEvent.RaiseEvent();
        }
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.CompareTag("Ground") || collision.collider.CompareTag("Base"))
        {
            IsPassing = false;
            IsThrown = false;

            if (!isBatTouch)
            {
                //throw ball but swing miss
                return;
            }
            //paul, homerun check
            if (!isGroundBall) //groundball or flying ball
            {
                if (this.transform.position.y > 0 || this.transform.position.y > 0)
                {
                    paulEvent.RaiseEvent();
                }
                Debug.Log("not paul => inplaygame");
                IsGroundBall = true;
            }
            
        }
        //in play game
        if (collision.collider.CompareTag("Bat"))
        {
            Debug.Log("인 플레이 게임");
            IsBatTouch = true;
            IsThrown = false;

            // 공의 Rigidbody 컴포넌트 가져오기
            Rigidbody batRb = collision.gameObject.GetComponent<Rigidbody>();
            
            //force ball
            if (batRb != null)
            {
                //계산이 잘못된 듯?
                // 충돌 방향 계산 (배트에서 공으로의 방향)
                Vector3 hitDirection = (collision.GetContact(0).point - transform.position).normalized;

                float speed = collision.transform.GetComponent<Bat>().GetSwingSpeed();
                // Debug.Log("방향 :" + hitDirection);
                Debug.Log("스피드 :" +speed);

                this._rigidbody.AddForce(hitDirection * speed * 2.5f, ForceMode.Impulse);
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
            grabInteractable.selectEntered.RemoveListener(OnGrab);  // **잡기 이벤트도 해제**
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
    }

    #region PROPERTY
    public bool IsThrown
    {
        get => isThrown;
        set => isThrown = value;
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
        set => isBatTouch = value;
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
        set => isZone = value;
    }
    
    #endregion
    
    
    private void InitializeComponents()
    {
        // XRGrabInteractable 설정 확인
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;

            // ThrowOnDetach와 isKinematic이 충돌하는지 확인
            if (_rigidbody != null && _rigidbody.isKinematic && grabInteractable.throwOnDetach)
            {
                Debug.LogWarning($"⚠️ 경고: Kinematic Rigidbody ({_rigidbody.isKinematic})와 ThrowOnDetach ({grabInteractable.throwOnDetach})가 충돌합니다! 이 문제를 해결하려면 둘 중 하나를 변경해야 합니다.");
                // 해결 방법 1: throwOnDetach 비활성화
                // grabInteractable.throwOnDetach = false;

                // 해결 방법 2: 그랩 시점에 물리 활성화 (OnGrab에서 처리)
                Debug.Log("👉 그랩 시점에서 Kinematic 상태를 해제하여 해결할 예정입니다.");
            }
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();


        // XR 이벤트 연결
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.selectEntered.AddListener(OnGrab);  // **잡을 때 이벤트 추가!**

        // 중력 저장
        originalGravity = Physics.gravity;

        // **기본 물리 설정 - 바닥 충돌 개선!**
        // Kinematic 상태 확인 후 안전하게 처리
        // if (_rigidbody.isKinematic)
        // {
        //     // 이미 Kinematic인 경우 velocity 설정하지 않음 (경고 회피)
        //     Debug.Log("🔒 Rigidbody가 이미 Kinematic 상태입니다. velocity는 설정하지 않습니다.");
        // }
        // else
        // {
        //     _rigidbody.velocity = Vector3.zero;         // 먼저 velocity 설정
        //     _rigidbody.angularVelocity = Vector3.zero;  // 먼저 angular velocity 설정
        //     _rigidbody.useGravity = false;              // 중력 끄기 (떨어지지 않게)
        //     _rigidbody.isKinematic = true;              // 마지막에 kinematic 설정
        // }

        // **충돌 감지 강화 설정**
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous; // 바닥 뚫림 방지
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;          // 부드러운 움직임

        Debug.Log($"⚙️ VRBaseball 초기화 완료! Kinematic: {_rigidbody.isKinematic}, ThrowOnDetach: {grabInteractable?.throwOnDetach} (그랩할 때까지 고정)");

        // 궤도선 설정
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
            trajectoryLine.useWorldSpace = true;
            trajectoryLine.startWidth = 0.02f;
            trajectoryLine.endWidth = 0.02f;
            trajectoryLine.positionCount = 0;
        }

        // 초기 위치 설정
        lastPosition = transform.position;
    }

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

    private void OnRelease(SelectExitEventArgs args)
    {
        Debug.Log("🎾 공을 놓았습니다! 던지기 시작!");
        Invoke(nameof(ThrowBall), 0.1f);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        Debug.Log("✋ 공을 잡았습니다! ");
        // **공을 잡는 순간 물리 활성화!**
        // if (_rigidbody != null)
        // {
        //
        //     // XRGrabInteractable 설정 확인 및 수정
        //     XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        //     if (grabInteractable != null)
        //     {
        //         // kinematic 충돌 방지를 위해 throwOnDetach 비활성화
        //         grabInteractable.throwOnDetach = false;
        //
        //         Debug.Log($"XRGrabInteractable 설정 확인: enabled={grabInteractable.enabled}, throwOnDetach={grabInteractable.throwOnDetach}");
        //     }
        //
        //     // 핵심: 물리 설정을 명확하게
        //     _rigidbody.isKinematic = false;  // 반드시 kinematic을 false로 설정
        //     _rigidbody.useGravity = true;    // 중력 활성화 (자연스러운 느낌)
        //     _rigidbody.velocity = Vector3.zero;      // velocity 초기화
        //     _rigidbody.angularVelocity = Vector3.zero; // angular velocity 초기화
        //
        //     Debug.Log($"[중요] 물리 설정 완료! Kinematic: {_rigidbody.isKinematic}, UseGravity: {_rigidbody.useGravity}");
        //
        //     // 위치 업데이트를 위한 lastPosition 설정
        //     lastPosition = transform.position;
        // }
    }

    private void ThrowBall()
    {
        if (isThrown) return;
        isThrown = true;
        isBatTouch = false;
        
        // XR 비활성화
        grabInteractable.enabled = false;
        pitchEvent.RaiseEvent();
        
        // **새로운 통합 25구역 시스템 사용** - 랜덤 타겟 위치 가져오기 ★★★★★★★★★★★★★★★★★★★★
        // if (enableRandomTargeting && pitchingSystemManager != null)
        // {
        //     // **🎯 새로운 통합 시스템 사용!**
        //     targetPosition = pitchingSystemManager.GetTargetPosition();
        //     Debug.Log($"🎯 새로운 투수 시스템에서 랜덤 타겟 선택: {targetPosition}");
        // }
        
        
        if (strikeZone != null)
        {
            // **정확한 StrikeZone 위치만 사용! 임의 보정 금지!**
            targetPosition = strikeZone.position;
            Debug.Log($"🎯 정확한 StrikeZone 타겟: {targetPosition}");
        }
        else
        {
            // 완전 못찾으면 씬 기준 고정 위치 (StrikeZone 위치)
            targetPosition = new Vector3(0f, 0.5f, 0f);
        }

        // **완전 무시하고 강제 방향!**
        Vector3 forceDirection = (targetPosition - transform.position).normalized;

        // **속도 설정 - 느린 투구 속도로 조정**
        float targetSpeed = 8.0f;  // 12.0f에서 8.0f로 감소 (약 29km/h, 더 여유있게)

        OnBallPhysics();

        // **정확한 직선 투구 - 스트라이크존 (0, 0.605, -14.06) 조준**
        Vector3 direction = (targetPosition - transform.position).normalized;

        // **거리 계산하여 적절한 속도 설정**
        float distance = Vector3.Distance(transform.position, targetPosition);
        float adjustedSpeed = targetSpeed * 1.2f; // 속도 20% 증가로 거리 보상

        Vector3 velocity = direction * adjustedSpeed;

        // **중력을 완전히 무시하고 직선으로!**
        _rigidbody.useGravity = false; // 중력 완전 제거

        _rigidbody.velocity = velocity;

        Debug.Log($"🎯 중력 제거 직선 투구! 거리: {distance:F2}m, 속도: {adjustedSpeed:F1}m/s");
        Debug.Log($"🎯 시작: {transform.position}, 타겟: {targetPosition}, 속도벡터: {velocity}");

        // 이펙트
        PlayThrowEffects();
        // OnBallThrown 이벤트는 충돌 시에만 발생하도록 수정!
    }    // 구 버전 보정 메서드 제거됨 - 단순화

    public void OnTouchBall()
    {
        grabInteractable.enabled = true;
    }
    public void OffTouchBall()
    {
        Debug.Log($"XRGrabInteractable 설정 확인: enabled={grabInteractable.enabled}, throwOnDetach={grabInteractable.throwOnDetach}");
        grabInteractable.enabled = false;
        
        // kinematic 충돌 방지를 위해 throwOnDetach 비활성화
        //grabInteractable.throwOnDetach = false;
    }
    
    private void StartCurveEffect()
    {
        // 단순화 - 커브 효과 비활성화
    }

    private void ApplyPitchPhysics()
    {
        // **야매 모드에서는 중력 완전 무시!**
        // 아무것도 하지 않음 - 직진만!
    }

    private void UpdateTrajectoryEffect()
    {
        // 단순화 - 궤도선 비활성화
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
        }
    }

    private Vector3[] PredictTrajectory(Vector3 startPos, Vector3 startVel, int steps, float timeStep)
    {
        // 단순화 - 빈 배열 반환
        return new Vector3[0];
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

            // 안전하게 오디오 실행
            if (throwSound != null && audioSource != null)
            {
                if (audioSource.enabled)
                {
                    audioSource.PlayOneShot(throwSound);
                }
                else
                {
                    Debug.Log("오디오 소스가 비활성화 상태입니다. 강제 활성화 시도.");
                    audioSource.enabled = true;
                    audioSource.PlayOneShot(throwSound);
                }
            }
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

    // 공 상태 초기화 => 일단 안쓰는데 지켜봄
    public void ResetBall(Vector3 position)
    {
        isThrown = false;
        eventFired = false; // 이벤트 플래그 초기화
        // 사용하지 않는 변수들 제거됨
        targetPosition = Vector3.zero;

        // XRGrabInteractable 다시 활성화 (새 공이 잡힐 수 있도록)
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
        }

        OffBallPhysics();

        // 위치 설정
        transform.position = position;
        lastPosition = position;

        // 이펙트 정리
        StopAllEffects();

        // 궤도선 숨기기
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
            trajectoryLine.positionCount = 0;
        }
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
    /// <summary>
    /// 공이 특정 구역에 착지했을 때 호출되는 메서드
    /// </summary>
    /// <param name="isStrike">스트라이크 여부</param>
    /// <param name="zoneName">구역 이름</param>
    public void OnBallLandedInZone(bool isStrike, string zoneName)
    {
        Debug.Log($"⚾ 공이 {zoneName}에 착지! {(isStrike ? "Strike ⚾" : "Ball ❌")}");
        
        // VRPitchingManager에게 결과 전달
        PitchingManager pitchingManager = FindObjectOfType<PitchingManager>();
        if (pitchingManager != null)
        {
            pitchingManager.OnBallResult(isStrike, zoneName);
        }
    }
    
    
    //Rigidbody
    #region PHYSICS

    public void OffBallPhysics()
    {
        // **이미 kinematic이면 먼저 해제하고 velocity 설정!**
        _rigidbody.isKinematic = false;  // 먼저 kinematic 해제

        _rigidbody.velocity = Vector3.zero;         // 이제 안전하게 velocity 설정
        _rigidbody.angularVelocity = Vector3.zero;  // 이제 안전하게 angular velocity 설정
        _rigidbody.useGravity = false;              // 중력 끄기
        _rigidbody.isKinematic = true;              // 다시 kinematic 설정
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 충돌 감지 개선
    }
    public void OnBallPhysics()
    {
        // **물리 완전 제어 - 야구 게임다운 설정**
        _rigidbody.isKinematic = false;  // kinematic 해제
        _rigidbody.useGravity = true;    // 중력 적용 (자연스러운 포물선)
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.drag = 0.02f;         // 최소한의 공기 저항
        _rigidbody.angularDrag = 0.05f;  // 최소한의 회전 저항
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous; // 바닥 충돌 개선
    }
        
    
    #endregion
}
