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

                // X-AXIS ROTATION BILLBOARDING:
                // Billboards always face the same horizontal direction (no Y rotation).
                // They tilt (rotate in X) to face the camera's altitude/height.
                // Camera's horizontal position (XZ) is ignored.

                // Right vector is always aligned with world X axis (no Y rotation)
                float3 right = float3(1, 0, 0);

                // Calculate forward vector by tilting based on camera height
                // Project toCamera onto the YZ plane (ignore X component) to get the tilt
                float3 toCameraYZ = normalize(float3(0, toCamera.y, toCamera.z));

                // Forward points toward camera in YZ plane
                float3 forward = toCameraYZ;

                // Up vector is perpendicular to both right and forward
                // Use cross(right, forward) to get upward-pointing vector
                float3 up = normalize(cross(right, forward));

                // Transform vertex from local space to billboard space
                // input.positionOS.xyz are the quad's local coordinates in object space
                // We need to apply the object's scale to get the correct size
                float3 scale = float3(
                    length(TransformObjectToWorld(float3(1,0,0)) - worldPos),
                    length(TransformObjectToWorld(float3(0,1,0)) - worldPos),
                    length(TransformObjectToWorld(float3(0,0,1)) - worldPos)
                );

                // Build the billboard by positioning vertices using right and up vectors
                // Apply X scale to right offset, Y scale to up offset
                // Quad vertices: Y from -0.5 (bottom) to +0.5 (top)
                // We want bottom at Y=0, so we shift all vertices UP by 0.5 in world space
                // This puts bottom at worldPos.y + 0, top at worldPos.y + scale.y
                float3 billboardPos = worldPos
                    + right * input.positionOS.x * scale.x
                    + up * (input.positionOS.y + 0.5) * scale.y;

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

                // X-axis rotation only
                float3 right = float3(1, 0, 0);
                float3 toCameraYZ = normalize(float3(0, toCamera.y, toCamera.z));
                float3 forward = toCameraYZ;
                float3 up = normalize(cross(right, forward)); // Fixed: correct cross product order

                // Calculate object scale
                float3 scale = float3(
                    length(TransformObjectToWorld(float3(1,0,0)) - worldPos),
                    length(TransformObjectToWorld(float3(0,1,0)) - worldPos),
                    length(TransformObjectToWorld(float3(0,0,1)) - worldPos)
                );

                // Build billboard (bottom at ground level)
                float3 billboardPos = worldPos
                    + right * input.positionOS.x * scale.x
                    + up * (input.positionOS.y + 0.5) * scale.y;

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

                // X-axis rotation only
                float3 right = float3(1, 0, 0);
                float3 toCameraYZ = normalize(float3(0, toCamera.y, toCamera.z));
                float3 forward = toCameraYZ;
                float3 up = normalize(cross(right, forward)); // Fixed: correct cross product order

                // Calculate object scale
                float3 scale = float3(
                    length(TransformObjectToWorld(float3(1,0,0)) - worldPos),
                    length(TransformObjectToWorld(float3(0,1,0)) - worldPos),
                    length(TransformObjectToWorld(float3(0,0,1)) - worldPos)
                );

                // Build billboard (bottom at ground level)
                float3 billboardPos = worldPos
                    + right * input.positionOS.x * scale.x
                    + up * (input.positionOS.y + 0.5) * scale.y;

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
