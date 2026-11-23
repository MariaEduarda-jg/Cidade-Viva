using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Perfis de jogador, locais ao dispositivo (ou ao navegador, no WebGL). Não existe conta na nuvem.
// Cada perfil é um arquivo JSON em <Application.persistentDataPath>/perfis/perfil_<id>.json.
// O Visitante fica só na memória e some quando o jogo fecha.
public static class Perfis
{
    public const int TamanhoMaximoDoNome = 12;
    public const int TamanhoDoCodigo = 3;
    public const string NomeDoVisitante = "Visitante";

    private const string ChaveDeMigracao = "Perfis_ProgressoMigrado";

    // Minigames que o GameProgress antigo gravava em PlayerPrefs ("Completado_<Id>" / "Recorde_<Id>").
    private static readonly string[] IdsAntigos =
    {
        "OndeEuVou", "TravessiaSegura", "ReciclaComigo", "CorreCorrendo", "PedalSeguro",
        "SobeSobe", "SemaforoMagico", "GuardaOSinal", "OlhoVivo",
    };

    private static Perfil ativo;

    public static string Pasta
    {
        get
        {
            TrazerDaPastaAntiga();
            return Path.Combine(Application.persistentDataPath, "perfis");
        }
    }

    // Até 2026-11-18 o projeto se chamava "DefaultCompany" (companyName), e no Windows a pasta dos dados
    // leva esse nome: ...\AppData\LocalLow\<empresa>\<produto>. Com a empresa "Glotec UnB" a pasta mudou;
    // na primeira vez, os perfis da pasta antiga são copiados pra nova (os arquivos antigos ficam onde estão).
    // No navegador a pasta não depende da empresa, então não há o que copiar.
    public const string EmpresaAntiga = "DefaultCompany";
    private static bool pastaAntigaConferida;

