Shader "SPACR/ColorGrading"
{

	 Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
		_RedIn ("red in", Vector) = (0,1,255)
		_RedOut ("red out", Vector) = (0,1,255)
		_GreenIn ("green in", Vector) = (0,1,255)
		_GreenOut ("green out", Vector) = (0,1,255)
		_BlueIn ("blue in", Vector) = (0,1,255)
		_BlueOut ("blue out", Vector) = (0,1,255)
    }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
					
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
	
			uniform sampler2D _MainTex;
			uniform float3 _RedIn;
			uniform float3 _RedOut;
			uniform float3 _GreenIn;
			uniform float3 _GreenOut;
        	uniform float3 _BlueIn;
			uniform float3 _BlueOut;
	
			float level(				
				float value, // 0-1
				float low_input, float high_input, // 0 - 1
				float gamma,
				float low_output, float high_output // 0 - 1
			)
			{
				float inv_gamma = 1 / gamma;
				if (high_input != low_input)
					value = (value - low_input) / (high_input - low_input);
				else
					value = (value - low_input);

				if (inv_gamma != 1.0 && value > 0)
					value =  pow (value, inv_gamma);

				//  determine the output intensity  
				if (high_output >= low_output)
					value = value * (high_output - low_output) + low_output;
				else if (high_output < low_output)
					value = low_output - value * (low_output - high_output);

				return value;

				// https://github.com/GNOME/gimp/blob/master/app/operations/gimpoperationlevels.c
				/*
					//  determine input intensity
					if (high_input != low_input)
						value = (value - low_input) / (high_input - low_input);
					else
						value = (value - low_input);

					if (inv_gamma != 1.0 && value > 0)
						value =  pow (value, inv_gamma);

					//  determine the output intensity  
					if (high_output >= low_output)
						value = value * (high_output - low_output) + low_output;
					else if (high_output < low_output)
						value = low_output - value * (low_output - high_output);

					return value;
				*/
			}

			fixed4 frag (v2f_img i) : SV_Target
			{
				float4 dst = tex2D(_MainTex, i.uv);
				dst.r = level(
					dst.r,
					_RedIn[0],_RedIn[2],_RedIn[1],_RedOut[0],_RedOut[2]
				);
				dst.g = level(
					dst.g,
					_GreenIn[0],_GreenIn[2],_GreenIn[1],_GreenOut[0],_GreenOut[2]
				);
				dst.b = level(
					dst.b,
					_BlueIn[0],_BlueIn[2],_BlueIn[1],_BlueOut[0],_BlueOut[2]
				);

				return dst;

			}
			ENDCG

        }
    }
} // shader
