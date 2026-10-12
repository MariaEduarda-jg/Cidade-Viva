using System.Globalization;
using System.Text;
using UnityEngine;

// Progresso do jogador nos minigames. Todo o jogo lê e grava por aqui, sempre no perfil ativo
// (ver Perfis): por minigame, as estrelas e o recorde de cada nível, num arquivo JSON por perfil.
// O minigame é identificado pelo nome da área sem espaços, pontuação nem acentos
// (ex.: "Onde Eu Vou?" -> "OndeEuVou"). Os níveis começam em 1 e liberam em sequência.
public static class GameProgress
{
    // Registra o resultado de um nível. Se a criança não concluiu, nada muda (não há punição).
    // Tudo é "o melhor até agora": jogar pior depois não apaga estrelas nem recorde.
    public static void FinalizarNivel(string nomeDaArea, int nivel, int pontuacao, int estrelas, bool concluiu)
    {
        if (!concluiu)
        {
            Debug.Log("Minigame " + nomeDaArea + ", nível " + nivel + ": " + Perfis.Ativo.nome + " não concluiu (nada foi gravado).");
            return;
        }

        nivel = Mathf.Max(1, nivel);
        ProgressoDoMinigame m = Perfis.Ativo.Progresso(IdDaArea(nomeDaArea), true);
        Normalizar(m);
        while (m.estrelasPorNivel.Count < nivel)
        {
            m.estrelasPorNivel.Add(0);
            m.recordePorNivel.Add(0);
        }

        m.estrelasPorNivel[nivel - 1] = Mathf.Max(m.estrelasPorNivel[nivel - 1], Mathf.Clamp(estrelas, 1, 3));
        m.recordePorNivel[nivel - 1] = Mathf.Max(m.recordePorNivel[nivel - 1], pontuacao);
        m.niveisConcluidos = Mathf.Max(m.niveisConcluidos, nivel);
        m.recorde = Mathf.Max(m.recorde, pontuacao);
        m.estrelas = 0;
        foreach (int e in m.estrelasPorNivel)
        {
            m.estrelas += e;
        }
        Perfis.SalvarAtivo();

        Debug.Log("Minigame " + nomeDaArea + ", nível " + nivel + " concluído por " + Perfis.Ativo.nome + " com pontuação " + pontuacao
            + " (estrelas do nível: " + m.estrelasPorNivel[nivel - 1] + ", total: " + m.estrelas + ", recorde: " + m.recorde + ")");
    }

    // Formas antigas, pra minigame sem níveis: contam como nível 1.
    public static void FinalizarMinigame(string nomeDaArea, int pontuacao)
    {
        FinalizarNivel(nomeDaArea, 1, pontuacao, 1, true);
    }

    public static void FinalizarMinigame(string nomeDaArea, int pontuacao, int estrelas, int nivel = 1)
    {
        FinalizarNivel(nomeDaArea, nivel, pontuacao, estrelas, true);
    }

    // Nível 1 sempre aberto; os outros abrem quando o anterior é concluído.
    public static bool NivelDesbloqueado(string nomeDaArea, int nivel)
    {
        return nivel <= 1 || ObterNiveisConcluidos(nomeDaArea) >= nivel - 1;
    }

    public static int ObterEstrelasDoNivel(string nomeDaArea, int nivel)
    {
        ProgressoDoMinigame m = Ler(nomeDaArea);
        return m != null && nivel >= 1 && nivel <= m.estrelasPorNivel.Count ? m.estrelasPorNivel[nivel - 1] : 0;
    }

    public static int ObterRecordeDoNivel(string nomeDaArea, int nivel)
    {
        ProgressoDoMinigame m = Ler(nomeDaArea);
        return m != null && nivel >= 1 && nivel <= m.recordePorNivel.Count ? m.recordePorNivel[nivel - 1] : 0;
    }

