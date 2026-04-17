using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//todo : 던지면 그 _rigidbody.interpolation 를 조절하는 거를 목표로
public class BaseballPhysics : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Baseball _baseball;
    
    [SerializeField] private Transform defaultParentBaseball;
    [SerializeField] private TrajectoryBaseBallData _trajectoryBaseBallData;
    
    [Header("Listening to EventChannels")] 
    [SerializeField] private FloatEventSO getVelocityEventSO; //from BattingSystem

    [Header("Debug")] 
    [SerializeField] private float _debugVelocity;
    
    //이런 것도 있구나
    //AnimationCurve ㅇㅇ =  AnimationCurve.Linear(0, 0, 1, 1);

    //커브나 다른 무언가 사용하기 위해 만든 변수
    private Vector3 velocityXY; // 실제 목표 위치
    private float beforeTime = 0f;
    private float assistWeight = 0f; //가중치
    
    private readonly float MAGNUS = 60.0f; //100 기준

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
        if (_rigidbody)
        {
            //CheckStrikeZoneTunneling();
            ApplyPitchMovement();
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("VelocityZone"))
        {
            PrintBallVelocity();
        }

        if (collider.gameObject.CompareTag("BallZone"))
        {
            _baseball.IsZone = true;
            // 수직선 그리기
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
    //todo 나중에 공 rigidbody Continuous Dynamic로 전환하고 이 함수는 제거할거임
    // private void CheckStrikeZoneTunneling()
    // {
    //     //너무 빨라서 스트라이크존을 지나친 경우의 판정
    //     float dis = _rigidbody.velocity.magnitude * Time.deltaTime;
    //     Ray ray = new Ray(this.transform.position, _rigidbody.velocity);
    //
    //     if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit rayHit, dis))
    //     {
    //         //Debug.DrawRay(ray.origin, ray.direction, Color.red, 0.5f);
    //         if(rayHit.transform.CompareTag("StrikeZone"))
    //         {
    //             IsStrike = true;
    //         }
    //     }
    // }

    private void ApplyPitchMovement()
    {
        if (_rigidbody)
        {
            if (_baseball.CurrentState != BallState.Pitched)
            {
                return;
            }

            switch (_baseball.SelectPitchType)
            {
                case PitchType.Curve:
                    float deltaTime = Time.time - beforeTime;
                    beforeTime = Time.time;
                    _rigidbody.velocity += new Vector3(0, -deltaTime * velocityXY.magnitude / 100 * MAGNUS,0);
                    break;
            }
        }
    }

    public void ThrowBall(Vector3 start, Vector3 target, float velocity_xy)
    {
        Vector3 force = CalculateVelocity(start, target, velocity_xy);
        
        //rotation zero
        SetVelocity(force); //계산하는 함수

        beforeTime = Time.time;
        velocityXY = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z);
    }
    
    /// <summary>
    /// 통합 계산 단위
    /// </summary>
    /// <param name="start"></param>
    /// <param name="target"></param>
    /// <param name="velocity_xy">km/h단위</param>
    /// <returns></returns>
    public Vector3 CalculateVelocity(Vector3 start, Vector3 target, float velocity_xy, float piterTypeForce = 0)
    {
        velocity_xy /= 3.6f;
        float g = Mathf.Abs(Physics.gravity.y) + piterTypeForce; // 9.81 (양수)
    
        Vector3 diff = target - start;
        Vector3 dirXZ = new Vector3(diff.x, 0, diff.z).normalized;
        float d = new Vector2(diff.x, diff.z).magnitude; // 수평 거리
        float h = diff.y; // 높이차
    
        // 비행 시간 계산: t = d / velocity_xy
        float t = d / velocity_xy;
    
        // y방향 초기 속도 Vy = (h + 0.5 * g * t^2) / t
        float vy = (h + 0.5f * g * t * t) / t; 
    
        // 최종 속도 벡터
        Vector3 velocity = dirXZ * velocity_xy;
        velocity.y = vy;
        return velocity;
    }

    public Vector3 GetVelocityByPitchType(Vector3 start, Vector3 target, float velocityXZ, PitchType pitchType)
    {
        float piterTypeForce = 0; 
        switch (pitchType)
        {
            case PitchType.Curve:
                piterTypeForce += velocityXZ / 100 * MAGNUS;
                break;
            case PitchType.FastBall:
                break;
        }
        return CalculateVelocity(start, target, velocityXZ,piterTypeForce);
    }

    public void ThrowPlayerBall(Vector3 targetPos, PitchType pitchType)
    {
        SetVelocity(CalculateAssistedVelocity(_rigidbody.velocity, targetPos, pitchType));
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

        // 2. 유저의 구속(Speed)은 그대로 유지한 채, 타겟에 완벽하게 꽂히는 정답 궤적(Velocity)을 알아냄
        Vector3 perfectVelocity = GetVelocityByPitchType(transform.position, targetPos, playerSpeedKmh, pitchType);

        // 3. 섞기 (Lerp 마법!)
        // assistWeight가 0.0이면 100% 똥볼(rawVRVelocity), 1.0이면 100% 완벽한 스트라이크(perfectVelocity)
        Vector3 finalVelocity = Vector3.Lerp(rawVRVelocity, perfectVelocity, Mathf.Clamp01(assistWeight));

        return finalVelocity;
    }
    
    // public Vector3 CalculateSimpleVelocity(Vector3 start, Vector3 target, float velocityXZ)
    // {
    //     velocityXZ /= 3.6f; //시속 평준화
    //     float g = Mathf.Abs(Physics.gravity.y); // 9.81 (양수)
    //     Vector3 dis = target - start;
    //
    //     float mytime = dis.magnitude / velocityXZ;
    //
    //     float velocityY = mytime / 2 * g;
    //     Vector3 velocityXZ_normal = dis.normalized;
    //     velocityXZ_normal *= velocityXZ;
    //
    //     Vector3 result = velocityXZ_normal + new Vector3(0, velocityY, 0);
    //     return result;
    // }
    //
    // private Vector3 CalculateCurveVelocity(Vector3 start, Vector3 target, float velocityXZ)
    // {
    //     velocityXZ /= 3.6f; //시속 평준화
    //     float g = Mathf.Abs(Physics.gravity.y) + (velocityXZ / 100 * MAGNUS); // 9.81 (양수)
    //     Vector3 dis = target - start;
    //
    //     float mytime = dis.magnitude / velocityXZ;
    //
    //     float velocityY = mytime / 2 * g;
    //     Vector3 velocityXZ_normal = dis.normalized;
    //     velocityXZ_normal *= velocityXZ;
    //
    //     Vector3 result = velocityXZ_normal + new Vector3(0, velocityY, 0);
    //     return result;
    // }
    

    public void SetGravity(bool useGravity)
    {
        _rigidbody.useGravity = useGravity;
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
    
    
    //todo debug는 낙하 계산과 디버깅을 나눌 예정
    #region TRAJECTORY

    
    /// <summary>
    /// 현재 위치와 속도를 바탕으로 미래의 궤적 데이터를 계산해 반환
    /// 치거나 던질때 
    /// </summary>
    public void PredictTrajectory(Vector3 startPos)
    {
        //속력 넣기
        Vector3 initialVelocity = _rigidbody.velocity;
        
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

    /// <summary>
    /// 볼 속력 출력
    /// </summary>
    private void PrintBallVelocity()
    {
        if(_baseball.IsZone)
        {
            return;
        }

        Vector3 v = GetVelocity(); 
        Vector3 speed = new Vector3(v.x, 0, v.z);
        float velocity = speed.magnitude * 3.6f; 
        getVelocityEventSO.RaiseEvent(velocity);
    }

    #endregion

    public TrajectoryBaseBallData GetTrajectoryBaseBallData() => _trajectoryBaseBallData;

    public Vector3 GetPosition()
    {
        return _rigidbody.position;
    }
}
