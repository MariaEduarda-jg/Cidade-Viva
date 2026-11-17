using System;
using UnityEngine;

// O que acontece num ponto do percurso de uma missão.
public enum TipoDeEvento
{
    Lugar,      // passa por um lugar (pode ter um pedido do safari)
    Parada,     // o carro para e alguém entra (precisa prender o cinto)
    Destino,    // fim da missão: o carro para, alguém desce ou entra
    Distraida,  // pessoa vem pra rua sem ver o carro: hora da buzina
    Atenta,     // pessoa anda pela calçada: não precisa de buzina
    Bola,       // uma bola atravessa a rua: o motorista freia
}

[Serializable]
public class OcupanteDaMissao
{
    [Tooltip("pai, mae, voce, filha, cacula, vovo (ou ursinho, caixa, nas coisas soltas).")]
    public string quem;
    [Tooltip("0 = motorista, 1 = da frente, 2 = atrás à esquerda, 3 = atrás no meio, 4 = atrás à direita.")]
    public int assento;
}

[Serializable]
public class EventoDaMissao
{
    public TipoDeEvento tipo;
    [Tooltip("Id do lugar (LugarDoPasseio) em Lugar, Parada e Destino.")]
    public string lugar = "";
    [Tooltip("Verbete pedido no safari ao passar pelo lugar (vazio = nenhum).")]
    public string pedido = "";
    [Tooltip("Parada e Destino: quem entra no carro, e em qual assento.")]
    public string entra = "";
    public int assento;
    [Tooltip("Destino: quem desce do carro.")]
    public string[] descem = new string[0];
    [Tooltip("Destino: quem está esperando lá e comemora.")]
    public string espera = "";
}

[Serializable]
public class MissaoDoPasseio
{
    public string titulo;
    [TextArea] public string texto;
    [Tooltip("O que este nível ensina de novo.")]
    public string novidade;
    public OcupanteDaMissao[] ocupantes = new OcupanteDaMissao[0];
    [Tooltip("Coisas soltas nos bancos (ursinho, caixa).")]
    public OcupanteDaMissao[] soltos = new OcupanteDaMissao[0];
    [Tooltip("Antes de sair, a criança guarda as coisas soltas no porta-malas.")]
    public bool guardarSoltos;
    public EventoDaMissao[] eventos = new EventoDaMissao[0];
    [Tooltip("Notícias do rádio (a primeira toca quando o carro sai; tocar no rádio passa pra próxima).")]
    public string[] radio = new string[0];
    public string recadoDeChegada;
}

// Tudo que vale pra todos os níveis do Passeio de Carro (Rua Viva, parte 1): missões, medidas, textos e sons.
// Os "trechos" são copiados das definições letra por letra (o construtor confere).
[CreateAssetMenu(fileName = "RuaVivaCarro_Config", menuName = "Cidade Viva/Rua Viva: config do passeio")]
public class RuaVivaConfig : ScriptableObject
{
    [Header("Trechos das definições (copiados do glossário)")]
    [Tooltip("De \"cinto de segurança\".")]
    public string trechoDaFivela = "com a fivela e o fecho";
    public string trechoDoCorpo = "prender ao nosso corpo e ao assento";
    public string trechoDeQuando = "quando entramos no carro";
    public string trechoDaFreada = "evitar machucar em freada";
    [Tooltip("De \"passageiro\".")]
    public string trechoDeDentro = "Pessoa que está dentro de um carro";
    public string trechoDoPassageiro = "mas não o conduz";
    public string trechoDoMotorista = "Ele acompanha o motorista";
    [Tooltip("De \"buzina\".")]
    public string trechoDoBotao = "Botão do veículo";
    public string trechoDeQuemAperta = "quando alguém apertado";
    public string trechoDaBuzina = "faz barulho para chamar atenção no trânsito";
    [Tooltip("De \"sinal vermelho\" (o carro para no semáforo do cruzamento).")]
    public string trechoDoVermelho = "os veículos ou os pedestres devem parar";

    [Header("Missões (uma por nível)")]
    public MissaoDoPasseio[] missoes = new MissaoDoPasseio[0];

    [Header("Cinto")]
    [Tooltip("Distância (m) em que a fivela encaixa sozinha no fecho.")]
    public float encaixeDaFivela = 0.12f;
    [Tooltip("Raio (pixels) em volta da fivela em que o dedo pega nela.")]
    public float raioDeToqueDaFivela = 110f;
    public float pausaEntreOcupantes = 0.6f;

