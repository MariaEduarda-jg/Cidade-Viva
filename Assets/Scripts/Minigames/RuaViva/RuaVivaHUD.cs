using UnityEngine;
using UnityEngine.UI;

// Interface do Passeio de Carro. Só apresentação.
public class RuaVivaHUD : MonoBehaviour
{
    [Header("Partes que aparecem em cada momento")]
    public GameObject painelDaGaragem;
    public GameObject painelDoPasseio;
    public Image telaPreta;

    [Header("Garagem")]
    public Text textoDoOcupante;
    public FivelaArrastavel fivela;
    public Text textoDoRecado;
    public RectTransform painelDoRecado;
    public Vector2 recadoNaGaragem = new Vector2(0f, 365f);
    public Vector2 recadoNoPasseio = new Vector2(520f, 275f);

    [Header("Nível e critérios")]
    public GameObject painelDoNivel;
    public Text textoDoNivel;
    public Image[] estrelasDosCriterios;
    public Text[] rotulosDosCriterios;
    public Text textoDoSafari;
    public Color corGanha = Color.white;
    public Color corPerdida = new Color(0.4f, 0.45f, 0.5f, 0.3f);

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

    [Header("Buzina")]
    public RectTransform botaoDaBuzina;
    public GameObject sinalDaBuzina;

    [Header("Balão e marca")]
    public GameObject balao;
    public Text textoDoBalao;
    public RectTransform marca;

    public void Parte(bool garagem, bool passeio)
    {
        painelDaGaragem.SetActive(garagem);
        painelDoPasseio.SetActive(passeio);
        painelDoNivel.SetActive(garagem || passeio);
        painelDoRecado.gameObject.SetActive(garagem || passeio);
        painelDoRecado.anchoredPosition = garagem ? recadoNaGaragem : recadoNoPasseio;
    }

    public void Escuro(float alfa)
    {
        Color c = telaPreta.color;
        c.a = alfa;
        telaPreta.color = c;
        telaPreta.gameObject.SetActive(alfa > 0.001f);
    }

    public void Ocupante(string texto)
    {
        textoDoOcupante.text = texto;
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

    public void Balao(string texto)
    {
        balao.SetActive(!string.IsNullOrEmpty(texto));
        textoDoBalao.text = texto ?? "";
    }

    // O botão da buzina pulsa (nos níveis com dica) e um sinal mostra que ela tocou, pra quem joga sem som.
    public void PulsarBuzina(bool pulsando)
    {
        float k = pulsando ? 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.14f : 1f;
        botaoDaBuzina.localScale = new Vector3(k, k, 1f);
    }

    public void SinalDaBuzina(bool ligado)
    {
        sinalDaBuzina.SetActive(ligado);
    }

    // posicao em pixels da tela; negativo esconde.
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
