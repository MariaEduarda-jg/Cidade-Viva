using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

// Obras da Cidade: a enchente cortou os caminhos. Em cada problema há um obstáculo (rio, morro ou via)
// e alguém que precisa passar; a criança escolhe uma estrutura na barra, lê a definição e toca no
// lugar que brilha. A obra sobe e quem passa testa: se a estrutura serve pelo que a definição diz,
// atravessa e comemora; se não serve, para antes e um balão explica com um trecho da definição.
// Não existe derrota: erro, dica e tempo estourado só custam estrela.
//
// Quem decide se a estrutura serve são as regras da ObrasConfig (derivadas das definições), e não o
// campo "correta" do problema; o construtor confere que os dois dizem a mesma coisa.
//
// O que cada campo do nível faz aqui:
//   tempoEmSegundos        tempo pro nível inteiro (0 = sem relógio). Só começa na primeira escolha e
//                          para enquanto o cartão do verbete está aberto.
//   regrasExtras
//     "dica_visivel"       a dica fica sempre na tela e não custa estrela (nível 1).
// Dois modos, cada um com os seus 5 níveis e o seu progresso: 0 = "Pontes e Passagens" (obstáculo entre
// dois pontos) e 1 = "Planejar a rua" (que via liga dois lugares, e o que acrescentar numa rua pronta).
// As abas da seleção de níveis trocam de modo (TrocarModo); o modo escolhido vale até sair do jogo.
// Os problemas de cada nível são os assets ObraProblema com aquele "modo" e "nivel" (Assets/Resources/Obras).
// Os marcados com "rotaDaAmbulancia" ficam em fila e são atravessados pela mesma ambulância, que no
// fim chega ao hospital; os outros ficam lado a lado.
public class ObrasManager : MinigameBase
{
    public const string RegraDicaVisivel = "dica_visivel";
    public const string IdDaFaixaDePedestre = "faixa_de_pedestre";

    [Header("Obras da Cidade")]
    public ObrasConfig config;
    public ObrasHUD hud;
    public AudioSource som;
    public Camera cameraDoJogo;
    [Tooltip("Pai dos lotes montados em tempo de jogo.")]
    public Transform mapa;

    [Header("Modelos inativos da cena")]
    public GameObject modeloDoLote;
    public GameObject modeloDoRio;
    public GameObject modeloDoMorro;
    public GameObject modeloDaAvenida;
    public GameObject modeloDaRua;
    public ObraLocal modeloDoLocal;
    public GameObject modeloDoHospital;
    public ObraEstrutura[] modelosDeEstrutura;
    [Tooltip("Cenários do modo Planejar a rua, um por situação.")]
    public List<ModeloDaSituacao> modelosDeSituacao = new List<ModeloDaSituacao>();

    [Header("Modos (0 = Pontes e Passagens, 1 = Planejar a rua)")]
    public LevelConfig[] niveisPorModo = new LevelConfig[0];
    [Tooltip("Nome com que o progresso de cada modo é guardado no perfil.")]
    public string[] areasPorModo = new string[0];

    [Header("Efeitos")]
    public ParticleSystem poeira;
    public ParticleSystem confete;
    public ParticleSystem estrelinhas;

    public enum Etapa
    {
        Parada,
        Escolhendo,
        Construindo,
        Fim,
    }

    [System.Serializable]
    public class ModeloDaSituacao
    {
        public ObstaculoDaObra situacao;
        public GameObject modelo;
    }

    // Um problema montado no mapa.
    public class Lote
    {
        // Outros veículos que esperam junto (problema do estacionamento).
        public List<Transform> figurantes = new List<Transform>();
        public ObraProblema problema;
        public Transform raiz;
        public Transform quem;
        public Animator animator;
        public ObraTransito transito;
        public ObraLocal local;
        public ObraEstrutura obra;
        public bool resolvido;
    }

    // Modo em jogo: 0 = Pontes e Passagens, 1 = Planejar a rua.
    public static int Modo { get; private set; }

