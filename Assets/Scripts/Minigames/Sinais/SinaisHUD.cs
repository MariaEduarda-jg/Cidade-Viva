using UnityEngine;
using UnityEngine.UI;

// Interface da Central de Sinais (os dois modos). Só apresentação.
public class SinaisHUD : MonoBehaviour
{
    [Header("Nível e estrelas")]
    public Text textoDoNivel;
    public Image[] estrelasDosCriterios;
    public Text[] rotulosDosCriterios;
    public Text textoDosErros;
    public Color corGanha = Color.white;
    public Color corPerdida = new Color(0.4f, 0.45f, 0.5f, 0.3f);

    [Header("Recado, dica, tempo e balão")]
    public Text textoDoRecado;
    public GameObject painelDaDica;
    public Text textoDaDica;
    public GameObject painelDoTempo;
    public RectTransform barraDeTempo;
    public GameObject balao;
    public Text textoDoBalao;

    [Header("Abas dos dois modos (só na seleção de níveis)")]
    public GameObject abasDosModos;
    public Image[] frentesDasAbas;
    public Color corDaAba = Color.white;
    public Color corDaAbaEscolhida = Color.yellow;

    [Header("Motorista Mirim")]
    public GameObject controlesDoMotorista;
    public RectTransform barraDeVelocidade;
    [Tooltip("Marca na barra até onde é \"devagar\".")]
    public RectTransform marcaDeDevagar;
    public Image[] luzesDoPainel;
    public Color[] coresDasLuzes = new Color[3];
    public Color corDaLuzApagada = new Color(0.2f, 0.22f, 0.25f, 1f);

    [Header("Sinaleiro")]
    public GameObject controlesDoSinaleiro;
    public Text textoDoContador;
    [Tooltip("Um grupo por via (A, B) e um pros pedestres: somem nos níveis que não usam.")]
    public GameObject[] gruposDoPainel;
    [Tooltip("Botões de cada grupo, na ordem verde, amarelo, vermelho (pedestres: verde, vermelho).")]
    public BotaoDeSinal[] botoesDeSinal;

    private Image imagemDaVelocidade;
    private Color corDaVelocidade;

    public void Modos(bool visivel, int atual)
    {
        abasDosModos.SetActive(visivel);
        for (int i = 0; i < frentesDasAbas.Length; i++)
        {
            frentesDasAbas[i].color = i == atual ? corDaAbaEscolhida : corDaAba;
        }
    }

    // Mostra só os controles do modo em jogo (-1 = nenhum).
    public void Controles(int modo)
    {
        controlesDoMotorista.SetActive(modo == 0);
        controlesDoSinaleiro.SetActive(modo == 1);
    }

    public void Nivel(string texto)
    {
        textoDoNivel.text = texto;
    }

    public void Criterios(bool semErros, bool noTempo, string[] rotulos, string erros)
    {
        bool[] ganhos = { semErros, noTempo };
        for (int i = 0; i < estrelasDosCriterios.Length; i++)
        {
            estrelasDosCriterios[i].color = ganhos[i] ? corGanha : corPerdida;
            rotulosDosCriterios[i].text = i < rotulos.Length ? rotulos[i] : "";
        }
        textoDosErros.text = erros;
    }

    public void Recado(string texto)
    {
        textoDoRecado.text = texto ?? "";
    }

    public void Dica(string texto)
    {
        painelDaDica.SetActive(!string.IsNullOrEmpty(texto));
        textoDaDica.text = texto ?? "";
    }

    // fracao = quanto do tempo ainda resta (0 a 1); negativo esconde.
    public void Tempo(float fracao)
    {
        painelDoTempo.SetActive(fracao >= 0f);
        if (fracao >= 0f)
        {
            barraDeTempo.anchorMax = new Vector2(Mathf.Clamp01(fracao), 1f);
        }
    }

    public void Balao(string texto)
    {
        balao.SetActive(!string.IsNullOrEmpty(texto));
        textoDoBalao.text = texto ?? "";
    }

    // ---- Motorista Mirim

    // No amarelo a barra diz se já está devagar: verde = pode cruzar, vermelha = ainda rápido demais.
    public void Velocidade(float fracao, float fracaoDevagar, bool noAmarelo, bool devagar)
    {
        if (imagemDaVelocidade == null)
        {
            imagemDaVelocidade = barraDeVelocidade.GetComponent<Image>();
            corDaVelocidade = imagemDaVelocidade.color;
        }
        imagemDaVelocidade.color = !noAmarelo ? corDaVelocidade : devagar ? coresDasLuzes[2] : coresDasLuzes[0];
        barraDeVelocidade.anchorMax = new Vector2(Mathf.Clamp01(fracao), 1f);
        marcaDeDevagar.anchorMin = new Vector2(fracaoDevagar, 0f);
        marcaDeDevagar.anchorMax = new Vector2(fracaoDevagar, 1f);
    }

    // Repete na tela, bem grande, a luz do próximo semáforo (ordem das luzes: vermelha, amarela, verde).
    public void LuzDoProximo(CorDoSinal cor, bool temSemaforo)
    {
        int acesa = !temSemaforo ? -1 : cor == CorDoSinal.Vermelho ? 0 : cor == CorDoSinal.Amarelo ? 1 : 2;
        for (int i = 0; i < luzesDoPainel.Length; i++)
        {
            luzesDoPainel[i].color = i == acesa ? coresDasLuzes[i] : corDaLuzApagada;
        }
    }

    // ---- Sinaleiro

    public void Contador(string texto)
    {
        textoDoContador.text = texto;
    }

    public void GruposDoPainel(bool viaA, bool viaB, bool pedestres)
    {
        gruposDoPainel[0].SetActive(viaA);
        gruposDoPainel[1].SetActive(viaB);
        gruposDoPainel[2].SetActive(pedestres);
    }

    // Destaca no painel a luz acesa de cada semáforo (grupo 0 = via A, 1 = via B, 2 = pedestres).
    public void LuzDoPainel(int grupo, CorDoSinal cor)
    {
        foreach (BotaoDeSinal botao in botoesDeSinal)
        {
            if (botao.grupo == grupo)
            {
                botao.Destacar(botao.cor == cor);
            }
        }
    }
}
