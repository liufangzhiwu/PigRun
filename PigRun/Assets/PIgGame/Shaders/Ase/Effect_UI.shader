// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Effect/UI"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		_Maintexture2("主纹理贴图", 2D) = "white" {}
		[KeywordEnum(R,G,B,A,RGBA)] _Keyword0("主纹理通道", Float) = 0
		_wenli_v1("纹理U", Float) = 0
		_wenli_u1("纹理V", Float) = 1
		_int("主纹理强度", Float) = 0
		_noisetexture("扰动贴图", 2D) = "white" {}
		_noise_v("扰动U", Float) = 0
		_noise_u("扰动V", Float) = 1
		_noise_int("扰动强度", Float) = 0
		[HDR]_MaskColor("流光颜色", Color) = (1,1,1,1)
		_Mask("遮罩贴图", 2D) = "black" {}
		_Add_u("遮罩U", Float) = -1
		_Add_v("遮罩V", Float) = -1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

	}

	SubShader
	{
		LOD 0

		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
		
		Stencil
		{
			Ref [_Stencil]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
			CompFront [_StencilComp]
			PassFront [_StencilOp]
			FailFront Keep
			ZFailFront Keep
			CompBack Always
			PassBack Keep
			FailBack Keep
			ZFailBack Keep
		}


		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		
		Pass
		{
			Name "Default"
		CGPROGRAM
			
			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile __ UNITY_UI_CLIP_RECT
			#pragma multi_compile __ UNITY_UI_ALPHACLIP
			
			#include "UnityShaderVariables.cginc"
			#define ASE_NEEDS_FRAG_COLOR
			#pragma shader_feature_local _KEYWORD0_R _KEYWORD0_G _KEYWORD0_B _KEYWORD0_A _KEYWORD0_RGBA

			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				
			};
			
			uniform fixed4 _Color;
			uniform fixed4 _TextureSampleAdd;
			uniform float4 _ClipRect;
			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform sampler2D _Mask;
			uniform float _Add_u;
			uniform float _Add_v;
			uniform float4 _Mask_ST;
			uniform sampler2D _noisetexture;
			uniform float _noise_v;
			uniform float _noise_u;
			uniform float4 _noisetexture_ST;
			uniform float _noise_int;
			uniform sampler2D _Maintexture2;
			uniform float _wenli_v1;
			uniform float _wenli_u1;
			uniform float4 _Maintexture2_ST;
			uniform float _int;
			uniform float4 _MaskColor;

			
			v2f vert( appdata_t IN  )
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID( IN );
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
				OUT.worldPosition = IN.vertex;
				
				
				OUT.worldPosition.xyz +=  float3( 0, 0, 0 ) ;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;
				
				OUT.color = IN.color * _Color;
				return OUT;
			}

			fixed4 frag(v2f IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				float2 uv_MainTex = IN.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 tex2DNode2 = tex2D( _MainTex, uv_MainTex );
				float2 appendResult16 = (float2(_Add_u , _Add_v));
				float2 uv_Mask = IN.texcoord.xy * _Mask_ST.xy + _Mask_ST.zw;
				float2 appendResult6 = (float2(_noise_v , _noise_u));
				float2 uv_noisetexture = IN.texcoord.xy * _noisetexture_ST.xy + _noisetexture_ST.zw;
				float4 lerpResult24 = lerp( float4( ( ( _Time.y * appendResult16 ) + uv_Mask ), 0.0 , 0.0 ) , tex2D( _noisetexture, ( ( _Time.y * appendResult6 ) + uv_noisetexture ) ) , _noise_int);
				float4 tex2DNode11 = tex2D( _Mask, lerpResult24.rg );
				float2 appendResult38 = (float2(_wenli_v1 , _wenli_u1));
				float2 uv_Maintexture2 = IN.texcoord.xy * _Maintexture2_ST.xy + _Maintexture2_ST.zw;
				float4 tex2DNode29 = tex2D( _Maintexture2, ( ( _Time.y * appendResult38 ) + uv_Maintexture2 ) );
				float4 temp_cast_2 = (tex2DNode29.r).xxxx;
				float4 temp_cast_3 = (tex2DNode29.r).xxxx;
				float4 temp_cast_4 = (tex2DNode29.g).xxxx;
				float4 temp_cast_5 = (tex2DNode29.b).xxxx;
				float4 temp_cast_6 = (tex2DNode29.a).xxxx;
				#if defined(_KEYWORD0_R)
				float4 staticSwitch34 = temp_cast_2;
				#elif defined(_KEYWORD0_G)
				float4 staticSwitch34 = temp_cast_4;
				#elif defined(_KEYWORD0_B)
				float4 staticSwitch34 = temp_cast_5;
				#elif defined(_KEYWORD0_A)
				float4 staticSwitch34 = temp_cast_6;
				#elif defined(_KEYWORD0_RGBA)
				float4 staticSwitch34 = tex2DNode29;
				#else
				float4 staticSwitch34 = temp_cast_2;
				#endif
				
				half4 color = ( ( tex2DNode2 + ( tex2DNode2.a * ( tex2DNode11.r + ( tex2DNode11.r * ( staticSwitch34 * _int ) ) ) * _MaskColor ) ) * IN.color );
				
				#ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
				
				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif

				return color;
			}
		ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=18912
