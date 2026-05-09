Shader "Hidden/Project/ShooterVisibilityFog"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _ShooterVisibilityFogEnabled;
            float3 _ShooterVisibilityCenter;
            float _ShooterVisibilityRadius;
            float _ShooterVisibilitySoftEdge;
            float4 _ShooterVisibilityFogColor;
            float _ShooterVisibilityFogOpacity;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                if (_ShooterVisibilityFogEnabled < 0.5)
                    return color;

                float depth = SampleSceneDepth(uv);
                float3 worldPosition = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float distanceFromCenter = distance(worldPosition.xz, _ShooterVisibilityCenter.xz);
                float fadeStart = _ShooterVisibilityRadius;
                float fadeEnd = _ShooterVisibilityRadius + max(_ShooterVisibilitySoftEdge, 0.001);
                float hiddenFactor = smoothstep(fadeStart, fadeEnd, distanceFromCenter);
                float fogAmount = saturate(hiddenFactor * _ShooterVisibilityFogOpacity);

                color.rgb = lerp(color.rgb, _ShooterVisibilityFogColor.rgb, fogAmount);
                return color;
            }
            ENDHLSL
        }
    }
}
