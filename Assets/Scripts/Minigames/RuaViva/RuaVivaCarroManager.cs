using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Rua Viva, parte 1: Passeio de Carro. Verbetes: cinto de segurança, passageiro e buzina.
// Cada nível é uma missão depois da enchente (config.missoes), e ensina uma coisa nova:
//   1 cinto e primeiro passeio; 2 primeira freada e buzina; 3 passageiros nas paradas;
//   4 coisas soltas e trânsito mais cheio; 5 a viagem inteira.
// Roteiro de um nível:
//   Missão: o cartão conta pra onde a família vai (o carro visto de fora, na frente de casa).
//   Coisas soltas (níveis 4 e 5): tocar em cada uma guarda no porta-malas.
//   Cintos, dentro do carro e de perto: o motorista prende o dele sozinho; depois a criança puxa a fivela de
//     cada um (o banco da frente, depois o de trás) até o fecho, e a tira fica atravessada no peito e no colo.
//   Chave: só com todos de cinto; tocar na chave liga o carro.
//   Passeio: o carro anda sozinho (a criança vai no meio do banco de trás: é passageira). Os lugares do
//     percurso trazem os pedidos do safari ("Olha o hospital! Você vê uma ambulância?"). Quem vem pra rua sem
//     ver o carro: tocar no meio do volante faz a mão do motorista apertar a buzina. Bola na rua: freada, e o
//     que estava solto voa. Paradas: alguém entra e precisa prender o cinto. Destino: fim da missão.
// Não existe derrota: pedido perdido, freada e buzina à toa só custam estrela.
//
// O que cada campo do nível faz aqui:
//   velocidade               velocidade do carro (m/s).
//   quantidadeDeItens        pedidos do safari (só informativo: os pedidos vêm da missão).
//   quantidadeDeObstaculos   pessoas distraídas (só informativo).
//   extras: janelaDaBuzina (segundos pra buzinar), tempoPorPedido (0 = sem relógio),
//     transito (metros entre os carros que vêm na outra mão; 0 = nenhum).
//   regrasExtras: figura_no_pedido, carro_para, carro_devagar, dica_buzina.
public class RuaVivaCarroManager : MinigameBase
{
    public const string IdDoCinto = "cinto_de_seguranca";
    public const string IdDoPassageiro = "passageiro";
    public const string IdDaBuzina = "buzina";
    public const string RegraFigura = "figura_no_pedido";
    public const string RegraCarroPara = "carro_para";
    public const string RegraCarroDevagar = "carro_devagar";
    public const string RegraDicaBuzina = "dica_buzina";
    public static readonly int[] OrdemDosCintos = { 1, 3, 2, 4 };

    private class Evento
    {
        public EventoDaMissao def;
        public float s;
        public LugarDoPasseio lugar;
        public ObjetoDoSafari objeto;
        public float sPedido;
        public bool pedidoResolvido;
        public float sParada = -1f;
        public bool parou;
        public float sLinha = -1f;
        public bool liberado;
        public bool avisouVermelho;
        public float tempoNoVermelho;
        public PessoaDistraida pessoa;
        public Transform bola;
        public bool bolaRolou;
        public bool bolaPassou;
    }

    private class Solto
    {
        public GameObject objeto;
        public int assento;
        public bool guardado;
        public bool caiu;
    }

    private class CarroDoTransito
    {
        public Transform carro;
        public float s;
    }

    [Header("Passeio de Carro")]
    public RuaVivaConfig config;
    public RuaVivaHUD hud;
    public AudioSource som;
    public Camera cameraDoJogo;
    public RuaVivaPercurso percurso;
    public CarroDaFamilia carro;

    [Header("Casa (onde a missão começa)")]
    public Transform lugarDoCarroEmCasa;
    public Transform poseDaCasa;

    [Header("Pessoas e coisas soltas")]
    [Tooltip("pai, mae, voce, filha, cacula, vovo.")]
    public OcupanteDoCarro[] pessoas = new OcupanteDoCarro[0];
    public GameObject modeloDeUrsinho;
    public GameObject modeloDeCaixa;
    [Tooltip("Camada do jogador: a câmera dentro do carro não desenha o próprio corpo (o retrovisor desenha).")]
    public int camadaDoJogador = 30;

    [Header("Efeitos")]
    public ParticleSystem estrelinhas;
    public ParticleSystem confete;
    public ParticleSystem susto;

    public enum Etapa { Selecao, Missao, Soltos, Cintos, Chave, Passeio, Parada, Chegada }

    public Etapa EtapaAtual { get; private set; } = Etapa.Selecao;
    public MissaoDoPasseio Missao { get; private set; }
    public int AssentoDaVez { get; private set; } = -1;
    public bool EsperandoCinto { get; private set; }
    public CintoDoAssento CintoDaVez { get { return AssentoDaVez >= 0 ? carro.cintos[AssentoDaVez] : null; } }
    public int CintosPresos { get; private set; }
    public int SoltosParaGuardar { get; private set; }
    public int ObjetosQueVoaram { get; private set; }
    public int PassageirosQueEntraram { get; private set; }
    public int PassageirosQueDesceram { get; private set; }
    public int Pedidos { get; private set; }
    public int Acertos { get; private set; }
    public int ToquesErrados { get; private set; }
    public int PedidosPerdidos { get; private set; }
    public int PessoasDistraidas { get; private set; }
    public int BuzinadasCertas { get; private set; }
    public int BuzinadasAToa { get; private set; }
    public int Freadas { get; private set; }
    public int FreadasDaBola { get; private set; }
    public int ParadasNoVermelho { get; private set; }
    public int CarrosDoTransito { get; private set; }
    public float Velocidade { get; private set; }
    public float TempoGasto { get; private set; }
    public bool Pausado { get; private set; }
    public string UltimoBalao { get; private set; }
    public string UltimoRecado { get; private set; }
    public string UltimoRadio { get; private set; }
    public float Fim { get; private set; }
    public ObjetoDoSafari ObjetoPedido { get { return pedido != null ? pedido.objeto : null; } }
    public string VerbetePedido { get; private set; }
    public IList<GameObject> CoisasSoltas { get { List<GameObject> l = new List<GameObject>(); foreach (Solto x in soltos) l.Add(x.objeto); return l; } }
    // Quem está vindo pra rua agora (null = ninguém: buzinar é à toa).
    public PessoaDistraida PessoaEmPerigo
    {
        get
        {
            foreach (Evento e in eventos)
            {
                if (e.pessoa != null && e.pessoa.EmPerigo) return e.pessoa;
            }
            return null;
        }
    }
    public bool SafariOk { get { return Pedidos == 0 || Acertos - ToquesErrados >= Mathf.CeilToInt(Pedidos * config.fracaoDoSafari); } }
    public bool BuzinaOk { get { return BuzinadasCertas == PessoasDistraidas; } }
    public bool SemSusto { get { return BuzinadasAToa == 0; } }

