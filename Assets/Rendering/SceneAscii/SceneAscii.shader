Shader "Hidden/Extraction/SceneAscii"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "Scene ASCII"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _Cell; // cell width, height, background brightness, intensity
            float4 _Tone; // exposure multiplier, contrast, adaptive ink
            float4 _Ink;
            float4 _SourceSize;
            float4 _QuestLens; // pointer viewport xy, radius in screen-height units, tool: 1 loupe / 2 UV / 3 reagent

            half4 Scene(float2 uv)
            {
                float2 inset = _SourceSize.zw * 0.5;
                uv = clamp(uv, inset, 1.0 - inset);
                uv = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
            }

            // A 5x7 bitmap font in 6x10 cells, sorted by actual ink coverage.
            float text_glyph(float2 uv, float value)
            {
                float2 pixel = floor(float2(uv.x * 6.0, (1.0 - uv.y) * 10.0)) - float2(0, 1);
                if (pixel.x < 0 || pixel.x >= 5 || pixel.y < 0 || pixel.y >= 7)
                    return 0;
                int index = (int)min(floor(saturate(value) * 10.0), 9.0);
                float2 packed = 0;
                if (index == 0) packed = float2(0.0, 0.0); //
                else if (index == 1) packed = float2(0.0, 4096.0); // .
                else if (index == 2) packed = float2(128.0, 128.0); // :
                else if (index == 3) packed = float2(0.0, 31.0); // -
                else if (index == 4) packed = float2(4224.0, 4255.0); // +
                else if (index == 5) packed = float2(31744.0, 992.0); // =
                else if (index == 6) packed = float2(8563.0, 845892.0); // %
                else if (index == 7) packed = float2(15008.0, 21983.0); // *
                else if (index == 8) packed = float2(32074.0, 338922.0); // #
                else if (index == 9) packed = float2(30254.0, 985013.0); // @
                float row = pixel.y < 3 ? pixel.y : pixel.y - 3;
                float bits = pixel.y < 3 ? packed.x : packed.y;
                return fmod(floor(bits / exp2(row * 5.0 + pixel.x)), 2.0);
            }


            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = (input.texcoord - _BlitScaleBias.zw) / _BlitScaleBias.xy;
                float2 lensCenter=_QuestLens.xy;
#if UNITY_UV_STARTS_AT_TOP
                lensCenter.y=1-lensCenter.y;
#endif
                float aspect=_SourceSize.x/max(1,_SourceSize.y);
                float lensDistance=length((uv-lensCenter)*float2(aspect,1));
                float lensOn=step(.5,_QuestLens.w)*(1-smoothstep(_QuestLens.z-.008,_QuestLens.z,lensDistance));
                float loupe=lensOn*(1-step(1.5,_QuestLens.w));
                float ultraviolet=lensOn*step(1.5,_QuestLens.w)*(1-step(2.5,_QuestLens.w));
                float reagent=lensOn*step(2.5,_QuestLens.w);
                // Magnification and finer ASCII cells are local to the moving lens.
                float2 sampleUV=lerp(uv,lensCenter+(uv-lensCenter)*.45,loupe);
                float2 grid = _SourceSize.xy / lerp(_Cell.xy,_Cell.xy*.55,loupe);
                float2 position = sampleUV * grid;
                float2 center = (floor(position) + 0.5) / grid;
                float2 offset = 0.3 / grid;
                half4 scene = (Scene(center) + Scene(center + offset) + Scene(center - offset)
                    + Scene(center + float2(offset.x, -offset.y))
                    + Scene(center + float2(-offset.x, offset.y))) * 0.2;

                float3 densityColor = max(scene.rgb * _Tone.x, 0);
#if !defined(UNITY_COLORSPACE_GAMMA)
                densityColor = LinearToSRGB(densityColor);
#endif
                float brightness = dot(densityColor, float3(0.299, 0.587, 0.114));
                brightness = saturate((brightness - 0.5) * _Tone.y + 0.5);
                float2 glyphUV = frac(position);
                // Bitmap rows are stored top to bottom, independently of graphics API UV origin.
#if UNITY_UV_STARTS_AT_TOP
                glyphUV.y = 1.0 - glyphUV.y;
#endif
                brightness=saturate(brightness+ultraviolet*.20+reagent*.08);
                float mask = text_glyph(glyphUV, brightness);
                half3 background = scene.rgb * _Cell.z;
                float backgroundLuma = dot(background, float3(0.299, 0.587, 0.114));
                float inkLuma = dot(_Ink.rgb, float3(0.299, 0.587, 0.114));
                // Choose whichever of the requested ink and near-black offers more luminance contrast.
                half3 ink = _Ink.rgb;
                if (_Tone.z > 0.5 && abs(backgroundLuma - 0.01) > abs(backgroundLuma - inkLuma))
                    ink = half3(0.01, 0.01, 0.01);
                ink=lerp(ink,half3(.65,.35,1),ultraviolet);
                ink=lerp(ink,half3(1,.73,.2),reagent);
                half4 result = half4(lerp(background, ink, mask), scene.a);
                if (_Cell.w < 1.0)
                    result = lerp(Scene(uv), result, _Cell.w);
                float rim=step(.5,_QuestLens.w)*(1-smoothstep(.001,.004,abs(lensDistance-_QuestLens.z)));
                result.rgb=lerp(result.rgb,_QuestLens.w<1.5?half3(.9,.8,.4):_QuestLens.w<2.5?half3(.65,.3,1):half3(1,.65,.2),rim);
                return result;
            }
            ENDHLSL
        }
    }
}
