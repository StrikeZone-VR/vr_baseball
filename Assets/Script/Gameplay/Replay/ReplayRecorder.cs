using UnityEngine;

//게임플레이 씬 시작과 동시에 녹화를 시작한다.
//- transform(ball/bat/양손/주자)은 매 FixedUpdate마다 저장(연속 데이터).
//- 상태(StatusSnapshot)는 "변할 때만" 저장(희소). 거의 안 바뀌므로 용량을 크게 아낀다.
public class ReplayRecorder : MonoBehaviour
{
    [Header("Refs (비워두면 런타임에 자동 연결)")]
    [SerializeField] private GamePlayManager manager;
    [SerializeField] private Baseball ball;
    [SerializeField] private Bat bat;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Recording")]
    [Tooltip("녹화를 담을 SO. 인스펙터에 .asset을 넣으면 거기에 기록(에디터에선 종료 후에도 유지). 비우면 런타임 임시 인스턴스.")]
    [SerializeField] private ReplayData data;
    [SerializeField] private bool recordOnEnable; //씬 시작하자마자 녹화
    [SerializeField] private bool debugLog = true;       //1초마다 녹화 상태를 콘솔에 출력(디버깅용)

    public ReplayData Data => data;
    public bool IsRecording { get; private set; }
    public int FrameCount => data != null ? data.frames.Count : 0;

    private float _logTimer;

    private MyXROriginManager _xrOrigin;
    private bool _hasLastStatus;
    private GamePlayManager.StatusSnapshot _lastStatus;
    private float _startTime;

    //인스펙터에 비어 있는 참조를 런타임에 찾아 채운다.
    //- manager/ball/bat : GamePlay 씬 (manager가 ball/bat을 들고 있음)
    //- 양손/머리 : PersistentManager 씬의 MyXROriginManager (다른 씬이라 드래그 연결 불가)
    //매니저를 못 찾으면 false(아직 게임플레이 씬 준비 전).
    public bool EnsureResolved()
    {
        if (manager == null) manager = FindAnyObjectByType<GamePlayManager>();
        if (manager == null) return false;

        if (ball == null) ball = manager.Ball;
        if (bat == null)  bat  = manager.Bat;

        if (_xrOrigin == null) _xrOrigin = FindAnyObjectByType<MyXROriginManager>();
        if (_xrOrigin != null)
        {
            if (rightHand == null) rightHand = _xrOrigin.RightHand;
            if (leftHand == null)  leftHand  = _xrOrigin.LeftHand;
        }
        return true;
    }

    void OnEnable()
    {
        if (recordOnEnable)
            StartRecording();
    }

    public void StartRecording()
    {
        if (data == null)
        {
            Debug.LogError("[ReplayRecorder] Data(ReplayData SO) 미할당. 인스펙터에 .asset을 넣어줘.");
            return;
        }
        data.frames.Clear();
        data.statusTrack.Clear();
        data.events.Clear();
        _hasLastStatus = false;
        _startTime = Time.time;
        IsRecording = true;
    }

    public void StopRecording()
    {
        IsRecording = false;
        SaveToDisk();
    }

    //런타임에 SO에 쓴 값은 디스크에 자동 저장되지 않는다. 에디터에서 dirty 표시 후 강제 저장해
    //플레이 종료/씬 전환 후에도 .asset에 남게 한다. (빌드에선 no-op)
    private void SaveToDisk()
    {
#if UNITY_EDITOR
        if (data == null) return;
        UnityEditor.EditorUtility.SetDirty(data);
        UnityEditor.AssetDatabase.SaveAssets();
        if (debugLog)
            Debug.Log($"[ReplayRecorder] 저장됨 → frames={data.frames.Count} status={data.statusTrack.Count}");
#endif
    }

    //플레이 종료/씬 언로드 시에도 확실히 디스크에 박는다.
    void OnDisable()
    {
        if (IsRecording) IsRecording = false;
        SaveToDisk();
    }