    private readonly List<Evento> eventos = new List<Evento>();
    private readonly List<Solto> soltos = new List<Solto>();
    private readonly List<CarroDoTransito> transito = new List<CarroDoTransito>();
    private readonly HashSet<ObjetoDoSafari> jaTocados = new HashSet<ObjetoDoSafari>();
    private Evento pedido;
    private float tempoDoPedido;
    private float limiteDoPedido;
    private float paradoAoLado;
    private bool parado;
    private float tempoDaUltimaBuzina = -10f;
    private Coroutine balaoAberto;
    private Coroutine figurinhaAberta;
    private Coroutine radioAberto;
    private bool assustando;
    private PessoaDistraida pessoaAvisada;
    private bool missaoAceita;
    private bool chaveGirada;
    private bool arrastando;
    private bool primeiroCintoDaCrianca = true;
    private int linhaDoRadio = -1;
    private float proximoCarroEm;

    // Câmera: segue uma pose (no carro ou no mundo) e pode olhar pra um alvo.
    private Transform poseAlvo;
    private Transform olharAlvo;
    private Vector3 atualP;
    private Quaternion atualR;
    private Transform referenciaAtual;
    private bool cameraNaHora;

    // ------------------------------------------------------------------ começo

    private void Start()
    {
        hud.Balao(null);
        hud.Figurinha(null, null);
        hud.Pedido(null, null, false);
        hud.TempoDoPedido(-1f);
        hud.Marca(Vector3.zero, false);
        hud.SinalDaBuzina(false);
        hud.Radio(null);
        hud.Missao(null, null, null, false);
        hud.Escuro(0f);
        hud.Jogando(false);
        carro.Preparar();
        foreach (OcupanteDoCarro p in pessoas) p.gameObject.SetActive(false);
        foreach (CintoDoAssento c in carro.cintos) c.Desocupar();
        carro.transform.SetPositionAndRotation(lugarDoCarroEmCasa.position, lugarDoCarroEmCasa.rotation);
        CameraEm(poseDaCasa, true);
        if (EscolhendoNivel)
        {
            return;
        }

        int indice = Mathf.Clamp(NivelAtual - 1, 0, config.missoes.Length - 1);
        Missao = config.missoes[indice];
        hud.Nivel(string.Format(config.textoDoNivel, NivelAtual));
        Planejar();
        AtualizarCriterios();
        StartCoroutine(Roteiro());
    }

    private OcupanteDoCarro Pessoa(string id)
    {
        foreach (OcupanteDoCarro p in pessoas)
        {
            if (p.id == id) return p;
        }
        Debug.LogError("[Rua Viva] Pessoa desconhecida: " + id);
        return null;
    }

    private IEnumerator Roteiro()
    {
        // 1. O cartão da missão, com o carro visto de fora.
        EtapaAtual = Etapa.Missao;
        hud.Missao(Missao.titulo, Missao.texto, Missao.novidade, Narracao.Voz.Disponivel);
        while (!missaoAceita)
        {
            yield return null;
        }
        hud.Missao(null, null, null, false);
        yield return Escurecer();
        foreach (OcupanteDaMissao o in Missao.ocupantes)
        {
            SentarNoCarro(Pessoa(o.quem), o.assento);
        }
        EspalharSoltos();
        hud.Jogando(true);

        // 2. Coisas soltas no porta-malas.
        if (Missao.guardarSoltos && SoltosParaGuardar > 0)
        {
            EtapaAtual = Etapa.Soltos;
            CameraEm(carro.posesDoCinto[soltos[0].assento], true);
            yield return Clarear();
            Dizer(config.recadoDosSoltos);
            while (SoltosParaGuardar > 0)
            {
                yield return null;
            }
            Dizer(config.recadoTudoGuardado);
            yield return new WaitForSeconds(0.9f);
        }
        else
        {
            CameraEm(carro.posesDoCinto[0], true);
            yield return Clarear();
        }

        // 3. Cintos: o motorista prende o dele; depois cada um, o banco da frente e então o de trás.
        EtapaAtual = Etapa.Cintos;
        yield return CintoDoMotorista();
        foreach (int a in OrdemDosCintos)
        {
            if (carro.cintos[a].Ocupado && !carro.cintos[a].Preso)
            {
                yield return CintoDe(a);
            }
        }

        // 4. A chave: só agora o carro liga.
        EtapaAtual = Etapa.Chave;
        CameraEm(carro.poseDaChave, false);
        Dizer(config.recadoDaChave);
        while (!chaveGirada)
        {
            yield return null;
        }
        Tocar(config.somDoMotor);
        yield return carro.GirarChave();
        Soltar(estrelinhas, carro.chave.transform.position);
        yield return new WaitForSeconds(0.7f);

        // 5. Pra rua.
        yield return Escurecer();
        percurso.S = 0f;
        percurso.Reiniciar();
        percurso.PosicionarCarro(carro.transform);
        CameraEm(carro.poseDoPasseio, true);
        EtapaAtual = Etapa.Passeio;
        Dizer(config.recadoDoPasseio);
        yield return Clarear();
        yield return AbrirCartaoSeNovo(IdDoPassageiro);
        MudarRadio();
    }

    // O botão "Vamos!" do cartão da missão.
    public void ComecarMissao()
    {
        if (EtapaAtual == Etapa.Missao)
        {
            missaoAceita = true;
        }
    }

    public void OuvirMissao()
    {
        if (Missao != null && Narracao.Voz.Disponivel)
        {
            Narracao.Voz.Falar(Missao.titulo + ". " + Missao.texto);
        }
    }

    private void SentarNoCarro(OcupanteDoCarro p, int assento)
    {
        p.Sentar(carro.assentos[assento], assento);
        CintoDoAssento c = carro.cintos[assento];
        Vector3 peito = c.transform.InverseTransformPoint(carro.assentos[assento].TransformPoint(p.PeitoNoAssento));
        Vector3 colo = c.transform.InverseTransformPoint(carro.assentos[assento].TransformPoint(new Vector3(0f, 0.08f, 0.2f)));
        c.Ocupar(peito, colo);
        c.Soltar();
    }

    private void EspalharSoltos()
    {
        int[] porAssento = new int[5];
        foreach (OcupanteDaMissao o in Missao.soltos)
        {
            GameObject modelo = o.quem == "ursinho" ? modeloDeUrsinho : modeloDeCaixa;
            GameObject go = Instantiate(modelo, carro.transform);
            go.SetActive(true);
            go.name = "Solto_" + o.quem;
            float lado = porAssento[o.assento] == 0 ? -0.1f : 0.12f;
            porAssento[o.assento]++;
            go.transform.localPosition = carro.transform.InverseTransformPoint(carro.assentos[o.assento].position) + new Vector3(lado, -0.04f, 0.08f);
            go.transform.localRotation = Quaternion.Euler(0f, 160f + lado * 100f, 0f);
            soltos.Add(new Solto { objeto = go, assento = o.assento });
            SoltosParaGuardar++;
        }
    }

