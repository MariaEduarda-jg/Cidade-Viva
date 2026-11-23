using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Regras do "Corre, Correndo!" (3D): corrida em 3 faixas até a chegada. A criança desvia, pula e
// desliza; cada batida gasta um escudo e bater sem escudo encerra a tentativa ("tente de novo").
//
// O que o nível (LevelConfig) define:
//   velocidade        multiplicador da velocidade da corrida
//   errosPermitidos   batidas permitidas = escudos no começo (-1 usa o padrão da config)
//   regras extras     obstáculos liberados: "pocas", "carros", "barreiras", "galhos";
//                     padrões: "duas_faixas", "fileira_cheia", "sequencias", "carros_em_movimento";
//                     clima: "chuva" (só visual)
//   extras            "metaDeDistancia" (m), "espacoEntreObstaculos" (m), "fracaoDeMoedas" (0 a 1)
// Estrelas: 1 por chegar, +1 por chegar com pelo menos 1 escudo, +1 por pegar a maioria das moedas.
public class CorreManager : MinigameBase
{
    public const string RegraChuva = "chuva";

    [Header("Corre, Correndo!")]
    public CorreConfig config;
    public CorreJogador jogador;
    public CorrePista pista;
    public CorreHUD hud;
    public AudioSource som;
    public AudioSource musica;

    [Header("Efeitos")]
    public ParticleSystem confete;
    public ParticleSystem estrelinhas;
    public ParticleSystem brilho;
    public ParticleSystem respingo;

    [Header("Chuva (nível com a regra \"chuva\")")]
    public ParticleSystem chuva;
    public Renderer[] asfalto;
    public Material asfaltoMolhado;
    public Light sol;
    public Color corDoSolNaChuva = new Color(0.72f, 0.78f, 0.86f);
    public Color corDaNeblinaNaChuva = new Color(0.66f, 0.72f, 0.78f);

    // Estado da partida (público só pra leitura, pra teste e depuração).
    public int Escudos { get; private set; }
    public int Moedas { get; private set; }
    public int Batidas { get; private set; }
    public float Meta { get; private set; }
    public bool Correndo { get; private set; }

    private float fracaoDeMoedas;
    private float proximoPasso;
    private float proximoAviso;
    private bool avisouChegada;
    private readonly Dictionary<string, int> avisosDados = new Dictionary<string, int>();

    // Moedas que precisam ser pegas pra ganhar a estrela das moedas.
    public int MoedasParaEstrela
    {
        get { return Mathf.CeilToInt(pista.MoedasCriadas * fracaoDeMoedas); }
    }

    private void Start()
    {
        Meta = Nivel.Extra("metaDeDistancia", 200f);
        fracaoDeMoedas = Nivel.Extra("fracaoDeMoedas", config.fracaoDeMoedasParaEstrela);
        Escudos = Nivel.TemLimiteDeErros ? Mathf.Min(Nivel.errosPermitidos, config.escudosMaximos) : config.escudosIniciais;

        jogador.Preparar(config);
        pista.Preparar(config, Nivel, Meta, () => Escudos);
        pista.aoCarroTrocarDeFaixa = AoCarroTrocarDeFaixa;

        hud.MostrarNivel(NivelAtual);
        hud.MostrarEscudos(Escudos);
        hud.MostrarMoedas(0);
        hud.MostrarDistancia(0f, Meta, config.formatoDaDistancia);

        bool chovendo = Nivel.TemRegra(RegraChuva);
        if (chovendo)
        {
            Chover();
        }
        hud.Dica(chovendo ? config.dicaChuva : config.dicaInicial, 4f);

        if (musica != null && config.musica != null)
        {
            musica.clip = config.musica;
            musica.loop = true;
            musica.Play();
        }

        Correndo = true;
        jogador.Correr();
        pista.Comecar();
    }

    // Chuva leve: gotas, asfalto brilhando e luz mais fria. Não muda a jogabilidade.
    private void Chover()
    {
        if (chuva != null)
        {
            chuva.Play();
        }
        if (asfaltoMolhado != null)
        {
            foreach (Renderer r in asfalto)
            {
                r.sharedMaterial = asfaltoMolhado;
            }
        }
        if (sol != null)
        {
            sol.color = corDoSolNaChuva;
            sol.intensity *= 0.8f;
        }
        RenderSettings.fogColor = corDaNeblinaNaChuva;
    }

