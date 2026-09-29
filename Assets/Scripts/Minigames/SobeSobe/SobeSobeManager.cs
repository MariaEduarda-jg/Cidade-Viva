using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Regras do "Sobe, Sobe!": gera plataformas (algumas com moeda) acima, faz a câmera subir com o
// jogador, sobe a água e termina o jogo se ele cair da tela ou a água alcançar.
// A dificuldade aumenta por nível (a cada intervaloDeNivel segundos): água mais rápida,
// plataformas mais estreitas e mais espaçadas.
// Plataformas, moedas, jogador e água ficam dentro de "mundo", que desce conforme a câmera sobe.
public class SobeSobeManager : MonoBehaviour
{
    public string nomeDaArea = "Sobe, Sobe!";
    public SobeJogador jogador;
    public RectTransform mundo;
    public RectTransform agua;
    public RectTransform plataformaInicial;
    public RectTransform modeloDePlataforma;
    public RectTransform modeloDeMoeda;
    public float larguraDaArea = 1000f;

    [Header("Dificuldade")]
    public float intervaloDeNivel = 15f;
    public int nivelMaximo = 6;

    [Header("Plataformas")]
    public float larguraInicialDaPlataforma = 240f;
    public float reducaoDeLarguraPorNivel = 15f;
    public float distanciaMinima = 160f;
    public float distanciaMaxima = 220f;
    public float aumentoDeDistanciaPorNivel = 10f;
    public float deslocamentoMaximoX = 420f;

    [Header("Moedas")]
    [Range(0f, 1f)] public float chanceDeMoeda = 0.3f;
    public int pontosPorMoeda = 20;
    public float alturaDaMoedaSobreAPlataforma = 60f;

    [Header("Água")]
    public float velocidadeInicialDaAgua = 80f;
    public float aumentoDaAguaPorNivel = 12f;
    public float alturaInicialDaAgua = -700f;
    // A água nunca fica mais longe que isso abaixo da plataforma mais alta em que o jogador pisou,
    // pra continuar aparecendo no pé da tela enquanto ele sobe.
    public float distanciaMaximaDaAgua = 300f;

    [Header("Câmera e pontuação")]
    // Altura na tela (a partir do centro) acima da qual a câmera acompanha o jogador.
    public float alturaDoJogadorNaTela = 150f;
    public float pixelsPorPonto = 10f;
    public Text textoPontos;
    public TelaFimDeJogo telaFimDeJogo;

    private readonly List<RectTransform> plataformas = new List<RectTransform>();
    private readonly List<RectTransform> moedas = new List<RectTransform>();
    private RectTransform tela;
    private float tempo;
    private float cameraY;
    private float aguaY;
    private float alturaInicial;
    private float alturaMaxima;
    private float topoDoUltimoPouso;
    private int pontosDeMoedas;
    private Vector2 ultimaPlataforma;

    public bool Jogando { get; private set; }
    public int Nivel => Mathf.Min(nivelMaximo, Mathf.FloorToInt(tempo / intervaloDeNivel));
    public int Pontuacao => Mathf.FloorToInt(alturaMaxima / pixelsPorPonto) + pontosDeMoedas;

    private void Start()
    {
        tela = (RectTransform)mundo.parent;
        telaFimDeJogo.gameObject.SetActive(false);

        plataformas.Add(plataformaInicial);
        ultimaPlataforma = new Vector2(0f, plataformaInicial.anchoredPosition.y);
        topoDoUltimoPouso = Topo(plataformaInicial);
        alturaInicial = jogador.RectTransform.anchoredPosition.y;
        aguaY = alturaInicialDaAgua;
        Jogando = true;

        Atualizar();
    }

    private void Update()
    {
        if (!Jogando)
        {
            return;
        }

        tempo += Time.deltaTime;
        aguaY += (velocidadeInicialDaAgua + Nivel * aumentoDaAguaPorNivel) * Time.deltaTime;
        aguaY = Mathf.Max(aguaY, topoDoUltimoPouso - distanciaMaximaDaAgua);
        Atualizar();
        ColetarMoedas();

        float jogadorY = jogador.RectTransform.anchoredPosition.y;
        float meiaAlturaJogador = jogador.RectTransform.rect.height / 2f;
        if (jogadorY + meiaAlturaJogador < cameraY - tela.rect.height / 2f)
        {
            FimDeJogo("Você caiu!");
        }
        else if (jogadorY < aguaY)
        {
            FimDeJogo("A água te alcançou!");
        }
    }

