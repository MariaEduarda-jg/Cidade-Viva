using System.Collections;
using UnityEngine;

// Uma estrutura construída num problema (ponte, túnel, viaduto...). O modelo fica inativo na cena e o
// ObrasManager copia. "Pecas" sobem do chão uma depois da outra; "caminho" são os pontos por onde
// quem passa atravessa, de um lado ao outro.
public class ObraEstrutura : MonoBehaviour
{
    public string idDoVerbete;
    public Transform pecas;
    public Transform[] caminho = new Transform[0];
    [Tooltip("O caminho já vai até onde quem passa termina (retorno, estacionamento, calçada...). Desligado, ele ainda segue até o outro lado do lote.")]
    public bool caminhoCompleto;
    [Tooltip("Estacionamento: vagas dos outros veículos que esperam junto.")]
    public Transform[] vagas = new Transform[0];
    [Tooltip("De quão fundo as peças sobem, em metros.")]
    public float profundidade = 6f;

    private Vector3[] lugares;

    private void Guardar()
    {
        if (lugares != null)
        {
            return;
        }
        lugares = new Vector3[pecas.childCount];
        for (int i = 0; i < lugares.Length; i++)
        {
            lugares[i] = pecas.GetChild(i).localPosition;
        }
    }

    // As peças sobem em sequência e assentam com um pequeno "quique".
    public IEnumerator Aparecer(float duracao)
    {
        Guardar();
        int n = lugares.Length;
        for (int i = 0; i < n; i++)
        {
            pecas.GetChild(i).localPosition = lugares[i] + Vector3.down * profundidade;
        }

        float cadaUma = duracao * 0.5f;
        float atraso = n > 1 ? (duracao - cadaUma) / (n - 1) : 0f;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            for (int i = 0; i < n; i++)
            {
                float k = Mathf.Clamp01((t - i * atraso) / cadaUma);
                // Passa um pouco do lugar e volta.
                float curva = 1f - Mathf.Pow(1f - k, 3f) + Mathf.Sin(k * Mathf.PI) * 0.06f;
                pecas.GetChild(i).localPosition = lugares[i] + Vector3.down * profundidade * (1f - curva);
            }
            yield return null;
        }
        for (int i = 0; i < n; i++)
        {
            pecas.GetChild(i).localPosition = lugares[i];
        }
    }

    public IEnumerator Sumir(float duracao)
    {
        Guardar();
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = t / duracao;
            for (int i = 0; i < lugares.Length; i++)
            {
                pecas.GetChild(i).localPosition = lugares[i] + Vector3.down * profundidade * k * k;
            }
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
