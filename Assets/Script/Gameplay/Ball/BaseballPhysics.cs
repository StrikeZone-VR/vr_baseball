using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//todo : 던지면 그 _rigidbody.interpolation 를 조절하는 거를 목표로
public class BaseballPhysics : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Baseball _baseball;

    private TrajectoryBaseBallData _trajectoryBaseBallData;
    
    [Header("Debug")] 
    [SerializeField] private float _debugVelocity;
    
    //이런 것도 있구나
    //AnimationCurve ㅇㅇ =  AnimationCurve.Linear(0, 0, 1, 1);

    //커브나 다른 무언가 사용하기 위해 만든 변수
    private Vector3 velocityXY; // 실제 목표 위치
    private float beforeTime = 0f;
    
    
    private readonly float MAGNUS = 60.0f; //100 기준

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _baseball = GetComponent<Baseball>();
        
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 움직임 => 이거 안하면 오류 생기는 듯
    }

    private void Update()
    {
        CalTrajectory();
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
            IsZone = true;
            // 수직선 그리기
        }
        //into Strike Zone
        if (collider.gameObject.CompareTag("StrikeZone"))
        {
            IsZone = true;
            IsStrike = true;
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
            Foul();
        }

        //homerun
        if (collider.CompareTag("Homerun"))
        {
            Homerun();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Ground") || collision.collider.CompareTag("Base"))
        {
            if (_baseball.myDefenderComponent == null)
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

    
    // todo player => 이거는 MyBodyPitcherComponet에 들어가야 함. 또는 어딘가 여기에 있으면 안됨
    // public void PlayerThrowBall()
    // {
    //     Vector3 targetPosition = strikeZone.GetZone(4).position;
    //
    //     Vector3 targetVector = (targetPosition - transform.position);
    //     float dis = targetVector.magnitude;
    //     // **정확한 직선 투구 - 스트라이크존 (0, 0.605, -14.06) 조준**
    //     Vector3 direction = targetVector.normalized;
    //
    //     float time = dis / _rigidbody.velocity.magnitude;
    //     //Debug.Log("time + time한 후에는 스트라이크가 (" + Time.time+ ") : "+ time);
    //     if(bat)
    //         StartCoroutine(StartSwingAfter(time - bat.RotationTime / 2));
    //     
    //     //time - bat.RotationTime / 2)
    //     float ac = Mathf.Abs(Physics.gravity.y) * time / 2;
    //     //_rigidbody.velocity = ( direction) * _rigidbody.velocity.magnitude;
    //     SetVelocity(
    //         ( (1.0f - ball_accuracy_weight) * _rigidbody.velocity.normalized
    //          + ball_accuracy_weight * direction )
    //         * _rigidbody.velocity.magnitude + new Vector3(0, ac, 0) * ball_accuracy_weight
    //     );
    // }
    
    
    //todo debug
    //절대 여기서 디버그 넣지말자
    // public void DebugThrowPlayerBall()
    // {
    //     if (_currentState == BallState.Pitched) return;
    //     
    //     CurrentState = BallState.Pitched;
    //     HasPassedStrikeZone = false;
    //     //IsPassing = true;
    //
    //     pitchEvent.RaiseEvent();
    //
    //     //int index = Random.Range(0, 25);
    //     IsZone = false;
    //     IsStrike = false;
    //     
    //     //정면
    //     Vector3 targetPosition = strikeZone.GetZone(4).position;
    //     Vector3 targetVector = (targetPosition - _rigidbody.transform.position);
    //     
    //     float velocity = 100.0f / 3.6f; 
    //     float dis = targetVector.magnitude;
    //     // **정확한 직선 투구 - 스트라이크존 (0, 0.605, -14.06) 조준**
    //     Vector3 direction = targetVector.normalized;
    //
    //     float time = dis / velocity;
    //     //Debug.Log("time + time한 후에는 스트라이크가 (" + Time.time+ ") : "+ time);
    //     
    //     //Debug.Log("예측한 시간대 : " + (time - bat.RotationTime / 2 ));
    //     debugShootTime = time - bat.RotationTime / 2f;
    //     //0.08
    //     debugShootTime2 = Time.time;
    //     if(bat)
    //         StartCoroutine(StartSwingAfter(time - bat.RotationTime / 2));
    //     
    //     //time - bat.RotationTime / 2)
    //     float ac = Mathf.Abs(Physics.gravity.y) * time / 2;
    //
    //     SetVelocity((direction) * velocity + new Vector3(0, ac, 0));
    //     playAudioClipEvent.RaiseEvent(0);
    //     
    //     // 이펙트
    //     PlayThrowEffects();
    // }
    
    //todo 마찬가지로 여기도 Pitcher에 있어야 함
    //AI 공 던지는 함수
    // public void PitchingBall()
    // {
    //     _myBall.CurrentState = BallState.Pitched;
    //     
    //     //random value 0 ~ 24
    //     int index = Random.Range(0, 25);
    //     //index = 22; //한 가운데
    //
    //     Transform SZTransform = strikeZone.GetZone(index);
    //
    //     //Debug.Log("투수 : " + _ball.transform.position);
    //     //Debug.Log("스트라이크 존 " + index + " : "+ SZTransform.position);
    //     Vector3 velocity = new Vector3();
    //     
    //     int pitchTypeIndex = Random.Range(0, 10);
    //     
    //     if (pitchTypeIndex <= 2)
    //     {
    //         _myBall.SelectPitchType = PitchType.Curve;
    //         Debug.Log("커브");
    //     }
    //     else
    //     {
    //         _myBall.SelectPitchType = PitchType.FastBall;
    //         Debug.Log("직구");
    //     }
    //     
    //     if(_myBall.SelectPitchType == PitchType.FastBall)
    //         velocity = CalculateSimpleVelocity(_myBall.transform.position, SZTransform.position, velocityXZ);
    //     //else if(_myBall.SelectPitchType == PitchType.Curve)
    //     else
    //         velocity = CalculateCurveVelocity(_myBall.transform.position, SZTransform.position, velocityXZ);
    //
    //     //Debug.Log("속력 : " + velocity.magnitude * 3.6f);
    //
    //     _myBall.ThrowBall(velocity);
    // }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="start"></param>
    /// <param name="target"></param>
    /// <param name="velocity_xy">km/h단위</param>
    /// <returns></returns>
    public Vector3 CalculateVelocity(Vector3 start, Vector3 target, float velocity_xy)
    {
        velocity_xy /= 3.6f;
        float g = Mathf.Abs(Physics.gravity.y); // 9.81 (양수)
        Vector3 diff = target - start;
        Vector3 dirXZ = new Vector3(diff.x, 0, diff.z).normalized;
        float d = new Vector2(diff.x, diff.z).magnitude; // 수평 거리
        float h = diff.y; // 높이차

        // 비행 시간 계산: t = d / velocity_xy
        float t = d / velocity_xy;

        // y방향 초기 속도 Vy = (h + 0.5 * g * t^2) / t
        float vy = (h + g * t * t) / t;

        // 최종 속도 벡터
        Vector3 velocity = dirXZ * velocity_xy;
        velocity.y = vy;
        return velocity;
    }
    
    
    public Vector3 CalculateSimpleVelocity(Vector3 start, Vector3 target, float velocityXZ)
    {
        velocityXZ /= 3.6f; //시속 평준화
        float g = Mathf.Abs(Physics.gravity.y); // 9.81 (양수)
        Vector3 dis = target - start;

        float mytime = dis.magnitude / velocityXZ;

        float velocityY = mytime / 2 * g;
        Vector3 velocityXZ_normal = dis.normalized;
        velocityXZ_normal *= velocityXZ;

        Vector3 result = velocityXZ_normal + new Vector3(0, velocityY, 0);
        return result;
    }
    
    private Vector3 CalculateCurveVelocity(Vector3 start, Vector3 target, float velocityXZ)
    {
        velocityXZ /= 3.6f; //시속 평준화
        float g = Mathf.Abs(Physics.gravity.y) + (velocityXZ / 100 * MAGNUS); // 9.81 (양수)
        Vector3 dis = target - start;

        float mytime = dis.magnitude / velocityXZ;

        float velocityY = mytime / 2 * g;
        Vector3 velocityXZ_normal = dis.normalized;
        velocityXZ_normal *= velocityXZ;

        Vector3 result = velocityXZ_normal + new Vector3(0, velocityY, 0);
        return result;
    }
    

    public void SetGravity(bool useGravity)
    {
        _rigidbody.useGravity = useGravity;
    }
    
    public void SetPosition(Vector3 position)
    {
        if (_rigidbody)
        {
            //Debug.Log("공의 고정 위치 : " + position);
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
    #region DEBUG

    
    /// <summary>
    /// 현재 위치와 속도를 바탕으로 미래의 궤적 데이터를 계산해 반환합니다.
    /// </summary>
    public void PredictTrajectory(Vector3 startPos, Vector3 initialVelocity)
    {
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
                    if (!data.HasPassedStrikeZone) 
                    {
                        data.StrikeZonePoint = hit.point;
                        data.HasPassedStrikeZone = true;
                        
                        // 🚨 주의: bat.MoveAxis는 여기서 호출하지 마! (Physics는 타자를 몰라야 함)
                        // 나중에 이 반환된 데이터를 받은 타자AI나 매니저가 처리해야 해!
                    }
                }
                // 2. 바닥/벽 등 물리적 충돌 확인
                else if (!hit.collider.isTrigger)
                {
                    data.LandingPoint = hit.point;
                    data.HasLanded = true;
                    data.PathPoints.Add(hit.point); // 마지막 도착점 저장
                    break; // 계산 종료
                }
            }

            data.PathPoints.Add(nextP); // 궤적 점 저장
            p = nextP;
        }
    }
    void CalTrajectory(bool isDebug = false)
    {
        Vector3 predictedStrikePos;
        float dashLength = 0.1f; // 그려지는 짧은 선 길이
        float gapLength  = 0.1f; // 대시 사이 공백
        
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

            // 수정된 완벽한 충돌 감지 로직
            if (Physics.Linecast(p, nextP, out var hit, -1, QueryTriggerInteraction.Collide))
            {
                // 1. 만약 부딪힌 게 Trigger(스트라이크 존 등)라면?
                if (hit.collider.isTrigger && (hit.collider.CompareTag("BallZone") || hit.collider.CompareTag("StrikeZone")))
                {
                    // 존을 관통하는 위치만 쓱 기록하고 break는 하지 않음! (궤적 통과)
                    if (!hasPassedStrikeZone) 
                    {
                        predictedStrikePos = hit.point; // 관통한 정확한 좌표
                        hasPassedStrikeZone = true;
                        bat.MoveAxis(predictedStrikePos);
                    }
                }
                // 2. 만약 부딪힌 게 진짜 물리적인 벽이나 땅이라면?
                else if(!hit.collider.isTrigger)
                {
                    if(isDebug) 
                        Gizmos.DrawWireSphere(hit.point, 0.2f);
            
                    _targetPosition = hit.point; // 최종 도착 지점 기록
                    //Debug.Log("[Defender] : " + _targetPosition);

                    break; // 여기서 궤적 그리기 종료
                }
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

    private void DebugDrawSp(Vector3 point)
    {
        // 충돌 지점을 중심으로 평면에 평행한 아주 짧은 선 두 개를 그립니다.
        float crossSize = 0.05f; // 십자선 크기
        Color hitColor = Color.yellow; // 충돌 시 색상
        float duration = 100f; // 시각화 지속 시간
        
        
        // 평면의 로컬 좌표계 기준 방향 벡터 가져오기
        Vector3 right = strikeZone.transform.right;
        Vector3 up = strikeZone.transform.up;
        
        Debug.Log("[Baseball] position : " + point);
        
        // 수평선 그리기
        Debug.DrawLine(point - right * crossSize, point + right * crossSize, hitColor, duration);
        // 수직선 그리기
        Debug.DrawLine(point - up * crossSize, point + up * crossSize, hitColor, duration);
    }

    #endregion

    public TrajectoryBaseBallData GetTrajectoryBaseBallData() => _trajectoryBaseBallData;
}
