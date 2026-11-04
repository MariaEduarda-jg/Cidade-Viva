using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Rua Viva, parte 1: Passeio de Carro. Verbetes: cinto de segurança, passageiro e buzina.
//   Garagem: o jogador prende o próprio cinto e o de cada ocupante (arrastar a fivela até o fecho).
//     O carro só sai com todos de cinto. Primeira vez no perfil: abre os cartões de cinto e passageiro.
//   Passeio: o carro anda sozinho (o jogador é passageiro: não conduz). O motorista pede "Você vê um...?"
//     e o jogador toca no objeto pela janela. Acertar mostra a palavra e a ilustração, sem abrir o cartão.
//   Buzina: uma pessoa vem pra rua sem ter visto o carro: tocar a buzina na hora faz ela parar. Sem buzina,
//     o motorista freia. Buzinar sem precisar assusta o bichinho e perde a estrela "sem susto".
// Não existe derrota: pedido perdido e freada só custam estrela.
//
// O que cada campo do nível faz aqui:
//   velocidade               velocidade do carro (m/s).
//   quantidadeDeItens        pedidos do safari.
//   quantidadeDeObstaculos   pessoas distraídas (hora de buzinar).
//   extras: ocupantes, atentas (pessoas na calçada que não precisam de buzina), distracoes (outros objetos
//     pela janela), dificuldadeMinima, dificuldadeMaxima, distanciaMaxima (até onde ficam os objetos),
//     janelaDaBuzina (segundos pra buzinar), tempoPorPedido (0 = sem relógio).
//   regrasExtras: figura_no_pedido, carro_para, carro_devagar, dica_buzina, ciclistas.
public class RuaVivaCarroManager : MinigameBase
{
    public const string IdDoCinto = "cinto_de_seguranca";
    public const string IdDoPassageiro = "passageiro";
    public const string IdDaBuzina = "buzina";
    public const string RegraFigura = "figura_no_pedido";
    public const string RegraCarroPara = "carro_para";
    public const string RegraCarroDevagar = "carro_devagar";
    public const string RegraDicaBuzina = "dica_buzina";
    public const string RegraCiclistas = "ciclistas";

    private class Evento
    {
        public float s;
        public ObjetoDoSafari objeto; // pedido do safari
        public PessoaDistraida pessoa; // pessoa distraída ou atenta
        public bool resolvido;
    }

    [Header("Passeio de Carro")]
    public RuaVivaConfig config;
    public RuaVivaHUD hud;
    public AudioSource som;
    public Camera cameraDoJogo;
    public RuaVivaPercurso percurso;

    [Header("Garagem")]
    public Transform poseDaGaragem;
    public Transform carroDaGaragem;
    [Tooltip("A família, na ordem dos ocupantes: o jogador primeiro, depois o motorista.")]
    public Transform[] familia = new Transform[0];
    public Transform portaDoCarro;

    [Header("Bichinho e efeitos")]
    public Transform bichinho;
    public ParticleSystem estrelinhas;
    public ParticleSystem confete;
    public ParticleSystem susto;

    public enum Etapa { Selecao, Garagem, Saindo, Passeio, Chegou }

    public Etapa EtapaAtual { get; private set; } = Etapa.Selecao;
    public int OcupantesPresos { get; private set; }
    public int Ocupantes { get; private set; }
    public bool EsperandoCinto { get; private set; }
    public int Pedidos { get; private set; }
    public int Acertos { get; private set; }
    public int ToquesErrados { get; private set; }
    public int PedidosPerdidos { get; private set; }
    public int PessoasDistraidas { get; private set; }
    public int BuzinadasCertas { get; private set; }
    public int BuzinadasAToa { get; private set; }
    public int Freadas { get; private set; }
    public float Velocidade { get; private set; }
    public float TempoGasto { get; private set; }
    public bool Pausado { get; private set; }
    public string UltimoBalao { get; private set; }
    public string UltimoRecado { get; private set; }
    public float Fim { get; private set; }
    // O pedido em aberto (null = nenhum).
    public ObjetoDoSafari ObjetoPedido { get { return pedido != null ? pedido.objeto : null; } }
    public string VerbetePedido { get; private set; }
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
    public bool SafariOk { get { return Acertos - ToquesErrados >= Mathf.CeilToInt(Pedidos * config.fracaoDoSafari); } }
    public bool BuzinaOk { get { return BuzinadasCertas == PessoasDistraidas; } }
    public bool SemSusto { get { return BuzinadasAToa == 0; } }
    public IList<ObjetoDoSafari> ObjetosNaRua { get { return objetosNaRua; } }

