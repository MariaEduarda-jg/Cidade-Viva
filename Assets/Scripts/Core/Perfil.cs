using System;
using System.Collections.Generic;

// Dados de um jogador. É exatamente o que vai pro arquivo JSON do perfil.
[Serializable]
public class Perfil
{
    public string id;
    public string nome;
    // Índice do bichinho em IconesDePerfil.avatares.
    public int avatar;
    // Código secreto: 3 índices de IconesDePerfil.figuras, na ordem escolhida.
    public int[] codigo = new int[0];
    public List<ProgressoDoMinigame> minigames = new List<ProgressoDoMinigame>();

    // Perfil "Visitante": só existe na memória, nunca vira arquivo.
    [NonSerialized] public bool visitante;

    public ProgressoDoMinigame Progresso(string idDoMinigame, bool criarSeFaltar)
    {
        foreach (ProgressoDoMinigame m in minigames)
        {
            if (m.id == idDoMinigame)
            {
                return m;
            }
        }

        if (!criarSeFaltar)
        {
            return null;
        }

        var novo = new ProgressoDoMinigame { id = idDoMinigame };
        minigames.Add(novo);
        return novo;
    }
}

[Serializable]
public class ProgressoDoMinigame
{
    // Nome da área sem espaços, pontuação nem acentos (ex.: "OndeEuVou").
    public string id;
    // Número do nível mais alto já concluído (0 = nenhum). O nível seguinte é o último liberado.
    public int niveisConcluidos;
    // Soma das estrelas de todos os níveis.
    public int estrelas;
    // Melhor pontuação em qualquer nível.
    public int recorde;
    // Por nível (posição 0 = nível 1): melhor número de estrelas (1 a 3; 0 = não concluído) e melhor pontuação.
    public List<int> estrelasPorNivel = new List<int>();
    public List<int> recordePorNivel = new List<int>();
}