    // Plataforma cujo topo o pé do jogador cruzou neste frame (o pouso), ou null.
    public RectTransform PlataformaEmbaixo(float x, float largura, float peAntes, float peDepois)
    {
        foreach (RectTransform plataforma in plataformas)
        {
            float topo = Topo(plataforma);
            float distanciaX = Mathf.Abs(x - plataforma.anchoredPosition.x);
            if (peAntes >= topo && peDepois <= topo && distanciaX <= (plataforma.rect.width + largura) / 2f)
            {
                topoDoUltimoPouso = Mathf.Max(topoDoUltimoPouso, topo);
                return plataforma;
            }
        }

        return null;
    }

    private void Atualizar()
    {
        float jogadorY = jogador.RectTransform.anchoredPosition.y;
        cameraY = Mathf.Max(cameraY, jogadorY - alturaDoJogadorNaTela);
        mundo.anchoredPosition = new Vector2(0f, -cameraY);
        agua.anchoredPosition = new Vector2(0f, aguaY - agua.rect.height / 2f);

        GerarPlataformas();
        RemoverAbaixo(plataformas);
        RemoverAbaixo(moedas);

        alturaMaxima = Mathf.Max(alturaMaxima, jogadorY - alturaInicial);
        AtualizarTexto();
    }

    private void GerarPlataformas()
    {
        float topoDaTela = cameraY + tela.rect.height / 2f;
        while (ultimaPlataforma.y < topoDaTela + distanciaMaxima)
        {
            int nivel = Nivel;
            float largura = larguraInicialDaPlataforma - nivel * reducaoDeLarguraPorNivel;
            float aumento = nivel * aumentoDeDistanciaPorNivel;
            float limiteX = larguraDaArea / 2f - largura / 2f;
            float y = ultimaPlataforma.y + Random.Range(distanciaMinima + aumento, distanciaMaxima + aumento);
            float x = Mathf.Clamp(ultimaPlataforma.x + Random.Range(-deslocamentoMaximoX, deslocamentoMaximoX), -limiteX, limiteX);

            RectTransform plataforma = Criar(modeloDePlataforma, new Vector2(x, y));
            plataforma.sizeDelta = new Vector2(largura, plataforma.sizeDelta.y);
            plataformas.Add(plataforma);
            ultimaPlataforma = new Vector2(x, y);

            if (modeloDeMoeda != null && Random.value < chanceDeMoeda)
            {
                moedas.Add(Criar(modeloDeMoeda, new Vector2(x, Topo(plataforma) + alturaDaMoedaSobreAPlataforma)));
            }
        }
    }

    private RectTransform Criar(RectTransform modelo, Vector2 posicao)
    {
        RectTransform objeto = Instantiate(modelo, mundo);
        objeto.gameObject.SetActive(true);
        objeto.anchoredPosition = posicao;
        // Fica atrás do jogador e da água. (Não usar a plataformaInicial: ela é destruída ao sair da tela.)
        objeto.SetSiblingIndex(jogador.transform.GetSiblingIndex());
        return objeto;
    }

    private void ColetarMoedas()
    {
        Rect areaDoJogador = RectNoMundo(jogador.RectTransform);
        for (int i = moedas.Count - 1; i >= 0; i--)
        {
            if (areaDoJogador.Overlaps(RectNoMundo(moedas[i])))
            {
                pontosDeMoedas += pontosPorMoeda;
                Destroy(moedas[i].gameObject);
                moedas.RemoveAt(i);
            }
        }
    }

    private void RemoverAbaixo(List<RectTransform> objetos)
    {
        float fundoDaTela = cameraY - tela.rect.height / 2f;
        for (int i = objetos.Count - 1; i >= 0; i--)
        {
            if (objetos[i].anchoredPosition.y < fundoDaTela - 200f)
            {
                Destroy(objetos[i].gameObject);
                objetos.RemoveAt(i);
            }
        }
    }

    private void AtualizarTexto()
    {
        if (textoPontos != null)
        {
            textoPontos.text = "Pontos: " + Pontuacao;
        }
    }

    private void FimDeJogo(string motivo)
    {
        Jogando = false;
        telaFimDeJogo.Mostrar(nomeDaArea, Pontuacao, motivo);
    }

    private static float Topo(RectTransform plataforma)
    {
        return plataforma.anchoredPosition.y + plataforma.rect.height / 2f;
    }

    // Tudo fica dentro de "mundo" com âncora no centro, então dá pra comparar as posições direto.
    private static Rect RectNoMundo(RectTransform rt)
    {
        Vector2 tamanho = rt.rect.size;
        return new Rect(rt.anchoredPosition - tamanho / 2f, tamanho);
    }
}
