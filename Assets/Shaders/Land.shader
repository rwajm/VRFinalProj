Shader "Custom/Land"
{
    Properties
    {
        _Color ("Land Color", Color) = (0.4, 0.6, 0.3, 1)
        _Brightness ("Brightness", Range(0.5, 1.5)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Geometry+3"  // 타일 위, 물 아래
        }
        
        LOD 200
        
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
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };
            
            fixed4 _Color;
            float _Brightness;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Lambert 조명
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(0.4, dot(i.worldNormal, lightDir));
                
                fixed4 col = _Color;
                col.rgb *= NdotL * _Brightness;
                
                // Fog 적용
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}