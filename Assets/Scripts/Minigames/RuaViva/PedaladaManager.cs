using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Rua Viva, parte 2: Pedalada. Verbetes: ciclista e engarrafamento.
// A criança põe o capacete e pedala; a bicicleta anda sozinha e ela troca de faixa.
// Onde há ciclovia ou ciclofaixa, pedala nela; ficar na pista dos carros (ou na calçada) custa a estrela
// "lugar certo", com um balão que cita a definição. No nível 3, os carros se acumulam até a ambulância
// não passar, e a criança toca no engarrafamento.
// Regras derivadas SOMENTE das definições (ver PedaladaConfig e a conferência do construtor).
public class PedaladaManager : MinigameBase
{
    public enum Etapa { Selecao, Roteiro, Capacete, Pedalando, Engarrafamento, Chegada }

    public const string RegraDica = "dica";
    public const string RegraCarros = "carros";

    [Header("Pedalada")]
    public PedaladaConfig config;
    public PedaladaHUD hud;
    public AudioSource som;
    public AudioSource somDaSirene;
    public Camera cameraDoJogo;
    [Tooltip("Uma rota por nível (na ordem).")]
    public PedaladaRota[] rotas = new PedaladaRota[0];
    [Tooltip("A bicicleta com o menino (filho \"Visual\" balança na batida).")]
    public Transform ciclista;
    public Transform visualDoCiclista;
    public Animator animadorDoCiclista;
    [Tooltip("Pernas pedalando e rodas girando (só aparência).")]
    public PedalarNaBicicleta pedalar;
    [Tooltip("Tamanho do capacete quando chega na cabeça (o do banquinho tem tamanho 1). A cabeça do menino é grande: ~1,15.")]
    public float escalaDoCapaceteNaCabeca = 1.15f;
    [Tooltip("Onde ficam as coisas da cabeça, ao lado da bicicleta, antes de sair.")]
    public Transform banquinho;
    public CoisaDaCabeca[] coisasDaCabeca = new CoisaDaCabeca[0];
    [Tooltip("Carros do trânsito (inativos; o manager reaproveita).")]
    public Transform[] carrosDoTransito = new Transform[0];
    public ParticleSystem estrelinhas;
    public ParticleSystem confete;

    // ------------------------------------------------------------------ estado (o piloto lê)
    public Etapa EtapaAtual { get; private set; } = Etapa.Selecao;
    public RoteiroDaPedalada Roteiro { get; private set; }
    public PedaladaRota Rota { get; private set; }
    public float S { get; private set; }
    public int Faixa { get; private set; }
    public int TrechoAtual { get; private set; }
    public float Comprimento { get; private set; }
    public bool CapaceteDePrimeira { get; private set; } = true;
    public bool ComCapacete { get; private set; }
    public int VezesNoLugarErrado { get; private set; }
    public int Batidas { get; private set; }
    public bool ViuOEngarrafamento { get; private set; }
    public bool NoTempo { get; private set; } = true;
    public bool TemTempo { get; private set; }
    public float TempoRestante { get; private set; }
    public float TempoGasto { get; private set; }
    public string UltimoRecado { get; private set; } = "";
    public string UltimoBalao { get; private set; } = "";
    public bool Pausado { get; private set; }

    private float[] inicios = new float[0];
    private float x;
    private float velocidadeLateral;
    private float parado;
    private float noLugarErrado;
    private bool contouNoTrecho;
    private int avisado = -1;
    private bool[] obstaculoDerrubado = new bool[0];
    private readonly List<Carro> carros = new List<Carro>();
    private float proximoCarro;
    private float tempoNoEngarrafamento;
    private Coroutine balaoAberto;
    private Vector3 cabecaLocal;
    private Transform cabeca;
    private readonly List<Pedestre> pedestres = new List<Pedestre>();
    private static readonly int Velocidade = Animator.StringToHash("Velocidade");

    private class Carro
    {
        public Transform t;
        public int faixa;
        public float x;
        public float z;
        public float v;
        public float parado;
        public bool buzinou;
    }

    private class Pedestre
    {
        public Transform t;
        public int caminho;
        public float d;
        public float comprimento;
        public float sentido;
        public float lado;
        public Animator animator;
    }

    [Tooltip("Velocidade dos pedestres nas calçadas (m/s), combinando com o passo da animação de andar.")]
    public float velocidadeDosPedestres = 1.35f;
    [Tooltip("Cada pedestre anda à direita do caminho (metros do meio), pra quem vai e quem vem não se atravessarem.")]
    public float afastamentoDosPedestres = 0.45f;
    [Tooltip("Se a bicicleta vem pela calçada, o pedestre vai pra beira (metros do meio).")]
    public float beiraDaCalcada = 1.0f;

    // ------------------------------------------------------------------ começo