    // ------------------------------------------------------------------ cintos

    private IEnumerator CintoDoMotorista()
    {
        AssentoDaVez = 0;
        CintoDoAssento c = carro.cintos[0];
        CameraEm(carro.posesDoCinto[0], false);
        Dizer(config.recadoDoMotorista);
        yield return new WaitForSeconds(0.6f);
        Vector3 de = c.fivela.position;
        Vector3 meio = (c.fivela.position + c.fecho.position) / 2f + carro.transform.forward * 0.25f;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f)
        {
            Vector3 p = Vector3.Lerp(Vector3.Lerp(de, meio, t), Vector3.Lerp(meio, c.fecho.position, t), t);
            c.Levar(p);
            yield return null;
        }
        c.Prender();
        Tocar(config.somDaFivela);
        Soltar(estrelinhas, c.fecho.position);
        yield return new WaitForSeconds(config.pausaEntreOcupantes);
        AssentoDaVez = -1;
    }

    private IEnumerator CintoDe(int assento)
    {
        AssentoDaVez = assento;
        CintoDoAssento c = carro.cintos[assento];
        c.Soltar();
        CameraEm(carro.posesDoCinto[assento], false);
        OcupanteDoCarro quem = QuemEsta(assento);
        Dizer(string.Format(config.textoDoOcupante, quem != null ? quem.nome : "?"));
        EsperandoCinto = true;
        while (EsperandoCinto)
        {
            yield return null;
        }
        yield return new WaitForSeconds(config.pausaEntreOcupantes);
        if (primeiroCintoDaCrianca)
        {
            primeiroCintoDaCrianca = false;
            yield return AbrirCartaoSeNovo(IdDoCinto);
        }
        AssentoDaVez = -1;
    }

    private OcupanteDoCarro QuemEsta(int assento)
    {
        foreach (OcupanteDoCarro p in pessoas)
        {
            if (p.gameObject.activeSelf && p.Assento == assento) return p;
        }
        return null;
    }

    // O dedo arrastando a fivela (posição na tela).
    public void ArrastarFivela(Vector2 tela)
    {
        if (!EsperandoCinto || Pausado)
        {
            return;
        }
        CintoDoAssento c = CintoDaVez;
        Vector3 meio = (c.PeitoNoMundo + c.fecho.position) / 2f;
        Plane plano = new Plane(-cameraDoJogo.transform.forward, meio);
        Ray raio = cameraDoJogo.ScreenPointToRay(tela);
        if (plano.Raycast(raio, out float d))
        {
            Vector3 p = raio.GetPoint(d);
            // Não deixa a fivela ir pra longe do banco.
            Vector3 local = c.transform.InverseTransformPoint(p);
            Vector3 centro = c.transform.InverseTransformPoint(meio);
            local = centro + Vector3.ClampMagnitude(local - centro, 0.7f);
            c.Levar(c.transform.TransformPoint(local));
        }
        Vector3 fechoNaTela = cameraDoJogo.WorldToScreenPoint(c.fecho.position);
        if (c.DistanciaAoFecho <= config.encaixeDaFivela || (fechoNaTela.z > 0f && Vector2.Distance(tela, fechoNaTela) <= config.raioDeToqueDaFivela * 0.6f))
        {
            PrenderCinto();
        }
    }

    // Soltou o dedo longe do fecho: a fivela volta pro lugar, sem erro.
    public void SoltarFivela()
    {
        arrastando = false;
        if (EsperandoCinto)
        {
            CintoDaVez.Soltar();
            Tocar(config.somDeSoltar);
        }
    }

    // A fivela chegou ao fecho (arrastando ou pelo teclado): "clique".
    public void PrenderCinto()
    {
        if (!EsperandoCinto)
        {
            return;
        }
        CintoDoAssento c = CintoDaVez;
        c.Prender();
        EsperandoCinto = false;
        arrastando = false;
        CintosPresos++;
        hud.Marca(Vector3.zero, false);
        Tocar(config.somDaFivela);
        Soltar(estrelinhas, c.fecho.position);
    }

    // ------------------------------------------------------------------ coisas soltas e chave

    public bool GuardarSolto(GameObject objeto)
    {
        if (EtapaAtual != Etapa.Soltos)
        {
            return false;
        }
        foreach (Solto x in soltos)
        {
            if (!x.guardado && objeto != null && (objeto == x.objeto || objeto.transform.IsChildOf(x.objeto.transform)))
            {
                x.guardado = true;
                SoltosParaGuardar--;
                Tocar(config.somDoPortaMalas);
                StartCoroutine(VoarProPortaMalas(x.objeto.transform));
                return true;
            }
        }
        return false;
    }

    private IEnumerator VoarProPortaMalas(Transform t)
    {
        Vector3 de = t.position;
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.5f)
        {
            Vector3 ate = carro.portaMalas.position;
            t.position = Vector3.Lerp(de, ate, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.35f;
            t.localScale = Vector3.one * (1f - k * 0.6f);
            yield return null;
        }
        Soltar(estrelinhas, carro.portaMalas.position);
        t.gameObject.SetActive(false);
    }

    public void LigarCarro()
    {
        if (EtapaAtual == Etapa.Chave)
        {
            chaveGirada = true;
        }
        else if (EtapaAtual == Etapa.Cintos || EtapaAtual == Etapa.Soltos)
        {
            Dizer(config.recadoDaChaveSemCinto);
        }
    }

    // ------------------------------------------------------------------ preparar o percurso da missão

    private void Planejar()
    {
        Random.State antes = Random.state;
        Random.InitState(137 * NivelAtual + 11);
        float janela = Nivel.Extra("janelaDaBuzina", 4f);
        float s = config.comecoDoPasseio;
        int pedestre = 0;
        foreach (EventoDaMissao def in Missao.eventos)
        {
            Evento e = new Evento { def = def };
            if (def.tipo == TipoDeEvento.Lugar || def.tipo == TipoDeEvento.Parada || def.tipo == TipoDeEvento.Destino)
            {
                LugarDoPasseio modelo = percurso.Lugar(def.lugar);
                if (modelo == null)
                {
                    Debug.LogError("[Rua Viva] Lugar desconhecido: " + def.lugar);
                    continue;
                }
                s = ProximoTrechoReto(s, modelo.comprimento);
                e.s = s;
                e.lugar = Instantiate(modelo, percurso.colocados);
                e.lugar.gameObject.SetActive(true);
                e.lugar.transform.SetPositionAndRotation(Chao(percurso.Posicao(s)), Quaternion.LookRotation(percurso.Direcao(s)));
                if (!string.IsNullOrEmpty(def.pedido))
                {
                    e.objeto = e.lugar.Objeto(def.pedido);
                    if (e.objeto == null)
                    {
                        Debug.LogError("[Rua Viva] O lugar " + def.lugar + " não tem " + def.pedido);
                    }
                    else
                    {
                        e.sPedido = s + e.lugar.transform.InverseTransformPoint(e.objeto.transform.position).z;
                        Pedidos++;
                    }
                }
                if (def.tipo != TipoDeEvento.Lugar)
                {
                    e.sParada = s + e.lugar.parada;
                }
                if (e.lugar.sinal != null)
                {
                    e.sLinha = s + e.lugar.linha;
                    e.lugar.sinal.Mudar(CorDoSinal.Vermelho);
                }
                foreach (string quem in new[] { def.entra, def.espera })
                {
                    if (string.IsNullOrEmpty(quem) || e.lugar.chegada == null) continue;
                    OcupanteDoCarro p = Pessoa(quem);
                    p.FicarDePe(e.lugar.chegada.position, e.lugar.chegada.position - e.lugar.transform.right);
                }
                s += modelo.comprimento + config.espacoEntreEventos * 0.5f;
            }
            else
            {
                s = ProximoTrechoReto(s, 20f) + 10f;
                e.s = s;
                if (def.tipo == TipoDeEvento.Bola)
                {
                    GameObject bola = Instantiate(percurso.modeloDaBola, percurso.colocados);
                    bola.SetActive(true);
                    e.bola = bola.transform;
                    e.bola.position = percurso.Ponto(s, 4.6f, 0.15f);
                }
                else
                {
                    GameObject modelo = percurso.modelosDePedestre[pedestre++ % percurso.modelosDePedestre.Length];
                    GameObject copia = Instantiate(modelo, percurso.colocados);
                    copia.SetActive(true);
                    e.pessoa = copia.AddComponent<PessoaDistraida>();
                    e.pessoa.Preparar(this, percurso, s, def.tipo == TipoDeEvento.Atenta, config.ladoDeOndeVem, config.beiraDaRua, janela,
                        config.distanciaDaFreada, Nivel.velocidade, config.velocidadeDeAtravessar);
                    if (def.tipo == TipoDeEvento.Distraida) PessoasDistraidas++;
                }
                s += config.espacoEntreEventos;
            }
            eventos.Add(e);
            if (def.tipo == TipoDeEvento.Destino) Fim = e.sParada;
        }
        if (Fim <= 0f) Fim = Mathf.Min(s, percurso.Comprimento - 5f);
        Random.state = antes;
        proximoCarroEm = 0f;
        Physics.SyncTransforms();
    }

    private static Vector3 Chao(Vector3 p)
    {
        p.y = 0f;
        return p;
    }

    private float ProximoTrechoReto(float s, float comprimento)
    {
        while (s < percurso.Comprimento - 40f - comprimento && !percurso.EhRetoEntre(s, s + comprimento))
        {
            s += 5f;
        }
        return s;
    }

    // ------------------------------------------------------------------ a cada quadro

    private void Update()
    {
        if (EscolhendoNivel || Finalizado)
        {
            return;
        }
        bool toque = Input.GetMouseButtonDown(0) && !SobreAInterface();
        if (EtapaAtual == Etapa.Missao && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            ComecarMissao();
        }
        if (EsperandoCinto && !Pausado)
        {
            CuidarDoArrasto(toque);
            return;
        }
        if (toque && !Pausado)
        {
            TocarNaTela(Input.mousePosition);
        }
        if (EtapaAtual == Etapa.Chave && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            LigarCarro();
        }
        if (EtapaAtual == Etapa.Soltos && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            foreach (Solto x in soltos)
            {
                if (!x.guardado) { GuardarSolto(x.objeto); break; }
            }
        }
        // A estrela mostra onde tocar: a chave, ou a próxima coisa solta.
        if (EtapaAtual == Etapa.Chave || EtapaAtual == Etapa.Soltos)
        {
            Transform alvo = null;
            if (EtapaAtual == Etapa.Chave && !chaveGirada) alvo = carro.chave.transform;
            foreach (Solto x in soltos)
            {
                if (EtapaAtual == Etapa.Soltos && !x.guardado) { alvo = x.objeto.transform; break; }
            }
            Vector3 naTela = alvo != null ? cameraDoJogo.WorldToScreenPoint(alvo.position) : Vector3.zero;
            hud.Marca(naTela, alvo != null && naTela.z > 0f);
        }
        bool andando = EtapaAtual == Etapa.Passeio || EtapaAtual == Etapa.Parada || EtapaAtual == Etapa.Chegada;
        if (!andando)
        {
            return;
        }
        CuidarDoTransito();
        if (Pausado)
        {
            return;
        }
        if (EtapaAtual == Etapa.Passeio)
        {
            TempoGasto += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Space))
            {
                Buzinar();
            }
            Dirigir();
            CuidarDosEventos();
        }
        if (EtapaAtual == Etapa.Passeio || EtapaAtual == Etapa.Chegada)
        {
            CuidarDosPedidos();
        }
        CuidarDaDicaDaBuzina();
    }

    private static bool SobreAInterface()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void CuidarDoArrasto(bool toque)
    {
        CintoDoAssento c = CintoDaVez;
        if (toque)
        {
            Vector3 naTela = cameraDoJogo.WorldToScreenPoint(c.fivela.position);
            arrastando = naTela.z > 0f && Vector2.Distance(Input.mousePosition, naTela) <= config.raioDeToqueDaFivela;
        }
        if (arrastando && Input.GetMouseButton(0))
        {
            ArrastarFivela(Input.mousePosition);
        }
        if (arrastando && Input.GetMouseButtonUp(0))
        {
            SoltarFivela();
        }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            PrenderCinto();
            return;
        }
        // A estrela mostra a fivela (onde pegar) e, enquanto ela é arrastada, o fecho (onde levar).
        Vector3 marca = cameraDoJogo.WorldToScreenPoint(arrastando ? c.fecho.position : c.fivela.position);
        hud.Marca(marca, marca.z > 0f);
    }

    private void LateUpdate()
    {
        if (poseAlvo == null)
        {
            return;
        }
        Transform referencia = poseAlvo.IsChildOf(carro.transform) ? carro.transform : null;
        Vector3 p = referencia != null ? referencia.InverseTransformPoint(poseAlvo.position) : poseAlvo.position;
        Quaternion r = poseAlvo.rotation;
        if (olharAlvo != null && olharAlvo.gameObject.activeInHierarchy)
        {
            Vector3 alvo = olharAlvo.position + Vector3.up * 0.9f;
            if (olharAlvo.GetComponent<OcupanteDoCarro>() == null) alvo = olharAlvo.position;
            r = Quaternion.LookRotation(alvo - poseAlvo.position);
        }
        if (referencia != null) r = Quaternion.Inverse(referencia.rotation) * r;
        if (cameraNaHora || referencia != referenciaAtual)
        {
            atualP = p;
            atualR = r;
            referenciaAtual = referencia;
            cameraNaHora = false;
        }
        else
        {
            float k = 1f - Mathf.Exp(-5f * Time.deltaTime);
            atualP = Vector3.Lerp(atualP, p, k);
            atualR = Quaternion.Slerp(atualR, r, k);
        }
        if (referencia != null)
        {
            cameraDoJogo.transform.SetPositionAndRotation(referencia.TransformPoint(atualP), referencia.rotation * atualR);
        }
        else
        {
            cameraDoJogo.transform.SetPositionAndRotation(atualP, atualR);
        }
        // Dentro do carro, a câmera fica na cabeça do jogador: o corpo dele só aparece no retrovisor.
        int mascara = ~0;
        if (poseAlvo == carro.poseDoPasseio) mascara &= ~(1 << camadaDoJogador);
        cameraDoJogo.cullingMask = mascara;
    }

    private void CameraEm(Transform pose, bool naHora)
    {
        poseAlvo = pose;
        cameraNaHora = naHora;
    }

    private void Dirigir()
    {
        float alvo = Nivel.velocidade;
        if (parado)
        {
            alvo = 0f;
        }
        else if (pedido != null && pedido.objeto != null)
        {
            float d = pedido.sPedido - percurso.S;
            if (Nivel.TemRegra(RegraCarroPara))
            {
                // Nível que ensina: o carro vai parando ao lado do objeto e espera um pouco.
                if (d < 12f && paradoAoLado < config.esperaAoLadoDoObjeto)
                {
                    alvo = Mathf.Clamp(d - 0.5f, 0f, config.velocidadeDevagar * 2f);
                }
                if (Velocidade < 0.1f && d < 3f)
                {
                    paradoAoLado += Time.deltaTime;
                }
            }
            else if (Nivel.TemRegra(RegraCarroDevagar) && d < 15f)
            {
                alvo = config.velocidadeDevagar * 1.6f;
            }
        }
        float taxa = alvo < Velocidade ? (parado ? config.freioForte : config.freio) : config.aceleracao;
        Velocidade = Mathf.MoveTowards(Velocidade, alvo, taxa * Time.deltaTime);

        // Onde o carro tem de parar: parada, destino, sinal vermelho, bola na rua.
        foreach (Evento e in eventos)
        {
            if (e.sParada >= 0f && !e.parou) Limitar(e.sParada - percurso.S, config.freio);
            if (e.sLinha >= 0f && !e.liberado) Limitar(e.sLinha - 2.6f - percurso.S, config.freio);
            if (e.bola != null && e.bolaRolou && !e.bolaPassou) Limitar(e.s - 6f - percurso.S, config.freioForte);
        }
        percurso.S += Velocidade * Time.deltaTime;
        percurso.PosicionarCarro(carro.transform);
        carro.GirarRodas(Velocidade * Time.deltaTime);
    }

    private void Limitar(float distancia, float freio)
    {
        if (distancia < -1f || distancia > 80f)
        {
            return;
        }
        float maxima = Mathf.Sqrt(2f * freio * Mathf.Max(0f, distancia));
        Velocidade = Mathf.Min(Velocidade, maxima);
    }

    private void CuidarDosEventos()
    {
        foreach (Evento e in eventos)
        {
            if (e.sParada >= 0f && !e.parou && percurso.S >= e.sParada - 0.4f && Velocidade < 0.05f)
            {
                e.parou = true;
                Velocidade = 0f;
                StartCoroutine(e.def.tipo == TipoDeEvento.Destino ? Chegar(e) : FazerParada(e));
                return;
            }
            if (e.sLinha >= 0f && !e.liberado && percurso.S >= e.sLinha - 30f && !e.avisouVermelho)
            {
                e.avisouVermelho = true;
                Dizer(string.Format(config.recadoDoVermelho, config.trechoDoVermelho));
            }
            if (e.sLinha >= 0f && !e.liberado && percurso.S >= e.sLinha - 3.4f && Velocidade < 0.05f)
            {
                e.tempoNoVermelho += Time.deltaTime;
                if (e.tempoNoVermelho >= config.esperaNoVermelho)
                {
                    e.liberado = true;
                    ParadasNoVermelho++;
                    e.lugar.sinal.Mudar(CorDoSinal.Verde);
                    Dizer(config.recadoDoVerde);
                }
            }
            if (e.bola != null && !e.bolaRolou && percurso.S >= e.s - 16f)
            {
                e.bolaRolou = true;
                FreadasDaBola++;
                StartCoroutine(RolarBola(e));
                StartCoroutine(Freada(true));
            }
        }
    }

    private IEnumerator RolarBola(Evento e)
    {
        float lateral = 4.6f;
        while (lateral > -9f)
        {
            lateral -= 4f * Time.deltaTime;
            e.bola.position = percurso.Ponto(e.s, lateral, 0.15f);
            e.bola.Rotate(percurso.Direcao(e.s) * -4f / 0.15f * Mathf.Rad2Deg * Time.deltaTime, Space.World);
            if (!e.bolaPassou && lateral < -5.6f)
            {
                yield return new WaitForSeconds(0.6f);
                e.bolaPassou = true;
            }
            yield return null;
        }
        e.bola.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ freada

    private IEnumerator Freada(bool daBola)
    {
        Tocar(config.somDaFreada);
        StartCoroutine(Solavanco());
        StartCoroutine(Inclinar());
        Solto voou = null;
        foreach (Solto x in soltos)
        {
            if (x.guardado || x.caiu) continue;
            x.caiu = true;
            ObjetosQueVoaram++;
            StartCoroutine(Cair(x));
            if (voou == null) voou = x;
        }
        if (daBola)
        {
            Dizer(config.recadoDaBola);
            Balao(string.Format(voou != null ? config.balaoDaFreadaComSolto : config.balaoDaFreadaSemSolto, config.trechoDaFreada));
        }
        yield break;
    }

    private IEnumerator Solavanco()
    {
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            percurso.Tranco = Mathf.Sin(t / 0.6f * Mathf.PI) * 4f;
            yield return null;
        }
        percurso.Tranco = 0f;
    }

    // Quem está de cinto vai um pouco pra frente e o cinto segura no banco.
    private IEnumerator Inclinar()
    {
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            float k = t / 0.8f;
            float graus = Mathf.Sin(Mathf.Min(1f, k * 3f) * Mathf.PI * 0.5f) * 14f * (1f - k) + Mathf.Sin(k * Mathf.PI * 3f) * 2f * (1f - k);
            foreach (Transform a in carro.assentos) a.localRotation = Quaternion.Euler(graus, 0f, 0f);
            yield return null;
        }
        foreach (Transform a in carro.assentos) a.localRotation = Quaternion.identity;
    }

    // A coisa solta voa pra frente, por cima dos bancos, e cai no painel, dando cambalhota (dá pra ver de trás).
    private IEnumerator Cair(Solto x)
    {
        Transform t = x.objeto.transform;
        Vector3 de = t.localPosition;
        Vector3 ate = carro.transform.InverseTransformPoint(carro.pousoDosSoltos.position) + new Vector3(-0.12f * ObjetosQueVoaram, 0f, 0f);
        Quaternion giro = t.localRotation;
        for (float k = 0f; k < 1f; k += Time.deltaTime / 0.7f)
        {
            t.localPosition = Vector3.Lerp(de, ate, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.35f;
            t.localRotation = giro * Quaternion.Euler(k * 200f, 0f, k * 40f);
            yield return null;
        }
        t.localPosition = ate;
        t.localRotation = giro * Quaternion.Euler(110f, 0f, 30f);
        Soltar(susto, t.position + Vector3.up * 0.1f);
    }

    // ------------------------------------------------------------------ paradas e chegada

    private IEnumerator FazerParada(Evento e)
    {
        EtapaAtual = Etapa.Parada;
        yield return Entrar(e);
        CameraEm(carro.poseDoPasseio, false);
        Dizer(config.recadoTodosDeCinto);
        yield return new WaitForSeconds(0.6f);
        EtapaAtual = Etapa.Passeio;
    }

    // Alguém entra no carro (vem andando até a porta), senta e prende o cinto.
    private IEnumerator Entrar(Evento e)
    {
        OcupanteDoCarro p = Pessoa(e.def.entra);
        int a = e.def.assento;
        Dizer(string.Format(config.recadoDaParada, e.lugar.nome));
        olharAlvo = p.transform;
        yield return p.Andar(Chao(carro.portas[a].position));
        olharAlvo = null;
        SentarNoCarro(p, a);
        PassageirosQueEntraram++;
        Soltar(estrelinhas, carro.assentos[a].position + Vector3.up * 0.5f);
        Dizer(string.Format(config.recadoDeQuemEntrou, p.nome));
        yield return new WaitForSeconds(1.2f);
        yield return CintoDe(a);
    }

    private IEnumerator Chegar(Evento e)
    {
        EtapaAtual = Etapa.Chegada;
        // O pedido do destino ainda vale um pouco, com o carro parado.
        for (float t = 0f; pedido != null && t < 8f; t += Time.deltaTime)
        {
            yield return null;
        }
        if (pedido != null)
        {
            FecharPedido(false, config.recadoDePassou);
        }
        hud.Marca(Vector3.zero, false);

        foreach (string id in e.def.descem)
        {
            OcupanteDoCarro p = Pessoa(id);
            int a = p.Assento;
            carro.cintos[a].Desocupar();
            p.FicarDePe(Chao(carro.portas[a].position), e.lugar.chegada.position);
            PassageirosQueDesceram++;
            Dizer(string.Format(config.recadoDeQuemDesceu, p.nome));
            olharAlvo = p.transform;
            StartCoroutine(p.Andar(e.lugar.chegada.position + e.lugar.transform.forward * (PassageirosQueDesceram * 0.8f)));
            yield return new WaitForSeconds(1.4f);
        }
        if (!string.IsNullOrEmpty(e.def.entra))
        {
            yield return Entrar(e);
            CameraEm(carro.poseDoPasseio, false);
        }
        if (!string.IsNullOrEmpty(e.def.espera))
        {
            OcupanteDoCarro p = Pessoa(e.def.espera);
            p.Comemorar();
            olharAlvo = p.transform;
        }
        Dizer(Missao.recadoDeChegada);
        Tocar(config.somDeComemoracao);
        Soltar(confete, cameraDoJogo.transform.position + cameraDoJogo.transform.forward * 6f);
        yield return new WaitForSeconds(config.pausaAntesDoFim);
        while (Pausado)
        {
            yield return null;
        }
        int pontos = Mathf.Max(40, 20 * Acertos + 30 * BuzinadasCertas - 10 * BuzinadasAToa - 5 * ToquesErrados);
        FinalizarNivel(new ResultadoDoNivel { concluiu = true, pontuacao = pontos, erros = BuzinadasAToa + Freadas + PedidosPerdidos, tempoGasto = TempoGasto });
    }

    // ------------------------------------------------------------------ safari

    private void CuidarDosPedidos()
    {
        if (pedido == null)
        {
            float tempoPorPedido = Nivel.Extra("tempoPorPedido", 0f);
            float antecipacao = Mathf.Max(config.antecipacaoDoPedido, tempoPorPedido * Nivel.velocidade);
            foreach (Evento e in eventos)
            {
                if (e.objeto != null && !e.pedidoResolvido && percurso.S >= e.sPedido - antecipacao && !ParadaAntes(e.sPedido))
                {
                    AbrirPedido(e, tempoPorPedido);
                    break;
                }
            }
        }
        if (pedido == null)
        {
            if (PessoaEmPerigo == null) hud.Marca(Vector3.zero, false);
            return;
        }

        tempoDoPedido += Time.deltaTime;
        if (limiteDoPedido > 0f)
        {
            hud.TempoDoPedido(1f - tempoDoPedido / limiteDoPedido);
            if (tempoDoPedido >= limiteDoPedido)
            {
                FecharPedido(false, config.recadoDoTempo);
                return;
            }
        }
        bool passou = percurso.S > pedido.sPedido + config.toleranciaDepoisDoObjeto;
        bool cansouDeEsperar = Nivel.TemRegra(RegraCarroPara) && paradoAoLado >= config.esperaAoLadoDoObjeto && percurso.S > pedido.sPedido - 3f;
        if (passou || cansouDeEsperar)
        {
            FecharPedido(false, config.recadoDePassou);
            return;
        }

        // Nível com dica: uma marca mostra o objeto depois de um tempinho.
        if (Nivel.TemRegra(RegraFigura) && tempoDoPedido >= config.tempoAteAMarca)
        {
            Vector3 naTela = cameraDoJogo.WorldToScreenPoint(pedido.objeto.transform.position + Vector3.up * 2f);
            hud.Marca(naTela, naTela.z > 0f);
        }
        else if (PessoaEmPerigo == null)
        {
            hud.Marca(Vector3.zero, false);
        }
    }

    // Há uma parada (ou o destino) antes deste ponto: o pedido espera o carro sair dela.
    private bool ParadaAntes(float s)
    {
        foreach (Evento f in eventos)
        {
            if (f.sParada >= 0f && !f.parou && f.sParada < s - 1f) return true;
            if (f.sParada >= 0f && f.parou && f.def.tipo == TipoDeEvento.Parada && EtapaAtual == Etapa.Parada) return true;
        }
        return false;
    }

    private void AbrirPedido(Evento e, float tempoPorPedido)
    {
        pedido = e;
        tempoDoPedido = 0f;
        paradoAoLado = 0f;
        limiteDoPedido = tempoPorPedido;
        VerbetePedido = e.def.pedido;
        hud.Pedido(TextoDoPedido(), Nivel.TemRegra(RegraFigura) ? VerbeteDatabase.Instancia.Buscar(VerbetePedido).ilustracao : null, Narracao.Voz.Disponivel);
        hud.TempoDoPedido(limiteDoPedido > 0f ? 1f : -1f);
        Tocar(config.somDePedido);
    }

    private string TextoDoPedido()
    {
        Verbete v = VerbeteDatabase.Instancia.Buscar(VerbetePedido);
        return string.Format(config.textoDoPedido, pedido.lugar.fala, pedido.objeto.artigo, v.palavra);
    }

    private void FecharPedido(bool acertou, string recado)
    {
        pedido.pedidoResolvido = true;
        pedido = null;
        VerbetePedido = null;
        if (!acertou)
        {
            PedidosPerdidos++;
        }
        hud.Pedido(null, null, false);
        hud.TempoDoPedido(-1f);
        hud.Marca(Vector3.zero, false);
        Dizer(recado);
        AtualizarCriterios();
    }

    // Botão "Ouvir" do pedido: só fala quando a criança aperta (sala cheia).
    public void OuvirPedido()
    {
        if (VerbetePedido != null && Narracao.Voz.Disponivel)
        {
            Narracao.Voz.Falar(TextoDoPedido());
        }
    }

    // Um toque na tela: o que estiver debaixo do dedo (coisa solta, chave, volante, rádio ou o safari).
    public void TocarNaTela(Vector2 ponto)
    {
        Ray raio = cameraDoJogo.ScreenPointToRay(ponto);
        RaycastHit[] acertos = Physics.RaycastAll(raio, 400f, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(acertos, (a, b) => a.distance.CompareTo(b.distance));
        ObjetoDoSafari melhor = null;
        foreach (RaycastHit h in acertos)
        {
            if (EtapaAtual == Etapa.Soltos)
            {
                // Um toque guarda uma coisa só (a de cima, se estiverem juntas).
                if (GuardarSolto(h.collider.gameObject)) return;
                continue;
            }
            if (h.collider == carro.chave)
            {
                LigarCarro();
                return;
            }
            if (EtapaAtual != Etapa.Passeio && EtapaAtual != Etapa.Chegada)
            {
                continue;
            }
            if (h.collider == carro.centroDoVolante)
            {
                Buzinar();
                return;
            }
            if (h.collider == carro.radio)
            {
                MudarRadio();
                return;
            }
            ObjetoDoSafari o = h.collider.GetComponent<ObjetoDoSafari>();
            if (o == null) continue;
            // Entre os que estão debaixo do dedo, ganha o que responde ao pedido.
            if (VerbetePedido != null && o.Aceita(VerbetePedido))
            {
                TocarObjeto(o);
                return;
            }
            if (melhor == null) melhor = o;
        }
        if (melhor != null)
        {
            TocarObjeto(melhor);
        }
    }

    public void TocarObjeto(ObjetoDoSafari objeto)
    {
        if ((EtapaAtual != Etapa.Passeio && EtapaAtual != Etapa.Chegada) || objeto == null)
        {
            return;
        }
        if (VerbetePedido != null && objeto.Aceita(VerbetePedido))
        {
            Verbete pedida = VerbeteDatabase.Instancia.Buscar(VerbetePedido);
            Acertos++;
            Tocar(config.somDeAcerto);
            Soltar(estrelinhas, objeto.transform.position + Vector3.up * 2f);
            objeto.Comemorar();
            MostrarFigurinha(pedida);
            FecharPedido(true, config.recadoDeAcerto);
            return;
        }
        // Tocou em outra coisa: aprende o nome dela. Só conta como erro se havia pedido, e uma vez por objeto.
        Verbete v = VerbeteDatabase.Instancia.Buscar(objeto.idDoVerbete);
        Tocar(config.somDeOutroObjeto);
        objeto.Comemorar();
        Dizer(string.Format(config.recadoDeOutroObjeto, v.palavra));
        if (VerbetePedido != null && jaTocados.Add(objeto))
        {
            ToquesErrados++;
            AtualizarCriterios();
        }
    }

    private void MostrarFigurinha(Verbete v)
    {
        if (figurinhaAberta != null)
        {
            StopCoroutine(figurinhaAberta);
        }
        figurinhaAberta = StartCoroutine(Figurinha(v));
    }

    private IEnumerator Figurinha(Verbete v)
    {
        hud.Figurinha(v.palavra, v.ilustracao);
        yield return new WaitForSeconds(config.tempoDaFigurinha);
        hud.Figurinha(null, null);
        figurinhaAberta = null;
    }

    // ------------------------------------------------------------------ rádio e trânsito

    public void MudarRadio()
    {
        if (Missao == null || Missao.radio.Length == 0)
        {
            return;
        }
        linhaDoRadio = (linhaDoRadio + 1) % Missao.radio.Length;
        UltimoRadio = Missao.radio[linhaDoRadio];
        Tocar(config.somDoRadio, 0.6f);
        if (radioAberto != null) StopCoroutine(radioAberto);
        radioAberto = StartCoroutine(MostrarRadio(config.prefixoDoRadio + " " + UltimoRadio));
    }

    private IEnumerator MostrarRadio(string texto)
    {
        hud.Radio(texto);
        yield return new WaitForSeconds(config.tempoDoRadio);
        hud.Radio(null);
        radioAberto = null;
    }

    // Carros que vêm na outra mão (níveis com trânsito mais cheio).
    private void CuidarDoTransito()
    {
        float intervalo = Nivel.Extra("transito", 0f);
        if (intervalo > 0f && percurso.modelosDeTransito.Length > 0 && percurso.S >= proximoCarroEm && EtapaAtual == Etapa.Passeio)
        {
            float s = Mathf.Min(percurso.S + 140f, percurso.Comprimento - 2f);
            GameObject modelo = percurso.modelosDeTransito[CarrosDoTransito % percurso.modelosDeTransito.Length];
            GameObject go = Instantiate(modelo, percurso.colocados);
            go.SetActive(true);
            transito.Add(new CarroDoTransito { carro = go.transform, s = s });
            CarrosDoTransito++;
            proximoCarroEm = percurso.S + intervalo * (0.7f + 0.6f * ((CarrosDoTransito * 37) % 10) / 10f);
        }
        for (int i = transito.Count - 1; i >= 0; i--)
        {
            CarroDoTransito c = transito[i];
            c.s -= config.velocidadeDoTransito * Time.deltaTime;
            if (c.s < percurso.S - 30f || c.s < 1f)
            {
                Destroy(c.carro.gameObject);
                transito.RemoveAt(i);
                continue;
            }
            c.carro.position = Chao(percurso.Ponto(c.s, -3.6f));
            c.carro.rotation = Quaternion.LookRotation(-percurso.Direcao(c.s));
        }
    }

    // ------------------------------------------------------------------ buzina

    // Toque no meio do volante (ou B/Espaço): a mão do motorista aperta a buzina.
    public void Buzinar()
    {
        if (EtapaAtual != Etapa.Passeio || Pausado || Time.time - tempoDaUltimaBuzina < config.intervaloEntreBuzinadas)
        {
            return;
        }
        tempoDaUltimaBuzina = Time.time;
        StartCoroutine(carro.ApertarBuzina());
        Tocar(config.somDaBuzina);
        StartCoroutine(MostrarSinalDaBuzina());

        PessoaDistraida pessoa = PessoaEmPerigo;
        if (pessoa != null)
        {
            pessoa.OuviuABuzina();
            BuzinadasCertas++;
            Soltar(estrelinhas, pessoa.transform.position + Vector3.up * 2f);
            Tocar(config.somDeAcerto);
            Dizer(config.recadoDaBuzinaCerta);
            AtualizarCriterios();
            StartCoroutine(AbrirCartaoSeNovo(IdDaBuzina));
            return;
        }

        BuzinadasAToa++;
        AtualizarCriterios();
        Balao(string.Format(config.balaoDaBuzinaAToa, config.trechoDaBuzina));
        if (!assustando)
        {
            StartCoroutine(Assustar());
        }
    }

    private IEnumerator MostrarSinalDaBuzina()
    {
        hud.SinalDaBuzina(true);
        yield return new WaitForSeconds(0.45f);
        hud.SinalDaBuzina(false);
    }

    // Todo mundo no carro dá um pulinho de susto.
    private IEnumerator Assustar()
    {
        assustando = true;
        Tocar(config.somDoSusto);
        Soltar(susto, cameraDoJogo.transform.position + cameraDoJogo.transform.forward * 0.9f);
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            float y = Mathf.Sin(t / 0.5f * Mathf.PI) * 0.09f;
            foreach (Transform a in carro.assentos) a.localPosition = new Vector3(a.localPosition.x, PosicaoDoAssentoY(a) + y, a.localPosition.z);
            yield return null;
        }
        foreach (Transform a in carro.assentos) a.localPosition = new Vector3(a.localPosition.x, PosicaoDoAssentoY(a), a.localPosition.z);
        assustando = false;
    }

    private readonly Dictionary<Transform, float> alturaDosAssentos = new Dictionary<Transform, float>();

    private float PosicaoDoAssentoY(Transform a)
    {
        if (!alturaDosAssentos.TryGetValue(a, out float y))
        {
            y = a.localPosition.y;
            alturaDosAssentos[a] = y;
        }
        return y;
    }

    // Nos níveis com dica: a marca aponta o meio do volante e o recado avisa.
    private void CuidarDaDicaDaBuzina()
    {
        PessoaDistraida pessoa = Nivel.TemRegra(RegraDicaBuzina) && EtapaAtual == Etapa.Passeio ? PessoaEmPerigo : null;
        if (pessoa == null)
        {
            pessoaAvisada = null;
            return;
        }
        if (pessoa != pessoaAvisada)
        {
            pessoaAvisada = pessoa;
            Dizer(config.recadoDeBuzinar);
        }
        Vector3 naTela = cameraDoJogo.WorldToScreenPoint(carro.botaoDaBuzina.position);
        hud.Marca(naTela, naTela.z > 0f);
    }

    // A pessoa chegou na hora da freada sem ninguém ter buzinado: o motorista para o carro.
    public void PerdeuABuzina(PessoaDistraida pessoa)
    {
        Freadas++;
        parado = true;
        Dizer(config.recadoDaFreada);
        Balao(string.Format(config.balaoDaFreada, config.trechoDaBuzina, config.trechoDaFreada));
        AtualizarCriterios();
        StartCoroutine(Freada(false));
    }

    public void CaminhoLivre()
    {
        parado = false;
    }

    // ------------------------------------------------------------------ cartões, estrelas, recados

    private IEnumerator AbrirCartaoSeNovo(string id)
    {
        if (GameProgress.VerbeteDesbloqueado(id))
        {
            yield break;
        }
        while (Pausado)
        {
            yield return null;
        }
        Pausado = true;
        Time.timeScale = 0f;
        bool fechou = false;
        CartaoVerbete.Mostrar(id, () => fechou = true);
        while (!fechou)
        {
            yield return null;
        }
        Time.timeScale = 1f;
        Pausado = false;
    }

    // Três critérios à vista da criança: safari, buzina na hora, sem buzinar à toa.
    // 3 = os três; 2 = dois; 1 = concluiu.
    protected override int CalcularEstrelas(ResultadoDoNivel resultado)
    {
        if (!resultado.concluiu)
        {
            return 0;
        }
        int ganhos = (SafariOk ? 1 : 0) + (BuzinaOk ? 1 : 0) + (SemSusto ? 1 : 0);
        return Mathf.Max(1, ganhos);
    }

    private void AtualizarCriterios()
    {
        // Durante o passeio, a estrela do safari e a da buzina ficam acesas enquanto ainda dá pra ganhar.
        bool safari = Pedidos == 0 || Acertos - ToquesErrados + (Pedidos - Acertos - PedidosPerdidos) >= Mathf.CeilToInt(Pedidos * config.fracaoDoSafari);
        bool buzina = Freadas == 0;
        hud.Criterios(safari, buzina, SemSusto, config.rotulosDosCriterios, string.Format(config.textoDoSafari, Acertos, Pedidos));
    }

    private IEnumerator Escurecer()
    {
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            hud.Escuro(t / 0.4f);
            yield return null;
        }
        hud.Escuro(1f);
    }

    private IEnumerator Clarear()
    {
        yield return null;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            hud.Escuro(1f - t / 0.4f);
            yield return null;
        }
        hud.Escuro(0f);
    }

    private void Dizer(string texto)
    {
        UltimoRecado = texto;
        hud.Recado(texto);
    }

    private void Balao(string texto)
    {
        UltimoBalao = texto;
        if (balaoAberto != null)
        {
            StopCoroutine(balaoAberto);
        }
        balaoAberto = StartCoroutine(MostrarBalao(texto));
    }

    private IEnumerator MostrarBalao(string texto)
    {
        hud.Balao(texto);
        yield return new WaitForSeconds(config.tempoDoBalao);
        hud.Balao(null);
        balaoAberto = null;
    }

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
