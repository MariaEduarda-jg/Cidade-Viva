using System;
using UnityEngine;

// O que tem em cada faixa de um trecho da Pedalada (três faixas: esquerda, meio, direita).
public enum TipoDeFaixa
{
    Grama,       // canteiro com arbustos: não dá pra passar (bater é "batida")
    Ciclovia,    // via própria da bicicleta, separada dos carros
    Ciclofaixa,  // parte da rua pintada de vermelho, ao lado dos carros
    Carros,      // pista dos carros
    Calcada,     // calçada (de pedestres)
}

[Serializable]
public class TrechoDaPedalada
{
    public TipoDeFaixa esquerda = TipoDeFaixa.Grama;
    public TipoDeFaixa meio = TipoDeFaixa.Ciclovia;
    public TipoDeFaixa direita = TipoDeFaixa.Calcada;
    public float comprimento = 36f;
    [Tooltip("Parque (gramado e árvores) ou rua (prédios e calçadas) em volta.")]
    public bool parque = true;
    [Tooltip("Cone e buraco na faixa da rua, no meio do trecho (é preciso desviar).")]
    public bool obstaculo;
    [Tooltip("Carros acumulados nas duas faixas dos carros, com a ambulância presa no meio.")]
    public bool engarrafamento;
    [Tooltip("Recado que aparece um pouco antes de o trecho começar (vazio = nenhum).")]
    public string aviso = "";

    public TipoDeFaixa Faixa(int i)
    {
        return i < 0 ? esquerda : (i > 0 ? direita : meio);
    }

    // A faixa da bicicleta (ciclovia ou ciclofaixa), ou 2 se o trecho não tem.
    public int FaixaDaBicicleta
    {
        get
        {
            for (int i = -1; i <= 1; i++)
            {
                TipoDeFaixa f = Faixa(i);
                if (f == TipoDeFaixa.Ciclovia || f == TipoDeFaixa.Ciclofaixa)
                {
                    return i;
                }
            }
            return 2;
        }
    }

    // Quando a ciclovia deste trecho muda de faixa no próximo, ela faz uma curva nos últimos metros
    // (7 m por faixa). Devolve o comprimento da curva (0 = sem curva). O construtor e o manager usam a mesma conta.
    public static float Curva(TrechoDaPedalada t, TrechoDaPedalada proximo)
    {
        if (t == null || proximo == null) return 0f;
        int a = t.FaixaDaBicicleta, b = proximo.FaixaDaBicicleta;
        if (a == 2 || b == 2 || a == b || t.Faixa(a) != TipoDeFaixa.Ciclovia) return 0f;
        return 7f * Mathf.Abs(a - b);
    }

    // Onde a bicicleta deve ir: a ciclovia/ciclofaixa; sem ela, a faixa dos carros mais à direita.
    public int FaixaCerta
    {
        get
        {
            int b = FaixaDaBicicleta;
            if (b != 2)
            {
                return b;
            }
            for (int i = 1; i >= -1; i--)
            {
                if (Faixa(i) == TipoDeFaixa.Carros)
                {
                    return i;
                }
            }
            return 0;
        }
    }
}

[Serializable]
public class RoteiroDaPedalada
{
    public string titulo;
    [TextArea] public string texto;
    public string novidade;
    [Tooltip("O que a criança pode pôr na cabeça antes de sair (o primeiro é sempre o capacete).")]
    public string[] coisasDaCabeca = { "capacete", "bone" };
    public TrechoDaPedalada[] trechos = new TrechoDaPedalada[0];
    public string recadoDeChegada;
}

// Tudo que vale pra todos os níveis da Pedalada (Rua Viva, parte 2): roteiros, medidas, textos e sons.
// Os "trechos" são copiados das definições letra por letra (o construtor confere).
[CreateAssetMenu(fileName = "RuaVivaPedalada_Config", menuName = "Cidade Viva/Rua Viva: config da pedalada")]
public class PedaladaConfig : ScriptableObject
{
    [Header("Trechos das definições (copiados do glossário)")]
    [Tooltip("De \"ciclista\".")]
    public string trechoDoCiclista = "Pessoa que conduz uma bicicleta";
    public string trechoDoCapacete = "deve usar equipamentos de segurança, como capacete";
    public string trechoDaCiclovia = "incluindo o uso de ciclovia ou ciclofaixa quando disponíveis";
    [Tooltip("De \"engarrafamento\".")]
    public string trechoDoAcumulo = "muitos veículos acumulados na via";
    public string trechoDaPassagem = "atrapalha a passagem de outros veículos";
    [Tooltip("De \"calçada\" (a bicicleta não vai pela calçada).")]
    public string trechoDaCalcada = "destinada para a passagem de pedestres ou animais";
    [Tooltip("De \"ciclovia\" (no parque, longe dos carros).")]
    public string trechoDaViaSeparada = "está em via separada do trânsito dos veículos";
    [Tooltip("De \"ciclofaixa\" (na rua, ao lado dos carros).")]
    public string trechoDoLadoDosCarros = "está ao lado da via dos outros veículos";

