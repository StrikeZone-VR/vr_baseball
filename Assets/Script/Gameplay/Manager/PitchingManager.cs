/// <summary>
/// 🎯 VR 투수 게임 메인 매니저 - 공 생성, 던지기, 카운트 관리
/// </summary>

using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;


//pitcherManager ---(SO)---> PitchingBallController
//               ---(SO)---> UI
public class PitchingManager : MonoBehaviour
{
    [Header("게임 오브젝트 참조")]
    public PitchSelectionUI pitchSelectionUI; // 구종 선택 UI
    [SerializeField] private PitchingBallController ball;

    
    [Header("게임 설정")]
    private Vector3 ballResetPosition = new Vector3(-9f, 0.5f, -9f); 
    public int maxBalls = 10;               // 최대 공 개수 (5에서 10으로 증가)
    public float ballResetDelay = 3.0f;     // 공 리셋 딜레이 (착지 후 3초간 보여줌)

    [Header("오디오")]
    public AudioClip gameStartSound;
    public AudioClip strikeSound;
    public AudioClip ballSound;

    private AudioSource audioSource;
    private int ballsThrown = 0;
    private List<GameObject> thrownBalls = new List<GameObject>();  // 던진 공들 관리

    // 통계
    private int strikes = 0;
    private int balls = 0;

    [Header("broadcasting on Events")]
    public System.Action<int, int> OnCountChanged; // strikes, balls
    public System.Action<bool> OnPitchResult;      // isStrike

    #region EventFunction

    private void OnEnable()
    {
        pitchSelectionUI.OnPitchSelected += OnPitchTypeSelected;
    }

    private void OnDisable()
    {
        pitchSelectionUI.OnPitchSelected -= OnPitchTypeSelected;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    // 디버그용 키보드 컨트롤
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.R))
            ResetGame();
        if (Input.GetKeyDown(KeyCode.U))
            ToggleUI();
        if (Input.GetKeyDown(KeyCode.N))
            ResetBall();
