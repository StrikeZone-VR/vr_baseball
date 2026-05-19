Shader "UI/RoundedCorners"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // x = width, y = height, z = radius (in pixels)
        // 기본값을 매우 큰 사각형 + radius 0 으로 둬서, 스크립트가 갱신하기 전까지는 클리핑 없이 그대로 렌더링되게 함
        _WidthHeightRadius ("WidthHeightRadius", Vector) = (100000, 100000, 0, 0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1; // RoundedCorners 가 IMeshModifier 로 박아넣은 rect-local 좌표
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 localPos : TEXCOORD2; // RectTransform 로컬 좌표 (rect.center 기준 오프셋)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _WidthHeightRadius;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                // UV1 에 rect-local 좌표를 박아넣어둠 → 캔버스 배칭이 vertex 위치를 변환해도 그대로 통과.
                // RoundedCorners.ModifyMesh 에서 (vert.position - rect.center) 를 uv1 에 기록함.
                OUT.localPos = v.texcoord1;

                OUT.color = v.color * _Color;
                return OUT;
            }

            // Signed distance from a rounded box centered at origin.
            // p = pixel offset from center, b = half-size minus radius, r = corner radius
            float RoundedBoxSDF(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                // Compute rounded-corner mask in pixel space.
                float2 halfSize = _WidthHeightRadius.xy * 0.5;
                float radius = min(_WidthHeightRadius.z, min(halfSize.x, halfSize.y));
                float dist = RoundedBoxSDF(IN.localPos, halfSize - radius, radius);
                // 1px anti-aliased edge
                float mask = saturate(0.5 - dist);
                color.a *= mask;

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