    [Header("Roteiros (um por nível)")]
    public RoteiroDaPedalada[] roteiros = new RoteiroDaPedalada[0];

    [Header("Medidas")]
    public float larguraDaFaixa = 2.4f;
    public float tempoDeTrocaDeFaixa = 0.25f;
    [Tooltip("Quanto tempo a bicicleta pode ficar no lugar errado antes de contar (dá tempo de trocar).")]
    public float toleranciaNoLugarErrado = 1.3f;
    [Tooltip("A esta distância antes de um trecho novo aparece o aviso dele.")]
    public float antecedenciaDoAviso = 16f;
    public float paradaNaBatida = 1.2f;
    public float velocidadeDosCarros = 10f;
    public float distanciaEntreCarros = 6.5f;
    public float intervaloDosCarros = 2.2f;
    [Tooltip("A esta distância do começo do engarrafamento a bicicleta para pra olhar.")]
    public float paradaNoEngarrafamento = 10f;
    public float tempoAteAMarca = 4f;
    public float tempoDoBalao = 4f;
    public float pausaAntesDoFim = 1.5f;

    [Header("Textos")]
    public string textoDoNivel = "Nível {0}";
    public string[] rotulosDosCriterios = { "capacete", "lugar certo", "sem batida" };
    public string[] rotulosComTempo = { "lugar certo", "sem batida", "no tempo" };
    public string botaoDoRoteiro = "Vamos!";
    public string recadoDoCapacete = "Antes de pedalar: o que vai na cabeça? Toque nele!";
    [Tooltip("{0} = a coisa escolhida, {1} = trecho do capacete.")]
    public string balaoDaCabecaErrada = "{0} não é capacete!\nCiclista: \"{1}\".";
    public string recadoDoCapaceteCerto = "Capacete na cabeça! Agora é ciclista.";
    public string recadoDaSaida = "Use as setas (ou toque nos botões) pra trocar de faixa.";
    [Tooltip("{0} = \"ciclovia\" ou \"ciclofaixa\", {1} = trecho do ciclista.")]
    public string balaoNaPistaDosCarros = "Aqui tem {0}! Pedale nela.\nCiclista: \"{1}\".";
    [Tooltip("{0} = trecho da calçada, {1} = onde a bicicleta vai.")]
    public string balaoNaCalcada = "Calçada: \"{0}\".\nA bicicleta vai {1}.";
    public string naCiclovia = "na ciclovia";
    public string naCiclofaixa = "na ciclofaixa";
    public string naRua = "na rua";
    public string recadoDoCanteiro = "Ali é canteiro! Espere a ciclovia chegar.";
    public string recadoDaBatida = "Opa! Bateu. Olhe o caminho e troque de faixa antes.";
    public string recadoDoEngarrafamento = "Os carros não andam e a ambulância não passa! Toque no engarrafamento.";
    [Tooltip("{0} = trecho do acúmulo, {1} = trecho da passagem.")]
    public string balaoDoEngarrafamento = "Engarrafamento: \"{0}\" e \"{1}\".\nA ambulância ficou presa. A bicicleta segue na ciclofaixa.";
    public string recadoDepoisDoEngarrafamento = "Siga pela ciclofaixa!";
    public string recadoDoTempo = "Acabou o tempo! Termine o caminho.";

    [Header("Sons")]
    public AudioClip somDoCapacete;
    public AudioClip somDeErro;
    public AudioClip somDeTrocarDeFaixa;
    public AudioClip somDaBatida;
    public AudioClip somDaCampainha;
    public AudioClip somDaSirene;
    public AudioClip somDaBuzina;
    public AudioClip somDeComemoracao;
}
