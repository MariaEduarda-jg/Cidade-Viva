using System;
using System.Collections.Generic;
using UnityEngine;

// Campanha "Os 5 dias da reconstrução": uma camada POR CIMA das mecânicas (não muda nenhuma delas).
// Cada dia abre com uma conversa da mãe no Hub e tem missões; cada missão é um NPC que conversa,
// mostra o objetivo e abre UM nível de uma mecânica. Os dados são gerados pelo CampanhaConstrutor.

public enum QuemFala { Mae, Mecanico, Engenheira, Guarda, Vizinha }

[Serializable]
public class FalaDoNpc
{
    public QuemFala quem;
    public string nome;
    [Tooltip("Onde fica, pra frase \"Fale com ... na ...\".")]
    public string lugar;
}

// Progresso da campanha, guardado no perfil (perfil antigo, sem o campo, carrega com tudo zerado).
[Serializable]
public class ProgressoDaCampanha
{
    public bool iniciada;
    public bool tutorialFeito;
    [Tooltip("0 andar até a mãe, 1 falar com ela, 2 seguir a trilha, 3 jogando a 1ª missão, 4 abrir o Álbum.")]
    public int passoDoTutorial;
    [Tooltip("Último dia cuja conversa de abertura com a mãe já aconteceu.")]
    public int diaAberto;
    [Tooltip("Último dia cuja conversa de fim com a mãe já aconteceu.")]
    public int diaFechado;
    public string missaoAtiva = "";
    public List<string> missoesFeitas = new List<string>();
}
