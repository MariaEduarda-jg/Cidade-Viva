using System.Collections.Generic;
using UnityEngine;

// Trânsito do "Travessia Segura": cria os veículos em cada pista, faz andar, parar no semáforo
// e manter distância do da frente. Também responde às perguntas do jogo: "bateu?", "vem carro?".
public class TravessiaTrafego : MonoBehaviour
{
    public class Pista
    {
        public float z;
        public float largura;
        // +1 = vai pra +X (vem da esquerda de quem atravessa); -1 = o contrário.
        public int direcao;
        public bool ciclovia;
        public float proximoSurgimento;
        public readonly List<TravessiaVeiculo> veiculos = new List<TravessiaVeiculo>();
    }

    public readonly List<Pista> pistas = new List<Pista>();

    private TravessiaConfig config;
    private TravessiaSemaforo semaforo;
    private GameObject modeloCiclista;
    private float multiplicador = 1f;
    private float intervalo = 3.5f;
    private bool imprevisiveis;
    private bool temAmbulancia;
    private float intervaloDaAmbulancia;
    private float proximaAmbulancia;
    private bool rodando;

    public void Preparar(TravessiaConfig config, TravessiaSemaforo semaforo, GameObject modeloCiclista,
        float multiplicador, float intervalo, bool imprevisiveis, bool temAmbulancia, float intervaloDaAmbulancia)
    {
        this.config = config;
        this.semaforo = semaforo;
        this.modeloCiclista = modeloCiclista;
        this.multiplicador = multiplicador;
        this.intervalo = intervalo;
        this.imprevisiveis = imprevisiveis;
        this.temAmbulancia = temAmbulancia;
        this.intervaloDaAmbulancia = intervaloDaAmbulancia;
        proximaAmbulancia = intervaloDaAmbulancia * 0.6f;

        // Já começa com a rua "viva": um veículo no meio do caminho em cada pista.
        for (int i = 0; i < pistas.Count; i++)
        {
            Pista p = pistas[i];
            Criar(p, -config.distanciaDeSurgimento * Random.Range(0.35f, 0.75f), false);
            p.proximoSurgimento = intervalo * Random.Range(0.3f, 1f);
        }
        rodando = true;
    }

    public void Parar()
    {
        rodando = false;
    }

    private void Update()
    {
        if (!rodando || config == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        foreach (Pista p in pistas)
        {
            p.proximoSurgimento -= dt;
            if (p.proximoSurgimento <= 0f && EntradaLivre(p))
            {
                Criar(p, -config.distanciaDeSurgimento, false);
                // Carros imprevisíveis também chegam em intervalos irregulares.
                p.proximoSurgimento = intervalo * (p.ciclovia ? 1.8f : 1f) * (imprevisiveis ? Random.Range(0.55f, 1.6f) : Random.Range(0.85f, 1.2f));
            }
            Mover(p, dt);
        }

        if (temAmbulancia)
        {
            proximaAmbulancia -= dt;
            if (proximaAmbulancia <= 0f)
            {
                Pista p = PistaDeCarroAleatoria();
                if (p != null && EntradaLivre(p))
                {
                    Criar(p, -config.distanciaDeSurgimento, true);
                    proximaAmbulancia = intervaloDaAmbulancia * Random.Range(0.85f, 1.25f);
                }
            }
        }
    }

    private Pista PistaDeCarroAleatoria()
    {
        var carros = pistas.FindAll(p => !p.ciclovia);
        return carros.Count > 0 ? carros[Random.Range(0, carros.Count)] : null;
    }

    private bool EntradaLivre(Pista p)
    {
        foreach (TravessiaVeiculo v in p.veiculos)
        {
            if (v.Traseira < -config.distanciaDeSurgimento + 9f)
            {
                return false;
            }
        }
        return true;
    }

    private void Criar(Pista p, float s, bool ambulancia)
    {
        GameObject modelo = ambulancia ? config.ambulancia
            : p.ciclovia ? modeloCiclista
            : config.carros[Random.Range(0, config.carros.Length)];
        if (modelo == null)
        {
            return;
        }

        GameObject go = Instantiate(modelo, transform);
        go.SetActive(true);
        // Os prefabs de veículo têm a frente em +Z.
        go.transform.rotation = Quaternion.Euler(0f, p.direcao > 0 ? 90f : -90f, 0f);

        var v = go.AddComponent<TravessiaVeiculo>();
        v.s = s;
        v.ambulancia = ambulancia;
        v.ciclista = p.ciclovia && !ambulancia;
        v.velocidadeAlvo = ambulancia ? config.velocidadeDaAmbulancia
            : p.ciclovia ? config.velocidadeDoCiclista * Mathf.Lerp(1f, multiplicador, 0.5f)
            : config.velocidadeBase * multiplicador * Random.Range(0.9f, 1.1f);
        v.velocidade = v.velocidadeAlvo;

        // Comprimento real do modelo, pra batida e pra distância entre veículos.
        Renderer[] rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            foreach (Renderer r in rends)
            {
                b.Encapsulate(r.bounds);
            }
            v.meioComprimento = Mathf.Max(0.5f, b.extents.x);
        }

        if (ambulancia)
        {
            if (config.sirene != null)
            {
                var som = go.AddComponent<AudioSource>();
                som.clip = config.sirene;
                som.loop = true;
                som.volume = 0.45f;
                som.spatialBlend = 0f;
                som.Play();
            }
            var luzGo = new GameObject("Luz_Da_Sirene");
            luzGo.transform.SetParent(go.transform, false);
            luzGo.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            v.sirene = luzGo.AddComponent<Light>();
            v.sirene.type = LightType.Point;
            v.sirene.range = 9f;
            v.sirene.intensity = 3f;
        }

        p.veiculos.Add(v);
        Posicionar(p, v);
    }

