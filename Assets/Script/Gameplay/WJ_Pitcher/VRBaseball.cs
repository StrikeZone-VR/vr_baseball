using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class VRBaseball : MonoBehaviour
{
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
    public StrikeZoneAreaManager areaManager;

    [Header("투구 보정 설정")]
    [Range(0f, 1f)]
    public float aimAssistStrength = 0.8f;     // 보정 강도 (0=보정없음, 1=완전보정)
    public bool enableRandomTargeting = true;   // 랜덤 타겟팅 활성화

    // 상태 변수
    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private bool isThrown = false;
    // 사용하지 않는 변수들 제거: isCurveActive, throwTime, curveTimer
    private Vector3 targetPosition;             // 실제 목표 위치

    // 속도 추적
    private Vector3 throwVelocity;
    private Vector3 lastPosition;
    private Vector3 originalGravity;

    // 이벤트
    public System.Action<VRBaseball> OnBallThrown;
    public System.Action<VRBaseball, bool> OnBallLanded; // bool: isStrike

    void Start()
    {
        // 강제 활성화
        this.enabled = true;

        Debug.Log("VRBaseball Start() 메서드 호출됨! 활성화 상태: " + this.enabled);

        InitializeComponents();
        UpdatePitchData();
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // XRGrabInteractable 설정 확인
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;

            // ThrowOnDetach와 isKinematic이 충돌하는지 확인
            if (rb != null && rb.isKinematic && grabInteractable.throwOnDetach)
            {
                Debug.LogWarning($"⚠️ 경고: Kinematic Rigidbody ({rb.isKinematic})와 ThrowOnDetach ({grabInteractable.throwOnDetach})가 충돌합니다! 이 문제를 해결하려면 둘 중 하나를 변경해야 합니다.");
                // 해결 방법 1: throwOnDetach 비활성화
                // grabInteractable.throwOnDetach = false;

                // 해결 방법 2: 그랩 시점에 물리 활성화 (OnGrab에서 처리)
                Debug.Log("👉 그랩 시점에서 Kinematic 상태를 해제하여 해결할 예정입니다.");
            }
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // XR 이벤트 연결
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.selectEntered.AddListener(OnGrab);  // **잡을 때 이벤트 추가!**

        // 중력 저장
        originalGravity = Physics.gravity;

        // **기본 물리 설정 - 순서 중요! velocity 먼저, kinematic 나중에!**
        // Kinematic 상태 확인 후 안전하게 처리
        if (rb.isKinematic)
        {
            // 이미 Kinematic인 경우 velocity 설정하지 않음 (경고 회피)
            Debug.Log("🔒 Rigidbody가 이미 Kinematic 상태입니다. velocity는 설정하지 않습니다.");
        }
        else
        {
            rb.velocity = Vector3.zero;         // 먼저 velocity 설정
            rb.angularVelocity = Vector3.zero;  // 먼저 angular velocity 설정
            rb.useGravity = false;              // 중력 끄기 (떨어지지 않게)
            rb.isKinematic = true;              // 마지막에 kinematic 설정
        }

        Debug.Log($"⚙️ VRBaseball 초기화 완료! Kinematic: {rb.isKinematic}, ThrowOnDetach: {grabInteractable?.throwOnDetach} (그랩할 때까지 고정)");

        // 궤도선 설정
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
            trajectoryLine.useWorldSpace = true;
            trajectoryLine.startWidth = 0.02f;
            trajectoryLine.endWidth = 0.02f;
            trajectoryLine.positionCount = 0;
        }

        // 컴포넌트 찾기
        if (trailEffect == null)
            trailEffect = GetComponentInChildren<ParticleSystem>();

        // 영역 매니저 찾기
        if (areaManager == null)
        {
            areaManager = FindObjectOfType<StrikeZoneAreaManager>();
        }

        // StrikeZone 찾기 - 단순화
        if (strikeZone == null)
        {
            GameObject strikeZoneObj = GameObject.FindGameObjectWithTag("StrikeZone");
            if (strikeZoneObj != null)
            {
                strikeZone = strikeZoneObj.transform;
            }
            else if (areaManager != null && areaManager.strikeZoneParent != null)
            {
                strikeZone = areaManager.strikeZoneParent;
            }
        }

        // 초기 위치 설정
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (isThrown)
        {
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
        Debug.Log("✋ 공을 잡았습니다! 물리 활성화!");

        // **공을 잡는 순간 물리 활성화!**
        if (rb != null)
        {
            // 강제로 스크립트 활성화
            this.enabled = true;

            // XRGrabInteractable 설정 확인 및 수정
            XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                // 확실히 활성화 및 설정
                grabInteractable.enabled = true;
                grabInteractable.throwOnDetach = true;

                Debug.Log($"XRGrabInteractable 설정 확인: enabled={grabInteractable.enabled}, throwOnDetach={grabInteractable.throwOnDetach}");
            }

            // 핵심: 물리 설정을 명확하게
            rb.isKinematic = false;  // 반드시 kinematic을 false로 설정
            rb.useGravity = true;    // 중력 활성화 (자연스러운 느낌)
            rb.velocity = Vector3.zero;      // velocity 초기화
            rb.angularVelocity = Vector3.zero; // angular velocity 초기화

            Debug.Log($"[중요] 물리 설정 완료! Kinematic: {rb.isKinematic}, UseGravity: {rb.useGravity}");

            // 위치 업데이트를 위한 lastPosition 설정
            lastPosition = transform.position;
        }
    }

    private void ThrowBall()
    {
        if (isThrown) return;
        isThrown = true;

        // XR 비활성화
        grabInteractable.enabled = false;

        // 스트라이크존 찾기
        if (strikeZone == null)
        {
            strikeZone = GameObject.FindGameObjectWithTag("StrikeZone")?.transform;
            if (strikeZone == null && areaManager != null)
                strikeZone = areaManager.strikeZoneParent;
        }

        // **야매 시스템 발동** - 무조건 스트라이크존 중앙으로!
        Vector3 targetPosition;
        if (strikeZone != null)
        {
            targetPosition = strikeZone.position;
        }
        else
        {
            // 스트라이크존 못찾으면 앞으로
            targetPosition = transform.position + Vector3.forward * 8f;
        }

        // **완전 무시하고 강제 방향!**
        Vector3 forceDirection = (targetPosition - transform.position).normalized;

        // **속도 설정 - 개선된 버전**
        float targetSpeed = 5f;  // 더 빠르게! (0.8f → 5f)

        // **물리 완전 제어 - 개선된 버전**
        // 강제로 비키네마틱 상태로 만들고 물리 설정 모두 초기화
        rb.isKinematic = false;  // **먼저 kinematic 해제!**
        rb.useGravity = false;   // 중력 완전 차단 (직선으로 날아가도록)
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.drag = 0f;            // 공기 저항 제거
        rb.angularDrag = 0f;     // 회전 저항 제거
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 충돌 감지 개선

        // **강제 속도 적용**
        Vector3 finalVelocity = forceDirection * targetSpeed;
        rb.velocity = finalVelocity;

        // 확실히 던져지게 추가 힘도 가함
        rb.AddForce(forceDirection * targetSpeed * 2f, ForceMode.Impulse);

        Debug.Log($"🎯 야매 시스템 발동! 타겟: {targetPosition}, 속도: {targetSpeed}");

        // 이펙트
        PlayThrowEffects();
        // OnBallThrown 이벤트는 충돌 시에만 발생하도록 수정!
    }    // 구 버전 보정 메서드 제거됨 - 단순화

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
            // 메인 트레일 항상 실행
            if (trailEffect != null)
                trailEffect.Play();

            // 구종별 추가 이펙트 실행
            switch (selectedPitchType)
            {
                case PitchType.FastBall:
                    if (fastBallSpeedLines != null)
                        fastBallSpeedLines.Play();
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

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"🔥 충돌 감지! 공 던진 상태: {isThrown}, 충돌 객체: {collision.gameObject.name}");

        if (isThrown)
        {
            // **충돌 시 즉시 멈춤!**
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;  // 중력도 끄기
            rb.isKinematic = true;  // 완전히 멈추기

            // 파티클 효과 정지
            StopAllEffects();

            // 충돌 처리
            if (collision.gameObject.CompareTag("Ground") ||
                collision.gameObject.CompareTag("StrikeZone") ||
                collision.gameObject.name.Contains("Ground") ||
                collision.gameObject.name.Contains("MainZone") ||  // MainZoneVisual 추가!
                collision.gameObject.name.Contains("Zone"))        // 기타 Zone 객체들
            {
                Debug.Log($"✅ 유효한 충돌 객체 확인: {collision.gameObject.name}");

                if (bounceSound != null && audioSource != null)
                    audioSource.PlayOneShot(bounceSound);

                Vector3 hitPosition = collision.contacts[0].point;
                bool isStrike = false;

                // 스트라이크 판정 로직 개선
                if (areaManager != null)
                {
                    isStrike = areaManager.IsStrikePosition(hitPosition);
                    Debug.Log($"🎯 AreaManager 판정: {(isStrike ? "스트라이크" : "볼")} (위치: {hitPosition})");
                }
                else
                {
                    // 기존 방식: 스트라이크존 콜라이더 내부인지 확인
                    if (strikeZone != null)
                    {
                        Collider strikeZoneCollider = strikeZone.GetComponent<Collider>();
                        if (strikeZoneCollider != null)
                        {
                            isStrike = strikeZoneCollider.bounds.Contains(hitPosition);
                            Debug.Log($"🎯 기본 판정: {(isStrike ? "스트라이크" : "볼")} (위치: {hitPosition})");
                        }
                    }
                }

                Debug.Log($"⚾ 최종 판정: {(isStrike ? "🎯 스트라이크!" : "❌ 볼!")} - 공 완전 정지!");

                // 이벤트 발생 - 공이 착지했을 때 둘 다 발생!
                Debug.Log($"🚀 OnBallThrown 이벤트 발생 시도!");
                OnBallThrown?.Invoke(this);   // 이제 여기서 새 공 스폰
                Debug.Log($"📊 OnBallLanded 이벤트 발생 시도!");
                OnBallLanded?.Invoke(this, isStrike);
            }
            else
            {
                // **조건에 맞지 않는 충돌 객체라도 이벤트는 발생시키기!**
                Debug.Log($"❓ 알 수 없는 충돌 객체: {collision.gameObject.name}, 하지만 이벤트는 발생!");

                // 스트라이크/볼 판정은 안 되지만 새 공은 스폰해야 함
                Debug.Log($"🚀 OnBallThrown 이벤트 발생 시도! (알 수 없는 충돌)");
                OnBallThrown?.Invoke(this);   // 새 공 스폰
                Debug.Log($"📊 OnBallLanded 이벤트 발생 시도! (기본 볼 처리)");
                OnBallLanded?.Invoke(this, false); // 일단 볼로 처리
            }
        }
    }

    // 이벤트 한 번만 발생시키기 위한 플래그
    private bool eventFired = false;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🎯 트리거 감지! 공 던진 상태: {isThrown}, 트리거 객체: {other.name}, 이벤트 발생 여부: {eventFired}");

        // 이벤트가 이미 발생했거나 공이 던져지지 않았으면 무시
        if (!isThrown || eventFired) return;

        if (other.CompareTag("StrikeZone"))
        {
            // 이벤트 플래그 설정 (중복 호출 방지)
            eventFired = true;

            try
            {
                // **트리거 충돌 시에도 즉시 멈춤!**
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.useGravity = false;
                    rb.isKinematic = true;
                }

                Debug.Log($"🎯 트리거 스트라이크 감지! 콜라이더: {other.name} - 공 완전 정지!");

                // 파티클 효과 정지
                StopAllEffects();

                // 이벤트 발생 - 한 번만 발생!
                Debug.Log($"🚀 OnBallThrown 이벤트 발생! (트리거)");
                if (OnBallThrown != null) OnBallThrown(this);

                Debug.Log($"📊 OnBallLanded 이벤트 발생! (트리거)");
                if (OnBallLanded != null) OnBallLanded(this, true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"OnTriggerEnter 오류 발생: {e.Message}");
            }
        }
    }

    public void ResetBall(Vector3 position)
    {
        // 공 상태 초기화
        isThrown = false;
        // 사용하지 않는 변수들 제거됨
        targetPosition = Vector3.zero;

        // XRGrabInteractable 다시 활성화 (새 공이 잡힐 수 있도록)
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
        }

        // 물리 초기화
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false; // 리셋된 공도 중력 비활성화 (안정적 배치)

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

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnRelease);
            grabInteractable.selectEntered.RemoveListener(OnGrab);  // **잡기 이벤트도 해제**
        }
    }
}