    // ------------------------------------------------------------------ a cada quadro

    private void Update()
    {
        if (EscolhendoNivel || Finalizado || !Correndo)
        {
            return;
        }

        if (Teclado.Apertou(KeyCode.LeftArrow) || Teclado.Apertou(KeyCode.A)) IrParaAEsquerda();
        if (Teclado.Apertou(KeyCode.RightArrow) || Teclado.Apertou(KeyCode.D)) IrParaADireita();
        if (Teclado.Apertou(KeyCode.UpArrow) || Teclado.Apertou(KeyCode.W) || Teclado.Apertou(KeyCode.Space)) Pular();
        if (Teclado.Apertou(KeyCode.DownArrow) || Teclado.Apertou(KeyCode.S)) Deslizar();

        hud.MostrarDistancia(pista.Distancia, Meta, config.formatoDaDistancia);
        Passos();
        ConferirObjetos();

        if (!avisouChegada && pista.Distancia > Meta - 30f)
        {
            avisouChegada = true;
            hud.Dica(config.dicaChegada);
        }
        if (Correndo && pista.Distancia >= Meta)
        {
            StartCoroutine(Vencer());
        }
    }

    private void Passos()
    {
        if (jogador.Pulando || jogador.Deslizando || Time.time < proximoPasso || config.passos == null || config.passos.Length == 0)
        {
            return;
        }
        proximoPasso = Time.time + 2.6f / Mathf.Max(4f, pista.Velocidade);
        Tocar(config.passos[Random.Range(0, config.passos.Length)], 0.45f);
    }

    private void ConferirObjetos()
    {
        float meiaFaixa = config.larguraDaFaixa * 0.42f;
        for (int i = pista.objetos.Count - 1; i >= 0; i--)
        {
            CorreObjeto o = pista.objetos[i];
            if (o.resolvido)
            {
                continue;
            }

            bool naMinhaFaixa = Mathf.Abs(o.x - jogador.X) < meiaFaixa;
            switch (o.categoria)
            {
                case CorreObjeto.Categoria.Moeda:
                    if (naMinhaFaixa && Mathf.Abs(o.z) < 0.9f && (!o.noAlto || jogador.Altura > 0.5f))
                    {
                        Moedas++;
                        hud.MostrarMoedas(Moedas);
                        Tocar(config.moeda, 0.7f);
                        Soltar(brilho, o.transform.position);
                        pista.Remover(o);
                    }
                    break;

                case CorreObjeto.Categoria.Escudo:
                    if (naMinhaFaixa && Mathf.Abs(o.z) < 0.9f)
                    {
                        Escudos = Mathf.Min(Escudos + 1, config.escudosMaximos);
                        hud.MostrarEscudos(Escudos);
                        hud.Dica(config.dicaEscudo, 1.6f);
                        Tocar(config.escudo);
                        Soltar(brilho, o.transform.position);
                        pista.Remover(o);
                    }
                    break;

                case CorreObjeto.Categoria.Obstaculo:
                    Avisar(o);
                    if (naMinhaFaixa && Mathf.Abs(o.z) < o.tipo.meioComprimento + 0.25f && !Passou(o))
                    {
                        o.resolvido = true;
                        Bater(o);
                        if (!Correndo)
                        {
                            return;
                        }
                    }
                    break;
            }
        }
    }

    // Pulando por cima ou deslizando por baixo, quando o obstáculo deixa.
    private bool Passou(CorreObjeto o)
    {
        if (jogador.Invencivel)
        {
            return true;
        }
        return (o.tipo.pulavel && jogador.Altura > 0.35f) || (o.tipo.deslizavel && jogador.Deslizando);
    }

    // O motivo do perigo aparece por um instante, nas primeiras vezes que cada tipo de obstáculo chega perto.
    private void Avisar(CorreObjeto o)
    {
        if (o.avisou || o.z > config.distanciaDoAviso || o.z < 8f || Time.time < proximoAviso || string.IsNullOrEmpty(o.tipo.aviso))
        {
            return;
        }
        avisosDados.TryGetValue(o.tipo.id, out int dados);
        o.avisou = true;
        if (dados >= config.avisosPorTipo)
        {
            return;
        }
        avisosDados[o.tipo.id] = dados + 1;
        proximoAviso = Time.time + 2.4f;
        hud.Dica(o.tipo.aviso);
    }

