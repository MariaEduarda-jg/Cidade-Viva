using UnityEngine;

// Regras e apresentação do "Sobe, Sobe!".
[CreateAssetMenu(fileName = "SobeSobe_Config", menuName = "Cidade Viva/Sobe Sobe - Config")]
public class SobeSobeConfig : ScriptableObject
{
    public float larguraDaArea = 1000f;

    [Header("Dificuldade")]
    public float intervaloDeNivel = 15f;
    public int nivelMaximo = 6;

    [Header("Jogador")]
    public float velocidadeHorizontal = 650f;
    public float forcaDoPulo = 1150f;
    public float gravidade = 2000f;

    [Header("Plataformas")]
    public float larguraInicialDaPlataforma = 240f;
    public float reducaoDeLarguraPorNivel = 15f;
    public float distanciaMinima = 160f;
    public float distanciaMaxima = 220f;
    public float aumentoDeDistanciaPorNivel = 10f;
    public float deslocamentoMaximoX = 420f;

    [Header("Moedas")]
    [Range(0f, 1f)] public float chanceDeMoeda = 0.3f;
    public int pontosPorMoeda = 20;
    public float alturaDaMoedaSobreAPlataforma = 60f;

    [Header("Água")]
    public float velocidadeInicialDaAgua = 80f;
    public float aumentoDaAguaPorNivel = 12f;
    public float alturaInicialDaAgua = -700f;
    // A água nunca fica mais longe que isso abaixo da plataforma mais alta em que o jogador pisou,
    // pra continuar aparecendo no pé da tela enquanto ele sobe.
    public float distanciaMaximaDaAgua = 300f;

    [Header("Câmera e pontuação")]
    // Altura na tela (a partir do centro) acima da qual a câmera acompanha o jogador.
    public float alturaDoJogadorNaTela = 150f;
    public float pixelsPorPonto = 10f;

    [Header("Apresentação")]
    public Color corDoJogador = Color.white;
    public Color corDaPlataforma = Color.white;
    public Color corDaMoeda = Color.white;
    public Color corDaAgua = Color.white;
    public string formatoPontos = "Pontos: {0}";
    public string tituloAoCair = "Você caiu!";
    public string tituloAoSerAlcancado = "A água te alcançou!";
}
