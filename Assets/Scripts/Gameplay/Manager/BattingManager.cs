using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class BattingManager : GameManager
{
    [SerializeField] private BattingController battingController;
    [SerializeField] private AssetReference menuScene;

    [Space] 
    [Header("Broadcasting on BallEvents")] 
    [SerializeField] private VoidEventSO hitEventSO;
    [SerializeField] private VoidEventSO groundEventSO;

    const float WAIT_TIME = 2.0f; //원래 7임
    [SerializeField] private BattingModel battingModel;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        groundEventSO .onEventRaised += AddGroundBallCount;
        hitEventSO.onEventRaised += AddHit;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        groundEventSO.onEventRaised -= AddGroundBallCount;
        hitEventSO.onEventRaised -= AddHit;
    }

    protected override void Start()
    {
        base.Start();
        moveOriginEvent.RaiseEvent(new Vector3(0.6f, 1.3f, -0.98f));
        rotateOriginEvent.RaiseEvent(new Vector3(0, -135f, 0));

        battingController.Init();

        HitCount = 0;
        Strike = 0;
        BallCount = 0;
        HomerunCount = 0;
        FoulCount = 0;

        //StartCoroutine(BackPitching());
    }

    protected override void PitcherGetBall()
    {
        StartCoroutine(WaitingBackToPitcher());
    }
    IEnumerator WaitingBackToPitcher()
    {
        _ball.IsInGamePlay = false; //true인 순간 SetBall에서 투수가 공을 안던짐
        //StartCoroutine(BackPitching());

        yield return new WaitForSeconds(WAIT_TIME);
        battingController.PitcherGetBall();
    }

    protected override void WaitPitchingToText(int time)
    {
        battingController.WaitPitchingToText(time);
        if (time == 3)
        {
            playAudioClipEvent.RaiseEvent(2);
        }
    }

    protected override void SetVelocityToText(float velocity)
    {
        battingController.SetVelocityToText(velocity);
    }


    //all base ball script
    //foul to gamemanager
    //

    public void BackMenuScene()
    {
        playAudioClipEvent.RaiseEvent(0); //play click sound
        sceneEventSO.RaiseEvent(menuScene);
    }

    #region PROTERTYS
    public int HitCount
    {
        get { return battingModel.Hit; }
        set
        {
            battingModel.Hit = value;
            battingController.SetHitText(value);
        }
    }

    //안타
    void AddHit()
    {
        ++HitCount;
        PitcherGetBall(); //BatterMode는 수비수가 없으니 안타 후 공을 바로 투수에게 복귀
    }

    void AddGroundBallCount()
    {
        ++GroundBallCount;
    }



    public override int BallCount
    {
        get { return baseballModel.BallCount; }
        set
        {
            baseballModel.BallCount = value;
            battingController.SetBallCountToText(value);
        }
    }
    
    public override int Strike
    {
        get { return baseballModel.Strike; }
        set
        {
            baseballModel.Strike = value;
            battingController.SetStrikeToText(value);
        }
    }

    protected override void Homerun()
    {
        ++HomerunCount;
    }
    protected override void Foul()
    {
        ++FoulCount;
    }
    public int HomerunCount
    {
        get { return battingModel.Homerun; }
        set
        {
            battingModel.Homerun = value;
            battingController.SetHomerunToText(value);
        }
    }

    public int FoulCount
    {
        get { return battingModel.Foul; }
        set
        {
            battingModel.Foul = value;
            battingController.SetFoulToText(value);
        }
    }
    public int GroundBallCount
    {
        get { return battingModel.GroundBall; }
        set
        {
            battingModel.GroundBall = value;
            battingController.SetGroundballToText(value);
        }
    }
    #endregion
}