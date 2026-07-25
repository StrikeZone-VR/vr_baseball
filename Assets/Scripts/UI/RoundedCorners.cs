using UnityEngine;
using UnityEngine.UI;

namespace VR_Baseball.UI
{
    /// <summary>
    /// UI Graphic(Image/RawImage) 의 모서리를 셰이더 기반으로 둥글게 만든다.
    
    /// 그래서 IMeshModifier 로 각 정점의 rect-local 좌표(rect.center 기준 오프셋)를 UV1 에 박아넣는다.
    /// UV1 은 캔버스 배칭이 변환하지 않고 그대로 통과시키므로 SDF 가 정확히 동작한다.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class RoundedCorners : MonoBehaviour, IMeshModifier, IMaterialModifier
    {
        // 모서리 반경(px). RectTransform 의 짧은 변의 절반을 넘으면 셰이더에서 자동 클램프.
        [SerializeField, Min(0f)] private float radius = 20f;

        // 셰이더 이름 — Resources/UIRoundedCorners.shader 와 일치해야 함
        private const string ShaderName = "UI/RoundedCorners";
        private static readonly int WidthHeightRadiusId = Shader.PropertyToID("_WidthHeightRadius");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        private Material _material;
        private Graphic _graphic;
        private RectTransform _rect;

        public float Radius
        {
            get => radius;
            set
            {
                radius = Mathf.Max(0f, value);
                MarkDirty();
            }
        }

        private void OnEnable()
        {
            MarkDirty();
        }

        private void OnDisable()
        {
            MarkDirty();
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                if (Application.isPlaying) Destroy(_material);
                else DestroyImmediate(_material);
                _material = null;
            }
        }

        private void OnValidate()
        {
            MarkDirty();
        }

        private void OnRectTransformDimensionsChange()
        {
            MarkDirty();
        }

        private Graphic GetGraphic()
        {
            if (_graphic == null) _graphic = GetComponent<Graphic>();
            return _graphic;
        }

        private RectTransform GetRect()
        {
            if (_rect == null) _rect = (RectTransform)transform;
            return _rect;
        }

        private void MarkDirty()
        {
            var g = GetGraphic();
            if (g != null)
            {
                // 정점 데이터(UV1)와 머티리얼 모두 갱신 필요
                g.SetVerticesDirty();
                g.SetMaterialDirty();
            }
        }

        // IMeshModifier (deprecated overload) — 새 API 로 위임
        public void ModifyMesh(Mesh mesh) { }

        // IMeshModifier — Graphic 이 mesh 를 만든 직후, 캔버스 배칭이 변환하기 전에 호출됨.
        // 이 시점의 vert.position 은 rect-local 좌표. 이걸 rect.center 만큼 빼서 SDF 가 기대하는 형태(중심 기준 오프셋)로
        // 만든 뒤 uv1 에 저장한다.
        public void ModifyMesh(VertexHelper vh)
        {
            if (!isActiveAndEnabled) return;
            var rect = GetRect().rect;
            var center = rect.center;

            var vert = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vert, i);
                vert.uv1 = new Vector4(vert.position.x - center.x, vert.position.y - center.y, 0f, 0f);
                vh.SetUIVertex(vert, i);
            }
        }

        // IMaterialModifier — Unity 가 렌더링할 머티리얼을 요청할 때마다 호출됨.
        // baseMaterial: 원본 Image 머티리얼(보통 UI/Default). 우리는 우리 셰이더의 머티리얼을 돌려준다.
        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled) return baseMaterial;

            if (_material == null)
            {
                var shader = Shader.Find(ShaderName);
                if (shader == null)
                {
                    Debug.LogWarning($"[RoundedCorners] Shader '{ShaderName}' not found. Place under Resources or Always Included Shaders.", this);
                    return baseMaterial;
                }
                _material = new Material(shader) { hideFlags = HideFlags.DontSave };
            }

            // 원본 머티리얼의 메인 텍스처를 그대로 가져온다(스프라이트 텍스처 등)
            if (baseMaterial != null && baseMaterial.HasProperty(MainTexId))
            {
                _material.SetTexture(MainTexId, baseMaterial.GetTexture(MainTexId));
            }

            // RectTransform 크기/반경 갱신. rect.center 는 셰이더가 아니라 ModifyMesh 에서 처리됨.
            var r = GetRect().rect;
            _material.SetVector(WidthHeightRadiusId, new Vector4(r.width, r.height, radius, 0f));

            return _material;
        }
    }
}
