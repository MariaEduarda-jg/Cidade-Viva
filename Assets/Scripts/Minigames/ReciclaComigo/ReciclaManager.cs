using UnityEngine;
using UnityEngine.UI;

// Faz surgir os itens de lixo um de cada vez e finaliza o "Recicla Comigo!" após 10 descartes certos.
public class ReciclaManager : MinigameBase
{
    public ReciclaConfig config;
    public ItemLixo modelo;
    public RectTransform areaDosItens;
    public Lixeira[] lixeiras;
    public Text textoProgresso;

    private int descartados;
    private int acertosDePrimeira;
    private int ultimoIndice = -1;

    private void Start()
    {
        modelo.gameObject.SetActive(false);
        foreach (Lixeira lixeira in lixeiras)
        {
            LixeiraConfig aparencia = config.BuscarLixeira(lixeira.tipoAceito);
            if (aparencia != null)
            {
                lixeira.Aplicar(aparencia);
            }
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

        if (descartados >= config.totalDeItens)
        {
            Finalizar();
            return;
        }

        Invoke(nameof(CriarProximoItem), config.intervaloEntreItens);
    }

    private void CriarProximoItem()
    {
        // Evita repetir o mesmo item duas vezes seguidas.
        int indice = Random.Range(0, config.itens.Count);
        if (config.itens.Count > 1 && indice == ultimoIndice)
        {
            indice = (indice + 1) % config.itens.Count;
        }

        ultimoIndice = indice;

        TipoDeLixo dados = config.itens[indice];
        ItemLixo item = Instantiate(modelo, areaDosItens);
        item.name = "Item_" + dados.nome;
        item.gameObject.SetActive(true);
        item.Configurar(dados.nome, dados.tipo, dados.cor, new Vector2(Random.Range(-config.xMaximo, config.xMaximo), config.ySurgimento));
    }

    private void AtualizarProgresso()
    {
        if (textoProgresso != null)
        {
            textoProgresso.text = string.Format(config.formatoProgresso, descartados, config.totalDeItens);
        }
    }

    private void Finalizar()
    {
        int pontuacao = Mathf.RoundToInt(100f * acertosDePrimeira / config.totalDeItens);
        FinalizarJogo(pontuacao, config.tituloDoFim);
    }
}
