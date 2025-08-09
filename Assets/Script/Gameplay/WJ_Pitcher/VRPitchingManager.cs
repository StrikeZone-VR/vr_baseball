using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using System.Collections;
using System.Collections.Generic;

public class VRPitchingManager : MonoBehaviour
{
    [Header("게임 오브젝트 참조")]
    public Transform pitcherMound;          // 투수 마운드 위치
    public Transform homeplate;             // 홈플레이트 위치  
    public Transform strikeZone;            // 스트라이크 존
    public VRBaseball baseballPrefab;       // 야구공 프리팹
    public PitchSelectionUI pitchSelectionUI; // 구종 선택 UI

    [Header("VR 설정")]
    public XROrigin xrOrigin;               // XR Origin
    public Transform leftController;        // 왼쪽 컨트롤러
    public Transform rightController;       // 오른쪽 컨트롤러

    [Header("게임 설정")]
    public Vector3 ballSpawnOffset = new Vector3(0, 1.5f, 0.5f); // 공 생성 위치 오프셋
    public int maxBalls = 10;               // 최대 공 개수 (5에서 10으로 증가)
    public float ballResetDelay = 1.5f;     // 공 리셋 딜레이 (3f에서 1.5f로 단축)

    [Header("오디오")]
    public AudioClip gameStartSound;
    public AudioClip strikeSound;
    public AudioClip ballSound;

    private AudioSource audioSource;
    private VRBaseball currentBall;
    private int ballsThrown = 0;
    private GameObject originalBall;  // 원본 공 레퍼런스 추가 (스폰용)
    private List<GameObject> thrownBalls = new List<GameObject>();  // 던진 공들 관리

    // 통계
    private int strikes = 0;
    private int balls = 0;

