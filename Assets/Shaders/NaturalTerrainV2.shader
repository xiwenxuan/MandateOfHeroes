Shader "Mandate/Natural Terrain V2"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _NoiseStrength ("Natural variation", Range(0,0.25)) = 0.08
        _Saturation ("Saturation", Range(0,1.5)) = 1
        _SlopeStrength ("Slope shading", Range(0,1)) = 0.35
        _CurvatureStrength ("Curvature shading", Range(0,1)) = 0.15
        _RidgeStrength ("Ridge highlight", Range(0,1)) = 0.15
        _ValleyStrength ("Valley darkening", Range(0,1)) = 0.12
        _MacroScale ("Macro scale", Range(1,12)) = 5
        _MacroStrength ("Macro strength", Range(0,0.2)) = 0.06
        _FusionMode ("Style D fusion mode", Range(0,1)) = 0
        _FusionStrength ("Style D fusion strength", Range(0,1)) = 0
        _FusionMountainTint ("Mountain mass tint", Color) = (0.48,0.43,0.31,1)
        _FusionForestTint ("Forest area tint", Color) = (0.29,0.43,0.27,1)
        _FusionRiverValleyTint ("River valley tint", Color) = (0.43,0.64,0.65,1)
        _FusionPlainTint ("Plain tint", Color) = (0.72,0.69,0.49,1)
        _VisualDetail ("Presentation-only detail blend", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 180

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            // Global grid rows run north-to-south, so the authoritative mesh winding
            // is intentionally preserved even though its front face is opposite to
            // Unity's default convention. Render both sides instead of mutating the
            // permanent Cell topology merely for presentation.
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            fixed4 _Tint;
            half _NoiseStrength;
            half _Saturation;
            half _SlopeStrength;
            half _CurvatureStrength;
            half _RidgeStrength;
            half _ValleyStrength;
            half _MacroScale;
            half _MacroStrength;
            half _FusionMode;
            half _FusionStrength;
            fixed4 _FusionMountainTint;
            fixed4 _FusionForestTint;
            fixed4 _FusionRiverValleyTint;
            fixed4 _FusionPlainTint;
            half _VisualDetail;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 fusionPrimary : TEXCOORD1;
                float4 fusionSecondary : TEXCOORD2;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR0;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 fusionPrimary : TEXCOORD3;
                float4 fusionSecondary : TEXCOORD4;
                UNITY_FOG_COORDS(5)
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1,0)), f.x),
                            lerp(hash21(i + float2(0,1)), hash21(i + float2(1,1)), f.x), f.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 normal = UnityObjectToWorldNormal(v.normal);
                normal *= normal.y < 0.0 ? -1.0 : 1.0;
                normal = normalize(normal);
                float lambert = 0.50 + 0.50 * saturate(dot(normal, normalize(_WorldSpaceLightPos0.xyz)));
                float slope = 1.0 - saturate(normal.y);
                float slopeShade = lerp(1.0, 0.70 + 0.30 * lambert, slope * _SlopeStrength);
                float variation = ((valueNoise(v.uv * 5.0) * 0.7 + valueNoise(v.uv * 19.0) * 0.3) - 0.5)
                                  * _NoiseStrength;
                float macro = (valueNoise(v.uv * _MacroScale) - 0.5) * _MacroStrength;
                float ridge = pow(saturate(normal.y), 5.0) * _RidgeStrength;
                float valley = pow(saturate(1.0 - normal.y), 2.0) * _ValleyStrength * 0.32;
                float3 baseColour = saturate(v.color.rgb * _Tint.rgb * (lambert + variation + macro + ridge - valley) * slopeShade);
                float featureRidge = saturate(v.fusionPrimary.x);
                float featureValley = saturate(v.fusionPrimary.y);
                float mountainMass = saturate(v.fusionPrimary.z);
                float plainMass = saturate(v.fusionPrimary.w);
                float forestArea = saturate(v.fusionSecondary.x);
                float riverValley = saturate(v.fusionSecondary.y);
                float relief = saturate(v.fusionSecondary.z);
                float basin = saturate(v.fusionSecondary.w);
                float fusion = saturate(_FusionMode * _FusionStrength);
                float3 fusionColour = baseColour;
                float strategicMacro = valueNoise(v.uv * 2.1 + float2(13.7, 5.9));
                float foothill = saturate(relief * (1.0 - mountainMass) * 1.8);
                fusionColour = lerp(fusionColour, _FusionPlainTint.rgb,
                    plainMass * fusion * (0.22 + 0.12 * basin));
                fusionColour *= 1.0 + plainMass * fusion * (strategicMacro - 0.5) * 0.10;
                fusionColour = lerp(fusionColour, _FusionMountainTint.rgb,
                    mountainMass * fusion * (0.38 + 0.22 * relief));
                fusionColour = lerp(fusionColour, _FusionMountainTint.rgb * 0.92,
                    foothill * fusion * 0.18);
                fusionColour = lerp(fusionColour, _FusionForestTint.rgb,
                    forestArea * fusion * 0.72);
                fusionColour *= 1.0 + forestArea * fusion * (valueNoise(v.uv * 11.0) - 0.5) *
                    lerp(0.06, 0.14, _VisualDetail);
                fusionColour = lerp(fusionColour, _FusionRiverValleyTint.rgb,
                    riverValley * fusion * 0.66);
                fusionColour *= 1.0 + featureRidge * fusion * (0.20 + mountainMass * 0.12);
                fusionColour *= 1.0 - featureValley * fusion * (0.18 + relief * 0.08);
                fusionColour *= 1.0 + (valueNoise(v.uv * lerp(15.0, 52.0, _VisualDetail)) - 0.5) *
                    _VisualDetail * 0.055;
                baseColour = saturate(lerp(baseColour, fusionColour, fusion));
                float luminance = dot(baseColour, float3(0.299, 0.587, 0.114));
                baseColour = lerp(luminance.xxx, baseColour, _Saturation);
                o.color = fixed4(saturate(baseColour), v.color.a);
                o.normal = normal;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.fusionPrimary = v.fusionPrimary;
                o.fusionSecondary = v.fusionSecondary;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float edgeChange = saturate(length(ddx(i.normal)) + length(ddy(i.normal)));
                float curvature = (edgeChange - 0.08) * _CurvatureStrength * 0.24;
                fixed4 colour = fixed4(saturate(i.color.rgb * (1.0 - curvature)), i.color.a);
                UNITY_APPLY_FOG(i.fogCoord, colour);
                return colour;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Color"
}
