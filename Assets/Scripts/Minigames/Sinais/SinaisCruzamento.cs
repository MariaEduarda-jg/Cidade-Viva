using UnityEngine;

// Um lugar do percurso do Motorista Mirim onde há semáforo. Quem passa aqui está sempre em movimento:
//   Pedestres: chegam pela calçada, esperam na beira da faixa, atravessam no verde deles e vão embora
//     pela outra calçada; depois outra leva chega (não ficam indo e voltando na faixa).
//   Veículos: vêm de longe, param na linha deles (um atrás do outro), passam quando o sinal do carrinho
//     está vermelho, um de cada vez, e seguem até sumir.
//   Veículos com "surgeNoAmarelo" (bonde, ambulância): não ficam esperando à vista; saem de longe quando
//     o sinal do carrinho fica amarelo e chegam ao cruzamento depois de ele ficar vermelho.
// A rua do carrinho corre no Z local.
public class SinaisCruzamento : MonoBehaviour
{
    public SinalDeTransito sinal;
    public SinalDeTransito sinalDePedestre;
    [Tooltip("Semáforo de quem vem na mão contrária (obra): abre quando o do carrinho está vermelho.")]
    public SinalDeTransito sinalDaOutraMao;
    [Tooltip("Distância na rota do carrinho até a linha onde ele tem de parar.")]
    public float sDaLinha;
    [Tooltip("Frase curta que aparece quando o carrinho vai chegando neste lugar.")]
    public string recadoDoLugar;

    [Header("Pedestres (posição inicial: x = lado da calçada, z = onde esperam na faixa)")]
    public Transform[] pedestres = new Transform[0];
    public float velocidadeNaCalcada = 1.8f;
    public float velocidadeNaFaixa = 2.6f;
    [Tooltip("De onde chegam pela calçada e para onde vão depois de atravessar (z local).")]
    public float zDeChegada = 22f;
    public float zDeSaida = -40f;

    [Header("Veículos")]
    public Transform[] carros = new Transform[0];
    [Tooltip("Para onde cada veículo anda, em coordenadas locais (mesma ordem de \"carros\").")]
    public Vector3[] direcaoDosCarros = new Vector3[0];
    [Tooltip("Do ponto de espera até ter saído do caminho do carrinho.")]
    public float esperaDosCarros = 12f;
    [Tooltip("Quanto o veículo anda antes do ponto de espera (de onde surge) e depois de sair do caminho (onde some).")]
    public float fimDosCarros = 30f;
    public float velocidadeDosCarros = 10f;
    [Tooltip("Tempo entre um veículo sair e o seguinte sair: é o que mantém a distância entre eles.")]
    public float intervaloDosCarros = 1.6f;
    public bool surgeNoAmarelo;

    // O sinal do carrinho vai abrir: ninguém novo começa a atravessar.
    public bool Fechando { get; set; }

    // Ninguém no caminho do carrinho.
    public bool Livre
    {
        get
        {
            for (int i = 0; i < fasePedestre.Length; i++)
            {
                if (fasePedestre[i] == FaseDoPedestre.Atravessando) return false;
            }
            for (int i = 0; i < carroAndando.Length; i++)
            {
                if (carroAndando[i] && avanco[i] < esperaDosCarros) return false;
            }
            return !bondeACaminho;
        }
    }

    private enum FaseDoPedestre { Chegando, Esperando, Atravessando, Saindo }

    private FaseDoPedestre[] fasePedestre = new FaseDoPedestre[0];
    private Vector3[] posicaoPedestre;
    private float[] ladoPedestre;
    private float[] zDaFaixa;
    private Vector3[] escalaPedestre;
    private Animator[] animadores;
    private Vector3[] desvioPedestre;
    [Tooltip("Quanto cada pedestre anda à direita do caminho (metros), pra não atravessar quem vem do outro lado.")]
    public float maoDireita = 0.4f;

    private float[] avanco;
    private bool[] carroAndando = new bool[0];
    private Vector3[] inicioDosCarros;
    private Vector3[] escalaCarro;
    private float tempoDesdeASaida;
    private bool bondeACaminho;
    private bool emUso = true;
    private static readonly int Velocidade = Animator.StringToHash("Velocidade");

