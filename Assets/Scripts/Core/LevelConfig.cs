using System;
using System.Collections.Generic;
using UnityEngine;

// Os níveis de um minigame (um asset por minigame, em Assets/Configs/Niveis).
// O MinigameBase escolhe o nível e entrega o NivelConfig pro manager, que aplica os parâmetros.
[CreateAssetMenu(fileName = "Niveis_Minigame", menuName = "Cidade Viva/Níveis de minigame (LevelConfig)")]
public class LevelConfig : ScriptableObject
{
    public const int QuantidadePadrao = 5;

    public List<NivelConfig> niveis = new List<NivelConfig>();

    public int Quantidade
    {
        get { return niveis.Count; }
    }

    // nivel começa em 1. Fora da lista devolve o mais próximo, pra nunca faltar config.
    public NivelConfig Obter(int nivel)
    {
        if (niveis.Count == 0)
        {
            return new NivelConfig();
        }
        return niveis[Mathf.Clamp(nivel, 1, niveis.Count) - 1];
    }
}

// Dificuldade de um nível. Os campos são os mesmos pra todos os minigames; cada manager usa os
// que fazem sentido pra ele e ignora o resto. O que for específico demais vai em "extras".
[Serializable]
public class NivelConfig
{
    [Tooltip("Frase curta pra criança, mostrada na seleção de níveis e no começo da partida.")]
    public string objetivo = "";

    [Header("Dificuldade")]
    [Tooltip("Multiplicador de velocidade do minigame (1 = velocidade normal).")]
    public float velocidade = 1f;
    public int quantidadeDeObstaculos;
    [Tooltip("Tempo limite em segundos. 0 = sem limite.")]
    public float tempoEmSegundos;
    public int quantidadeDeItens;
    [Tooltip("Quantos erros a criança pode cometer antes de precisar tentar de novo. -1 = sem limite.")]
    public int errosPermitidos = -1;

    [Tooltip("Regras ligadas neste nível, por nome (ex.: \"contramao\"). Cada minigame define as suas.")]
    public List<string> regrasExtras = new List<string>();
    [Tooltip("Números específicos do minigame, por nome.")]
    public List<ParametroExtra> extras = new List<ParametroExtra>();

    [Header("Estrelas")]
    public CriterioDeEstrelas estrelas = new CriterioDeEstrelas();

    public bool TemRegra(string regra)
    {
        return regrasExtras.Contains(regra);
    }

    public float Extra(string nome, float padrao)
    {
        foreach (ParametroExtra p in extras)
        {
            if (p.nome == nome)
            {
                return p.valor;
            }
        }
        return padrao;
    }

    public bool TemLimiteDeErros
    {
        get { return errosPermitidos >= 0; }
    }

    public bool TemLimiteDeTempo
    {
        get { return tempoEmSegundos > 0f; }
    }
}

[Serializable]
public class ParametroExtra
{
    public string nome;
    public float valor;
}

// O que o minigame informa ao terminar um nível. Preencha o que o minigame mede; o resto fica zero.
[Serializable]
public struct ResultadoDoNivel
{
    // false = a criança não chegou ao fim (estourou erros ou tempo): sem estrelas, tenta de novo.
    public bool concluiu;
    public int pontuacao;
    public int erros;
    public float tempoGasto;
    public float tempoRestante;
    public int vidasRestantes;
}

public enum MedidaDeEstrelas
{
    Pontuacao,
    Erros,
    TempoGasto,
    TempoRestante,
    VidasRestantes,
}

// Regra das estrelas de um nível: concluir vale 1; a medida escolhida decide a 2ª e a 3ª.
// Em Erros e TempoGasto vale "no máximo X"; nas outras medidas, "pelo menos X".
[Serializable]
public class CriterioDeEstrelas
{
    public MedidaDeEstrelas medida = MedidaDeEstrelas.Pontuacao;
    public float para2Estrelas = 50f;
    public float para3Estrelas = 90f;

    public bool MenorEhMelhor
    {
        get { return medida == MedidaDeEstrelas.Erros || medida == MedidaDeEstrelas.TempoGasto; }
    }

    public int Calcular(ResultadoDoNivel r)
    {
        if (!r.concluiu)
        {
            return 0;
        }

        float valor = Valor(r);
        if (MenorEhMelhor)
        {
            return valor <= para3Estrelas ? 3 : (valor <= para2Estrelas ? 2 : 1);
        }
        return valor >= para3Estrelas ? 3 : (valor >= para2Estrelas ? 2 : 1);
    }

    private float Valor(ResultadoDoNivel r)
    {
        switch (medida)
        {
            case MedidaDeEstrelas.Erros: return r.erros;
            case MedidaDeEstrelas.TempoGasto: return r.tempoGasto;
            case MedidaDeEstrelas.TempoRestante: return r.tempoRestante;
            case MedidaDeEstrelas.VidasRestantes: return r.vidasRestantes;
            default: return r.pontuacao;
        }
    }

    // Texto pra mostrar à criança como ganhar 3 estrelas neste nível.
    public string ComoGanhar3()
    {
        switch (medida)
        {
            case MedidaDeEstrelas.Erros:
                return para3Estrelas <= 0f ? "3 estrelas: sem nenhum erro" : "3 estrelas: até " + para3Estrelas.ToString("0") + " erros";
            case MedidaDeEstrelas.TempoGasto:
                return "3 estrelas: em até " + para3Estrelas.ToString("0") + " segundos";
            case MedidaDeEstrelas.TempoRestante:
                return "3 estrelas: sobrando " + para3Estrelas.ToString("0") + " segundos";
            case MedidaDeEstrelas.VidasRestantes:
                return "3 estrelas: terminar com " + para3Estrelas.ToString("0") + " vidas";
            default:
                return "3 estrelas: " + para3Estrelas.ToString("0") + " pontos";
        }
    }
}
