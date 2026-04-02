Shader "Custom/WaterWave Effect" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
 
	CGINCLUDE
	#include "UnityCG.cginc"
	uniform sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	uniform float _distanceFactor;
	uniform float _timeFactor;
	uniform float _duration;
	uniform float _currentTime;
	uniform float _totalFactor;
	uniform float _waveWidth;
	uniform float _curWaveDis;
	uniform float4 _startPos;
 
	fixed4 frag(v2f_img i) : SV_Target
	{
		//DX下纹理坐标反向问题
		#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			_startPos.y = 1 - _startPos.y;
		#endif

		//计算uv到中间点的向量(向外扩，反过来就是向里缩)
		float2 vec = _startPos.xy - i.uv;
		//根据屏幕长宽调整波纹为正圆形
		vec *= float2(_ScreenParams.x / _ScreenParams.y, 1);
		//归一化
		float2 normalizedVec = normalize(vec);
		//vec向量的模长
		float magnitudeVec = sqrt(vec.x * vec.x + vec.y * vec.y);

		//根据长度区分在sin中的值（相当于区分出波纹的起伏）来决定偏移系数
		//dis在这里都是小于1的，所以我们需要乘以一个比较大的数，比如60，这样就有多个波峰波谷
		//sin函数是（-1，1）的值域，我们希望偏移值很小，所以这里我们缩小100倍
		float sinFactor = sin(magnitudeVec * _distanceFactor + _currentTime * _timeFactor) * _totalFactor * 0.01;

		//计算波纹移动了多少距离，如果超过了波纹最大半径，渐渐淡出
		float fadeFactor = clamp(_waveWidth - abs(_curWaveDis - magnitudeVec), 0, 1);

		//计算每个像素uv的偏移值
		float2 offset = normalizedVec  * sinFactor * fadeFactor * clamp(_duration - _currentTime, 0, _duration) / _duration;

		//像素采样时偏移offset
		float2 uv = offset + i.uv;
		return tex2D(_MainTex, uv);	
	}
 
	ENDCG
 
	SubShader 
	{
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off
			Fog { Mode off }
 
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest 
			ENDCG
		}
	}
	Fallback off
}
