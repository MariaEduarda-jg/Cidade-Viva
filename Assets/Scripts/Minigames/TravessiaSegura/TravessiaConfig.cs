using UnityEngine;

// Regras e apresentação do "Travessia Segura" que valem pra todos os níveis.
// O que muda de nível pra nível (quantas pistas, velocidade, semáforo, ambulância...) fica no
// LevelConfig (Assets/Configs/Niveis/Niveis_TravessiaSegura.asset).
[CreateAssetMenu(fileName = "TravessiaSegura_Config", menuName = "Cidade Viva/Travessia Segura - Config")]
public class TravessiaConfig : ScriptableObject
{
    [Header("Rua (metros)")]
    public float larguraDaPista = 3.6f;
    public float larguraDaCiclovia = 2f;
    [Tooltip("Metade da largura da faixa de pedestre, ao longo da rua.")]
    public float meiaLarguraDaFaixa = 2.1f;
    [Tooltip("Distância do centro da faixa até a linha onde os carros param.")]
    public float linhaDeParada = 3.6f;
    [Tooltip("Distância do centro da faixa até onde os veículos surgem e somem.")]
    public float distanciaDeSurgimento = 44f;

    [Header("Trânsito")]
    [Tooltip("Velocidade dos carros (m/s) quando o nível tem velocidade 1.")]
    public float velocidadeBase = 7f;
    public float velocidadeDoCiclista = 4f;
    public float velocidadeDaAmbulancia = 13f;
    public float aceleracao = 14f;
    [Tooltip("Segundos entre um carro e outro na mesma pista, se o nível não disser.")]
    public float intervaloPadraoDeCarros = 3.5f;
    public GameObject[] carros;
    public GameObject ambulancia;

    [Header("Jogador")]
    public float duracaoDoPasso = 0.32f;
    public float raioDoJogador = 0.45f;
    [Tooltip("Distância, pela calçada, entre a faixa e os pontos ao lado dela.")]
    public float distanciaAoLadoDaFaixa = 5f;
    public int vidas = 3;

    [Header("Olhar para os dois lados")]
    public float duracaoDasSetas = 2.6f;
    [Tooltip("Veículo que chega na faixa em menos tempo que isto (segundos) aparece como perigo.")]
    public float tempoDePerigo = 3f;

    [Header("Semáforo de pedestres (se o nível não disser)")]
    public float tempoVerde = 6f;
    public float tempoVermelho = 7f;
    public float tempoPiscando = 2f;

    [Header("Ambulância (se o nível não disser)")]
    public float intervaloDaAmbulancia = 14f;

    [Header("Pontuação")]
    public int pontuacaoMaxima = 100;
    public int pontuacaoMinima = 10;
    public int pontosPorTropeco = 20;
    public float tempoIdeal = 25f;
    public int pontosPorSegundoExtra = 1;

    [Header("Textos")]
    public string tituloDoFim = "Você atravessou!";
    public string dicaInicial = "Olhe para os dois lados antes de atravessar.";
    public string dicaIrAteAFaixa = "Vá até a faixa de pedestre.";
    public string dicaForaDaFaixa = "Atravesse na faixa de pedestre!";
    public string dicaPrecisaOlhar = "Primeiro olhe para os dois lados!";
    public string dicaSinalVermelho = "Espere o bonequinho verde!";
    public string dicaAmbulancia = "Ambulância! Deixe ela passar.";
    public string dicaVemCarro = "Vem carro! Espere.";
    public string dicaLivre = "Livre! Pode atravessar.";
    public string dicaTropeco = "Opa! Vamos tentar de novo.";
    public string dicaSinalAbriu = "Sinal verde! Pode atravessar.";

    [Header("Sons")]
    public AudioClip[] passos;
    public AudioClip buzina;
    public AudioClip sirene;
    public AudioClip campainhaDeBicicleta;
    public AudioClip acerto;
    public AudioClip erro;
    public AudioClip tropeco;
    public AudioClip olhar;
    public AudioClip bloqueado;
    public AudioClip sinalVerde;
}
