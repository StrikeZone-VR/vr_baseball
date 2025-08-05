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
        InitializeComponents();
        UpdatePitchData();
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // XRGrabInteractable이 항상 활성화되도록 보장
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // XR 이벤트 연결
        grabInteractable.selectExited.AddListener(OnRelease);

        // 중력 저장
        originalGravity = Physics.gravity;

        // 초기에는 일반 중력 사용 (공이 자연스럽게 떨어지도록)
        rb.useGravity = true;

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
        
        // **천천히 쭉 뻗는 속도**
        float targetSpeed = 0.8f;  // 천천히!
        
        // **물리 완전 제어**
        rb.useGravity = false;  // 중력 완전 차단
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.drag = 0f;
        rb.angularDrag = 0f;
        
        // **강제 속도 적용**
        Vector3 finalVelocity = forceDirection * targetSpeed;
        rb.velocity = finalVelocity;
        
        Debug.Log($"🎯 야매 시스템 발동! 타겟: {targetPosition}, 속도: {targetSpeed}");

        // 이펙트
        PlayThrowEffects();
        OnBallThrown?.Invoke(this);
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

        if (throwSound != null && audioSource != null)
            audioSource.PlayOneShot(throwSound);
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
                collision.gameObject.name.Contains("Ground"))
            {
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

                // 이벤트 발생
                OnBallLanded?.Invoke(this, isStrike);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isThrown && other.CompareTag("StrikeZone"))
        {
            // **트리거 충돌 시에도 즉시 멈춤!**
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
            
            Debug.Log($"🎯 트리거 스트라이크 감지! 콜라이더: {other.name} - 공 완전 정지!");

            // 파티클 효과 정지
            StopAllEffects();

            OnBallLanded?.Invoke(this, true); // 스트라이크 처리
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
        }
    }
}
