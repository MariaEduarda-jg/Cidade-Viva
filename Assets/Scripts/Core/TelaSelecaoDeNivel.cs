using System;
using UnityEngine;
using UnityEngine.UI;

// Tela de seleção de níveis (prefab Assets/Prefabs/UI_SelecaoDeNivel), igual em todos os minigames.
// Aparece ao entrar no minigame: um botão grande por nível, com as estrelas já ganhas ou um cadeado.
// Quem abre é o MinigameBase; os botões do prefab chamam Escolher(nivel).
public class TelaSelecaoDeNivel : MonoBehaviour
{
    [Serializable]
    public class BotaoDeNivel
    {
        public Button botao;
        public Image[] estrelas;
        public GameObject cadeado;
        // Número e estrelas: somem quando o nível está trancado.
        public GameObject conteudo;
    }

    public Text titulo;
    // Dica do próximo nível a jogar (o "objetivo" do NivelConfig).
    public Text dica;
    public BotaoDeNivel[] botoes;

    [Header("Cores das estrelas")]
    public Color estrelaGanha = Color.white;
    public Color estrelaApagada = new Color(0.35f, 0.4f, 0.45f, 0.3f);

    private MinigameBase minigame;

    public void Mostrar(MinigameBase dono)
    {
        minigame = dono;
        if (titulo != null)
        {
            titulo.text = dono.nomeDaArea;
        }

        int proximo = 1;
        for (int i = 0; i < botoes.Length; i++)
        {
            int nivel = i + 1;
            BotaoDeNivel b = botoes[i];
            bool existe = nivel <= dono.niveis.Quantidade;
            b.botao.gameObject.SetActive(existe);
            if (!existe)
            {
                continue;
            }

            bool liberado = GameProgress.NivelDesbloqueado(dono.nomeDaArea, nivel);
            int ganhas = GameProgress.ObterEstrelasDoNivel(dono.nomeDaArea, nivel);
            b.botao.interactable = liberado;
            b.cadeado.SetActive(!liberado);
            b.conteudo.SetActive(liberado);
            for (int e = 0; e < b.estrelas.Length; e++)
            {
                b.estrelas[e].color = e < ganhas ? estrelaGanha : estrelaApagada;
            }
            if (liberado)
            {
                proximo = nivel;
            }
        }

        if (dica != null)
        {
            dica.text = dono.niveis.Obter(proximo).objetivo;
        }
        Centralizar();
        AbrirEspacoParaAbas();
        gameObject.SetActive(true);
    }

    private bool espacoAberto;
    private float[] xOriginais;

    // Com menos de 5 níveis (Pedalada, Motorista Mirim), os botões que existem ficam no meio da tela.
    private void Centralizar()
    {
        if (botoes.Length < 2)
        {
            return;
        }
        if (xOriginais == null)
        {
            xOriginais = new float[botoes.Length];
            for (int i = 0; i < botoes.Length; i++)
            {
                xOriginais[i] = ((RectTransform)botoes[i].botao.transform).anchoredPosition.x;
            }
        }
        float passo = xOriginais[1] - xOriginais[0];
        float meio = (xOriginais[0] + xOriginais[botoes.Length - 1]) / 2f;
        var visiveis = new System.Collections.Generic.List<RectTransform>();
        foreach (BotaoDeNivel b in botoes)
        {
            if (b.botao.gameObject.activeSelf) visiveis.Add((RectTransform)b.botao.transform);
        }
        for (int k = 0; k < visiveis.Count; k++)
        {
            Vector2 p = visiveis[k].anchoredPosition;
            p.x = meio + (k - (visiveis.Count - 1) / 2f) * passo;
            visiveis[k].anchoredPosition = p;
        }
    }

    // Nas cenas com dois modos, as abas ficam no alto da tela: o título e o subtítulo descem pra não ficar embaixo delas.
    private void AbrirEspacoParaAbas()
    {
        if (espacoAberto || titulo == null)
        {
            return;
        }
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }
        bool temAbas = false;
        foreach (Transform t in canvas.rootCanvas.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Abas_Dos_Modos")
            {
                temAbas = true;
                break;
            }
        }
        if (!temAbas)
        {
            return;
        }
        espacoAberto = true;
        titulo.rectTransform.anchoredPosition += new Vector2(0f, -62f);
        titulo.fontSize = Mathf.RoundToInt(titulo.fontSize * 0.88f);
        Transform subtitulo = titulo.transform.parent.Find("Subtitulo");
        if (subtitulo != null)
        {
            ((RectTransform)subtitulo).anchoredPosition += new Vector2(0f, -52f);
        }
    }

    public void Escolher(int nivel)
    {
        if (minigame != null)
        {
            minigame.JogarNivel(nivel);
        }
    }
}
