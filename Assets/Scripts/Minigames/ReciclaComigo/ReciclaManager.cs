using UnityEngine;
using UnityEngine.UI;

// Faz surgir os itens de lixo um de cada vez e finaliza o "Recicla Comigo!" após 10 descartes certos.
public class ReciclaManager : MonoBehaviour
{
    [System.Serializable]
    public class TipoDeLixo
    {
        public string nome;
        public string tipo;
        public Color cor = Color.white;
    }

    public TipoDeLixo[] itens;
    public ItemLixo modelo;
    public RectTransform areaDosItens;
    public int totalDeItens = 10;
    public float xMaximo = 650f;
    public float ySurgimento = 280f;
    public float intervaloEntreItens = 0.4f;
    public Text textoProgresso;
    public Text textoFinal;

    private int descartados;
    private int acertosDePrimeira;
    private int ultimoIndice = -1;

    private void Start()
    {
        modelo.gameObject.SetActive(false);
        if (textoFinal != null)
        {
            textoFinal.gameObject.SetActive(false);
        }

        AtualizarProgresso();
        CriarProximoItem();
    }

    public void ItemDescartado(ItemLixo item)
    {
        descartados++;
        if (!item.errou)
        {
            acertosDePrimeira++;
        }

        AtualizarProgresso();

        if (descartados >= totalDeItens)
        {
            Finalizar();
            return;
        }

        Invoke(nameof(CriarProximoItem), intervaloEntreItens);
    }

    private void CriarProximoItem()
    {
        // Evita repetir o mesmo item duas vezes seguidas.
        int indice = Random.Range(0, itens.Length);
        if (itens.Length > 1 && indice == ultimoIndice)
        {
            indice = (indice + 1) % itens.Length;
        }

        ultimoIndice = indice;

        TipoDeLixo dados = itens[indice];
        ItemLixo item = Instantiate(modelo, areaDosItens);
        item.name = "Item_" + dados.nome;
        item.gameObject.SetActive(true);
        item.Configurar(dados.nome, dados.tipo, dados.cor, new Vector2(Random.Range(-xMaximo, xMaximo), ySurgimento));
    }

    private void AtualizarProgresso()
    {
        if (textoProgresso != null)
        {
            textoProgresso.text = "Lixo separado: " + descartados + "/" + totalDeItens;
        }
    }

    private void Finalizar()
    {
        int pontuacao = Mathf.RoundToInt(100f * acertosDePrimeira / totalDeItens);
        GameProgress.FinalizarMinigame("Recicla Comigo!", pontuacao);

        if (textoFinal != null)
        {
            textoFinal.text = "Cidade mais limpa! Pontuação: " + pontuacao;
            textoFinal.gameObject.SetActive(true);
        }
    }
}
