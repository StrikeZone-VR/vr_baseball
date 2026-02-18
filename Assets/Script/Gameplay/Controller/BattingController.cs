using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class BattingController : GameController
{
    [SerializeField] private Pitcher pitcher;
    [SerializeField] private Baseball ball;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hitText;
    [SerializeField] private TextMeshProUGUI groundBallText;
    [SerializeField] private TextMeshProUGUI foulText;
    [SerializeField] private TextMeshProUGUI strikeText;
    [SerializeField] private TextMeshProUGUI homerunText;
    [SerializeField] private TextMeshProUGUI ballCountText;
    
    [SerializeField] private TextMeshProUGUI waitText;
    [SerializeField] private TextMeshProUGUI velocityControllerText;
    [SerializeField] private TextMeshProUGUI velocityText;

    public void Init()
    {
        PitcherGetBall();
    }

    public void PitcherGetBall()
    {
        ball.IsBatTouch = false;
        ball.IsThrown = false;
        ball.IsGroundBall = false;
        ball.IsPassing = false;
        ball.IsZone = false;
        ball.IsStrike = false;
        
        pitcher.SetMyBall(ball);
    }
    
    public void WaitPitchingToText(int time)
    {
        waitText.text = time.ToString();
    }

    public void SetVelocityToText(float velocity)
    {
        velocityText.text = "시속 : " +velocity.ToString("F2") + "km/h";
    }
    public void SetStrikeToText(int strike)
    {
        strikeText.text = strike.ToString();
    }
    public void SetBallCountToText(int ball_count)
    {
        ballCountText.text = ball_count.ToString();
    }
    public void SetFoulToText(int foul)
    {
        foulText.text = foul.ToString();
    }
    public void SetHomerunToText(int homerun)
    {
        homerunText.text = homerun.ToString();
    }

    public void SetGroundballToText(int groundball)
    {
        groundBallText.text = groundball.ToString();
    }

    public void SetHitText(int hit)
    {
        hitText.text = hit.ToString();
    }

    public void SetVelocityControllerText(float velocity)
    {
        velocityControllerText.text = "시속 " + velocity.ToString() + "km/h";
    }
    
    //button function
    
    public void PlusVelocityBall()
    {
        pitcher.VelocityXZ += 10f;
        SetVelocityControllerText(pitcher.VelocityXZ);
    }
    public void MinusVelocityBall()
    {
        pitcher.VelocityXZ -= 10f;
        SetVelocityControllerText(pitcher.VelocityXZ);
    }
}
