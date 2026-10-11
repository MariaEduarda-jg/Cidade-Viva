using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Regras do "Sobe, Sobe!": gera plataformas (algumas com moeda) acima, faz a câmera subir com o
// jogador, sobe a água e termina o jogo se ele cair da tela ou a água alcançar.
// A dificuldade aumenta por nível (a cada intervaloDeNivel segundos): água mais rápida,
// plataformas mais estreitas e mais espaçadas.
// Plataformas, moedas, jogador e água ficam dentro de "mundo", que desce conforme a câmera sobe.
public class SobeSobeManager : MinigameBase
{
    public SobeSobeConfig config;
    public SobeJogador jogador;
    public RectTransform mundo;
    public RectTransform agua;
    public RectTransform plataformaInicial;
    public RectTransform modeloDePlataforma;
    public RectTransform modeloDeMoeda;
    public Text textoPontos;

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
    public int Nivel => Mathf.Min(config.nivelMaximo, Mathf.FloorToInt(tempo / config.intervaloDeNivel));
    public int Pontuacao => Mathf.FloorToInt(alturaMaxima / config.pixelsPorPonto) + pontosDeMoedas;

    private void Start()
    {
        tela = (RectTransform)mundo.parent;
        Pintar(jogador.RectTransform, config.corDoJogador);
        Pintar(plataformaInicial, config.corDaPlataforma);
        Pintar(modeloDePlataforma, config.corDaPlataforma);
        Pintar(modeloDeMoeda, config.corDaMoeda);
        Pintar(agua, config.corDaAgua);

        plataformas.Add(plataformaInicial);
        ultimaPlataforma = new Vector2(0f, plataformaInicial.anchoredPosition.y);
        topoDoUltimoPouso = Topo(plataformaInicial);
        alturaInicial = jogador.RectTransform.anchoredPosition.y;
        aguaY = config.alturaInicialDaAgua;
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
        aguaY += (config.velocidadeInicialDaAgua + Nivel * config.aumentoDaAguaPorNivel) * Time.deltaTime;
        aguaY = Mathf.Max(aguaY, topoDoUltimoPouso - config.distanciaMaximaDaAgua);
        Atualizar();
        ColetarMoedas();

        float jogadorY = jogador.RectTransform.anchoredPosition.y;
        float meiaAlturaJogador = jogador.RectTransform.rect.height / 2f;
        if (jogadorY + meiaAlturaJogador < cameraY - tela.rect.height / 2f)
        {
            FimDeJogo(config.tituloAoCair);
        }
        else if (jogadorY < aguaY)
        {
            FimDeJogo(config.tituloAoSerAlcancado);
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
        cameraY = Mathf.Max(cameraY, jogadorY - config.alturaDoJogadorNaTela);
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
        while (ultimaPlataforma.y < topoDaTela + config.distanciaMaxima)
        {
            int nivel = Nivel;
            float largura = config.larguraInicialDaPlataforma - nivel * config.reducaoDeLarguraPorNivel;
            float aumento = nivel * config.aumentoDeDistanciaPorNivel;
            float limiteX = config.larguraDaArea / 2f - largura / 2f;
            float y = ultimaPlataforma.y + Random.Range(config.distanciaMinima + aumento, config.distanciaMaxima + aumento);
            float x = Mathf.Clamp(ultimaPlataforma.x + Random.Range(-config.deslocamentoMaximoX, config.deslocamentoMaximoX), -limiteX, limiteX);

            RectTransform plataforma = Criar(modeloDePlataforma, new Vector2(x, y));
            plataforma.sizeDelta = new Vector2(largura, plataforma.sizeDelta.y);
            plataformas.Add(plataforma);
            ultimaPlataforma = new Vector2(x, y);

            if (modeloDeMoeda != null && Random.value < config.chanceDeMoeda)
            {
                moedas.Add(Criar(modeloDeMoeda, new Vector2(x, Topo(plataforma) + config.alturaDaMoedaSobreAPlataforma)));
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
                pontosDeMoedas += config.pontosPorMoeda;
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
            textoPontos.text = string.Format(config.formatoPontos, Pontuacao);
        }
    }

    private void FimDeJogo(string motivo)
    {
        Jogando = false;
        FinalizarJogo(Pontuacao, motivo);
    }

    private static void Pintar(RectTransform objeto, Color cor)
    {
        Graphic grafico = objeto != null ? objeto.GetComponent<Graphic>() : null;
        if (grafico != null)
        {
            grafico.color = cor;
        }
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
