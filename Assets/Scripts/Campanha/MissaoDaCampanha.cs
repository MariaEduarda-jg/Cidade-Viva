using System;
using System.Collections.Generic;
using UnityEngine;

// Uma missão da campanha: um NPC, as falas e o nível de uma mecânica que ela abre.
[CreateAssetMenu(menuName = "Cidade Viva/Campanha: missão", fileName = "Missao")]
public class MissaoDaCampanha : ScriptableObject
{
    public string id;
    public QuemFala npc;
    [Tooltip("Frase curta no alto da tela enquanto a missão está aceita.")]
    public string objetivo;
    [Tooltip("2 ou 3 balões: o MOTIVO da missão. Nunca explicar verbete nem resumir definição (quem ensina é o cartão).")]
    [TextArea] public string[] falas = new string[0];
    [TextArea] public string agradecimento;

    [Header("O nível que a missão abre")]
    public string cena;
    [Tooltip("-1 = cena de um modo só. Obras: 0 Pontes e Passagens, 1 Planejar a rua. Sinais: 0 Motorista Mirim, 1 Sinaleiro.")]
    public int modo = -1;
    [Tooltip("Mesmo nomeDaArea do minigame (é onde o progresso do nível fica guardado).")]
    public string nomeDaArea;
    public int nivel = 1;

    [Header("No Hub")]
    [Tooltip("Objeto Area_* do Hub onde se entra pra jogar.")]
    public string areaDoHub;
    [Tooltip("O que se conserta no Hub quando a missão é feita (texto pra revisão; o objeto é Conserto_<id>).")]
    public string conserto;
}
