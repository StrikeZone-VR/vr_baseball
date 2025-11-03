using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class BattingSystem : MonoBehaviour
{
    [SerializeField] private Baseball _ball;
    [SerializeField] private Pitcher pitcher;

    private int hitCount = 0;
    private int foulCount = 0;
    private int strikeCount = 0;
    private int homerunCount = 0;
    private int ballCount = 0;

    [Header("UI")]
    [SerializeField] private AssetReference menuScene;
    [SerializeField] private TextMeshProUGUI hitText;
    [SerializeField] private TextMeshProUGUI foulText;
    [SerializeField] private TextMeshProUGUI strikeText;
    [SerializeField] private TextMeshProUGUI homerunText;
    [SerializeField] private TextMeshProUGUI ballCountText;
    
    [SerializeField] private TextMeshProUGUI waitText;
    [SerializeField] private TextMeshProUGUI velocityText;
    
    [Space] 
    [Header("Events")] 
    [SerializeField] private Vector3EventSO moveOriginEvent;
    [SerializeField] private Vector3EventSO rotateOriginEvent;
    [SerializeField] private IntEventSO waitPitcherEvent;
    [SerializeField] private SceneEventSO sceneEventSO;
    [SerializeField] private IntEventSO playAudioClipEvent;

    [Space] 
    [Header("Listening to BallEvents")] 
    [SerializeField] private VoidEventSO hitEventSO;
    [SerializeField] private VoidEventSO foulEventSO;
    [SerializeField] private VoidEventSO strikeEventSO;
    [SerializeField] private VoidEventSO ballEventSO;
    [SerializeField] private VoidEventSO homerunEventSO;
    [SerializeField] private VoidEventSO backToPitcherEvent; //to baseball

    [SerializeField] private FloatEventSO getVelocityEventSO; //ball?

    private void OnEnable()
    {
        hitEventSO.onEventRaised += AddHit;
        foulEventSO.onEventRaised += AddFoul;
        strikeEventSO.onEventRaised += AddStrike;
        homerunEventSO.onEventRaised += AddHomerun;
        ballEventSO.onEventRaised += AddBallCount;
        backToPitcherEvent.onEventRaised += BackBallToPitcher;

        waitPitcherEvent.onEventRaised += WaitPitchingToText;
        getVelocityEventSO.onEventRaised += SetVelocityToText;
    }

    private void OnDisable()
    {
        hitEventSO.onEventRaised -= AddHit;
        foulEventSO.onEventRaised -= AddFoul;
        strikeEventSO.onEventRaised -= AddStrike;
        homerunEventSO.onEventRaised -= AddHomerun;
        ballEventSO.onEventRaised -= AddBallCount;
        backToPitcherEvent.onEventRaised -= BackBallToPitcher;


        waitPitcherEvent.onEventRaised -= WaitPitchingToText;
        getVelocityEventSO.onEventRaised -= SetVelocityToText;
    }

    private void Start()
    {
        moveOriginEvent.RaiseEvent(new Vector3(0.6f, 1.3f, -0.98f));
        rotateOriginEvent.RaiseEvent(new Vector3(0, -135f, 0));
        pitcher.SetMyBall(_ball);

        HitCount = 0;
        StrikeCount = 0;
        HomerunCount = 0;
        FoulCount = 0;

        //StartCoroutine(BackPitching());
    }

    private void BackBallToPitcher()
    {
        StartCoroutine(WaitingBackToPitcher());
    }
    IEnumerator WaitingBackToPitcher()
    {
        //StartCoroutine(BackPitching());

        yield return new WaitForSeconds(7f);
        pitcher.SetMyBall(_ball);
    }

    private void WaitPitchingToText(int time)
    {
        waitText.text = time.ToString();
        if (time == 3)
        {
            playAudioClipEvent.RaiseEvent(2);
        }
    }


    private void SetVelocityToText(float velocity)
    {
        velocityText.text = "시속 : " +velocity.ToString() + "km/h";
    }


    //all base ball script
    //paul to gamemanager
    //

    public void BackMenuScene()
    {
        playAudioClipEvent.RaiseEvent(0); //play click sound
        sceneEventSO.RaiseEvent(menuScene);
    }

    #region PROTERTYS
    public int HitCount
    {
        get { return hitCount; }
        set
        {
            hitCount = value;
            hitText.text = hitCount.ToString();
        }
    }

    //안타
    void AddHit()
    {
        ++HitCount;
        BackBallToPitcher();
    }

    void AddStrike()
    {
        ++StrikeCount;
        BackBallToPitcher();
    }

    void AddHomerun()
    {
        ++HomerunCount;
        BackBallToPitcher();
    }

    void AddFoul()
    {        
        ++FoulCount;
        BackBallToPitcher();
    }
    void AddBallCount()
    {        
        ++BallCount;
        BackBallToPitcher();
    }

    
    public int BallCount
    {
        get { return ballCount; }
        set
        {
            ballCount = value;
            ballCountText.text = ballCount.ToString();
        }
    }
    
    public int StrikeCount
    {
        get { return strikeCount; }
        set
        {
            strikeCount = value;
            strikeText.text = strikeCount.ToString();
        }
    }

    public int HomerunCount
    {
        get { return homerunCount; }
        set
        {
            homerunCount = value;
            homerunText.text = homerunCount.ToString();
        }
    }

    public int FoulCount
    {
        get { return foulCount; }
        set
        {
            foulCount = value;
            foulText.text = foulCount.ToString();
        }
    }
    #endregion
}