    // Campanha: a missão escolhe o modo antes de carregar a cena (não muda nada do jogo).
    public static void DefinirModo(int modo)
    {
        Modo = Mathf.Max(0, modo);
    }
    public Etapa EtapaAtual { get; private set; }
    public IList<Lote> Lotes { get { return lotes; } }
    public int IndiceDoProblema { get; private set; } = -1;
    public ObraProblema ProblemaAtual { get { return IndiceDoProblema >= 0 ? lotes[IndiceDoProblema].problema : null; } }
    // Id do verbete da estrutura escolhida na barra (null = nenhuma).
    public string EstruturaEscolhida { get; private set; }
    public int Erros { get; private set; }
    public int Pontuacao { get; private set; }
    public int Resolvidos { get { return lotes.Count(l => l.resolvido); } }
    public bool UsouDica { get; private set; }
    public bool EstourouOTempo { get; private set; }
    public float TempoRestante { get; private set; }
    // Último balão de explicação mostrado (pra conferir nos testes).
    public string UltimoBalao { get; private set; }
    public bool AmbulanciaChegou { get; private set; }

    private readonly List<Lote> lotes = new List<Lote>();
    private readonly HashSet<string> cartoesMostrados = new HashSet<string>();
    private Transform hospital;
    private bool dicaVisivel;
    private bool relogioLigado;
    private int errosDoProblema;
    private float giro;
    private float inclinacao;
    private Vector3 foco;
    private Vector3 toqueInicial;
    private bool toqueNaInterface;
    private bool arrastou;
    private static readonly int Velocidade = Animator.StringToHash("Velocidade");

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
        if (EscolhendoNivel)
        {
            return;
        }

        dicaVisivel = Nivel.TemRegra(RegraDicaVisivel);
        TempoRestante = Nivel.tempoEmSegundos;
        inclinacao = config.inclinacaoDaCamera;
        MontarMapa();
        if (lotes.Count == 0)
        {
            Debug.LogError("Obras da Cidade: nenhum ObraProblema com nivel = " + NivelAtual + " em Resources/" + ObraProblema.PastaNoResources + ".");
            return;
        }

