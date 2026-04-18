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

    protected virtual void Update()
    {
        
        if (_myBall)
        {
            FrontBall();
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

    private void FixedUpdate()
    {
    }

    protected void LookAtPlayer(Vector3 targetPosition)
    {
        player.LookAtPlayer(targetPosition);
        FrontBall();
    }

    private void FrontBall()
    {
        // 내 현재 위치 + (내가 바라보는 정면 방향 * 0.5f 거리)
        Vector3 frontPosition = transform.position + (transform.forward * BALL_DISTANCE) + (Vector3.up * 0.5f);
        _myBall.GetComponent<BaseballPhysics>().SetPosition(frontPosition);
    }
    
    //touch ball
    void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("이게 업데이트마냥 안뜬다?");
        //flyout 
        
        //Catch
        //공을 건드린 경우
        //ㄴ  물리적으로 공을 가지고있는 상태에서 MyDefender를 빠져 나간경우
        //    계속 투수가 따라가서 enter 조건이 안 생겨서 SetBall을 설정할 수 없다.
        //    ㄴ 근데 또 그러면 던졌는데 받았다 기술로 이상한 아웃이 생길 수 있음
        //       ㄴ 어차피 디버깅 안타 함수도 오류 해결해서 Stay 함수 제거함.
        if (collision.gameObject.CompareTag("Ball") 
            && player.GetBallDefender() == null)
        {
            Baseball baseball = collision.gameObject.GetComponent<Baseball>();
            //owner ball
            SetMyBall(baseball);
            isTracking = false;
            
            OutRunner();
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
        //Debug.Break();
        
        //만약 던졌는데 던진 공이 닿아서 다시 붙여지는 경우
        // if (Vector3.Distance(position, transform.position) <= 0.6f)
        // {
        //     Debug.LogError("??????????");
        //     return;
        // }
        //pass
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
            
            //분명 movePlayer에 !nav를 했는데...
            
            //공 패스중이면 그냥 대기해라
            if (player.IsPassingBall())
            {
                // if (gameObject.name == "Catcher")
                // {
                //     Debug.Log("[catchman] : um");
                // }
                isTracking = false;
            }
            
            if (!isTracking)
            {
                //long base dis => go to the base
                if (IsInPosition)
                {
                    // if (gameObject.name == "Catcher")
                    // {
                    //     Debug.Log("[catchman] : um2");
                    // }
                    player.StopMove();
                }
                else
                {
                    // if (gameObject.name == "Catcher")
                    // {
                    //     Debug.Log("<color=green>[catchman] : 썸띵온유얼 마인</color>" + defenderTransform.position);
                    // }
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
