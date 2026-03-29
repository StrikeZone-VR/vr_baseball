using System.Collections;
using UnityEngine;

public class PitcherComponent : DefenderComponent
{
    private const float ADDFORCE = 20.0f;
    private Coroutine coroutine;
    [SerializeField] private StrikeZone strikeZone;

    [SerializeField] private VoidEventSO swingEvent; //from GameManager
    [SerializeField] private IntEventSO waitPitcherEvent; //from BattingSystem

    [SerializeField] private float velocityXZ = 40;
    //_myBall

    const int WAIT_TIME = 5; //5.0f
    protected bool isThrowBallStop = false; //debug

    protected override void Update()
    {
        float dis = Vector3.Distance(defenderTransform.position, transform.position);

        if (dis <= 1.0f)
        {
            IsInPosition = true;
        }
        else
        {
            IsInPosition = false;
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
        
        //Debug.Log("SetMyBall"); //수비를 하면 Pitching이 안되는지

        //만약 배트가 터치됐다면 => 경기중
        if (myBall.IsBatTouch)
        {
            return;
        }
        StopPitching();
        coroutine = StartCoroutine(WaitPitching());
        
        //Debug.Log("음? : "+_ball.MyDefender.name);
        //transform.LookAt(_ball.transform, Vector3.up);
    }

    IEnumerator WaitPitching()
    {
        //멈춰야 하거나 내 공이 없다면
        if (IsThrowBallStop || !_myBall)
        {
            yield break;
        }
        LookAtPlayer(strikeZone.transform.position);
        
        //5임
        for (int i = WAIT_TIME; i > 0; i--)
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
        _myBall.IsThrown = true;
        _myBall.HasPassedStrikeZone = false;
        _myBall.IsBatTouch = false;
        
        //random value 0 ~ 24
        int index = Random.Range(0, 25);
        //index = 22; //한 가운데

        Transform SZTransform = strikeZone.GetZone(index);

        //Debug.Log("투수 : " + _ball.transform.position);
        //Debug.Log("스트라이크 존 " + index + " : "+ SZTransform.position);
        Vector3 velocity = new Vector3();
        
        int pitchTypeIndex = Random.Range(0, 10);
        
        if (pitchTypeIndex <= 2)
        {
            _myBall.SelectPitchType = PitchType.Curve;
            Debug.Log("커브");
        }
        else
        {
            _myBall.SelectPitchType = PitchType.FastBall;
            Debug.Log("직구");
        }
        
        if(_myBall.SelectPitchType == PitchType.FastBall)
            velocity = CalculateSimpleVelocity(_myBall.transform.position, SZTransform.position, velocityXZ);
        //else if(_myBall.SelectPitchType == PitchType.Curve)
        else
            velocity = CalculateCurveVelocity(_myBall.transform.position, SZTransform.position, velocityXZ);

        //Debug.Log("속력 : " + velocity.magnitude * 3.6f);

        _myBall.ThrowBall(velocity);
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
        float g = Mathf.Abs(Physics.gravity.y) + (velocityXZ / 100 * _myBall.MAGNUS); // 9.81 (양수)
        Vector3 dis = target - start;

        float mytime = dis.magnitude / velocityXZ;

        float velocityY = mytime / 2 * g;
        Vector3 velocityXZ_normal = dis.normalized;
        velocityXZ_normal *= velocityXZ;

        Vector3 result = velocityXZ_normal + new Vector3(0, velocityY, 0);
        return result;
    }

    public float VelocityXZ
    {
        set
        {
            if (value <= 0 || 200 <= value)
            {
                return;
            }
            velocityXZ = value;
            Debug.Log("속력 설정 : " + velocityXZ);
        }
        get { return velocityXZ; }
    }

    public bool IsThrowBallStop
    {
        get => isThrowBallStop;
        set
        {
            isThrowBallStop = value;
            
            StopPitching();
            //어차피 WaitPitching에서 판별해준다. 
            coroutine = StartCoroutine(WaitPitching());
        }
    }
    public void StopPitching()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine); 
        }
    }
}
