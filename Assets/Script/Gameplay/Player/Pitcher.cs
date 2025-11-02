using System.Collections;
using UnityEngine;

public class Pitcher : Defender
{
    private const float ADDFORCE = 20.0f;
    private Coroutine coroutine;
    [SerializeField] private StrikeZone strikeZone;
    [SerializeField] private VoidEventSO swingEvent; //from GameManager
    [SerializeField] private IntEventSO waitPitcherEvent; //from BattingSystem

    //_myBall


    protected override void Update()
    {
        float dis = Vector3.Distance(defenderTransform.position, transform.position);

        //keeping
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (!_ball.MyDefender)
            {
                SetMyBall(_ball);
                _ball.IsGroundBall = false;
                _ball.IsPassing = false;
            }
        }
        
        if (dis <= 1.0f)
        {
            isInPosition = true;
        }
        else
        {
            isInPosition = false;
        }

        base.Update();
    }
    
    //protected override void Update()
    //{
    //    base.Update();
    //    if (Input.GetKeyDown(KeyCode.Space))
    //        PitchBall();
    //}

    
    //이걸로 공 설정해라
    public override void SetMyBall(Baseball myBall)
    {
        base.SetMyBall(myBall);

        Debug.Log("백백");
        _ball.IsThrown = false;
        _ball.IsGroundBall = false;
        _ball.IsPassing = false;
        _ball.IsZone = false;
        _ball.IsStrike = false;
        
        if (coroutine == null)
        {
            coroutine = StartCoroutine(WaitBatting());
        }
        //transform.LookAt(_ball.transform, Vector3.up);
    }
    
    IEnumerator WaitBatting()
    {
        for (int i = 5; i > 0; i--)
        {
            waitPitcherEvent.RaiseEvent(i);
            yield return new WaitForSeconds(1.0f);
        }

        coroutine = null;
        PitchingBall();
    }
    
    
    //AI 공 던지는 함수
    public void PitchingBall()
    {
        _ball.IsThrown = true;
        _ball.IsBatTouch = false;
        //Debug.Log("Throwing ball" + transform.rotation.eulerAngles.x + ", " + transform.rotation.eulerAngles.z);
        //transform.rotation.eulerAngles.x, ADDFORCE, transform.rotation.eulerAngles.z => you should be setting cos sin
        
        //random value 0 ~ 24
        int index = Random.Range(0, 25);
        Transform SZTransform = strikeZone.GetZone(index);

        //Vector3 velocity = CalculateLaunchVelocity(transform.position, SZTransform.position - new Vector3(0,0.9f,0), 0.9f);
        Vector3 velocity = CalculateVelocity(
            transform.position,
            SZTransform.position - new Vector3(0,0.9f,0),
            100f
        );
        
        //Debug.Log("속력 : " + velocity.magnitude * 3.6f);

        //player's
        // _ball.RemovePlayer(); => throw ball => new Vector3(x, ADDFORCE * 0.2f ,z) 
        _ball.ThrowBall(velocity);
        StartCoroutine(Swing());
    }

    IEnumerator Swing()
    {
        yield return new WaitForSeconds(0.5f); 
        swingEvent.RaiseEvent();
    }

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


}
