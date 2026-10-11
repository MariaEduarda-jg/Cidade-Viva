using System;
using System.Collections.Generic;
using UnityEngine;

// A pista do "Corre, Correndo!". O jogador fica parado em z = 0 e o mundo vem na direção dele:
//   - os trechos de cenário (rua, casas, árvores, postes) rolam e voltam pro fim da fila;
//   - obstáculos, moedas e escudos são criados lá na frente, em fileiras, e vêm junto.
// Toda fileira deixa pelo menos um jeito de passar: uma faixa livre, ou um obstáculo que dá pra
// pular ou deslizar por baixo.
public class CorrePista : MonoBehaviour
{
    [Serializable]
    public class Modelo
    {
        // Mesmo id do TipoDeObstaculo na config, ou "moeda", "escudo", "chegada".
        public string id;
        public GameObject objeto;
    }

    public const string RegraDuasFaixas = "duas_faixas";
    public const string RegraFileiraCheia = "fileira_cheia";
    public const string RegraSequencias = "sequencias";
    public const string RegraCarrosEmMovimento = "carros_em_movimento";

    [Header("Cenário que se repete")]
    public Transform[] trechos;
    public float comprimentoDoTrecho = 24f;

    [Header("Modelos inativos, copiados em tempo de jogo")]
    public Modelo[] modelos;
    public Transform paiDosObjetos;

    public readonly List<CorreObjeto> objetos = new List<CorreObjeto>();

    public float Distancia { get; private set; }
    public float Velocidade { get; private set; }
    public float Meta { get; private set; }
    public int MoedasCriadas { get; private set; }
    public bool Rodando { get; private set; }
    // Avisa o gerente quando um carro em movimento começa a trocar de faixa (pra buzinar).
    public Action<CorreObjeto> aoCarroTrocarDeFaixa;

    private CorreConfig config;
    private NivelConfig nivel;
    private float velocidadeAlvo;
    private float espaco;
    private float proximaFileira;      // em "metros de pista" (distância percorrida + z)
    private float proximoEscudo;
    private int faixaLivreAnterior;
    private bool segundaDaSequencia;
    private int fileiras;
    private bool chegadaCriada;
    private Func<int> escudosDoJogador;
    private readonly List<TipoDeObstaculo> permitidos = new List<TipoDeObstaculo>();

    public void Preparar(CorreConfig config, NivelConfig nivel, float meta, Func<int> escudosDoJogador)
    {
        this.config = config;
        this.nivel = nivel;
        this.escudosDoJogador = escudosDoJogador;
        Meta = meta;
        velocidadeAlvo = config.velocidadeBase * (nivel.velocidade > 0f ? nivel.velocidade : 1f);
        espaco = nivel.Extra("espacoEntreObstaculos", config.espacoPadrao);
        proximaFileira = config.distanciaLivreNoInicio;
        proximoEscudo = config.distanciaEntreEscudos * 0.6f;

        permitidos.Clear();
        foreach (TipoDeObstaculo t in config.tipos)
        {
            if (t.id != CorreConfig.IdCarroEmMovimento && (string.IsNullOrEmpty(t.regra) || nivel.TemRegra(t.regra)))
            {
                permitidos.Add(t);
            }
        }
    }

    public void Comecar()
    {
        Rodando = true;
    }

    public void Parar()
    {
        Rodando = false;
        Velocidade = 0f;
    }

    // Tropeço: perde o embalo por um instante e volta a acelerar sozinho.
    public void Frear(float fator)
    {
        Velocidade *= fator;
    }

    private void Update()
    {
        if (!Rodando || config == null)
        {
            return;
        }

        // Arranca devagar, pra corrida não começar no susto.
        Velocidade = Mathf.MoveTowards(Velocidade, velocidadeAlvo, velocidadeAlvo * 0.9f * Time.deltaTime);
        float passo = Velocidade * Time.deltaTime;
        Distancia += passo;

        RolarCenario(passo);
        CriarOQueFalta();
        MoverObjetos(passo);
    }

    private void RolarCenario(float passo)
    {
        float total = comprimentoDoTrecho * trechos.Length;
        foreach (Transform t in trechos)
        {
            Vector3 p = t.position;
            p.z -= passo;
            if (p.z < -comprimentoDoTrecho * 1.5f)
            {
                p.z += total;
            }
            t.position = p;
        }
    }

    // ------------------------------------------------------------------ criação

