using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//수비수
public class Defender : Player
{
    [SerializeField] private VoidEventSO outEventSO;

    private const float BALL_DISTANCE = 0.5f;

    protected virtual void Update()
    {
        //follow ball
        if (!_myBall)
        {
            //Ball이 누군가의 소속이 없다면 => MyPlayer
            if (_ball.MyDefender)
            {
                return;
            }
            //if bat is not touching
            if (!_ball.IsBatTouch)
            {
                return;
            }
            nav.SetDestination(_ball.transform.position);
            LookAtPlayer(_ball.transform.position);
        }
        else //have ball
        {
            float x = Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.PI / 180);
            float z = Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.PI / 180);

            //player angle
            _myBall.transform.position = transform.position + new Vector3(BALL_DISTANCE * x, 0, BALL_DISTANCE * z);
        }
    }
    
    //on
    void OnCollisionEnter(Collision collision)
    {
        //touch ball
        if (collision.gameObject.CompareTag("Ball"))
        {
            SetMyBall(collision.gameObject.GetComponent<Baseball>());
            Baseball baseball = _myBall.GetComponent<Baseball>();
            collision.rigidbody.velocity = Vector3.zero;

            bool isGroundball = baseball.IsGroundBall;
            baseball.MyDefender = this;

            if (!isGroundball)
            {
                outEventSO.Raised();
            }
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

        transform.LookAt(_ball.transform, Vector3.up);
        nav.ResetPath();
    }
}
