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
    
    
    //공 던지는 함수
    public void PitchingBall()
    {
        _ball.IsThrown = true;
        _ball.IsBatTouch = false;
        //Debug.Log("Throwing ball" + transform.rotation.eulerAngles.x + ", " + transform.rotation.eulerAngles.z);
        //transform.rotation.eulerAngles.x, ADDFORCE, transform.rotation.eulerAngles.z => you should be setting cos sin
        
        //random value 0 ~ 24
        int index = Random.Range(0, 25);
        Transform SZTransform = strikeZone.GetZone(index);

        Vector3 velocity = CalculateLaunchVelocity(transform.position, SZTransform.position - new Vector3(0,0.9f,0), 45f);
        
        float x = ADDFORCE * Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.PI / 180);
        float z = ADDFORCE * Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.PI / 180);

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

}
