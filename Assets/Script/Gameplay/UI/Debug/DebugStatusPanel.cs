#if UNITY_EDITOR
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//VR 헤드셋 안에서 시야 오른쪽에 게임/볼/주자 상태를 실시간으로 띄우는 디버그 패널.
//World-Space Canvas + TextMeshPro를 런타임에 코드로 생성해 카메라(HMD)에 붙인다(head-locked).
//pull(폴링) 방식: 매 프레임 GamePlayManager.BuildDebugStatus()를 읽어와 텍스트만 갱신.
//PersistentManager 씬의 빈 오브젝트에 이 컴포넌트만 올리면 됨(인스펙터 연결 불필요).
//VR 카메라는 다른 씬에 있어 드래그 연결이 안 되므로 런타임에 Camera.main으로 찾는다.
public class DebugStatusPanel : MonoBehaviour
{
    private static DebugStatusPanel _instance;

    //카메라 기준 위치/크기 (보기 불편하면 이 값들만 조절하면 됨)
    private static readonly Vector3 LocalOffset = new Vector3(0.52f, 0f, 0.8f); //오른쪽 + 앞 0.8m
    private const float CanvasWidth = 420f;
    private const float CanvasHeight = 620f;
    private const float CanvasScale = 0.0008f; //World-Space 캔버스 축소 (약 0.34m 폭)

    private const KeyCode ToggleKey = KeyCode.F1; //F1으로 패널 on/off

    private readonly StringBuilder _sb = new StringBuilder();
    private GamePlayManager _gp;
    [SerializeField] private Camera _cam;
    private GameObject _canvasGo;
    private TextMeshProUGUI _text;
    private float _refindTimer;
    [SerializeField] private bool _visible = true;

    private void Awake()
    {
        //실수로 두 개 올려도 중복되지 않게 가드
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Update()
    {
        //F1으로 패널 켜고 끄기
        if (Input.GetKeyDown(ToggleKey))
        {
            _visible = !_visible;
            if (_canvasGo != null) _canvasGo.SetActive(_visible);
        }
        if (!_visible) return; //꺼져 있으면 갱신/생성 모두 스킵

        //매니저는 게임플레이 씬 로드/언로드로 바뀌므로 없을 때만 주기적으로 다시 찾는다
        if (_gp == null)
        {
            _refindTimer -= Time.unscaledDeltaTime;
            if (_refindTimer <= 0f)
            {
                _gp = FindAnyObjectByType<GamePlayManager>();
                _refindTimer = 0.5f;
            }
        }

        //카메라(HMD) 확보. 씬이 바뀌면 카메라가 죽으므로 캔버스도 다시 만든다
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) return;
        }
        if (_canvasGo == null)
        {
            BuildCanvas();
        }

        //텍스트 갱신
        _sb.Clear();
        if (_gp != null)
        {
            _gp.BuildDebugStatus(_sb);
        }
        else
        {
            //매니저가 없는 씬(메뉴 등)에서도 패널이 떠 있는지 확인용
            _sb.AppendLine("<b>[ DEBUG STATUS ]</b>");
            _sb.AppendLine("GamePlayManager 탐색 중...");
            _sb.AppendLine("(게임플레이 씬이 아닐 수 있음)");
        }
        _text.text = _sb.ToString();
    }

    //World-Space Canvas를 카메라 자식으로 생성한다 (배경 Image + TMP 텍스트)
    private void BuildCanvas()
    {
        _canvasGo = new GameObject("DebugStatusCanvas");
        _canvasGo.transform.SetParent(_cam.transform, false);
        _canvasGo.transform.localPosition = LocalOffset;
        //카메라 자식이라 회전 0이면 캔버스 정면(+Z)이 시점과 같은 방향 → 글자가 똑바로 보임
        _canvasGo.transform.localRotation = Quaternion.identity;

        var canvas = _canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = _cam;

        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
        rt.localScale = Vector3.one * CanvasScale;

        //반투명 검정 배경
        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(_canvasGo.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        //텍스트
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(_canvasGo.transform, false);
        _text = textGo.AddComponent<TextMeshProUGUI>();
        _text.fontSize = 22f;
        _text.color = Color.white;
        _text.alignment = TextAlignmentOptions.TopLeft;
        _text.richText = true;
        var trt = _text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(14f, 14f);
        trt.offsetMax = new Vector2(-14f, -14f);
    }
}
#endif
