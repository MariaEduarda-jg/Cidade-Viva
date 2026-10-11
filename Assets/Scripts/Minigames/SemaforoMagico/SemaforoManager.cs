using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CorDoSemaforo
{
    Verde,
    Amarelo,
    Vermelho
}

// "Semáforo Mágico": um carrinho anda pela rua (a rua é que se move) e chega a um semáforo por vez.
// Quando o semáforo acende, o jogador tem o tempo do carro chegar à faixa de parada pra reagir:
// verde = seguir (↑/W ou botão), amarelo = não apertar nada (o carro vai devagar sozinho),
// vermelho = parar (espaço ou botão; o carro freia até a faixa, espera abrir e segue).
// Tecla errada ou sem reação tira 1 vida; no vermelho o carro sempre para (nunca fura o sinal).
// A cada semáforo o carro fica mais rápido e o tempo de reação diminui.
public class SemaforoManager : MinigameBase
{
    private enum Estado
    {
        Dirigindo,
        Decidindo,
        Freando,
        Parado,
        Passando
    }

    public SemaforoConfig config;

    [Header("Cena")]
    public RectTransform carro;
    public SemaforoDaRua semaforo;
    public RectTransform[] faixasDaRua;
    public float espacamentoDasFaixas = 240f;
    public float larguraDaTela = 1920f;

    [Header("Interface")]
    public RunnerEscudos vidas;
    public Text textoFeedback;
    public Text textoPontos;
    public Text textoCombo;
    public Text textoSemaforos;
    public RectTransform barraDeTempo;

    // Os 3 primeiros semáforos mostram cada cor em ordem, com dica (tutorial).
    private static readonly CorDoSemaforo[] Tutorial = { CorDoSemaforo.Verde, CorDoSemaforo.Amarelo, CorDoSemaforo.Vermelho };

    private Estado estado;
    private CorDoSemaforo cor;
    private bool respondeu;
    private float velocidadeBase;
    private float velocidade;
    private float janela;
    private float distanciaAoAcender;
    private float desaceleracao;
    private float tempoNoEstado;
    private float deslocamentoDasFaixas;
    private float larguraDaBarra;
    private int semaforosPassados;
    private int acertos;
    private int combo;
    private int comboMaximo;

    public bool Jogando { get; private set; }
    public bool AguardandoReacao => Jogando && estado == Estado.Decidindo && !respondeu;
    public CorDoSemaforo Cor => cor;
    public int Pontuacao => acertos * config.pontosPorAcerto + comboMaximo * config.pontosPorComboMaximo;

    private float FrenteDoCarro => carro.anchoredPosition.x + carro.rect.width / 2f;
    // Distância da frente do carro até a faixa de parada (o pivô do semáforo fica na faixa).
    private float DistanciaAteAFaixa => semaforo.GetComponent<RectTransform>().anchoredPosition.x - FrenteDoCarro;

    private void Start()
    {
        larguraDaBarra = barraDeTempo.sizeDelta.x;

        velocidadeBase = config.velocidadeInicial;
        velocidade = velocidadeBase;
        janela = config.janelaInicial;
        Jogando = true;
        PosicionarProximoSemaforo();
        MostrarFeedback(config.textoDeInicio, config.corNeutra);
        AtualizarTextos();
    }

    private void Update()
    {
        if (!Jogando)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            Correr();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            Parar();
        }

        tempoNoEstado += Time.deltaTime;
        float dt = Time.deltaTime;

        switch (estado)
        {
            case Estado.Dirigindo:
                velocidade = Mathf.MoveTowards(velocidade, velocidadeBase, velocidadeBase * dt);
                if (DistanciaAteAFaixa <= velocidadeBase * janela)
                {
                    AcenderSemaforo();
                }

                break;

            case Estado.Decidindo:
                if (cor == CorDoSemaforo.Amarelo)
                {
                    velocidade = Mathf.MoveTowards(velocidade, velocidadeBase * config.fatorDevagarNoAmarelo, velocidadeBase * dt);
                }

                if (DistanciaAteAFaixa <= 0f)
                {
                    ChegouNaFaixaSemResponder();
                }

                break;

            case Estado.Freando:
                velocidade = Mathf.Max(0f, velocidade - desaceleracao * dt);
                if (velocidade <= 1f || DistanciaAteAFaixa <= 1f)
                {
                    PararNaFaixa();
                }

                break;

            case Estado.Parado:
                velocidade = 0f;
                if (tempoNoEstado >= config.tempoParadoNoVermelho)
                {
                    semaforo.Acender(CorDoSemaforo.Verde);
                    MudarEstado(Estado.Passando);
                }

                break;

            case Estado.Passando:
                float alvo = cor == CorDoSemaforo.Amarelo ? velocidadeBase * config.fatorDevagarNoAmarelo : velocidadeBase;
                velocidade = Mathf.MoveTowards(velocidade, alvo, velocidadeBase * dt);
                if (DistanciaAteAFaixa < -carro.rect.width - 150f)
                {
                    SemaforoPassou();
                }

                break;
        }

