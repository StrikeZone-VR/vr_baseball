using UnityEngine;

public class Pitcher : Defender
{
    private const float ADDFORCE = 500.0f;

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
            PitchBall();
    }

    private void HaveBall()
    {
        //ready handling ball
        _ball.RemovePlayer();
        SetMyBall(_ball.gameObject);
    }
    
    //공 던지는 함수
    private void PitchBall()
    {
        //Debug.Log("Throwing ball" + transform.rotation.eulerAngles.x + ", " + transform.rotation.eulerAngles.z);
        //transform.rotation.eulerAngles.x, ADDFORCE, transform.rotation.eulerAngles.z => you should be setting cos sin
        
        float x = ADDFORCE * Mathf.Sin(transform.rotation.eulerAngles.y * Mathf.PI / 180);
        float z = ADDFORCE * Mathf.Cos(transform.rotation.eulerAngles.y * Mathf.PI / 180);
        
        _ball.ThrowBall(new Vector3(x, ADDFORCE,z));
        
        //player's
        
    }

}
