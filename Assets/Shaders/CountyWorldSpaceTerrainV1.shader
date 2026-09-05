Shader "Mandate/County World Space Terrain V1"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.08
        _Ambient ("Strategic Map Ambient", Range(0,0.5)) = 0.18
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert
        #pragma target 3.0
        fixed4 _Color;
        half _Glossiness;
        half _Ambient;
        struct Input { fixed4 color : COLOR; };
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color;
        }
        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.color.rgb * _Color.rgb;
            // Retain readable land-use and elevation masses in the wide
            // strategic view while directional lights still describe relief.
            o.Emission = IN.color.rgb * _Color.rgb * _Ambient;
            o.Alpha = 1;
            o.Gloss = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
