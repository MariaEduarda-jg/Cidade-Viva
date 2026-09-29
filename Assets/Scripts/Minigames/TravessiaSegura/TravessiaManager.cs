using UnityEngine;
using UnityEngine.UI;

// Cronometra a travessia e calcula a pontuação do "Travessia Segura".
public class TravessiaManager : MonoBehaviour
{
    public int pontuacaoMaxima = 100;
    public int pontuacaoMinima = 10;
    public float tempoIdeal = 5f;
    public int pontosPorSegundoExtra = 5;
    public int penalidadePorBatida = 10;
    public Text textoFinal;

    private bool cronometroRodando;
    private float tempoInicio;
    private int batidas;

    private void Start()
    {
        if (textoFinal != null)
        {
            textoFinal.gameObject.SetActive(false);
        }
    }

    // O tempo só começa a contar no primeiro passo, pra criança poder olhar a rua antes.
    public void IniciarCronometro()
    {
        if (cronometroRodando)
        {
            return;
        }

        cronometroRodando = true;
        tempoInicio = Time.time;
    }

    public void RegistrarBatida()
    {
        batidas++;
    }

    public void TravessiaCompleta()
    {
        float tempo = Time.time - tempoInicio;
        int segundosExtras = Mathf.CeilToInt(Mathf.Max(0f, tempo - tempoIdeal));
        int pontuacao = pontuacaoMaxima - segundosExtras * pontosPorSegundoExtra - batidas * penalidadePorBatida;
        pontuacao = Mathf.Clamp(pontuacao, pontuacaoMinima, pontuacaoMaxima);

        GameProgress.FinalizarMinigame("Travessia Segura", pontuacao);

        if (textoFinal != null)
        {
            textoFinal.text = "Você atravessou! Pontuação: " + pontuacao;
            textoFinal.gameObject.SetActive(true);
        }
    }
}
