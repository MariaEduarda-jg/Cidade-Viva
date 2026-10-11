using System;
using UnityEngine;

// Grupos em que o jogo organiza os verbetes do Glotrânsito.
public enum GrupoDeVerbete
{
    Veiculos,
    ViasEEstruturas,
    Sinais,
    PessoasESeguranca,
    Situacao,
}

// Um verbete do glossário Glotrânsito (https://glotransito.com), como está em Assets/Data/verbetes.json.
// A definição é sempre a do glossário, copiada sem alteração: o jogo mostra esse texto, nunca uma versão reescrita.
[Serializable]
public class Verbete
{
    // Palavra sem acentos nem espaços (ex.: "faixa_de_pedestre"). É o que identifica o verbete no perfil salvo.
    public string id;
    public string palavra;
    public string definicao;
    // Nome de um GrupoDeVerbete (ex.: "ViasEEstruturas").
    public string grupo;
    public string urlDaIlustracao;
    // Nome do arquivo em Assets/Art/Verbetes; vazio quando o glossário não tem ilustração.
    public string arquivoDaIlustracao;
    public bool semIlustracao;

    // Preenchida pela VerbeteDatabase ao carregar; nula se o verbete não tem ilustração.
    [NonSerialized] public Sprite ilustracao;

    public GrupoDeVerbete Grupo
    {
        get
        {
            GrupoDeVerbete g;
            return Enum.TryParse(grupo, out g) ? g : GrupoDeVerbete.Situacao;
        }
    }
}