    public System.Action<int, int> OnCountChanged; // strikes, balls
    public System.Action<bool> OnPitchResult;      // isStrike

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        InitializeGame();
    }

    private void InitializeGame()
    {
        // 스트라이크존 태그 설정
        if (strikeZone != null)
            strikeZone.gameObject.tag = "StrikeZone";

        // **씬에 이미 있는 VRBaseball 찾기**
        VRBaseball existingBall = FindObjectOfType<VRBaseball>();
        if (existingBall != null)
        {
            Debug.Log("씬에서 기존 VRBaseball을 찾았습니다. 이것을 첫 번째 공으로 사용합니다.");
            currentBall = existingBall;
            SetupExistingBall();
            ballsThrown = 0; // 기존 공은 카운트하지 않음
        }
        else
        {
            // 기존 공이 없으면 새로 생성
            SpawnNewBall();
        }

        // UI 초기화
        if (pitchSelectionUI != null)
        {
            pitchSelectionUI.OnPitchSelected += OnPitchTypeSelected;
            pitchSelectionUI.ShowUI();
        }

        // 게임 시작 사운드
        if (audioSource != null && gameStartSound != null)
            audioSource.PlayOneShot(gameStartSound);
    }

    private void SpawnNewBall()
    {
        try
        {
            Debug.Log($"SpawnNewBall 호출됨! 현재 공 개수: {ballsThrown}/무제한");

            // 무제한 공 생성 허용 - maxBalls 제한 제거

            // 이전 공의 이벤트 구독 해제 (하지만 파괴하지 않음)
            if (currentBall != null)
            {
                Debug.Log("기존 공에 연결된 이벤트 리스너 제거");

                // 이벤트 리스너 제거
                currentBall.OnBallThrown -= OnBallThrown;
                currentBall.OnBallLanded -= OnBallLanded;

                // 공 파괴하지 않고 그대로 둠 (야구장에 남겨둠)
                currentBall = null;
                currentBall = null;
            }

            // 새 공 생성 - 전략 선택
            Vector3 spawnPosition = GetBallSpawnPosition();
            Debug.Log($"새 공 생성 위치: {spawnPosition}");

            VRBaseball newBall = null;

            if (originalBall != null && originalBall.GetComponent<VRBaseball>() != null)
            {
                Debug.Log("원본 공을 템플릿으로 사용하여 새 공 생성");
                // 원본 공을 복제
                newBall = Instantiate(originalBall.GetComponent<VRBaseball>(), spawnPosition, Quaternion.identity);
                newBall.name = "VRBaseball_Clone_" + ballsThrown;
            }
            else if (baseballPrefab != null)
            {
                Debug.Log("프리팹에서 새 공 생성");
                // 프리팹에서 공 생성
                newBall = Instantiate(baseballPrefab, spawnPosition, Quaternion.identity);
                newBall.name = "VRBaseball_Prefab_" + ballsThrown;
            }
            else
            {
                Debug.LogError("생성할 공이 없습니다! 원본 공도, 프리팹도 없음.");
                return;
            }

            // 현재 공으로 설정
            currentBall = newBall;

            // 복제된 공의 컴포넌트가 비활성화되지 않도록 강제 활성화
            if (currentBall != null)
            {
                currentBall.enabled = true;
                Debug.Log($"VRBaseball 스크립트 강제 활성화: {currentBall.enabled}");
            }
            else
            {
                Debug.LogError("새 공 생성 실패!");
                return;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SpawnNewBall 오류: {e.Message}\n{e.StackTrace}");
            return;
        }

        // **한 프레임 뒤에 물리 설정 - VRBaseball Start() 후에 실행되도록!**
        StartCoroutine(SetupBallAfterFrame());

        // XR Grab Interactable 강제 활성화 (새 공이 잡힐 수 있도록)
        XRGrabInteractable grabComponent = currentBall.GetComponent<XRGrabInteractable>();
        if (grabComponent != null)
        {
            grabComponent.enabled = true;
            // kinematic 충돌 방지를 위해 throwOnDetach 비활성화
            grabComponent.throwOnDetach = false;

            // Rigidbody 설정도 바로 적용
            Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                // 그랩하기 전까지는 Kinematic=true로 설정 (자리 고정)
                ballRb.isKinematic = true;
                ballRb.useGravity = false;  // 중력도 끄기
                Debug.Log($"Rigidbody 설정: isKinematic={ballRb.isKinematic}, useGravity={ballRb.useGravity}");
            }
        }        // AudioSource가 있는지 확인하고 필요하면 추가
        AudioSource audioSrc = currentBall.GetComponent<AudioSource>();
        if (audioSrc == null)
        {
            audioSrc = currentBall.gameObject.AddComponent<AudioSource>();
            Debug.Log("AudioSource 컴포넌트 추가됨");
        }
        audioSrc.enabled = true;

        // 공 이벤트 등록
        currentBall.OnBallThrown += OnBallThrown;
        currentBall.OnBallLanded += OnBallLanded;

        // UI에 공 등록
        if (pitchSelectionUI != null)
            pitchSelectionUI.RegisterBaseball(currentBall);

        ballsThrown++;

        // 공이 확실히 보이도록 위치 강제 설정
        Vector3 finalPosition = GetBallSpawnPosition();
        currentBall.transform.position = finalPosition;

        Debug.Log($"새 공 생성 완료! 위치: {finalPosition}, 공 번호: {ballsThrown}");
    }

    private System.Collections.IEnumerator SetupBallAfterFrame()
    {
        yield return null; // 한 프레임 대기
        yield return null; // 한 프레임 더 대기 (안정성 추가)

        try
        {
            // 공이 아직 유효한지 확인
            if (currentBall == null)
            {
                Debug.LogWarning("SetupBallAfterFrame: currentBall이 null 상태입니다!");
                yield break;
            }

            // 컴포넌트 얻기
            Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
            XRGrabInteractable grabInteractable = currentBall.GetComponent<XRGrabInteractable>();
            VRBaseball vrBallScript = currentBall.GetComponent<VRBaseball>();

            // VRBaseball 스크립트가 활성화되어 있는지 확인
            if (vrBallScript != null)
            {
                vrBallScript.enabled = true;
                Debug.Log($"VRBaseball 스크립트 상태: {vrBallScript.enabled} (한 프레임 후 확인)");
            }
            else
            {
                Debug.LogError("VRBaseball 스크립트가 없습니다! 공 생성에 문제가 있습니다.");
            }

            // Rigidbody 설정
            if (ballRb != null)
            {
                // 중요: throwOnDetach 비활성화 (isKinematic과의 충돌 방지)
                if (grabInteractable != null)
                {
                    grabInteractable.throwOnDetach = false;
                }

                // 초기 물리 설정
                ballRb.isKinematic = false;  // non-kinematic으로 유지
                ballRb.velocity = Vector3.zero;         // velocity 초기화
                ballRb.angularVelocity = Vector3.zero;  // angular velocity 초기화  
                ballRb.useGravity = false;              // 중력 비활성화 (그랩 전까지는 떨어지지 않게)
                ballRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 충돌 감지 개선

                Debug.Log($"🔧 한 프레임 후 물리 설정 완료! Kinematic: {ballRb.isKinematic}, UseGravity: {ballRb.useGravity}");
            }
            else
            {
                Debug.LogError("Rigidbody 컴포넌트가 없습니다!");
                // 필요하다면 여기서 Rigidbody 추가
                ballRb = currentBall.gameObject.AddComponent<Rigidbody>();
                ballRb.isKinematic = false;
                ballRb.useGravity = false;
            }

            // XRGrabInteractable 설정
            if (grabInteractable != null)
            {
                grabInteractable.throwOnDetach = false;  // 중요: Kinematic과 충돌 방지를 위해 비활성화
                grabInteractable.enabled = true;        // 확실히 활성화

                Debug.Log($"🔧 XRGrabInteractable 설정 완료! throwOnDetach: {grabInteractable.throwOnDetach}, enabled: {grabInteractable.enabled}");
            }
            else
            {
                Debug.LogError("XRGrabInteractable 컴포넌트가 없습니다!");
            }

            // AudioSource 확인
            AudioSource audioSrc = currentBall.GetComponent<AudioSource>();
            if (audioSrc != null)
            {
                audioSrc.enabled = true;
                Debug.Log("AudioSource 활성화됨");
            }

            // 위치 재확인
            Vector3 finalPosition = GetBallSpawnPosition();
            currentBall.transform.position = finalPosition;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SetupBallAfterFrame 오류: {e.Message}\n{e.StackTrace}");
        }
    }

    private void SetupExistingBall()
    {
        if (currentBall == null) return;

        // **물리 설정 - kinematic 상태 확인 후 안전하게 설정!**
        Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            // **이미 kinematic이면 먼저 해제하고 velocity 설정!**
            if (ballRb.isKinematic)
            {
                ballRb.isKinematic = false;  // 먼저 kinematic 해제
            }

            ballRb.velocity = Vector3.zero;         // 이제 안전하게 velocity 설정
            ballRb.angularVelocity = Vector3.zero;  // 이제 안전하게 angular velocity 설정
            ballRb.useGravity = false;              // 중력 끄기
            ballRb.isKinematic = true;              // 다시 kinematic 설정
        }

        // XR Grab Interactable 강제 활성화 및 설정
        XRGrabInteractable grabComponent = currentBall.GetComponent<XRGrabInteractable>();
        if (grabComponent != null)
        {
            grabComponent.enabled = true;
            // 첫 번째 공은 제대로 동작하므로 기본 설정 유지
            // 씬에 있는 초기 공은 throwOnDetach가 올바르게 설정되어 있을 것임
        }

        // 공 이벤트 등록
        currentBall.OnBallThrown += OnBallThrown;
        currentBall.OnBallLanded += OnBallLanded;

        // UI에 공 등록
        if (pitchSelectionUI != null)
            pitchSelectionUI.RegisterBaseball(currentBall);

        Debug.Log($"기존 공 설정 완료! 위치: {currentBall.transform.position}, Kinematic: {ballRb?.isKinematic}");
    }

    private Vector3 GetBallSpawnPosition()
    {
        // **절대 좌표로 고정!** basePosition 문제 해결
        Vector3 fixedSpawnPosition = new Vector3(0f, 0.3f, -5.49f); // 완전 고정 위치

        Debug.Log($"고정 스폰 위치 설정: {fixedSpawnPosition}");

        return fixedSpawnPosition;
    }

    private void OnPitchTypeSelected(PitchType pitchType)
    {
        if (currentBall != null)
            currentBall.SetPitchType(pitchType);
    }

    private void OnBallThrown(VRBaseball ball)
    {
        Debug.Log($"🎾 VRPitchingManager: OnBallThrown 이벤트 수신됨! 딜레이 후 새 공 생성 시작!");

        // 이미 예약된 SpawnNewBall 함수 호출이 있다면 취소
        CancelInvoke(nameof(SpawnNewBall));

        // 중요! 처음 공이면 원본으로 저장 (복제용)
        if (originalBall == null && !ball.name.Contains("Clone"))
        {
            // 중요! 원본 공을 스폰 전에 백업
            Debug.Log("🔄 원본 공을 복제용으로 백업합니다!");
            originalBall = ball.gameObject;
        }

        // 던진 공을 관리 리스트에 추가
        if (ball.gameObject != originalBall && !thrownBalls.Contains(ball.gameObject))
        {
            thrownBalls.Add(ball.gameObject);
            Debug.Log($"🗂️ 던진 공 리스트에 추가: {ball.name}, 총 {thrownBalls.Count}개");
        }

        // **즉시 이전 공들 정리 - 충돌 방지를 위해!**
        CleanupOldBalls();

        // 현재 공 저장 (안전하게)
        VRBaseball throwBall = currentBall;

        // 참조를 끊어 GC 대상이 되지 않게
        currentBall = null;

        // 딜레이 후 새 공 생성
        Invoke(nameof(SpawnNewBall), ballResetDelay);
    }

    private void OnBallLanded(VRBaseball ball, bool isStrike)
    {
        Debug.Log($"=== 공 착지 결과 ===");
        Debug.Log($"위치: {ball.transform.position}");
        Debug.Log($"결과: {(isStrike ? "스트라이크!" : "볼!")}");

        if (isStrike)
        {
            strikes++;
            Debug.Log($"🎯 스트라이크! 현재 스트라이크: {strikes}");

            if (audioSource != null && strikeSound != null)
            {
                audioSource.PlayOneShot(strikeSound);
                Debug.Log("스트라이크 사운드 재생!");
            }
            else
            {
                Debug.LogWarning("스트라이크 사운드 재생 실패 - AudioSource 또는 사운드 클립 없음");
            }
        }
        else
        {
            balls++;
            Debug.Log($"⚾ 볼! 현재 볼: {balls}");

            if (audioSource != null && ballSound != null)
            {
                audioSource.PlayOneShot(ballSound);
                Debug.Log("볼 사운드 재생!");
            }
        }

        OnCountChanged?.Invoke(strikes, balls);
        OnPitchResult?.Invoke(isStrike);

        // 게임 종료 조건 체크
        if (strikes >= 3)
        {
            Debug.Log("🔥 삼진아웃!");
            ResetCount();
        }
        else if (balls >= 4)
        {
            Debug.Log("🚶 포볼!");
            ResetCount();
        }
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
        if (currentBall != null)
            Destroy(currentBall.gameObject);

        // 통계 리셋
        ballsThrown = 0;
        ResetCount();

        // 새 게임 시작
        SpawnNewBall();
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

    // 디버그용 키보드 컨트롤
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.R))
            ResetGame();
        if (Input.GetKeyDown(KeyCode.U))
            ToggleUI();
        if (Input.GetKeyDown(KeyCode.N))
            SpawnNewBall();
#endif
    }

    // 던진 공들 정리 메서드
    private void CleanupOldBalls()
    {
        // **모든 이전 공들을 즉시 제거!** (충돌 방지)
        for (int i = thrownBalls.Count - 1; i >= 0; i--)
        {
            if (thrownBalls[i] != null)
            {
                Debug.Log($"🗑️ 이전 공 제거: {thrownBalls[i].name}");
                Destroy(thrownBalls[i]);
            }
        }
        
        // 리스트 완전히 비우기
        thrownBalls.Clear();
        Debug.Log($"🧹 모든 이전 공 제거 완료! 남은 공: {thrownBalls.Count}개");
    }

    // 공은 제거하지 않습니다 - 모두 보존

    // 게임 정보 반환
    public int GetStrikeCount() => strikes;
    public int GetBallCount() => balls;
    public int GetBallsThrown() => ballsThrown;
    public int GetMaxBalls() => maxBalls;
    public VRBaseball GetCurrentBall() => currentBall;

    void OnDestroy()
    {
        if (pitchSelectionUI != null)
            pitchSelectionUI.OnPitchSelected -= OnPitchTypeSelected;
    }
}