using System.Collections.Generic;
using UnityEngine;

// Modo "Sinaleiro" da Central de Sinais: a criança vê um cruzamento de cima e acende os semáforos.
// Via A (azul) corre em X, da esquerda pra direita; via B (laranja) corre em Z, de baixo pra cima;
// a faixa de pedestres atravessa a via A, antes do cruzamento. Carros e pedestres obedecem a luz:
//   verde     "podem continuar andando"                 -> passam.
//   amarelo   "o motorista deve diminuir a velocidade"  -> os carros passam devagar.
//   vermelho  "devem parar"                             -> param antes da linha e fazem fila.
// O semáforo de pedestre só tem verde e vermelho ("luzes vermelha e verde").
// Se carros das duas vias entram juntos no cruzamento, ou um carro e um pedestre na faixa, há uma
// "quase batida" cômica (todo mundo freia, um carro dá um pulinho pra trás) e conta um erro.
// O nível acaba quando todos os carros e pedestres passaram.
//
// O que cada campo do nível faz aqui:
//   quantidadeDeItens        quantos carros precisam passar (somando as vias).
//   extras "pedestres"       quantos pedestres precisam atravessar.
//   extras "intervalo"       segundos entre um carro e o próximo, em cada via.
//   regrasExtras "duas_vias" (a via B também recebe carros), "pedestres" (a faixa funciona) e
//                "ambulancia" (uma ambulância de sirene aparece na via B e não pode ficar esperando).
public class SinaisSinaleiro : MonoBehaviour
{
    public const string RegraDuasVias = "duas_vias";
    public const string RegraPedestres = "pedestres";
    public const string RegraAmbulancia = "ambulancia";

    [Header("Semáforos")]
    public SinalDeTransito[] sinaisDasVias = new SinalDeTransito[2];
    public SinalDeTransito sinalDePedestre;

    [Header("Cena")]
    public Transform mundo;
    [Tooltip("O que só aparece quando a via B e a faixa de pedestres estão em uso.")]
    public GameObject enfeitesDaViaB;
    public GameObject enfeitesDaFaixa;
    public Transform poseDaCamera;

    [Header("Modelos inativos")]
    public GameObject[] modelosDeCarro = new GameObject[0];
    public GameObject modeloDaAmbulancia;
    public GameObject[] modelosDePedestre = new GameObject[0];

    [Header("Medidas (a partir do meio do cruzamento)")]
    [Tooltip("Onde os carros nascem e onde somem, ao longo da via.")]
    public float inicioDaVia = -42f;
    public float fimDaVia = 42f;
    [Tooltip("Onde a frente do carro para no vermelho: via A (antes da faixa) e via B.")]
    public float[] linhaDaVia = { -10.5f, -5.5f };
    [Tooltip("Pista em que os carros andam: z na via A, x na via B.")]
    public float[] pistaDaVia = { -1.75f, 1.75f };
    public float meioCarro = 2f;
    [Tooltip("Meia largura do cruzamento.")]
    public float meioCruzamento = 3.5f;
    [Tooltip("A faixa de pedestres atravessa a via A entre estes dois x.")]
    public Vector2 faixa = new Vector2(-8.8f, -5.6f);
    [Tooltip("Por onde os pedestres andam: x da faixa, z de onde nascem, de onde esperam e onde somem.")]
    public float xDosPedestres = -7.2f;
    public float inicioDosPedestres = -12f;
    public float esperaDosPedestres = -4.6f;
    public float fimDosPedestres = 12f;

    public class Carro
    {
        public Transform t;
        public int via;
        public float s;
        public float v;
        public bool passouDaLinha;
        public bool comprometido;
        public bool ambulancia;
        public float esperando;
        public bool buzinou;
        public bool cobrouAEspera;
        public float congelado;
        public float recuo;
        public float imune;
    }

    public class Pedestre
    {
        public Transform t;
        public Animator animator;
        public float z;
        public bool atravessando;
        public float xDaFila;
    }

    public int CarrosQuePassaram { get; private set; }
    public int PedestresQuePassaram { get; private set; }
    public int TotalDeCarros { get; private set; }
    public int TotalDePedestres { get; private set; }
    public int QuaseBatidas { get; private set; }
    public bool DuasVias { get; private set; }
    public bool ComPedestres { get; private set; }
    public bool ComAmbulancia { get; private set; }
    public bool AmbulanciaNaVia { get { return carros.Exists(c => c.ambulancia); } }
    public bool AmbulanciaPassou { get; private set; }
    public IList<Carro> Carros { get { return carros; } }
    public IList<Pedestre> Pedestres { get { return pedestres; } }

