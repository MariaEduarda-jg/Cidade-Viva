using UnityEngine;

// Deixa o visual de uma área do Hub cinza ou colorido sem trocar objetos nem materiais:
// muda a cor (e, onde há textura, troca pela versão cinza) por MaterialPropertyBlock.
public class SaturacaoDaArea : MonoBehaviour
{
    [System.Serializable]
    public struct ParDeTextura
    {
        public Texture colorida;
        public Texture cinza;
    }

    [Tooltip("Raiz dos objetos que ficam cinza. Vazio = este objeto.")]
    public Transform raiz;

    [Range(0f, 1f)]
    [Tooltip("0 = cinza, 1 = cor cheia. Usado enquanto o minigame não foi completado.")]
    public float saturacaoSemCompletar = 0f;

    public ParDeTextura[] texturasCinza;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");

    public void Aplicar(float saturacao)
    {
        Transform alvo = raiz != null ? raiz : transform;
        var bloco = new MaterialPropertyBlock();
        foreach (Renderer rend in alvo.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materiais = rend.sharedMaterials;
            for (int i = 0; i < materiais.Length; i++)
            {
                Material m = materiais[i];
                if (m == null)
                {
                    continue;
                }

                bloco.Clear();
                if (saturacao < 1f)
                {
                    if (m.HasProperty(BaseColor))
                    {
                        bloco.SetColor(BaseColor, Dessaturar(m.GetColor(BaseColor), saturacao));
                    }
                    if (m.HasProperty(EmissionColor) && m.IsKeywordEnabled("_EMISSION"))
                    {
                        // Luz "apagada": além de cinza, bem mais fraca.
                        bloco.SetColor(EmissionColor, Dessaturar(m.GetColor(EmissionColor), saturacao) * Mathf.Lerp(0.3f, 1f, saturacao));
                    }
                    if (saturacao < 0.5f)
                    {
                        TrocarTextura(m, BaseMap, bloco);
                        TrocarTextura(m, EmissionMap, bloco);
                    }
                }
                rend.SetPropertyBlock(bloco, i);
            }
        }
    }

    private void TrocarTextura(Material m, int propriedade, MaterialPropertyBlock bloco)
    {
        if (!m.HasProperty(propriedade) || texturasCinza == null)
        {
            return;
        }

        Texture atual = m.GetTexture(propriedade);
        if (atual == null)
        {
            return;
        }

        foreach (ParDeTextura par in texturasCinza)
        {
            if (par.colorida == atual && par.cinza != null)
            {
                bloco.SetTexture(propriedade, par.cinza);
                return;
            }
        }
    }

    // Cinza um pouco "lavado" (puxado pro claro), pra área parecer apagada e não suja.
    public static Color Dessaturar(Color cor, float saturacao)
    {
        float cinza = Mathf.Lerp(cor.grayscale, 0.62f, 0.35f);
        return Color.Lerp(new Color(cinza, cinza, cinza, cor.a), cor, saturacao);
    }
}
