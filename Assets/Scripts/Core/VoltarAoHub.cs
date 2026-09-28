using UnityEngine;
using UnityEngine.SceneManagement;

public class VoltarAoHub : MonoBehaviour
{
    // Lido pelo HubSpawn ao carregar o Hub, para reposicionar o jogador num ponto seguro.
    public static bool voltandoDeMinigame;

    public void Voltar()
    {
        voltandoDeMinigame = true;
        SceneManager.LoadScene("Hub");
    }
}
