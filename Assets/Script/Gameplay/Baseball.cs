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
    
    
    [Space]
    //from GameManager
    [Header("Listening to Events")] 
    [SerializeField] private VoidEventSO allTrackingOffEvent;
    [SerializeField] private VoidEventSO addBallCountEvent;
    [SerializeField] private VoidEventSO addStrikeEvent;
    [SerializeField] private VoidEventSO paulEvent;
    [SerializeField] private VoidEventSO homerunEvent;
    [SerializeField] private VoidEventSO pitchEvent;
    [SerializeField] private VoidEventSO runSignalEvent;
    [SerializeField] private VoidEventSO backToPitcherEvent; //?
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

    // 속도 추적
    const float targetSpeed = 8.0f; // 8.0 => 25? , 32 => 140


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
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("VelocityZone"))
        {
            PrintBallVelocity();
        }

        Debug.Log("건드린 물체 : " + collider.gameObject.name + " - " + collider.gameObject.tag);
        //into Strike Zone
        if (collider.gameObject.CompareTag("StrikeZone"))
        {
            IsZone = true;
            IsStrike = true;
            //Debug.Log("스트라이크 : " + IsStrike);
            //addStrikeEvent.RaiseEvent();
        }

        //paul
        if (collider.CompareTag("Paul"))
        {
            if (isBatTouch && isThrown && !IsGroundBall)
                paulEvent.RaiseEvent();
            else
            {
                //PitchResult(); -> 이거 왜 넣었더라
                if(backToPitcherEvent !=null)
                    backToPitcherEvent.RaiseEvent();
            }
        }

        //homerun
        if (collider.CompareTag("Homerun"))
        {
            if (isBatTouch)
                homerunEvent.RaiseEvent();

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

                float speed = collision.transform.GetComponent<Bat>().GetSwingSpeed();
                // Debug.Log("방향 :" + hitDirection);
                Debug.Log("스피드 :" + speed);

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
    }

    #region PROPERTY

    public bool IsThrown
    {
        get => isThrown;
        set
        {
            isThrown = value;
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
            Debug.Log("스트라이크 : " + value);
            isStrike = value;

        }
    }

    #endregion


    private void InitializeComponents()
    {
        // XRGrabInteractable 설정 확인
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
            grabInteractable.throwOnDetach = true;
        }

        // XR 이벤트 연결
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.selectEntered.AddListener(OnGrab); // **잡을 때 이벤트 추가!**

        // **충돌 감지 강화 설정**
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous; // 바닥 뚫림 방지
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 움직임

        // 초기 위치 설정
    }

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

    private void OnRelease(SelectExitEventArgs args)
    {
        Debug.Log("🎾 공을 놓았습니다! 던지기 시작!");
        Invoke(nameof(ThrowPlayerBall), 0.1f);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        Debug.Log("✋ 공을 잡았습니다! ");
        grabInteractable.throwOnDetach = false;
    }

    #region PLAYER
    //player
    private void ThrowPlayerBall()
    {
        if (IsThrown) return;

        IsStrike = false;
        IsThrown = true;
        isBatTouch = false;
        IsZone = false;

        // XR 비활성화
        grabInteractable.enabled = false;
        pitchEvent.RaiseEvent();

        int index = Random.Range(0, 25);
        Debug.Log(index);
        
        targetPosition = strikeZone.GetZone(index).position;
        
        //아래는 직구
        // **완전 무시하고 강제 방향!**
        Vector3 forceDirection = (targetPosition - transform.position).normalized;

        OnBallPhysics();

        // **정확한 직선 투구 - 스트라이크존 (0, 0.605, -14.06) 조준**
        Vector3 direction = (targetPosition - transform.position).normalized;

        // **거리 계산하여 적절한 속도 설정**
        float distance = Vector3.Distance(transform.position, targetPosition);
        float adjustedSpeed = targetSpeed * 1.2f; // 속도 20% 증가로 거리 보상

        Vector3 velocity = direction * adjustedSpeed;

        _rigidbody.useGravity = false; // 중력 완전 제거
        _rigidbody.velocity = velocity;

        Debug.Log($"🎯 중력 제거 직선 투구! 거리: {distance:F2}m, 속도: {adjustedSpeed:F1}m/s");
        Debug.Log($"🎯 시작: {transform.position}, 타겟: {targetPosition}, 속도벡터: {velocity}");
        playAudioClipEvent.RaiseEvent(0);
        
        // 이펙트
        PlayThrowEffects();
    } // 구 버전 보정 메서드 제거됨 - 단순화

    #endregion
    
    public void OnTouchBall()
    {
        grabInteractable.enabled = true;
    }

    public void OffTouchBall()
    {
        grabInteractable.enabled = false;
    }

    //피칭 결과 알려주는 함수
    private void PitchResult()
    {
        //투수가 공을 안 던진경우
        if (!IsThrown)
        {
            return;
        }

        //볼을 맞춘 경우
        if (IsBatTouch)
        {
            //Paul
            if (this.transform.position.x > -0.5f || this.transform.position.z > -0.5f)
            {
                Debug.Log("파울");
                paulEvent.RaiseEvent();
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
        }
        else if(IsStrike) //스윙 안했는데 스트라이크존에 들어간 경우
        {
            playAudioClipEvent.RaiseEvent(3);
            Debug.Log("스트라이크2");
            addStrikeEvent.RaiseEvent();
        }
        else //스트라이크 존에도 안 닿았고 스윙도 안했다면
        {
            Debug.Log("볼");
            addBallCountEvent.RaiseEvent();
        }
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
        Debug.Log(velocity+ "km/h"); //수치 재미를 위해 * 4.5 할듯
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

    
    //Rigidbody
    #region PHYSICS

    public void OffBallPhysics()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
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
}