    void FixedUpdate()
    {
        if (!IsRecording)
            return;
        if (!EnsureResolved()) //매니저가 아직 없으면(씬 준비 전) 이번 프레임은 건너뜀
        {
            LogThrottled("[ReplayRecorder] 대기중 — GamePlayManager 못 찾음(아직 게임플레이 씬 아님?)");
            return;
        }

        float t = Time.time - _startTime;

        // 1) transform — 매 프레임 (연속)
        TransformFrame f = new TransformFrame();
        f.time = t;
        if (ball != null)      f.ball      = new PoseKey(ball.transform);
        if (bat != null)       f.bat       = new PoseKey(bat.transform);
        if (leftHand != null)  f.leftHand  = new PoseKey(leftHand);
        if (rightHand != null) f.rightHand = new PoseKey(rightHand);

        Transform[] runnerTs = manager.GetRunnerTransforms();
        f.runners = new PoseKey[runnerTs.Length];
        for (int i = 0; i < runnerTs.Length; i++)
            f.runners[i] = new PoseKey(runnerTs[i]);

        Transform[] defenderTs = manager.GetDefenderTransforms(); //수비수(투수=0 포함)
        f.defenders = new PoseKey[defenderTs.Length];
        for (int i = 0; i < defenderTs.Length; i++)
            if (defenderTs[i] != null) f.defenders[i] = new PoseKey(defenderTs[i]);

        Data.frames.Add(f);

        // 2) status — 변할 때만 (희소)
        GamePlayManager.StatusSnapshot s = manager.CaptureStatus();
        if (!_hasLastStatus || StatusChanged(_lastStatus, s))
        {
            Data.statusTrack.Add(new StatusKeyframe { time = t, status = s });
            _lastStatus = s;
            _hasLastStatus = true;
        }

        LogThrottled($"[ReplayRecorder] 녹화중 frames={Data.frames.Count} status={Data.statusTrack.Count} " +
                     $"| ball={(ball != null)} bat={(bat != null)} R손={(rightHand != null)} L손={(leftHand != null)} 주자={runnerTs.Length} 수비={defenderTs.Length}");
    }

    //debugLog가 켜져 있으면 1초에 한 번만 콘솔에 출력(매 프레임 스팸 방지).
    private void LogThrottled(string msg)
    {
        if (!debugLog) return;
        _logTimer += Time.fixedDeltaTime;
        if (_logTimer < 1f) return;
        _logTimer = 0f;
        Debug.Log(msg);
    }

    //이벤트 채널(strike/foul/homerun 등)에서 호출해 순간 마커를 남긴다.
    public void AddEvent(string type)
    {
        if (!IsRecording)
            return;
        Data.events.Add(new EventMarker { time = Time.time - _startTime, type = type });
    }

    //StatusSnapshot은 RunnerSnapshot[]을 품어서 기본 Equals(참조 비교)가 안 먹는다.
    //그래서 변경 여부를 필드 직접 비교로 판단한다.
    //주의: 연속적으로 매 프레임 바뀌는 값(ballPos/ballVel/defenderDis)은 일부러 비교에서 제외한다.
    //      포함하면 매 프레임 키프레임이 찍혀 희소 압축이 무의미해진다.
    //      이 연속값들은 재생 때 transform 트랙에서 채워 넣는다.
    static bool StatusChanged(GamePlayManager.StatusSnapshot a, GamePlayManager.StatusSnapshot b)
    {
        if (a.hasBall != b.hasBall) return true;
        if (a.ballState != b.ballState) return true;
        if (a.isInGamePlay != b.isInGamePlay || a.isGroundBall != b.isGroundBall ||
            a.isZone != b.isZone || a.isStrike != b.isStrike || a.useGravity != b.useGravity) return true;
        if (a.defenderName != b.defenderName) return true;
        if (a.playerIsHome != b.playerIsHome) return true;
        if (a.beforeScore != b.beforeScore) return true;
        if (a.isFlyingOut != b.isFlyingOut) return true;
        if (a.throwBallStop != b.throwBallStop) return true;
        if (a.runningIndex != b.runningIndex) return true;
        if (a.batterName != b.batterName) return true;

        int ac = a.runners != null ? a.runners.Length : 0;
        int bc = b.runners != null ? b.runners.Length : 0;
        if (ac != bc) return true;
        for (int i = 0; i < ac; i++)
        {
            if (a.runners[i].baseIndex != b.runners[i].baseIndex) return true;
            if (a.runners[i].isMove != b.runners[i].isMove) return true;
            if (a.runners[i].name != b.runners[i].name) return true;
        }
        return false;
    }
}
