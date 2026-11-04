using System;
using System.Collections;
using UnityEngine;

// Como um semáforo do percurso se comporta num nível (ver SinaisConfig.roteiros).
public enum ComportamentoDoSemaforo
{
    Ciclo,          // troca sozinho: verde, amarelo, vermelho (nível difícil).
    FicaVerde,      // fica verde até o carrinho passar.
    FicaVermelho,   // vermelho até o carrinho parar e quem atravessa terminar; aí abre o verde.
    FechaAoChegar,  // verde; quando o carrinho chega perto: amarelo (tempo fixo), vermelho, espera e verde.
}

// Quais lugares do percurso um nível usa e como o semáforo de cada um se comporta.
[Serializable]
public class RoteiroDoNivel
{
    public int primeiroLugar;
    public ComportamentoDoSemaforo[] semaforos = new ComportamentoDoSemaforo[0];
}

// Modo "Motorista Mirim" da Central de Sinais: a criança dirige um carrinho por um passeio com curvas
// pelo bairro e só controla a velocidade (acelerar e frear; o carrinho faz as curvas sozinho). Cada
// lugar do passeio tem um semáforo por um motivo diferente, e a regra é a da definição de cada sinal,
// julgada na hora em que a frente do carrinho chega à linha:
//   verde     "podem continuar andando"                 -> passa.
//   amarelo   "o motorista deve diminuir a velocidade"  -> o acerto é diminuir; cruzar rápido é erro.
//             O amarelo dura um tempo fixo e vira vermelho, como em qualquer semáforo.
//   vermelho  "devem parar"                             -> não pode cruzar; para antes e espera abrir.
// Erro (cruzar no vermelho, ou rápido no amarelo): o carrinho "tosse", volta um pouco e um balão
// explica com o trecho da definição. A primeira vez que cada regra é cumprida abre o cartão do sinal.
//
// O que cada campo do nível faz aqui:
//   extras "tempoVerde", "tempoAmarelo", "tempoVermelho"   duração das luzes dos semáforos em Ciclo.
// Os lugares e o comportamento de cada semáforo vêm de SinaisConfig.roteiros (um por nível).
public class SinaisMotorista : MonoBehaviour
{
    public Transform carro;
    [Tooltip("Os lugares do percurso com semáforo, na ordem em que o carrinho chega.")]
    public SinaisCruzamento[] cruzamentos = new SinaisCruzamento[0];
    [Tooltip("Arco de chegada: fica depois do último lugar usado no nível.")]
    public Transform chegada;
    public float distanciaAteAChegada = 28f;
    [Tooltip("O carrinho começa esta distância antes da linha do primeiro lugar do nível.")]
    public float distanciaDoComeco = 40f;
    [Tooltip("Quanto a mão do carrinho fica à direita do meio da rua (o arco de chegada vai no meio da rua).")]
    public float ladoDaPista = 1.8f;

    [Header("Percurso (gerado pelo construtor)")]
    public Vector3[] rota = new Vector3[0];
    [Tooltip("Distância entre dois pontos seguidos da rota.")]
    public float passoDaRota = 0.25f;

    public float Velocidade { get; private set; }
    // Distância já percorrida na rota (posição do meio do carrinho).
    public float S { get; private set; }
    // Índice do próximo lugar (maior que Ultimo quando já passou de todos).
    public int Proximo { get; private set; }
    public int Primeiro { get; private set; }
    public int Ultimo { get; private set; }
    public bool Recuando { get; private set; }
    // Parou antes da linha com o sinal vermelho (falta só esperar o verde).
    public bool ParouNoVermelho { get; private set; }
    public SinaisCruzamento ProximoCruzamento { get { return Proximo <= Ultimo ? cruzamentos[Proximo] : null; } }

    private SinaisManager manager;
    private SinaisConfig config;
    private RoteiroDoNivel roteiro;
    private bool acelerando;
    private bool freando;
    private bool rodando;
    private float sDaChegada;
    private float tempoParado;
    private bool avisouDoLugar;
    private bool jaAbriu;
    private bool avisouQueEstaLonge;
    private bool jaAndou;
    private bool acertouOAmarelo;
    private bool errouOAmarelo;
    private float tempoParadoLonge;
    private Vector3 olharDaCamera;

    // Distância da frente do carrinho até a linha do próximo semáforo (negativa = já passou).
    public float DistanciaAteALinha
    {
        get { return ProximoCruzamento == null ? float.MaxValue : ProximoCruzamento.sDaLinha - (S + config.frenteDoCarro); }
    }