    private void Mover(Pista p, float dt)
    {
        // Do mais adiantado pro mais atrasado, pra cada um olhar o da frente.
        p.veiculos.Sort((a, b) => b.s.CompareTo(a.s));
        bool parar = semaforo != null && semaforo.CarrosDevemParar;
        float a = config.aceleracao;

        for (int i = 0; i < p.veiculos.Count; i++)
        {
            TravessiaVeiculo v = p.veiculos[i];

            if (imprevisiveis && !v.ambulancia && !p.ciclovia && Time.time >= v.proximaMudanca)
            {
                v.proximaMudanca = Time.time + Random.Range(0.8f, 2f);
                v.velocidadeAlvo = config.velocidadeBase * multiplicador * Random.Range(0.55f, 1.45f);
            }

            float maxima = v.velocidadeAlvo;

            // Semáforo: para na linha, se ainda dá tempo de frear. Ambulância não para.
            if (parar && !v.ambulancia)
            {
                float ateALinha = -config.linhaDeParada - v.Frente;
                bool daPraFrear = ateALinha >= v.velocidade * v.velocidade / (2f * a) - 0.6f;
                if (ateALinha >= -0.4f && (daPraFrear || v.velocidade < 0.5f))
                {
                    maxima = Mathf.Min(maxima, Mathf.Sqrt(2f * a * Mathf.Max(0f, ateALinha - 0.15f)));
                }
            }

            // Distância do veículo da frente.
            if (i > 0)
            {
                float folga = p.veiculos[i - 1].Traseira - v.Frente - 1.6f;
                maxima = Mathf.Min(maxima, Mathf.Sqrt(2f * a * Mathf.Max(0f, folga)));
            }

            v.velocidade = v.velocidade > maxima ? maxima : Mathf.MoveTowards(v.velocidade, maxima, a * 0.6f * dt);
            v.s += v.velocidade * dt;
            Posicionar(p, v);

            if (v.sirene != null)
            {
                v.sirene.color = Mathf.FloorToInt(Time.time * 5f) % 2 == 0 ? new Color(1f, 0.25f, 0.2f) : new Color(0.3f, 0.5f, 1f);
            }
        }

        for (int i = p.veiculos.Count - 1; i >= 0; i--)
        {
            if (p.veiculos[i].Traseira > config.distanciaDeSurgimento)
            {
                Destroy(p.veiculos[i].gameObject);
                p.veiculos.RemoveAt(i);
            }
        }
    }

    private static void Posicionar(Pista p, TravessiaVeiculo v)
    {
        v.transform.position = new Vector3(p.direcao * v.s, 0.02f, p.z);
    }

    // ------------------------------------------------------------------ perguntas do jogo

    // Veículo encostando no jogador (posição no mundo), ou null.
    public TravessiaVeiculo Atingiu(Vector3 jogador, float raio)
    {
        foreach (Pista p in pistas)
        {
            if (Mathf.Abs(jogador.z - p.z) > p.largura * 0.5f)
            {
                continue;
            }
            foreach (TravessiaVeiculo v in p.veiculos)
            {
                if (Mathf.Abs(p.direcao * v.s - jogador.x) < v.meioComprimento + raio)
                {
                    return v;
                }
            }
        }
        return null;
    }

    // Em quantos segundos o próximo veículo desta pista chega na faixa de pedestre.
    // Infinito = ninguém vindo (ou todo mundo parado no semáforo).
    public float TempoAteAFaixa(int pista)
    {
        Pista p = pistas[pista];
        bool parar = semaforo != null && semaforo.CarrosDevemParar;
        float menor = float.PositiveInfinity;
        foreach (TravessiaVeiculo v in p.veiculos)
        {
            if (v.Traseira > config.meiaLarguraDaFaixa)
            {
                continue; // já passou
            }
            float ateAFaixa = -config.meiaLarguraDaFaixa - v.Frente;
            if (ateAFaixa <= 0f)
            {
                return 0f; // está em cima da faixa
            }
            if (parar && !v.ambulancia && v.Frente <= -config.linhaDeParada + 0.5f && v.velocidade < v.velocidadeAlvo * 0.6f)
            {
                continue; // freando ou parado no sinal
            }
            menor = Mathf.Min(menor, ateAFaixa / Mathf.Max(v.velocidade, v.velocidadeAlvo * 0.5f, 0.5f));
        }
        return menor;
    }

    // Tem ambulância que ainda não passou pela faixa.
    public bool AmbulanciaChegando
    {
        get
        {
            foreach (Pista p in pistas)
            {
                foreach (TravessiaVeiculo v in p.veiculos)
                {
                    if (v.ambulancia && v.Traseira < config.meiaLarguraDaFaixa)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