    private void AoCarroTrocarDeFaixa(CorreObjeto carro)
    {
        Tocar(config.buzina, 0.8f);
        hud.Dica(carro.tipo.aviso);
        proximoAviso = Time.time + 2.4f;
    }

    // ------------------------------------------------------------------ ações (teclado e botões da tela)

    public void IrParaAEsquerda()
    {
        if (PodeAgir())
        {
            jogador.MudarDeFaixa(-1);
        }
    }

    public void IrParaADireita()
    {
        if (PodeAgir())
        {
            jogador.MudarDeFaixa(1);
        }
    }

    public void Pular()
    {
        if (PodeAgir() && jogador.Pular())
        {
            Tocar(config.pulo, 0.7f);
        }
    }

    public void Deslizar()
    {
        if (PodeAgir() && jogador.Deslizar())
        {
            Tocar(config.deslizar, 0.8f);
        }
    }

    private bool PodeAgir()
    {
        return !EscolhendoNivel && !Finalizado && Correndo;
    }

    // ------------------------------------------------------------------ batida, vitória e fim

    private void Bater(CorreObjeto o)
    {
        Batidas++;
        if (config.batida != null && config.batida.Length > 0)
        {
            Tocar(config.batida[Random.Range(0, config.batida.Length)], 0.8f);
        }
        Tocar(config.tropeco);
        // Buraco e poça: espirra; o resto: estrelinhas girando em cima da cabeça.
        Soltar(o.tipo.pulavel && respingo != null ? respingo : estrelinhas, jogador.transform.position + Vector3.up * (o.tipo.pulavel ? 0.3f : 1.5f));
        hud.Dica(string.Format(config.formatoDaBatida, o.tipo.aviso), 3f);
        proximoAviso = Time.time + 3f;

        if (Escudos <= 0)
        {
            StartCoroutine(Perder());
            return;
        }

        Escudos--;
        hud.MostrarEscudos(Escudos);
        pista.Frear(0.35f);
        jogador.Bater();
    }

    private IEnumerator Perder()
    {
        Correndo = false;
        pista.Parar();
        jogador.Cair();
        Tocar(config.erro);
        yield return new WaitForSeconds(1.5f);
        Terminar(false);
    }

    private IEnumerator Vencer()
    {
        Correndo = false;
        pista.Parar();
        jogador.Comemorar();
        Tocar(config.acerto);
        Soltar(confete, jogador.transform.position + Vector3.up * 2.4f);
        hud.Dica("", 0f);
        yield return new WaitForSeconds(1.8f);
        Terminar(true);
    }

    private void Terminar(bool concluiu)
    {
        if (musica != null)
        {
            musica.Stop();
        }
        int pontuacao = config.pontosPorConcluir + Moedas * config.pontosPorMoeda + Escudos * config.pontosPorEscudo;
        FinalizarNivel(new ResultadoDoNivel
        {
            concluiu = concluiu,
            pontuacao = concluiu ? pontuacao : 0,
            erros = Batidas,
            vidasRestantes = Escudos,
            tempoGasto = Time.timeSinceLevelLoad,
        }, config.tituloDoFim);
    }

    protected override int CalcularEstrelas(ResultadoDoNivel resultado)
    {
        if (!resultado.concluiu)
        {
            return 0;
        }
        int estrelas = 1;
        if (Escudos >= 1)
        {
            estrelas++;
        }
        if (Moedas >= MoedasParaEstrela)
        {
            estrelas++;
        }
        return estrelas;
    }

    // ------------------------------------------------------------------ som e partículas

    private void Tocar(AudioClip clipe, float volume = 1f)
    {
        if (som != null && clipe != null)
        {
            som.PlayOneShot(clipe, volume);
        }
    }

    private static void Soltar(ParticleSystem efeito, Vector3 onde)
    {
        if (efeito != null)
        {
            efeito.transform.position = onde;
            efeito.Play();
        }
    }
}
