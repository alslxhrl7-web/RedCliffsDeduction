Shader "RedCliffs/Character Hair"
{
    Properties
    {
        _BaseMap("Color and coverage", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        [Normal] _BumpMap("Tangent normal", 2D) = "bump" {}
        _BumpScale("Normal strength", Range(0,2)) = 1
        _MetallicGlossMap("Material", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = .45
        _Metallic("Metallic", Range(0,1)) = 0
        _Cutoff("Coverage cutoff", Range(0,1)) = .18
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Clip", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
        _ZWrite("Depth write", Float) = 1
        _AlphaToMask("Coverage", Float) = 0
        _Surface("Surface", Float) = 0
        _EnvironmentReflections("Environment", Float) = 1
        _SpecularStrength("Strand reflection", Range(0,.4)) = .09
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Name "StrandLighting"
            Tags {"LightMode"="UniversalForwardOnly"}
            Cull [_Cull] ZWrite [_ZWrite] AlphaToMask [_AlphaToMask]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor;
                half _BumpScale, _Smoothness, _Metallic, _Cutoff, _AlphaClip, _Cull;
                half _ZWrite, _AlphaToMask, _Surface, _EnvironmentReflections, _SpecularStrength;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half4 tangentWS:TEXCOORD2; float2 uv:TEXCOORD3; half fog:TEXCOORD4; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes v)
            {
                Varyings o=(Varyings)0; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n=GetVertexNormalInputs(v.normalOS,v.tangentOS);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.normalWS=n.normalWS;
                o.tangentWS=half4(n.tangentWS,v.tangentOS.w*GetOddNegativeScale());
                o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);return o;
            }
            half3 StrandLight(Light light, half3 n, half3 strand, half3 view, half3 color)
            {
                half nl=dot(n,light.direction);
                half diffuse=saturate((nl+.18h)/1.18h);
                half3 h=SafeNormalize(light.direction+view);
                half t=dot(normalize(strand+n*.15h),h);
                half t2=dot(normalize(strand-n*.22h),h);
                half exponent=lerp(20.h,90.h,_Smoothness);
                half primary=pow(saturate(1-t*t),exponent*.5h);
                half secondary=pow(saturate(1-t2*t2),6.h)*.30h;
                half spec=(primary+secondary)*_SpecularStrength*saturate(nl+.2h);
                return (color*diffuse+half3(.8h,.72h,.62h)*spec)*light.color*light.distanceAttenuation*light.shadowAttenuation;
            }
            half4 Frag(Varyings i, FRONT_FACE_TYPE facing:FRONT_FACE_SEMANTIC):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(base.a-_Cutoff);
                #endif
                half3 n=normalize(i.normalWS);half3 tangent=normalize(i.tangentWS.xyz);
                half3 bitangent=normalize(cross(n,tangent)*i.tangentWS.w);
                half faceSign=IS_FRONT_VFACE(facing,1.h,-1.h);
                #if defined(_NORMALMAP)
                    half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
                    n=normalize(tangent*normalTS.x+bitangent*normalTS.y+n*normalTS.z);
                #endif
                n*=faceSign;
                half3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
                half3 color=base.rgb*max(SampleSH(n),half3(.025h,.025h,.025h));
                Light main=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                color+=StrandLight(main,n,bitangent,view,base.rgb);
                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData=(InputData)0;inputData.positionWS=i.positionWS;inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                    #if USE_CLUSTER_LIGHT_LOOP
                        [loop] for(uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);lightIndex++)
                        {Light light=GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1));color+=StrandLight(light,n,bitangent,view,base.rgb);}
                    #endif
                    uint count=GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(count)
                        Light light=GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1));color+=StrandLight(light,n,bitangent,view,base.rgb);
                    LIGHT_LOOP_END
                #endif
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    Fallback "Universal Render Pipeline/Lit"
}
