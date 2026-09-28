using UnityEngine;
using UnityEngine.UI;

// Conta acertos e erros do "Onde Eu Vou?" e finaliza o minigame quando todas as placas estão no lugar.
public class OndeEuVouManager : MonoBehaviour
{
    public int totalDePlacas = 3;
    public int pontuacaoMaxima = 100;
    public int penalidadePorErro = 10;
    public Text textoFinal;

    private int acertos;
    private int erros;

    private void Start()
    {
        if (textoFinal != null)
        {
            textoFinal.gameObject.SetActive(false);
        }
    }

    public void RegistrarErro()
    {
        erros++;
    }

    public void RegistrarAcerto()
    {
        acertos++;
        if (acertos < totalDePlacas)
        {
            return;
        }

        int pontuacao = Mathf.Max(0, pontuacaoMaxima - erros * penalidadePorErro);
        GameProgress.FinalizarMinigame("Onde Eu Vou?", pontuacao);

        if (textoFinal != null)
        {
            textoFinal.text = "Muito bem! Pontuação: " + pontuacao;
            textoFinal.gameObject.SetActive(true);
        }
    }
}