    private void Start()
    {
        foreach (Transform c in carrosDoTransito)
        {
            c.gameObject.SetActive(false);
        }
        for (int i = 0; i < rotas.Length; i++)
        {
            rotas[i].gameObject.SetActive(i == NivelAtual - 1);
        }
        hud.Balao(null);
        hud.Marca(Vector3.zero, false);
        hud.Pedalando(false);
        hud.Tempo(-1f);

        if (EscolhendoNivel)
        {
            hud.Jogando(false);
            hud.Roteiro(null, null, null, false);
            return;
        }

        int indice = Mathf.Clamp(NivelAtual - 1, 0, config.roteiros.Length - 1);
        Roteiro = config.roteiros[indice];
        Rota = rotas[Mathf.Clamp(NivelAtual - 1, 0, rotas.Length - 1)];
        inicios = new float[Roteiro.trechos.Length + 1];
        for (int i = 0; i < Roteiro.trechos.Length; i++)
        {
            inicios[i + 1] = inicios[i] + Roteiro.trechos[i].comprimento;
        }
        Comprimento = inicios[Roteiro.trechos.Length];
        obstaculoDerrubado = new bool[Roteiro.trechos.Length];
        TemTempo = Nivel.tempoEmSegundos > 0f;
        TempoRestante = Nivel.tempoEmSegundos;

        Faixa = Roteiro.trechos[0].FaixaCerta;
        x = Faixa * config.larguraDaFaixa;
        S = 0f;
        PosicionarCiclista();
        banquinho.SetPositionAndRotation(ciclista.position + ciclista.right * 1.25f + ciclista.forward * 0.9f, ciclista.rotation);
        PrepararPedestres();
        if (Rota.setaDaDica != null)
        {
            Rota.setaDaDica.gameObject.SetActive(false);
        }

        // As coisas da cabeça do nível, em ordem embaralhada (fixa por nível).
        var usadas = new List<CoisaDaCabeca>();
        foreach (CoisaDaCabeca c in coisasDaCabeca)
        {
            bool usa = System.Array.IndexOf(Roteiro.coisasDaCabeca, c.id) >= 0;
            c.gameObject.SetActive(usa);
            if (usa) usadas.Add(c);
        }
        var sorteio = new System.Random(NivelAtual * 7 + 3);
        for (int i = usadas.Count - 1; i > 0; i--)
        {
            int j = sorteio.Next(i + 1);
            (usadas[i], usadas[j]) = (usadas[j], usadas[i]);
        }
        for (int i = 0; i < usadas.Count; i++)
        {
            usadas[i].transform.position = banquinho.position + banquinho.right * ((i - (usadas.Count - 1) / 2f) * 0.42f) + Vector3.up * 0.47f;
        }
        cabeca = AcharCabeca();

        hud.Jogando(false);
        hud.Nivel(string.Format(config.textoDoNivel, NivelAtual));
        EtapaAtual = Etapa.Roteiro;
        hud.Roteiro(Roteiro.titulo, Roteiro.texto, Roteiro.novidade, Narracao.Voz.Disponivel);
        AtualizarCriterios();
        PoseDoCapacete(true);
    }

    public void ComecarRoteiro()
    {
        if (EtapaAtual != Etapa.Roteiro)
        {
            return;
        }
        hud.Roteiro(null, null, null, false);
        hud.Jogando(true);
        EtapaAtual = Etapa.Capacete;
        Dizer(config.recadoDoCapacete);
    }

    public void OuvirRoteiro()
    {
        if (Roteiro != null && Narracao.Voz.Disponivel)
        {
            Narracao.Voz.Falar(Roteiro.titulo + ". " + Roteiro.texto);
        }
    }

    // ------------------------------------------------------------------ capacete

    // Devolve true se era o capacete.
    public bool EscolherCoisa(string id)
    {
        if (EtapaAtual != Etapa.Capacete || Pausado)
        {
            return false;
        }
        CoisaDaCabeca coisa = null;
        foreach (CoisaDaCabeca c in coisasDaCabeca)
        {
            if (c.id == id && c.gameObject.activeSelf) coisa = c;
        }
        if (coisa == null)
        {
            return false;
        }
        if (coisa.id != "capacete")
        {
            CapaceteDePrimeira = false;
            Tocar(config.somDeErro);
            Balao(string.Format(config.balaoDaCabecaErrada, coisa.nome, config.trechoDoCapacete));
            StartCoroutine(Balancar(coisa.transform));
            AtualizarCriterios();
            return false;
        }
        StartCoroutine(PorCapacete(coisa));
        return true;
    }

    // Balança e volta sempre pra posição reta (identidade): se vários balanços se sobrepõem
    // (a criança aperta muitas vezes), nenhum guarda a inclinação do outro.
    private readonly HashSet<Transform> balancando = new HashSet<Transform>();