    private ComportamentoDoSemaforo ComportamentoDe(int lugar)
    {
        int i = lugar - Primeiro;
        return i >= 0 && i < roteiro.semaforos.Length ? roteiro.semaforos[i] : ComportamentoDoSemaforo.Ciclo;
    }

    public void Comecar(SinaisManager manager)
    {
        this.manager = manager;
        config = manager.config;
        NivelConfig nivel = manager.Nivel;
        roteiro = config.roteiros[Mathf.Clamp(manager.NivelAtual - 1, 0, config.roteiros.Length - 1)];
        Primeiro = Mathf.Clamp(roteiro.primeiroLugar, 0, cruzamentos.Length - 1);
        Ultimo = Mathf.Clamp(Primeiro + roteiro.semaforos.Length - 1, Primeiro, cruzamentos.Length - 1);
        Proximo = Primeiro;

        float verde = nivel.Extra("tempoVerde", 6f), amarelo = nivel.Extra("tempoAmarelo", 5f), vermelho = nivel.Extra("tempoVermelho", 5f);
        for (int i = 0; i < cruzamentos.Length; i++)
        {
            bool usado = i >= Primeiro && i <= Ultimo;
            cruzamentos[i].Usar(usado);
            if (!usado)
            {
                continue;
            }
            SinalDeTransito sinal = cruzamentos[i].sinal;
            switch (ComportamentoDe(i))
            {
                case ComportamentoDoSemaforo.Ciclo:
                    // Cada semáforo começa num ponto diferente do ciclo, pra criança encontrar as três cores.
                    CorDoSinal inicial = i % 3 == 0 ? CorDoSinal.Vermelho : i % 3 == 1 ? CorDoSinal.Verde : CorDoSinal.Amarelo;
                    sinal.LigarCiclo(verde, amarelo, vermelho, inicial, i % 3 == 1 ? verde * 0.5f : 0f);
                    break;
                case ComportamentoDoSemaforo.FicaVermelho:
                    sinal.DesligarCiclo();
                    sinal.Mudar(CorDoSinal.Vermelho);
                    break;
                default:
                    sinal.DesligarCiclo();
                    sinal.Mudar(CorDoSinal.Verde);
                    break;
            }
            sinal.Mudou += AoMudarOSinal;
        }

        S = Mathf.Max(0f, cruzamentos[Primeiro].sDaLinha - distanciaDoComeco);
        sDaChegada = cruzamentos[Ultimo].sDaLinha + distanciaAteAChegada;
        Quaternion giroDaChegada = Quaternion.LookRotation(Direcao(sDaChegada));
        chegada.rotation = giroDaChegada;
        chegada.position = Posicao(sDaChegada) - giroDaChegada * Vector3.right * ladoDaPista;

        manager.hud.Recado(config.recadoDeComecar);
        rodando = true;
        ColocarCarro();
        olharDaCamera = Direcao(S);
        PosicionarCamera();
        AtualizarTela();
    }

    private void AoMudarOSinal(SinalDeTransito sinal)
    {
        // "Tic" só do semáforo que a criança está olhando.
        if (ProximoCruzamento == null || ProximoCruzamento.sinal != sinal)
        {
            return;
        }
        manager.Tocar(config.somDoTic);
        if (sinal.Cor == CorDoSinal.Amarelo && DistanciaAteALinha <= config.distanciaDoAvisoDeAmarelo)
        {
            manager.hud.Recado(string.Format(config.dicaDoAmarelo, config.trechoDoAmarelo));
        }
    }

    // Botões de segurar e teclado chamam estes dois.
    public void Acelerar(bool segurando)
    {
        acelerando = segurando;
        if (segurando && manager != null)
        {
            manager.LigarRelogio();
        }
    }

    public void Frear(bool segurando)
    {
        freando = segurando;
    }

    private void Update()
    {
        if (!rodando || manager == null || !manager.Rodando || manager.Pausado)
        {
            return;
        }

        bool acelera = acelerando || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        bool freia = freando || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Space);
        if (acelera)
        {
            manager.LigarRelogio();
        }