        hud.Nivel(string.Format(config.textoDoNivel, NivelAtual));
        hud.Balao(null);
        AtualizarCriterios();
        EtapaAtual = Etapa.Escolhendo;
        SelecionarProblema(0);
        foco = AlvoDaCamera();
        PosicionarCamera();
    }

    // ------------------------------------------------------------------ mapa

    private void MontarMapa()
    {
        List<ObraProblema> problemas = Resources.LoadAll<ObraProblema>(ObraProblema.PastaNoResources)
            .Where(p => p.modo == Modo && p.nivel == NivelAtual).OrderBy(p => p.ordem).ToList();
        int emFila = problemas.Count(p => p.rotaDaAmbulancia);
        int aoLado = problemas.Count - emFila;
        int j = 0, k = 0;
        Transform ambulancia = null;

        foreach (ObraProblema p in problemas)
        {
            Vector3 pos;
            if (p.rotaDaAmbulancia)
            {
                pos = new Vector3(0f, 0f, k++ * config.comprimentoDoLote);
            }
            else
            {
                float x = emFila > 0 ? (j + 1) * config.larguraDoLote : (j - (aoLado - 1) * 0.5f) * config.larguraDoLote;
                pos = new Vector3(x, 0f, 0f);
                j++;
            }

            var lote = new Lote { problema = p };
            GameObject raiz = Instantiate(modeloDoLote, mapa);
            raiz.name = "Lote_" + p.name;
            raiz.transform.localPosition = pos;
            raiz.SetActive(true);
            lote.raiz = raiz.transform;
            // Pontas do caminho: no modo das pontes dependem de quem passa; no de planejar a rua, da situação.
            bool pontes = p.obstaculo <= ObstaculoDaObra.Via;
            bool veiculo = p.quemPassa == QuemPassaNaObra.Veiculo;
            lote.raiz.Find("Caminho_Veiculo").gameObject.SetActive(pontes ? veiculo : TemPontasDeAsfalto(p.obstaculo));
            lote.raiz.Find("Caminho_Pedestre").gameObject.SetActive(pontes && !veiculo);
            lote.raiz.Find("Enfeites").gameObject.SetActive(pontes);

            GameObject obstaculo = Instantiate(ModeloDoCenario(p), lote.raiz);
            obstaculo.name = "Obstaculo";
            obstaculo.transform.localPosition = Vector3.zero;
            obstaculo.SetActive(true);
            lote.transito = obstaculo.GetComponentInChildren<ObraTransito>();
            Transform figurantes = obstaculo.transform.Find("Figurantes");
            if (figurantes != null)
            {
                foreach (Transform f in figurantes)
                {
                    lote.figurantes.Add(f);
                }
            }

            lote.local = Instantiate(modeloDoLocal, lote.raiz);
            lote.local.name = "Local";
            lote.local.transform.localPosition = Vector3.zero;
            lote.local.indiceDoProblema = lotes.Count;
            lote.local.gameObject.SetActive(true);

            if (p.rotaDaAmbulancia && ambulancia != null)
            {
                lote.quem = ambulancia;
            }
            else
            {
                lote.quem = Instantiate(p.modeloDeQuemPassa, mapa).transform;
                lote.quem.name = "Quem_Passa_" + p.name;
                lote.quem.position = lote.raiz.TransformPoint(XDeQuemPassa(p), 0f, config.zDaEspera);
                lote.quem.rotation = Quaternion.identity;
                if (p.rotaDaAmbulancia)
                {
                    ambulancia = lote.quem;
                }
            }
            lote.animator = lote.quem.GetComponentInChildren<Animator>();
            lotes.Add(lote);
        }

        if (emFila > 0)
        {
            hospital = Instantiate(modeloDoHospital, mapa).transform;
            hospital.name = "Hospital";
            hospital.localPosition = new Vector3(0f, 0f, (emFila - 0.5f) * config.comprimentoDoLote);
            hospital.gameObject.SetActive(true);
        }
    }

    private GameObject ModeloDoCenario(ObraProblema p)
    {
        switch (p.obstaculo)
        {
            case ObstaculoDaObra.Rio: return modeloDoRio;
            case ObstaculoDaObra.Morro: return modeloDoMorro;
            case ObstaculoDaObra.Via: return p.ruaComum ? modeloDaRua : modeloDaAvenida;
        }
        return modelosDeSituacao.First(m => m.situacao == p.obstaculo).modelo;
    }

    // Nas ligações (via, via rural, via urbana) e na beira da rua, as duas pontas do lote já são rua.
    private static bool TemPontasDeAsfalto(ObstaculoDaObra situacao)
    {
        return situacao == ObstaculoDaObra.CampoECidade || situacao == ObstaculoDaObra.DentroDaCidade
            || situacao == ObstaculoDaObra.LugarQualquer || situacao == ObstaculoDaObra.BeiraDaRua;
    }

    // Em que lado do lote (x) quem passa espera: no meio, na beira da rua ou na pista de ida.
    public float XDeQuemPassa(ObraProblema p)
    {
        return p.obstaculo == ObstaculoDaObra.BeiraDaRua ? config.xDaBeira
            : p.obstaculo == ObstaculoDaObra.MudarDeSentido ? config.xDaPistaDeIda : 0f;
    }

    // Onde a obra nasce: no meio do lote ou na beira da rua.
    public float XDaObra(ObraProblema p)
    {
        return p.obstaculo == ObstaculoDaObra.BeiraDaRua ? config.xDaBeira : 0f;
    }

    // ------------------------------------------------------------------ ações da criança

    // Passa a cuidar de outro problema (tocando no lugar dele). Devolve false se ainda não pode.
    public bool SelecionarProblema(int indice)
    {
        if (EtapaAtual != Etapa.Escolhendo || indice < 0 || indice >= lotes.Count || lotes[indice].resolvido)
        {
            return false;
        }
        string motivo = MotivoPraNaoEscolher(indice);
        if (motivo != null)
        {
            Tocar(config.somDeBloqueio);
            hud.Recado(motivo);
            return false;
        }

        IndiceDoProblema = indice;
        EstruturaEscolhida = null;
        errosDoProblema = 0;
        for (int i = 0; i < lotes.Count; i++)
        {
            lotes[i].local.gameObject.SetActive(!lotes[i].resolvido);
            lotes[i].local.Destacar(i == indice);
        }

        ObraProblema p = ProblemaAtual;
        hud.Problema(string.IsNullOrEmpty(p.enunciado) ? string.Format(config.enunciado, p.nomeDeQuemPassa, config.NomeDoObstaculo(p)) : p.enunciado,
            string.Format(config.textoDoContador, Resolvidos + 1, lotes.Count));
        hud.Opcoes(VerbetesDasOpcoes(), -1);
        hud.Definicao(null, false);
        hud.BarraVisivel(true, !dicaVisivel);
        hud.Dica(dicaVisivel ? string.Format(config.dica, p.trechoDaDefinicao) : null);
        hud.Recado(config.recadoDeEscolher);
        return true;
    }

    // Na fila da ambulância, os trechos são resolvidos na ordem em que ela passa.
    public string MotivoPraNaoEscolher(int indice)
    {
        if (!lotes[indice].problema.rotaDaAmbulancia)
        {
            return null;
        }
        for (int i = 0; i < indice; i++)
        {
            if (lotes[i].problema.rotaDaAmbulancia && !lotes[i].resolvido)
            {
                return config.recadoDaAmbulanciaAntes;
            }
        }
        return null;
    }

    // Botão da barra (ou teclas 1 a 4): escolhe a estrutura e mostra a definição dela.
    public void EscolherOpcao(int indice)
    {
        if (EtapaAtual != Etapa.Escolhendo || ProblemaAtual == null || indice < 0 || indice >= ProblemaAtual.opcoes.Length)
        {
            return;
        }
        relogioLigado = true;
        EstruturaEscolhida = ProblemaAtual.opcoes[indice];
        Tocar(config.somDeEscolher);
        hud.Opcoes(VerbetesDasOpcoes(), indice);
        hud.Definicao(VerbeteDatabase.Instancia.Buscar(EstruturaEscolhida), Narracao.Voz.Disponivel);
        hud.Recado(config.recadoDeConstruir);
    }

    public void EscolherEstrutura(string idDoVerbete)
    {
        if (ProblemaAtual != null)
        {
            EscolherOpcao(System.Array.IndexOf(ProblemaAtual.opcoes, idDoVerbete));
        }
    }

    // Toque no lugar destacado de um problema: troca de problema ou constrói no da vez.
    public void TocarNoLocal(int indice)
    {
        if (EtapaAtual != Etapa.Escolhendo || indice < 0 || indice >= lotes.Count || lotes[indice].resolvido)
        {
            return;
        }
        if (indice != IndiceDoProblema)
        {
            SelecionarProblema(indice);
            return;
        }
        Construir();
    }

    // Constrói a estrutura escolhida no problema da vez (Enter/Espaço fazem o mesmo).
    public void Construir()
    {
        if (EtapaAtual != Etapa.Escolhendo || ProblemaAtual == null)
        {
            return;
        }
        if (string.IsNullOrEmpty(EstruturaEscolhida))
        {
            // Ação que ainda não dá: explica o que fazer, sem contar erro.
            Tocar(config.somDeBloqueio);
            hud.Recado(config.recadoSemEstrutura);
            return;
        }
        StartCoroutine(Testar(lotes[IndiceDoProblema], EstruturaEscolhida));
    }

    public void PedirDica()
    {
        if (EtapaAtual != Etapa.Escolhendo || ProblemaAtual == null || dicaVisivel)
        {
            return;
        }
        UsouDica = true;
        Tocar(config.somDeDica);
        hud.Dica(string.Format(config.dica, ProblemaAtual.trechoDaDefinicao));
        AtualizarCriterios();
    }

    // A definição só é lida em voz alta quando a criança pede (o jogo roda em sala de aula).
    public void OuvirDefinicao()
    {
        Verbete v = string.IsNullOrEmpty(EstruturaEscolhida) ? null : VerbeteDatabase.Instancia.Buscar(EstruturaEscolhida);
        if (v != null && Narracao.Voz.Disponivel)
        {
            Narracao.Voz.Falar(v.definicao);
        }
    }

    // A estrutura serve pra este problema? Decide pelas regras tiradas das definições.
    public bool Serve(string idDoVerbete, ObraProblema p)
    {
        RegraDaEstrutura regra = config.Regra(idDoVerbete);
        return regra != null && regra.Serve(p.obstaculo, p.quemPassa);
    }

    // Texto do balão pra uma estrutura que não serve: o trecho da definição dela e o que há no lugar.
    public string Explicacao(string idDoVerbete, ObraProblema p)
    {
        RegraDaEstrutura regra = config.Regra(idDoVerbete);
        Verbete v = VerbeteDatabase.Instancia.Buscar(idDoVerbete);
        string palavra = v != null ? v.palavra : idDoVerbete;
        if (regra == null)
        {
            return palavra;
        }
        if (!regra.VenceObstaculo(p.obstaculo))
        {
            return string.Format(config.balaoDeObstaculoErrado, palavra, regra.trechoDoObstaculo, config.OQueHaAqui(p.obstaculo));
        }
        // Via rural e via urbana não dizem quem passa: isso vem da definição de via.
        Verbete deQuem = string.IsNullOrEmpty(regra.verbeteDoTrechoDeQuem) ? null : VerbeteDatabase.Instancia.Buscar(regra.verbeteDoTrechoDeQuem);
        return string.Format(config.balaoDeQuemErrado, deQuem != null ? deQuem.palavra : palavra, regra.trechoDeQuemPassa, config.QuemEstaPassando(p.quemPassa));
    }

    private Verbete[] VerbetesDasOpcoes()
    {
        return ProblemaAtual.opcoes.Select(id => VerbeteDatabase.Instancia.Buscar(id)).ToArray();
    }

    // ------------------------------------------------------------------ obra e teste

    private IEnumerator Testar(Lote lote, string idDoVerbete)
    {
        EtapaAtual = Etapa.Construindo;
        ObraProblema p = lote.problema;
        hud.BarraVisivel(false, false);
        hud.Recado(config.recadoDaObra);
        lote.local.gameObject.SetActive(false);

        // A obra aparece: peças subindo, poeira e som.
        ObraEstrutura modelo = modelosDeEstrutura.First(m => m.idDoVerbete == idDoVerbete);
        lote.obra = Instantiate(modelo, lote.raiz);
        lote.obra.name = "Obra_" + idDoVerbete;
        lote.obra.transform.localPosition = new Vector3(XDaObra(p), 0f, 0f);
        lote.obra.gameObject.SetActive(true);
        Tocar(config.somDeObra);
        Soltar(poeira, lote.raiz.TransformPoint(0f, 0.3f, -4f));
        yield return lote.obra.Aparecer(config.tempoDaObra);
        Soltar(poeira, lote.raiz.TransformPoint(0f, 0.3f, 4f));

        // Quem passa chega perto pra testar.
        yield return Mover(lote, new[] { lote.raiz.TransformPoint(XDeQuemPassa(p), 0f, config.zDaParada) });

        if (Serve(idDoVerbete, p))
        {
            bool esperouOsCarros = idDoVerbete == IdDaFaixaDePedestre && lote.transito != null;
            if (esperouOsCarros)
            {
                // "quando o veículo para": os carros param antes da faixa e só então o pedestre atravessa.
                lote.transito.Parado = true;
                while (!lote.transito.FaixaLivre)
                {
                    yield return null;
                }
            }

            var pontos = lote.obra.caminho.Select(t => t.position).ToList();
            if (!lote.obra.caminhoCompleto)
            {
                pontos.Add(lote.raiz.TransformPoint(0f, 0f, config.zDaChegada));
            }
            yield return Mover(lote, pontos);
            if (lote.obra.vagas.Length > 0 && lote.figurantes.Count > 0)
            {
                // Estacionamento: os outros veículos também são guardados, cada um na sua vaga.
                yield return Guardar(lote);
            }
            if (esperouOsCarros)
            {
                lote.transito.Parado = false;
            }

            lote.resolvido = true;
            Pontuacao += Mathf.Max(config.minimoPorProblema, config.pontosPorProblema - config.descontoPorErro * errosDoProblema);
            Tocar(config.somDeAcerto);
            Soltar(confete, lote.quem.position + Vector3.up * 1.5f);
            hud.Recado(string.Format(config.recadoDeAcerto, p.nomeDeQuemPassa));
            yield return Comemorar(lote.quem);

            // A primeira vez que cada estrutura dá certo abre o cartão do verbete (que põe no álbum).
            if (cartoesMostrados.Add(idDoVerbete))
            {
                bool fechou = false;
                CartaoVerbete.Mostrar(idDoVerbete, () => fechou = true);
                while (!fechou)
                {
                    yield return null;
                }
            }

            // A ambulância já segue até o próximo trecho.
            Lote proximoDaFila = p.rotaDaAmbulancia ? lotes.FirstOrDefault(l => l.problema.rotaDaAmbulancia && !l.resolvido) : null;
            if (proximoDaFila != null)
            {
                yield return Mover(lote, new[] { proximoDaFila.raiz.TransformPoint(0f, 0f, config.zDaEspera) });
            }

            EtapaAtual = Etapa.Escolhendo;
            int proximo = lotes.FindIndex(l => !l.resolvido && MotivoPraNaoEscolher(lotes.IndexOf(l)) == null);
            if (proximo < 0)
            {
                yield return Concluir();
            }
            else
            {
                SelecionarProblema(proximo);
            }
        }
        else
        {
            // Não serve: reação engraçada, balão com o porquê e a obra some. Tenta de novo, sem punição.
            Erros++;
            errosDoProblema++;
            AtualizarCriterios();
            Tocar(config.somDeErro);
            Soltar(estrelinhas, lote.quem.position + Vector3.up * 1.6f);
            UltimoBalao = Explicacao(idDoVerbete, p);
            hud.Balao(UltimoBalao);
            hud.Recado("");
            yield return Reagir(lote.quem);
            float ate = Time.time + config.tempoDoBalao;
            yield return null;
            // O balão fica um tempo; um toque na tela adianta.
            while (Time.time < ate && !Toque.Apertou && !Input.anyKeyDown)
            {
                yield return null;
            }
            hud.Balao(null);
            yield return lote.obra.Sumir(config.tempoDeDesmontar);
            Destroy(lote.obra.gameObject);
            lote.obra = null;

            EtapaAtual = Etapa.Escolhendo;
            int indice = IndiceDoProblema;
            int errosAteAqui = errosDoProblema;
            SelecionarProblema(indice);
            errosDoProblema = errosAteAqui;
        }
    }

    private IEnumerator Concluir()
    {
        EtapaAtual = Etapa.Fim;
        IndiceDoProblema = -1;
        hud.BarraVisivel(false, false);
        hud.Definicao(null, false);
        hud.Dica(null);

        Lote ultimoDaFila = lotes.LastOrDefault(l => l.problema.rotaDaAmbulancia);
        if (ultimoDaFila != null && hospital != null)
        {
            // Com todas as obras prontas, a ambulância segue até a porta do hospital.
            Tocar(config.somDaSirene);
            yield return Mover(ultimoDaFila, new[] { hospital.TransformPoint(0f, 0f, -1.2f) });
            AmbulanciaChegou = true;
            hud.Recado(config.recadoDoHospital);
            Tocar(config.somDeComemoracao);
            Soltar(confete, ultimoDaFila.quem.position + Vector3.up * 2f);
            yield return Comemorar(ultimoDaFila.quem);
        }
        else
        {
            Tocar(config.somDeComemoracao);
        }

        yield return new WaitForSeconds(config.pausaAntesDoFim);
        float tempoGasto = Nivel.TemLimiteDeTempo ? Nivel.tempoEmSegundos - Mathf.Max(0f, TempoRestante) : 0f;
        FinalizarNivel(new ResultadoDoNivel
        {
            concluiu = true, pontuacao = Pontuacao, erros = Erros, tempoGasto = tempoGasto, tempoRestante = Mathf.Max(0f, TempoRestante),
        });
    }

    // Leva quem passa de ponto em ponto. Veículo inclina nas rampas; pessoa e bicho só viram de lado.
    private IEnumerator Mover(Lote lote, IEnumerable<Vector3> pontos)
    {
        Transform quem = lote.quem;
        bool veiculo = lote.problema.quemPassa == QuemPassaNaObra.Veiculo;
        bool ciclista = lote.problema.quemPassa == QuemPassaNaObra.Ciclista;
        float velocidade = veiculo ? config.velocidadeDeVeiculo : ciclista ? config.velocidadeDeBicicleta : config.velocidadeAPe;
        // Quem está na bicicleta não mexe as pernas como quem anda.
        Andando(ciclista ? null : lote.animator, true);
        foreach (Vector3 ponto in pontos)
        {
            while ((quem.position - ponto).sqrMagnitude > 0.0004f)
            {
                Vector3 direcao = ponto - quem.position;
                Vector3 olhar = veiculo ? direcao : new Vector3(direcao.x, 0f, direcao.z);
                if (olhar.sqrMagnitude > 0.0001f)
                {
                    quem.rotation = Quaternion.Slerp(quem.rotation, Quaternion.LookRotation(olhar), 12f * Time.deltaTime);
                }
                quem.position = Vector3.MoveTowards(quem.position, ponto, velocidade * Time.deltaTime);
                yield return null;
            }
        }
        Vector3 frente = quem.forward;
        frente.y = 0f;
        if (frente.sqrMagnitude > 0.0001f)
        {
            quem.rotation = Quaternion.LookRotation(frente);
        }
        Andando(lote.animator, false);
    }

    private IEnumerator Guardar(Lote lote)
    {
        int andando = 0;
        for (int i = 0; i < lote.figurantes.Count && i < lote.obra.vagas.Length; i++)
        {
            andando++;
            StartCoroutine(Estacionar(lote.figurantes[i], lote.obra.vagas[i].position, () => andando--));
        }
        while (andando > 0)
        {
            yield return null;
        }
    }

    // Leva um veículo até a frente da vaga e depois pra dentro dela.
    private IEnumerator Estacionar(Transform carro, Vector3 vaga, System.Action aoChegar)
    {
        Vector3[] pontos = { new Vector3(vaga.x, vaga.y, vaga.z - 4.5f), vaga };
        foreach (Vector3 ponto in pontos)
        {
            while ((carro.position - ponto).sqrMagnitude > 0.0004f)
            {
                Vector3 direcao = ponto - carro.position;
                carro.rotation = Quaternion.Slerp(carro.rotation, Quaternion.LookRotation(direcao), 10f * Time.deltaTime);
                carro.position = Vector3.MoveTowards(carro.position, ponto, config.velocidadeDeVeiculo * Time.deltaTime);
                yield return null;
            }
        }
        carro.rotation = Quaternion.identity;
        aoChegar();
    }

    private static void Andando(Animator animator, bool andando)
    {
        if (animator == null)
        {
            return;
        }
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == Velocidade)
            {
                animator.SetFloat(Velocidade, andando ? 0.5f : 0f);
                return;
            }
        }
    }

    // Três pulinhos de alegria.
    private IEnumerator Comemorar(Transform quem)
    {
        Vector3 chao = quem.position;
        const float duracao = 1.2f;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            quem.position = chao + Vector3.up * (Mathf.Abs(Mathf.Sin(t / duracao * Mathf.PI * 3f)) * 0.6f);
            yield return null;
        }
        quem.position = chao;
    }

    // "Ué?": balança de um lado pro outro e dá um passinho pra trás.
    private IEnumerator Reagir(Transform quem)
    {
        Vector3 lugar = quem.position;
        Quaternion giroInicial = quem.rotation;
        const float duracao = 0.9f;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = t / duracao;
            quem.rotation = giroInicial * Quaternion.Euler(0f, Mathf.Sin(k * Mathf.PI * 6f) * 22f * (1f - k), 0f);
            quem.position = lugar - quem.forward * (Mathf.Sin(k * Mathf.PI) * 0.35f) + Vector3.up * (Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f)) * 0.25f);
            yield return null;
        }
        quem.SetPositionAndRotation(lugar, giroInicial);
    }

    // ------------------------------------------------------------------ estrelas

    private void AtualizarCriterios()
    {
        hud.Criterios(Erros == 0, !EstourouOTempo, !UsouDica, config.rotulosDosCriterios);
    }

    // Três critérios à vista da criança: sem erros, no tempo, sem dica.
    protected override int CalcularEstrelas(ResultadoDoNivel resultado)
    {
        if (!resultado.concluiu)
        {
            return 0;
        }
        int cumpridos = (Erros == 0 ? 1 : 0) + (!EstourouOTempo ? 1 : 0) + (!UsouDica ? 1 : 0);
        return cumpridos >= 3 ? 3 : (cumpridos == 2 ? 2 : 1);
    }

    // ------------------------------------------------------------------ todo quadro: relógio, teclado, toque, câmera

    private void Update()
    {
        if (EscolhendoNivel || Finalizado || lotes.Count == 0)
        {
            return;
        }

        bool cartaoAberto = CartaoVerbete.EstaAberto;
        if (Nivel.TemLimiteDeTempo)
        {
            if (relogioLigado && !cartaoAberto && EtapaAtual != Etapa.Fim && !EstourouOTempo)
            {
                TempoRestante -= Time.deltaTime;
                if (TempoRestante <= 0f)
                {
                    TempoRestante = 0f;
                    EstourouOTempo = true;
                    AtualizarCriterios();
                    if (EtapaAtual == Etapa.Escolhendo)
                    {
                        hud.Recado(config.recadoDeTempoEsgotado);
                    }
                }
            }
            hud.Tempo(TempoRestante / Nivel.tempoEmSegundos);
        }
        else
        {
            hud.Tempo(-1f);
        }

        if (cartaoAberto)
        {
            return;
        }

        // Girar a câmera um pouco: arrastar com o mouse (ou o dedo), ou Q/E.
        float teclas = (Teclado.Segurando(KeyCode.E) ? 1f : 0f) - (Teclado.Segurando(KeyCode.Q) ? 1f : 0f);
        giro = Mathf.Clamp(giro + teclas * 60f * Time.deltaTime, -config.giroMaximoDaCamera, config.giroMaximoDaCamera);
        if (Toque.Apertou)
        {
            toqueInicial = Toque.Posicao;
            toqueNaInterface = Toque.SobreAInterface();
            arrastou = false;
        }
        else if (Toque.Segurando && !toqueNaInterface)
        {
            Vector3 delta = Toque.Posicao - toqueInicial;
            if (!arrastou && delta.magnitude > 12f)
            {
                arrastou = true;
            }
            if (arrastou)
            {
                giro = Mathf.Clamp(giro + Toque.EixoDoMouse("Mouse X") * config.sensibilidadeDoGiro * 10f, -config.giroMaximoDaCamera, config.giroMaximoDaCamera);
                inclinacao = Mathf.Clamp(inclinacao - Toque.EixoDoMouse("Mouse Y") * config.sensibilidadeDoGiro * 10f,
                    config.limitesDaInclinacao.x, config.limitesDaInclinacao.y);
            }
        }
        else if (Toque.Soltou && !toqueNaInterface && !arrastou)
        {
            // Toque simples no mapa: procura o lugar destacado embaixo do dedo.
            Ray raio = cameraDoJogo.ScreenPointToRay(Toque.Posicao);
            RaycastHit toque;
            if (Physics.Raycast(raio, out toque, 500f))
            {
                ObraLocal local = toque.collider.GetComponentInParent<ObraLocal>();
                if (local != null)
                {
                    TocarNoLocal(local.indiceDoProblema);
                }
            }
        }

        if (EtapaAtual != Etapa.Escolhendo)
        {
            return;
        }
        for (int i = 0; i < 4; i++)
        {
            if (Teclado.Apertou(KeyCode.Alpha1 + i) || Teclado.Apertou(KeyCode.Keypad1 + i))
            {
                EscolherOpcao(i);
            }
        }
        if (Teclado.Apertou(KeyCode.Return) || Teclado.Apertou(KeyCode.Space))
        {
            Construir();
        }
        if (Teclado.Apertou(KeyCode.D))
        {
            PedirDica();
        }
        if (Teclado.Apertou(KeyCode.O))
        {
            OuvirDefinicao();
        }
        if (Teclado.Apertou(KeyCode.Tab))
        {
            // Próximo problema ainda aberto.
            for (int passo = 1; passo < lotes.Count; passo++)
            {
                int i = (IndiceDoProblema + passo) % lotes.Count;
                if (!lotes[i].resolvido && MotivoPraNaoEscolher(i) == null)
                {
                    SelecionarProblema(i);
                    break;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (EscolhendoNivel || lotes.Count == 0)
        {
            return;
        }
        foco = Vector3.Lerp(foco, AlvoDaCamera(), 1f - Mathf.Exp(-4f * Time.deltaTime));
        PosicionarCamera();
    }

    // A câmera acompanha o problema da vez; enquanto alguém atravessa, fica no mesmo lugar.
    private Vector3 AlvoDaCamera()
    {
        if (EtapaAtual == Etapa.Fim && hospital != null)
        {
            return hospital.position + config.deslocamentoDoFoco - Vector3.forward * 6f;
        }
        Lote lote = IndiceDoProblema >= 0 ? lotes[IndiceDoProblema] : lotes[lotes.Count - 1];
        return lote.raiz.position + config.deslocamentoDoFoco;
    }

    private void PosicionarCamera()
    {
        Quaternion rotacao = Quaternion.Euler(inclinacao, giro, 0f);
        cameraDoJogo.transform.SetPositionAndRotation(foco - rotacao * Vector3.forward * config.distanciaDaCamera, rotacao);
    }

    private void Tocar(AudioClip clipe)
    {
        if (clipe != null && som != null)
        {
            som.PlayOneShot(clipe);
        }
    }

    private static void Soltar(ParticleSystem particulas, Vector3 posicao)
    {
        if (particulas == null)
        {
            return;
        }
        particulas.transform.position = posicao;
        particulas.Play();
    }
}
