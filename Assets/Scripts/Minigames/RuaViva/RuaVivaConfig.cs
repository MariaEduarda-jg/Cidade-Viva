using UnityEngine;

// Tudo que vale pra todos os níveis do Passeio de Carro (Rua Viva, parte 1): medidas, textos e sons.
// Os "trechos" são copiados das definições letra por letra (o construtor confere).
[CreateAssetMenu(fileName = "RuaVivaCarro_Config", menuName = "Cidade Viva/Rua Viva: config do passeio")]
public class RuaVivaConfig : ScriptableObject
{
    [Header("Trechos das definições (copiados do glossário)")]
    [Tooltip("De \"cinto de segurança\".")]
    public string trechoDaFivela = "com a fivela e o fecho";
    public string trechoDeQuando = "quando entramos no carro";
    public string trechoDaFreada = "evitar machucar em freada";
    [Tooltip("De \"passageiro\".")]
    public string trechoDoPassageiro = "mas não o conduz";
    public string trechoDoMotorista = "Ele acompanha o motorista";
    [Tooltip("De \"buzina\".")]
    public string trechoDaBuzina = "faz barulho para chamar atenção no trânsito";

    [Header("Garagem")]
    [Tooltip("Distância (em unidades da tela) em que a fivela encaixa sozinha no fecho.")]
    public float encaixeDaFivela = 150f;
    [Tooltip("Pausa entre um ocupante e o seguinte.")]
    public float pausaEntreOcupantes = 0.45f;
    public string[] nomesDosOcupantes = { "Você", "Pai (motorista)", "Mãe", "Filha", "Caçula" };

    [Header("Passeio")]
    public float alturaDaCamera = 1.35f;
    [Tooltip("Quanto a câmera olha pra direita do carro (45 = vê a rua à frente e a janela).")]
    public float giroDaCamera = 45f;
    public float aceleracao = 3f;
    public float freio = 9f;
    public float comecoDoPasseio = 25f;
    public float espacoEntreEventos = 50f;
    public float fimDepoisDoUltimo = 30f;
    [Tooltip("A esta distância o motorista faz o pedido.")]
    public float antecipacaoDoPedido = 45f;
    [Tooltip("Depois de passar do objeto, por quanto o pedido ainda vale.")]
    public float toleranciaDepoisDoObjeto = 4f;
    [Tooltip("Menor distância da mão do carro até um objeto (fica depois da calçada).")]
    public float lateralMinima = 6f;
    public float velocidadeDevagar = 1.5f;
    [Tooltip("Quanto tempo o carro espera parado ao lado do objeto, nos níveis em que ele para.")]
    public float esperaAoLadoDoObjeto = 8f;
    [Tooltip("Depois deste tempo no pedido, a marca mostra onde está (nível com dica).")]
    public float tempoAteAMarca = 4f;
    public float tempoDaFigurinha = 1.6f;
    [Tooltip("Pra ganhar a estrela do safari: (acertos - toques errados) / pedidos.")]
    [Range(0f, 1f)] public float fracaoDoSafari = 0.75f;

    [Header("Buzina")]
    [Tooltip("Distância em que o motorista freia se ninguém buzinou.")]
    public float distanciaDaFreada = 9f;
    public float ladoDeOndeVem = 6.5f;
    [Tooltip("Beira da rua: até onde a pessoa chega antes de ouvir ou de atravessar.")]
    public float beiraDaRua = 2f;
    public float velocidadeDeAtravessar = 3f;
    [Tooltip("Dois toques mais próximos que isto contam como um só.")]
    public float intervaloEntreBuzinadas = 0.35f;

    [Header("Ritmo")]
    public float tempoDoBalao = 3f;
    public float pausaAntesDoFim = 1.4f;

    [Header("Textos")]
    public string textoDoNivel = "Nível {0}";
    public string[] rotulosDosCriterios = { "safari", "buzina", "sem susto" };
    [Tooltip("{0} = acertos, {1} = pedidos.")]
    public string textoDoSafari = "Safari: {0} de {1}";
    public string recadoDaGaragem = "Arraste a fivela até o fecho!";
    [Tooltip("{0} = quem está prendendo o cinto.")]
    public string textoDoOcupante = "Cinto de: {0}";
    public string recadoDeSair = "Todos de cinto. Vamos passear!";
    public string recadoDoPasseio = "Olhe pela janela!";
    [Tooltip("{0} = \"um\" ou \"uma\"; {1} = a palavra do verbete.")]
    public string textoDoPedido = "Você vê {0} {1}?";
    public string recadoDeAcerto = "Isso!";
    [Tooltip("{0} = a palavra do que foi tocado.")]
    public string recadoDeOutroObjeto = "Esse é: {0}. Procure de novo!";
    public string recadoDePassou = "Passou! Olhe o próximo.";
    public string recadoDoTempo = "Acabou o tempo desse! Olhe o próximo.";
    public string recadoDeBuzinar = "Buzine agora!";
    public string recadoDaBuzinaCerta = "Isso! A pessoa ouviu e parou.";
    [Tooltip("{0} = trecho da definição de buzina.")]
    public string balaoDaBuzinaAToa = "Ninguém precisava ouvir! O bichinho levou um susto.\nBuzina: \"{0}\".";
    [Tooltip("{0} = trecho da definição de buzina, {1} = trecho da freada.")]
    public string balaoDaFreada = "Ufa! O motorista freou.\nBuzina: \"{0}\".\nCinto: \"{1}\".";
    public string recadoDaFreada = "Ufa! Quase não viu o carro.";
    public string recadoDeChegada = "Chegamos! Que passeio bom.";

    [Header("Sons")]
    public AudioClip somDaFivela;
    public AudioClip somDeSoltar;
    public AudioClip somDoMotor;
    public AudioClip somDaBuzina;
    public AudioClip somDeAcerto;
    public AudioClip somDeOutroObjeto;
    public AudioClip somDoSusto;
    public AudioClip somDaFreada;
    public AudioClip somDePedido;
    public AudioClip somDeComemoracao;
}
