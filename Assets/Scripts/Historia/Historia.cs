using UnityEngine;
using UnityEngine.SceneManagement;

// Quando a abertura e o final aparecem (versão final do jogo):
//   - abertura: na primeira vez que o perfil entra no jogo (Perfil.viuAbertura); o menu também tem "Ver abertura".
//   - final: quando o perfil chega a todos os verbetes (38 de 38), uma vez só (Perfil.viuFinal), ao chegar no Hub;
//     o Álbum tem "Ver o final" pra rever.
// Os textos da história ficam nas cenas (CenaDaHistoria); não vêm do glossário.
public static class Historia
{
    public const string CenaDaAbertura = "Abertura";
    public const string CenaDoFinal = "Final";
    public const string CenaDoHub = "Hub";

    // Pra onde a história volta quando acaba (o Hub, ou o menu quando foi aberta pelo "Ver abertura").
    public static string DepoisDaHistoria { get; private set; } = CenaDoHub;
    // true quando a história foi aberta só pra rever (não grava nada no perfil).
    public static bool Revendo { get; private set; }
    // "Jogar sem conta" cria um Visitante novo a cada vez: ele lembra que já viu a abertura enquanto o jogo estiver aberto.
    private static bool visitanteViuAbertura;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        DepoisDaHistoria = CenaDoHub;
        Revendo = false;
        visitanteViuAbertura = false;
        SceneManager.sceneLoaded -= AoCarregar;
        SceneManager.sceneLoaded += AoCarregar;
    }

    // Chamado pelo menu depois de escolher o perfil (ou jogar sem conta).
    public static void EntrarNoJogo()
    {
        Perfil p = Perfis.Ativo;
        if (!p.viuAbertura && !(p.visitante && visitanteViuAbertura))
        {
            Abrir(CenaDaAbertura, CenaDoHub, false);
            return;
        }
        SceneManager.LoadScene(CenaDoHub);
    }

    // Botão "Ver abertura" do menu: volta pro menu no fim.
    public static void VerAbertura()
    {
        Abrir(CenaDaAbertura, SceneManager.GetActiveScene().name, true);
    }

    // Botão "Ver o final" do Álbum.
    public static void VerFinal()
    {
        Abrir(CenaDoFinal, CenaDoHub, true);
    }

    public static bool CompletouTudo
    {
        get
        {
            VerbeteDatabase banco = VerbeteDatabase.Instancia;
            return banco != null && banco.Quantidade > 0 && GameProgress.ContarVerbetesDesbloqueados() >= banco.Quantidade;
        }
    }

    // A cena da história acabou (ou a criança apertou Pular).
    public static void Terminou(bool final)
    {
        if (!Revendo)
        {
            if (final) Perfis.Ativo.viuFinal = true;
            else Perfis.Ativo.viuAbertura = true;
            if (!final && Perfis.Ativo.visitante) visitanteViuAbertura = true;
            Perfis.SalvarAtivo();
        }
        string destino = DepoisDaHistoria;
        Revendo = false;
        DepoisDaHistoria = CenaDoHub;
        Time.timeScale = 1f;
        SceneManager.LoadScene(destino);
    }

    private static void Abrir(string cena, string depois, bool revendo)
    {
        DepoisDaHistoria = depois;
        Revendo = revendo;
        Time.timeScale = 1f;
        SceneManager.LoadScene(cena);
    }

    // Gatilho do final: ao chegar no Hub com todos os verbetes e sem ter visto o final.
    private static void AoCarregar(Scene cena, LoadSceneMode modo)
    {
        if (cena.name == CenaDoHub && Perfis.TemPerfilAtivo && !Perfis.Ativo.viuFinal && CompletouTudo)
        {
            Abrir(CenaDoFinal, CenaDoHub, false);
        }
    }
}