        if (Jogando)
        {
            MoverRua(velocidade * dt);
            AtualizarBarra();
        }
    }

    // Chamados pelas teclas e pelos botões da tela.
    public void Correr()
    {
        Responder(CorDoSemaforo.Verde);
    }

    public void Parar()
    {
        Responder(CorDoSemaforo.Vermelho);
    }

    private void Responder(CorDoSemaforo acao)
    {
        // Tira o foco do botão clicado, senão o espaço "clica" nele de novo.
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (!AguardandoReacao)
        {
            return;
        }

        respondeu = true;
        if (acao == cor)
        {
            Acerto();
        }
        else
        {
            Erro(config.textoTeclaErrada);
        }

        if (!Jogando)
        {
            return;
        }

        // No vermelho o carro sempre freia até a faixa, mesmo se a criança errou a tecla.
        if (cor == CorDoSemaforo.Vermelho)
        {
            float distancia = Mathf.Max(1f, DistanciaAteAFaixa);
            desaceleracao = velocidade * velocidade / (2f * distancia);
            MudarEstado(Estado.Freando);
        }
        else
        {
            MudarEstado(Estado.Passando);
        }
    }

    private void AcenderSemaforo()
    {
        cor = semaforosPassados < Tutorial.Length ? Tutorial[semaforosPassados] : (CorDoSemaforo)Random.Range(0, 3);
        semaforo.Acender(cor);
        respondeu = false;
        distanciaAoAcender = Mathf.Max(1f, DistanciaAteAFaixa);
        MostrarFeedback(semaforosPassados < Tutorial.Length ? config.Dica(cor) : "", config.corNeutra);
        MudarEstado(Estado.Decidindo);
    }

    private void ChegouNaFaixaSemResponder()
    {
        respondeu = true;
        if (cor == CorDoSemaforo.Amarelo)
        {
            Acerto();
            MudarEstado(Estado.Passando);
            return;
        }

        Erro(config.textoSemReacao);
        if (!Jogando)
        {
            return;
        }

        if (cor == CorDoSemaforo.Vermelho)
        {
            PararNaFaixa();
        }
        else
        {
            MudarEstado(Estado.Passando);
        }
    }

    private void PararNaFaixa()
    {
        // Encosta o carro exatamente na faixa.
        MoverRua(Mathf.Max(0f, DistanciaAteAFaixa));
        velocidade = 0f;
        MudarEstado(Estado.Parado);
    }

    private void SemaforoPassou()
    {
        semaforosPassados++;
        AtualizarTextos();
        if (semaforosPassados >= config.semaforosParaVencer)
        {
            FimDeJogo(string.Format(config.tituloAoVencer, config.semaforosParaVencer));
            return;
        }

        velocidadeBase = Mathf.Min(config.velocidadeMaxima, velocidadeBase + config.aumentoDeVelocidadePorSemaforo);
        janela = Mathf.Max(config.janelaMinima, janela - config.reducaoDaJanelaPorSemaforo);
        PosicionarProximoSemaforo();
    }

    private void PosicionarProximoSemaforo()
    {
        RectTransform rt = semaforo.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(larguraDaTela / 2f + 150f, rt.anchoredPosition.y);
        semaforo.Acender(null);
        MudarEstado(Estado.Dirigindo);
    }

    private void MudarEstado(Estado novo)
    {
        estado = novo;
        tempoNoEstado = 0f;
    }

    private void MoverRua(float distancia)
    {
        RectTransform rt = semaforo.GetComponent<RectTransform>();
        rt.anchoredPosition -= new Vector2(distancia, 0f);

        deslocamentoDasFaixas = Mathf.Repeat(deslocamentoDasFaixas + distancia, espacamentoDasFaixas);
        float inicio = -larguraDaTela / 2f - espacamentoDasFaixas;
        for (int i = 0; i < faixasDaRua.Length; i++)
        {
            Vector2 pos = faixasDaRua[i].anchoredPosition;
            pos.x = inicio + i * espacamentoDasFaixas - deslocamentoDasFaixas;
            faixasDaRua[i].anchoredPosition = pos;
        }
    }

    private void Acerto()
    {
        acertos++;
        combo++;
        comboMaximo = Mathf.Max(comboMaximo, combo);
        MostrarFeedback(config.Parabens(cor), config.corDoAcerto);
        AtualizarTextos();
    }

    private void Erro(string mensagem)
    {
        combo = 0;
        vidas.Perder();
        MostrarFeedback(mensagem + "\n" + config.Dica(cor), config.corDoErro);
        AtualizarTextos();
        if (vidas.Restantes == 0)
        {
            FimDeJogo(config.tituloAoPerder);
        }
    }

    private void FimDeJogo(string titulo)
    {
        Jogando = false;
        AtualizarBarra();
        FinalizarJogo(Pontuacao, titulo);
    }

    private void AtualizarBarra()
    {
        float fracao = AguardandoReacao ? Mathf.Clamp01(DistanciaAteAFaixa / distanciaAoAcender) : 0f;
        barraDeTempo.sizeDelta = new Vector2(larguraDaBarra * fracao, barraDeTempo.sizeDelta.y);
    }

    private void MostrarFeedback(string mensagem, Color corDoTexto)
    {
        textoFeedback.text = mensagem;
        textoFeedback.color = corDoTexto;
    }

    private void AtualizarTextos()
    {
        textoPontos.text = string.Format(config.formatoPontos, Pontuacao);
        textoCombo.text = string.Format(config.formatoCombo, combo, comboMaximo);
        textoSemaforos.text = string.Format(config.formatoSemaforos, Mathf.Min(semaforosPassados + 1, config.semaforosParaVencer), config.semaforosParaVencer);
    }
}
