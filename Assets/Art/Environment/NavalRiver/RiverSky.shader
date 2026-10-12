Shader "RedCliffs/RiverSky"
{
    Properties {
        _SkyTint("Zenith", Color) = (.25,.4,.58,1)
        _GroundColor("Horizon", Color) = (.44,.54,.58,1)
        _Exposure("Exposure", Float) = 1
    }
    SubShader {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 pos:SV_POSITION; float3 direction:TEXCOORD0; };
            float4 _SkyTint, _GroundColor; float _Exposure;
            v2f vert(float4 vertex:POSITION) {v2f o;o.pos=UnityObjectToClipPos(vertex);o.direction=vertex.xyz;return o;}
            fixed4 frag(v2f i):SV_Target {
                float height=saturate(normalize(i.direction).y);
                return float4(lerp(_GroundColor.rgb,_SkyTint.rgb,pow(height,.6))*_Exposure,1);
            }
            ENDCG
        }
    }
}
