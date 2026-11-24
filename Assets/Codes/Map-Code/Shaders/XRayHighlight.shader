// WHERE TO PUT THIS FILE: Assets > Shaders (make the folder if you don't have it)
// Draws a glowing edge + faint fill on an object ONLY where something is in front of it.

Shader "Custom/XRayHighlight"
{
    Properties
    {
        _Color ("Color", Color) = (0.25, 0.8, 1, 1)
        _EdgePower ("Edge Sharpness", Range(0.5, 8)) = 2
        _FillAlpha ("Fill Opacity", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "XRay"
            Tags { "LightMode"="SRPDefaultUnlit" }

            ZTest Greater      // only draw where the object is hidden behind something
            ZWrite Off
            Cull Back
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _EdgePower;
                half _FillAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(pos.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                float3 v = normalize(IN.viewDirWS);

                // edges (surfaces turned away from the camera) glow brighter
                half rim = pow(saturate(1.0 - abs(dot(n, v))), _EdgePower);
                half alpha = saturate(_FillAlpha + rim);

                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