    private IEnumerator Balancar(Transform t)
    {
        if (!balancando.Add(t))
        {
            yield break;
        }
        for (float k = 0f; k < 0.4f; k += Time.deltaTime)
        {
            t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 40f) * 12f);
            yield return null;
        }
        t.localRotation = Quaternion.identity;
        balancando.Remove(t);
    }

    private IEnumerator PorCapacete(CoisaDaCabeca capacete)
    {
        EtapaAtual = Etapa.Chegada; // trava os toques enquanto a animação roda
        Tocar(config.somDoCapacete);
        Vector3 de = capacete.transform.position;
        Vector3 ate = PontoDaCabeca(capacete);
        Quaternion rot = capacete.transform.rotation;
        Quaternion rotFinal = ciclista.rotation;
        // O capacete cresce no ar até o tamanho da cabeça do menino.
        Vector3 escalaDe = capacete.transform.localScale;
        Vector3 escalaAte = escalaDe * escalaDoCapaceteNaCabeca;
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            float k = t / 0.6f;
            capacete.transform.position = Vector3.Lerp(de, ate, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.5f;
            capacete.transform.rotation = Quaternion.Slerp(rot, rotFinal, k);
            capacete.transform.localScale = Vector3.Lerp(escalaDe, escalaAte, k);
            yield return null;
        }
        capacete.transform.SetPositionAndRotation(ate, rotFinal);
        capacete.transform.localScale = escalaAte;
        capacete.transform.SetParent(cabeca != null ? cabeca : visualDoCiclista, true);
        ComCapacete = true;
        foreach (CoisaDaCabeca c in coisasDaCabeca)
        {
            if (c != capacete) c.gameObject.SetActive(false);
        }
        Soltar(estrelinhas, ate);
        Dizer(config.recadoDoCapaceteCerto);
        yield return AbrirCartaoSeNovo("ciclista");
        yield return new WaitForSeconds(0.6f);
        Sair();
    }

    private Transform AcharCabeca()
    {
        foreach (Transform t in visualDoCiclista.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (n.Contains("head") && !n.Contains("end") && !n.Contains("top"))
            {
                return t;
            }
        }
        return null;
    }

    // O alto da cabeça, medido pelas malhas do menino.
    private Vector3 PontoDaCabeca(CoisaDaCabeca coisa)
    {
        float topo = float.MinValue;
        foreach (Renderer r in visualDoCiclista.GetComponentsInChildren<Renderer>())
        {
            if (r.GetComponentInParent<CoisaDaCabeca>() != null || !(r is SkinnedMeshRenderer)) continue;
            topo = Mathf.Max(topo, r.bounds.max.y);
        }
        Vector3 c = cabeca != null ? cabeca.position : visualDoCiclista.position;
        if (topo == float.MinValue) topo = c.y + 0.25f;
        return new Vector3(c.x, topo - coisa.altura * 0.7f * escalaDoCapaceteNaCabeca, c.z);
    }

    // ------------------------------------------------------------------ pedalar

    private void Sair()
    {
        EtapaAtual = Etapa.Pedalando;
        hud.Pedalando(true);
        Dizer(config.recadoDaSaida);
        Tocar(config.somDaCampainha, 0.7f);
        proximoCarro = 0f;
        if (Nivel.TemRegra(RegraCarros))
        {
            // Já tem carros andando lá na frente.
            for (float z = 18f; z < 80f; z += 22f)
            {
                PorCarro(z, z % 44f < 22f ? -1 : 0);
            }
        }
    }

    // Pros botões da tela (evento de botão precisa de void).
    public void BotaoDeFaixa(int lado)
    {
        MudarDeFaixa(lado);
    }

    public bool MudarDeFaixa(int lado)
    {
        if (EtapaAtual != Etapa.Pedalando || Pausado || parado > 0f)
        {
            return false;
        }
        int nova = Mathf.Clamp(Faixa + lado, -1, 1);
        if (nova == Faixa)
        {
            return false;
        }
        // Canteiro: não dá pra entrar (a não ser que ele acabe logo ali na frente).
        int prox = TrechoEm(S) + 1;
        bool acabaLogo = NaCurva(S) || (prox < Roteiro.trechos.Length && inicios[prox] - S <= 4f && Roteiro.trechos[prox].Faixa(nova) != TipoDeFaixa.Grama);
        if (TipoEm(S, nova) == TipoDeFaixa.Grama && !acabaLogo)
        {
            StartCoroutine(Balancar(visualDoCiclista));
            Dizer(config.recadoDoCanteiro);
            return false;
        }
        Faixa = nova;
        Tocar(config.somDeTrocarDeFaixa, 1f);
        return true;
    }

    // Nos últimos metros de um trecho em que a ciclovia faz a curva pra outra faixa.
    private bool NaCurva(float s)
    {
        int k = TrechoEm(s);
        if (k + 1 >= Roteiro.trechos.Length)
        {
            return false;
        }
        float curva = TrechoDaPedalada.Curva(Roteiro.trechos[k], Roteiro.trechos[k + 1]);
        return curva > 0f && s >= inicios[k + 1] - curva;
    }

    private TipoDeFaixa TipoEm(float s, int faixa)
    {
        return Roteiro.trechos[TrechoEm(s)].Faixa(faixa);
    }

    private int TrechoEm(float s)
    {
        for (int i = Roteiro.trechos.Length - 1; i >= 0; i--)
        {
            if (s >= inicios[i]) return i;
        }
        return 0;
    }

    private void Update()
    {
        if (EtapaAtual == Etapa.Selecao || Roteiro == null)
        {
            return;
        }
        LerTeclado();
        MoverPedestres();
        if (Rota.luzesDaSirene.Length > 0)
        {
            bool liga = Mathf.FloorToInt(Time.time * 4f) % 2 == 0;
            for (int i = 0; i < Rota.luzesDaSirene.Length; i++)
            {
                Rota.luzesDaSirene[i].enabled = (i % 2 == 0) == liga;
            }
        }

        if (EtapaAtual == Etapa.Pedalando && !Pausado)
        {
            Pedalar(Time.deltaTime);
            MoverCarros(Time.deltaTime);
        }
        else if (EtapaAtual == Etapa.Engarrafamento && !Pausado)
        {
            MoverCarros(Time.deltaTime);
            ContarTempo(Time.deltaTime);
            tempoNoEngarrafamento += Time.deltaTime;
            bool marca = tempoNoEngarrafamento >= config.tempoAteAMarca && Rota.ambulancia != null;
            Vector3 p = marca ? cameraDoJogo.WorldToScreenPoint(Rota.ambulancia.position + Vector3.up * 2.6f) : Vector3.zero;
            hud.Marca(p, marca && p.z > 0f);
        }
        AnimarCiclista();
    }

    private void LateUpdate()
    {
        if (EtapaAtual == Etapa.Pedalando || EtapaAtual == Etapa.Chegada && ComCapacete)
        {
            Vector3 alvo = ciclista.position + new Vector3(-x * 0.35f, 2.7f, -5.6f);
            cameraDoJogo.transform.position = Vector3.Lerp(cameraDoJogo.transform.position, alvo, 1f - Mathf.Exp(-6f * Time.deltaTime));
            Quaternion olhar = Quaternion.LookRotation(ciclista.position + new Vector3(-x * 0.3f, 0.9f, 7f) - cameraDoJogo.transform.position);
            cameraDoJogo.transform.rotation = Quaternion.Slerp(cameraDoJogo.transform.rotation, olhar, 1f - Mathf.Exp(-6f * Time.deltaTime));
        }
        else if (EtapaAtual == Etapa.Engarrafamento)
        {
            Vector3 meio = Rota.Mundo(-config.larguraDaFaixa * 0.5f, inicios[TrechoAtual] + Roteiro.trechos[TrechoAtual].comprimento * 0.4f);
            Vector3 alvo = ciclista.position + new Vector3(1.6f, 5.2f, -6f);
            cameraDoJogo.transform.position = Vector3.Lerp(cameraDoJogo.transform.position, alvo, 1f - Mathf.Exp(-3f * Time.deltaTime));
            Quaternion olhar = Quaternion.LookRotation(meio - cameraDoJogo.transform.position);
            cameraDoJogo.transform.rotation = Quaternion.Slerp(cameraDoJogo.transform.rotation, olhar, 1f - Mathf.Exp(-3f * Time.deltaTime));
        }
    }

    private void PoseDoCapacete(bool jaLa)
    {
        Vector3 pos = ciclista.position + ciclista.right * 1.9f + ciclista.forward * 2.3f + Vector3.up * 1.5f;
        Quaternion rot = Quaternion.LookRotation(ciclista.position + ciclista.right * 0.55f + Vector3.up * 0.9f - pos);
        cameraDoJogo.transform.SetPositionAndRotation(pos, rot);
    }

    private void LerTeclado()
    {
        if (EtapaAtual == Etapa.Roteiro && (Teclado.Apertou(KeyCode.Return) || Teclado.Apertou(KeyCode.Space)))
        {
            ComecarRoteiro();
            return;
        }
        if (EtapaAtual == Etapa.Capacete)
        {
            for (int i = 0; i < 3; i++)
            {
                if (Teclado.Apertou(KeyCode.Alpha1 + i) || Teclado.Apertou(KeyCode.Keypad1 + i))
                {
                    CoisaDaCabeca c = CoisaNaPosicao(i);
                    if (c != null) EscolherCoisa(c.id);
                }
            }
        }
        if (EtapaAtual == Etapa.Pedalando)
        {
            if (Teclado.Apertou(KeyCode.LeftArrow) || Teclado.Apertou(KeyCode.A)) MudarDeFaixa(-1);
            if (Teclado.Apertou(KeyCode.RightArrow) || Teclado.Apertou(KeyCode.D)) MudarDeFaixa(1);
        }
        if (EtapaAtual == Etapa.Engarrafamento && (Teclado.Apertou(KeyCode.Return) || Teclado.Apertou(KeyCode.Space)))
        {
            TocarNoEngarrafamento();
        }
        if (Toque.Apertou && !(Toque.SobreAInterface()))
        {
            TocarNaTela(Toque.Posicao);
        }
    }

    // A coisa da cabeça que está na posição i (da esquerda pra direita, como a criança vê).
    private CoisaDaCabeca CoisaNaPosicao(int i)
    {
        var ativas = new List<CoisaDaCabeca>();
        foreach (CoisaDaCabeca c in coisasDaCabeca)
        {
            if (c.gameObject.activeSelf && c.transform.parent == banquinho) ativas.Add(c);
        }
        ativas.Sort((a, b) => cameraDoJogo.WorldToScreenPoint(a.transform.position).x.CompareTo(cameraDoJogo.WorldToScreenPoint(b.transform.position).x));
        return i < ativas.Count ? ativas[i] : null;
    }

    public void TocarNaTela(Vector2 ponto)
    {
        if (Pausado)
        {
            return;
        }
        Ray raio = cameraDoJogo.ScreenPointToRay(ponto);
        if (!Physics.Raycast(raio, out RaycastHit hit, 200f))
        {
            return;
        }
        if (EtapaAtual == Etapa.Capacete)
        {
            CoisaDaCabeca c = hit.collider.GetComponentInParent<CoisaDaCabeca>();
            if (c != null) EscolherCoisa(c.id);
        }
        else if (EtapaAtual == Etapa.Engarrafamento)
        {
            foreach (Transform t in Rota.engarrafamento)
            {
                if (t != null && hit.collider.transform.IsChildOf(t))
                {
                    TocarNoEngarrafamento();
                    return;
                }
            }
        }
    }

    private void Pedalar(float dt)
    {
        ContarTempo(dt);
        if (parado > 0f)
        {
            parado -= dt;
        }
        else
        {
            S += Nivel.velocidade * dt;
        }

        int k = TrechoEm(S);
        if (k != TrechoAtual)
        {
            EntrarNoTrecho(k);
        }
        TrechoDaPedalada trecho = Roteiro.trechos[k];

        // Obstáculo no meio do trecho.
        if (Rota.obstaculos.Length > k && Rota.obstaculos[k] != null && !obstaculoDerrubado[k])
        {
            float zObs = inicios[k] + trecho.comprimento * 0.5f;
            if (Faixa == trecho.FaixaCerta && S >= zObs - 0.9f && S < zObs + 0.5f)
            {
                obstaculoDerrubado[k] = true;
                Rota.obstaculos[k].localRotation *= Quaternion.Euler(0f, 0f, 80f);
                Bater();
            }
        }

        // Lugar errado: pista dos carros com ciclovia/ciclofaixa no trecho, ou calçada.
        TipoDeFaixa aqui = trecho.Faixa(Faixa);
        bool temDaBicicleta = trecho.FaixaDaBicicleta != 2;
        bool errado = aqui == TipoDeFaixa.Calcada || (aqui == TipoDeFaixa.Carros && temDaBicicleta);
        // Na curva da ciclovia, a bicicleta atravessa as faixas: não conta.
        if (NaCurva(S))
        {
            errado = false;
        }
        // Já indo pra faixa certa do trecho que vem (depois do aviso) não conta.
        if (k + 1 < Roteiro.trechos.Length && Faixa == Roteiro.trechos[k + 1].FaixaCerta && inicios[k + 1] - S <= config.antecedenciaDoAviso + 2f)
        {
            errado = false;
        }
        if (errado && parado <= 0f)
        {
            noLugarErrado += dt;
            if (noLugarErrado >= config.toleranciaNoLugarErrado && !contouNoTrecho)
            {
                contouNoTrecho = true;
                VezesNoLugarErrado++;
                Tocar(config.somDeErro);
                Balao(aqui == TipoDeFaixa.Calcada
                    ? string.Format(config.balaoNaCalcada, config.trechoDaCalcada, OndeVai(trecho))
                    : string.Format(config.balaoNaPistaDosCarros, NomeDaFaixa(trecho), config.trechoDaCiclovia));
                AtualizarCriterios();
            }
        }
        else
        {
            noLugarErrado = 0f;
        }

        // Aviso e dica do trecho que vem.
        if (k + 1 < Roteiro.trechos.Length)
        {
            float faltam = inicios[k + 1] - S;
            if (faltam <= config.antecedenciaDoAviso && avisado < k + 1)
            {
                avisado = k + 1;
                if (!string.IsNullOrEmpty(Roteiro.trechos[k + 1].aviso)) Dizer(Roteiro.trechos[k + 1].aviso);
            }
            MostrarDica(k + 1, faltam);
        }
        else if (Rota.setaDaDica != null)
        {
            Rota.setaDaDica.gameObject.SetActive(false);
        }

        // Engarrafamento: a bicicleta para um pouco antes, pra olhar.
        for (int i = k; i < Mathf.Min(k + 2, Roteiro.trechos.Length); i++)
        {
            if (Roteiro.trechos[i].engarrafamento && !ViuOEngarrafamento && S >= inicios[i] - config.paradaNoEngarrafamento)
            {
                PararNoEngarrafamento(i);
                break;
            }
        }

        PosicionarCiclista();

        if (S >= Comprimento)
        {
            StartCoroutine(Chegar());
        }
    }

    private void EntrarNoTrecho(int k)
    {
        TrechoAtual = k;
        noLugarErrado = 0f;
        contouNoTrecho = false;
        // O canteiro começa onde a bicicleta estava: bate e vai pra faixa certa.
        if (Roteiro.trechos[k].Faixa(Faixa) == TipoDeFaixa.Grama)
        {
            Bater();
            Faixa = Roteiro.trechos[k].FaixaCerta;
        }
    }

    private void MostrarDica(int proximo, float faltam)
    {
        if (Rota.setaDaDica == null)
        {
            return;
        }
        bool mostra = Nivel.TemRegra(RegraDica) && faltam <= config.antecedenciaDoAviso && faltam > -3f
                      && Roteiro.trechos[proximo].FaixaCerta != Faixa;
        Rota.setaDaDica.gameObject.SetActive(mostra);
        if (mostra)
        {
            float lx = Roteiro.trechos[proximo].FaixaCerta * config.larguraDaFaixa;
            Rota.setaDaDica.position = Rota.Mundo(lx, inicios[proximo] + 1f) + Vector3.up * (1.6f + Mathf.Sin(Time.time * 5f) * 0.15f);
        }
    }

    private string NomeDaFaixa(TrechoDaPedalada t)
    {
        return t.Faixa(t.FaixaDaBicicleta) == TipoDeFaixa.Ciclovia ? "ciclovia" : "ciclofaixa";
    }

    private string OndeVai(TrechoDaPedalada t)
    {
        if (t.FaixaDaBicicleta == 2) return config.naRua;
        return t.Faixa(t.FaixaDaBicicleta) == TipoDeFaixa.Ciclovia ? config.naCiclovia : config.naCiclofaixa;
    }

    private void Bater()
    {
        Batidas++;
        parado = config.paradaNaBatida;
        Tocar(config.somDaBatida);
        Dizer(config.recadoDaBatida);
        Soltar(estrelinhas, ciclista.position + Vector3.up * 1.4f);
        StartCoroutine(Balancar(visualDoCiclista));
        AtualizarCriterios();
    }

    private void PosicionarCiclista()
    {
        x = Mathf.SmoothDamp(x, Faixa * config.larguraDaFaixa, ref velocidadeLateral, config.tempoDeTrocaDeFaixa * 0.5f);
        ciclista.position = Rota.Mundo(x, S);
        ciclista.rotation = Rota.transform.rotation * Quaternion.Euler(0f, Mathf.Clamp(velocidadeLateral * 4f, -25f, 25f), 0f);
    }

    private void AnimarCiclista()
    {
        if (pedalar != null)
        {
            pedalar.velocidade = EtapaAtual == Etapa.Pedalando && !Pausado && parado <= 0f ? Nivel.velocidade : 0f;
        }
        if (animadorDoCiclista != null && animadorDoCiclista.isActiveAndEnabled)
        {
            animadorDoCiclista.SetFloat(Velocidade, 0f);
        }
    }

    private void ContarTempo(float dt)
    {
        TempoGasto += dt;
        if (!TemTempo)
        {
            return;
        }
        TempoRestante = Mathf.Max(0f, TempoRestante - dt);
        hud.Tempo(TempoRestante);
        if (TempoRestante <= 0f && NoTempo)
        {
            NoTempo = false;
            Dizer(config.recadoDoTempo);
            AtualizarCriterios();
        }
    }

    // ------------------------------------------------------------------ engarrafamento

    private void PararNoEngarrafamento(int trecho)
    {
        EtapaAtual = Etapa.Engarrafamento;
        TrechoAtual = trecho;
        tempoNoEngarrafamento = 0f;
        hud.Pedalando(false);
        Dizer(config.recadoDoEngarrafamento);
        if (somDaSirene != null && config.somDaSirene != null)
        {
            somDaSirene.clip = config.somDaSirene;
            somDaSirene.loop = true;
            somDaSirene.Play();
        }
        if (Rota.setaDaDica != null) Rota.setaDaDica.gameObject.SetActive(false);
    }

    public void TocarNoEngarrafamento()
    {
        if (EtapaAtual != Etapa.Engarrafamento || Pausado || ViuOEngarrafamento)
        {
            return;
        }
        ViuOEngarrafamento = true;
        StartCoroutine(DepoisDoEngarrafamento());
    }

    private IEnumerator DepoisDoEngarrafamento()
    {
        hud.Marca(Vector3.zero, false);
        Tocar(config.somDeComemoracao);
        if (Rota.ambulancia != null) Soltar(estrelinhas, Rota.ambulancia.position + Vector3.up * 2.5f);
        yield return AbrirCartaoSeNovo("engarrafamento");
        Balao(string.Format(config.balaoDoEngarrafamento, config.trechoDoAcumulo, config.trechoDaPassagem));
        // A fila ocupa as faixas dos carros: a bicicleta segue pela ciclofaixa.
        TrechoDaPedalada t = Roteiro.trechos[TrechoAtual];
        if (t.FaixaDaBicicleta != 2) Faixa = t.FaixaDaBicicleta;
        Dizer(config.recadoDepoisDoEngarrafamento);
        hud.Pedalando(true);
        TrechoAtual = TrechoEm(S);
        EtapaAtual = Etapa.Pedalando;
    }

    // ------------------------------------------------------------------ carros do trânsito

    private void PorCarro(float z, int faixa)
    {
        if (TipoEm(z, faixa) != TipoDeFaixa.Carros || z < 0f || z > Comprimento - 4f)
        {
            return;
        }
        foreach (Carro c in carros)
        {
            if (c.faixa == faixa && Mathf.Abs(c.z - z) < config.distanciaEntreCarros + 4f) return;
        }
        if (EmEngarrafamento(z, faixa, 6f))
        {
            return;
        }
        foreach (Transform t in carrosDoTransito)
        {
            if (t.gameObject.activeSelf) continue;
            t.gameObject.SetActive(true);
            carros.Add(new Carro { t = t, faixa = faixa, x = faixa * config.larguraDaFaixa, z = z, v = config.velocidadeDosCarros });
            PosicionarCarro(carros[carros.Count - 1]);
            return;
        }
    }

    // O z da traseira da fila do engarrafamento nesta faixa (ou infinito).
    private bool EmEngarrafamento(float z, int faixa, float folga)
    {
        for (int i = 0; i < Roteiro.trechos.Length; i++)
        {
            TrechoDaPedalada t = Roteiro.trechos[i];
            if (t.engarrafamento && t.Faixa(faixa) == TipoDeFaixa.Carros && z > inicios[i] - folga && z < inicios[i + 1])
            {
                return true;
            }
        }
        return false;
    }

    private float FimDaFila(float z, int faixa)
    {
        for (int i = 0; i < Roteiro.trechos.Length; i++)
        {
            TrechoDaPedalada t = Roteiro.trechos[i];
            if (t.engarrafamento && t.Faixa(faixa) == TipoDeFaixa.Carros && inicios[i] + 1f >= z)
            {
                return inicios[i] + 1f;
            }
        }
        return float.MaxValue;
    }

    private void MoverCarros(float dt)
    {
        if (Nivel.TemRegra(RegraCarros) && EtapaAtual == Etapa.Pedalando)
        {
            proximoCarro -= dt;
            if (proximoCarro <= 0f)
            {
                proximoCarro = config.intervaloDosCarros;
                PorCarro(S - 28f, Random.value < 0.5f ? -1 : 0);
            }
        }

        for (int i = carros.Count - 1; i >= 0; i--)
        {
            Carro c = carros[i];
            // O que está na frente: outro carro, a bicicleta, o fim da fila do engarrafamento ou um obstáculo.
            float frente = FimDaFila(c.z, c.faixa) - 1f;
            foreach (Carro o in carros)
            {
                if (o != c && o.faixa == c.faixa && o.z > c.z) frente = Mathf.Min(frente, o.z - 4.2f);
            }
            // Bicicleta ou obstáculo na frente: se a outra faixa dos carros estiver livre, passa por ela.
            float bloqueio = float.MaxValue;
            if (Faixa == c.faixa && S > c.z) bloqueio = S - 1.5f;
            int k = TrechoEm(c.z + 3f);
            if (Rota.obstaculos.Length > k && Rota.obstaculos[k] != null && !obstaculoDerrubado[k] && Roteiro.trechos[k].FaixaCerta == c.faixa)
            {
                float zObs = inicios[k] + Roteiro.trechos[k].comprimento * 0.5f;
                if (zObs > c.z) bloqueio = Mathf.Min(bloqueio, zObs - 1.5f);
            }
            if (bloqueio - c.z < 14f && FaixaLivre(c, -1 - c.faixa))
            {
                c.faixa = -1 - c.faixa;
                bloqueio = float.MaxValue;
            }
            frente = Mathf.Min(frente, bloqueio);
            float folga = frente - c.z - config.distanciaEntreCarros * 0.5f;
            float alvo = Mathf.Clamp(folga * 1.6f, 0f, config.velocidadeDosCarros);
            c.v = Mathf.MoveTowards(c.v, alvo, (alvo < c.v ? 14f : 4f) * dt);
            c.z += c.v * dt;
            c.parado = c.v < 0.3f ? c.parado + dt : 0f;
            // Atrás da bicicleta na pista dos carros, onde tinha ciclofaixa: buzina uma vez.
            if (!c.buzinou && c.parado > 1.5f && Faixa == c.faixa && S - c.z < 10f && S > c.z && Roteiro.trechos[TrechoEm(S)].FaixaDaBicicleta != 2)
            {
                c.buzinou = true;
                Tocar(config.somDaBuzina, 0.35f);
            }
            bool sai = c.z > S + 90f || c.z < S - 40f || TipoEm(Mathf.Min(c.z + 3f, Comprimento - 0.01f), c.faixa) != TipoDeFaixa.Carros;
            if (sai)
            {
                c.t.gameObject.SetActive(false);
                carros.RemoveAt(i);
                continue;
            }
            PosicionarCarro(c);
        }
    }

    // As faixas dos carros são -1 e 0: a outra de -1 é 0, e a de 0 é -1.
    private bool FaixaLivre(Carro c, int faixa)
    {
        if (TipoEm(c.z, faixa) != TipoDeFaixa.Carros || TipoEm(Mathf.Min(c.z + 15f, Comprimento - 0.01f), faixa) != TipoDeFaixa.Carros)
        {
            return false;
        }
        if (Faixa == faixa && S > c.z - 6f && S < c.z + 16f)
        {
            return false;
        }
        if (FimDaFila(c.z, faixa) < c.z + 18f)
        {
            return false;
        }
        int k = TrechoEm(c.z + 8f);
        if (Rota.obstaculos.Length > k && Rota.obstaculos[k] != null && !obstaculoDerrubado[k] && Roteiro.trechos[k].FaixaCerta == faixa)
        {
            return false;
        }
        foreach (Carro o in carros)
        {
            if (o != c && o.faixa == faixa && Mathf.Abs(o.z - c.z) < 9f) return false;
        }
        return true;
    }

    private void PosicionarCarro(Carro c)
    {
        c.x = Mathf.MoveTowards(c.x, c.faixa * config.larguraDaFaixa, 2.4f * Time.deltaTime);
        float giro = (c.faixa * config.larguraDaFaixa - c.x) * 10f;
        c.t.SetPositionAndRotation(Rota.Mundo(c.x, c.z), Rota.transform.rotation * Quaternion.Euler(0f, Mathf.Clamp(giro, -15f, 15f), 0f));
    }

    // ------------------------------------------------------------------ pedestres (na calçada)

    private void PrepararPedestres()
    {
        pedestres.Clear();
        // Cada pedestre segue um caminho de calçada contínuo, feito pelo construtor (as calçadas do parque
        // ligadas de um trecho ao outro, e as calçadas da rua). No fim do caminho ele volta pro começo,
        // que fica fora da vista (antes da largada, depois da chegada ou numa pracinha).
        for (int i = 0; i < Rota.pedestres.Length && i < Rota.caminhoDoPedestre.Length; i++)
        {
            int c = Rota.caminhoDoPedestre[i];
            pedestres.Add(new Pedestre
            {
                t = Rota.pedestres[i],
                caminho = c,
                d = Rota.partidaDoPedestre[i],
                comprimento = Rota.ComprimentoDoCaminho(c),
                sentido = Rota.pedestreDeVolta[i] ? -1f : 1f,
                lado = (Rota.pedestreDeVolta[i] ? -1f : 1f) * afastamentoDosPedestres,
                animator = Rota.pedestres[i].GetComponentInChildren<Animator>(),
            });
        }
    }

    private void MoverPedestres()
    {
        foreach (Pedestre p in pedestres)
        {
            if (p.comprimento <= 0f) continue;
            p.d = Mathf.Repeat(p.d + p.sentido * velocidadeDosPedestres * Time.deltaTime, p.comprimento);
            Vector3 local = Rota.PontoNoCaminho(p.caminho, p.d, out Vector3 direcao);
            // Mão direita do caminho, e desvio da bicicleta: se ela está na calçada e vem perto,
            // o pedestre vai pra beira do lado oposto (ninguém atravessa ninguém).
            Vector3 lateral = new Vector3(direcao.z, 0f, -direcao.x);
            float alvo = p.sentido * afastamentoDosPedestres;
            Vector3 bici = Rota.transform.InverseTransformPoint(ciclista.position);
            Vector3 ate = bici - local;
            float aoLongo = Vector3.Dot(ate, direcao), deLado = Vector3.Dot(ate, lateral);
            if (Mathf.Abs(aoLongo) < 4.5f && Mathf.Abs(deLado) < 1.6f)
            {
                alvo = deLado > 0f ? -beiraDaCalcada : beiraDaCalcada;
            }
            p.lado = Mathf.MoveTowards(p.lado, alvo, 2.5f * Time.deltaTime);
            local += lateral * p.lado;
            Vector3 olhar = Rota.transform.TransformDirection(direcao * p.sentido);
            olhar.y = 0f;
            p.t.SetPositionAndRotation(Rota.transform.TransformPoint(local), olhar.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(olhar) : p.t.rotation);
            if (p.animator != null && p.animator.isActiveAndEnabled) p.animator.SetFloat(Velocidade, 0.5f);
        }
    }

    // ------------------------------------------------------------------ chegada

    private IEnumerator Chegar()
    {
        EtapaAtual = Etapa.Chegada;
        hud.Pedalando(false);
        hud.Marca(Vector3.zero, false);
        if (Rota.setaDaDica != null) Rota.setaDaDica.gameObject.SetActive(false);
        if (somDaSirene != null) somDaSirene.Stop();
        Dizer(Roteiro.recadoDeChegada);
        Tocar(config.somDeComemoracao);
        Tocar(config.somDaCampainha, 0.7f);
        Soltar(confete, ciclista.position + Vector3.up * 2f);
        if (animadorDoCiclista != null && animadorDoCiclista.HasState(0, Animator.StringToHash("Victory")))
        {
            animadorDoCiclista.CrossFade("Victory", 0.1f);
        }
        AtualizarCriterios();
        yield return new WaitForSeconds(config.pausaAntesDoFim);
        int erros = VezesNoLugarErrado + Batidas + (CapaceteDePrimeira ? 0 : 1);
        int pontos = Mathf.Max(40, 100 - 10 * erros);
        FinalizarNivel(new ResultadoDoNivel { concluiu = true, pontuacao = pontos, erros = erros, tempoGasto = TempoGasto, tempoRestante = TempoRestante });
    }

    // ------------------------------------------------------------------ cartões, estrelas, recados

    private IEnumerator AbrirCartaoSeNovo(string id)
    {
        if (GameProgress.VerbeteDesbloqueado(id))
        {
            yield break;
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

    public bool[] Criterios()
    {
        bool lugar = VezesNoLugarErrado == 0;
        bool batida = Batidas == 0;
        return TemTempo ? new[] { lugar, batida, NoTempo } : new[] { CapaceteDePrimeira, lugar, batida };
    }

    // 3 = os três critérios; 2 = faltou um; 1 = concluiu.
    protected override int CalcularEstrelas(ResultadoDoNivel resultado)
    {
        if (!resultado.concluiu)
        {
            return 0;
        }
        int faltam = 0;
        foreach (bool g in Criterios())
        {
            if (!g) faltam++;
        }
        return faltam == 0 ? 3 : (faltam == 1 ? 2 : 1);
    }

    private void AtualizarCriterios()
    {
        hud.Criterios(Criterios(), TemTempo ? config.rotulosComTempo : config.rotulosDosCriterios);
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

    private void Tocar(AudioClip clipe, float volume = 1f)
    {
        if (clipe != null && som != null)
        {
            som.PlayOneShot(clipe, volume);
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
