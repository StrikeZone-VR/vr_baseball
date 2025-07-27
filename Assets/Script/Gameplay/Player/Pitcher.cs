using UnityEngine;

public class Pitcher : Defender
{
    //_myBall

    protected override void Start()
    {
        base.Start();
        HaveBall();
    }
    
    protected override void Update()
    {
        base.Update();
        if (Input.GetKeyDown(KeyCode.Space))
            DebugThrowBall();
    }

    private void HaveBall()
    {
        //ready handling ball
        _ball.RemovePlayer();
        _myBall = _ball.gameObject;
    }
    
    //공 던지는 함수
    private void DebugThrowBall()
    {
        //transform.rotation.eulerAngles.x, ADDFORCE, transform.rotation.eulerAngles.z => you should be setting cos sin
        _ball.ThrowBall(new Vector3(0, 0, 0));
        
        //player's
        
    }

}
