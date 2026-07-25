using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Serialization;

//게임 시작할때 실행되는 GameManager
public class GameManager : MonoBehaviour
{
    [SerializeField] protected Baseball _ball; //일단 PitchingBallController도 여깄음

    [Header("Broadcasting on EventChannels")] 
    [SerializeField] private VoidEventSO addBallCountEvent; //to Baseball
    [SerializeField] private VoidEventSO strikeEvent; // toStrikeZone, batter
    [SerializeField] private VoidEventSO foulEvent; // toStrikeZone
    [SerializeField] private VoidEventSO homerunEvent; // toStrikeZone

    [Space]
    [SerializeField] private VoidEventSO backToPitcherEvent; //?
    [SerializeField] private FloatEventSO getVelocityEvent; //ball?
    [SerializeField] private IntEventSO waitPitcherEvent;

    //가져오기
    [Header("Listening to EventChannels")] 
    [SerializeField] protected Vector3EventSO moveOriginEvent;
    [SerializeField] protected Vector3EventSO rotateOriginEvent;
    
    [SerializeField] protected SceneEventSO sceneEventSO;
    [SerializeField] protected IntEventSO playAudioClipEvent;

    [SerializeField] protected BaseballModel baseballModel;

    
    protected virtual void OnEnable()
    {
        addBallCountEvent.onEventRaised += AddBallCount;
        strikeEvent.onEventRaised += AddStrike;
        foulEvent.onEventRaised += Foul;
        homerunEvent.onEventRaised += Homerun;

        backToPitcherEvent.onEventRaised += PitcherGetBall;
        getVelocityEvent.onEventRaised += SetVelocityToText;
        waitPitcherEvent.onEventRaised += WaitPitchingToText;
    }

    protected virtual void OnDisable()
    {
        addBallCountEvent.onEventRaised -= AddBallCount;
        strikeEvent.onEventRaised -= AddStrike;
        foulEvent.onEventRaised -= Foul;
        homerunEvent.onEventRaised -= Homerun;

        backToPitcherEvent.onEventRaised -= PitcherGetBall;
        getVelocityEvent.onEventRaised -= SetVelocityToText;
        waitPitcherEvent.onEventRaised -= WaitPitchingToText;
    }

    protected virtual void Start()
    {
        baseballModel.Init();
    }

    #region PROPERTY

    public virtual int Strike //나중에 battingSystem에서는 override
    {
        get => baseballModel.Strike;
        set => baseballModel.Strike = value;
    }
    
    public virtual int BallCount //나중에 battingSystem에서는 override
    {
        get => baseballModel.BallCount;
        set => baseballModel.BallCount = value;
    }

    protected virtual void AddBallCount()
    {
        BallCount++;
    }

    protected virtual void AddStrike()
    {
        Strike++;
    }

    //pitcher에서는 안 쓰일거다. batter와 gameplay에서 쓰인다
    protected virtual void Foul() { }
    protected virtual void Homerun() {}

    #endregion

    //Gameplay에서는 알아서 사용
    protected virtual void PitcherGetBall() { }
    protected virtual void SetVelocityToText(float velocity) {}
    
    //PitchingManager에서는 안 쓰일 예정. 투수 대기 함수
    protected virtual void WaitPitchingToText(int time) { }


}
