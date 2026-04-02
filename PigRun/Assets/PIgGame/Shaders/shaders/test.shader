// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Effect/test"
{
	Properties
	{
		[Enum(UnityEngine.Rendering.CullMode)]_CullMode("CullMode", Float) = 2
		[Enum(AlphaBlend,10,Additive,1)]_Dst("Mode", Float) = 10
		[HDR]_Main_color("Main_color", Color) = (0.8773585,0.8773585,0.8773585,1)
		_Maintex("Maintex", 2D) = "white" {}
		Main_U("Main_U", Float) = 0
		Main_V("Main_V", Float) = 0
		[Toggle(_NOISE_ON)] _Noise("Noise", Float) = 0
		_Noise_tex("Noise_tex", 2D) = "white" {}
		Noise_U("Noise_U", Float) = 0
		_Noise_V("Noise_V", Float) = 0
		_Noise_Int("Noise_Int", Range( 0 , 1)) = 0
		[KeywordEnum(Mask01,Multiply,Mask02)] _Mask("Mask", Float) = 1
		_Mask01("Mask01", 2D) = "white" {}
		_Mask1_U("Mask1_U", Float) = 0
		_Mask1_V("Mask1_V", Float) = 0
		_Mask02("Mask02", 2D) = "white" {}
		_Mask2_U("Mask2_U", Float) = 0
		Main_V2("Mask2_V", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IsEmissive" = "true"  }
		Cull [_CullMode]
		ZWrite Off
		Blend SrcAlpha [_Dst]
		
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "UnityShaderVariables.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _NOISE_ON
		#pragma shader_feature_local _MASK_MASK01 _MASK_MULTIPLY _MASK_MASK02
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		uniform half _Dst;
		uniform half _CullMode;
		uniform sampler2D _Maintex;
		uniform half Main_U;
		uniform half Main_V;
		uniform float4 _Maintex_ST;
		uniform sampler2D _Noise_tex;
		uniform half Noise_U;
		uniform half _Noise_V;
		uniform float4 _Noise_tex_ST;
		uniform half _Noise_Int;
		uniform half4 _Main_color;
		uniform sampler2D _Mask01;
		uniform half _Mask1_U;
		uniform half _Mask1_V;
		uniform float4 _Mask01_ST;
		uniform sampler2D _Mask02;
		uniform half _Mask2_U;
		uniform half Main_V2;
		uniform float4 _Mask02_ST;

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			half2 appendResult6 = (half2(Main_U , Main_V));
			float2 uv_Maintex = i.uv_texcoord * _Maintex_ST.xy + _Maintex_ST.zw;
			half2 MainUVMove2 = ( ( _Time.y * appendResult6 ) + uv_Maintex );
			half2 appendResult28 = (half2(Noise_U , _Noise_V));
			float2 uv_Noise_tex = i.uv_texcoord * _Noise_tex_ST.xy + _Noise_tex_ST.zw;
			half2 NoiseMove34 = (tex2D( _Noise_tex, ( ( _Time.y * appendResult28 ) + uv_Noise_tex ) )).rg;
			half2 lerpResult38 = lerp( MainUVMove2 , NoiseMove34 , float2( 0.6886792,0 ));
			#ifdef _NOISE_ON
				half2 staticSwitch53 = ( MainUVMove2 + ( lerpResult38 * _Noise_Int ) );
			#else
				half2 staticSwitch53 = MainUVMove2;
			#endif
			half4 tex2DNode14 = tex2D( _Maintex, staticSwitch53 );
			half2 appendResult58 = (half2(_Mask1_U , _Mask1_V));
			float2 uv_Mask01 = i.uv_texcoord * _Mask01_ST.xy + _Mask01_ST.zw;
			half3 desaturateInitialColor79 = tex2D( _Mask01, ( ( _Time.y * appendResult58 ) + uv_Mask01 ) ).rgb;
			half desaturateDot79 = dot( desaturateInitialColor79, float3( 0.299, 0.587, 0.114 ));
			half3 desaturateVar79 = lerp( desaturateInitialColor79, desaturateDot79.xxx, 1.0 );
			half3 temp_output_72_0 = (desaturateVar79).xyz;
			half2 appendResult65 = (half2(_Mask2_U , Main_V2));
			float2 uv_Mask02 = i.uv_texcoord * _Mask02_ST.xy + _Mask02_ST.zw;
			half3 desaturateInitialColor80 = tex2D( _Mask02, ( ( _Time.y * appendResult65 ) + uv_Mask02 ) ).rgb;
			half desaturateDot80 = dot( desaturateInitialColor80, float3( 0.299, 0.587, 0.114 ));
			half3 desaturateVar80 = lerp( desaturateInitialColor80, desaturateDot80.xxx, 1.0 );
			half3 temp_output_73_0 = (desaturateVar80).xyz;
			#if defined(_MASK_MASK01)
				half3 staticSwitch74 = temp_output_72_0;
			#elif defined(_MASK_MULTIPLY)
				half3 staticSwitch74 = ( temp_output_72_0 * temp_output_73_0 );
			#elif defined(_MASK_MASK02)
				half3 staticSwitch74 = temp_output_73_0;
			#else
				half3 staticSwitch74 = ( temp_output_72_0 * temp_output_73_0 );
			#endif
			half3 Mask77 = staticSwitch74;
			c.rgb = 0;
			c.a = ( tex2DNode14.a * _Main_color.a * i.vertexColor.a * Mask77 ).x;
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
			half2 appendResult6 = (half2(Main_U , Main_V));
			float2 uv_Maintex = i.uv_texcoord * _Maintex_ST.xy + _Maintex_ST.zw;
			half2 MainUVMove2 = ( ( _Time.y * appendResult6 ) + uv_Maintex );
			half2 appendResult28 = (half2(Noise_U , _Noise_V));
			float2 uv_Noise_tex = i.uv_texcoord * _Noise_tex_ST.xy + _Noise_tex_ST.zw;
			half2 NoiseMove34 = (tex2D( _Noise_tex, ( ( _Time.y * appendResult28 ) + uv_Noise_tex ) )).rg;
			half2 lerpResult38 = lerp( MainUVMove2 , NoiseMove34 , float2( 0.6886792,0 ));
			#ifdef _NOISE_ON
				half2 staticSwitch53 = ( MainUVMove2 + ( lerpResult38 * _Noise_Int ) );
			#else
				half2 staticSwitch53 = MainUVMove2;
			#endif
			half4 tex2DNode14 = tex2D( _Maintex, staticSwitch53 );
			o.Emission = ( (tex2DNode14).rgb * (_Main_color).rgb * (i.vertexColor).rgb );
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardCustomLighting keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				half4 color : COLOR0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				o.color = v.color;
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.vertexColor = IN.color;
				SurfaceOutputCustomLightingCustom o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputCustomLightingCustom, o )
				surf( surfIN, o );
				UnityGI gi;
				UNITY_INITIALIZE_OUTPUT( UnityGI, gi );
				o.Alpha = LightingStandardCustomLighting( o, worldViewDir, gi ).a;
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18912
98;333;1317;685;2557.754;213.426;3.057423;True;False
Node;AmplifyShaderEditor.CommentaryNode;32;-6104.857,774.6379;Inherit;False;1516.144;344.3229;Noise;10;34;46;33;31;29;30;28;27;45;44;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;44;-6090.002,948.7855;Inherit;False;Property;_Noise_V;Noise_V;10;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;45;-6080.722,861.5698;Inherit;False;Property;Noise_U;Noise_U;9;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;27;-5879.402,824.6379;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;28;-5845.144,898.5172;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;10;-6113.721,272.6317;Inherit;False;971.7179;389.3309;主贴图流动;8;9;6;7;8;5;2;43;41;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;41;-6077.691,468.9463;Inherit;False;Property;Main_V;Main_V;6;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;43;-6081.4,385.4418;Inherit;False;Property;Main_U;Main_U;5;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;29;-5885.07,999.8632;Inherit;False;0;33;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-5694.401,866.8376;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;81;-6118.377,1184.831;Inherit;False;2117.488;835.5176;Mask;23;57;63;64;56;58;65;66;59;60;61;67;68;69;62;70;71;80;79;73;72;75;74;77;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;31;-5542.575,886.2245;Inherit;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;5;-5900.15,322.6318;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;57;-6059.368,1381.145;Half;False;Property;_Mask1_V;Mask1_V;15;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;63;-6068.377,1743.933;Half;False;Property;_Mask2_U;Mask2_U;17;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;64;-6064.668,1827.438;Half;False;Property;Main_V2;Mask2_V;18;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;6;-5865.892,396.5113;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;56;-6063.077,1297.641;Half;False;Property;_Mask1_U;Mask1_U;14;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;59;-5881.827,1234.831;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;33;-5316.373,858.6947;Inherit;True;Property;_Noise_tex;Noise_tex;8;0;Create;True;0;0;0;False;1;;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-5715.151,364.8316;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;58;-5847.569,1308.71;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;65;-5852.87,1755.002;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;66;-5887.127,1681.123;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;8;-5905.818,497.8573;Inherit;False;0;14;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;9;-5563.325,384.2186;Inherit;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;61;-5696.829,1277.031;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;46;-5011.772,872.4418;Inherit;False;True;True;False;False;1;0;COLOR;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;67;-5892.795,1856.348;Inherit;False;0;71;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;60;-5887.495,1410.056;Inherit;False;0;70;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;68;-5702.128,1723.323;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;2;-5334.168,378.4823;Inherit;False;MainUVMove;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;34;-4789.461,866.7555;Inherit;False;NoiseMove;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;69;-5550.302,1742.71;Inherit;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;62;-5545.002,1296.417;Inherit;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;37;-3258.45,254.7722;Inherit;False;2;MainUVMove;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;36;-3255.306,340.6003;Inherit;False;34;NoiseMove;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;70;-5336.289,1266.932;Inherit;True;Property;_Mask01;Mask01;13;0;Create;False;0;0;0;False;1;;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;71;-5346.851,1715.216;Inherit;True;Property;_Mask02;Mask02;16;0;Create;False;0;0;0;False;1;;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DesaturateOpNode;80;-5039.868,1719.212;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DesaturateOpNode;79;-5029.68,1261.821;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;38;-3038.14,269.8813;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0.6886792,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-3177.983,455.8843;Inherit;False;Property;_Noise_Int;Noise_Int;11;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;52;-2819.948,360.5012;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;55;-2825.093,160.3328;Inherit;False;2;MainUVMove;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;73;-4864.973,1714.508;Inherit;False;True;True;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;72;-4859.401,1266.589;Inherit;False;True;True;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;75;-4656.172,1499.721;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;54;-2555.952,304.1225;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch;74;-4490.221,1480.088;Inherit;False;Property;_Mask;Mask;12;0;Create;False;0;0;0;False;0;False;0;1;1;True;;KeywordEnum;3;Mask01;Multiply;Mask02;Create;True;True;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;53;-2373.665,171.4504;Inherit;False;Property;_Noise;Noise;7;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;77;-4224.887,1479.808;Inherit;False;Mask;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;17;-2027.69,441.8832;Half;False;Property;_Main_color;Main_color;2;1;[HDR];Create;True;0;0;0;False;1;;False;0.8773585,0.8773585,0.8773585,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;14;-2131.607,147.1217;Inherit;True;Property;_Maintex;Maintex;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;21;-2020.847,680.6362;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;23;-1762.484,436.8397;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;76;-1944.255,930.457;Inherit;False;77;Mask;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;24;-1837.82,674.2466;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;22;-1545.329,178.2759;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;85;-261.4237,588.2568;Inherit;False;Property;_Dst;Mode;1;1;[Enum];Create;False;0;2;AlphaBlend;10;Additive;1;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;86;-268.8312,718.4155;Inherit;False;Property;_CullMode;CullMode;0;1;[Enum];Create;False;0;2;AlphaBlend;10;Additive;1;1;UnityEngine.Rendering.CullMode;True;0;False;2;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;18;-852.3341,234.053;Inherit;False;3;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-833.883,498.5121;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-298.6754,163.1653;Half;False;True;-1;2;ASEMaterialInspector;0;0;CustomLighting;Effect/test;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;2;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;Transparent;;Transparent;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;True;85;0;1;False;-1;1;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;3;-1;-1;-1;0;False;0;0;True;86;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;28;0;45;0
WireConnection;28;1;44;0
WireConnection;30;0;27;0
WireConnection;30;1;28;0
WireConnection;31;0;30;0
WireConnection;31;1;29;0
WireConnection;6;0;43;0
WireConnection;6;1;41;0
WireConnection;33;1;31;0
WireConnection;7;0;5;0
WireConnection;7;1;6;0
WireConnection;58;0;56;0
WireConnection;58;1;57;0
WireConnection;65;0;63;0
WireConnection;65;1;64;0
WireConnection;9;0;7;0
WireConnection;9;1;8;0
WireConnection;61;0;59;0
WireConnection;61;1;58;0
WireConnection;46;0;33;0
WireConnection;68;0;66;0
WireConnection;68;1;65;0
WireConnection;2;0;9;0
WireConnection;34;0;46;0
WireConnection;69;0;68;0
WireConnection;69;1;67;0
WireConnection;62;0;61;0
WireConnection;62;1;60;0
WireConnection;70;1;62;0
WireConnection;71;1;69;0
WireConnection;80;0;71;0
WireConnection;79;0;70;0
WireConnection;38;0;37;0
WireConnection;38;1;36;0
WireConnection;52;0;38;0
WireConnection;52;1;48;0
WireConnection;73;0;80;0
WireConnection;72;0;79;0
WireConnection;75;0;72;0
WireConnection;75;1;73;0
WireConnection;54;0;55;0
WireConnection;54;1;52;0
WireConnection;74;1;72;0
WireConnection;74;0;75;0
WireConnection;74;2;73;0
WireConnection;53;1;55;0
WireConnection;53;0;54;0
WireConnection;77;0;74;0
WireConnection;14;1;53;0
WireConnection;23;0;17;0
WireConnection;24;0;21;0
WireConnection;22;0;14;0
WireConnection;18;0;22;0
WireConnection;18;1;23;0
WireConnection;18;2;24;0
WireConnection;19;0;14;4
WireConnection;19;1;17;4
WireConnection;19;2;21;4
WireConnection;19;3;76;0
WireConnection;0;2;18;0
WireConnection;0;9;19;0
ASEEND*/
//CHKSM=BECE81B58225764E368F46318CF42CBFA05A7882