    [Header("Passeio")]
    public float aceleracao = 3f;
    public float freio = 5f;
    [Tooltip("Freada forte (bola e pessoa distraída).")]
    public float freioForte = 16f;
    public float comecoDoPasseio = 30f;
    public float espacoEntreEventos = 40f;
    [Tooltip("A esta distância o motorista faz o pedido.")]
    public float antecipacaoDoPedido = 45f;
    [Tooltip("Depois de passar do objeto, por quanto o pedido ainda vale.")]
    public float toleranciaDepoisDoObjeto = 4f;
    public float velocidadeDevagar = 1.5f;
    [Tooltip("Quanto tempo o carro espera parado ao lado do objeto, nos níveis em que ele para.")]
    public float esperaAoLadoDoObjeto = 8f;
    [Tooltip("Depois deste tempo no pedido, a marca mostra onde está (nível com dica).")]
    public float tempoAteAMarca = 4f;
    public float tempoDaFigurinha = 1.6f;
    [Tooltip("Pra ganhar a estrela do safari: (acertos - toques errados) / pedidos.")]
    [Range(0f, 1f)] public float fracaoDoSafari = 0.75f;
    [Tooltip("Tempo parado no sinal vermelho antes de abrir o verde.")]
    public float esperaNoVermelho = 3f;
    public float velocidadeDoTransito = 8f;

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
    public float tempoDoBalao = 3.5f;
    public float tempoDoRadio = 5f;
    public float pausaAntesDoFim = 2f;

    [Header("Textos")]
    public string textoDoNivel = "Nível {0}";
    public string[] rotulosDosCriterios = { "safari", "buzina", "sem susto" };
    [Tooltip("{0} = acertos, {1} = pedidos.")]
    public string textoDoSafari = "Safari: {0} de {1}";
    public string botaoDaMissao = "Vamos!";
    public string recadoDosSoltos = "Tem coisa solta no banco! Toque nela pra guardar no porta-malas.";
    public string recadoTudoGuardado = "Tudo guardado no porta-malas!";
    public string recadoDoMotorista = "O motorista prende o cinto dele.";
    [Tooltip("{0} = quem está prendendo o cinto.")]
    public string textoDoOcupante = "Cinto de: {0}. Puxe a fivela até o fecho!";
    public string recadoDaChave = "Todos de cinto! Agora toque na chave pra ligar o carro.";
    public string recadoDaChaveSemCinto = "Ainda falta alguém prender o cinto.";
    public string recadoDoPasseio = "Olhe pela janela!";
    [Tooltip("{0} = o que o motorista diz do lugar; {1} = \"um\" ou \"uma\"; {2} = a palavra do verbete.")]
    public string textoDoPedido = "{0} Você vê {1} {2}?";
    public string recadoDeAcerto = "Isso!";
    [Tooltip("{0} = a palavra do que foi tocado.")]
    public string recadoDeOutroObjeto = "Esse é: {0}. Procure de novo!";
    public string recadoDePassou = "Passou! Olhe o próximo.";
    public string recadoDoTempo = "Acabou o tempo desse! Olhe o próximo.";
    public string recadoDeBuzinar = "Alguém vem sem olhar! Toque no meio do volante!";
    public string recadoDaBuzinaCerta = "Isso! O motorista apertou a buzina e a pessoa parou.";
    [Tooltip("{0} = trecho da definição de buzina.")]
    public string balaoDaBuzinaAToa = "Ninguém precisava ouvir! Todo mundo levou um susto.\nBuzina: \"{0}\".";
    [Tooltip("{0} = trecho da definição de buzina, {1} = trecho da freada.")]
    public string balaoDaFreada = "Ufa! O motorista freou.\nBuzina: \"{0}\".\nCinto: \"{1}\".";
    public string recadoDaFreada = "Ufa! Quase não viu o carro.";
    public string recadoDaBola = "Opa, uma bola! O motorista freou.";
    [Tooltip("{0} = trecho da freada.")]
    public string balaoDaFreadaComSolto = "Quem estava de cinto ficou no banco. O que estava solto voou!\nCinto: \"{0}\".";
    [Tooltip("{0} = trecho da freada.")]
    public string balaoDaFreadaSemSolto = "Todo mundo de cinto ficou no banco, e nada voou: estava tudo guardado!\nCinto: \"{0}\".";
    [Tooltip("{0} = trecho do sinal vermelho.")]
    public string recadoDoVermelho = "Sinal vermelho: \"{0}\".";
    public string recadoDoVerde = "Verde! Pode seguir.";
    [Tooltip("{0} = o nome do lugar.")]
    public string recadoDaParada = "Paramos: {0}. Alguém vai entrar!";
    [Tooltip("{0} = quem entrou.")]
    public string recadoDeQuemEntrou = "{0} entrou no carro: agora precisa prender o cinto!";
    public string recadoTodosDeCinto = "Todos de cinto. Vamos!";
    [Tooltip("{0} = quem desceu.")]
    public string recadoDeQuemDesceu = "Tchau, {0}!";
    public string prefixoDoRadio = "Rádio Cidade:";

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
    public AudioClip somDoRadio;
    public AudioClip somDoPortaMalas;
}
