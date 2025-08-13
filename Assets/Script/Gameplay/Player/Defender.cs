using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

//수비수
public class Defender : Player
{
    [SerializeField] protected Transform defenderTransform;

    [SerializeField] protected IntEventSO outBatterEventSO; //gamemanager

    [SerializeField] private bool isTracking = false;
    protected bool isInPosition = false;

    protected virtual void Update()
    {
        if (_myBall)
        {
            FrontBall();
        }

        //defend my position
        if (!IsTracking)
        {
            //long base dis => go to the base
            if (!isInPosition)
            {
                nav.SetDestination(defenderTransform.position);
                LookAtPlayer(defenderTransform.position);
            }
            //defend pos
        }
        
    }
    
    //touch ball
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            //owner ball
            SetMyBall(collision.gameObject.GetComponent<Baseball>());
            Baseball baseball = _myBall;
            collision.rigidbody.velocity = Vector3.zero;

            isTracking = false;
            baseball.MyDefender = this;
            
            OutRunner();
        }
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
        if (position == transform.position)
        {
            return;
        }
        Vector3 launchVelocity = CalculateLaunchVelocity(transform.position, position, 45f);
        
        //cal dis
        //_ball.ThrowBall(dir * dis);
        _ball.ThrowBall(launchVelocity);
    }
    
    public Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 target, float angleDeg)
    {
        float gravity = Physics.gravity.y; // 보통 -9.81f
        float angle = angleDeg * Mathf.Deg2Rad;

        Vector3 direction = target - start;
        Vector3 directionXZ = new Vector3(direction.x, 0, direction.z);
        float distance = directionXZ.magnitude;

        float yOffset = direction.y;

        float velocity = Mathf.Sqrt((gravity * distance * distance) / 
                                    (2 * (yOffset - Mathf.Tan(angle) * distance) * Mathf.Pow(Mathf.Cos(angle), 2)));

        Vector3 launchVelocity = directionXZ.normalized;
        launchVelocity *= velocity * Mathf.Cos(angle);
        launchVelocity.y = velocity * Mathf.Sin(angle);

        return launchVelocity;
    }
    
    public void SetMyBall(Baseball myBall)
    {
        _myBall = myBall;
        _myBall.MyDefender = this;
        IsTracking = false;

        transform.LookAt(_ball.transform, Vector3.up);
    }


    //out decision
    protected virtual void OutRunner()
    {
        bool isGroundball = _myBall.IsGroundBall;
        bool isBatTouch = _myBall.IsBatTouch;

        //flying out
        if (isBatTouch && !isGroundball)
        {
            outBatterEventSO.RaiseEvent(0);
        }
    }

    #region PROPERTIES
    public bool IsTracking
    {
        get => isTracking;
        set
        {
            isTracking = value;
            if (!nav)
            {
                return;
            }
            if (!isTracking )
            {
                nav.ResetPath();
            }
            else
            {
                nav.SetDestination(_ball.transform.position);
            }
            
        }
    }
    #endregion
}
