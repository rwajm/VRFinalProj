Shader "Custom/Road"
{
    Properties
    {
        _Color ("Road Color", Color) = (0.25, 0.25, 0.25, 1)
        _Brightness ("Brightness", Range(0.5, 1.5)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Geometry+10"  // 지도 타일 위에 렌더링
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
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float4 color : COLOR;
                UNITY_FOG_COORDS(1)
            };
            
            fixed4 _Color;
            float _Brightness;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // 간단한 Lambert 조명
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(0.3, dot(i.worldNormal, lightDir));
                
                fixed4 col = _Color * i.color;
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