    // Melhor pontuação em qualquer nível.
    public static int ObterRecorde(string nomeDaArea)
    {
        ProgressoDoMinigame m = Ler(nomeDaArea);
        return m != null ? m.recorde : 0;
    }

    // Soma das estrelas de todos os níveis.
    public static int ObterEstrelas(string nomeDaArea)
    {
        ProgressoDoMinigame m = Ler(nomeDaArea);
        return m != null ? m.estrelas : 0;
    }

    public static int ObterNiveisConcluidos(string nomeDaArea)
    {
        ProgressoDoMinigame m = Ler(nomeDaArea);
        return m != null ? m.niveisConcluidos : 0;
    }

    // "Completado" = concluiu pelo menos o nível 1 (é o que colore a área no Hub).
    public static bool FoiCompletado(string nomeDaArea)
    {
        return ObterNiveisConcluidos(nomeDaArea) > 0;
    }

    // ---- Álbum Glotrânsito: verbetes desbloqueados no perfil ativo (id = Verbete.id) ----

    // Avisa quem mostra o álbum (cena do álbum, botão do Hub) que o conjunto de verbetes desbloqueados mudou.
    public static event System.Action VerbetesMudaram;

    // Põe a figurinha do verbete no álbum. Devolve true se era nova (pra tela poder comemorar).
    public static bool DesbloquearVerbete(string idDoVerbete)
    {
        ProgressoDoVerbete v = Perfis.Ativo.EstadoDoVerbete(idDoVerbete, true);
        if (v.desbloqueado)
        {
            return false;
        }

        v.desbloqueado = true;
        Perfis.SalvarAtivo();
        Debug.Log("Verbete desbloqueado por " + Perfis.Ativo.nome + ": " + idDoVerbete);
        if (VerbetesMudaram != null)
        {
            VerbetesMudaram();
        }
        return true;
    }

    // Esvazia o álbum do perfil ativo. Só pra testes (menu Cidade Viva > Teste).
    public static void BloquearTodosOsVerbetes()
    {
        Perfis.Ativo.verbetes.Clear();
        Perfis.SalvarAtivo();
        if (VerbetesMudaram != null)
        {
            VerbetesMudaram();
        }
    }

    public static bool VerbeteDesbloqueado(string idDoVerbete)
    {
        ProgressoDoVerbete v = Perfis.Ativo.EstadoDoVerbete(idDoVerbete, false);
        return v != null && v.desbloqueado;
    }

    public static int ContarVerbetesDesbloqueados()
    {
        int total = 0;
        foreach (Verbete v in VerbeteDatabase.Instancia.Todos)
        {
            if (VerbeteDesbloqueado(v.id))
            {
                total++;
            }
        }
        return total;
    }

    static ProgressoDoMinigame Ler(string nomeDaArea)
    {
        ProgressoDoMinigame m = Perfis.Ativo.Progresso(IdDaArea(nomeDaArea), false);
        if (m != null)
        {
            Normalizar(m);
        }
        return m;
    }

    // Progresso gravado antes de existirem níveis (ou migrado do PlayerPrefs) só tem os totais:
    // vira "nível 1 concluído", com as estrelas e o recorde que havia.
    static void Normalizar(ProgressoDoMinigame m)
    {
        if (m.estrelasPorNivel.Count > 0 || m.niveisConcluidos <= 0)
        {
            return;
        }

        m.niveisConcluidos = 1;
        m.estrelas = Mathf.Clamp(m.estrelas, 1, 3);
        m.estrelasPorNivel.Add(m.estrelas);
        m.recordePorNivel.Add(m.recorde);
    }

    // Mantém só letras e dígitos, sem acento: "Semáforo Mágico" -> "SemaforoMagico".
    static string IdDaArea(string nomeDaArea)
    {
        var id = new StringBuilder();
        foreach (char c in nomeDaArea.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c))
                id.Append(c);
        }
        return id.ToString();
    }
}
