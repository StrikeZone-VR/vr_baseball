using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

public class BattingController : GameController
{
    [SerializeField] private PitcherComponent pitcherComponent;
    [SerializeField] private Baseball ball;

    [Header("투수 데이터")]
    //지금 마운드에 선 투수. 여기서 받아서 PitcherComponent로 내려보낸다.
    //ㄴ 컴포넌트가 직접 물고 있으면 선수를 바꿀 때마다 씬 오브젝트를 만져야 해서 컨트롤러가 쥔다.
    [SerializeField] private PitcherSO pitcherSO;

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
        //공을 넘기기 전에 투수 데이터를 먼저 내려보낸다.
        //ㄴ Init()이 아니라 여기에 두는 이유: 통합 씬의 GamePlayManager는 Init()을 안 부르고
        //   PitcherGetBall()만 부른다. 여기 두어야 단독 씬/통합 씬 양쪽에 다 걸린다.
        ApplyPitcherData();

        pitcherComponent.SetMyBall(ball);
    }

    /// <summary>
    /// 현재 투수 데이터를 PitcherComponent로 주입한다.
    /// </summary>
    private void ApplyPitcherData()
    {
        if (pitcherComponent != null)
        {
            pitcherComponent.PitcherData = pitcherSO;
        }
    }

    /// <summary>
    /// 런타임에 투수를 갈아끼울 때 사용 (선수 교체, 이닝 교대 등).
    /// </summary>
    public void SetPitcherData(PitcherSO data)
    {
        pitcherSO = data;
        ApplyPitcherData();
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
        pitcherComponent.VelocityXZ += 10f;
        SetVelocityControllerText(pitcherComponent.VelocityXZ);
    }
    public void MinusVelocityBall()
    {
        pitcherComponent.VelocityXZ -= 10f;
        SetVelocityControllerText(pitcherComponent.VelocityXZ);
    }
}
