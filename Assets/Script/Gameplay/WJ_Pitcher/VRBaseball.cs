using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class VRBaseball : MonoBehaviour
{
    [Header("물리 설정")]
    public float baseThrowForce = 10f;
    public float maxThrowForce = 25f;
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

    // 상태 변수
    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private bool isThrown = false;
    private bool isCurveActive = false;
    private float throwTime = 0f;
    private float curveTimer = 0f;

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

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // XR 이벤트 연결
        grabInteractable.selectExited.AddListener(OnRelease);

        // 중력 저장
        originalGravity = Physics.gravity;

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

        if (strikeZone == null)
        {
            GameObject strikeZoneObj = GameObject.FindGameObjectWithTag("StrikeZone");
            if (strikeZoneObj != null)
                strikeZone = strikeZoneObj.transform;
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
        // 컨트롤러에서 놓았을 때 던지기 실행
        if (throwVelocity.magnitude > 2f) // 최소 속도 체크
        {
            ThrowBall();
        }
    }

    private void ThrowBall()
    {
        if (isThrown) return;

        isThrown = true;
        throwTime = 0f;

        // 던지는 힘 계산
        float throwMagnitude = Mathf.Clamp(throwVelocity.magnitude, baseThrowForce, maxThrowForce);
        Vector3 throwDirection = throwVelocity.normalized;

        // 스트라이크 존 방향 보정 (약간)
        if (strikeZone != null)
        {
            throwDirection = ApplyStrikeZoneCorrection(throwDirection);
        }

        // 애니메이션 커브 적용
        float smoothMultiplier = throwSmoothingCurve.Evaluate(throwMagnitude / maxThrowForce);

        // 최종 속도 계산 (구종별 속도 배수 적용)
        Vector3 finalVelocity = throwDirection * throwMagnitude * currentPitchData.speedMultiplier;

        rb.velocity = finalVelocity * smoothMultiplier;

        // 회전 효과 적용
        if (currentPitchData.spinStrength > 0)
        {
            rb.angularVelocity = currentPitchData.spinDirection * currentPitchData.spinStrength;
        }

        // 커브 효과 시작 예약
        if (currentPitchData.curveStrength > 0)
        {
            Invoke(nameof(StartCurveEffect), currentPitchData.curveDelay);
        }

        // 이펙트 및 사운드
        PlayThrowEffects();

        OnBallThrown?.Invoke(this);

        Debug.Log($"공 던짐! 구종: {currentPitchData.pitchName}, 속도: {throwMagnitude:F1}");
    }

    private Vector3 ApplyStrikeZoneCorrection(Vector3 originalDirection)
    {
        Vector3 toStrikeZone = (strikeZone.position - transform.position).normalized;

        // 원래 방향과 스트라이크 존 방향을 적절히 블렌딩 (80% 원래, 20% 보정)
        Vector3 correctedDirection = Vector3.Lerp(originalDirection, toStrikeZone, 0.2f);

        return correctedDirection.normalized;
    }

    private void StartCurveEffect()
    {
        isCurveActive = true;
        curveTimer = 0f;
    }

    private void ApplyPitchPhysics()
    {
        throwTime += Time.deltaTime;

        // 개별 중력 적용 (구종별 중력 배수)
        Vector3 customGravity = originalGravity * currentPitchData.gravityMultiplier;
        rb.AddForce(customGravity, ForceMode.Acceleration);

        // 커브 효과 적용
        if (isCurveActive && currentPitchData.curveStrength > 0)
        {
            curveTimer += Time.deltaTime;

            // 커브 강도를 시간에 따라 증가
            float curveIntensity = Mathf.Lerp(0f, currentPitchData.curveStrength, curveTimer);
            Vector3 curveForce = currentPitchData.curveDirection * curveIntensity * Time.deltaTime;

            rb.AddForce(curveForce, ForceMode.Acceleration);
        }
    }

    private void UpdateTrajectoryEffect()
    {
        if (trajectoryLine != null && rb.velocity.magnitude > 1f)
        {
            // 간단한 궤도 예측선 그리기
            Vector3[] points = PredictTrajectory(transform.position, rb.velocity, 10, 0.1f);
            trajectoryLine.positionCount = points.Length;
            trajectoryLine.SetPositions(points);
        }
    }

    private Vector3[] PredictTrajectory(Vector3 startPos, Vector3 startVel, int steps, float timeStep)
    {
        Vector3[] points = new Vector3[steps];
        Vector3 currentPos = startPos;
        Vector3 currentVel = startVel;

        // 구종별 커스텀 중력 적용
        Vector3 customGravity = originalGravity * currentPitchData.gravityMultiplier;

        for (int i = 0; i < steps; i++)
        {
            points[i] = currentPos;

            // 구종별 물리 적용
            currentVel += customGravity * timeStep;

            // 커브 효과 시뮬레이션 (간단화)
            if (currentPitchData.curveStrength > 0 && i > 3) // 약간의 지연 후 커브 적용
            {
                Vector3 curveEffect = currentPitchData.curveDirection * (currentPitchData.curveStrength * 0.1f) * timeStep;
                currentVel += curveEffect;
            }

            currentPos += currentVel * timeStep;
        }

        return points;
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
            // 파티클 효과 정지
            StopAllEffects();

            // 충돌 처리
            if (collision.gameObject.CompareTag("Ground") ||
                collision.gameObject.CompareTag("StrikeZone") ||
                collision.gameObject.name.Contains("Ground"))
            {
                if (bounceSound != null && audioSource != null)
                    audioSource.PlayOneShot(bounceSound);

                Debug.Log($"공이 땅에 닿음! 위치: {collision.contacts[0].point}");

                // 이벤트 발생
                OnBallLanded?.Invoke(this, false); // 기본적으로 볼 처리
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isThrown && other.CompareTag("StrikeZone"))
        {
            // 파티클 효과 정지
            StopAllEffects();

            Debug.Log("스트라이크!");
            OnBallLanded?.Invoke(this, true); // 스트라이크 처리
        }
    }

    public void ResetBall(Vector3 position)
    {
        // 공 상태 초기화
        isThrown = false;
        isCurveActive = false;
        throwTime = 0f;
        curveTimer = 0f;

        // 물리 초기화
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = true;

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

        Debug.Log("공이 리셋되었습니다.");
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }
}
