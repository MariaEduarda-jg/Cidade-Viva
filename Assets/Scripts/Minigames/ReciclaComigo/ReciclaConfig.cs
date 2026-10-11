using System;
using System.Collections.Generic;
using UnityEngine;

// Um item de lixo que pode surgir: nome mostrado, tipo da lixeira certa e cor.
[Serializable]
public class TipoDeLixo
{
    public string nome;
    public string tipo;
    public Color cor = Color.white;
}

// Aparência de uma lixeira, ligada à lixeira da cena pelo tipo.
[Serializable]
public class LixeiraConfig
{
    public string tipo;
    public string rotulo;
    public Color cor = Color.white;
    public Color corDaTampa = Color.gray;
}

// Regras e apresentação do "Recicla Comigo!".
[CreateAssetMenu(fileName = "ReciclaComigo_Config", menuName = "Cidade Viva/Recicla Comigo - Config")]
public class ReciclaConfig : ScriptableObject
{
    [Header("Regras")]
    public int totalDeItens = 10;
    public float intervaloEntreItens = 0.4f;

    [Header("Apresentação")]
    public List<TipoDeLixo> itens = new List<TipoDeLixo>();
    public List<LixeiraConfig> lixeiras = new List<LixeiraConfig>();
    // Onde os itens surgem, em relação ao centro da área dos itens.
    public float xMaximo = 650f;
    public float ySurgimento = 280f;
    public string formatoProgresso = "Lixo separado: {0}/{1}";
    public string tituloDoFim = "Cidade mais limpa!";

    public LixeiraConfig BuscarLixeira(string tipo)
    {
        return lixeiras.Find(l => l.tipo == tipo);
    }
}