    public static int TrazerDaPastaAntiga()
    {
        if (pastaAntigaConferida)
        {
            return 0;
        }
        pastaAntigaConferida = true;
#if UNITY_WEBGL && !UNITY_EDITOR
        return 0;
#else
        int copiados = 0;
        try
        {
            DirectoryInfo produto = new DirectoryInfo(Application.persistentDataPath.TrimEnd('/', Path.DirectorySeparatorChar));
            DirectoryInfo empresa = produto.Parent;
            if (empresa == null || empresa.Parent == null || empresa.Name == EmpresaAntiga)
            {
                return 0;
            }
            string antiga = Path.Combine(empresa.Parent.FullName, EmpresaAntiga, produto.Name, "perfis");
            if (!Directory.Exists(antiga))
            {
                return 0;
            }
            string nova = Path.Combine(Application.persistentDataPath, "perfis");
            Directory.CreateDirectory(nova);
            foreach (string arquivo in Directory.GetFiles(antiga, "perfil_*.json"))
            {
                string destino = Path.Combine(nova, Path.GetFileName(arquivo));
                if (!File.Exists(destino))
                {
                    File.Copy(arquivo, destino);
                    copiados++;
                }
            }
            if (copiados > 0)
            {
                Debug.Log("Perfis: " + copiados + " perfil(is) copiado(s) da pasta antiga (" + antiga + ").");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Perfis: não deu pra copiar os perfis da pasta antiga: " + e.Message);
        }
        return copiados;
#endif
    }

    public static bool TemPerfilAtivo
    {
        get { return ativo != null; }
    }

    // Perfil em uso. Se ninguém entrou (ex.: abrir a cena de um minigame direto no Editor),
    // cria um Visitante na hora, pra o jogo nunca ficar sem onde ler e gravar o progresso.
    public static Perfil Ativo
    {
        get
        {
            if (ativo == null)
            {
                Debug.Log("Nenhum perfil ativo: jogando como Visitante (o progresso não será guardado).");
                EntrarComoVisitante();
            }
            return ativo;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        ativo = null;
        pastaAntigaConferida = false;
    }

    public static List<Perfil> Listar()
    {
        var lista = new List<Perfil>();
        if (!Directory.Exists(Pasta))
        {
            return lista;
        }

        foreach (string arquivo in Directory.GetFiles(Pasta, "perfil_*.json"))
        {
            try
            {
                Perfil p = JsonUtility.FromJson<Perfil>(File.ReadAllText(arquivo));
                if (p != null && !string.IsNullOrEmpty(p.id))
                {
                    lista.Add(p);
                }
            }
            catch (Exception e)
            {
                // Arquivo estragado não pode impedir as outras crianças de entrar.
                Debug.LogWarning("Perfil ilegível, ignorado: " + arquivo + " (" + e.Message + ")");
            }
        }

        lista.Sort((a, b) => string.Compare(a.nome, b.nome, StringComparison.CurrentCultureIgnoreCase));
        return lista;
    }

    public static Perfil Criar(string nome, int avatar, int[] codigo)
    {
        var perfil = new Perfil
        {
            id = Guid.NewGuid().ToString("N"),
            nome = LimparNome(nome),
            avatar = avatar,
            codigo = (int[])codigo.Clone(),
        };
        MigrarPlayerPrefs(perfil);
        Salvar(perfil);
        return perfil;
    }

    public static string LimparNome(string nome)
    {
        nome = (nome ?? "").Trim();
        return nome.Length > TamanhoMaximoDoNome ? nome.Substring(0, TamanhoMaximoDoNome) : nome;
    }

    public static bool CodigoConfere(Perfil perfil, IList<int> tentativa)
    {
        if (perfil.codigo == null || tentativa == null || perfil.codigo.Length != tentativa.Count)
        {
            return false;
        }

        for (int i = 0; i < perfil.codigo.Length; i++)
        {
            if (perfil.codigo[i] != tentativa[i])
            {
                return false;
            }
        }
        return true;
    }

    public static void Entrar(Perfil perfil)
    {
        ativo = perfil;
    }

    public static void EntrarComoVisitante()
    {
        ativo = new Perfil { id = "visitante", nome = NomeDoVisitante, avatar = -1, visitante = true };
    }

    // Volta pro estado "ninguém entrou". O progresso do Visitante se perde aqui.
    public static void Sair()
    {
        ativo = null;
    }

    public static void SalvarAtivo()
    {
        if (ativo != null)
        {
            Salvar(ativo);
        }
    }

    public static void Salvar(Perfil perfil)
    {
        if (perfil.visitante)
        {
            return;
        }

        Directory.CreateDirectory(Pasta);
        File.WriteAllText(Path.Combine(Pasta, "perfil_" + perfil.id + ".json"), JsonUtility.ToJson(perfil, true));
        SincronizarArquivos();
    }

    // O jogo antigo guardava um único progresso em PlayerPrefs. Ele passa pro primeiro perfil
    // criado neste dispositivo (uma vez só); as chaves antigas ficam onde estão.
    private static void MigrarPlayerPrefs(Perfil perfil)
    {
        if (PlayerPrefs.GetInt(ChaveDeMigracao, 0) == 1)
        {
            return;
        }

        foreach (string id in IdsAntigos)
        {
            bool completado = PlayerPrefs.GetInt("Completado_" + id, 0) == 1;
            int recorde = PlayerPrefs.GetInt("Recorde_" + id, 0);
            if (!completado && recorde <= 0)
            {
                continue;
            }

            ProgressoDoMinigame m = perfil.Progresso(id, true);
            m.niveisConcluidos = Mathf.Max(m.niveisConcluidos, 1);
            m.estrelas = Mathf.Max(m.estrelas, 1);
            m.recorde = Mathf.Max(m.recorde, recorde);
        }

        PlayerPrefs.SetInt(ChaveDeMigracao, 1);
        PlayerPrefs.Save();
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void CidadeVivaSincronizarArquivos();
#endif

    // No navegador, persistentDataPath é um disco em memória espelhado no IndexedDB;
    // sem pedir a sincronização, o que foi gravado pode se perder ao fechar a aba.
    private static void SincronizarArquivos()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        CidadeVivaSincronizarArquivos();
#endif
    }
}