        if (!Recuando)
        {
            if (freia)
            {
                Velocidade = Mathf.MoveTowards(Velocidade, 0f, config.freio * Time.deltaTime);
            }
            else if (acelera)
            {
                Velocidade = Mathf.MoveTowards(Velocidade, config.velocidadeMaxima, config.aceleracao * Time.deltaTime);
            }
            else
            {
                Velocidade = Mathf.MoveTowards(Velocidade, 0f, config.atrito * Time.deltaTime);
            }
            S += Velocidade * Time.deltaTime;
            ColocarCarro();

            SinaisCruzamento cruzamento = ProximoCruzamento;
            if (cruzamento != null)
            {
                float distancia = DistanciaAteALinha;
                AvisarDoLugar(cruzamento, distancia);
                GerirSemaforo(cruzamento, distancia);
                if (distancia <= 0f)
                {
                    Julgar(cruzamento);
                }
            }
            else if (S >= sDaChegada)
            {
                rodando = false;
                Velocidade = 0f;
                manager.Concluir(config.recadoDeChegada, carro.position + Vector3.up * 2f);
            }
        }
        AtualizarTela();
    }

    private void AvisarDoLugar(SinaisCruzamento cruzamento, float distancia)
    {
        if (!avisouDoLugar && distancia <= config.distanciaDoAvisoDoLugar)
        {
            avisouDoLugar = true;
            if (!string.IsNullOrEmpty(cruzamento.recadoDoLugar))
            {
                manager.hud.Recado(cruzamento.recadoDoLugar);
            }
        }
    }

    // O semáforo do lugar da vez, conforme o roteiro do nível.
    private void GerirSemaforo(SinaisCruzamento cruzamento, float distancia)
    {
        SinalDeTransito sinal = cruzamento.sinal;
        bool parado = sinal.Cor == CorDoSinal.Vermelho && Velocidade < 0.15f && distancia >= 0f;
        if (parado && distancia <= config.distanciaDeParada)
        {
            ParouNoVermelho = true;
        }
        else if (parado && jaAndou && !avisouQueEstaLonge)
        {
            // Só se a criança já saiu do lugar e parou longe da linha por um tempinho.
            tempoParadoLonge += Time.deltaTime;
            if (tempoParadoLonge >= 2.5f)
            {
                avisouQueEstaLonge = true;
                manager.hud.Recado(config.recadoDeChegarPerto);
            }
        }
        else
        {
            tempoParadoLonge = 0f;
        }
        jaAndou = jaAndou || Velocidade > 0.5f;

        // Diminuiu no amarelo: é isso que a definição pede, e é aqui que a criança acerta.
        if (sinal.Cor == CorDoSinal.Amarelo && !acertouOAmarelo && !errouOAmarelo && distancia <= config.distanciaDoAvisoDeAmarelo
            && Velocidade > 0.2f && Velocidade <= config.velocidadeDevagar)
        {
            acertouOAmarelo = true;
            manager.Acerto(SinaisManager.IdDoAmarelo, config.recadoDoAmarelo, carro.position + Vector3.up * 1.6f);
        }

        ComportamentoDoSemaforo comportamento = ComportamentoDe(Proximo);
        if (comportamento == ComportamentoDoSemaforo.FechaAoChegar && !jaAbriu)
        {
            if (sinal.Cor == CorDoSinal.Verde && distancia <= config.distanciaDoAmarelo)
            {
                sinal.Mudar(CorDoSinal.Amarelo);
            }
            else if (sinal.Cor == CorDoSinal.Amarelo && sinal.TempoNaCor >= config.tempoDoAmarelo)
            {
                sinal.Mudar(CorDoSinal.Vermelho);
            }
        }
        // Vermelho segurado: abre depois de o carrinho esperar parado e de quem atravessa terminar.
        if (!sinal.Automatico && sinal.Cor == CorDoSinal.Vermelho && ParouNoVermelho)
        {
            tempoParado += Time.deltaTime;
            if (tempoParado >= config.esperaNoVermelho)
            {
                cruzamento.Fechando = true;
                if (cruzamento.Livre)
                {
                    jaAbriu = true;
                    sinal.Mudar(CorDoSinal.Verde);
                }
            }
        }
    }

    // A frente do carrinho chegou à linha: vale a luz acesa agora (ou a de antes, se acabou de trocar).
    private void Julgar(SinaisCruzamento cruzamento)
    {
        SinalDeTransito sinal = cruzamento.sinal;
        // A tolerância só ajuda: entre a luz de agora e a de antes, vale a que exige menos da criança
        // (verde exige menos que amarelo, que exige menos que vermelho).
        CorDoSinal cor = sinal.Cor;
        if (sinal.TempoNaCor < config.tolerancia && (int)sinal.CorAnterior < (int)cor)
        {
            cor = sinal.CorAnterior;
        }
        Vector3 onde = carro.position + Vector3.up * 1.6f;

        if (cor == CorDoSinal.Vermelho)
        {
            manager.Erro(string.Format(config.balaoDoVermelho, config.trechoDoVermelho), onde);
            StartCoroutine(Recuar(cruzamento));
            return;
        }
        if (cor == CorDoSinal.Amarelo && Velocidade > config.velocidadeDevagar)
        {
            manager.Erro(string.Format(config.balaoDoAmarelo, config.trechoDoAmarelo), onde);
            errouOAmarelo = true;
            // Dá tempo de tentar de novo: o amarelo recomeça a contar, mas continua indo pro vermelho.
            if (!sinal.Automatico)
            {
                sinal.RecontarACor();
            }
            StartCoroutine(Recuar(cruzamento));
            return;
        }

        if (cor == CorDoSinal.Amarelo)
        {
            if (!acertouOAmarelo)
            {
                manager.Acerto(SinaisManager.IdDoAmarelo, config.recadoDoAmarelo, onde);
            }
        }
        else if (ParouNoVermelho)
        {
            manager.Acerto(SinaisManager.IdDoVermelho, config.recadoDoVermelho, onde);
        }
        else
        {
            manager.Acerto(SinaisManager.IdDoVerde, config.recadoDoVerde, onde);
        }
        ParouNoVermelho = false;
        tempoParado = 0f;
        avisouDoLugar = false;
        jaAbriu = false;
        avisouQueEstaLonge = false;
        acertouOAmarelo = false;
        errouOAmarelo = false;
        Proximo++;
    }

    // "Cof, cof": o carrinho treme, solta fumaça e volta um pouco pra tentar de novo.
    private IEnumerator Recuar(SinaisCruzamento cruzamento)
    {
        Recuando = true;
        Velocidade = 0f;
        ParouNoVermelho = false;
        tempoParado = 0f;
        manager.Tocar(config.somDaTosse);
        float de = S;
        float ate = cruzamento.sDaLinha - config.frenteDoCarro - config.recuo;
        const float duracao = 0.9f;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = t / duracao;
            S = Mathf.Lerp(de, ate, k * k * (3f - 2f * k));
            ColocarCarro();
            carro.position += carro.right * (Mathf.Sin(k * Mathf.PI * 10f) * 0.12f * (1f - k));
            carro.rotation *= Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 8f) * 4f * (1f - k));
            yield return null;
        }
        S = ate;
        ColocarCarro();
        Recuando = false;
    }

    // ------------------------------------------------------------------ rota

    public Vector3 Posicao(float s)
    {
        float i = Mathf.Clamp(s / passoDaRota, 0f, rota.Length - 1.001f);
        int a = Mathf.FloorToInt(i);
        return Vector3.Lerp(rota[a], rota[a + 1], i - a);
    }

    public Vector3 Direcao(float s)
    {
        Vector3 d = Posicao(s + 1f) - Posicao(s - 1f);
        d.y = 0f;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
    }

    private void ColocarCarro()
    {
        carro.position = Posicao(S);
        carro.rotation = Quaternion.LookRotation(Direcao(S));
    }

    // ------------------------------------------------------------------ câmera e tela

    private void LateUpdate()
    {
        if (manager != null && !manager.EscolhendoNivel && rodando)
        {
            // Nas curvas a câmera vira aos poucos, olhando um pouco adiante na rota.
            olharDaCamera = Vector3.Slerp(olharDaCamera, Direcao(S + 3f), 1f - Mathf.Exp(-config.giroDaCamera * Time.deltaTime));
            PosicionarCamera();
        }
    }

    private void PosicionarCamera()
    {
        Quaternion giro = Quaternion.LookRotation(olharDaCamera);
        Vector3 base_ = Posicao(S);
        base_.y = 0f;
        manager.cameraDoJogo.transform.position = base_ + giro * config.cameraAtrasDoCarro;
        manager.cameraDoJogo.transform.LookAt(base_ + giro * config.focoNaFrenteDoCarro);
    }

    private void AtualizarTela()
    {
        SinaisHUD hud = manager.hud;
        SinaisCruzamento cruzamento = ProximoCruzamento;
        bool amarelo = cruzamento != null && cruzamento.sinal.Cor == CorDoSinal.Amarelo;
        hud.Velocidade(Velocidade / config.velocidadeMaxima, config.velocidadeDevagar / config.velocidadeMaxima, amarelo, Velocidade <= config.velocidadeDevagar);
        hud.LuzDoProximo(cruzamento != null ? cruzamento.sinal.Cor : CorDoSinal.Verde, cruzamento != null);
        if (!manager.DicaVisivel)
        {
            return;
        }
        if (cruzamento == null)
        {
            hud.Dica(null);
            return;
        }
        CorDoSinal cor = cruzamento.sinal.Cor;
        hud.Dica(cor == CorDoSinal.Verde ? string.Format(config.dicaDoVerde, config.trechoDoVerde)
            : cor == CorDoSinal.Amarelo ? string.Format(config.dicaDoAmarelo, config.trechoDoAmarelo)
            : string.Format(config.dicaDoVermelho, config.trechoDoVermelho));
    }
}
