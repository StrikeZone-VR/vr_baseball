using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//녹화된 ReplayData를 제3자 시점으로 재생한다.
//- 격리: 재생 중 Time.timeScale=0으로 라이브 시뮬(물리/AI)을 멈추고, 재생은 unscaledDeltaTime으로 돌린다.
//- 대상: 라이브 오브젝트는 XR이 계속 건드리므로 별도 "고스트" Transform에 그린다(주자는 프리팹 풀링).
//- 상태: status 트랙에서 현재 시간 이하 마지막 키프레임을 읽어 FormatStatus로 패널에 띄운다.
//        연속값(ballPos/ballVel)은 희소 status엔 stale이라 transform 트랙에서 패치한다.
public class ReplayPlayer : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private ReplayRecorder recorder; //PlayLatest() 시 여기서 Data를 가져온다

    [Header("Ghosts (비우면 재생 시 라이브 오브젝트를 복제해 자동 생성)")]
    [SerializeField] private Transform ballGhost;
    [SerializeField] private Transform batGhost;
    [SerializeField] private Transform leftHandGhost;
    [SerializeField] private Transform rightHandGhost;
    [SerializeField] private GameObject runnerGhostPrefab; //주자 수만큼 풀링. 비우면 살아있는 주자 복제→없으면 캡슐

    [Header("Status UI (비우면 자동 생성)")]
    [SerializeField] private TMP_Text statusText;

    [Header("Playback")]
    [SerializeField] private float playbackSpeed = 1f;
    [SerializeField] private bool freezeLiveGameWhileReplaying = true; //Time.timeScale=0
    [SerializeField] private bool loop = false;

    [Header("Auto build (씬 수작업 최소화)")]
    [SerializeField] private bool autoBuildGhosts = true;       //비어있는 고스트를 라이브 복제로 생성
    [SerializeField] private bool autoBuildStatusPanel = true;  //statusText 없으면 머리 고정 패널 생성
    [Tooltip("재생 중 라이브 액터(공/배트/손/주자)의 렌더러를 꺼서 고스트와 겹쳐 보이지 않게 한다.")]
    [SerializeField] private bool hideLiveActorsWhileReplaying = true;

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

    private bool _autoBuilt;                                    //고스트/패널 1회만 생성
    private GameObject _statusCanvasGo;                         //자동 생성한 상태 패널 캔버스
    private Transform _runnerTemplate;                          //주자 고스트 복제 원본
    private readonly List<GameObject> _builtObjects = new List<GameObject>(); //정리용(씬 언로드 시 파괴)
    private readonly List<Transform> _hiddenLive = new List<Transform>();     //재생 중 숨긴 라이브 액터(복원용)

    //레코더가 들고 있는 지금까지의 녹화를 즉석 재생한다.
    //녹화는 멈추지 않는다 — timeScale=0이면 레코더의 FixedUpdate도 멈추므로 재생 중 데이터가 변하지 않고,
    //Stop()으로 timeScale이 복구되면 이어서 녹화가 계속된다(보다가 라이브로 복귀).
    public void PlayLatest()
    {
        if (recorder == null) recorder = FindAnyObjectByType<ReplayRecorder>();
        if (recorder == null)
        {
            Debug.LogWarning("[ReplayPlayer] recorder 미연결 (씬에 ReplayRecorder 없음)");
            return;
        }
        recorder.EnsureResolved(); //고스트 생성에 쓸 라이브 소스(공/배트/손) 확보
        Debug.Log($"[ReplayPlayer] PlayLatest — 녹화 프레임 수={recorder.FrameCount} " +
                  $"(ball={recorder.BallSource != null} bat={recorder.BatSource != null} " +
                  $"R손={recorder.RightHandSource != null} L손={recorder.LeftHandSource != null})");
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

        AutoBuild();          //고스트/상태 패널 1회 생성
        SetGhostsActive(true);
        if (_statusCanvasGo != null) _statusCanvasGo.SetActive(true);
        if (hideLiveActorsWhileReplaying) HideLiveActors();

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

        SetGhostsActive(false);
        if (_statusCanvasGo != null) _statusCanvasGo.SetActive(false);

        //주자 고스트 숨기기
        for (int i = 0; i < _runnerPool.Count; i++)
            if (_runnerPool[i] != null) _runnerPool[i].gameObject.SetActive(false);

        RestoreLiveActors(); //숨겼던 라이브 액터 렌더러 복원
    }

    private void OnDestroy()
    {
        //씬 언로드 시 자동 생성물 정리. 고스트는 씬 루트, 패널은 영구 카메라 자식이라 명시적 파괴 필요.
        RestoreLiveActors();
        for (int i = 0; i < _builtObjects.Count; i++)
            if (_builtObjects[i] != null) Destroy(_builtObjects[i]);
        _builtObjects.Clear();
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
        while (_runnerPool.Count < count)
        {
            GameObject go;
            if (runnerGhostPrefab != null)
            {
                go = Instantiate(runnerGhostPrefab);
                StripForGhost(go);
            }
            else if (_runnerTemplate != null) //살아있는 주자 복제(가장 그럴듯함)
            {
                go = Instantiate(_runnerTemplate.gameObject);
                StripForGhost(go);
                go.transform.localScale = _runnerTemplate.lossyScale; //루트로 빠지므로 월드 스케일로 보정
            }
            else //최후의 수단: 캡슐 도형
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(go.GetComponent<Collider>());
            }
            go.name = "[ReplayGhost] Runner";
            _builtObjects.Add(go);
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

    // ===== 자동 생성 (씬 수작업 최소화) ==========================================

    //비어있는 고스트/상태 패널을 1회 생성한다. 라이브 오브젝트를 복제해 고스트로 쓴다.
    private void AutoBuild()
    {
        if (_autoBuilt) return;
        _autoBuilt = true;

        if (recorder == null) recorder = FindAnyObjectByType<ReplayRecorder>();

        if (autoBuildGhosts && recorder != null)
        {
            if (ballGhost == null      && recorder.BallSource != null)      ballGhost      = Ghostify(recorder.BallSource, "Ball");
            if (batGhost == null       && recorder.BatSource != null)       batGhost       = Ghostify(recorder.BatSource, "Bat");
            if (rightHandGhost == null && recorder.RightHandSource != null) rightHandGhost = Ghostify(recorder.RightHandSource, "RightHand");
            if (leftHandGhost == null  && recorder.LeftHandSource != null)  leftHandGhost  = Ghostify(recorder.LeftHandSource, "LeftHand");
            _runnerTemplate = recorder.GetRunnerTemplate();
        }

        if (autoBuildStatusPanel && statusText == null)
            BuildStatusCanvas();

        Debug.Log($"[ReplayPlayer] AutoBuild 완료 — ballGhost={ballGhost != null} batGhost={batGhost != null} " +
                  $"R손Ghost={rightHandGhost != null} L손Ghost={leftHandGhost != null} 패널={statusText != null}");
    }

    //라이브 오브젝트를 복제해 "순수 시각용" 고스트로 만든다(물리/AI/XR 스크립트 비활성).
    private Transform Ghostify(Transform source, string label)
    {
        GameObject g = Instantiate(source.gameObject);
        g.name = $"[ReplayGhost] {label}";
        StripForGhost(g);
        //부모 없이 루트에 두므로 local = world. 원본의 현재 월드 포즈/스케일로 맞춘다.
        g.transform.SetPositionAndRotation(source.position, source.rotation);
        g.transform.localScale = source.lossyScale;
        _builtObjects.Add(g);
        return g.transform;
    }

    //고스트가 스스로 움직이거나 물리에 간섭하지 않도록 시뮬 관련 컴포넌트를 끈다.
    //Animator는 남겨 포즈를 유지(렌더러도 유지). transform은 ReplayPlayer가 직접 구동한다.
    private static void StripForGhost(GameObject g)
    {
        foreach (var rb in g.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
        foreach (var col in g.GetComponentsInChildren<Collider>(true)) col.enabled = false;
        foreach (var na in g.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true)) na.enabled = false;
        //우리 스크립트/XR 드라이버(TrackedPoseDriver, XRBaseController 등)는 MonoBehaviour라 일괄 비활성
        foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
    }

    //DebugStatusPanel과 같은 방식의 머리 고정 월드 캔버스를 만들어 statusText로 쓴다(왼쪽에 배치).
    private void BuildStatusCanvas()
    {
        Camera cam = ResolveHeadCamera();
        if (cam == null) { _autoBuilt = false; return; } //카메라 아직 없으면 다음 재생 때 재시도

        _statusCanvasGo = new GameObject("[Replay] StatusCanvas");
        _statusCanvasGo.transform.SetParent(cam.transform, false);
        _statusCanvasGo.transform.localPosition = new Vector3(-0.52f, 0f, 0.8f); //왼쪽 + 앞 0.8m
        _statusCanvasGo.transform.localRotation = Quaternion.identity;

        var canvas = _statusCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(420f, 620f);
        rt.localScale = Vector3.one * 0.0008f;

        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(_statusCanvasGo.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(_statusCanvasGo.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 22f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.richText = true;
        var trt = tmp.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(14f, 14f); trt.offsetMax = new Vector2(-14f, -14f);

        statusText = tmp;
        _builtObjects.Add(_statusCanvasGo);
    }

    private Camera ResolveHeadCamera()
    {
        var xr = FindAnyObjectByType<MyXROriginManager>();
        if (xr != null && xr.HeadCamera != null) return xr.HeadCamera;
        return Camera.main;
    }

    private void SetGhostsActive(bool on)
    {
        if (ballGhost != null)      ballGhost.gameObject.SetActive(on);
        if (batGhost != null)       batGhost.gameObject.SetActive(on);
        if (leftHandGhost != null)  leftHandGhost.gameObject.SetActive(on);
        if (rightHandGhost != null) rightHandGhost.gameObject.SetActive(on);
        //주자 고스트는 ApplyRunners가 매 프레임 활성/비활성을 관리한다.
    }

    // ===== 재생 중 라이브 액터 숨김 =============================================

    //재생 중에는 라이브 공/배트/손/주자가 고스트와 겹쳐 보이므로 렌더러를 끈다. Stop에서 복원.
    private void HideLiveActors()
    {
        if (recorder == null) return;
        _hiddenLive.Clear();
        HideRenderers(recorder.BallSource);
        HideRenderers(recorder.BatSource);
        HideRenderers(recorder.LeftHandSource);
        HideRenderers(recorder.RightHandSource);
        if (recorder.Manager != null)
        {
            Transform[] rs = recorder.Manager.GetRunnerTransforms();
            for (int i = 0; i < rs.Length; i++) HideRenderers(rs[i]);
        }
    }

    private void HideRenderers(Transform t)
    {
        if (t == null) return;
        foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        _hiddenLive.Add(t);
    }

    private void RestoreLiveActors()
    {
        for (int i = 0; i < _hiddenLive.Count; i++)
        {
            Transform t = _hiddenLive[i];
            if (t == null) continue;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        }
        _hiddenLive.Clear();
    }
}