#endif
    }
    
    #endregion

    //pitch start
    public void StartPitchingGame()
    {
        ballsThrown = 0; // 기존 공은 카운트하지 않음
        ResetBall();
        
        // UI 초기화
        if (pitchSelectionUI != null)
        {
            pitchSelectionUI.ShowUI();
        }

        // 게임 시작 사운드
        if (audioSource != null && gameStartSound != null)
            audioSource.PlayOneShot(gameStartSound);
    }

    public void EndPitchingGame()
    {
        pitchSelectionUI.HideUI();
    }

    //SpawnNewBall => InitBall
    public void ResetBall()
    {
        ball.GetComponent<Baseball>().RemovePlayer();
        // **한 프레임 뒤에 물리 설정 - VRBaseball Start() 후에 실행되도록!**
        StartCoroutine(SetupBallAfterFrame());

        // XR Grab Interactable 강제 활성화 (새 공이 잡힐 수 있도록)
        XRGrabInteractable grabComponent = ball.GetComponent<XRGrabInteractable>();
        if (grabComponent != null)
        {
            grabComponent.enabled = true;
            
            // kinematic 충돌 방지를 위해 throwOnDetach 비활성화
            grabComponent.throwOnDetach = false;
            
            ball.OffBallPhysics();
            
        } 
        
        // AudioSource가 있는지 확인하고 필요하면 추가
        AudioSource audioSrc = ball.GetComponent<AudioSource>();
        if (audioSrc == null)
        {
            audioSrc = ball.gameObject.AddComponent<AudioSource>();
            Debug.Log("AudioSource 컴포넌트 추가됨");
        }
        audioSrc.enabled = true;

        // UI에 공 등록 ★ => 굳이,,,?
        if (pitchSelectionUI != null)
            pitchSelectionUI.RegisterBaseball(ball);

        ballsThrown++;

        // init ball
        Vector3 finalPosition = ballResetPosition;
        ball.transform.position = finalPosition;
    }

    //ball setting => 제거할 예정, 볼 자체에 넣거나 뭔가 할 예정 ★★★
    private IEnumerator SetupBallAfterFrame()
    {
        yield return null; // 한 프레임 대기
        yield return null; // 한 프레임 더 대기 (안정성 추가)

        try
        {
            // 공이 아직 유효한지 확인
            if (ball == null)
            {
                Debug.LogWarning("SetupBallAfterFrame: currentBall이 null 상태입니다!");
                yield break;
            }

            // get components
            XRGrabInteractable grabInteractable = ball.GetComponent<XRGrabInteractable>();
            PitchingBallController vrBallScript = ball.GetComponent<PitchingBallController>();

            // Baseball 스크립트가 활성화되어 있는지 확인
            if (vrBallScript != null)
            {
                vrBallScript.enabled = true;
                Debug.Log($"VRBaseball 스크립트 상태: {vrBallScript.enabled} (한 프레임 후 확인)");
            }
            else
            {
                Debug.LogError("VRBaseball 스크립트가 없습니다! 공 생성에 문제가 있습니다.");
            }

            // XRGrabInteractable 설정
            if (grabInteractable != null)
            {
                grabInteractable.throwOnDetach = false;  //throwOnDetach 비활성화 (isKinematic과의 충돌 방지)
                grabInteractable.enabled = true;        // 확실히 활성화

                Debug.Log($"🔧 XRGrabInteractable 설정 완료! throwOnDetach: {grabInteractable.throwOnDetach}, enabled: {grabInteractable.enabled}");
            }
            else
            {
                Debug.LogError("XRGrabInteractable 컴포넌트가 없습니다!");
            }
            ball.OffBallPhysics();

            // AudioSource 확인
            AudioSource audioSrc = ball.GetComponent<AudioSource>();
            if (audioSrc != null)
            {
                audioSrc.enabled = true;
                Debug.Log("AudioSource 활성화됨");
            }

            // 위치 재확인
            Vector3 finalPosition = ballResetPosition;
            ball.transform.position = finalPosition;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SetupBallAfterFrame 오류: {e.Message}\n{e.StackTrace}");
        }
    }

    //
    private void SetupExistingBall()
    {
        if (ball == null) return;

        ball.OffBallPhysics();

        // XR Grab Interactable 강제 활성화 및 설정
        XRGrabInteractable grabComponent = ball.GetComponent<XRGrabInteractable>();
        if (grabComponent != null)
        {
            grabComponent.enabled = true;
            // 첫 번째 공은 제대로 동작하므로 기본 설정 유지
            // 씬에 있는 초기 공은 throwOnDetach가 올바르게 설정되어 있을 것임
        }


        // UI에 공 등록
        if (pitchSelectionUI != null)
            pitchSelectionUI.RegisterBaseball(ball);
    }

    private void OnPitchTypeSelected(PitchType pitchType)
    {
        if (ball != null)
            ball.SetPitchType(pitchType);
    }


    private void ResetCount()
    {
        strikes = 0;
        balls = 0;
        OnCountChanged?.Invoke(strikes, balls);
    }

    public void ResetGame()
    {
        // 현재 공 제거
        if (ball != null)
            Destroy(ball.gameObject);

        // 통계 리셋
        ballsThrown = 0;
        ResetCount();

        // 새 게임 시작
        ResetBall();
    }

    public void ToggleUI()
    {
        if (pitchSelectionUI != null)
        {
            if (pitchSelectionUI.pitchSelectionCanvas.gameObject.activeInHierarchy)
                pitchSelectionUI.HideUI();
            else
                pitchSelectionUI.ShowUI();
        }
    }

    public PitchingBallController GetCurrentBall() => ball;

    /// <summary>
    /// 공이 특정 구역에 착지했을 때 호출되는 메서드
    /// </summary>
    /// <param name="isStrike">스트라이크 여부</param>
    /// <param name="zoneName">구역 이름</param>
    public void OnBallResult(bool isStrike, string zoneName)
    {
        Debug.Log($"⚾ 공 결과 수신: {zoneName} - {(isStrike ? "Strike ⚾" : "Ball ❌")}");
        
        // 카운트 업데이트
        if (isStrike)
        {
            strikes++;
            PlayAudio(strikeSound);
            Debug.Log($"⚾ Strike! 현재 카운트: {balls}-{strikes}");
        }
        else
        {
            balls++;
            PlayAudio(ballSound);
            Debug.Log($"❌ Ball! 현재 카운트: {balls}-{strikes}");
        }
        
        // 이벤트 발생
        OnCountChanged?.Invoke(strikes, balls);
        OnPitchResult?.Invoke(isStrike);
        
        // 카운트 리셋 체크 (3볼 또는 3스트라이크)
        if (balls >= 3 || strikes >= 3)
        {
            Debug.Log($"🔄 카운트 리셋! (볼: {balls}, 스트라이크: {strikes})");
            ResetCount();
        }
    }

    private void PlayAudio(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

}