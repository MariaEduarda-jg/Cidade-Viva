using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Regras da campanha (estática): em que dia a criança está, que missões estão disponíveis,
// que áreas do Hub estão liberadas e como uma missão abre o nível dela.
//  - Missão feita = está na lista do perfil OU o nível dela já foi concluído (assim perfil antigo,
//    que já jogou, começa no dia certo).
//  - Dia atual = o primeiro dia com missão por fazer. As missões do dia ficam livres depois que a mãe
//    abre o dia; só uma missão do mesmo modo espera a anterior (ordem dos níveis).
//  - Áreas: liberadas se têm missão disponível ou feita; antes de falar com a mãe, nenhuma.
//    Quando os dados acabam (hoje só existe o Dia 1), tudo fica livre.
public static class Campanha
{
    private static CampanhaDados dados;

    public static CampanhaDados Dados
    {
        get
        {
            if (dados == null) dados = Resources.Load<CampanhaDados>("CampanhaDados");
            return dados;
        }
    }

    public static bool Existe => Dados != null && Dados.dias.Length > 0;

    public static ProgressoDaCampanha P
    {
        get
        {
            Perfil perfil = Perfis.Ativo;
            if (perfil.campanha == null) perfil.campanha = new ProgressoDaCampanha();
            if (perfil.campanha.missoesFeitas == null) perfil.campanha.missoesFeitas = new List<string>();
            if (perfil.campanha.missaoAtiva == null) perfil.campanha.missaoAtiva = "";
            return perfil.campanha;
        }
    }

    // Missão que a criança foi jogar (pra o Hub saber, ao voltar, quem agradece). Só na memória.
    public static string VoltandoDe { get; set; }
    public static int MoedasAntesDaMissao { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        dados = null;
        VoltandoDe = null;
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    // Último passo do tutorial: abrir o Álbum.
    private static void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        if (!Existe || cena.name != "Album_Glotransito") return;
        if (!P.tutorialFeito && P.passoDoTutorial >= 4)
        {
            P.tutorialFeito = true;
            Salvar();
        }
    }

    public static void Salvar()
    {
        if (!Perfis.Ativo.visitante) Perfis.SalvarAtivo();
    }

    // Primeira vez do perfil na campanha: perfil que já jogou não vê o tutorial e começa no dia certo.
    public static void Preparar()
    {
        if (!Existe || P.iniciada) return;
        P.iniciada = true;
        if (TemProgressoAntigo())
        {
            P.tutorialFeito = true;
            P.passoDoTutorial = 5;
            P.diaAberto = Mathf.Min(DiaAtual, Dados.dias.Length);
            P.diaFechado = DiaAtual - 1;
        }
        Salvar();
    }

    public static bool TemProgressoAntigo()
    {
        foreach (ProgressoDoMinigame m in Perfis.Ativo.minigames)
        {
            if (m.niveisConcluidos > 0) return true;
        }
        return false;
    }

    public static IEnumerable<MissaoDaCampanha> Todas()
    {
        if (!Existe) yield break;
        foreach (DiaDaCampanha d in Dados.dias)
            foreach (MissaoDaCampanha m in d.missoes)
                if (m != null) yield return m;
    }

    public static MissaoDaCampanha Buscar(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (MissaoDaCampanha m in Todas()) if (m.id == id) return m;
        return null;
    }

    public static int DiaDa(MissaoDaCampanha missao)
    {
        for (int i = 0; i < Dados.dias.Length; i++)
            foreach (MissaoDaCampanha m in Dados.dias[i].missoes)
                if (m == missao) return i + 1;
        return 0;
    }

    public static bool Feita(MissaoDaCampanha m)
    {
        return P.missoesFeitas.Contains(m.id) || GameProgress.ObterEstrelasDoNivel(m.nomeDaArea, m.nivel) > 0;
    }

    // 1..dias; dias + 1 = todos os dias que existem foram feitos.
    public static int DiaAtual
    {
        get
        {
            if (!Existe) return 1;
            for (int i = 0; i < Dados.dias.Length; i++)
                foreach (MissaoDaCampanha m in Dados.dias[i].missoes)
                    if (m != null && !Feita(m)) return i + 1;
            return Dados.dias.Length + 1;
        }
    }

