
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//todo : 던지면 그 _rigidbody.interpolation 를 조절하는 거를 목표로
public class BaseballPhysics : MonoBehaviour
{
    //public float ballMass = 0.145f; // kg
    //public float ballRadius = 0.037f; // m
    //public float airDensity = 1.225f; // kg/m³

    private Rigidbody _rigidbody;
    private Baseball _baseball;

    [SerializeField] private Transform defaultParentBaseball;
    [SerializeField] private TrajectoryBaseBallData _trajectoryBaseBallData;

    [Header("Listening to EventChannels")]
    [SerializeField] private FloatEventSO getVelocityEventSO; //from BattingSystem

    [Header("Debug")]
    [SerializeField] private float _debugVelocity;

    private float flightTime = 0;
    private float ball_accuracy_weight = 0.0f; //0~1, 1일수록 보정값이 매우 높음
    private bool _canMeasureVelocity = true; //velocity 측정

    //거리/시간으로 속력 측정용: Pitched 상태 진입 시점에 기록
    private float _pitchStartTime = 0f;
    private Vector3 _pitchStartPos = Vector3.zero;

    [Tooltip("플레이어 던지기 속력 배율 — UI 슬라이더로 런타임 조절")]
    [SerializeField, Range(0.5f, 5f)] private float speedWeight = 2.0f;

    //이런 것도 있구나
    //AnimationCurve ㅇㅇ =  AnimationCurve.Linear(0, 0, 1, 1);

    //커브나 다른 무언가 사용하기 위해 만든 변수
    private Vector3 velocityXY; // 실제 목표 위치
    private float beforeTime = 0f;

