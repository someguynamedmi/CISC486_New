// Swirling, pixelated "melted paint" skybox in three grayscale tones.
// Two modes:
//   Screen Space ON  -> flat 2D swirl locked to the screen (closest to the classic 2D look).
//   Screen Space OFF -> swirl painted onto the sky itself, so it moves as the camera turns.
Shader "Skybox/SwirlSkybox"
{
    Properties
    {
        [Header(Colors)]
        _ColorLight   ("Light Tone", Color) = (0.92, 0.92, 0.92, 1)
        _ColorMid     ("Mid Tone",   Color) = (0.45, 0.45, 0.45, 1)
        _ColorDark    ("Dark Tone",  Color) = (0.06, 0.06, 0.06, 1)

        [Header(Mapping)]
        [Toggle(_SCREENSPACE)] _ScreenSpace ("Screen Space (flat look)", Float) = 1
        _Center       ("Swirl Center Direction (world mode)", Vector) = (0, 0, 1, 0)

        [Header(Motion)]
        _Speed        ("Flow Speed", Range(0, 3)) = 0.6
        _SpinSpeed    ("Spin Speed", Range(-2, 2)) = 0.25
        _SpinAmount   ("Spin Twist", Range(0, 6)) = 2.0

        [Header(Shape)]
        _Zoom         ("Zoom", Range(0.5, 10)) = 4.0
        _Warp         ("Warp Strength", Range(0, 1.5)) = 0.6
        _Bands        ("Band Frequency", Range(0.2, 4)) = 1.2
        _Softness     ("Edge Softness", Range(0.001, 0.3)) = 0.04

        [Header(Retro)]
        _PixelSize    ("Pixel Grid", Range(32, 1080)) = 260
        _Vignette     ("Vignette (screen mode)", Range(0, 1.5)) = 0.6
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _SCREENSPACE
            #include "UnityCG.cginc"

            float4 _ColorLight, _ColorMid, _ColorDark;
            float4 _Center;
            float _Speed, _SpinSpeed, _SpinAmount;
            float _Zoom, _Warp, _Bands, _Softness;
            float _PixelSize, _Vignette;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos       : SV_POSITION;
                float3 dir       : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;              // skybox vertices = view directions
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            float3 Swirl (float2 p, float t)
            {
                // Pixel snap
                p = floor(p * _PixelSize) / _PixelSize;

                // Whirlpool twist around the center
                float r = length(p);
                float ang = atan2(p.y, p.x)
                          + _SpinAmount * (1.0 - saturate(r * 1.4))
                          + t * _SpinSpeed;
                p = float2(cos(ang), sin(ang)) * r * _Zoom;

                // Domain warping for the liquid paint look
                float ft = t * _Speed;
                float2 q = p;
                [unroll]
                for (int k = 0; k < 5; k++)
                {
                    float fk = (float)k;
                    q += _Warp * float2(
                        sin(q.y * 1.3 + ft + fk * 1.7),
                        cos(q.x * 1.1 - ft * 0.8 + fk * 2.3));
                    q += _Warp * 0.5 * float2(
                        cos(q.x + q.y + ft * 0.5),
                        sin(q.x - q.y - ft * 0.4));
                }

                float v = 0.5 + 0.5 * sin(length(q) * _Bands + q.x * 0.35 - ft * 0.7);

                float s = _Softness;
                float3 col = lerp(_ColorDark.rgb, _ColorMid.rgb, smoothstep(0.33 - s, 0.33 + s, v));
                col = lerp(col, _ColorLight.rgb, smoothstep(0.70 - s, 0.70 + s, v));
                return col;
            }

            float4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float3 col;

            #if defined(_SCREENSPACE)
                float2 uv = i.screenPos.xy / i.screenPos.w;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = uv - 0.5;
                p.x *= aspect;
                col = Swirl(p, t);

                col *= 1.0 - _Vignette * dot(p, p);
            #else
                // Stereographic projection centered on _Center:
                // seamless everywhere except the point directly opposite the center.
                float3 d = normalize(i.dir);
                float3 fwd = normalize(_Center.xyz + 1e-5);
                float3 up = abs(fwd.y) > 0.99 ? float3(1, 0, 0) : float3(0, 1, 0);
                float3 right = normalize(cross(up, fwd));
                up = cross(fwd, right);

                float denom = max(1.0 + dot(d, fwd), 0.02);
                float2 p = float2(dot(d, right), dot(d, up)) / denom * 0.5;
                col = Swirl(p, t);
            #endif

                return float4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
