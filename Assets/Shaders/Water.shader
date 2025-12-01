Shader "Custom/Water"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.2, 0.5, 0.8, 0.85)
        _Glossiness ("Smoothness", Range(0,1)) = 0.8
        _SpecColor ("Specular Color", Color) = (1, 1, 1, 1)
        _WaveSpeed ("Wave Speed", Range(0, 2)) = 0.5
        _WaveScale ("Wave Scale", Range(0, 0.1)) = 0.02
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Geometry+5"  // 도로보다 아래, 타일 위
        }
        
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        
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
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };
            
            fixed4 _Color;
            fixed4 _SpecColor;
            float _Glossiness;
            float _WaveSpeed;
            float _WaveScale;
            
            v2f vert(appdata v)
            {
                v2f o;
                
                // 물결 애니메이션 (선택사항)
                float wave = sin(_Time.y * _WaveSpeed + v.vertex.x * 10 + v.vertex.z * 10) * _WaveScale;
                v.vertex.y += wave;
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Diffuse (확산광)
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(0.4, dot(i.worldNormal, lightDir));
                
                // Specular (반사광) - 물의 반짝임
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 halfDir = normalize(lightDir + viewDir);
                float specular = pow(max(0, dot(i.worldNormal, halfDir)), 32.0) * _Glossiness;
                
                // Fresnel 효과 (가장자리가 더 밝음)
                float fresnel = pow(1.0 - max(0, dot(viewDir, i.worldNormal)), 3.0);
                
                fixed4 col = _Color;
                col.rgb *= NdotL; // Diffuse
                col.rgb += specular * 0.5; // Specular
                col.rgb += fresnel * 0.2; // Fresnel
                
                // Fog 적용
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/Diffuse"
}