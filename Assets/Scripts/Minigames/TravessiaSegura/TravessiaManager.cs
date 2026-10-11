using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Regras do "Travessia Segura" (3D). A criança sai da calçada de cá e precisa chegar à de lá:
// vai até a faixa, olha para os dois lados, respeita o semáforo de pedestres e dá passagem à
// ambulância. Se um veículo encosta nela, tropeça, volta ao início e perde uma vida (estrela).
//
// O que o nível (LevelConfig) define:
//   quantidadeDeObstaculos   pistas de carro (1 a 3)
//   velocidade               multiplicador da velocidade dos carros
//   errosPermitidos          tropeços permitidos (vidas = errosPermitidos + 1; -1 usa o padrão da config)
//   regras extras            "comecar_fora_da_faixa", "semaforo", "ciclista", "ambulancia", "imprevisiveis"
//   extras                   "intervaloDeCarros", "tempoVerde", "tempoVermelho", "intervaloDaAmbulancia"
// Estrelas: medida VidasRestantes (3 vidas = 3 estrelas).
//
// Posições do jogador: 0 = calçada de cá; 1..N = pistas (a ciclovia, se houver, é a última);
// N+1 = calçada de lá. Na calçada de cá ele também anda de lado: coluna -1, 0 (faixa) ou +1.
public class TravessiaManager : MinigameBase
{
    public const string RegraComecarForaDaFaixa = "comecar_fora_da_faixa";
    public const string RegraSemaforo = "semaforo";
    public const string RegraCiclista = "ciclista";
    public const string RegraAmbulancia = "ambulancia";
    public const string RegraImprevisiveis = "imprevisiveis";

    [Header("Travessia Segura")]
    public TravessiaConfig config;
    public TravessiaJogador jogador;
    public TravessiaTrafego trafego;
    public TravessiaSemaforo semaforo;
    public TravessiaHUD hud;
    public Camera cameraDoJogo;
    public AudioSource som;

    [Header("Cenário que se ajusta à largura da rua")]
    public Transform asfalto;
    // Tudo que fica do outro lado da rua (calçada, casas, chegada): anda junto com a largura.
    public Transform ladoDeLa;
    public Transform pinturas;
    // Modelos inativos, copiados pra montar as pinturas da rua e as setas do "olhar".
    public GameObject modeloDeTraco;
    public GameObject modeloDeListra;
    public GameObject modeloDeLinhaDeParada;
    public GameObject modeloDeCiclovia;
    public GameObject modeloDeCiclista;
    public SpriteRenderer modeloDeSeta;
    public Sprite spritePerigo;
    public Sprite spriteLivre;
    public Color corPerigo = new Color(0.82f, 0.33f, 0.25f);
    public Color corLivre = new Color(0.31f, 0.56f, 0.42f);

    [Header("Efeitos")]
    public ParticleSystem confete;
    public ParticleSystem estrelinhas;

    public const float AlturaDaCalcada = 0.15f;

    // Estado da partida (público só pra leitura, pra teste e depuração).
    public int Posicao { get; private set; }
    public int Coluna { get; private set; }
    public int Vidas { get; private set; }
    public int Tropecos { get; private set; }
    public bool Olhou { get; private set; }
    public float LarguraDaRua { get; private set; }

    private bool emCena;      // tropeçando ou comemorando: ninguém anda
    private bool comecou;
    private float tempoInicio;
    private bool semaforoLigado;
    private bool temAmbulancia;
    private readonly List<GameObject> setas = new List<GameObject>();

    private int UltimaPosicao
    {
        get { return trafego.pistas.Count + 1; }
    }

    private void Start()
    {
        MontarRua();

        Vidas = Nivel.TemLimiteDeErros ? Nivel.errosPermitidos + 1 : config.vidas;
        hud.MostrarNivel(NivelAtual);
        hud.MostrarVidas(Vidas);
        hud.Ambulancia(false);

        semaforoLigado = Nivel.TemRegra(RegraSemaforo);
        if (semaforoLigado)
        {
            semaforo.Ligar(Nivel.Extra("tempoVerde", config.tempoVerde), Nivel.Extra("tempoVermelho", config.tempoVermelho), config.tempoPiscando);
        }
        else
        {
            semaforo.Desligar();
        }
        hud.Semaforo(semaforoLigado, false);

        temAmbulancia = Nivel.TemRegra(RegraAmbulancia);
        trafego.Preparar(config, semaforoLigado ? semaforo : null, modeloDeCiclista,
            Nivel.velocidade > 0f ? Nivel.velocidade : 1f,
            Nivel.Extra("intervaloDeCarros", config.intervaloPadraoDeCarros),
            Nivel.TemRegra(RegraImprevisiveis), temAmbulancia,
            Nivel.Extra("intervaloDaAmbulancia", config.intervaloDaAmbulancia));

        VoltarAoInicio(Nivel.TemRegra(RegraComecarForaDaFaixa) ? -1 : 0);
        hud.Dica(Coluna == 0 ? config.dicaInicial : config.dicaIrAteAFaixa, 0f);
    }

