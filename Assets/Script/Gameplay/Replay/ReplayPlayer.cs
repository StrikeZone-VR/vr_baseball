using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//녹화된 ReplayData를 단독 ReplayScene에서 제3자 시점으로 재생한다.
//- 진행: 재생은 unscaledDeltaTime으로 돌린다(라이브 게임이 없는 단독 씬이라 timeScale에 의존하지 않음).
//- 대상: 씬에 직접 배치한 "고스트" Transform에 그린다(주자는 프리팹 풀링).
//- 상태: status 트랙에서 현재 시간 이하 마지막 키프레임을 읽어 FormatStatus로 패널에 띄운다.
//        연속값(ballPos/ballVel)은 희소 status엔 stale이라 transform 트랙에서 패치한다.
public class ReplayPlayer : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("재생할 .asset. ReplayScene에서 ReplaySO를 직접 물린다.")]
    [SerializeField] private ReplayData sourceData;
    [SerializeField] private bool playOnStart = false; //ReplayScene 시작과 동시에 자동 재생

    [Header("Ghosts (씬에 직접 배치한 비주얼을 연결)")]
    [SerializeField] private Transform ballGhost;
    [SerializeField] private Transform batGhost;
    [SerializeField] private Transform leftHandGhost;
    [SerializeField] private Transform rightHandGhost;
    [SerializeField] private GameObject runnerGhostPrefab; //주자 수만큼 풀링. 비우면 캡슐
    [SerializeField] private GameObject defenderGhostPrefab; //수비수(투수 포함) 수만큼 풀링. 비우면 캡슐

    [Header("Status UI (비우면 자동 생성)")]
    [SerializeField] private TMP_Text statusText;

    [Header("Playback")]
    [SerializeField] private float playbackSpeed = 1f;
    [SerializeField] private bool loop = false;

    [Header("Auto build (씬 수작업 최소화)")]
    [SerializeField] private bool autoBuildStatusPanel = true;  //statusText 없으면 머리 고정 패널 생성

    public bool IsPlaying { get; private set; }
    public bool IsPaused { get; private set; }
    public float Duration => _duration;
    public float PlayTime => _playTime;
    public float Time01 => _duration > 0f ? Mathf.Clamp01(_playTime / _duration) : 0f;

    private ReplayData _data;
    private float _playTime;
    private float _duration;
    private readonly StringBuilder _sb = new StringBuilder();
    private readonly List<Transform> _runnerPool = new List<Transform>();
    private readonly List<Transform> _defenderPool = new List<Transform>();

    private bool _autoBuilt;                                    //패널 1회만 생성
    private GameObject _statusCanvasGo;                         //자동 생성한 상태 패널 캔버스
    private readonly List<GameObject> _builtObjects = new List<GameObject>(); //정리용(씬 언로드 시 파괴)

    private void Start()
    {
        if (playOnStart) Play(sourceData);
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

        AutoBuild();          //상태 패널 1회 생성
        SetGhostsActive(true);
        if (_statusCanvasGo != null) _statusCanvasGo.SetActive(true);

        Apply(_playTime);
    }

    public void Stop()
    {
        IsPlaying = false;
        IsPaused = false;

        SetGhostsActive(false);
        if (_statusCanvasGo != null) _statusCanvasGo.SetActive(false);

        //주자/수비수 고스트 숨기기
        HidePool(_runnerPool);
        HidePool(_defenderPool);
    }

    private static void HidePool(List<Transform> pool)
    {
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null) pool[i].gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        //씬 언로드 시 자동 생성물 정리. 패널은 영구 카메라 자식이라 명시적 파괴 필요.
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

    //녹화 프레임 단위로 한 칸씩 이동(dir = +1 앞, -1 뒤). 스텝은 정지 상태 기준이라 자동 일시정지한다.
    public void StepFrame(int dir)
    {
        if (!IsPlaying || _data == null || _data.frames.Count == 0) return;
        IsPaused = true; //스텝하는 순간 멈춘 상태로 둔다(Update가 시간 안 흘리도록)

        //현재 시간 이하 마지막 프레임에서 dir칸 이동. 보간 중(프레임 사이)이면 -1은 현재 프레임으로 스냅된다.
        int i = FindFrameIndex(_playTime);
        int target = Mathf.Clamp(i + dir, 0, _data.frames.Count - 1);
        _playTime = _data.frames[target].time;
        Apply(_playTime);
    }

    private void Update()
    {
#if UNITY_EDITOR
        //에디터 테스트용 단축키 (패널 F1 / 레코더 키들과 안 겹치게 F2/F3)
        //if (Input.GetKeyDown(KeyCode.F2)) Play(sourceData);
        //if (Input.GetKeyDown(KeyCode.F3)) Stop();
        if (Input.GetKeyDown(KeyCode.F2)) TogglePause();
        if (Input.GetKey(KeyCode.RightArrow)) StepFrame(1);  //→ 한 프레임 앞으로
        if (Input.GetKey(KeyCode.LeftArrow))  StepFrame(-1); //← 한 프레임 뒤로
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
        ApplyDefenders(frames[i], frames[j], a);
    }

    private void ApplyRunners(TransformFrame from, TransformFrame to, float a)
        => ApplyGhostPool(from.runners, to.runners, a, _runnerPool, runnerGhostPrefab, "Runner");

    private void ApplyDefenders(TransformFrame from, TransformFrame to, float a)
        => ApplyGhostPool(from.defenders, to.defenders, a, _defenderPool, defenderGhostPrefab, "Defender");

    //주자·수비수 공통: 풀을 인원수만큼 확보하고, 인덱스별로 from→to 보간을 적용한다.
    private void ApplyGhostPool(PoseKey[] from, PoseKey[] to, float a, List<Transform> pool, GameObject prefab, string label)
    {
        int count = from != null ? from.Length : 0;
        EnsureGhostPool(pool, prefab, label, count);
        for (int k = 0; k < pool.Count; k++)
        {
            bool active = k < count;
            if (pool[k] != null) pool[k].gameObject.SetActive(active);
            if (!active) continue;

            PoseKey f = from[k];
            //다음 프레임에 같은 인덱스가 있으면 보간, 없으면 스냅
            if (to != null && k < to.Length)
            {
                SetPose(pool[k], f, to[k], a);
            }
            else
            {
                pool[k].position = f.pos;
                pool[k].rotation = f.rot;
            }
        }
    }

    private void EnsureGhostPool(List<Transform> pool, GameObject prefab, string label, int count)
    {
        while (pool.Count < count)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab);
                StripForGhost(go);
            }
            else //최후의 수단: 캡슐 도형
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(go.GetComponent<Collider>());
            }
            go.name = $"[ReplayGhost] {label}";
            _builtObjects.Add(go);
            pool.Add(go.transform);
        }
    }

    private void ApplyStatus(float t)
    {
        if (statusText == null) return;

        _sb.Clear();

        //현재 재생 위치(프레임/시간) 헤더 — status 키프레임 유무와 무관하게 항상 맨 위에 표시
        int frame = FindFrameIndex(t);
        _sb.AppendLine($"<b>[ REPLAY ]</b> frame {frame + 1}/{_data.frames.Count} " +
                       $"({t:F2}/{_duration:F2}s){(IsPaused ? "  [일시정지]" : "")}");
        _sb.AppendLine();

        int k = FindStatusIndex(t);
        if (k >= 0)
        {
            GamePlayManager.StatusSnapshot s = _data.statusTrack[k].status; //struct 복사 → 원본 불변

            //희소 status엔 연속값이 stale하므로 transform 트랙에서 채워 넣는다
            if (ballGhost != null) s.ballPos = ballGhost.position;
            s.ballVel = SampleBallVelocity(t);

            GamePlayManager.FormatStatus(_sb, s);
        }

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

    //비어있는 상태 패널을 1회 생성한다. 고스트는 씬에 직접 배치해 인스펙터로 연결한다.
    private void AutoBuild()
    {
        if (_autoBuilt) return;
        _autoBuilt = true;

        if (autoBuildStatusPanel && statusText == null)
            BuildStatusCanvas();

        Debug.Log($"[ReplayPlayer] AutoBuild 완료 — ballGhost={ballGhost != null} batGhost={batGhost != null} " +
                  $"R손Ghost={rightHandGhost != null} L손Ghost={leftHandGhost != null} 패널={statusText != null}");
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
}
