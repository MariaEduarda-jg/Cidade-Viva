using System;
using UnityEngine;

// Pista pequena do lado de fora da oficina, onde o veículo recém-montado dá uma volta de teste.
// A criança dirige com as setas/WASD (ou os botões da tela) e pega 3 moedas. Não dá pra errar:
// o veículo não sai da pista e, se o tempo máximo passar, o teste termina do mesmo jeito.
//
// Dois casos especiais, que vêm da definição do verbete:
//   - trilhos (bonde): o veículo anda só pra frente e pra trás em cima dos trilhos, sem volante;
//   - reboque: ele não tem motor, então fica parado até a criança engatar o veículo que puxa
//     (Engatar). Depois disso ela dirige esse veículo, e o reboque vai atrás.
public class OficinaPista : MonoBehaviour
{
    public Transform partida;
    public GameObject[] moedas;
    [Tooltip("Metade do tamanho da área onde o veículo pode andar, a partir deste objeto.")]
    public Vector2 meiaArea = new Vector2(9f, 5f);

    [Header("Trilhos (bonde)")]
    // Trilhos no chão, ligados só quando o veículo anda em trilhos.
    public GameObject trilhos;
    public Transform partidaNosTrilhos;

    public bool Ativa { get; private set; }
    public int MoedasPegas { get; private set; }
    public float Velocidade { get; private set; }
    public bool EmTrilhos { get; private set; }
    // true enquanto o reboque espera ser engatado.
    public bool EsperandoEngate { get; private set; }

    // O veículo montado na bancada.
    public ChassiDaOficina Veiculo
    {
        get { return veiculo; }
    }

    // Quem a criança dirige: o próprio veículo ou, no caso do reboque, o veículo que puxa.
    public Transform Guiado { get; private set; }

    // Disparado a cada moeda pega (posição no mundo), pro manager tocar som e soltar partículas.
    public event Action<Vector3> PegouMoeda;
    public event Action Engatou;

    private ChassiDaOficina veiculo;
    private Transform puxador;
    private OficinaConfig config;
    private Action aoTerminar;
    private Vector3[] moedasNoLugar;
    private float direcaoDosBotoes;
    private bool aceleradorDosBotoes;
    private float fatorDeVelocidade = 1f;
    private float distanciaDoReboque;
    private float tempo;
    private float esperaFinal = -1f;
    private float engatando = -1f;
    private Vector3 puxadorDeOnde;

    // "novoPuxador" (opcional): veículo com motor que vai puxar o montado, já no tamanho da pista e com a frente em +Z.
    public void Comecar(ChassiDaOficina novoVeiculo, OficinaConfig novaConfig, bool emTrilhos, float velocidade, Transform novoPuxador,
        float comprimentoDoVeiculo, float comprimentoDoPuxador, Action quandoTerminar)
    {
        veiculo = novoVeiculo;
        config = novaConfig;
        aoTerminar = quandoTerminar;
        EmTrilhos = emTrilhos && trilhos != null && partidaNosTrilhos != null;
        fatorDeVelocidade = Mathf.Clamp(velocidade, 0.3f, 2f);
        puxador = novoPuxador;
        MoedasPegas = 0;
        Velocidade = 0f;
        tempo = 0f;
        esperaFinal = -1f;
        engatando = -1f;
        direcaoDosBotoes = 0f;
        aceleradorDosBotoes = false;

        if (moedasNoLugar == null)
        {
            moedasNoLugar = new Vector3[moedas.Length];
            for (int i = 0; i < moedas.Length; i++)
            {
                moedasNoLugar[i] = moedas[i].transform.localPosition;
            }
        }

        Transform largada = EmTrilhos ? partidaNosTrilhos : partida;
        if (trilhos != null)
        {
            trilhos.SetActive(EmTrilhos);
        }
        veiculo.transform.SetParent(transform, true);
        veiculo.transform.SetPositionAndRotation(largada.position, largada.rotation);
        veiculo.transform.localScale = Vector3.one * config.escalaDoVeiculoNaPista;
        for (int i = 0; i < moedas.Length; i++)
        {
            // Nos trilhos, as moedas ficam em cima deles.
            Vector3 p = moedasNoLugar[i];
            moedas[i].transform.localPosition = EmTrilhos ? new Vector3(p.x, p.y, partidaNosTrilhos.localPosition.z) : p;
            moedas[i].SetActive(true);
        }

        Guiado = veiculo.transform;
        EsperandoEngate = puxador != null;
        if (puxador != null)
        {
            // O veículo que puxa espera um pouco à frente, de costas pro reboque.
            distanciaDoReboque = (comprimentoDoVeiculo + comprimentoDoPuxador) * 0.5f + 0.25f;
            puxador.SetParent(transform, true);
            puxador.SetPositionAndRotation(largada.position + largada.forward * (distanciaDoReboque + 2.6f), largada.rotation);
        }
        Ativa = true;
    }

