Shader "RealmsOfEldor/BillboardCylindrical"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="TransparentCutout"
            "IgnoreProjector"="True"
            "DisableBatching"="True" // Required for vertex shader billboarding
        }

        LOD 100
        Cull Off
        Lighting Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Cutoff;

            v2f vert (appdata v)
            {
                v2f o;

                // Get object position in world space (center of billboard)
                float3 worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;

                // Calculate direction to camera (only XZ plane for cylindrical billboard)
                float3 camToObj = _WorldSpaceCameraPos - worldPos;
                camToObj.y = 0; // Project onto horizontal plane (stay upright)

                // Normalize direction
                camToObj = normalize(camToObj);

                // Calculate right and up vectors for billboard
                float3 up = float3(0, 1, 0); // Always point up (cylindrical)
                float3 right = normalize(cross(up, camToObj));

                // Reconstruct vertex position using billboard orientation
                // v.vertex is in local space, typically (-0.5 to 0.5) for a quad
                float3 billboardPos = worldPos + right * v.vertex.x + up * v.vertex.y;

                // Transform to clip space
                o.vertex = mul(UNITY_MATRIX_VP, float4(billboardPos, 1.0));

                // Pass through UVs and color
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;

                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample texture
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // Alpha cutoff for transparency
                clip(col.a - _Cutoff);

                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDCG
        }

        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM
            #pragma vertex vert_shadow
            #pragma fragment frag_shadow
            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"

            struct appdata_shadow
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f_shadow
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;

            v2f_shadow vert_shadow(appdata_shadow v)
            {
                v2f_shadow o;

                // Same billboard calculation as main pass
                float3 worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 camToObj = _WorldSpaceCameraPos - worldPos;
                camToObj.y = 0;
                camToObj = normalize(camToObj);

                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(up, camToObj));

                float3 billboardPos = worldPos + right * v.vertex.x + up * v.vertex.y;

                // Transform for shadow casting
                v.vertex = mul(unity_WorldToObject, float4(billboardPos, 1.0));

                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            float4 frag_shadow(v2f_shadow i) : SV_Target
            {
                // Alpha cutoff for shadows
                fixed4 texcol = tex2D(_MainTex, i.uv);
                clip(texcol.a - _Cutoff);

                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    Fallback "Transparent/Cutout/VertexLit"
}