    private void CriarOQueFalta()
    {
        float horizonte = Distancia + config.distanciaDeSurgimento;
        while (proximaFileira <= horizonte && proximaFileira < Meta - 18f)
        {
            float proximoEspaco = CriarFileira(proximaFileira);
            proximaFileira += proximoEspaco;
        }

        if (!chegadaCriada && Meta <= horizonte)
        {
            chegadaCriada = true;
            Criar("chegada", CorreObjeto.Categoria.Chegada, null, 0, Meta);
        }
    }

    private float XDaFaixa(int faixa)
    {
        return faixa * config.larguraDaFaixa;
    }

    // Cria a fileira na posição "p" da pista e devolve a distância até a próxima.
    private float CriarFileira(float p)
    {
        fileiras++;
        bool curta = segundaDaSequencia;
        segundaDaSequencia = false;

        // A faixa livre desta fileira. Quando a fileira vem logo depois da outra, fica perto da anterior.
        int livre = curta ? Mathf.Clamp(faixaLivreAnterior + UnityEngine.Random.Range(-1, 2), -1, 1) : UnityEngine.Random.Range(-1, 2);

        if (nivel.TemRegra(RegraCarrosEmMovimento) && !curta && fileiras % 4 == 0 && config.Tipo(CorreConfig.IdCarroEmMovimento) != null)
        {
            float adiantado = CriarCarroEmMovimento(p, livre);
            faixaLivreAnterior = livre;
            // O carro anda sobre a pista: a próxima fileira fica depois de onde ele aparece.
            return Mathf.Max(espaco * 1.15f, adiantado + 6f);
        }

        var bloqueadas = new List<int>();
        for (int f = -1; f <= 1; f++)
        {
            if (f != livre)
            {
                bloqueadas.Add(f);
            }
        }

        bool cheia = nivel.TemRegra(RegraFileiraCheia) && !curta && UnityEngine.Random.value < 0.3f;
        bool duas = cheia || (nivel.TemRegra(RegraDuasFaixas) && UnityEngine.Random.value < 0.55f);
        if (!duas)
        {
            bloqueadas.RemoveAt(UnityEngine.Random.Range(0, bloqueadas.Count));
        }

        TipoDeObstaculo noLivre = null;
        foreach (int f in bloqueadas)
        {
            Criar(Sortear(null).id, CorreObjeto.Categoria.Obstaculo, null, XDaFaixa(f), p);
        }
        if (cheia)
        {
            // As três faixas ocupadas: na "livre" vai um obstáculo que dá pra pular ou deslizar.
            noLivre = Sortear(t => t.pulavel || t.deslizavel);
            if (noLivre != null)
            {
                Criar(noLivre.id, CorreObjeto.Categoria.Obstaculo, null, XDaFaixa(livre), p);
            }
        }

        bool sequencia = nivel.TemRegra(RegraSequencias) && !curta && UnityEngine.Random.value < 0.4f;
        float proximoEspaco = sequencia ? config.espacoDaSequencia : espaco;
        segundaDaSequencia = sequencia;

        // Recompensas, sempre onde dá pra pegar sem bater.
        if (noLivre != null && noLivre.pulavel && UnityEngine.Random.value < config.chanceDeMoedasNoPulo * 2f)
        {
            for (int i = -1; i <= 1; i++)
            {
                Criar("moeda", CorreObjeto.Categoria.Moeda, null, XDaFaixa(livre), p + i * 1.4f, true);
            }
        }
        else if (!sequencia && noLivre == null)
        {
            float inicio = p + 3.5f;
            if (escudosDoJogador() < config.escudosMaximos && p >= proximoEscudo)
            {
                proximoEscudo = p + config.distanciaEntreEscudos;
                Criar("escudo", CorreObjeto.Categoria.Escudo, null, XDaFaixa(livre), p + proximoEspaco * 0.5f);
            }
            else if (UnityEngine.Random.value < config.chanceDeFilaDeMoedas)
            {
                int quantas = Mathf.Min(config.moedasPorFila, Mathf.FloorToInt((proximoEspaco - 6f) / config.espacoEntreMoedas));
                for (int i = 0; i < quantas; i++)
                {
                    Criar("moeda", CorreObjeto.Categoria.Moeda, null, XDaFaixa(livre), inicio + i * config.espacoEntreMoedas);
                }
            }
        }

        faixaLivreAnterior = livre;
        return proximoEspaco;
    }