    private readonly List<Evento> eventos = new List<Evento>();
    private readonly List<ObjetoDoSafari> objetosNaRua = new List<ObjetoDoSafari>();
    private readonly HashSet<ObjetoDoSafari> jaTocados = new HashSet<ObjetoDoSafari>();
    private Evento pedido;
    private float tempoDoPedido;
    private float limiteDoPedido;
    private float paradoAoLado;
    private bool parado;
    private float tempoDaUltimaBuzina = -10f;
    private Coroutine balaoAberto;
    private Coroutine figurinhaAberta;
    private bool assustando;
    private PessoaDistraida pessoaAvisada;
    private readonly List<Transform> ocupantes = new List<Transform>();

    private void Start()
    {
        hud.Balao(null);
        hud.Figurinha(null, null);
        hud.Pedido(null, null, false);
        hud.TempoDoPedido(-1f);
        hud.Marca(Vector3.zero, false);
        hud.SinalDaBuzina(false);
        hud.Escuro(0f);
        ColocarNaGaragem();
        if (EscolhendoNivel)
        {
            hud.Parte(false, false);
            return;
        }

        hud.Nivel(string.Format(config.textoDoNivel, NivelAtual));
        hud.Parte(true, false);
        Pedidos = Nivel.quantidadeDeItens;
        PessoasDistraidas = Nivel.quantidadeDeObstaculos;
        AtualizarCriterios();
        Ocupantes = Mathf.Clamp(Mathf.RoundToInt(Nivel.Extra("ocupantes", 2f)), 1, familia.Length);
        for (int i = 0; i < Ocupantes; i++)
        {
            ocupantes.Add(familia[i]);
        }
        Planejar();
        EtapaAtual = Etapa.Garagem;
        ProximoOcupante();
    }

    private void ColocarNaGaragem()
    {
        cameraDoJogo.transform.SetPositionAndRotation(poseDaGaragem.position, poseDaGaragem.rotation);
        bichinho.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ garagem

    private void ProximoOcupante()
    {
        if (OcupantesPresos >= Ocupantes)
        {
            StartCoroutine(Sair());
            return;
        }
        EsperandoCinto = true;
        hud.fivela.Voltar();
        hud.Ocupante(string.Format(config.textoDoOcupante, config.nomesDosOcupantes[OcupantesPresos]));
        Dizer(config.recadoDaGaragem);
        // Quem é a vez dá um pulinho.
        StartCoroutine(Pulinho(ocupantes[OcupantesPresos]));
    }

    private IEnumerator Pulinho(Transform quem)
    {
        Vector3 base_ = quem.position;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            quem.position = base_ + Vector3.up * Mathf.Sin(t / 0.4f * Mathf.PI) * 0.35f;
            yield return null;
        }
        quem.position = base_;
    }

    // A fivela chegou ao fecho (arrastando ou pelo teclado): prende o cinto de quem é a vez.
    public void PrenderCinto()
    {
        if (!EsperandoCinto)
        {
            return;
        }
        EsperandoCinto = false;
        hud.fivela.Encaixar();
        Tocar(config.somDaFivela);
        Transform quem = ocupantes[OcupantesPresos];
        OcupantesPresos++;
        StartCoroutine(Entrar(quem, OcupantesPresos == 1));
    }

    public void SoltouAFivela()
    {
        Tocar(config.somDeSoltar);
    }

    private IEnumerator Entrar(Transform quem, bool ehOJogador)
    {
        Soltar(estrelinhas, quem.position + Vector3.up * 1.2f);
        Vector3 de = quem.position;
        Vector3 ate = portaDoCarro.position;
        Animator animador = quem.GetComponentInChildren<Animator>();
        if (animador != null) animador.SetFloat("Velocidade", 0.5f);
        quem.rotation = Quaternion.LookRotation(ate - de);
        for (float t = 0f; t < config.pausaEntreOcupantes; t += Time.deltaTime)
        {
            quem.position = Vector3.Lerp(de, ate, t / config.pausaEntreOcupantes);
            yield return null;
        }
        quem.gameObject.SetActive(false);

        if (ehOJogador)
        {
            // Primeira vez neste perfil: entra no álbum, com o cartão.
            yield return AbrirCartaoSeNovo(IdDoCinto);
            yield return AbrirCartaoSeNovo(IdDoPassageiro);
        }
        ProximoOcupante();
    }

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

