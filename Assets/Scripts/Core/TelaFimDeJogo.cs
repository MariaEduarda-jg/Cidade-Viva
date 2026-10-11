using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tela de fim de jogo reaproveitável (prefab Assets/Prefabs/UI_FimDeJogo). Só apresentação:
// quem registra o resultado é o MinigameBase, que chama Mostrar. Os botões do prefab chamam
// ProximoNivel e TentarDeNovo (aqui) e VoltarAoHub.Voltar.
public class TelaFimDeJogo : MonoBehaviour
{
    public Text textoTitulo;
    public Text textoPontuacao;
    // "Nível 2" (some quando o minigame não tem níveis).
    public Text textoDoNivel;
    // As 3 estrelas, da esquerda pra direita.
    public Image[] estrelas;
    public Button botaoProximoNivel;

    [Header("Textos")]
    public string tituloPadrao = "Fim de jogo!";
    public string tituloSemConcluir = "Quase! Tente de novo";
    public string formatoNivel = "Nível {0}";
    public string formatoPontuacao = "Pontuação: {0}";
    public string textoNovoRecorde = "Novo recorde!";
    public string formatoRecorde = "Recorde: {0}";

    [Header("Cores das estrelas")]
    public Color estrelaGanha = Color.white;
    public Color estrelaApagada = new Color(0.35f, 0.4f, 0.45f, 0.3f);

    private MinigameBase minigame;

    public void Mostrar(MinigameBase dono, string titulo, int pontuacao, int recorde, bool novoRecorde)
    {
        Mostrar(dono, titulo, pontuacao, recorde, novoRecorde, 0, true);
    }

    // estrelasGanhas: 0 a 3. concluiu = false troca o título e esconde "Próximo nível".
    public void Mostrar(MinigameBase dono, string titulo, int pontuacao, int recorde, bool novoRecorde, int estrelasGanhas, bool concluiu)
    {
        minigame = dono;
        textoTitulo.text = !concluiu ? tituloSemConcluir : (string.IsNullOrEmpty(titulo) ? tituloPadrao : titulo);
        textoPontuacao.text = string.Format(formatoPontuacao, pontuacao) + "\n"
            + (novoRecorde ? textoNovoRecorde : string.Format(formatoRecorde, recorde));

        bool temNiveis = dono != null && dono.TemNiveis;
        if (textoDoNivel != null)
        {
            textoDoNivel.gameObject.SetActive(temNiveis);
            if (temNiveis)
            {
                textoDoNivel.text = string.Format(formatoNivel, dono.NivelAtual);
            }
        }

        if (estrelas != null)
        {
            for (int i = 0; i < estrelas.Length; i++)
            {
                if (estrelas[i] != null)
                {
                    estrelas[i].color = i < estrelasGanhas ? estrelaGanha : estrelaApagada;
                }
            }
        }

        if (botaoProximoNivel != null)
        {
            botaoProximoNivel.gameObject.SetActive(concluiu && temNiveis && dono.TemProximoNivel);
        }

        gameObject.SetActive(true);
    }

    public void ProximoNivel()
    {
        if (minigame != null)
        {
            minigame.ProximoNivel();
        }
    }

    public void TentarDeNovo()
    {
        if (minigame != null)
        {
            minigame.ReiniciarJogo();
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
