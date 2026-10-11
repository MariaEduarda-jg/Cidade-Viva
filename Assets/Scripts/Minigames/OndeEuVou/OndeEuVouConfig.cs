using System;
using System.Collections.Generic;
using UnityEngine;

// Aparência de uma placa arrastável, ligada à placa da cena pelo tipo.
[Serializable]
public class PlacaConfig
{
    public string tipo;
    public string rotulo;
    public Color cor = Color.white;
}

// Regras e apresentação do "Onde Eu Vou?".
[CreateAssetMenu(fileName = "OndeEuVou_Config", menuName = "Cidade Viva/Onde Eu Vou - Config")]
public class OndeEuVouConfig : ScriptableObject
{
    [Header("Pontuação")]
    public int pontuacaoMaxima = 100;
    public int penalidadePorErro = 10;

    [Header("Apresentação")]
    public List<PlacaConfig> placas = new List<PlacaConfig>();
    public Color corAcerto = new Color(0.3f, 0.9f, 0.3f, 1f);
    public float duracaoPiscada = 0.6f;
    public string tituloDoFim = "Muito bem!";

    public PlacaConfig BuscarPlaca(string tipo)
    {
        return placas.Find(p => p.tipo == tipo);
    }
}