    private IEnumerator Sair()
    {
        EtapaAtual = Etapa.Saindo;
        Dizer(config.recadoDeSair);
        Tocar(config.somDoMotor);
        Vector3 de = carroDaGaragem.position;
        Vector3 ate = de + carroDaGaragem.forward * 9f;
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            float k = t / 1.2f;
            carroDaGaragem.position = Vector3.Lerp(de, ate, k * k);
            hud.Escuro(Mathf.Clamp01((k - 0.5f) * 2f));
            yield return null;
        }
        // Agora a câmera está dentro do carro, na rua.
        hud.Parte(false, true);
        bichinho.gameObject.SetActive(true);
        percurso.S = 0f;
        percurso.ColocarCamera(cameraDoJogo, config.alturaDaCamera, config.giroDaCamera);
        EtapaAtual = Etapa.Passeio;
        Dizer(config.recadoDoPasseio);
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            hud.Escuro(1f - t / 0.5f);
            yield return null;
        }
        hud.Escuro(0f);
    }

    // ------------------------------------------------------------------ preparar o percurso do nível

    private void Planejar()
    {
        // Sorteio com semente fixa por nível: o mesmo nível tem sempre o mesmo passeio.
        Random.State antes = Random.state;
        Random.InitState(137 * NivelAtual + 11);

        int dificuldadeMin = Mathf.RoundToInt(Nivel.Extra("dificuldadeMinima", 1f));
        int dificuldadeMax = Mathf.RoundToInt(Nivel.Extra("dificuldadeMaxima", 1f));
        float distanciaMaxima = Nivel.Extra("distanciaMaxima", 10f);
        int atentas = Mathf.RoundToInt(Nivel.Extra("atentas", 0f));
        int distracoes = Mathf.RoundToInt(Nivel.Extra("distracoes", 0f));
        float janela = Nivel.Extra("janelaDaBuzina", 4f);
        bool ciclistas = Nivel.TemRegra(RegraCiclistas);

        // Pedidos sem repetir, primeiro os da dificuldade pedida.
        List<ObjetoDoSafari> candidatos = new List<ObjetoDoSafari>();
        foreach (ObjetoDoSafari m in percurso.modelosDoSafari)
        {
            if (m.dificuldade >= dificuldadeMin && m.dificuldade <= dificuldadeMax) candidatos.Add(m);
        }
        Embaralhar(candidatos);

        // A ordem mistura pedidos e pessoas: 0 = pedido, 1 = distraída, 2 = atenta.
        List<int> ordem = new List<int>();
        for (int i = 0; i < Pedidos; i++) ordem.Add(0);
        for (int i = 0; i < PessoasDistraidas; i++) ordem.Add(1);
        for (int i = 0; i < atentas; i++) ordem.Add(2);
        Embaralhar(ordem);
        // O nível começa com um pedido: é a primeira coisa que a criança aprende.
        int primeiroPedido = ordem.IndexOf(0);
        if (primeiroPedido > 0) { ordem[primeiroPedido] = ordem[0]; ordem[0] = 0; }

        float s = config.comecoDoPasseio + config.antecipacaoDoPedido;
        int proximo = 0;
        int pedestre = 0;
        int posicao = 0;
        foreach (int tipo in ordem)
        {
            s = ProximoTrechoReto(s);
            Evento e = new Evento { s = s };
            if (tipo == 0)
            {
                ObjetoDoSafari modelo = candidatos[proximo % candidatos.Count];
                proximo++;
                e.objeto = Colocar(modelo, s, Random.Range(0f, 1f), distanciaMaxima);
            }
            else
            {
                bool ciclista = ciclistas && posicao % 2 == 1 && percurso.modeloDeCiclista != null;
                GameObject modeloDaPessoa = ciclista ? percurso.modeloDeCiclista : percurso.modelosDePedestre[pedestre++ % percurso.modelosDePedestre.Length];
                GameObject copia = Instantiate(modeloDaPessoa, percurso.colocados);
                copia.SetActive(true);
                e.pessoa = copia.AddComponent<PessoaDistraida>();
                e.pessoa.Preparar(this, percurso, s, tipo == 2, config.ladoDeOndeVem, config.beiraDaRua, janela, config.distanciaDaFreada, Nivel.velocidade, config.velocidadeDeAtravessar);
            }
            eventos.Add(e);
            posicao++;
            s += config.espacoEntreEventos;
        }
        Fim = Mathf.Min(s - config.espacoEntreEventos + config.fimDepoisDoUltimo, percurso.Comprimento - 5f);

        // Outros objetos pela janela: é entre eles que a criança procura.
        List<ObjetoDoSafari> outros = new List<ObjetoDoSafari>(percurso.modelosDoSafari);
        Embaralhar(outros);
        int colocadas = 0;
        for (int i = 0; colocadas < distracoes && i < eventos.Count * 3; i++)
        {
            Evento e = eventos[i % eventos.Count];
            float onde = e.s + 20f;
            ObjetoDoSafari modelo = outros[i % outros.Count];
            if (e.objeto == null || !percurso.EhReto(onde) || modelo.Aceita(e.objeto.idDoVerbete) || e.objeto.Aceita(modelo.idDoVerbete))
            {
                continue;
            }
            Colocar(modelo, onde, Random.Range(0f, 1f), distanciaMaxima);
            colocadas++;
        }

        // Casas nos vãos, onde não há objeto longe que elas pudessem esconder.
        if (percurso.modelosDeCasa.Length > 0)
        {
            int casa = 0;
            for (int i = 0; i + 1 < eventos.Count; i++)
            {
                if (eventos[i].pessoa == null && eventos[i + 1].objeto != null) continue;
                float onde = (eventos[i].s + eventos[i + 1].s) / 2f;
                if (!percurso.EhReto(onde)) continue;
                GameObject c = Instantiate(percurso.modelosDeCasa[casa++ % percurso.modelosDeCasa.Length], percurso.colocados);
                c.SetActive(true);
                c.transform.position = percurso.Posicao(onde) + percurso.Direita(onde) * 15f;
                c.transform.rotation = Quaternion.LookRotation(-percurso.Direita(onde));
            }
        }

        Random.state = antes;
        Physics.SyncTransforms();
    }

    private float ProximoTrechoReto(float s)
    {
        while (s < percurso.Comprimento - 40f && !percurso.EhReto(s))
        {
            s += 5f;
        }
        return s;
    }

    private ObjetoDoSafari Colocar(ObjetoDoSafari modelo, float s, float sorteio, float distanciaMaxima)
    {
        ObjetoDoSafari o = Instantiate(modelo, percurso.colocados);
        o.gameObject.SetActive(true);
        o.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        o.Preparar(1.25f, 1.6f);
        float minima = config.lateralMinima + o.meiaLargura;
        float lateral = Mathf.Lerp(minima, Mathf.Max(minima, distanciaMaxima), sorteio);
        o.transform.position = percurso.Posicao(s) + percurso.Direita(s) * lateral;
        o.transform.rotation = Quaternion.LookRotation(percurso.Direcao(s)) * Quaternion.Euler(0f, o.giroExtra, 0f);
        objetosNaRua.Add(o);
        return o;
    }

    private static void Embaralhar<T>(List<T> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T t = lista[i];
            lista[i] = lista[j];
            lista[j] = t;
        }
    }

    // ------------------------------------------------------------------ passeio

    private void Update()
    {
        if (EscolhendoNivel || Finalizado)
        {
            return;
        }
        if (EtapaAtual == Etapa.Garagem && EsperandoCinto && !Pausado
            && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            PrenderCinto();
        }
        if (EtapaAtual != Etapa.Passeio && EtapaAtual != Etapa.Chegou)
        {
            return;
        }
        if (!Pausado && EtapaAtual == Etapa.Passeio)
        {
            TempoGasto += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Space))
            {
                Buzinar();
            }
            if (Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                TocarNaTela(Input.mousePosition);
            }
            Dirigir();
            CuidarDosPedidos();
            if (percurso.S >= Fim)
            {
                Chegar();
            }
        }
        CuidarDaDicaDaBuzina();
        percurso.ColocarCamera(cameraDoJogo, config.alturaDaCamera, config.giroDaCamera);
    }

    // Nos níveis com dica: o botão pulsa, a marca aponta a pessoa e o recado avisa.
    private void CuidarDaDicaDaBuzina()
    {
        PessoaDistraida pessoa = Nivel.TemRegra(RegraDicaBuzina) ? PessoaEmPerigo : null;
        hud.PulsarBuzina(pessoa != null);
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
        Vector3 naTela = cameraDoJogo.WorldToScreenPoint(pessoa.transform.position + Vector3.up * 2.4f);
        hud.Marca(naTela, naTela.z > 0f);
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
            float d = pedido.s - percurso.S;
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
        float taxa = alvo < Velocidade ? config.freio : config.aceleracao;
        Velocidade = Mathf.MoveTowards(Velocidade, alvo, taxa * Time.deltaTime);
        percurso.S += Velocidade * Time.deltaTime;
    }

    private void CuidarDosPedidos()
    {
        // Abre o próximo pedido quando o objeto vem chegando.
        if (pedido == null)
        {
            float tempoPorPedido = Nivel.Extra("tempoPorPedido", 0f);
            float antecipacao = Mathf.Max(config.antecipacaoDoPedido, tempoPorPedido * Nivel.velocidade);
            foreach (Evento e in eventos)
            {
                if (e.objeto != null && !e.resolvido && percurso.S >= e.s - antecipacao)
                {
                    AbrirPedido(e, tempoPorPedido);
                    break;
                }
            }
        }
        if (pedido == null)
        {
            hud.Marca(Vector3.zero, false);
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
        bool passou = percurso.S > pedido.s + config.toleranciaDepoisDoObjeto;
        bool cansouDeEsperar = Nivel.TemRegra(RegraCarroPara) && paradoAoLado >= config.esperaAoLadoDoObjeto && percurso.S > pedido.s;
        if (passou || cansouDeEsperar)
        {
            FecharPedido(false, config.recadoDePassou);
            return;
        }

        // Nível com dica: uma marca mostra o objeto depois de um tempinho.
        bool comMarca = Nivel.TemRegra(RegraFigura) && tempoDoPedido >= config.tempoAteAMarca;
        if (comMarca)
        {
            Vector3 naTela = cameraDoJogo.WorldToScreenPoint(pedido.objeto.transform.position + Vector3.up * 2f);
            hud.Marca(naTela, naTela.z > 0f);
        }
        else
        {
            hud.Marca(Vector3.zero, false);
        }
    }

    private void AbrirPedido(Evento e, float tempoPorPedido)
    {
        pedido = e;
        tempoDoPedido = 0f;
        paradoAoLado = 0f;
        limiteDoPedido = tempoPorPedido;
        VerbetePedido = e.objeto.idDoVerbete;
        Verbete v = VerbeteDatabase.Instancia.Buscar(VerbetePedido);
        string texto = string.Format(config.textoDoPedido, e.objeto.artigo, v.palavra);
        hud.Pedido(texto, Nivel.TemRegra(RegraFigura) ? v.ilustracao : null, Narracao.Voz.Disponivel);
        hud.TempoDoPedido(limiteDoPedido > 0f ? 1f : -1f);
        Dizer(config.recadoDoPasseio);
        Tocar(config.somDePedido);
    }

    private void FecharPedido(bool acertou, string recado)
    {
        pedido.resolvido = true;
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
            Verbete v = VerbeteDatabase.Instancia.Buscar(VerbetePedido);
            Narracao.Voz.Falar(string.Format(config.textoDoPedido, pedido.objeto.artigo, v.palavra));
        }
    }

    // Um toque na tela: procura o objeto debaixo do dedo.
    public void TocarNaTela(Vector2 ponto)
    {
        Ray raio = cameraDoJogo.ScreenPointToRay(ponto);
        RaycastHit[] acertos = Physics.RaycastAll(raio, 400f, ~0, QueryTriggerInteraction.Collide);
        ObjetoDoSafari melhor = null;
        float menor = float.MaxValue;
        foreach (RaycastHit h in acertos)
        {
            ObjetoDoSafari o = h.collider.GetComponent<ObjetoDoSafari>();
            if (o == null) continue;
            // Entre os que estão debaixo do dedo, ganha o que responde ao pedido.
            if (VerbetePedido != null && o.Aceita(VerbetePedido))
            {
                TocarObjeto(o);
                return;
            }
            if (h.distance < menor) { menor = h.distance; melhor = o; }
        }
        if (melhor != null)
        {
            TocarObjeto(melhor);
        }
    }

    public void TocarObjeto(ObjetoDoSafari objeto)
    {
        if (EtapaAtual != Etapa.Passeio || objeto == null)
        {
            return;
        }
        Verbete v = VerbeteDatabase.Instancia.Buscar(objeto.idDoVerbete);
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

    // ------------------------------------------------------------------ buzina

    public void Buzinar()
    {
        if (EtapaAtual != Etapa.Passeio || Pausado || Time.time - tempoDaUltimaBuzina < config.intervaloEntreBuzinadas)
        {
            return;
        }
        tempoDaUltimaBuzina = Time.time;
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

    // O bichinho pula, dá uma volta no ar e cai sentado.
    private IEnumerator Assustar()
    {
        assustando = true;
        Tocar(config.somDoSusto);
        Soltar(susto, bichinho.position + bichinho.up * 0.3f);
        Vector3 pos = bichinho.localPosition;
        Quaternion giro = bichinho.localRotation;
        const float duracao = 0.8f;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = t / duracao;
            bichinho.localPosition = pos + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.55f;
            bichinho.localRotation = giro * Quaternion.Euler(0f, k * 720f, Mathf.Sin(k * Mathf.PI * 6f) * 15f);
            yield return null;
        }
        bichinho.localPosition = pos;
        bichinho.localRotation = giro;
        assustando = false;
    }

    // A pessoa chegou na hora da freada sem ninguém ter buzinado: o motorista para o carro.
    public void PerdeuABuzina(PessoaDistraida pessoa)
    {
        Freadas++;
        parado = true;
        Tocar(config.somDaFreada);
        Dizer(config.recadoDaFreada);
        Balao(string.Format(config.balaoDaFreada, config.trechoDaBuzina, config.trechoDaFreada));
        AtualizarCriterios();
        StartCoroutine(Solavanco());
    }

    public void CaminhoLivre()
    {
        parado = false;
    }

    private IEnumerator Solavanco()
    {
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            percurso.Tranco = Mathf.Sin(t / 0.6f * Mathf.PI) * 7f;
            yield return null;
        }
        percurso.Tranco = 0f;
    }

    // ------------------------------------------------------------------ fim

    private void Chegar()
    {
        EtapaAtual = Etapa.Chegou;
        hud.Pedido(null, null, false);
        hud.TempoDoPedido(-1f);
        hud.Marca(Vector3.zero, false);
        hud.PulsarBuzina(false);
        Velocidade = 0f;
        StartCoroutine(Fechar());
    }

    private IEnumerator Fechar()
    {
        while (Pausado)
        {
            yield return null;
        }
        Dizer(config.recadoDeChegada);
        Tocar(config.somDeComemoracao);
        Soltar(confete, cameraDoJogo.transform.position + cameraDoJogo.transform.forward * 8f);
        yield return new WaitForSeconds(config.pausaAntesDoFim);
        int pontos = Mathf.Max(40, 20 * Acertos + 30 * BuzinadasCertas - 10 * BuzinadasAToa - 5 * ToquesErrados);
        FinalizarNivel(new ResultadoDoNivel { concluiu = true, pontuacao = pontos, erros = BuzinadasAToa + Freadas + PedidosPerdidos, tempoGasto = TempoGasto });
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
        bool safari = Acertos - ToquesErrados + (Pedidos - Acertos - PedidosPerdidos) >= Mathf.CeilToInt(Pedidos * config.fracaoDoSafari);
        bool buzina = Freadas == 0;
        hud.Criterios(safari, buzina, SemSusto, config.rotulosDosCriterios, string.Format(config.textoDoSafari, Acertos, Pedidos));
    }

    // ------------------------------------------------------------------ recados, sons e partículas

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
