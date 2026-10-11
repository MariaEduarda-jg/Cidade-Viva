using UnityEngine;
using UnityEngine.SceneManagement;

// Base de todo minigame. Cuida do que é igual em todos: escolher o nível, entregar a dificuldade
// do nível (LevelConfig), calcular as estrelas, registrar o resultado no GameProgress,
// mostrar a tela de fim de jogo e reiniciar.
// O manager de cada minigame herda desta classe, lê "Nivel" pra se configurar e chama
// FinalizarNivel (ou FinalizarJogo) quando a partida acaba.
public abstract class MinigameBase : MonoBehaviour
{
    [Header("Minigame")]
    // Mesmo nome usado na AreaTrigger do Hub (ex.: "Onde Eu Vou?").
    public string nomeDaArea;
    // Instância do prefab UI_FimDeJogo na cena.
    public TelaFimDeJogo telaFimDeJogo;

    [Header("Níveis")]
    // Asset com os níveis deste minigame. Sem ele, o minigame funciona como antes (um nível só).
    public LevelConfig niveis;
    // Instância do prefab UI_SelecaoDeNivel na cena.
    public TelaSelecaoDeNivel telaDeSelecao;

    [Header("Estrelas sem LevelConfig (terminar a partida já vale 1)")]
    public int pontosPara2Estrelas = 50;
    public int pontosPara3Estrelas = 90;

    public bool Finalizado { get; private set; }

    // Nível em jogo (começa em 1) e a dificuldade dele. "Nivel" nunca é nulo depois do Awake.
    public int NivelAtual { get; private set; } = 1;
    public NivelConfig Nivel { get; private set; } = new NivelConfig();

    // true enquanto a tela de seleção está aberta: a partida ainda não começou (o tempo fica parado).
    public bool EscolhendoNivel { get; private set; }

    public bool TemNiveis
    {
        get { return niveis != null && niveis.Quantidade > 0; }
    }

    public bool TemProximoNivel
    {
        get { return TemNiveis && NivelAtual < niveis.Quantidade; }
    }

    // O nível escolhido precisa sobreviver ao recarregar a cena (é assim que a partida começa
    // e que "Tentar de novo" repete o mesmo nível). Vale só pra cena em que foi escolhido.
    private static string cenaDoNivelEscolhido;
    private static int nivelEscolhido;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        EsquecerNivelEscolhido();
    }

    // Chamado ao sair pro Hub: da próxima vez que entrar no minigame, a seleção aparece de novo.
    public static void EsquecerNivelEscolhido()
    {
        cenaDoNivelEscolhido = null;
        nivelEscolhido = 0;
        Time.timeScale = 1f;
    }

    protected virtual void Awake()
    {
        if (telaFimDeJogo != null)
        {
            telaFimDeJogo.gameObject.SetActive(false);
        }

        if (!TemNiveis)
        {
            if (telaDeSelecao != null)
            {
                telaDeSelecao.gameObject.SetActive(false);
            }
            return;
        }

        bool jaEscolheu = cenaDoNivelEscolhido == SceneManager.GetActiveScene().name && nivelEscolhido >= 1;
        NivelAtual = jaEscolheu ? Mathf.Clamp(nivelEscolhido, 1, niveis.Quantidade) : 1;
        Nivel = niveis.Obter(NivelAtual);

        if (!jaEscolheu && telaDeSelecao != null)
        {
            EscolhendoNivel = true;
            Time.timeScale = 0f;
            telaDeSelecao.Mostrar(this);
        }
        else
        {
            Time.timeScale = 1f;
            if (telaDeSelecao != null)
            {
                telaDeSelecao.gameObject.SetActive(false);
            }
        }

        AplicarNivel(Nivel);
    }

    // Ponto de extensão: o manager pode sobrescrever pra ajustar a cena conforme o nível.
    // Roda no Awake da base. Quem preferir pode só ler a propriedade Nivel no próprio Start.
    protected virtual void AplicarNivel(NivelConfig nivel)
    {
    }

    // Começa (ou recomeça) a partida no nível pedido, se ele já estiver liberado.
    public void JogarNivel(int nivel)
    {
        if (!TemNiveis || nivel < 1 || nivel > niveis.Quantidade || !GameProgress.NivelDesbloqueado(nomeDaArea, nivel))
        {
            return;
        }

        string cena = SceneManager.GetActiveScene().name;
        cenaDoNivelEscolhido = cena;
        nivelEscolhido = nivel;
        Time.timeScale = 1f;
        SceneManager.LoadScene(cena);
    }

    public void ProximoNivel()
    {
        JogarNivel(NivelAtual + 1);
    }

    public void FinalizarJogo(int pontuacao)
    {
        FinalizarJogo(pontuacao, null);
    }

    // Forma antiga: só a pontuação. Com LevelConfig conta como nível concluído; as estrelas saem
    // do critério do nível (que, pra estes minigames, deve usar a medida Pontuacao).
    // titulo nulo ou vazio usa o título padrão do prefab.
    public virtual void FinalizarJogo(int pontuacao, string titulo)
    {
        FinalizarNivel(new ResultadoDoNivel { concluiu = true, pontuacao = pontuacao }, titulo);
    }

    // Forma completa: o minigame diz se a criança concluiu e o que mediu (erros, tempo, vidas...).
    public virtual void FinalizarNivel(ResultadoDoNivel resultado, string titulo = null)
    {
        if (Finalizado)
        {
            return;
        }

        Finalizado = true;
        int recordeAnterior = TemNiveis ? GameProgress.ObterRecordeDoNivel(nomeDaArea, NivelAtual) : GameProgress.ObterRecorde(nomeDaArea);
        int estrelas = CalcularEstrelas(resultado);
        GameProgress.FinalizarNivel(nomeDaArea, NivelAtual, resultado.pontuacao, estrelas, resultado.concluiu);

        if (telaFimDeJogo != null)
        {
            bool novoRecorde = resultado.concluiu && resultado.pontuacao > recordeAnterior;
            telaFimDeJogo.Mostrar(this, titulo, resultado.pontuacao, Mathf.Max(recordeAnterior, resultado.pontuacao), novoRecorde,
                estrelas, resultado.concluiu);
        }
    }

    // Com LevelConfig, quem decide é o critério do nível. Sem ele: 1 estrela por terminar,
    // 2 e 3 pelos limites de pontuação configurados na cena.
    protected virtual int CalcularEstrelas(ResultadoDoNivel resultado)
    {
        if (TemNiveis)
        {
            return Nivel.estrelas.Calcular(resultado);
        }
        if (!resultado.concluiu)
        {
            return 0;
        }
        if (resultado.pontuacao >= pontosPara3Estrelas)
        {
            return 3;
        }
        return resultado.pontuacao >= pontosPara2Estrelas ? 2 : 1;
    }

    // Recarrega a cena no mesmo nível.
    public virtual void ReiniciarJogo()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
