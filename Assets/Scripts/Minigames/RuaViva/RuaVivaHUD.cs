using UnityEngine;
using UnityEngine.UI;

// Interface do Passeio de Carro. Só apresentação.
public class RuaVivaHUD : MonoBehaviour
{
    public Image telaPreta;

    [Header("Cartão da missão")]
    public GameObject cartaoDaMissao;
    public Text tituloDaMissao;
    public Text textoDaMissao;
    public Text novidadeDaMissao;
    public GameObject botaoOuvirMissao;

    [Header("Nível e critérios")]
    public GameObject painelDoNivel;
    public Text textoDoNivel;
    public Image[] estrelasDosCriterios;
    public Text[] rotulosDosCriterios;
    public Text textoDoSafari;
    public Color corGanha = Color.white;
    public Color corPerdida = new Color(0.4f, 0.45f, 0.5f, 0.3f);

    [Header("Recado (o que fazer agora)")]
    public GameObject painelDoRecado;
    public Text textoDoRecado;

    [Header("Pedido do motorista")]
    public GameObject balaoDoPedido;
    public Text textoDoPedido;
    public Image figuraDoPedido;
    public GameObject botaoOuvir;
    public GameObject painelDoTempo;
    public RectTransform barraDoTempo;

    [Header("Acerto: a palavra e a ilustração")]
    public GameObject figurinha;
    public Text palavraDaFigurinha;
    public Image imagemDaFigurinha;

    [Header("Rádio, buzina, balão e marca")]
    public GameObject painelDoRadio;
    public Text textoDoRadio;
    public GameObject sinalDaBuzina;
    public GameObject balao;
    public Text textoDoBalao;
    public RectTransform marca;

    public void Escuro(float alfa)
    {
        Color c = telaPreta.color;
        c.a = alfa;
        telaPreta.color = c;
        telaPreta.gameObject.SetActive(alfa > 0.001f);
    }

    public void Missao(string titulo, string texto, string novidade, bool ouvir)
    {
        cartaoDaMissao.SetActive(!string.IsNullOrEmpty(titulo));
        tituloDaMissao.text = titulo ?? "";
        textoDaMissao.text = texto ?? "";
        novidadeDaMissao.text = novidade ?? "";
        botaoOuvirMissao.SetActive(ouvir);
    }

    public void Jogando(bool sim)
    {
        painelDoNivel.SetActive(sim);
        painelDoRecado.SetActive(sim);
    }

    public void Recado(string texto)
    {
        textoDoRecado.text = texto ?? "";
    }

    public void Nivel(string texto)
    {
        textoDoNivel.text = texto;
    }

    public void Criterios(bool safari, bool buzina, bool semSusto, string[] rotulos, string textoSafari)
    {
        bool[] ganhos = { safari, buzina, semSusto };
        for (int i = 0; i < estrelasDosCriterios.Length; i++)
        {
            estrelasDosCriterios[i].color = ganhos[i] ? corGanha : corPerdida;
            rotulosDosCriterios[i].text = i < rotulos.Length ? rotulos[i] : "";
        }
        textoDoSafari.text = textoSafari;
    }

    public void Pedido(string texto, Sprite figura, bool ouvir)
    {
        balaoDoPedido.SetActive(!string.IsNullOrEmpty(texto));
        textoDoPedido.text = texto ?? "";
        figuraDoPedido.sprite = figura;
        figuraDoPedido.gameObject.SetActive(figura != null);
        botaoOuvir.SetActive(ouvir && !string.IsNullOrEmpty(texto));
    }

    // fracao = quanto do tempo do pedido ainda resta; negativo esconde.
    public void TempoDoPedido(float fracao)
    {
        painelDoTempo.SetActive(fracao >= 0f);
        if (fracao >= 0f)
        {
            barraDoTempo.anchorMax = new Vector2(Mathf.Clamp01(fracao), 1f);
        }
    }

    public void Figurinha(string palavra, Sprite imagem)
    {
        figurinha.SetActive(!string.IsNullOrEmpty(palavra));
        palavraDaFigurinha.text = palavra ?? "";
        imagemDaFigurinha.sprite = imagem;
        imagemDaFigurinha.gameObject.SetActive(imagem != null);
    }

    public void Radio(string texto)
    {
        painelDoRadio.SetActive(!string.IsNullOrEmpty(texto));
        textoDoRadio.text = texto ?? "";
    }

    public void Balao(string texto)
    {
        balao.SetActive(!string.IsNullOrEmpty(texto));
        textoDoBalao.text = texto ?? "";
    }

    public void SinalDaBuzina(bool ligado)
    {
        sinalDaBuzina.SetActive(ligado);
    }

    // posicao em pixels da tela.
    public void Marca(Vector3 posicaoNaTela, bool visivel)
    {
        marca.gameObject.SetActive(visivel);
        if (visivel)
        {
            marca.position = posicaoNaTela;
            float k = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.12f;
            marca.localScale = new Vector3(k, k, 1f);
        }
    }
}
