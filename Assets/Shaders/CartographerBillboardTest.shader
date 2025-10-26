Shader "RealmsOfEldor/CartographerBillboardTest"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Geometry"
            "RenderType"="Opaque"
            "DisableBatching"="True"
        }

        LOD 100
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

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
                half4 _Color;
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

                // Project camera direction onto XZ plane (ignore Y to stay upright)
                float3 toCameraFlat = float3(toCamera.x, 0, toCamera.z);
                toCameraFlat = normalize(toCameraFlat);

                // Calculate billboard orientation vectors
                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, toCameraFlat));

                // Transform vertex from local space to billboard space
                float3 billboardPos = worldPos
                    + right * input.positionOS.x
                    + up * input.positionOS.y;

                // Transform to clip space
                output.positionCS = TransformWorldToHClip(billboardPos);

                // Pass through UVs
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                // NO alpha cutout - render everything
                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
