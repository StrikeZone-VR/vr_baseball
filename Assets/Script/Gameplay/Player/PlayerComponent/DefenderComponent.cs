using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

//수비수
public class DefenderComponent : PlayerComponent
{
    protected Baseball _myBall = null;

    [SerializeField] protected Transform defenderTransform;

    [SerializeField] protected VoidEventSO flyingOutEvent; //gameplayManager

    [SerializeField] private bool isTracking = false;
    [SerializeField] protected bool isInPosition = false;
    [SerializeField] protected TrajectoryBaseBallData _trajectoryBaseBallData;
    
    private const float BALL_DISTANCE = 0.5f;
    private const float ISINPOSITION_RANGE = 10.0f;
    private const float PICKUP_RANGE = 0.8f; //잠든 공 폴백 픽업 거리(XZ). 머뭇거리면 1.0~1.2로

    protected virtual void Update()
    {
        if (_myBall)
        {
            FrontBall();
        }
        else if (isTracking) //공 쫓아가는 수비수(한 명)만 검사 — 나머지는 비용 0
        {
            TryPickupGroundBall();
        }

        //defend my position
        // if (!IsTracking)
        // {
        //     //long base dis => go to the base
        //     if (!IsInPosition)
        //     {
        //         MovePlayer(defenderTransform.position);
        //     }
        //     //defend pos
        // }
    }

    //잠든 공 폴백 픽업.
    //공이 멀리 굴러가 멈추면 rigidbody가 sleep 상태가 되는데, NavMeshAgent는 transform을
    //직접 움직여서 잠든 공을 깨우지 못한다 → 수비수가 공을 밟고 서 있어도 OnCollisionEnter가
    //그래서 추적 중엔 거리로도 픽업을 판정한다. 공중 공(플라이)은 기존 충돌 판정 그대로.
    private void TryPickupGroundBall()
    {
        Baseball ball = player.GetBaseBall();
        if (ball == null) return;
        if (ball.MyDefenderComponent != null) return; //이미 다른 수비수 소유
        if (!ball.IsInGamePlay) return;               //인플레이 타구만 (데드볼 줍기 방지)
        if (!ball.IsGroundBall) return;               //공중 공은 기존 충돌(캐치) 판정만 — 플라이아웃 유지

        //XZ 평면 거리만 비교 — 공(y≈0.19)과 수비수 원점(발밑 y≈0.04)의 높이차로 판정이 늦어지지 않게
        Vector3 toBall = ball.transform.position - transform.position;
        toBall.y = 0f;
        if (toBall.sqrMagnitude > PICKUP_RANGE * PICKUP_RANGE) return;

        //여기부터는 기존 OnCollisionEnter 성공 경로와 동일하게 처리
        SetMyBall(ball);
        isTracking = false;
        OutRunner(); //isGroundBall이 true라 플라이아웃은 발동 안 함(가드 통과용 동일 처리)
    }

    protected void LookAtPlayer(Vector3 targetPosition)
    {
        player.LookAtPlayer(targetPosition);
        FrontBall();
    }

    
    //touch ball (2)
    void OnCollisionEnter(Collision collision)
    {
        //Catch
        if (collision.gameObject.CompareTag("Ball") 
            && player.GetBallDefender() == null)
        {
            Baseball baseball = collision.gameObject.GetComponent<Baseball>();
            //owner ball
            SetMyBall(baseball);
            isTracking = false;
            
            OutRunner(); //flyout 
        }
    }


    public void RemoveBall()
    {
        _myBall = null;
    }
    
    //position => direction
    public void ThrowBall(Vector3 position)
    {
        LookAtPlayer(position);
        _myBall.ThrowBall(transform.position, position, 45f, false);
    }
    
    /// <summary>
    /// 기본 잡기
    /// </summary>
    /// <param name="myBall"></param>
    public virtual void SetMyBall(Baseball myBall)
    {
        myBall.RemoveDefender();
        _myBall = myBall;
        //Debug.Log("[defender] : 잡잡기");
        _myBall.MyDefenderComponent = this; // _myBall.CurrentState = BallState.Grabbed;

        //IsTracking = false; => 어차피 MyDefender에서 모든 주자가 false임
        //transform.LookAt(_ball.transform, Vector3.up);
    }


    /// <summary>
    /// 플라잉아웃
    /// </summary>
    protected virtual void OutRunner()
    {
        if (!_myBall)
        {
            return;
        }
        
        bool isGroundball = _myBall.IsGroundBall;
        bool isBatTouch = _myBall.IsInGamePlay;

        //flying out
         if (isBatTouch && !isGroundball)
         {
             _myBall.IsGroundBall = true; //어차피 플라잉 아웃 한번 잡으면 돌아가야함
             
             flyingOutEvent.RaiseEvent();
         }
    }
    private void FrontBall()
    {
        // 내 현재 위치 + (내가 바라보는 정면 방향 * 0.5f 거리)
        Vector3 frontPosition = transform.position + (transform.forward * BALL_DISTANCE) + (Vector3.up * 0.5f);
        _myBall.GetComponent<BaseballPhysics>().SetPosition(frontPosition);
    }
    #region PROPERTIES
    /// <summary>
    /// isInPosition 뒤에 호출해라.
    /// </summary>
    public bool IsTracking
    {
        get => isTracking;
        set
        {
            isTracking = value;
            
            //공 패스중이면 그냥 대기해라
            if (player.IsPassingBall())
            {
                isTracking = false;
            }
            
            if (!isTracking)
            {
                if (IsInPosition)
                {
                    player.StopMove();
                }
                else
                {
                    player.MovePlayer(defenderTransform.position);
                }
            }
            else //istracking. 공 줍는 기능
            {
                player.MovePlayer(_trajectoryBaseBallData.GetLandingPoint());
            }
        }
    }

    //디버깅용  처리
    public virtual bool IsInPosition
    {
        get => isInPosition;
        set => isInPosition = value;
    }

    #endregion
}
