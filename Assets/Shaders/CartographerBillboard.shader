Shader "RealmsOfEldor/CartographerBillboard"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
            "IgnoreProjector"="True"
            "DisableBatching"="True"
        }

        LOD 100
        Cull Off
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Get object's world position (center of billboard)
                float3 worldPos = TransformObjectToWorld(float3(0, 0, 0));

                // Get camera position
                float3 cameraPos = GetCameraPositionWS();

                // Calculate direction from billboard to camera
                float3 toCamera = normalize(cameraPos - worldPos);

                // ISOMETRIC BILLBOARDING (Song of Conquest style):
                // Billboard rotates to face camera, but maintains its upright orientation.
                // The sprite rotates around its vertical axis (Y) to face the camera's XZ direction.

                // Project camera direction onto XZ plane (ignore Y to stay upright)
                float3 toCameraFlat = float3(toCamera.x, 0, toCamera.z);
                toCameraFlat = normalize(toCameraFlat);

                // Calculate billboard orientation vectors
                // Up vector stays vertical (Y+)
                float3 up = float3(0, 1, 0);

                // Right vector is perpendicular to both up and toCamera (cross product)
                float3 right = normalize(cross(up, toCameraFlat));

                // Forward vector is perpendicular to right and up
                float3 forward = cross(right, up);

                // Transform vertex from local space to billboard space
                // input.positionOS.xy are the quad's local coordinates (-0.5 to 0.5)
                // Apply object scale to the vertex offsets
                float3 localPos = TransformObjectToWorld(input.positionOS.xyz) - worldPos;
                float3 billboardPos = worldPos
                    + right * localPos.x
                    + up * localPos.y;

                // Transform to clip space
                output.positionCS = TransformWorldToHClip(billboardPos);
                output.positionWS = billboardPos;

                // Pass through UVs
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                // Fog
                output.fogCoord = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                // Alpha cutout (discard transparent pixels)
                clip(color.a - _Cutoff);

                // Simple lighting (receive main directional light)
                Light mainLight = GetMainLight();
                half3 lighting = mainLight.color * mainLight.distanceAttenuation;

                // Add ambient
                lighting += half3(0.4, 0.4, 0.5) * 0.3; // Ambient contribution

                color.rgb *= lighting;

                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogCoord);

                return color;
            }
            ENDHLSL
        }

        // Shadow casting pass - CRITICAL for shadows to work!
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _Cutoff;
            CBUFFER_END

            float3 _LightDirection;

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Same billboard transformation as main pass
                float3 worldPos = TransformObjectToWorld(float3(0, 0, 0));
                float3 cameraPos = GetCameraPositionWS();
                float3 toCamera = normalize(cameraPos - worldPos);
                float3 toCameraFlat = normalize(float3(toCamera.x, 0, toCamera.z));

                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, toCameraFlat));

                // Apply object scale
                float3 localPos = TransformObjectToWorld(input.positionOS.xyz) - worldPos;
                float3 billboardPos = worldPos
                    + right * localPos.x
                    + up * localPos.y;

                // Apply shadow bias
                float3 normalWS = up; // Billboard normal faces up
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(billboardPos, normalWS, _LightDirection));

                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample alpha for cutout
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // Depth pass for depth texture
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _Cutoff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Same billboard transformation
                float3 worldPos = TransformObjectToWorld(float3(0, 0, 0));
                float3 cameraPos = GetCameraPositionWS();
                float3 toCamera = normalize(cameraPos - worldPos);
                float3 toCameraFlat = normalize(float3(toCamera.x, 0, toCamera.z));

                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, toCameraFlat));

                // Apply object scale
                float3 localPos = TransformObjectToWorld(input.positionOS.xyz) - worldPos;
                float3 billboardPos = worldPos
                    + right * localPos.x
                    + up * localPos.y;

                output.positionCS = TransformWorldToHClip(billboardPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
