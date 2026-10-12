// Veículo em montagem na Oficina: o modelo aparece cinza ("molde") e ganha a cor de verdade só
// em volta dos pontos onde a criança já encaixou uma peça. Os pontos são esferas no mundo,
// passadas pelo ChassiDaOficina em _MoldeEsferas (xyz = centro, w = raio).
// Iluminação simples (sol + luz ambiente), suficiente pra bancada.
Shader "CidadeViva/Molde"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        _BaseColor ("Cor", Color) = (1, 1, 1, 1)
        _CorDoMolde ("Cor do molde", Color) = (0.79, 0.82, 0.85, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _CorDoMolde;
            CBUFFER_END

            float4 _MoldeEsferas[12];
            float _MoldeQuantidade;

            struct Entrada
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Saida
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Saida vert(Entrada i)
            {
                Saida o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                return o;
            }

            half4 frag(Saida i) : SV_Target
            {
                half3 cor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;

                // 1 dentro de alguma esfera (peça já encaixada), 0 fora, com a borda suave.
                float revelado = 0.0;
                for (int k = 0; k < 12; k++)
                {
                    float d = distance(i.positionWS, _MoldeEsferas[k].xyz);
                    float dentro = 1.0 - smoothstep(_MoldeEsferas[k].w * 0.82, _MoldeEsferas[k].w, d);
                    revelado = max(revelado, dentro * step(k + 0.5, _MoldeQuantidade));
                }

                half3 base = lerp(_CorDoMolde.rgb, cor, revelado);
                float3 normal = normalize(i.normalWS);
                Light sol = GetMainLight();
                half3 luz = sol.color * saturate(dot(normal, sol.direction)) * 0.85 + SampleSH(normal);
                return half4(base * luz, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
