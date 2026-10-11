using UnityEngine;

// Regras e apresentação do "Semáforo Mágico".
[CreateAssetMenu(fileName = "SemaforoMagico_Config", menuName = "Cidade Viva/Semaforo Magico - Config")]
public class SemaforoConfig : ScriptableObject
{
    [Header("Ritmo")]
    public float velocidadeInicial = 320f;
    public float aumentoDeVelocidadePorSemaforo = 12f;
    public float velocidadeMaxima = 600f;
    // Tempo pra reagir = tempo do carro chegar à faixa depois que o sinal acende.
    public float janelaInicial = 2.5f;
    public float reducaoDaJanelaPorSemaforo = 0.08f;
    public float janelaMinima = 1f;
    public float fatorDevagarNoAmarelo = 0.45f;
    public float tempoParadoNoVermelho = 1.5f;
    public int semaforosParaVencer = 20;

    [Header("Pontuação")]
    public int pontosPorAcerto = 10;
    public int pontosPorComboMaximo = 5;

    [Header("Textos")]
    public string textoDeInicio = "Vamos lá!";
    public string dicaVerde = "Verde: aperte ↑ e siga!";
    public string dicaAmarelo = "Amarelo: não aperte nada, vá devagar";
    public string dicaVermelho = "Vermelho: aperte ESPAÇO e pare!";
    public string parabensVerde = "Isso! Verde = siga!";
    public string parabensAmarelo = "Isso! Amarelo = devagar";
    public string parabensVermelho = "Isso! Vermelho = pare!";
    public string textoTeclaErrada = "Ops!";
    public string textoSemReacao = "Muito devagar!";
    public string formatoPontos = "Pontos: {0}";
    public string formatoCombo = "Combo: {0}  (máx. {1})";
    public string formatoSemaforos = "Semáforo: {0} / {1}";
    public string tituloAoVencer = "Parabéns! Você passou por {0} semáforos!";
    public string tituloAoPerder = "Fim de jogo!";

    [Header("Cores do feedback")]
    public Color corNeutra = Color.white;
    public Color corDoAcerto = new Color(0.55f, 1f, 0.55f);
    public Color corDoErro = new Color(1f, 0.6f, 0.55f);

    public string Dica(CorDoSemaforo cor)
    {
        switch (cor)
        {
            case CorDoSemaforo.Verde:
                return dicaVerde;
            case CorDoSemaforo.Amarelo:
                return dicaAmarelo;
            default:
                return dicaVermelho;
        }
    }

    public string Parabens(CorDoSemaforo cor)
    {
        switch (cor)
        {
            case CorDoSemaforo.Verde:
                return parabensVerde;
            case CorDoSemaforo.Amarelo:
                return parabensAmarelo;
            default:
                return parabensVermelho;
        }
    }
}