    private void Awake()
    {
        int n = pedestres.Length;
        fasePedestre = new FaseDoPedestre[n];
        posicaoPedestre = new Vector3[n];
        ladoPedestre = new float[n];
        zDaFaixa = new float[n];
        escalaPedestre = new Vector3[n];
        animadores = new Animator[n];
        desvioPedestre = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 p = pedestres[i].localPosition;
            ladoPedestre[i] = p.x;
            zDaFaixa[i] = p.z;
            escalaPedestre[i] = pedestres[i].localScale;
            animadores[i] = pedestres[i].GetComponentInChildren<Animator>();
            // Já começam a caminho, cada um num ponto da calçada.
            posicaoPedestre[i] = new Vector3(p.x, p.y, p.z + 5f + i * 7f);
        }

        int m = carros.Length;
        avanco = new float[m];
        carroAndando = new bool[m];
        inicioDosCarros = new Vector3[m];
        escalaCarro = new Vector3[m];
        for (int i = 0; i < m; i++)
        {
            inicioDosCarros[i] = carros[i].localPosition;
            escalaCarro[i] = carros[i].localScale;
            avanco[i] = surgeNoAmarelo ? -fimDosCarros : -fimDosCarros * (0.15f + 0.4f * i);
        }
        tempoDesdeASaida = intervaloDosCarros;
    }

    // Lugar que o nível não usa: fica como cenário, sem ninguém passando.
    public void Usar(bool usado)
    {
        emUso = usado;
        if (usado)
        {
            return;
        }
        sinal.DesligarCiclo();
        sinal.Mudar(CorDoSinal.Verde);
        foreach (Transform p in pedestres) p.gameObject.SetActive(false);
        foreach (Transform c in carros) c.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!emUso)
        {
            return;
        }
        bool vermelho = sinal.Cor == CorDoSinal.Vermelho;
        if (!vermelho)
        {
            Fechando = false;
        }
        AtualizarPedestres(vermelho);
        if (surgeNoAmarelo)
        {
            AtualizarQuemSurge();
        }
        else
        {
            AtualizarVeiculos(vermelho);
        }
        if (sinalDaOutraMao != null)
        {
            sinalDaOutraMao.Mudar(vermelho && !Fechando && sinal.TempoNaCor > 0.6f ? CorDoSinal.Verde : CorDoSinal.Vermelho);
        }
    }

    private void AtualizarPedestres(bool vermelho)
    {
        if (sinalDePedestre == null)
        {
            return;
        }
        // O semáforo de pedestre abre com o vermelho dos carros e fecha antes de eles voltarem.
        float falta = sinal.TempoQueFalta;
        sinalDePedestre.Mudar(vermelho && falta > 1f && !Fechando ? CorDoSinal.Verde : CorDoSinal.Vermelho);
        for (int i = 0; i < pedestres.Length; i++)
        {
            float lado = ladoPedestre[i], y = posicaoPedestre[i].y;
            Vector3 alvo;
            float velocidade = velocidadeNaCalcada;
            switch (fasePedestre[i])
            {
                case FaseDoPedestre.Chegando:
                    alvo = new Vector3(lado, y, zDaFaixa[i]);
                    break;
                case FaseDoPedestre.Esperando:
                    alvo = posicaoPedestre[i];
                    float travessia = 2f * Mathf.Abs(lado) / velocidadeNaFaixa;
                    if (sinalDePedestre.Cor == CorDoSinal.Verde && falta > travessia + 0.4f)
                    {
                        fasePedestre[i] = FaseDoPedestre.Atravessando;
                        alvo = new Vector3(-lado, y, zDaFaixa[i]);
                        velocidade = velocidadeNaFaixa;
                    }
                    break;
                case FaseDoPedestre.Atravessando:
                    alvo = new Vector3(-lado, y, zDaFaixa[i]);
                    velocidade = velocidadeNaFaixa;
                    break;
                default:
                    alvo = new Vector3(-lado, y, zDeSaida);
                    break;
            }

            Vector3 caminho = alvo - posicaoPedestre[i];
            bool anda = caminho.sqrMagnitude > 0.0001f;
            if (anda)
            {
                posicaoPedestre[i] = Vector3.MoveTowards(posicaoPedestre[i], alvo, velocidade * Time.deltaTime);
                pedestres[i].localRotation = Quaternion.LookRotation(caminho);
            }
            else if (fasePedestre[i] == FaseDoPedestre.Chegando)
            {
                fasePedestre[i] = FaseDoPedestre.Esperando;
                // Espera virado pra faixa.
                pedestres[i].localRotation = Quaternion.LookRotation(new Vector3(-lado, 0f, 0f));
            }
            else if (fasePedestre[i] == FaseDoPedestre.Atravessando)
            {
                fasePedestre[i] = FaseDoPedestre.Saindo;
            }
            else if (fasePedestre[i] == FaseDoPedestre.Saindo)
            {
                // Foi embora: outra pessoa chega pela calçada, do lado de onde esta veio.
                fasePedestre[i] = FaseDoPedestre.Chegando;
                posicaoPedestre[i] = new Vector3(lado, y, zDeChegada);
            }

            if (anda)
            {
                Vector3 direita = new Vector3(caminho.z, 0f, -caminho.x).normalized * maoDireita;
                desvioPedestre[i] = Vector3.MoveTowards(desvioPedestre[i], direita, 1.5f * Time.deltaTime);
            }
            pedestres[i].localPosition = posicaoPedestre[i] + desvioPedestre[i];
            // Surge e some aos poucos nas pontas da calçada.
            float ponta = Mathf.Min(Mathf.Abs(posicaoPedestre[i].z - zDeChegada), Mathf.Abs(posicaoPedestre[i].z - zDeSaida));
            pedestres[i].localScale = escalaPedestre[i] * Mathf.Clamp01(ponta / 1.2f);
            if (animadores[i] != null)
            {
                animadores[i].SetFloat(Velocidade, anda ? 0.5f : 0f);
            }
        }
    }

    // Veículos em fluxo: vêm, param na linha deles, passam no vermelho do carrinho, um de cada vez.
    private void AtualizarVeiculos(bool vermelho)
    {
        tempoDesdeASaida += Time.deltaTime;
        float falta = sinal.TempoQueFalta;
        float cruzar = esperaDosCarros / velocidadeDosCarros;
        for (int i = 0; i < carros.Length; i++)
        {
            if (!carroAndando[i] && avanco[i] >= 0f && vermelho && !Fechando && sinal.TempoNaCor > 0.6f
                && falta > cruzar + 0.6f && tempoDesdeASaida >= intervaloDosCarros)
            {
                carroAndando[i] = true;
                tempoDesdeASaida = 0f;
            }
            if (carroAndando[i])
            {
                avanco[i] += velocidadeDosCarros * Time.deltaTime;
                if (avanco[i] >= esperaDosCarros + fimDosCarros)
                {
                    avanco[i] = -fimDosCarros;
                    carroAndando[i] = false;
                }
            }
            else if (avanco[i] < 0f)
            {
                avanco[i] = Mathf.Min(0f, avanco[i] + velocidadeDosCarros * Time.deltaTime);
            }
            ColocarVeiculo(i);
        }
    }

    // Bonde e ambulância: passam uma vez a cada vez que o sinal do carrinho fecha.
    private void AtualizarQuemSurge()
    {
        if (!bondeACaminho && sinal.Cor == CorDoSinal.Amarelo && avanco.Length > 0 && avanco[0] <= -fimDosCarros)
        {
            bondeACaminho = true;
        }
        for (int i = 0; i < carros.Length; i++)
        {
            if (!bondeACaminho)
            {
                carros[i].gameObject.SetActive(false);
                continue;
            }
            carros[i].gameObject.SetActive(true);
            avanco[i] += velocidadeDosCarros * Time.deltaTime;
            if (avanco[i] >= esperaDosCarros + fimDosCarros)
            {
                avanco[i] = -fimDosCarros;
                bondeACaminho = false;
                carros[i].gameObject.SetActive(false);
                continue;
            }
            ColocarVeiculo(i);
        }
    }

    private void ColocarVeiculo(int i)
    {
        carros[i].localPosition = inicioDosCarros[i] + direcaoDosCarros[i] * avanco[i];
        // Surge e some aos poucos nas pontas do caminho.
        float ponta = Mathf.Min(avanco[i] + fimDosCarros, esperaDosCarros + fimDosCarros - avanco[i]);
        carros[i].localScale = escalaCarro[i] * Mathf.Clamp01(ponta / 3f);
    }
}
