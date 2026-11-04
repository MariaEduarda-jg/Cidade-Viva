using UnityEngine;

// Tudo que vale pra todos os níveis da Central de Sinais (os dois modos): medidas, textos e sons.
// As regras vêm das definições dos verbetes; os "trechos" abaixo são copiados delas letra por letra
// (o SinaisConstrutor confere) e é com eles que os balões e as dicas são montados.
[CreateAssetMenu(fileName = "Sinais_Config", menuName = "Cidade Viva/Sinais: config")]
public class SinaisConfig : ScriptableObject
{
    [Header("Trechos das definições (copiados do glossário)")]
    [Tooltip("De \"sinal verde\".")]
    public string trechoDoVerde = "os veículos ou os pedestres podem continuar andando";
    [Tooltip("De \"sinal amarelo\".")]
    public string trechoDoAmarelo = "o motorista deve diminuir a velocidade";
    [Tooltip("De \"sinal vermelho\".")]
    public string trechoDoVermelho = "os veículos ou os pedestres devem parar";
    [Tooltip("De \"semáforo\".")]
    public string trechoDoSemaforo = "para organizar a passagem de carros e pessoas nas ruas";
    [Tooltip("De \"semáforo de pedestre\".")]
    public string trechoDoSemaforoDePedestre = "para organizar a passagem de pedestres na via";

    [Header("Motorista Mirim: o carrinho")]
    public float velocidadeMaxima = 9f;
    [Tooltip("No amarelo, o carrinho tem de cruzar a linha no máximo a esta velocidade.")]
    public float velocidadeDevagar = 4.5f;
    public float aceleracao = 5f;
    public float freio = 10f;
    [Tooltip("Sem apertar nada, o carrinho vai perdendo um pouco de velocidade.")]
    public float atrito = 0.8f;
    [Tooltip("Meio comprimento do carrinho: a frente dele é que conta na linha.")]
    public float frenteDoCarro = 1.8f;
    [Tooltip("Depois de um erro, o carrinho volta a esta distância da linha.")]
    public float recuo = 9f;
    [Tooltip("Sinal que acabou de mudar há menos que isto ainda vale a cor de antes (não dava tempo de reagir).")]
    public float tolerancia = 0.7f;
    [Tooltip("A que distância da linha conta \"parou no vermelho\".")]
    public float distanciaDeParada = 14f;
    public Vector3 cameraAtrasDoCarro = new Vector3(0f, 7.5f, -11f);
    public Vector3 focoNaFrenteDoCarro = new Vector3(0f, 1f, 9f);
    [Tooltip("Quão depressa a câmera acompanha as curvas.")]
    public float giroDaCamera = 3.5f;

    [Header("Motorista Mirim: os semáforos")]
    [Tooltip("Um por nível: quais lugares usa e como o semáforo de cada um se comporta.")]
    public RoteiroDoNivel[] roteiros = new RoteiroDoNivel[0];
    [Tooltip("A esta distância da linha o semáforo que muda ao chegar acende o amarelo.")]
    public float distanciaDoAmarelo = 32f;
    [Tooltip("Quanto dura o amarelo dos semáforos que fecham ao chegar.")]
    public float tempoDoAmarelo = 5.5f;
    [Tooltip("No vermelho segurado, quanto tempo o carrinho espera parado antes de abrir.")]
    public float esperaNoVermelho = 3f;
    public float distanciaDoAvisoDeAmarelo = 50f;
    public float distanciaDoAvisoDoLugar = 55f;

    [Header("Sinaleiro: carros e pedestres")]
    public float velocidadeDosCarros = 8f;
    [Tooltip("No amarelo, o carro passa a esta fração da velocidade.")]
    public float fracaoNoAmarelo = 0.4f;
    public float velocidadeDosPedestres = 2.6f;
    [Tooltip("Carro parado há este tempo dá uma buzinada suave.")]
    public float esperaPraBuzinar = 7f;
    [Tooltip("A ambulância parada no vermelho há este tempo conta um erro.")]
    public float esperaDaAmbulancia = 8f;
    [Tooltip("Em que segundo do nível a ambulância aparece.")]
    public float chegadaDaAmbulancia = 18f;

    [Header("Ritmo")]
    public float tempoDoBalao = 3.2f;
    public float pausaAntesDoFim = 1.4f;

    [Header("Textos gerais")]
    public string textoDoNivel = "Nível {0}";
    public string[] rotulosDosCriterios = { "sem erros", "no tempo" };
    public string textoDosErros = "Erros: {0}";
    public string recadoDeTempoEsgotado = "O tempo passou, mas pode terminar com calma.";

    [Header("Textos do Motorista Mirim")]
    public string recadoDeComecar = "Segure Acelerar para andar!";
    [Tooltip("{0} = trecho da definição.")]
    public string dicaDoVerde = "Sinal verde: \"{0}\"";
    public string dicaDoAmarelo = "Sinal amarelo: \"{0}\"";
    public string dicaDoVermelho = "Sinal vermelho: \"{0}\"";
    [Tooltip("{0} = trecho da definição.")]
    public string balaoDoVermelho = "Cof, cof! Sinal vermelho:\n\"{0}\".\nPare antes da linha!";
    public string balaoDoAmarelo = "Cof, cof! Sinal amarelo:\n\"{0}\".\nVá mais devagar!";
    public string recadoDoVerde = "Verde: pode seguir!";
    public string recadoDoAmarelo = "Isso! Diminuiu no amarelo.";
    public string recadoDoVermelho = "Isso! Parou no vermelho e esperou.";
    public string recadoDeChegada = "Chegou! Dirigiu direitinho.";
    public string recadoDeChegarPerto = "Chegue mais perto da linha e pare lá.";
    public string rotuloDevagar = "devagar";

    [Header("Textos do Sinaleiro")]
    public string recadoDoSinaleiro = "Abra o sinal de uma via de cada vez!";
    [Tooltip("{0} = quantos já passaram, {1} = quantos precisam passar.")]
    public string textoDoContador = "Passaram {0} de {1}";
    public string nomeDaViaA = "Via azul";
    public string nomeDaViaB = "Via laranja";
    public string nomeDosPedestres = "Pedestres";
    public string balaoDeDuasVias = "Ui, quase! Os carros das duas vias passaram juntos.\nSemáforo: \"{0}\".";
    public string balaoDePedestre = "Ui, quase! Carro e pedestre passaram juntos.\nSemáforo de pedestre: \"{0}\".";
    public string balaoDaAmbulancia = "A ambulância ficou esperando! Abra o sinal pra ela.";
    public string recadoDaAmbulancia = "Vem uma ambulância! Abra o sinal da via dela.";
    public string recadoDeFim = "Todo mundo passou!";

    [Header("Sons")]
    public AudioClip somDoTic;
    public AudioClip somDeAcerto;
    public AudioClip somDeErro;
    public AudioClip somDaTosse;
    public AudioClip somDaBuzina;
    [Range(0f, 1f)] public float volumeDaBuzina = 0.3f;
    public AudioClip somDaSirene;
    [Range(0f, 1f)] public float volumeDaSirene = 0.5f;
    public AudioClip somDeComemoracao;
    public AudioClip somDeBotao;
}
