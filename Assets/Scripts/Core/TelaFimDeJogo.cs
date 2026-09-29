using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tela de fim de jogo reaproveitável (prefab em Assets/Prefabs): registra a pontuação no
// GameProgress, mostra o resultado e oferece "Tentar de novo" e "Voltar ao Hub".
public class TelaFimDeJogo : MonoBehaviour
{
    public Text texto;

    public void Mostrar(string nomeDaArea, int pontuacao, string titulo = "Fim de jogo!")
    {
        bool novoRecorde = pontuacao > GameProgress.ObterRecorde(nomeDaArea);
        GameProgress.FinalizarMinigame(nomeDaArea, pontuacao);

        if (texto != null)
        {
            texto.text = titulo + "\nPontuação: " + pontuacao
                + (novoRecorde ? "\nNovo recorde!" : "\nRecorde: " + GameProgress.ObterRecorde(nomeDaArea));
        }

        gameObject.SetActive(true);
    }

    public void TentarDeNovo()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