    // Carro que vem no sentido contrário pela faixa do meio e atravessa pra uma das faixas do lado.
    // A faixa "livre" nunca é o destino. Devolve quantos metros de pista ele aparece além de "p"
    // (ele vem mais rápido que a pista, então nasce mais longe pra chegar ao jogador em "p").
    private float CriarCarroEmMovimento(float p, int livre)
    {
        int destino = livre == 0 ? (UnityEngine.Random.value < 0.5f ? -1 : 1) : -livre;

        TipoDeObstaculo tipo = config.Tipo(CorreConfig.IdCarroEmMovimento);
        CorreObjeto carro = Criar(tipo.id, CorreObjeto.Categoria.Obstaculo, tipo, 0f, p);
        if (carro == null)
        {
            return 0f;
        }
        float zNormal = carro.z;
        carro.z = zNormal * (velocidadeAlvo + config.velocidadeDoCarroEmMovimento) / velocidadeAlvo;
        carro.velocidadePropria = config.velocidadeDoCarroEmMovimento;
        carro.xDeDestino = XDaFaixa(destino);
        carro.trocaComecaEmZ = 38f;
        Posicionar(carro);
        return carro.z - zNormal;
    }

    private TipoDeObstaculo Sortear(Predicate<TipoDeObstaculo> filtro)
    {
        float total = 0f;
        foreach (TipoDeObstaculo t in permitidos)
        {
            if (filtro == null || filtro(t))
            {
                total += Mathf.Max(0.01f, t.peso);
            }
        }
        if (total <= 0f)
        {
            return filtro == null ? null : Sortear(null);
        }

        float sorteio = UnityEngine.Random.value * total;
        foreach (TipoDeObstaculo t in permitidos)
        {
            if (filtro != null && !filtro(t))
            {
                continue;
            }
            sorteio -= Mathf.Max(0.01f, t.peso);
            if (sorteio <= 0f)
            {
                return t;
            }
        }
        return permitidos[permitidos.Count - 1];
    }

    private CorreObjeto Criar(string id, CorreObjeto.Categoria categoria, TipoDeObstaculo tipo, float x, float posicaoNaPista, bool noAlto = false)
    {
        GameObject modelo = null;
        foreach (Modelo m in modelos)
        {
            if (m.id == id)
            {
                modelo = m.objeto;
                break;
            }
        }
        if (modelo == null)
        {
            Debug.LogWarning("Corre, Correndo!: não há modelo na cena para '" + id + "'.");
            return null;
        }

        GameObject go = Instantiate(modelo, paiDosObjetos);
        var o = go.AddComponent<CorreObjeto>();
        o.categoria = categoria;
        o.tipo = tipo ?? (categoria == CorreObjeto.Categoria.Obstaculo ? config.Tipo(id) : null);
        o.x = x;
        o.xDeDestino = x;
        o.z = posicaoNaPista - Distancia;
        o.noAlto = noAlto;
        Posicionar(o);
        go.SetActive(true);
        objetos.Add(o);
        if (categoria == CorreObjeto.Categoria.Moeda)
        {
            MoedasCriadas++;
        }
        return o;
    }

    // ------------------------------------------------------------------ movimento

    private void MoverObjetos(float passo)
    {
        for (int i = objetos.Count - 1; i >= 0; i--)
        {
            CorreObjeto o = objetos[i];
            o.z -= passo + o.velocidadePropria * Time.deltaTime;

            if (o.velocidadePropria > 0f && o.z < o.trocaComecaEmZ && !Mathf.Approximately(o.x, o.xDeDestino))
            {
                if (!o.trocando)
                {
                    o.trocando = true;
                    if (aoCarroTrocarDeFaixa != null)
                    {
                        aoCarroTrocarDeFaixa(o);
                    }
                }
                o.x = Mathf.MoveTowards(o.x, o.xDeDestino, config.larguraDaFaixa / 0.9f * Time.deltaTime);
            }

            Posicionar(o);
            if (o.categoria == CorreObjeto.Categoria.Moeda || o.categoria == CorreObjeto.Categoria.Escudo)
            {
                o.transform.Rotate(0f, 200f * Time.deltaTime, 0f, Space.World);
            }

            if (o.z < -14f)
            {
                Destroy(o.gameObject);
                objetos.RemoveAt(i);
            }
        }
    }

    private static void Posicionar(CorreObjeto o)
    {
        Vector3 p = o.transform.position;
        o.transform.position = new Vector3(o.x, o.noAlto ? 1.7f : p.y, o.z);
    }

    public void Remover(CorreObjeto o)
    {
        objetos.Remove(o);
        Destroy(o.gameObject);
    }
}