    #region Unity Lifecycle

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _baseball = GetComponent<Baseball>();

        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 움직임 => 이거 안하면 오류 생기는 듯
    }

    private void Update()
    {
        PredictTrajectory(GetPosition());
    }


    private void FixedUpdate()
    {
        CheckStrikeZoneTunneling();
        ApplyPitchMovement();
    }

    #endregion

    #region Unity Callbacks

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("VelocityZone"))
        {
            PrintBallVelocity();
        }

        if (collider.gameObject.CompareTag("BallZone"))
        {
            _baseball.IsZone = true;
        }
        //into Strike Zone
        if (collider.gameObject.CompareTag("StrikeZone"))
        {
            _baseball.IsZone = true;
            _baseball.IsStrike = true;
            //debugShootTime2 = Time.time - debugShootTime2;

            //Debug.Log("[Pitcher] : 시간 차이(양수면 실제 시간이 오래 걸린겨) : " + (debugShootTime2 - debugShootTime));
            //Debug.Break();
            //addStrikeEvent.RaiseEvent();
        }
        //Debug.Log("건드린 물체 : " + collider.gameObject.name + " - " + collider.gameObject.tag);
        //Debug.Log("모드 : " + _rigidbody.collisionDetectionMode);
        //foul
        if (collider.CompareTag("Foul"))
        {
            _baseball.Foul();
        }

        //homerun
        if (collider.CompareTag("Homerun"))
        {
            _baseball.Homerun();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Ground") || collision.collider.CompareTag("Base"))
        {

            //잡지 않았다면
            if (_baseball.CurrentState != BallState.Grabbed)
            {
                //파울
                if ((transform.position.x > 0 || transform.position.z > 0) && _baseball.IsInGamePlay)
                {
                    _baseball.Foul();
                    return;
                }
                _baseball.PitchResult();

                //혹여나 송구미스
                _baseball.CurrentState = BallState.FreeBall;

                if (_baseball.IsInGamePlay)
                {
                    //throw ball but swing miss
                    _baseball.IsGroundBall = true;
                }
            }

        }

        //in play game
        if (collision.collider.CompareTag("Bat"))
        {
            //두번 건드리는거 방지
            if (_baseball.IsInGamePlay)
            {
                return;
            };

            _baseball.Hit();
            ApplyBatHitForce(collision);
        }
    }

    private void ApplyBatHitForce(Collision collision)
    {
        // 방망이의 Rigidbody 컴포넌트 가져오기
        Rigidbody batRb = collision.gameObject.GetComponent<Rigidbody>();

        //force ball
        if (batRb != null)
        {
            //계산이 잘못된 듯?
            // 충돌 방향 계산 (배트에서 공으로의 방향)
            Vector3 hitDirection = (transform.position - collision.GetContact(0).point ).normalized;
            Bat bat = collision.transform.GetComponent<Bat>();

            //배트 터치
            float speed = bat.GetSwingSpeed();

            Debug.LogWarning("[Bat] : 가중치는 4배에서 2배로 줄임");
            //가중치 4배 * speed * 4f
            _rigidbody.AddForce(hitDirection * speed * 2, ForceMode.Impulse);
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.collider.CompareTag("Bat"))
        {
            Debug.Log("hit! 공 속도 :" + _rigidbody.velocity);
        }
    }

    #endregion

    #region Pitching

    public void ThrowBall(Vector3 start, Vector3 target, float velocity_xy)
    {
        Vector3 fw = _baseball.GetSelectedPitchTypeSO().ForceWeight;
        Debug.Log($"[Throw] 요청속력={velocity_xy}km/h, start={start}, target={target}, forceWeight={fw}");

        Vector3 force = GetVelocityByPitchType(start, target, velocity_xy);

        Debug.Log($"[Throw] CalculateVelocity 결과: v=({force.x:F2},{force.y:F2},{force.z:F2}), " +
                  $"|원래 xz속력|={new Vector2(force.x, force.z).magnitude * 3.6f:F1}km/h, " +
                  $"|total|={force.magnitude * 3.6f:F1}km/h");

        //rotation zero
        SetVelocity(force); //계산하는 함수

        beforeTime = Time.time;
        velocityXY = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z);
        _canMeasureVelocity = true; //속력 측정 준비 (VelocityZone 통과시 한번 출력)
    }

    public void ThrowPlayerBall(Vector3 targetPos, PitchType pitchType)
    {
        SetVelocity(CalculateAssistedVelocity(_rigidbody.velocity, targetPos, pitchType));
    }

    private void ApplyPitchMovement() //FixedUpdated
    {
        if (_rigidbody)
        {
            //던지는 함수가 아니라면 => 땅볼인데 커브라면 무조건 땅바닥으로 박는다
            if (_baseball.CurrentState != BallState.Pitched)
            {
                return;
            }

            //슬라이더면 슬라이더 힘 추가 커브면 커브 힘 추가
            Vector3 force = _baseball.GetSelectedPitchTypeSO()
                .GetForce(_rigidbody.velocity);
            _rigidbody.velocity += Time.fixedDeltaTime * force; //m / s
        }
    }

    /// <summary>
    /// Pitched 상태 진입 시 시작 시간/위치 기록 (거리/시간 속력 측정용)
    /// </summary>
    public void RecordPitchStart()
    {
        if (!_rigidbody) return;
        _pitchStartTime = Time.time;
        _pitchStartPos = _rigidbody.position;
    }

    #endregion

    #region Velocity Calculation

    /// <summary>
    /// 통합 계산 단위
    /// </summary>
    /// <param name="start"></param>
    /// <param name="target"></param>
    /// <param name="velocity_xy">km/h단위</param>
    /// <returns></returns>
    public Vector3 CalculateVelocity(Vector3 start, Vector3 target,
        float velocity_xy, Vector3 piterTypeForce)
    {
        velocity_xy /= 3.6f;
        float g = Mathf.Abs(Physics.gravity.y); // 9.81 (양수)

        Vector3 diff = target - start;
        Vector3 dirXZ = new Vector3(diff.x, 0, diff.z).normalized;
        float d = new Vector2(diff.x, diff.z).magnitude; // 수평 거리
        float h = diff.y; // 높이차

        // 비행 시간 계산: t = d / velocity_xy
        float t = d / velocity_xy;
        flightTime = t;

        // 평균(=초기) 속도에서의 축별 추가 가속도 (m/s²)
        // GetForce(v) = forceWeight * vXZ² 를 평균속도로 근사
        // 2-pass: 1차 보정이 만드는 횡속도가 vXZ²를 키워서 Magnus가 더 강해짐
        //         → mean(vx²) = vx_comp²/3 (대칭 운동 적분 결과)를 vSq에 더해 재계산
        float vSq = velocity_xy * velocity_xy; //제곱

        //ForceWeight를 비행 로컬 → 월드 변환 (PitchTypeSO.GetForce와 동일한 규약)
        //x = 비행 방향 기준 오른쪽, y = 월드 위, z = 비행 방향 전진
        //Unity는 왼손좌표계 → up × forward = right (forward × up이 아님!)
        Vector3 leftXZ = Vector3.Cross(Vector3.up, dirXZ); //외적 순서 유의 => z가 양수
        Vector3 forceWorld = leftXZ * piterTypeForce.x
                             + Vector3.up * piterTypeForce.y
                             + dirXZ * piterTypeForce.z;

        //마그누스 공식
        float aX = vSq * forceWorld.x;
        float aY = vSq * forceWorld.y; // 보통 음수 (아래로 휨)
        float aZ = vSq * forceWorld.z;

        // y방향 초기 속도 Vy = (h + 0.5 * g * t^2) / t
        // 유효 중력 = g - aY (forceWeight.y < 0 이면 더 빨리 떨어짐)
        float effectiveG = g - aY;
        float vy = (h + 0.5f * effectiveG * t * t) / t;

        // 최종 속도 벡터
        //v₀t + ½at² = 거리0. v_0 = -1/2at =>
        Vector3 velocity = dirXZ * velocity_xy;
        
        //dirXZ : +0.7, +0.7
        Debug.Log("forceWorld : " + leftXZ * piterTypeForce.x);
        
        // x/z 옆 휨 보정: 0.5*a*t² 만큼 휘므로 초기 방향을 반대로 살짝 틀어준다
        velocity.x -= 0.5f * aX * t;
        velocity.z -= 0.5f * aZ * t;
        velocity.y = vy;


        // 안전망: NaN/Infinity/극단치 방지 (Gizmo for-loop 무한반복 + Linecast 크래시 막기)
        const float MAX_SPEED = 200f; // 시속 720km 넘으면 비정상
        if (float.IsNaN(velocity.x) || float.IsInfinity(velocity.x) ||
            float.IsNaN(velocity.y) || float.IsInfinity(velocity.y) ||
            float.IsNaN(velocity.z) || float.IsInfinity(velocity.z) ||
            Mathf.Abs(velocity.x) > MAX_SPEED ||
            Mathf.Abs(velocity.y) > MAX_SPEED ||
            Mathf.Abs(velocity.z) > MAX_SPEED)
        {
            Debug.LogError($"[CalculateVelocity] 비정상 값! velocity={velocity}, piterTypeForce={piterTypeForce}, velocity_xy={velocity_xy}, t={t}. forceWeight 너무 큼 의심.");
            return Vector3.zero;
        }

        //=== Shooting method: 분석 공식(constant-a 가정)의 잔여 drift를 SimulateForward로 inverse 보정 ===
        //forceWeight가 크거나 비행이 길어 구속/방향이 크게 변하면 1차 분석 보정만으론 부족 → 시뮬 결과의 오차를 초기 속도에서 빼주는 방식으로 1~3회 수렴
        Vector3 preShootingLanding = SimulateForward(start, velocity, piterTypeForce, t);
        Vector3 currentLanding = preShootingLanding;
        const int MAX_ITER = 3;
        const float CONVERGE_SQ = 0.0001f; //1cm 임계
        for (int iter = 0; iter < MAX_ITER; iter++)
        {
            Vector3 driftIter = currentLanding - target;
            if (driftIter.sqrMagnitude < CONVERGE_SQ) break;
            velocity -= driftIter / t; //1차 선형 보정: 도달점이 target보다 X쪽으로 +dx 어긋났으면 초기 vx를 dx/t만큼 줄임
            currentLanding = SimulateForward(start, velocity, piterTypeForce, t);
        }

        return velocity;
    }

    //velocityXZ는 km/h
    /// <summary>
    /// 던질때 타입에 따라 추가 힘 계산
    /// </summary>
    /// <param name="start"></param>
    /// <param name="target"></param>
    /// <param name="velocityXZ"></param>
    /// <param name="pitchType"></param>
    /// <returns></returns>
    public Vector3 GetVelocityByPitchType(Vector3 start, Vector3 target, float velocityXZ)
    {
        Vector3 piterTypeForce = _baseball.GetSelectedPitchTypeSO().ForceWeight;
        return CalculateVelocity(start, target, velocityXZ, piterTypeForce);
    }

    /// <summary>
    /// Player 함수. 가중치를 추가로 적용해서 변경된 속력을 반환
    /// </summary>
    /// <param name="rawVRVelocity"></param>
    /// <param name="targetPos"></param>
    /// <param name="pitchType"></param>
    /// <param name="assistWeight"></param>
    /// <returns></returns>
    public Vector3 CalculateAssistedVelocity(Vector3 rawVRVelocity, Vector3 targetPos, PitchType pitchType)
    {
        // 1. 유저가 팔을 휘두른 "진짜 물리적 속력(시속)"을 구함
        float playerSpeedKmh = rawVRVelocity.magnitude * 3.6f;

        // 만약 너무 살짝 던졌다면 보정 계산 중 0나누기 에러가 날 수 있으니 방어 코드
        if (playerSpeedKmh <= 1.0f) return rawVRVelocity;

        playerSpeedKmh *= Mathf.Max(0.1f, speedWeight); //UI 슬라이더로 조절
// 🔥 핵심 방어선: 유저가 9km로 던졌어도 강제로 110km로 끌어올림!
        float finalSpeedKmh = Mathf.Min(playerSpeedKmh, 160f);
        // 3. 섞기 (Lerp 마법!)

        // 2. 유저의 구속(Speed)은 그대로 유지한 채, 타겟에 완벽하게 꽂히는 정답 궤적(Velocity)을 알아냄
        Vector3 perfectVelocity = GetVelocityByPitchType(transform.position, targetPos, finalSpeedKmh);

        // assistWeight가 0.0이면 100% 똥볼(rawVRVelocity), 1.0이면 100% 완벽한 스트라이크(perfectVelocity)
        Vector3 finalVelocity = Vector3.Lerp(rawVRVelocity, perfectVelocity, Mathf.Clamp01(ball_accuracy_weight));

        return finalVelocity;
    }

    /// <summary>
    /// ApplyPitchMovement와 동일한 식으로 비행 궤적을 수치 적분해서 duration 후 위치 반환.
    /// CalculateVelocity의 shooting method 보정에서 사용.
    /// </summary>
    private Vector3 SimulateForward(Vector3 start, Vector3 v0, Vector3 forceWeight, float duration)
    {
        Vector3 p = start;
        Vector3 v = v0;
        Vector3 g = Physics.gravity;
        const float dt = 0.01f; // 충분히 작은 스텝 (정확도 vs 비용)
        int steps = Mathf.CeilToInt(duration / dt);

        for (int i = 0; i < steps; i++)
        {
            Vector3 vXZ = new Vector3(v.x, 0, v.z);
            float vSqH = vXZ.sqrMagnitude;

            //GetForce와 동일: ForceWeight를 비행 로컬 → 월드 변환
            Vector3 force;
            if (vSqH < 0.0001f)
            {
                force = Vector3.zero;
            }
            else
            {
                Vector3 forward = vXZ.normalized;
                Vector3 right = new Vector3(forward.z, 0, -forward.x);
                Vector3 forceWorld = right * forceWeight.x
                                   + Vector3.up * forceWeight.y
                                   + forward * forceWeight.z;
                force = forceWorld * vSqH;
            }
            v += (force + g) * dt;
            p += v * dt;
        }
        return p;
    }

    #endregion

    #region Trajectory

    /// <summary>
    /// 현재 위치와 속도를 바탕으로 미래의 궤적 데이터를 계산해 반환
    /// 치거나 던질때
    /// </summary>
    public void PredictTrajectory(Vector3 startPos)
    {
        //속력 넣기
        Vector3 initialVelocity = _rigidbody.velocity;

        // 안전망: NaN/Infinity/극단치면 Physics.Linecast 크래시 + Gizmo 무한루프 → 조기 종료
        const float MAX_SPEED = 200f;
        const float MAX_COORD = 10000f;
        if (float.IsNaN(startPos.x) || float.IsInfinity(startPos.x) ||
            Mathf.Abs(startPos.x) > MAX_COORD || Mathf.Abs(startPos.y) > MAX_COORD || Mathf.Abs(startPos.z) > MAX_COORD ||
            float.IsNaN(initialVelocity.x) || float.IsInfinity(initialVelocity.x) ||
            float.IsNaN(initialVelocity.y) || float.IsInfinity(initialVelocity.y) ||
            float.IsNaN(initialVelocity.z) || float.IsInfinity(initialVelocity.z) ||
            initialVelocity.magnitude > MAX_SPEED)
        {
            Debug.LogError($"[PredictTrajectory] 비정상 값 감지! startPos={startPos}, velocity={initialVelocity}. 궤적 계산 건너뜀.");
            return;
        }

        _trajectoryBaseBallData.Init();
        _trajectoryBaseBallData.AddPathPoint(startPos); // 시작점 저장

        int steps = 160;
        float dt = 0.05f;
        Vector3 p = startPos;
        Vector3 v = initialVelocity;
        Vector3 g = Physics.gravity;

        for (int i = 0; i < steps; i++)
        {
            v += g * dt; // 중력 적용
            Vector3 nextP = p + v * dt;

            // 🚨 충돌 감지 로직
            if (Physics.Linecast(p, nextP, out var hit, -1, QueryTriggerInteraction.Collide))
            {
                // 1. 스트라이크 존 관통 확인
                if (hit.collider.isTrigger && (hit.collider.CompareTag("BallZone") || hit.collider.CompareTag("StrikeZone")))
                {
                    if (!_trajectoryBaseBallData.GetHasPassedStrikeZone())
                    {
                        _trajectoryBaseBallData.SetStrikeZonePoint(hit.point);
                        _trajectoryBaseBallData.SetHasPassedStrikeZone(true);
                    }
                }
                // 2. 바닥/벽 등 물리적 충돌 확인
                else if (!hit.collider.isTrigger)
                {
                    _trajectoryBaseBallData.SetLandingPoint(hit.point);
                    _trajectoryBaseBallData.SetHasLand(true);
                    _trajectoryBaseBallData.AddPathPoint(hit.point); // 마지막 도착점 저장
                    break; // 계산 종료
                }
            }

            _trajectoryBaseBallData.AddPathPoint(nextP); // 궤적 점 저장
            p = nextP;
        }
    }

    //todo 나중에 공 rigidbody Continuous Dynamic로 전환하고 이 함수는 제거할거임
    //=> Continuous Dynamic으로도 트리거 터널링은 안 잡혀서 이 함수 유지 필요
    private void CheckStrikeZoneTunneling()
    {
        //너무 빨라서 스트라이크존을 지나친 경우의 판정
        if (_baseball.CurrentState != BallState.Pitched) return;
        if (_baseball.IsStrike) return; //이미 트리거로 잡혔으면 중복 방지

        Vector3 velocity = _rigidbody.velocity;
        if (velocity.sqrMagnitude < 0.0001f) return; //정지/0속도 보호

        float dis = velocity.magnitude * Time.deltaTime;
        Ray ray = new Ray(this.transform.position, velocity);

        //트리거도 감지하도록 QueryTriggerInteraction.Collide
        if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit rayHit, dis,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            //Debug.DrawRay(ray.origin, ray.direction, Color.red, 0.5f);
            if(rayHit.transform.CompareTag("StrikeZone"))
            {
                _baseball.IsZone = true;
                _baseball.IsStrike = true;
            }
        }
    }

    /// <summary>
    /// 볼 속력 출력 (거리/시간 방식)
    /// </summary>
    private void PrintBallVelocity()
    {
        //중첩 방지인가?
        //IsZone 대신 별도 플래그 사용: 던질때만 켜지고, 한번 측정하면 끈다
        if(!_canMeasureVelocity)
        {
            return;
        }
        _canMeasureVelocity = false;

        //끝나는 시간 / 도착 포지션
        float endTime = Time.time;
        Vector3 endPos = _rigidbody.position;

        float elapsedTime = endTime - _pitchStartTime;
        if (elapsedTime <= 0f)
        {
            Debug.LogWarning("[측정] 경과 시간이 0 이하 → 측정 불가. _pitchStartTime이 설정되었는지 확인.");
            return;
        }

        Vector3 displacement = endPos - _pitchStartPos;
        Vector3 displacementXZ = new Vector3(displacement.x, 0, displacement.z); //ms 단위 계산용 (수평 거리)
        float velocity = (displacementXZ.magnitude / elapsedTime) * 3.6f; // m/s → km/h

        Vector3 v = GetVelocity();  //ms (참고용 rigidbody.velocity)
        Debug.Log($"[측정] startPos={_pitchStartPos}, endPos={endPos}, " +
                  $"v=({v.x:F2},{v.y:F2},{v.z:F2}), " +
                  $"distXZ={displacementXZ.magnitude:F2}m, elapsed={elapsedTime:F3}s, " +
                  $"|h|={velocity:F1}km/h, " +
                  $"forceWeight={_baseball.GetSelectedPitchTypeSO().ForceWeight}");

        getVelocityEventSO.RaiseEvent(velocity);
    }

    #endregion

    #region Accessors

    public void SetGravity(bool useGravity)
    {
        if (_rigidbody)
        {
            _rigidbody.useGravity = useGravity;
        }
    }

    public void SetPosition(Vector3 position)
    {
        if (_rigidbody)
        {
            _rigidbody.position = position;
        }

    }

    //위치관련
    public void SetVelocity(Vector3 velocity)
    {
        if (_rigidbody)
        {
            _rigidbody.transform.rotation = Quaternion.identity;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.velocity = velocity;
        }
    }

    public Vector3 GetVelocity()
    {
        if (!_rigidbody)
        {
            Debug.LogError("No rigidbody found");
            return Vector3.zero;
        }
        return _rigidbody.velocity;
    }

    public Vector3 GetPosition()
    {
        return _rigidbody.position;
    }

    public void SetRigidbodyMode(bool isContinusMode)
    {
        if (!_rigidbody)
            return;
        if (isContinusMode)
        {
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        else
        {
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
    }

    public float GetFlightTime() => flightTime;

    public TrajectoryBaseBallData GetTrajectoryBaseBallData() => _trajectoryBaseBallData;

    #endregion

    #region Properties

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

    public float SpeedWeight
    {
        get => speedWeight;
        set => speedWeight = value;
    }

    public bool CanMeasureVelocity
    {
        get => _canMeasureVelocity;
        set => _canMeasureVelocity = value;
    }

    #endregion
}
