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
        
        Debug.Log("[Pitcher] : SetMyBall"); //수비를 하면 Pitching이 안되는지

        //만약 배트가 터치됐다면 => 경기중
        if (myBall.IsInGamePlay)
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

        //스트라이크 보기
        LookAtPlayer(strikeZone.transform.position);
        
        //5임
        for (int i = WAIT_TIME; i > 0; i--)
        {
            waitPitcherEvent.RaiseEvent(i);
            yield return new WaitForSeconds(1.0f);
        }
        
        Vector3 targetPosition = strikeZone.GetZone(4).position; //랜덤넣자
        
        coroutine = null;
        _myBall.ThrowBall(
            _myBall.transform.position,
            targetPosition,
            velocityXZ,
            true
        );
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