    // ------------------------------------------------------------------ montagem da rua

    private void MontarRua()
    {
        int pistasDeCarro = Mathf.Clamp(Nivel.quantidadeDeObstaculos, 1, 3);
        trafego.pistas.Clear();
        float z = 0f;
        for (int i = 0; i < pistasDeCarro; i++)
        {
            // Mão brasileira: as pistas mais perto vêm da esquerda; as do outro lado, da direita.
            int direcao = i < (pistasDeCarro + 1) / 2 ? 1 : -1;
            trafego.pistas.Add(new TravessiaTrafego.Pista { z = z + config.larguraDaPista / 2f, largura = config.larguraDaPista, direcao = direcao });
            z += config.larguraDaPista;
        }
        if (Nivel.TemRegra(RegraCiclista))
        {
            trafego.pistas.Add(new TravessiaTrafego.Pista { z = z + config.larguraDaCiclovia / 2f, largura = config.larguraDaCiclovia, direcao = -1, ciclovia = true });
            z += config.larguraDaCiclovia;
        }
        LarguraDaRua = z;

        asfalto.localScale = new Vector3(asfalto.localScale.x, asfalto.localScale.y, LarguraDaRua);
        asfalto.position = new Vector3(0f, asfalto.position.y, LarguraDaRua / 2f);
        ladoDeLa.position = new Vector3(0f, 0f, LarguraDaRua);

        float comprimento = config.distanciaDeSurgimento;
        for (int i = 0; i < trafego.pistas.Count; i++)
        {
            TravessiaTrafego.Pista p = trafego.pistas[i];

            if (p.ciclovia)
            {
                Copiar(modeloDeCiclovia, new Vector3(0f, 0.012f, p.z), new Vector3(comprimento * 2f, 1f, p.largura - 0.2f));
            }
            else
            {
                // Linha de parada, do lado de onde os carros vêm.
                Copiar(modeloDeLinhaDeParada, new Vector3(-p.direcao * (config.linhaDeParada - 0.3f), 0.014f, p.z), new Vector3(0.45f, 1f, p.largura - 0.3f));
            }

            // Tracejado entre esta pista e a próxima (menos em cima da faixa de pedestre).
            if (i < trafego.pistas.Count - 1)
            {
                float zLinha = p.z + p.largura / 2f;
                for (float x = -comprimento; x <= comprimento; x += 3.2f)
                {
                    if (Mathf.Abs(x) > config.linhaDeParada + 0.5f)
                    {
                        Copiar(modeloDeTraco, new Vector3(x, 0.013f, zLinha), new Vector3(1.6f, 1f, 0.16f));
                    }
                }
            }
        }

        // Faixa de pedestre: listras ao longo da rua, de um meio-fio ao outro.
        for (float zl = 0.45f; zl < LarguraDaRua - 0.2f; zl += 0.9f)
        {
            Copiar(modeloDeListra, new Vector3(0f, 0.015f, zl), new Vector3(config.meiaLarguraDaFaixa * 2f, 1f, 0.5f));
        }

        EnquadrarCamera();
    }

    // Câmera elevada e inclinada, atrás da calçada de cá. Sobe e inclina até caber a rua toda entre
    // os botões de baixo (a criança, na calçada de cá) e os painéis de cima (a calçada de lá).
    private void EnquadrarCamera()
    {
        Transform cam = cameraDoJogo.transform;
        Vector3 partida = new Vector3(0f, AlturaDaCalcada, -1.3f);
        Vector3 chegada = new Vector3(0f, AlturaDaCalcada + 1.4f, LarguraDaRua + 2.2f);
        for (float altura = 8.5f; altura <= 30f; altura += 0.5f)
        {
            cam.position = new Vector3(0f, altura, -5.5f - altura * 0.28f);
            for (float alvoZ = LarguraDaRua * 0.5f; alvoZ >= -8f; alvoZ -= 0.25f)
            {
                cam.LookAt(new Vector3(0f, 0f, alvoZ));
                if (cameraDoJogo.WorldToViewportPoint(partida).y >= 0.36f)
                {
                    break;
                }
            }
            if (cameraDoJogo.WorldToViewportPoint(chegada).y <= 0.84f)
            {
                return;
            }
        }
    }

    private void Copiar(GameObject modelo, Vector3 posicao, Vector3 escala)
    {
        GameObject go = Instantiate(modelo, pinturas);
        go.transform.position = posicao;
        go.transform.localScale = escala;
        go.SetActive(true);
    }

