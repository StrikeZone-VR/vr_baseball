using UnityEngine;

public class Pitcher : Defender
{
    private const float ADDFORCE = 20.0f;

    //_myBall

    protected void Start()
    {
        HaveBall();
        
    }

    protected override void Update()
    {
        float dis = Vector3.Distance(defenderTransform.position, transform.position);

        //shoot
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (!_ball.MyDefender)
            {
                SetMyBall(_ball);
                _ball.IsGroundBall = false;
                _ball.IsPassing = false;
            }
            else
            {
                PitchingBall();
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

    public void HaveBall()
    {
        //ready handling ball
        _ball.RemovePlayer();
        SetMyBall(_ball);
    }
    
    //공 던지는 함수
    public void PitchingBall()
    {
        //Debug.Log("Throwing ball" + transform.rotation.eulerAngles.x + ", " + transform.rotation.eulerAngles.z);
        //transform.rotation.eulerAngles.x, ADDFORCE, transform.rotation.eulerAngles.z => you should be setting cos sin
        
        
        float x = ADDFORCE * Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.PI / 180);
        float z = ADDFORCE * Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.PI / 180);

        // _ball.RemovePlayer(); => throw ball 
        _ball.ThrowBall(new Vector3(x, ADDFORCE * 0.2f ,z));
        
        //player's
        
    }

}
