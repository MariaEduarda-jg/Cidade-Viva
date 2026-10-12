using UnityEngine;
using UnityEngine.UI;

// Interface das Obras da Cidade. Só apresentação: quem decide tudo é o ObrasManager.
public class ObrasHUD : MonoBehaviour
{
    [Header("Nível e estrelas")]
    public Text textoDoNivel;
    public Image[] estrelasDosCriterios;
    public Text[] rotulosDosCriterios;
    public Color corGanha = Color.white;
    public Color corPerdida = new Color(0.4f, 0.45f, 0.5f, 0.3f);

    [Header("Problema")]
    public Text textoDoEnunciado;
    public Text textoDoContador;
    public GameObject painelDoTempo;
    public RectTransform barraDeTempo;
    public GameObject painelDaDica;
    public Text textoDaDica;
    public Text textoDoRecado;

    [Header("Definição da estrutura escolhida")]
    public GameObject painelDaDefinicao;
    public Image ilustracao;
    public Text textoDaPalavra;
    public Text textoDaDefinicao;
    public GameObject botaoOuvir;

    [Header("Barra de ferramentas")]
    public GameObject barra;
    public BotaoDeEstrutura[] botoes;
    public float espacoEntreBotoes = 350f;
    public Color corDoBotao = Color.white;
    public Color corDoBotaoEscolhido = Color.yellow;
    public GameObject botaoDica;

    [Header("Abas dos dois modos (só na seleção de níveis)")]
    public GameObject abasDosModos;
    public Image[] frentesDasAbas;
    public Color corDaAba = Color.white;
    public Color corDaAbaEscolhida = Color.yellow;

    [Header("Balão de explicação")]
    public GameObject balao;
    public Text textoDoBalao;

    // As abas "Pontes e Passagens" e "Planejar a rua" aparecem junto com a seleção de níveis.
    public void Modos(bool visivel, int atual)
    {
        abasDosModos.SetActive(visivel);
        for (int i = 0; i < frentesDasAbas.Length; i++)
        {
            frentesDasAbas[i].color = i == atual ? corDaAbaEscolhida : corDaAba;
        }
    }

    public void Nivel(string texto)
    {
        textoDoNivel.text = texto;
    }

    public void Criterios(bool semErros, bool noTempo, bool semDica, string[] rotulos)
    {
        bool[] ganhos = { semErros, noTempo, semDica };
        for (int i = 0; i < estrelasDosCriterios.Length; i++)
        {
            estrelasDosCriterios[i].color = ganhos[i] ? corGanha : corPerdida;
            rotulosDosCriterios[i].text = i < rotulos.Length ? rotulos[i] : "";
        }
    }

    public void Problema(string enunciado, string contador)
    {
        textoDoEnunciado.text = enunciado;
        textoDoContador.text = contador;
    }

    // fracao < 0 esconde a barra (nível sem relógio).
    public void Tempo(float fracao)
    {
        painelDoTempo.SetActive(fracao >= 0f);
        if (fracao >= 0f)
        {
            barraDeTempo.anchorMax = new Vector2(Mathf.Clamp01(fracao), 1f);
        }
    }

    public void Dica(string texto)
    {
        painelDaDica.SetActive(!string.IsNullOrEmpty(texto));
        textoDaDica.text = texto ?? "";
    }

    public void Recado(string texto)
    {
        textoDoRecado.text = texto ?? "";
    }

    // Mostra os botões das opções do problema, centrados. "escolhida" = índice destacado (-1 = nenhum).
    public void Opcoes(Verbete[] opcoes, int escolhida)
    {
        for (int i = 0; i < botoes.Length; i++)
        {
            bool tem = opcoes != null && i < opcoes.Length && opcoes[i] != null;
            botoes[i].gameObject.SetActive(tem);
            if (!tem)
            {
                continue;
            }
            botoes[i].nome.text = PrimeiraMaiuscula(opcoes[i].palavra);
            botoes[i].icone.sprite = opcoes[i].ilustracao;
            botoes[i].icone.enabled = opcoes[i].ilustracao != null;
            botoes[i].frente.color = i == escolhida ? corDoBotaoEscolhido : corDoBotao;
            RectTransform rt = (RectTransform)botoes[i].transform;
            rt.anchoredPosition = new Vector2((i - (opcoes.Length - 1) * 0.5f) * espacoEntreBotoes, rt.anchoredPosition.y);
        }
    }

    // A barra some enquanto a obra acontece e quem passa atravessa.
    public void BarraVisivel(bool visivel, bool comDica)
    {
        barra.SetActive(visivel);
        botaoDica.SetActive(visivel && comDica);
    }

    // Palavra, ilustração e definição (texto do glossário, sem mudar nada). null esconde o painel.
    public void Definicao(Verbete v, bool comOuvir)
    {
        painelDaDefinicao.SetActive(v != null);
        if (v == null)
        {
            return;
        }
        textoDaPalavra.text = PrimeiraMaiuscula(v.palavra);
        textoDaDefinicao.text = v.definicao;
        ilustracao.sprite = v.ilustracao;
        ilustracao.enabled = v.ilustracao != null;
        botaoOuvir.SetActive(comOuvir);
    }

    public void Balao(string texto)
    {
        balao.SetActive(!string.IsNullOrEmpty(texto));
        textoDoBalao.text = texto ?? "";
    }

    public static string PrimeiraMaiuscula(string s)
    {
        return string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
