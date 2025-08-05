using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

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

        // 첫 번째 공 생성
        SpawnNewBall();

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
        if (ballsThrown >= maxBalls)
        {
            return;
        }

        // 기존 공 제거
        if (currentBall != null)
        {
            Destroy(currentBall.gameObject);
        }

        // 새 공 생성
        Vector3 spawnPosition = GetBallSpawnPosition();
        currentBall = Instantiate(baseballPrefab, spawnPosition, Quaternion.identity);

        // 물리 설정 - 중력 떨어짐 방지
        Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            ballRb.useGravity = false;  // 생성 시 중력 비활성화
            ballRb.velocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.isKinematic = false;
        }

        // XR Grab Interactable 강제 활성화 (새 공이 잡힐 수 있도록)
        XRGrabInteractable grabComponent = currentBall.GetComponent<XRGrabInteractable>();
        if (grabComponent != null)
        {
            grabComponent.enabled = true;
        }

        // 공 이벤트 등록
        currentBall.OnBallThrown += OnBallThrown;
        currentBall.OnBallLanded += OnBallLanded;

        // UI에 공 등록
        if (pitchSelectionUI != null)
            pitchSelectionUI.RegisterBaseball(currentBall);

        ballsThrown++;

        // 공이 확실히 보이도록 위치 강제 설정
        currentBall.transform.position = spawnPosition;

        Debug.Log($"새 공 생성 완료! 위치: {spawnPosition}, 중력: {ballRb?.useGravity}");
    }

    private Vector3 GetBallSpawnPosition()
    {
        // 투수 마운드 앞쪽 적당한 높이에 공 생성
        Vector3 basePosition = pitcherMound != null ? pitcherMound.position : transform.position;

        // 적당한 높이로 - 너무 높지도 낮지도 않게
        Vector3 properSpawnOffset = new Vector3(0, 1.5f, 0.8f); // Y 1.5f로 적당히
        Vector3 spawnPos = basePosition + properSpawnOffset;

        return spawnPos;
    }

    private void OnPitchTypeSelected(PitchType pitchType)
    {
        if (currentBall != null)
            currentBall.SetPitchType(pitchType);
    }

    private void OnBallThrown(VRBaseball ball)
    {
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