    // O veículo que puxa dá ré até o reboque e engata. Chamado pelo botão "Engatar".
    public void Engatar()
    {
        if (Ativa && EsperandoEngate && engatando < 0f)
        {
            engatando = 0f;
            puxadorDeOnde = puxador.position;
        }
    }

    // Botões da tela (segurar): -1 esquerda, +1 direita, 0 solto.
    public void DefinirDirecao(float direcao)
    {
        direcaoDosBotoes = Mathf.Clamp(direcao, -1f, 1f);
    }

    public void Acelerar(bool segurando)
    {
        aceleradorDosBotoes = segurando;
    }

    private void Update()
    {
        if (!Ativa)
        {
            return;
        }

        if (EsperandoEngate)
        {
            if (engatando >= 0f)
            {
                engatando += Time.deltaTime / 1.1f;
                float suave = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(engatando));
                Vector3 encostado = veiculo.transform.position + veiculo.transform.forward * distanciaDoReboque;
                puxador.position = Vector3.Lerp(puxadorDeOnde, encostado, suave);
                if (engatando >= 1f)
                {
                    EsperandoEngate = false;
                    Guiado = puxador;
                    if (Engatou != null)
                    {
                        Engatou();
                    }
                }
            }
            return;
        }

        if (esperaFinal >= 0f)
        {
            esperaFinal -= Time.deltaTime;
            Velocidade = Mathf.MoveTowards(Velocidade, 0f, config.aceleracao * 1.5f * Time.deltaTime);
            Andar(0f);
            if (esperaFinal < 0f)
            {
                Terminar();
            }
            return;
        }

        float direcao = EmTrilhos ? 0f : Mathf.Clamp(Input.GetAxisRaw("Horizontal") + direcaoDosBotoes, -1f, 1f);
        float pedal = Input.GetAxisRaw("Vertical");
        bool acelerando = aceleradorDosBotoes || pedal > 0.1f;
        float maxima = config.velocidadeMaxima * fatorDeVelocidade;
        float alvo = acelerando ? maxima : (pedal < -0.1f ? -maxima * 0.4f : 0f);
        Velocidade = Mathf.MoveTowards(Velocidade, alvo, config.aceleracao * Time.deltaTime);
        Andar(direcao);

        for (int i = 0; i < moedas.Length; i++)
        {
            if (!moedas[i].activeSelf)
            {
                continue;
            }
            Vector3 d = moedas[i].transform.position - Guiado.position;
            d.y = 0f;
            if (d.magnitude <= config.distanciaPraPegarMoeda)
            {
                moedas[i].SetActive(false);
                MoedasPegas++;
                if (PegouMoeda != null)
                {
                    PegouMoeda(moedas[i].transform.position);
                }
            }
        }

        tempo += Time.deltaTime;
        if (MoedasPegas >= moedas.Length || tempo >= config.tempoMaximoDoTeste)
        {
            esperaFinal = 0.9f;
        }
    }

    private void Andar(float direcao)
    {
        Transform t = Guiado;
        // Só vira andando, como um veículo de verdade; mas já vira bem mesmo devagar, pra criança
        // conseguir corrigir a direção sem precisar pegar velocidade.
        float embalo = Mathf.Abs(Velocidade) / Mathf.Max(0.1f, config.velocidadeMaxima);
        float fator = embalo < 0.04f ? 0f : Mathf.Sign(Velocidade) * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(embalo));
        t.Rotate(0f, direcao * config.giroEmGrausPorSegundo * fator * Time.deltaTime, 0f, Space.World);
        float passo = Velocidade * Time.deltaTime;
        Vector3 pos = t.position + t.forward * passo;
        Vector3 centro = transform.position;
        pos.x = Mathf.Clamp(pos.x, centro.x - meiaArea.x, centro.x + meiaArea.x);
        pos.z = Mathf.Clamp(pos.z, centro.z - meiaArea.y, centro.z + meiaArea.y);
        t.position = pos;
        veiculo.Girar(passo / Mathf.Max(0.1f, config.escalaDoVeiculoNaPista));

        // O reboque engatado vai atrás de quem puxa, preso nele.
        if (puxador != null && !EsperandoEngate)
        {
            veiculo.transform.SetPositionAndRotation(puxador.position - puxador.forward * distanciaDoReboque, puxador.rotation);
        }
    }

    private void Terminar()
    {
        Ativa = false;
        Velocidade = 0f;
        Action avisar = aoTerminar;
        aoTerminar = null;
        if (avisar != null)
        {
            avisar();
        }
    }
}
