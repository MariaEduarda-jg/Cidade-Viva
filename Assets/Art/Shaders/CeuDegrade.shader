// Céu em degradê: cor do topo, cor do horizonte e uma faixa de "chão" abaixo dele.
// Usado como skybox pelo prefab Ambiente_Padrao.
Shader "CidadeViva/CeuDegrade"
{
    Properties
    {
        _CorTopo ("Topo", Color) = (0.50, 0.71, 0.86, 1)
        _CorHorizonte ("Horizonte", Color) = (1.00, 0.84, 0.70, 1)
        _CorChao ("Abaixo do horizonte", Color) = (0.80, 0.72, 0.62, 1)
        _Expoente ("Altura do degradê", Range(0.2, 4)) = 0.75
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _CorTopo;
            fixed4 _CorHorizonte;
            fixed4 _CorChao;
            float _Expoente;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float y = normalize(i.dir).y;
                fixed4 cor = lerp(_CorHorizonte, _CorTopo, pow(saturate(y), _Expoente));
                return lerp(cor, _CorChao, saturate(-y * 4.0));
            }
            ENDCG
        }
    }
    Fallback Off
}
