using UnityEngine;

//게임플레이 씬 시작과 동시에 녹화를 시작한다.
//- transform(ball/bat/양손/주자)은 매 FixedUpdate마다 저장(연속 데이터).
//- 상태(StatusSnapshot)는 "변할 때만" 저장(희소). 거의 안 바뀌므로 용량을 크게 아낀다.
public class ReplayRecorder : MonoBehaviour
{
    [Header("Refs (인스펙터에서 연결)")]
    [SerializeField] private GamePlayManager manager;
    [SerializeField] private Baseball ball;
    [SerializeField] private Bat bat;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Recording")]
    [SerializeField] private bool recordOnEnable = true; //씬 시작하자마자 녹화

    public ReplayData Data { get; private set; }
    public bool IsRecording { get; private set; }

    private bool _hasLastStatus;
    private GamePlayManager.StatusSnapshot _lastStatus;
    private float _startTime;

    void OnEnable()
    {
        if (recordOnEnable)
            StartRecording();
    }

    public void StartRecording()
    {
        Data = new ReplayData();
        _hasLastStatus = false;
        _startTime = Time.time;
        IsRecording = true;
    }

    public void StopRecording()
    {
        IsRecording = false;
    }

    void FixedUpdate()
    {
        if (!IsRecording || manager == null)
            return;

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

        Data.frames.Add(f);

        // 2) status — 변할 때만 (희소)
        GamePlayManager.StatusSnapshot s = manager.CaptureStatus();
        if (!_hasLastStatus || StatusChanged(_lastStatus, s))
        {
            Data.statusTrack.Add(new StatusKeyframe { time = t, status = s });
            _lastStatus = s;
            _hasLastStatus = true;
        }
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
