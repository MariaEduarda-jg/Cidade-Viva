using System;
using System.Collections.Generic;
using UnityEngine;

// O que um objeto da pista é e como aparece. O id liga esta entrada ao modelo da cena
// (RunnerObjeto.id); a forma do modelo fica na cena, o resto vem daqui.
[Serializable]
public class RunnerObjetoConfig
{
    public string id;
    public TipoRunnerObjeto tipo = TipoRunnerObjeto.Obstaculo;
    // Só para coletáveis.
    public int pontos;
    public Color cor = Color.white;
    // Texto em cima do objeto, se o modelo tiver um (ex.: "BURACO").
    public string rotulo;
}

// Regras e apresentação de um runner ("Corre, Correndo!", "Pedal Seguro"): um asset por minigame.
[CreateAssetMenu(fileName = "Runner_Config", menuName = "Cidade Viva/Runner - Config")]
public class RunnerConfig : ScriptableObject
{
    [Header("Velocidade")]
    public float velocidadeInicial = 350f;
    public float aumentoDeVelocidade = 50f;
    public float intervaloDeAumento = 10f;
    public float velocidadeMaxima = 1000f;

    [Header("Pontuação")]
    public float pontosPorSegundo = 10f;

    [Header("Objetos da pista")]
    public List<RunnerObjetoConfig> objetos = new List<RunnerObjetoConfig>();
    [Range(0f, 1f)] public float chanceDeColetavel = 0.3f;
    [Range(0f, 1f)] public float chanceDePowerUp = 0.08f;
    public float distanciaEntreObjetos = 400f;

    [Header("Apresentação")]
    public Color corDaBatida = new Color(1f, 0.3f, 0.3f);
    public float duracaoDaBatida = 0.3f;
    public string formatoPontuacao = "Pontos: {0}";
    public string tituloDoFim = "Fim de jogo!";

    public RunnerObjetoConfig Buscar(string id)
    {
        return objetos.Find(o => o.id == id);
    }
}