    private Vector3 PontoDaPosicao(int posicao, int coluna)
    {
        if (posicao <= 0)
        {
            return new Vector3(coluna * config.distanciaAoLadoDaFaixa, AlturaDaCalcada, -1.3f);
        }
        if (posicao >= UltimaPosicao)
        {
            return new Vector3(0f, AlturaDaCalcada, LarguraDaRua + 1.3f);
        }
        return new Vector3(0f, 0.02f, trafego.pistas[posicao - 1].z);
    }

    private void VoltarAoInicio(int coluna)
    {
        Posicao = 0;
        Coluna = coluna;
        Olhou = false;
        jogador.Colocar(PontoDaPosicao(0, Coluna));
        hud.NaCalcada(true);
    }

    // ------------------------------------------------------------------ a cada quadro

    private void Update()
    {
        if (EscolhendoNivel || Finalizado)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) Andar();
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Voltar();
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) IrParaOLado(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) IrParaOLado(1);
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.Return)) Olhar();

        if (semaforoLigado)
        {
            hud.Semaforo(true, semaforo.EstadoAtual == TravessiaSemaforo.Estado.Verde || semaforo.EstadoAtual == TravessiaSemaforo.Estado.Piscando);
            if (semaforo.AcabouDeAbrir)
            {
                Tocar(config.sinalVerde);
                if (Posicao == 0 && !emCena)
                {
                    hud.Dica(config.dicaSinalAbriu);
                }
            }
        }

        if (temAmbulancia)
        {
            // O aviso vermelho do HUD já diz o que fazer; não repete na dica.
            hud.Ambulancia(trafego.AmbulanciaChegando);
        }

        // Só dá pra ser atingido dentro da rua.
        if (!emCena && jogador.transform.position.z > 0f && jogador.transform.position.z < LarguraDaRua)
        {
            TravessiaVeiculo veiculo = trafego.Atingiu(jogador.transform.position, config.raioDoJogador);
            if (veiculo != null)
            {
                Tropecar(veiculo);
            }
        }
    }

    // ------------------------------------------------------------------ ações (teclado e botões da tela)

    public void Andar()
    {
        if (!PodeAgir())
        {
            return;
        }

        if (Posicao == 0)
        {
            // Sair da calçada tem regra; depois disso é com a criança.
            string motivo = MotivoPraNaoSair();
            if (motivo != null)
            {
                Bloquear(motivo);
                return;
            }
        }
        else if (Posicao >= UltimaPosicao)
        {
            return;
        }

        IniciarCronometro();
        Posicao++;
        hud.NaCalcada(false);
        TocarPasso();
        jogador.MoverPara(PontoDaPosicao(Posicao, 0), config.duracaoDoPasso, AoChegar);
    }

    // Por que ainda não pode pisar na rua (null = pode).
    public string MotivoPraNaoSair()
    {
        if (Coluna != 0) return config.dicaForaDaFaixa;
        if (!Olhou) return config.dicaPrecisaOlhar;
        if (semaforoLigado && !semaforo.PedestrePodeComecar) return config.dicaSinalVermelho;
        if (temAmbulancia && trafego.AmbulanciaChegando) return config.dicaAmbulancia;
        return null;
    }

    public void Voltar()
    {
        if (!PodeAgir() || Posicao <= 0 || Posicao >= UltimaPosicao)
        {
            return;
        }

        Posicao--;
        TocarPasso();
        jogador.MoverPara(PontoDaPosicao(Posicao, 0), config.duracaoDoPasso, AoChegar);
    }

    public void IrParaAEsquerda()
    {
        IrParaOLado(-1);
    }

    public void IrParaADireita()
    {
        IrParaOLado(1);
    }

    private void IrParaOLado(int lado)
    {
        if (!PodeAgir() || Posicao != 0)
        {
            return;
        }

        int nova = Mathf.Clamp(Coluna + lado, -1, 1);
        if (nova == Coluna)
        {
            return;
        }

        Coluna = nova;
        Olhou = false;
        TocarPasso();
        jogador.MoverPara(PontoDaPosicao(0, Coluna), config.duracaoDoPasso * 2.2f, AoChegar);
        if (Coluna == 0)
        {
            hud.Dica(config.dicaInicial, 0f);
        }
    }

    public void Olhar()
    {
        if (!PodeAgir())
        {
            return;
        }

        if (Posicao == 0)
        {
            Olhou = true;
        }
        Tocar(config.olhar);
        jogador.Olhar(0.9f);

        bool perigo = MostrarSetas();
        hud.Dica(perigo ? config.dicaVemCarro : config.dicaLivre, config.duracaoDasSetas);
    }

    private bool PodeAgir()
    {
        return !EscolhendoNivel && !Finalizado && !emCena && !jogador.Ocupado;
    }

    private void AoChegar()
    {
        if (Posicao >= UltimaPosicao)
        {
            StartCoroutine(Vencer());
            return;
        }
        if (Posicao == 0)
        {
            // Voltou pra calçada: precisa olhar de novo antes de sair.
            Olhou = false;
            hud.NaCalcada(true);
        }
    }

    private void Bloquear(string motivo)
    {
        Tocar(config.bloqueado);
        hud.Dica(motivo);
    }

    // ------------------------------------------------------------------ olhar para os dois lados

    // Uma seta por pista, do lado de onde o veículo vem: vermelha se chega logo, verde se está livre.
    // Devolve true se alguma pista tem perigo.
    private bool MostrarSetas()
    {
        foreach (GameObject velha in setas)
        {
            if (velha != null)
            {
                Destroy(velha);
            }
        }
        setas.Clear();

        bool algumPerigo = false;
        for (int i = 0; i < trafego.pistas.Count; i++)
        {
            TravessiaTrafego.Pista p = trafego.pistas[i];
            bool perigo = trafego.TempoAteAFaixa(i) < config.tempoDePerigo;
            algumPerigo |= perigo;

            SpriteRenderer seta = Instantiate(modeloDeSeta, pinturas);
            seta.sprite = perigo ? spritePerigo : spriteLivre;
            seta.color = perigo ? corPerigo : corLivre;
            // A seta aponta pra onde o veículo vai; o sprite original aponta pra direita (+X).
            seta.flipX = perigo && p.direcao < 0;
            seta.transform.position = new Vector3(-p.direcao * (config.meiaLarguraDaFaixa + 2.4f), 1.5f, p.z);
            seta.transform.rotation = cameraDoJogo.transform.rotation;
            seta.gameObject.SetActive(true);
            setas.Add(seta.gameObject);
            Destroy(seta.gameObject, config.duracaoDasSetas);
        }
        return algumPerigo;
    }

    // ------------------------------------------------------------------ tropeço, vitória e fim

    private void Tropecar(TravessiaVeiculo veiculo)
    {
        emCena = true;
        Vidas--;
        Tropecos++;
        hud.MostrarVidas(Vidas);
        // Carro buzina, bicicleta toca a campainha.
        Tocar(veiculo.ciclista ? config.campainhaDeBicicleta : config.buzina);
        Tocar(config.tropeco);
        if (estrelinhas != null)
        {
            estrelinhas.transform.position = jogador.transform.position + Vector3.up * 1.2f;
            estrelinhas.Play();
        }

        jogador.Tropecar(0.9f, () =>
        {
            if (Vidas <= 0)
            {
                Terminar(false);
                return;
            }
            VoltarAoInicio(0);
            emCena = false;
            hud.Dica(config.dicaTropeco);
        });
    }

    private IEnumerator Vencer()
    {
        emCena = true;
        Tocar(config.acerto);
        jogador.Comemorar();
        if (confete != null)
        {
            confete.transform.position = jogador.transform.position + Vector3.up * 2.2f;
            confete.Play();
        }
        hud.Dica("", 0f);
        yield return new WaitForSeconds(1.8f);
        Terminar(true);
    }

    private void Terminar(bool concluiu)
    {
        trafego.Parar();
        float tempo = comecou ? Time.time - tempoInicio : 0f;
        int segundosExtras = Mathf.CeilToInt(Mathf.Max(0f, tempo - config.tempoIdeal));
        int pontuacao = config.pontuacaoMaxima - Tropecos * config.pontosPorTropeco - segundosExtras * config.pontosPorSegundoExtra;
        pontuacao = Mathf.Clamp(pontuacao, config.pontuacaoMinima, config.pontuacaoMaxima);
        if (!concluiu)
        {
            Tocar(config.erro);
        }

        FinalizarNivel(new ResultadoDoNivel
        {
            concluiu = concluiu,
            pontuacao = concluiu ? pontuacao : 0,
            erros = Tropecos,
            vidasRestantes = Vidas,
            tempoGasto = tempo,
        }, config.tituloDoFim);
    }

    private void IniciarCronometro()
    {
        if (!comecou)
        {
            comecou = true;
            tempoInicio = Time.time;
        }
    }

    // ------------------------------------------------------------------ som

    private void Tocar(AudioClip clipe)
    {
        if (som != null && clipe != null)
        {
            som.PlayOneShot(clipe);
        }
    }

    private void TocarPasso()
    {
        if (config.passos != null && config.passos.Length > 0)
        {
            Tocar(config.passos[Random.Range(0, config.passos.Length)]);
        }
    }
}