    private SinaisManager manager;
    private SinaisConfig config;
    private readonly List<Carro> carros = new List<Carro>();
    private readonly List<Pedestre> pedestres = new List<Pedestre>();
    private readonly int[] carrosPorNascer = new int[2];
    private readonly float[] proximoCarro = new float[2];
    private int pedestresPorNascer;
    private float proximoPedestre;
    private float intervalo;
    private float relogio;
    private bool ambulanciaPorNascer;
    private bool rodando;
    private System.Random sorteio;
    private static readonly int Velocidade = Animator.StringToHash("Velocidade");

    public void Comecar(SinaisManager manager)
    {
        this.manager = manager;
        config = manager.config;
        NivelConfig nivel = manager.Nivel;
        DuasVias = nivel.TemRegra(RegraDuasVias);
        ComPedestres = nivel.TemRegra(RegraPedestres);
        ComAmbulancia = nivel.TemRegra(RegraAmbulancia);
        intervalo = nivel.Extra("intervalo", 4f);
        // Sempre a mesma sequência num mesmo nível: o nível 5 não depende de sorte.
        sorteio = new System.Random(100 + manager.NivelAtual);

        TotalDeCarros = Mathf.Max(1, nivel.quantidadeDeItens);
        carrosPorNascer[1] = DuasVias ? TotalDeCarros / 2 : 0;
        carrosPorNascer[0] = TotalDeCarros - carrosPorNascer[1];
        TotalDePedestres = ComPedestres ? Mathf.RoundToInt(nivel.Extra("pedestres", 4f)) : 0;
        pedestresPorNascer = TotalDePedestres;
        ambulanciaPorNascer = ComAmbulancia;
        if (ComAmbulancia)
        {
            TotalDeCarros++;
        }
        proximoCarro[0] = 0.5f;
        proximoCarro[1] = 2.5f;
        proximoPedestre = 3f;

        enfeitesDaViaB.SetActive(DuasVias);
        enfeitesDaFaixa.SetActive(ComPedestres);
        foreach (SinalDeTransito sinal in sinaisDasVias)
        {
            sinal.DesligarCiclo();
            sinal.Mudar(CorDoSinal.Vermelho);
            sinal.Mudou += AoMudarOSinal;
        }
        sinalDePedestre.Mudar(CorDoSinal.Vermelho);
        sinalDePedestre.Mudou += AoMudarOSinal;

        manager.cameraDoJogo.transform.SetPositionAndRotation(poseDaCamera.position, poseDaCamera.rotation);
        manager.hud.GruposDoPainel(true, DuasVias, ComPedestres);
        manager.hud.Recado(config.recadoDoSinaleiro);
        manager.LigarRelogio();
        rodando = true;
        AtualizarTela();
    }

    private void AoMudarOSinal(SinalDeTransito sinal)
    {
        manager.Tocar(config.somDoTic);
        AtualizarTela();
    }

    // ------------------------------------------------------------------ painel (botões e teclado)

    // via: 0 = A (azul), 1 = B (laranja).
    public void MudarSinal(int via, CorDoSinal cor)
    {
        if (!rodando || via < 0 || via >= sinaisDasVias.Length || (via == 1 && !DuasVias))
        {
            return;
        }
        sinaisDasVias[via].Mudar(cor);
    }

    public void MudarSinalDePedestre(bool verde)
    {
        if (!rodando || !ComPedestres)
        {
            return;
        }
        sinalDePedestre.Mudar(verde ? CorDoSinal.Verde : CorDoSinal.Vermelho);
    }

    // ------------------------------------------------------------------ o que há no cruzamento (pra tela e pros testes)

    // Há carro desta via dentro do cruzamento (ou chegando nele, já depois da linha)?
    public bool ViaOcupada(int via)
    {
        foreach (Carro c in carros)
        {
            if (c.via == via && c.s + meioCarro > linhaDaVia[via] + 0.2f && c.s - meioCarro < meioCruzamento)
            {
                return true;
            }
        }
        return false;
    }

