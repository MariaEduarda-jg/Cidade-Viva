using System;
using System.Collections.Generic;
using UnityEngine;

// Pra que serve uma estrutura, segundo a definição do verbete dela: que obstáculos ela vence e quem
// passa por ela. Os dois trechos são copiados da definição, letra por letra; são eles que aparecem
// no balão quando a criança escolhe a estrutura errada.
[Serializable]
public class RegraDaEstrutura
{
    public string idDoVerbete;
    public ObstaculoDaObra[] obstaculos = new ObstaculoDaObra[0];
    [Tooltip("Trecho da definição que diz o que a estrutura atravessa.")]
    public string trechoDoObstaculo;
    public QuemPassaNaObra[] quemPassa = new QuemPassaNaObra[0];
    [Tooltip("Trecho da definição que diz quem passa por ela.")]
    public string trechoDeQuemPassa;
    [Tooltip("Quando a definição não diz quem passa e isso vem de outro verbete (via rural e via urbana são \"Via...\"): o id desse verbete. Vazio = o próprio.")]
    public string verbeteDoTrechoDeQuem;

    public bool VenceObstaculo(ObstaculoDaObra obstaculo)
    {
        return Array.IndexOf(obstaculos, obstaculo) >= 0;
    }

    public bool DeixaPassar(QuemPassaNaObra quem)
    {
        return Array.IndexOf(quemPassa, quem) >= 0;
    }

    public bool Serve(ObstaculoDaObra obstaculo, QuemPassaNaObra quem)
    {
        return VenceObstaculo(obstaculo) && DeixaPassar(quem);
    }
}

// Tudo que vale pra todos os níveis das Obras da Cidade: regras das estruturas, medidas, pontuação,
// textos e sons. As definições dos verbetes NÃO ficam aqui: vêm sempre do VerbeteDatabase.
[CreateAssetMenu(fileName = "Obras_Config", menuName = "Cidade Viva/Obras: config")]
public class ObrasConfig : ScriptableObject
{
    [Header("Estruturas (derivadas das definições)")]
    public List<RegraDaEstrutura> regras = new List<RegraDaEstrutura>();

    [Header("Mapa")]
    [Tooltip("Distância entre dois problemas lado a lado e entre dois trechos em fila, em metros.")]
    public float larguraDoLote = 20f;
    public float comprimentoDoLote = 26f;
    [Tooltip("Onde quem passa espera (z, a partir do meio do lote) e onde chega depois de atravessar.")]
    public float zDaEspera = -12.2f;
    public float zDaParada = -10.6f;
    public float zDaChegada = 12.2f;
    [Tooltip("Planejar a rua: a que distância do meio da rua fica a obra da beira (calçada, ciclofaixa) e quem vai usá-la.")]
    public float xDaBeira = 3.25f;
    [Tooltip("Planejar a rua: em que pista o veículo vem, no problema do retorno.")]
    public float xDaPistaDeIda = 2.9f;

    [Header("Ritmo")]
    public float tempoDaObra = 1.1f;
    public float tempoDeDesmontar = 0.5f;
    public float tempoDoBalao = 4.5f;
    public float velocidadeAPe = 3.2f;
    public float velocidadeDeVeiculo = 6f;
    public float velocidadeDeBicicleta = 4.5f;
    public float pausaAntesDoFim = 1.2f;

    [Header("Câmera")]
    public float distanciaDaCamera = 44f;
    public float inclinacaoDaCamera = 50f;
    public Vector2 limitesDaInclinacao = new Vector2(32f, 65f);
    public float giroMaximoDaCamera = 40f;
    [Tooltip("Graus por pixel arrastado.")]
    public float sensibilidadeDoGiro = 0.2f;
    [Tooltip("Pra onde a câmera olha, a partir do meio do problema atual.")]
    public Vector3 deslocamentoDoFoco = new Vector3(0f, 0f, -4.5f);

    [Header("Pontuação")]
    public int pontosPorProblema = 100;
    public int descontoPorErro = 10;
    public int minimoPorProblema = 40;

