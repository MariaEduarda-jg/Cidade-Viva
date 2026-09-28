using UnityEngine;

// Ponto único que todo minigame chama ao terminar.
// Por enquanto só registra no console; o salvamento de progresso e recordes entra depois.
public static class GameProgress
{
    public static void FinalizarMinigame(string nomeDaArea, int pontuacao)
    {
        Debug.Log("Minigame " + nomeDaArea + " finalizado com pontuação " + pontuacao);
    }
}
