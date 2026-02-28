using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

//수비수
public class Defender : Player
{
    [SerializeField] protected Transform defenderTransform;

    [SerializeField] protected VoidEventSO flyingOutEvent; //gameplayManager

    [SerializeField] private bool isTracking = false;
    [SerializeField] protected bool isInPosition = false;
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
        //Vector3.Distance(transform.position, defenderTransform.position);
    }

    protected override void LookAtPlayer(Vector3 targetPosition)
    {
        base.LookAtPlayer(targetPosition);
        
        FrontBall();
    }
    
    //touch ball
    void OnCollisionEnter(Collision collision)
    {
        //flyout 
        
        //Catch
        //공을 건드린 경우
        //ㄴ  물리적으로 공을 가지고있는 상태에서 MyDefender를 빠져 나간경우
        //    계속 투수가 따라가서 enter 조건이 안 생겨서 SetBall을 설정할 수 없다.
        //    ㄴ 근데 또 그러면 던졌는데 받았다 기술로 이상한 아웃이 생길 수 있음
        //       ㄴ 어차피 디버깅 안타 함수도 오류 해결해서 Stay 함수 제거함.
        if (collision.gameObject.CompareTag("Ball") && _ball.MyDefender == null)
        {
            //owner ball
            SetMyBall(collision.gameObject.GetComponent<Baseball>());
            Baseball baseball = _myBall;
            
            collision.rigidbody.velocity = Vector3.zero;
            baseball.MyDefender = this;
            isTracking = false;
            
            OutRunner();
        }
    }

    protected void FrontBall()
    {
        if (!_myBall)
        {
            return;
        }
        float x = Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.PI / 180);
        float z = Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.PI / 180);

        //player angle
        _myBall.SetPosition(
            transform.position 
            + new Vector3(BALL_DISTANCE * x, 0.5f, BALL_DISTANCE * z)
        );
    }

    public void RemoveBall()
    {
        _myBall = null;
    }
    
    
    //position => direction
    public void ThrowBall(Vector3 position)
    {
        LookAtPlayer(position);

        // float x = Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.PI / 180);
        // float z = Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.PI / 180);
        //
        // //front my ball
        // _myBall.transform.position = transform.position + new Vector3(BALL_DISTANCE * x, 0, BALL_DISTANCE * z);
        // float dis = Mathf.Sqrt(Mathf.Pow(position.x - transform.position.x, 2) + Mathf.Pow(position.z - transform.position.z, 2));
        // Vector3 dir = new Vector3(x, 1, z);
        // dir.Normalize();
        // dis *= 0.75f;
        
        //Debug.Log(dis);

        //have ball
        if (Vector3.Distance(position, transform.position) <= 0.3f)
        {
            return;
        }
        Vector3 launchVelocity = CalculateLaunchVelocity(transform.position, position, 45f);

        //cal dis
        //_ball.ThrowBall(dir * dis);
        
        _ball.IsPassing = true;
        _ball.ThrowBall(launchVelocity);
    }
    
    public Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 target, float angleDeg)
    {
        float gravity = Physics.gravity.y; // 보통 -9.81f
        float angle = angleDeg * Mathf.Deg2Rad; //각도?

        Vector3 direction = target - start;
        Vector3 directionXZ = new Vector3(direction.x, 0, direction.z);
        float distance = directionXZ.magnitude;

        float yOffset = direction.y;
        Vector3 launchVelocity = directionXZ.normalized;
        
        // Debug.Log("정제되지 않은 yOffset : "+yOffset); 
        // Debug.Log("정제되지 않은 Tan : "+Mathf.Tan(angle)); 
        // Debug.Log("정제되지 않은 distance : "+distance); 
        //
        // Debug.Log("정제되지 않은 a : "+2 * (yOffset - Mathf.Tan(angle) * distance)); //진짜 음수 => a 자체가 양수임
        // Debug.Log("정제되지 않은 분모 : "+2 * (yOffset - Mathf.Tan(angle) * distance) * Mathf.Pow(Mathf.Cos(angle), 2)); //여기가 음수가 나와야지
        // Debug.Log("정제되지 않은 제곱 : "+(gravity * distance * distance) / 
        //     (2 * (yOffset - Mathf.Tan(angle) * distance) * Mathf.Pow(Mathf.Cos(angle), 2)));
        
        // Debug.Log("정제되지 않은 제곱 : "+(gravity * distance * distance) / (2 * (yOffset - Mathf.Tan(angle) * distance) * Mathf.Pow(Mathf.Cos(angle), 2)));

        float velocity = Mathf.Sqrt((gravity * distance * distance) / 
                                    (2 * (yOffset - Mathf.Tan(angle) * distance) * Mathf.Pow(Mathf.Cos(angle), 2)));
        

        
        launchVelocity *= velocity * Mathf.Cos(angle); //속력 추가, 단 y는 제외
        launchVelocity.y = velocity * Mathf.Sin(angle);

        return launchVelocity;
    }
    
    public virtual void SetMyBall(Baseball myBall)
    {
        myBall.RemoveDefender();
        _myBall = myBall;
        _myBall.MyDefender = this;
        FrontBall();
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
        bool isBatTouch = _myBall.IsBatTouch;

        //flying out
         if (isBatTouch && !isGroundball)
         {
             Debug.Log("[Batting] : flying out");
             _myBall.IsGroundBall = true; //어차피 플라잉 아웃 한번 잡으면 돌아가야함
             
             flyingOutEvent.RaiseEvent();
         }
    }

    #region PROPERTIES
    public bool IsTracking
    {
        get => isTracking;
        set
        {
            isTracking = value;

            // if (gameObject.name == "ShortStop")
            // {
            //     Debug.Log("ShortStop : " + isTracking);
            // }
            
            if (!nav)
            {
                return;
            }

            //공 패스중이면 그냥 대기해라
            if (_ball.IsPassing)
            {
                isTracking = false;
            }
            
            if (!isTracking)
            {
                //long base dis => go to the base
                if (IsInPosition)
                {
                    StopMove();
                }
                else
                {
                    MovePlayer(defenderTransform.position);
                }
            }
            else //istracking 
            {
                MovePlayer(_ball.GetTargetPosition());
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
