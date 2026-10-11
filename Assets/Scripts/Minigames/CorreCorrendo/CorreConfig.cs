using System;
using System.Collections.Generic;
using UnityEngine;

// Um tipo de obstáculo do "Corre, Correndo!". O id liga esta entrada ao modelo inativo da cena
// (CorrePista.modelos); a forma fica na cena, as regras e o texto ficam aqui.
[Serializable]
public class TipoDeObstaculo
{
    public string id;
    [Tooltip("Regra extra do nível que libera este obstáculo. Vazio = aparece em todos os níveis.")]
    public string regra = "";
    [Tooltip("Dá pra passar pulando por cima.")]
    public bool pulavel;
    [Tooltip("Dá pra passar deslizando por baixo.")]
    public bool deslizavel;
    [Tooltip("Metade do comprimento do obstáculo ao longo da pista, em metros.")]
    public float meioComprimento = 0.8f;
    [Tooltip("Chance relativa de ser sorteado.")]
    public float peso = 1f;
    [Tooltip("Por que é perigoso e o que fazer. Curto: aparece por um instante.")]
    public string aviso;
}

// Regras e apresentação do "Corre, Correndo!" que valem pra todos os níveis.
// O que muda de nível pra nível fica no LevelConfig (Niveis_CorreCorrendo.asset).
[CreateAssetMenu(fileName = "CorreCorrendo_Config", menuName = "Cidade Viva/Corre, Correndo! - Config")]
public class CorreConfig : ScriptableObject
{
    public const string IdCarroEmMovimento = "carro_em_movimento";

    [Header("Pista (metros)")]
    public float larguraDaFaixa = 2.4f;
    [Tooltip("Velocidade da corrida (m/s) quando o nível tem velocidade 1.")]
    public float velocidadeBase = 9f;
    public float distanciaDeSurgimento = 75f;
    [Tooltip("Trecho livre no começo, pra criança entender o que está acontecendo.")]
    public float distanciaLivreNoInicio = 28f;
    [Tooltip("Metros entre uma fileira de obstáculos e a próxima, se o nível não disser.")]
    public float espacoPadrao = 16f;
    [Tooltip("Nas sequências combinadas, a segunda fileira vem a esta distância da primeira.")]
    public float espacoDaSequencia = 8.5f;

    [Header("Jogador")]
    public float tempoDeTrocaDeFaixa = 0.12f;
    public float duracaoDoPulo = 0.75f;
    public float alturaDoPulo = 1.6f;
    public float duracaoDoDeslize = 0.8f;
    [Tooltip("Depois de bater, fica piscando e não bate de novo por este tempo.")]
    public float tempoInvencivel = 1.5f;
    public int escudosIniciais = 3;
    public int escudosMaximos = 3;

    [Header("Obstáculos")]
    public List<TipoDeObstaculo> tipos = new List<TipoDeObstaculo>();
    [Tooltip("A que distância do jogador o aviso do obstáculo aparece.")]
    public float distanciaDoAviso = 26f;
    [Tooltip("Quantas vezes o aviso de cada tipo aparece por partida.")]
    public int avisosPorTipo = 2;
    [Tooltip("Velocidade extra (m/s) do carro em movimento, vindo no sentido contrário.")]
    public float velocidadeDoCarroEmMovimento = 2.5f;

    [Header("Moedas e escudos")]
    public int moedasPorFila = 4;
    public float espacoEntreMoedas = 2.2f;
    [Range(0f, 1f)] public float chanceDeFilaDeMoedas = 0.8f;
    [Range(0f, 1f)] public float chanceDeMoedasNoPulo = 0.4f;
    [Tooltip("Fração das moedas que precisa ser coletada pra ganhar a estrela, se o nível não disser.")]
    [Range(0f, 1f)] public float fracaoDeMoedasParaEstrela = 0.6f;
    [Tooltip("Metros entre um escudo extra e o próximo (só aparece se faltar escudo).")]
    public float distanciaEntreEscudos = 90f;

    [Header("Pontuação")]
    public int pontosPorConcluir = 100;
    public int pontosPorMoeda = 10;
    public int pontosPorEscudo = 30;

    [Header("Textos")]
    public string tituloDoFim = "Você chegou!";
    public string dicaInicial = "Desvie, pule e deslize. Pegue as moedas!";
    public string formatoDaBatida = "Opa! {0}";
    public string dicaEscudo = "Escudo extra!";
    public string dicaChegada = "A chegada está perto!";
    public string dicaChuva = "Está chovendo: a pista está molhada.";
    public string formatoDaDistancia = "{0} m de {1} m";

    [Header("Sons")]
    public AudioClip musica;
    public AudioClip[] passos;
    public AudioClip pulo;
    public AudioClip deslizar;
    public AudioClip moeda;
    public AudioClip escudo;
    public AudioClip[] batida;
    public AudioClip tropeco;
    public AudioClip buzina;
    public AudioClip acerto;
    public AudioClip erro;

    public TipoDeObstaculo Tipo(string id)
    {
        foreach (TipoDeObstaculo t in tipos)
        {
            if (t.id == id)
            {
                return t;
            }
        }
        return null;
    }
}
