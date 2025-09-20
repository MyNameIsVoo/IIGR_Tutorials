Shader "DrawMeshInstancedIndirect/SingleGrass"
{
    Properties
    {
        [MainColor] _BaseColor("BaseColor", Color) = (1,1,1,1)
		_AdditionalColor("AdditionalColor", Color) = (1,1,1,1)
        _GroundColor("_GroundColor", Color) = (0.5,0.5,0.5)

		_NoiseTexture("Noise texture", 2D) = "white" {}
		_WindTexture("Wind texture", 2D) = "white" {}
		_WindSpeed("Wind speed", float) = 0
		_WindStrength("Wind strength", float) = 0

        [Header(Grass Shape)]
        _GrassWidth("_GrassWidth", Float) = 1

        [Header(Lighting)]
        _RandomNormal("_RandomNormal", Float) = 0.15
		
        [HideInInspector]_PivotPosWS("_PivotPosWS", Vector) = (0,0,0,0)
        [HideInInspector]_BoundSize("_BoundSize", Vector) = (1,1,0)
    }
	
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline"}
        Pass
        {
			Cull Back
            ZTest Less
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                half3 color        : COLOR;
            };

			sampler2D _NoiseTexture;
			float4 _NoiseTexture_ST;

			sampler2D _WindTexture;
			float4 _WindTexture_ST;
			float _WindSpeed;
			float _WindStrength;

            CBUFFER_START(UnityPerMaterial)
                float3 _PivotPosWS;
                float2 _BoundSize;

                float _GrassWidth;

                half3 _BaseColor;
				half3 _AdditionalColor;
                half3 _GroundColor;

                half _RandomNormal;

                StructuredBuffer<float3> _AllInstancesTransformBuffer;
				StructuredBuffer<float> _AllInstancesHeightBuffer;
                StructuredBuffer<uint> _VisibleInstanceOnlyTransformIDBuffer;
            CBUFFER_END

            sampler2D _GrassBendingRT;

            half3 ApplySingleDirectLight(Light light, half3 N, half3 V, half3 albedo, half positionOSY)
            {
                half3 H = normalize(light.direction + V);
                half directDiffuse = dot(N, light.direction) * 0.5 + 0.5;
                float directSpecular = saturate(dot(N,H));

                directSpecular *= directSpecular;
                directSpecular *= directSpecular;
                directSpecular *= directSpecular;
                directSpecular *= 0.1 * positionOSY;

                half3 lighting = light.color * (light.shadowAttenuation * light.distanceAttenuation);
                half3 result = (albedo * directDiffuse + directSpecular) * lighting;
                return result; 
            }

			float random(float2 uv)
			{
				return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
			}

            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;

                float3 perGrassPivotPosWS = _AllInstancesTransformBuffer[_VisibleInstanceOnlyTransformIDBuffer[instanceID]];
				float perGrassHeight = lerp(2, 5, (sin(perGrassPivotPosWS.x*23.4643 + perGrassPivotPosWS.z) * 0.45 + 0.55)) *_AllInstancesHeightBuffer[_VisibleInstanceOnlyTransformIDBuffer[instanceID]];
                float2 grassBendingUV = ((perGrassPivotPosWS.xz - _PivotPosWS.xz) / _BoundSize) * 0.5 + 0.5;
                float stepped = tex2Dlod(_GrassBendingRT, float4(grassBendingUV, 0, 0)).x;
                float3 cameraTransformRightWS = UNITY_MATRIX_V[0].xyz;
                float3 cameraTransformForwardWS = -UNITY_MATRIX_V[2].xyz;
				float3 positionOS = IN.positionOS.x * _GrassWidth * (sin(perGrassPivotPosWS.x * 95.4643 + perGrassPivotPosWS.z) * 0.45 + 0.55) * cameraTransformRightWS;
				positionOS.y += IN.positionOS.y;

                float3 bendDir = cameraTransformForwardWS;
                bendDir.xz *= 0.3;
                bendDir.y = min(-0.5, bendDir.y);
                positionOS = lerp(positionOS.xyz + bendDir * positionOS.y / -bendDir.y, positionOS.xyz, stepped * 0.95 + 0.05);
                positionOS.y *= perGrassHeight;
     
                float3 viewWS = _WorldSpaceCameraPos - perGrassPivotPosWS;
                float ViewWSLength = length(viewWS);
                positionOS += cameraTransformRightWS * IN.positionOS.x * max(0, ViewWSLength * 0.002);
                
                float3 positionWS = positionOS + perGrassPivotPosWS;
				float4 worldPos = mul(unity_ObjectToWorld, positionWS);       
				
                float wind = tex2Dlod(_WindTexture, float4(worldPos.xz * _WindTexture_ST.xy + _Time.y * _WindSpeed, 0.0, 0.0)).xy;
                wind *= IN.positionOS.y * _WindStrength;
                float3 windOffset = cameraTransformRightWS * wind; 
                positionWS.xyz += windOffset;
                
                OUT.positionCS = TransformWorldToHClip(positionWS);

                Light mainLight;
#if _MAIN_LIGHT_SHADOWS
                mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
#else
                mainLight = GetMainLight();
#endif
				float randomValue = random(_AllInstancesTransformBuffer[_VisibleInstanceOnlyTransformIDBuffer[instanceID]].xy);
                half3 albedo;
				if (randomValue > 0.5)
					albedo = lerp(_GroundColor, _BaseColor, IN.positionOS.y);
				else
					albedo = lerp(_GroundColor, _AdditionalColor, IN.positionOS.y);

                half3 lightingResult = SampleSH(0) * albedo;

				half3 randomAddToN = (_RandomNormal * sin(perGrassPivotPosWS.x * 82.32523 + perGrassPivotPosWS.z) + wind * -0.25) * cameraTransformRightWS;
				half3 N = normalize(half3(0, 1, 0) + randomAddToN - cameraTransformForwardWS * 0.5);
				half3 V = viewWS / ViewWSLength;

                lightingResult += ApplySingleDirectLight(mainLight, N, V, albedo, positionOS.y);

                float fogFactor = ComputeFogFactor(OUT.positionCS.z);
                OUT.color = MixFog(lightingResult, fogFactor);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(IN.color, 1);
            }
            ENDHLSL
        }

		
		Pass
		{
			Cull Back
			ZTest Less
			Tags { "LightMode" = "ShadowCaster" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Attributes
			{
				float4 positionOS   : POSITION;
			};

			struct Varyings
			{
				float4 positionCS  : SV_POSITION;
				half3 color        : COLOR;
			};

			sampler2D _NoiseTexture;
			float4 _NoiseTexture_ST;

			sampler2D _WindTexture;
			float4 _WindTexture_ST;
			float _WindSpeed;
			float _WindStrength;

			CBUFFER_START(UnityPerMaterial)
				float3 _PivotPosWS;
				float2 _BoundSize;
				
				float _GrassWidth;

				half3 _BaseColor;
				half3 _GroundColor;

				half _RandomNormal;

				StructuredBuffer<float3> _AllInstancesTransformBuffer;
				StructuredBuffer<float> _AllInstancesHeightBuffer;
				StructuredBuffer<uint> _VisibleInstanceOnlyTransformIDBuffer;
			CBUFFER_END

			sampler2D _GrassBendingRT;

			half3 ApplySingleDirectLight(Light light, half3 N, half3 V, half3 albedo, half positionOSY)
			{
				half3 H = normalize(light.direction + V);
				half directDiffuse = dot(N, light.direction) * 0.5 + 0.5;
				float directSpecular = saturate(dot(N,H));

				directSpecular *= directSpecular;
				directSpecular *= directSpecular;
				directSpecular *= directSpecular;
				directSpecular *= 0.1 * positionOSY;

				half3 lighting = light.color * (light.shadowAttenuation * light.distanceAttenuation);
				half3 result = (albedo * directDiffuse + directSpecular) * lighting;
				return result;
			}

			Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
			{
				Varyings OUT;

				float3 perGrassPivotPosWS = _AllInstancesTransformBuffer[_VisibleInstanceOnlyTransformIDBuffer[instanceID]];
				float perGrassHeight = lerp(2, 5, (sin(perGrassPivotPosWS.x*23.4643 + perGrassPivotPosWS.z) * 0.45 + 0.55)) *_AllInstancesHeightBuffer[_VisibleInstanceOnlyTransformIDBuffer[instanceID]];
				float2 grassBendingUV = ((perGrassPivotPosWS.xz - _PivotPosWS.xz) / _BoundSize) * 0.5 + 0.5;
				float stepped = tex2Dlod(_GrassBendingRT, float4(grassBendingUV, 0, 0)).x;
				float3 cameraTransformRightWS = UNITY_MATRIX_V[0].xyz;
				float3 cameraTransformUpWS = UNITY_MATRIX_V[1].xyz;
				float3 cameraTransformForwardWS = -UNITY_MATRIX_V[2].xyz;

				float3 positionOS = IN.positionOS.x * cameraTransformRightWS * _GrassWidth;
				positionOS += IN.positionOS.y * cameraTransformUpWS;
				positionOS.y *= perGrassHeight;
    
				float3 viewWS = _WorldSpaceCameraPos - perGrassPivotPosWS;
				float ViewWSLength = length(viewWS);
				positionOS += cameraTransformRightWS * IN.positionOS.x * max(0, ViewWSLength * 0.002);

				float3 positionWS = positionOS + perGrassPivotPosWS;
				float4 worldPos = mul(unity_ObjectToWorld, positionWS);
          
				float wind = tex2Dlod(_WindTexture, float4(worldPos.xz * _WindTexture_ST.xy + _Time.y * _WindSpeed, 0.0, 0.0)).xy;
				wind *= IN.positionOS.y * _WindStrength;
				float3 windOffset = cameraTransformRightWS * wind;
				positionWS.xyz += windOffset;

				OUT.positionCS = TransformWorldToHClip(positionWS);

				Light mainLight;
#if _MAIN_LIGHT_SHADOWS
				mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
#else
				mainLight = GetMainLight();
#endif
				half3 randomAddToN = (_RandomNormal * sin(perGrassPivotPosWS.x * 82.32523 + perGrassPivotPosWS.z) + wind * -0.25) * cameraTransformRightWS;
				half3 N = normalize(half3(0,1,0) + randomAddToN - cameraTransformForwardWS * 0.5);

				half3 V = viewWS / ViewWSLength;
				half3 albedo = lerp(_GroundColor, _BaseColor, IN.positionOS.y);

				half3 lightingResult = SampleSH(0) * albedo + ApplySingleDirectLight(mainLight, N, V, albedo, positionOS.y);
				OUT.color = lightingResult;

				return OUT;
			}

			half4 frag(Varyings IN) : SV_Target
			{
				return half4(IN.color, 1);
			}
			ENDHLSL
		}
    }
}