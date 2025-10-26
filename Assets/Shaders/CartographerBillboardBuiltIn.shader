Shader "RealmsOfEldor/CartographerBillboardBuiltIn"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.1
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
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _Cutoff;

            v2f vert(appdata v)
            {
                v2f o;

                // Get object's world position (center of billboard)
                float3 worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;

                // Get camera position
                float3 cameraPos = _WorldSpaceCameraPos;

                // Calculate direction from billboard to camera
                float3 toCamera = normalize(cameraPos - worldPos);

                // ISOMETRIC BILLBOARDING:
                // Project camera direction onto XZ plane (ignore Y to stay upright)
                float3 toCameraFlat = float3(toCamera.x, 0, toCamera.z);
                toCameraFlat = normalize(toCameraFlat);

                // Calculate billboard orientation vectors
                // Up vector stays vertical (Y+)
                float3 up = float3(0, 1, 0);

                // Right vector is perpendicular to both up and toCamera
                float3 right = normalize(cross(up, toCameraFlat));

                // Transform vertex from local space to billboard space
                // v.vertex.xy are the quad's local coordinates (-0.5 to 0.5)
                float3 billboardPos = worldPos
                    + right * v.vertex.x
                    + up * v.vertex.y;

                // Transform to clip space
                o.pos = mul(UNITY_MATRIX_VP, float4(billboardPos, 1.0));

                // Pass through UVs
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // Fog
                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                // Alpha cutout (discard transparent pixels)
                clip(col.a - _Cutoff);

                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDCG
        }

        // Shadow caster pass for built-in pipeline
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Cutoff;

            v2f vert(appdata v)
            {
                v2f o;

                // Same billboard transformation as main pass
                float3 worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 cameraPos = _WorldSpaceCameraPos;
                float3 toCamera = normalize(cameraPos - worldPos);
                float3 toCameraFlat = normalize(float3(toCamera.x, 0, toCamera.z));

                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, toCameraFlat));

                float3 billboardPos = worldPos
                    + right * v.vertex.x
                    + up * v.vertex.y;

                // Transform to clip space for shadow casting
                float4 worldPosVec = float4(billboardPos, 1.0);
                o.pos = UnityObjectToClipPos(v.vertex); // Use original vertex for now
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample alpha for cutout
                fixed alpha = tex2D(_MainTex, i.uv).a;
                clip(alpha - _Cutoff);

                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    FallBack "Transparent/Cutout/VertexLit"
}