    [Header("Textos")]
    public string textoDoNivel = "Nível {0}";
    public string textoDoContador = "Obra {0} de {1}";
    [Tooltip("{0} = quem passa (\"O carro\"), {1} = obstáculo (\"o rio\").")]
    public string enunciado = "{0} precisa atravessar {1}.";
    public string nomeDoRio = "o rio";
    public string nomeDoMorro = "o morro";
    public string nomeDaAvenida = "a avenida";
    public string nomeDaRua = "a rua";
    public string recadoDeEscolher = "Toque numa estrutura e leia a definição.";
    public string recadoDeConstruir = "Agora toque no lugar que está brilhando!";
    public string recadoSemEstrutura = "Primeiro escolha uma estrutura!";
    public string recadoDaObra = "Construindo...";
    [Tooltip("{0} = quem passa.")]
    public string recadoDeAcerto = "Isso! {0} atravessou.";
    public string recadoDaAmbulanciaAntes = "Primeiro ajude a ambulância no trecho de antes!";
    public string recadoDoHospital = "A ambulância chegou ao hospital!";
    public string recadoDeTempoEsgotado = "O tempo acabou, mas pode terminar com calma.";
    [Tooltip("{0} = palavra do verbete, {1} = trecho da definição, {2} = o que há aqui, em frase inteira (\"Aqui é um rio!\").")]
    public string balaoDeObstaculoErrado = "A definição de {0} diz: \"{1}\".\n{2}";
    [Tooltip("{0} = palavra do verbete, {1} = trecho da definição, {2} = quem passa (\"um animal\").")]
    public string balaoDeQuemErrado = "A definição de {0} diz: \"{1}\".\nQuem passa aqui é {2}!";
    [Tooltip("O que há no lugar, em frase inteira (entra no fim do balão).")]
    public string aquiRio = "Aqui é um rio!";
    public string aquiMorro = "Aqui é um morro!";
    public string aquiVia = "Aqui é uma via!";
    public string aquiCampoECidade = "Aqui o caminho liga a fazenda à cidade!";
    public string aquiDentroDaCidade = "Aqui é dentro da cidade!";
    public string aquiLugarQualquer = "Aqui não é cidade nem fazenda!";
    public string aquiBeiraDaRua = "Aqui é a beira de uma rua com carros!";
    public string aquiSeparadoDoTransito = "Aqui o caminho fica longe dos carros!";
    public string aquiMudarDeSentido = "Aqui o veículo precisa voltar!";
    public string aquiGuardarVeiculos = "Aqui é preciso guardar os veículos!";
    public string umVeiculo = "um veículo";
    public string umPedestre = "um pedestre";
    public string umAnimal = "um animal";
    public string umCiclista = "um ciclista";
    [Tooltip("{0} = trecho da definição da estrutura certa.")]
    public string dica = "Procure a definição que diz: \"{0}\"";
    public string[] rotulosDosCriterios = { "sem erros", "no tempo", "sem dica" };

    [Header("Sons")]
    public AudioClip somDeEscolher;
    public AudioClip somDeObra;
    public AudioClip somDeAcerto;
    public AudioClip somDeErro;
    public AudioClip somDeComemoracao;
    public AudioClip somDeDica;
    public AudioClip somDeBloqueio;
    public AudioClip somDaSirene;

    public RegraDaEstrutura Regra(string idDoVerbete)
    {
        foreach (RegraDaEstrutura r in regras)
        {
            if (r.idDoVerbete == idDoVerbete)
            {
                return r;
            }
        }
        return null;
    }

    public string NomeDoObstaculo(ObraProblema p)
    {
        switch (p.obstaculo)
        {
            case ObstaculoDaObra.Rio: return nomeDoRio;
            case ObstaculoDaObra.Morro: return nomeDoMorro;
            default: return p.ruaComum ? nomeDaRua : nomeDaAvenida;
        }
    }

    public string OQueHaAqui(ObstaculoDaObra obstaculo)
    {
        switch (obstaculo)
        {
            case ObstaculoDaObra.Rio: return aquiRio;
            case ObstaculoDaObra.Morro: return aquiMorro;
            case ObstaculoDaObra.Via: return aquiVia;
            case ObstaculoDaObra.CampoECidade: return aquiCampoECidade;
            case ObstaculoDaObra.DentroDaCidade: return aquiDentroDaCidade;
            case ObstaculoDaObra.LugarQualquer: return aquiLugarQualquer;
            case ObstaculoDaObra.BeiraDaRua: return aquiBeiraDaRua;
            case ObstaculoDaObra.SeparadoDoTransito: return aquiSeparadoDoTransito;
            case ObstaculoDaObra.MudarDeSentido: return aquiMudarDeSentido;
            default: return aquiGuardarVeiculos;
        }
    }

    public string QuemEstaPassando(QuemPassaNaObra quem)
    {
        switch (quem)
        {
            case QuemPassaNaObra.Veiculo: return umVeiculo;
            case QuemPassaNaObra.Pedestre: return umPedestre;
            case QuemPassaNaObra.Ciclista: return umCiclista;
            default: return umAnimal;
        }
    }
}
