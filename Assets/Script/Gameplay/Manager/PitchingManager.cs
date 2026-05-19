using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// 
//pitchingManager ---(SO)---> PitchingBallController
//               ---(SO)---> UI
public class PitchingManager : GameManager
{
    [Header("Broadcasting to Events")]
    [SerializeField] private PitchingController pitcherController;
    
    const float WAIT_TIME = 2.0f; //원래 7임
    
    #region EventFunction

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    protected override void Start()
    {
        base.Start();
        
        moveOriginEvent.RaiseEvent(new Vector3(-13.21f, 1.3f, -13.35f));
        rotateOriginEvent.RaiseEvent(new Vector3(0, -135f, 0));

        Strike = 0;
        BallCount = 0;
        StartPitchingGame();
    }
    
    #endregion
    
    //pitch start
    public void StartPitchingGame()
    {
        pitcherController.StartPitchingGame();

        // 게임 시작 사운드
        //playAudioClipEvent.RaiseEvent(2);
    }
    
    public override int Strike //나중에 battingSystem에서는 override
    {
        get => baseballModel.Strike;
        set
        {
            baseballModel.Strike = value;
            pitcherController.SetStrike(value);
            pitcherController.SetTotalBallStatus(baseballModel.Strike + baseballModel.BallCount, baseballModel.Strike);
        }
    }
    
    public override int BallCount //나중에 battingSystem에서는 override
    {
        get => baseballModel.BallCount;
        set
        {
            baseballModel.BallCount = value;
            pitcherController.SetBallCount(value);
            pitcherController.SetTotalBallStatus(baseballModel.Strike + baseballModel.BallCount, baseballModel.Strike);
        }
    }
    
    protected override void SetVelocityToText(float velocity)
    {
        pitcherController.SetVelocityUI(velocity);
    }
    
    protected override void PitcherGetBall()
    {
        StartCoroutine(WaitingBackToPitcher());
    }
    
    IEnumerator WaitingBackToPitcher()
    {
        yield return new WaitForSeconds(WAIT_TIME);
        pitcherController.PlayerPitcherResetBall();
    }

    protected override void Foul()
    {
        Debug.Log("파울");
        pitcherController.PlayerPitcherResetBall();
    }

}