7;201;1317;683;2715.738;296.9556;2.136939;True;False
Node;AmplifyShaderEditor.RangedFloatNode;36;-2304.872,1055.842;Inherit;False;Property;_wenli_v1;纹理U;2;0;Create;False;0;0;0;False;0;False;0;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;35;-2302.851,1149.157;Inherit;False;Property;_wenli_u1;纹理V;3;0;Create;False;0;0;0;False;0;False;1;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-2380.933,577.2322;Inherit;False;Property;_noise_v;扰动U;6;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-2378.912,670.5474;Inherit;False;Property;_noise_u;扰动V;7;0;Create;False;0;0;0;False;0;False;1;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;7;-2141.346,534.7425;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;37;-2065.284,1013.352;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;6;-2199.198,625.6939;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;14;-1842.8,207.2854;Inherit;False;Property;_Add_u;遮罩U;11;0;Create;False;0;0;0;False;0;False;-1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;15;-1833.322,298.0715;Inherit;False;Property;_Add_v;遮罩V;12;0;Create;False;0;0;0;False;0;False;-1;-1.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;38;-2123.136,1104.303;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;20;-1623.901,134.6088;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;-1881.51,1084.412;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-1957.572,605.8024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;10;-2067.836,779.7764;Inherit;False;0;3;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;39;-1991.774,1258.386;Inherit;False;0;29;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;16;-1626.351,247.3243;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;41;-1643.826,1128.518;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;9;-1719.888,649.9086;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-1440.126,205.6687;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;19;-1550.391,379.6426;Inherit;False;0;11;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;29;-1338.849,851.7195;Inherit;True;Property;_Maintexture2;主纹理贴图;0;0;Create;False;0;0;0;False;0;False;-1;None;ba73897c8c7c77141821527da2e439a2;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;18;-1202.442,249.7748;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-1242.556,722.3972;Inherit;False;Property;_noise_int;扰动强度;8;0;Create;False;0;0;0;False;0;False;0;0.3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;3;-1571.948,623.9295;Inherit;True;Property;_noisetexture;扰动贴图;5;0;Create;False;0;0;0;False;0;False;-1;None;5c1413f80760c3a429d62f6d2e482704;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;24;-956.2123,488.3927;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;32;-873.8856,1130.698;Inherit;False;Property;_int;主纹理强度;4;0;Create;False;0;0;0;False;0;False;0;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;34;-943.1371,867.5477;Inherit;False;Property;_Keyword0;主纹理通道;1;0;Create;False;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;5;R;G;B;A;RGBA;Create;True;False;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;-676.0286,796.3189;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;11;-825.5942,459.4078;Inherit;True;Property;_Mask;遮罩贴图;10;0;Create;False;0;0;0;False;0;False;-1;None;3bf18040f389a4a4791855a6d900d36a;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-497.9568,675.6265;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateShaderPropertyNode;1;-912.0711,229.9409;Inherit;False;0;0;_MainTex;Shader;False;0;5;SAMPLER2D;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;12;-454.9638,782.0767;Inherit;False;Property;_MaskColor;流光颜色;9;1;[HDR];Create;False;0;0;0;False;0;False;1,1,1,1;0.3992402,0.5799439,2.418255,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-690.2989,224.784;Inherit;True;Property;_MainTexture;MainTexture;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;31;-428.7076,519.3191;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-245.8364,460.8809;Inherit;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;25;-98.01288,597.36;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;23;-84.2382,227.7108;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;234.4189,225.7702;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;464.9793,241.0887;Float;False;True;-1;2;ASEMaterialInspector;0;6;Effect/UI;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;2;5;False;-1;10;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;True;True;True;True;True;0;True;-9;False;False;False;False;False;False;False;True;True;0;True;-5;255;True;-8;255;True;-7;0;True;-4;0;True;-6;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;2;False;-1;True;0;True;-11;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;6;0;22;0
WireConnection;6;1;5;0
WireConnection;38;0;36;0
WireConnection;38;1;35;0
WireConnection;40;0;37;0
WireConnection;40;1;38;0
WireConnection;8;0;7;0
WireConnection;8;1;6;0
WireConnection;16;0;14;0
WireConnection;16;1;15;0
WireConnection;41;0;40;0
WireConnection;41;1;39;0
WireConnection;9;0;8;0
WireConnection;9;1;10;0
WireConnection;17;0;20;0
WireConnection;17;1;16;0
WireConnection;29;1;41;0
WireConnection;18;0;17;0
WireConnection;18;1;19;0
WireConnection;3;1;9;0
WireConnection;24;0;18;0
WireConnection;24;1;3;0
WireConnection;24;2;4;0
WireConnection;34;1;29;1
WireConnection;34;0;29;2
WireConnection;34;2;29;3
WireConnection;34;3;29;4
WireConnection;34;4;29;0
WireConnection;33;0;34;0
WireConnection;33;1;32;0
WireConnection;11;1;24;0
WireConnection;30;0;11;1
WireConnection;30;1;33;0
WireConnection;2;0;1;0
WireConnection;31;0;11;1
WireConnection;31;1;30;0
WireConnection;13;0;2;4
WireConnection;13;1;31;0
WireConnection;13;2;12;0
WireConnection;23;0;2;0
WireConnection;23;1;13;0
WireConnection;26;0;23;0
WireConnection;26;1;25;0
WireConnection;0;0;26;0
ASEEND*/
//CHKSM=C741232D1595B09D4F6A4A333F2838D78ECEA964