using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Central de Sinais: dois modos na mesma cena, cada um com os seus 5 níveis e o seu progresso.
//   0 = "Motorista Mirim" (SinaisMotorista): a criança dirige um carrinho e obedece os semáforos.
//   1 = "Sinaleiro" (SinaisSinaleiro): a criança acende os semáforos de um cruzamento.
// Este gerente cuida do que é comum: modo e abas da seleção, relógio, erros, estrelas, balão,
// sons e o cartão do verbete (abre na primeira vez que cada regra é cumprida na partida).
// As regras de cada modo ficam no componente dele, que avisa aqui com Acerto, Erro e Concluir.
// Não existe derrota: erro e tempo estourado só custam estrela.
//
// O que cada campo do nível faz (ver também o topo de SinaisMotorista e de SinaisSinaleiro):
//   tempoEmSegundos   tempo pra ganhar a estrela "no tempo" (o relógio só anda com o jogo rodando).
//   regrasExtras      "dica_visivel": a dica com o trecho da definição fica na tela (nível 1).
public class SinaisManager : MinigameBase
{
    public const string RegraDicaVisivel = "dica_visivel";
    public const string IdDoVerde = "sinal_verde";
    public const string IdDoAmarelo = "sinal_amarelo";
    public const string IdDoVermelho = "sinal_vermelho";
    public const string IdDoSemaforo = "semaforo";
    public const string IdDoSemaforoDePedestre = "semaforo_de_pedestre";

    [Header("Central de Sinais")]
    public SinaisConfig config;
    public SinaisHUD hud;
    public AudioSource som;
    public Camera cameraDoJogo;

    [Header("Modos (0 = Motorista Mirim, 1 = Sinaleiro)")]
    public LevelConfig[] niveisPorModo = new LevelConfig[0];
    [Tooltip("Nome com que o progresso de cada modo é guardado no perfil.")]
    public string[] areasPorModo = new string[0];
    public GameObject[] mundosPorModo = new GameObject[0];
    public SinaisMotorista motorista;
    public SinaisSinaleiro sinaleiro;

    [Header("Efeitos")]
    public ParticleSystem estrelinhas;
    public ParticleSystem confete;
    public ParticleSystem fumaca;

    // Modo em jogo: 0 = Motorista Mirim, 1 = Sinaleiro.
    public static int Modo { get; private set; }

    // Campanha: a missão escolhe o modo antes de carregar a cena (não muda nada do jogo).
    public static void DefinirModo(int modo)
    {
        Modo = Mathf.Max(0, modo);
    }
    public int Erros { get; private set; }
    public float TempoGasto { get; private set; }
    public bool EstourouOTempo { get { return Nivel.TemLimiteDeTempo && TempoGasto > Nivel.tempoEmSegundos; } }
    // true enquanto o cartão do verbete está aberto (o tempo do jogo fica parado).
    public bool Pausado { get; private set; }
    public bool Rodando { get; private set; }
    public bool DicaVisivel { get; private set; }
    // Último balão de explicação (pra conferir nos testes).
    public string UltimoBalao { get; private set; }
    // Verbetes cujo cartão já abriu nesta partida.
    public ICollection<string> CartoesMostrados { get { return cartoesMostrados; } }

    private readonly HashSet<string> cartoesMostrados = new HashSet<string>();
    private bool relogioLigado;
    private bool avisouDoTempo;
    private Coroutine balaoAberto;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarModo()
    {
        Modo = 0;
    }

    // Antes de a base escolher o nível: cada modo tem os seus níveis e o seu progresso.
    protected override void Awake()
    {
        if (Modo < niveisPorModo.Length && Modo < areasPorModo.Length)
        {
            niveis = niveisPorModo[Modo];
            nomeDaArea = areasPorModo[Modo];
        }
        base.Awake();
        hud.Modos(EscolhendoNivel, Modo);
        for (int i = 0; i < mundosPorModo.Length; i++)
        {
            mundosPorModo[i].SetActive(i == Modo);
        }
    }

    // Abas da seleção de níveis: troca de modo e mostra os níveis dele.
    public void TrocarModo(int modo)
    {
        if (!EscolhendoNivel || modo == Modo || modo < 0 || modo >= niveisPorModo.Length)
        {
            return;
        }
        Modo = modo;
        EsquecerNivelEscolhido();
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
    }

    private void Start()
    {
        hud.Balao(null);
        hud.Dica(null);
        if (EscolhendoNivel)
        {
            hud.Controles(-1);
            return;
        }

        DicaVisivel = Nivel.TemRegra(RegraDicaVisivel);
        hud.Controles(Modo);
        hud.Nivel(string.Format(config.textoDoNivel, NivelAtual));
        AtualizarCriterios();
        Rodando = true;
        if (Modo == 0)
        {
            motorista.Comecar(this);
        }
        else
        {
            sinaleiro.Comecar(this);
        }
    }

