using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

//녹화된 ReplayData를 제3자 시점으로 재생한다.
//- 격리: 재생 중 Time.timeScale=0으로 라이브 시뮬(물리/AI)을 멈추고, 재생은 unscaledDeltaTime으로 돌린다.
//- 대상: 라이브 오브젝트는 XR이 계속 건드리므로 별도 "고스트" Transform에 그린다(주자는 프리팹 풀링).
//- 상태: status 트랙에서 현재 시간 이하 마지막 키프레임을 읽어 FormatStatus로 패널에 띄운다.
//        연속값(ballPos/ballVel)은 희소 status엔 stale이라 transform 트랙에서 패치한다.
public class ReplayPlayer : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private ReplayRecorder recorder; //PlayLatest() 시 여기서 Data를 가져온다

    [Header("Ghosts (재생 대상, 비우면 해당 항목 스킵)")]
    [SerializeField] private Transform ballGhost;
    [SerializeField] private Transform batGhost;
    [SerializeField] private Transform leftHandGhost;
    [SerializeField] private Transform rightHandGhost;
    [SerializeField] private GameObject runnerGhostPrefab; //주자 수만큼 풀링

    [Header("Status UI (선택)")]
    [SerializeField] private TMP_Text statusText;

    [Header("Playback")]
    [SerializeField] private float playbackSpeed = 1f;
    [SerializeField] private bool freezeLiveGameWhileReplaying = true; //Time.timeScale=0
    [SerializeField] private bool loop = false;

    public bool IsPlaying { get; private set; }
    public bool IsPaused { get; private set; }
    public float Duration => _duration;
    public float PlayTime => _playTime;
    public float Time01 => _duration > 0f ? Mathf.Clamp01(_playTime / _duration) : 0f;

    private ReplayData _data;
    private float _playTime;
    private float _duration;
    private float _savedTimeScale = 1f;
    private readonly StringBuilder _sb = new StringBuilder();
    private readonly List<Transform> _runnerPool = new List<Transform>();

    //레코더가 들고 있는 마지막 녹화를 재생한다.
    public void PlayLatest()
    {
        if (recorder == null)
        {
            Debug.LogWarning("[ReplayPlayer] recorder 미연결");
            return;
        }
        recorder.StopRecording();
        Play(recorder.Data);
    }

    public void Play(ReplayData data)
    {
        if (data == null || data.frames.Count == 0)
        {
            Debug.LogWarning("[ReplayPlayer] 재생할 데이터가 없음");
            return;
        }
        _data = data;
        _duration = data.frames[data.frames.Count - 1].time;
        _playTime = 0f;
        IsPlaying = true;
        IsPaused = false;

        if (freezeLiveGameWhileReplaying)
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f; //라이브 시뮬 정지. 재생은 unscaledDeltaTime으로 진행
        }

        Apply(_playTime);
    }

    public void Stop()
    {
        IsPlaying = false;
        IsPaused = false;
        if (freezeLiveGameWhileReplaying)
            Time.timeScale = _savedTimeScale;

        //주자 고스트 숨기기
        for (int i = 0; i < _runnerPool.Count; i++)
            if (_runnerPool[i] != null) _runnerPool[i].gameObject.SetActive(false);
    }

    public void TogglePause()
    {
        if (IsPlaying) IsPaused = !IsPaused;
    }

    //0~1 정규화 위치로 점프(스크럽).
    public void Seek(float time01)
    {
        if (_data == null) return;
        _playTime = Mathf.Clamp01(time01) * _duration;
        Apply(_playTime);
    }

    private void Update()
    {
#if UNITY_EDITOR
        //에디터 테스트용 단축키 (패널 F1 / 레코더 키들과 안 겹치게 F2/F3)
        if (Input.GetKeyDown(KeyCode.F2)) PlayLatest();
        if (Input.GetKeyDown(KeyCode.F3)) Stop();
        if (Input.GetKeyDown(KeyCode.F4)) TogglePause();
#endif

        if (!IsPlaying || _data == null) return;

        if (!IsPaused)
        {
            _playTime += Time.unscaledDeltaTime * playbackSpeed;
            if (_playTime >= _duration)
            {
                if (loop)
                {
                    _playTime = 0f;
                }
                else
                {
                    _playTime = _duration;
                    Apply(_playTime);
                    Stop();
                    return;
                }
            }
        }
        Apply(_playTime);
    }

    //주어진 시간의 transform/status를 고스트와 패널에 반영
    private void Apply(float t)
    {
        ApplyTransforms(t);
        ApplyStatus(t);
    }

    private void ApplyTransforms(float t)
    {
        List<TransformFrame> frames = _data.frames;
        int i = FindFrameIndex(t);
        int j = Mathf.Min(i + 1, frames.Count - 1);
        float span = frames[j].time - frames[i].time;
        float a = span > 0f ? Mathf.Clamp01((t - frames[i].time) / span) : 0f;

        SetPose(ballGhost, frames[i].ball, frames[j].ball, a);
        SetPose(batGhost, frames[i].bat, frames[j].bat, a);
        SetPose(leftHandGhost, frames[i].leftHand, frames[j].leftHand, a);
        SetPose(rightHandGhost, frames[i].rightHand, frames[j].rightHand, a);

        ApplyRunners(frames[i], frames[j], a);
    }

    private void ApplyRunners(TransformFrame from, TransformFrame to, float a)
    {
        int count = from.runners != null ? from.runners.Length : 0;
        EnsureRunnerPool(count);
        for (int k = 0; k < _runnerPool.Count; k++)
        {
            bool active = k < count;
            if (_runnerPool[k] != null) _runnerPool[k].gameObject.SetActive(active);
            if (!active) continue;

            PoseKey f = from.runners[k];
            //다음 프레임에 같은 인덱스 주자가 있으면 보간, 없으면 스냅
            if (to.runners != null && k < to.runners.Length)
            {
                SetPose(_runnerPool[k], f, to.runners[k], a);
            }
            else
            {
                _runnerPool[k].position = f.pos;
                _runnerPool[k].rotation = f.rot;
            }
        }
    }

    private void EnsureRunnerPool(int count)
    {
        if (runnerGhostPrefab == null) return;
        while (_runnerPool.Count < count)
        {
            GameObject go = Instantiate(runnerGhostPrefab, transform);
            _runnerPool.Add(go.transform);
        }
    }

    private void ApplyStatus(float t)
    {
        if (statusText == null) return;

        int k = FindStatusIndex(t);
        if (k < 0)
        {
            statusText.text = "";
            return;
        }

        GamePlayManager.StatusSnapshot s = _data.statusTrack[k].status; //struct 복사 → 원본 불변

        //희소 status엔 연속값이 stale하므로 transform 트랙에서 채워 넣는다
        if (ballGhost != null) s.ballPos = ballGhost.position;
        s.ballVel = SampleBallVelocity(t);

        _sb.Clear();
        GamePlayManager.FormatStatus(_sb, s);
        statusText.text = _sb.ToString();
    }

    private Vector3 SampleBallVelocity(float t)
    {
        List<TransformFrame> frames = _data.frames;
        int i = FindFrameIndex(t);
        int j = Mathf.Min(i + 1, frames.Count - 1);
        float dt = frames[j].time - frames[i].time;
        if (dt <= 0f) return Vector3.zero;
        return (frames[j].ball.pos - frames[i].ball.pos) / dt;
    }

    private static void SetPose(Transform target, PoseKey from, PoseKey to, float a)
    {
        if (target == null) return;
        target.position = Vector3.Lerp(from.pos, to.pos, a);
        target.rotation = Quaternion.Slerp(from.rot, to.rot, a);
    }

    //frames[i].time <= t 를 만족하는 가장 큰 i (이진 탐색)
    private int FindFrameIndex(float t)
    {
        List<TransformFrame> frames = _data.frames;
        int lo = 0, hi = frames.Count - 1, res = 0;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (frames[mid].time <= t) { res = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return res;
    }

    //statusTrack[k].time <= t 를 만족하는 가장 큰 k (없으면 -1)
    private int FindStatusIndex(float t)
    {
        List<StatusKeyframe> track = _data.statusTrack;
        if (track.Count == 0) return -1;
        int lo = 0, hi = track.Count - 1, res = 0;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (track[mid].time <= t) { res = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return res;
    }
}