    public bool FaixaComPedestre
    {
        get
        {
            foreach (Pedestre p in pedestres)
            {
                if (p.atravessando && p.z < meioCruzamento + 0.3f)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public int CarrosEsperando(int via)
    {
        int n = 0;
        foreach (Carro c in carros)
        {
            if (c.via == via && !c.passouDaLinha)
            {
                n++;
            }
        }
        return n;
    }

    public int PedestresEsperando
    {
        get
        {
            int n = 0;
            foreach (Pedestre p in pedestres)
            {
                if (!p.atravessando)
                {
                    n++;
                }
            }
            return n;
        }
    }

    // ------------------------------------------------------------------ todo quadro

    private void Update()
    {
        if (!rodando || !manager.Rodando || manager.Pausado)
        {
            return;
        }

        Teclado();
        relogio += Time.deltaTime;
        Nascer();
        MoverCarros();
        MoverPedestres();
        ProcurarQuaseBatidas();

        if (CarrosQuePassaram >= TotalDeCarros && PedestresQuePassaram >= TotalDePedestres)
        {
            rodando = false;
            manager.Concluir(config.recadoDeFim, mundo.position + Vector3.up * 3f);
        }
        manager.hud.Contador(string.Format(config.textoDoContador, CarrosQuePassaram + PedestresQuePassaram, TotalDeCarros + TotalDePedestres));
    }

    private void Teclado()
    {
        if (global::Teclado.Apertou(KeyCode.Q)) MudarSinal(0, CorDoSinal.Verde);
        if (global::Teclado.Apertou(KeyCode.W)) MudarSinal(0, CorDoSinal.Amarelo);
        if (global::Teclado.Apertou(KeyCode.E)) MudarSinal(0, CorDoSinal.Vermelho);
        if (global::Teclado.Apertou(KeyCode.A)) MudarSinal(1, CorDoSinal.Verde);
        if (global::Teclado.Apertou(KeyCode.S)) MudarSinal(1, CorDoSinal.Amarelo);
        if (global::Teclado.Apertou(KeyCode.D)) MudarSinal(1, CorDoSinal.Vermelho);
        if (global::Teclado.Apertou(KeyCode.Z)) MudarSinalDePedestre(true);
        if (global::Teclado.Apertou(KeyCode.X)) MudarSinalDePedestre(false);
    }

    private void Nascer()
    {
        for (int via = 0; via < 2; via++)
        {
            bool ambulancia = via == 1 && ambulanciaPorNascer && relogio >= config.chegadaDaAmbulancia;
            if ((carrosPorNascer[via] <= 0 && !ambulancia) || (relogio < proximoCarro[via] && !ambulancia) || !TemEspacoPraNascer(via))
            {
                continue;
            }
            GameObject modelo = ambulancia ? modeloDaAmbulancia : modelosDeCarro[sorteio.Next(modelosDeCarro.Length)];
            var carro = new Carro { via = via, s = inicioDaVia, v = config.velocidadeDosCarros, ambulancia = ambulancia };
            carro.t = Instantiate(modelo, mundo).transform;
            carro.t.gameObject.SetActive(true);
            carro.t.localRotation = Quaternion.Euler(0f, via == 0 ? 90f : 0f, 0f);
            Posicionar(carro, 0f);
            carros.Add(carro);
            if (ambulancia)
            {
                ambulanciaPorNascer = false;
                manager.Tocar(config.somDaSirene, config.volumeDaSirene);
                manager.hud.Recado(config.recadoDaAmbulancia);
            }
            else
            {
                carrosPorNascer[via]--;
            }
            proximoCarro[via] = relogio + intervalo * (0.8f + (float)sorteio.NextDouble() * 0.5f);
        }

        if (pedestresPorNascer > 0 && relogio >= proximoPedestre)
        {
            var pedestre = new Pedestre { z = inicioDosPedestres, xDaFila = (PedestresEsperando % 3 - 1) * 0.9f };
            pedestre.t = Instantiate(modelosDePedestre[sorteio.Next(modelosDePedestre.Length)], mundo).transform;
            pedestre.t.gameObject.SetActive(true);
            pedestre.t.localRotation = Quaternion.identity;
            pedestre.animator = pedestre.t.GetComponentInChildren<Animator>();
            pedestres.Add(pedestre);
            pedestresPorNascer--;
            proximoPedestre = relogio + intervalo * (1.1f + (float)sorteio.NextDouble() * 0.6f);
        }
    }

    private bool TemEspacoPraNascer(int via)
    {
        foreach (Carro c in carros)
        {
            if (c.via == via && c.s < inicioDaVia + meioCarro * 2f + 3f)
            {
                return false;
            }
        }
        return true;
    }

    private void Posicionar(Carro c, float tremor)
    {
        c.t.localPosition = c.via == 0 ? new Vector3(c.s, 0.01f, pistaDaVia[0] + tremor) : new Vector3(pistaDaVia[1] + tremor, 0.01f, c.s);
    }

    private void MoverCarros()
    {
        float maxima = config.velocidadeDosCarros;
        for (int i = carros.Count - 1; i >= 0; i--)
        {
            Carro c = carros[i];
            c.imune = Mathf.Max(0f, c.imune - Time.deltaTime);
            if (c.congelado > 0f)
            {
                // Quase batida: treme parado e, se for o caso, dá um pulinho pra trás.
                c.congelado -= Time.deltaTime;
                float volta = Mathf.Min(c.recuo, 9f * Time.deltaTime);
                c.recuo -= volta;
                c.s -= volta;
                c.v = 0f;
                Posicionar(c, Mathf.Sin(Time.time * 45f) * 0.07f);
                continue;
            }

            SinalDeTransito sinal = sinaisDasVias[c.via];
            float alvo = maxima;
            float ateALinha = linhaDaVia[c.via] - (c.s + meioCarro);
            if (!c.passouDaLinha)
            {
                if (ateALinha <= 0f)
                {
                    c.passouDaLinha = true;
                    if (sinal.Cor == CorDoSinal.Verde)
                    {
                        manager.Acerto(SinaisManager.IdDoSemaforo, null, c.t.position + Vector3.up * 2f);
                    }
                    if (c.ambulancia)
                    {
                        AmbulanciaPassou = true;
                    }
                }
                else
                {
                    // Perto demais pra frear quando o sinal fechou: já estava passando.
                    if (sinal.Cor != CorDoSinal.Vermelho && ateALinha < 2f)
                    {
                        c.comprometido = true;
                    }
                    if (sinal.Cor == CorDoSinal.Vermelho && !c.comprometido)
                    {
                        alvo = Mathf.Min(alvo, Mathf.Sqrt(2f * 12f * Mathf.Max(0f, ateALinha - 0.15f)));
                    }
                    else if (sinal.Cor == CorDoSinal.Amarelo)
                    {
                        alvo = maxima * config.fracaoNoAmarelo;
                    }
                }
            }

            // Não encosta no carro da frente.
            Carro daFrente = DaFrente(c);
            if (daFrente != null)
            {
                float folga = daFrente.s - c.s - meioCarro * 2f - 1.2f;
                alvo = Mathf.Min(alvo, Mathf.Sqrt(2f * 12f * Mathf.Max(0f, folga)));
            }

            c.v = Mathf.MoveTowards(c.v, alvo, (alvo < c.v ? 16f : 6f) * Time.deltaTime);
            c.s += c.v * Time.deltaTime;
            Posicionar(c, 0f);

            // Quem espera muito buzina de leve; a ambulância não pode ficar esperando.
            if (c.v < 0.2f && !c.passouDaLinha)
            {
                c.esperando += Time.deltaTime;
                if (!c.buzinou && !c.ambulancia && c.esperando >= config.esperaPraBuzinar && ateALinha < 1f)
                {
                    c.buzinou = true;
                    manager.Tocar(config.somDaBuzina, config.volumeDaBuzina);
                }
                if (c.ambulancia && !c.cobrouAEspera && c.esperando >= config.esperaDaAmbulancia)
                {
                    c.cobrouAEspera = true;
                    manager.Erro(config.balaoDaAmbulancia, c.t.position + Vector3.up * 2f);
                }
            }

            if (c.s >= fimDaVia)
            {
                Destroy(c.t.gameObject);
                carros.RemoveAt(i);
                CarrosQuePassaram++;
            }
        }
    }

    private Carro DaFrente(Carro c)
    {
        Carro melhor = null;
        foreach (Carro outro in carros)
        {
            if (outro != c && outro.via == c.via && outro.s > c.s && (melhor == null || outro.s < melhor.s))
            {
                melhor = outro;
            }
        }
        return melhor;
    }

    private void MoverPedestres()
    {
        for (int i = pedestres.Count - 1; i >= 0; i--)
        {
            Pedestre p = pedestres[i];
            bool anda = true;
            // Na fila, quem chega para atrás de quem já está esperando na mesma coluna (não fica um dentro do outro).
            float paraEm = esperaDosPedestres;
            if (!p.atravessando)
            {
                foreach (Pedestre outro in pedestres)
                {
                    if (outro != p && !outro.atravessando && Mathf.Abs(outro.xDaFila - p.xDaFila) < 0.1f && outro.z > p.z)
                    {
                        paraEm = Mathf.Min(paraEm, outro.z - 0.9f);
                    }
                }
            }
            if (!p.atravessando && p.z >= paraEm && paraEm < esperaDosPedestres)
            {
                anda = false;
                p.z = paraEm;
            }
            else if (!p.atravessando)
            {
                if (p.z >= esperaDosPedestres)
                {
                    // Na beira da calçada: só começa a atravessar com o verde do semáforo de pedestre.
                    if (sinalDePedestre.Cor == CorDoSinal.Verde)
                    {
                        p.atravessando = true;
                        manager.Acerto(SinaisManager.IdDoSemaforoDePedestre, null, p.t.position + Vector3.up * 2f);
                    }
                    else
                    {
                        anda = false;
                    }
                }
            }
            if (anda)
            {
                p.z += config.velocidadeDosPedestres * Time.deltaTime;
            }
            // Atravessam lado a lado, cada um na sua coluna da fila.
            float x = xDosPedestres + p.xDaFila;
            p.t.localPosition = new Vector3(x, 0.08f, p.z);
            if (p.animator != null)
            {
                p.animator.SetFloat(Velocidade, anda ? 0.5f : 0f);
            }
            if (p.z >= fimDosPedestres)
            {
                Destroy(p.t.gameObject);
                pedestres.RemoveAt(i);
                PedestresQuePassaram++;
            }
        }
    }

    // ------------------------------------------------------------------ quase batidas

    private void ProcurarQuaseBatidas()
    {
        // Carro da via A e carro da via B dentro do cruzamento ao mesmo tempo.
        foreach (Carro a in carros)
        {
            if (a.via != 0 || a.imune > 0f || !NoCruzamento(a))
            {
                continue;
            }
            foreach (Carro b in carros)
            {
                if (b.via == 1 && b.imune <= 0f && NoCruzamento(b))
                {
                    QuaseBatida(a, b, string.Format(config.balaoDeDuasVias, config.trechoDoSemaforo));
                    return;
                }
            }
        }

        // Carro da via A em cima da faixa com pedestre atravessando.
        foreach (Carro a in carros)
        {
            if (a.via != 0 || a.imune > 0f || a.s + meioCarro < faixa.x || a.s - meioCarro > faixa.y)
            {
                continue;
            }
            foreach (Pedestre p in pedestres)
            {
                if (p.atravessando && Mathf.Abs(p.z) < meioCruzamento)
                {
                    QuaseBatida(a, null, string.Format(config.balaoDePedestre, config.trechoDoSemaforoDePedestre));
                    return;
                }
            }
        }
    }

    private bool NoCruzamento(Carro c)
    {
        return c.s + meioCarro > -meioCruzamento && c.s - meioCarro < meioCruzamento;
    }

    // Todo mundo freia a tempo: os carros tremem parados e um deles dá um pulinho pra trás. Ninguém se machuca.
    private void QuaseBatida(Carro a, Carro b, string balao)
    {
        QuaseBatidas++;
        Carro volta = b != null ? b : a;
        Vector3 onde = volta.t.position + Vector3.up * 2f;
        a.congelado = 1.1f;
        a.imune = 3.5f;
        if (b != null)
        {
            b.congelado = 1.1f;
            b.imune = 3.5f;
        }
        // Quem volta vai pra trás da linha dele, espera o outro sair e passa a obedecer o sinal de novo.
        float atrasDaLinha = linhaDaVia[volta.via] - meioCarro - 0.4f;
        volta.recuo = Mathf.Max(0f, volta.s - atrasDaLinha);
        volta.congelado = 2.6f;
        volta.passouDaLinha = false;
        volta.comprometido = false;
        manager.Tocar(config.somDaBuzina, config.volumeDaBuzina);
        manager.Erro(balao, onde);
    }

    private void AtualizarTela()
    {
        manager.hud.LuzDoPainel(0, sinaisDasVias[0].Cor);
        manager.hud.LuzDoPainel(1, sinaisDasVias[1].Cor);
        manager.hud.LuzDoPainel(2, sinalDePedestre.Cor);
    }
}
