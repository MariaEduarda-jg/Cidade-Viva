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
        gameObject.SetActive(true);
    }

    public void Escolher(int nivel)
    {
        if (minigame != null)
        {
            minigame.JogarNivel(nivel);
        }
    }
}
