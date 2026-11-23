using System;
using System.Collections.Generic;
using UnityEngine;

// Todos os dias, os NPCs e as frases gerais da campanha (Assets/Resources/CampanhaDados.asset).
[CreateAssetMenu(menuName = "Cidade Viva/Campanha: dados", fileName = "CampanhaDados")]
public class CampanhaDados : ScriptableObject
{
    public DiaDaCampanha[] dias = new DiaDaCampanha[0];
    public FalaDoNpc[] npcs = new FalaDoNpc[0];

    [Header("Tutorial (Dia 1, com a mãe)")]
    public string objetivoAndarAteAMae = "Ande até a sua mãe (setas ou W A S D).";
    public string objetivoFalarComAMae = "Fale com a sua mãe: toque nela ou aperte E.";
    public string objetivoAbrirOAlbum = "Toque no Álbum pra ver a sua figurinha nova!";
    [TextArea] public string[] falasDoAlbum = new string[0];

    [Header("Frases gerais")]
    [Tooltip("{0} = nome de quem tem missão, {1} = lugar.")]
    public string objetivoFalarCom = "Fale com {0} {1}.";
    public string objetivoVoltarAMae = "Volte e fale com a sua mãe.";
    public string objetivoLivre = "Jogue de novo o que quiser!";
    [TextArea] public string[] falasDaMaeComMissao = new string[0];
    [TextArea] public string[] falasDoFimDaCampanha = new string[0];
    [TextArea] public string falaAntesDaMae = "Calma! Primeiro fale com a sua mãe. Ela está perto da praça.";
    [TextArea] public string falaSemMissaoHoje = "Ainda não é hora daqui. Fale com a sua mãe pra saber o que fazer hoje.";
    [TextArea] public string falaMissaoAceita = "É só entrar ali! Siga as setas.";
    [TextArea] public string falaJaAjudou = "Obrigado pela ajuda! Se quiser jogar de novo, é só entrar.";
    public string textoConversar = "Conversar (E)";
    public string textoSeguir = "Seguir";
    public string textoVamos = "Vamos!";
    public string textoOuvir = "Ouvir";
    public string textoPular = "Pular tutorial";
    [Tooltip("{0} = moedas.")]
    public string textoMoedas = "+{0} moedas!";

    public string NomeDe(QuemFala q)
    {
        foreach (FalaDoNpc n in npcs) if (n.quem == q) return n.nome;
        return q.ToString();
    }

    public string LugarDe(QuemFala q)
    {
        foreach (FalaDoNpc n in npcs) if (n.quem == q) return n.lugar;
        return "";
    }
}
