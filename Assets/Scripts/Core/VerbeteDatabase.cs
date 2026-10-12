using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// Todos os verbetes do Glotrânsito que o jogo ensina. Carrega Assets/Data/verbetes.json e liga cada
// verbete à ilustração de Assets/Art/Verbetes.
// O asset fica em Assets/Resources/VerbeteDatabase.asset (gerado pelo menu Cidade Viva > Verbetes),
// então qualquer cena acessa por VerbeteDatabase.Instancia, sem precisar de referência.
public class VerbeteDatabase : ScriptableObject
{
    public const string NomeNoResources = "VerbeteDatabase";

    [Tooltip("Assets/Data/verbetes.json")]
    public TextAsset arquivoJson;

    [Tooltip("Ilustrações de Assets/Art/Verbetes. O nome de cada sprite é o id do verbete.")]
    public Sprite[] ilustracoes = new Sprite[0];

    [Tooltip("Assets/Prefabs/UI_CartaoVerbete.prefab, aberto por CartaoVerbete.Mostrar.")]
    public CartaoVerbete prefabDoCartao;

    [Serializable]
    private class Arquivo
    {
        public string fonte;
        public string extraidoEm;
        public List<Verbete> verbetes = new List<Verbete>();
    }

    private static VerbeteDatabase instancia;

    [NonSerialized] private List<Verbete> verbetes;
    [NonSerialized] private Dictionary<string, Verbete> porChave;

    public static VerbeteDatabase Instancia
    {
        get
        {
            if (instancia == null)
            {
                instancia = Resources.Load<VerbeteDatabase>(NomeNoResources);
                if (instancia == null)
                {
                    Debug.LogError("VerbeteDatabase não encontrado em Resources. Rode o menu Cidade Viva > Verbetes > Atualizar banco de verbetes.");
                    instancia = CreateInstance<VerbeteDatabase>();
                }
            }
            return instancia;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        instancia = null;
    }

    public IReadOnlyList<Verbete> Todos
    {
        get
        {
            Carregar();
            return verbetes;
        }
    }

    public int Quantidade
    {
        get { return Todos.Count; }
    }

    // Busca pela palavra ("faixa de pedestre") ou pelo id ("faixa_de_pedestre"),
    // sem diferenciar maiúsculas nem acentos. Devolve null se não existir.
    public Verbete Buscar(string palavraOuId)
    {
        Carregar();
        Verbete v;
        return porChave.TryGetValue(Chave(palavraOuId), out v) ? v : null;
    }

    // Verbetes de um grupo, na ordem do arquivo.
    public List<Verbete> ListarPorGrupo(GrupoDeVerbete grupo)
    {
        var lista = new List<Verbete>();
        foreach (Verbete v in Todos)
        {
            if (v.Grupo == grupo)
            {
                lista.Add(v);
            }
        }
        return lista;
    }

    // Lê o JSON de novo (o Editor usa depois de atualizar o arquivo).
    public void Recarregar()
    {
        verbetes = null;
        Carregar();
    }

    private void Carregar()
    {
        if (verbetes != null)
        {
            return;
        }

        verbetes = new List<Verbete>();
        porChave = new Dictionary<string, Verbete>();
        if (arquivoJson == null)
        {
            Debug.LogError("VerbeteDatabase sem o arquivo verbetes.json ligado.");
            return;
        }

        Arquivo arquivo = JsonUtility.FromJson<Arquivo>(arquivoJson.text);
        if (arquivo == null || arquivo.verbetes == null)
        {
            Debug.LogError("verbetes.json ilegível.");
            return;
        }

        var sprites = new Dictionary<string, Sprite>();
        foreach (Sprite s in ilustracoes)
        {
            if (s != null)
            {
                sprites[s.name] = s;
            }
        }

        foreach (Verbete v in arquivo.verbetes)
        {
            sprites.TryGetValue(v.id, out v.ilustracao);
            verbetes.Add(v);
            porChave[Chave(v.id)] = v;
            porChave[Chave(v.palavra)] = v;
        }
    }

    // Minúsculas, sem acentos, e com qualquer separador (espaço, hífen, sublinhado) igualado.
    private static string Chave(string texto)
    {
        var sb = new StringBuilder();
        foreach (char c in (texto ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        }
        return sb.ToString();
    }
}
