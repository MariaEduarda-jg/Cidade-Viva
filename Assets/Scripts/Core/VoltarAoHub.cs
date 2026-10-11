using UnityEngine;
using UnityEngine.SceneManagement;

public class VoltarAoHub : MonoBehaviour
{
    // Lido pelo HubSpawn ao carregar o Hub, para reposicionar o jogador num ponto seguro.
    public static bool voltandoDeMinigame;

    public void Voltar()
    {
        voltandoDeMinigame = true;
        // Sai do nível: na próxima entrada no minigame a seleção de níveis aparece de novo.
        MinigameBase.EsquecerNivelEscolhido();
        SceneManager.LoadScene("Hub");
    }
}
