/// <summary>
/// 🎯 투수 연습 시스템 통합 관리자 - 스트라이크존 9개 + 볼존 16개 (25존 시스템)
/// </summary>

using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System.Collections;
using TMPro;
/// <summary>
/// pitcher mode. 되도록 많은 부분을 없애면 된다.
/// </summary>
public class PitchingModeManager : MonoBehaviour
{
    [SerializeField] private Batter batter;
    [SerializeField] private Baseball baseball;
    [Header("🎯 존 설정")]
    public Transform strikeZoneParent;

    private int strike = 0;
    private int ball_count = 0;
    
    [Header("📊 확률 설정")]
    [Range(0, 100)]
    public float strikeProbability = 60f;
    
    [Header("🎨 시각화")]
    public bool showZonesInEditor = true;
    public bool showZonesInPlay = false;
    public Material strikeZoneMaterial;
    public Material ballZoneMaterial;
    
    [Header("⚙️ 존 크기")]
    public Vector3 zoneSize = new Vector3(0.167f, 0.33f, 0.1f);
    public float zoneSpacing = 0.167f;
    
    [SerializeField] private PitchingManager pitchingManager;
    [SerializeField] private PitchSelectionUI pitchSelectionUI;

    private Transform ballZoneParent;
    
    // 스트라이크존 중심점과 크기 (기존 9개 영역 기준)
    private Vector3 strikeZoneCenter;
    private Vector3 strikeZoneBounds;
    
    [Header("Events")] 
    [SerializeField] private VoidEventSO backToPitcherEvent; //baseball
    [SerializeField] private VoidEventSO pitchEvent;
    [SerializeField] private Vector3EventSO moveOriginEvent;
    [SerializeField] private Vector3EventSO rotateOriginEvent;

    private void OnEnable()
    {
        backToPitcherEvent.onEventRaised += BackPitcherBall;
        pitchEvent.onEventRaised += WaitingSwing;
        //swingEvent.onEventRaised;
    }

    private void OnDisable()
    {
        backToPitcherEvent.onEventRaised -= BackPitcherBall;
        pitchEvent.onEventRaised -= WaitingSwing;
    }

    void Start()
    {       
        moveOriginEvent.RaiseEvent(new Vector3(0.6f, 1.3f, -0.98f));
        rotateOriginEvent.RaiseEvent(new Vector3(0, -135f, 0));

        InitializeSystem();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            pitchingManager.StartPitchingGame();
        }
    }
    
    /// <summary>
    /// 시스템 초기화
    /// </summary>
    public void InitializeSystem()
    {
        pitchingManager.StartPitchingGame();
    }
    
    private void BackPitcherBall()
    {
        pitchingManager.ResetBall();
    
    }

    private void WaitingSwing()
    {
        Debug.Log("타자 필요 없을지도?");
        //StartCoroutine(StartSwing());
    }


    private IEnumerator StartSwing()
    {
        yield return new WaitForSeconds(1.5f);
        batter.StartSwing();
        
    }
    
}
