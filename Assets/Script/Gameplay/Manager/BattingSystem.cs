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

    [Space] 
    [Header("Events")] 
    [SerializeField] private Vector3EventSO moveOriginEvent;
    [SerializeField] private Vector3EventSO rotateOriginEvent;
    [SerializeField] private SceneEventSO sceneEventSO;

    [Space] 
    [Header("BallEvents")] 
    [SerializeField] private VoidEventSO hitEventSO;
    [SerializeField] private VoidEventSO foulEventSO;
    [SerializeField] private VoidEventSO strikeEventSO;
    [SerializeField] private VoidEventSO homerunEventSO;

    private void OnEnable()
    {
        hitEventSO.onEventRaised += AddHit;
        foulEventSO.onEventRaised += AddFoul;
        strikeEventSO.onEventRaised += AddStrike;
        homerunEventSO.onEventRaised += AddHomerun;
    }

    private void OnDisable()
    {
        hitEventSO.onEventRaised -= AddHit;
        foulEventSO.onEventRaised -= AddFoul;
        strikeEventSO.onEventRaised -= AddStrike;
        homerunEventSO.onEventRaised -= AddHomerun;
    }

    private void Start()
    {
        moveOriginEvent.RaiseEvent(new Vector3(0, 1.3f, 0));
        rotateOriginEvent.RaiseEvent(new Vector3(0, -135f, 0));
        pitcher.SetMyBall(_ball);

        HitCount = 0;
        StrikeCount = 0;
        HomerunCount = 0;
        FoulCount = 0;

        StartCoroutine(WaitPitching());
    }

    IEnumerator WaitPitching()
    {
        yield return new WaitForSeconds(5f);
        pitcher.SetMyBall(_ball);
        StartCoroutine(WaitPitching());
    }
    //all base ball script
    //paul to gamemanager
    //

    public void BackMenuScene()
    {
        sceneEventSO.RaiseEvent(menuScene);
    }

    public int HitCount
    {
        get { return hitCount; }
        set
        {
            hitCount = value;
            hitText.text = hitCount.ToString();
        }
    }

    void AddHit()
    {
        HitCount++;
    }

    void AddStrike()
    {
        StrikeCount++;
    }

    void AddHomerun()
    {
        HomerunCount++;
    }

    void AddFoul()
    {
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
}