    // ------------------------------------------------------------------ o que os modos avisam

    // O relógio só começa quando a criança faz a primeira coisa (ou, no Sinaleiro, quando os carros chegam).
    public void LigarRelogio()
    {
        relogioLigado = true;
    }

    // Regra cumprida. Na primeira vez de cada verbete na partida, abre o cartão dele (que põe no álbum).
    public void Acerto(string idDoVerbete, string recado, Vector3 onde)
    {
        if (!Rodando)
        {
            return;
        }
        Tocar(config.somDeAcerto);
        Soltar(estrelinhas, onde);
        if (!string.IsNullOrEmpty(recado))
        {
            hud.Recado(recado);
        }
        if (cartoesMostrados.Add(idDoVerbete))
        {
            StartCoroutine(MostrarCartao(idDoVerbete));
        }
    }

    private IEnumerator MostrarCartao(string idDoVerbete)
    {
        // Se outro cartão ainda está aberto, este espera a vez.
        while (Pausado)
        {
            yield return null;
        }
        Pausado = true;
        Time.timeScale = 0f;
        bool fechou = false;
        CartaoVerbete.Mostrar(idDoVerbete, () => fechou = true);
        while (!fechou)
        {
            yield return null;
        }
        Time.timeScale = 1f;
        Pausado = false;
    }

    // Regra quebrada: conta o erro e explica num balão, sem punição pesada.
    public void Erro(string balao, Vector3 onde)
    {
        if (!Rodando)
        {
            return;
        }
        Erros++;
        AtualizarCriterios();
        Tocar(config.somDeErro);
        Soltar(fumaca, onde);
        UltimoBalao = balao;
        if (balaoAberto != null)
        {
            StopCoroutine(balaoAberto);
        }
        balaoAberto = StartCoroutine(MostrarBalao(balao));
    }

    private IEnumerator MostrarBalao(string texto)
    {
        hud.Balao(texto);
        yield return new WaitForSeconds(config.tempoDoBalao);
        hud.Balao(null);
        balaoAberto = null;
    }

    public void Concluir(string recado, Vector3 onde)
    {
        if (!Rodando)
        {
            return;
        }
        Rodando = false;
        StartCoroutine(Fechar(recado, onde));
    }

    private IEnumerator Fechar(string recado, Vector3 onde)
    {
        // Um cartão aberto no último acerto fecha antes da tela de fim.
        while (Pausado)
        {
            yield return null;
        }
        hud.Recado(recado);
        hud.Dica(null);
        Tocar(config.somDeComemoracao);
        Soltar(confete, onde);
        yield return new WaitForSeconds(config.pausaAntesDoFim);
        int semErros = Erros == 0 ? 1 : 0;
        int noTempo = EstourouOTempo ? 0 : 1;
        FinalizarNivel(new ResultadoDoNivel
        {
            concluiu = true, pontuacao = Mathf.Max(40, 100 * (semErros + noTempo + 1) - 10 * Erros), erros = Erros, tempoGasto = TempoGasto,
            tempoRestante = Nivel.TemLimiteDeTempo ? Mathf.Max(0f, Nivel.tempoEmSegundos - TempoGasto) : 0f,
        });
    }

    // ------------------------------------------------------------------ estrelas e relógio

    private void AtualizarCriterios()
    {
        hud.Criterios(Erros == 0, !EstourouOTempo, config.rotulosDosCriterios, string.Format(config.textoDosErros, Erros));
    }

    // Dois critérios à vista da criança: sem erros e no tempo. 3 = os dois; 2 = um deles; 1 = concluiu.
    protected override int CalcularEstrelas(ResultadoDoNivel resultado)
    {
        if (!resultado.concluiu)
        {
            return 0;
        }
        return 1 + (Erros == 0 ? 1 : 0) + (EstourouOTempo ? 0 : 1);
    }

    private void Update()
    {
        if (EscolhendoNivel || Finalizado)
        {
            return;
        }
        if (Rodando && relogioLigado && !Pausado)
        {
            TempoGasto += Time.deltaTime;
            if (EstourouOTempo && !avisouDoTempo)
            {
                avisouDoTempo = true;
                AtualizarCriterios();
                hud.Recado(config.recadoDeTempoEsgotado);
            }
        }
        hud.Tempo(Nivel.TemLimiteDeTempo ? 1f - TempoGasto / Nivel.tempoEmSegundos : -1f);
    }

    // ------------------------------------------------------------------ sons e partículas

    public void Tocar(AudioClip clipe, float volume = 1f)
    {
        if (clipe != null && som != null)
        {
            som.PlayOneShot(clipe, volume);
        }
    }

    public static void Soltar(ParticleSystem particulas, Vector3 posicao)
    {
        if (particulas == null)
        {
            return;
        }
        particulas.transform.position = posicao;
        particulas.Play();
    }
}
