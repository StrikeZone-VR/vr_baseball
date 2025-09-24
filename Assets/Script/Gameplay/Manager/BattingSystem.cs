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

    [SerializeField] private AssetReference menuScene;
    [SerializeField] private TextMeshProUGUI hitText;
    [SerializeField] private TextMeshProUGUI foulText;
    [SerializeField] private TextMeshProUGUI strikeText;
    [SerializeField] private TextMeshProUGUI homerunText;
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
    [Header("BallEvents")] 
    [SerializeField] private VoidEventSO hitEventSO;
    [SerializeField] private VoidEventSO foulEventSO;
    [SerializeField] private VoidEventSO strikeEventSO;
    [SerializeField] private VoidEventSO homerunEventSO;
    [SerializeField] private FloatEventSO getVelocityEventSO;

    private void OnEnable()
    {
        hitEventSO.onEventRaised += AddHit;
        foulEventSO.onEventRaised += AddFoul;
        strikeEventSO.onEventRaised += AddStrike;
        homerunEventSO.onEventRaised += AddHomerun;

        waitPitcherEvent.onEventRaised += WaitPitchingToText;
        getVelocityEventSO.onEventRaised += SetVelocityToText;
    }

    private void OnDisable()
    {
        hitEventSO.onEventRaised -= AddHit;
        foulEventSO.onEventRaised -= AddFoul;
        strikeEventSO.onEventRaised -= AddStrike;
        homerunEventSO.onEventRaised -= AddHomerun;

        waitPitcherEvent.onEventRaised -= WaitPitchingToText;
        getVelocityEventSO.onEventRaised -= SetVelocityToText;
    }

    private void Start()
    {
        moveOriginEvent.RaiseEvent(new Vector3(0.75f, 1.3f, -0.95f));
        rotateOriginEvent.RaiseEvent(new Vector3(0, -135f, 0));
        pitcher.SetMyBall(_ball);

        HitCount = 0;
        StrikeCount = 0;
        HomerunCount = 0;
        FoulCount = 0;

        //StartCoroutine(BackPitching());
    }

    IEnumerator BackPitching()
    {
        Debug.Log("5초후에 돌아옴");
        yield return new WaitForSeconds(5f);
        pitcher.SetMyBall(_ball);
    }

    private void WaitPitchingToText(int time)
    {
        waitText.text = time.ToString();
        if (time == 3)
        {
            playAudioClipEvent.RaiseEvent(2);
        }
        if (time == 1)
        {
            if(_ball.IsBatTouch)
            {
                Debug.Log("배트를 건드려서 일단 무시 -> 나중에 땅 건드리면 backPitching 함수 실행해야함");
                return;
            }
            StartCoroutine(BackPitching());
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
        StartCoroutine(BackPitching());
        HitCount++;
    }

    void AddStrike()
    {
        StrikeCount++;
    }

    void AddHomerun()
    {
        StartCoroutine(BackPitching());
        HomerunCount++;
    }

    void AddFoul()
    {
        StartCoroutine(BackPitching());
        FoulCount++;
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