    public static bool Acabou => Existe && DiaAtual > Dados.dias.Length;
    public static DiaDaCampanha Dia(int n) => Existe && n >= 1 && n <= Dados.dias.Length ? Dados.dias[n - 1] : null;

    // A mãe tem o que dizer: abrir o dia atual ou fechar o anterior.
    public static bool MaeTemConversa => Existe && (P.diaFechado < DiaAtual - 1 || (!Acabou && P.diaAberto < DiaAtual));

    public static bool Disponivel(MissaoDaCampanha m)
    {
        if (Feita(m) || DiaDa(m) != DiaAtual || P.diaAberto < DiaAtual) return false;
        foreach (MissaoDaCampanha antes in Todas())
        {
            if (antes == m) break;
            if (antes.cena == m.cena && antes.modo == m.modo && !Feita(antes)) return false;
        }
        return true;
    }

    public static MissaoDaCampanha Ativa
    {
        get
        {
            MissaoDaCampanha m = Buscar(P.missaoAtiva);
            return m != null && !Feita(m) ? m : null;
        }
    }

    // A próxima que a mãe sugere (ordem da tabela).
    public static MissaoDaCampanha Sugerida
    {
        get
        {
            foreach (MissaoDaCampanha m in Todas()) if (Disponivel(m)) return m;
            return null;
        }
    }

    public static MissaoDaCampanha DisponivelDe(QuemFala quem)
    {
        foreach (MissaoDaCampanha m in Todas()) if (m.npc == quem && Disponivel(m)) return m;
        return null;
    }

    public static bool AjudouAlgumaVez(QuemFala quem)
    {
        foreach (MissaoDaCampanha m in Todas()) if (m.npc == quem && Feita(m)) return true;
        return false;
    }

    // Pode entrar livremente numa área de campanha? Se não, devolve a fala de quem explica.
    public static bool AreaLiberada(string areaDoHub, out string motivo)
    {
        motivo = null;
        if (!Existe || Acabou) return true;
        if (!P.tutorialFeito && P.passoDoTutorial < 2 || P.diaAberto < 1)
        {
            motivo = Dados.falaAntesDaMae;
            return false;
        }
        foreach (MissaoDaCampanha m in Todas())
        {
            if (m.areaDoHub == areaDoHub && (Feita(m) || Disponivel(m))) return true;
        }
        motivo = Dados.falaSemMissaoHoje;
        return false;
    }

    public static void Aceitar(MissaoDaCampanha m)
    {
        P.missaoAtiva = m.id;
        Salvar();
    }

    public static void AbrirDia()
    {
        if (P.diaFechado < DiaAtual - 1) P.diaFechado = DiaAtual - 1;
        if (!Acabou) P.diaAberto = Mathf.Max(P.diaAberto, DiaAtual);
        Salvar();
    }

    public static void PularTutorial()
    {
        P.tutorialFeito = true;
        P.passoDoTutorial = 5;
        AbrirDia();
    }

    // Abre o nível da missão direto (sem a tela de seleção), no modo certo.
    public static void Jogar(MissaoDaCampanha m)
    {
        if (m.cena == "Minigame_Obras") ObrasManager.DefinirModo(Mathf.Max(0, m.modo));
        if (m.cena == "Minigame_Sinais") SinaisManager.DefinirModo(Mathf.Max(0, m.modo));
        MinigameBase.PreEscolherNivel(m.cena, m.nivel);
        VoltandoDe = m.id;
        MoedasAntesDaMissao = Loja.Moedas;
        SceneManager.LoadScene(m.cena);
    }

    public static void Concluir(MissaoDaCampanha m)
    {
        if (!P.missoesFeitas.Contains(m.id)) P.missoesFeitas.Add(m.id);
        if (P.missaoAtiva == m.id) P.missaoAtiva = "";
        Salvar();
    }
}
