using UnityEngine;
using UnityEngine.UI;

// Interface da Pedalada. Só apresentação.
public class PedaladaHUD : MonoBehaviour
{
    [Header("Cartão do roteiro")]
    public GameObject cartaoDoRoteiro;
    public Text tituloDoRoteiro;
    public Text textoDoRoteiro;
    public Text novidadeDoRoteiro;
    public GameObject botaoOuvirRoteiro;

    [Header("Nível e critérios")]
    public GameObject painelDoNivel;
    public Text textoDoNivel;
    public Image[] estrelasDosCriterios;
    public Text[] rotulosDosCriterios;
    public Text textoDoTempo;
    public Color corGanha = Color.white;
    public Color corPerdida = new Color(0.4f, 0.45f, 0.5f, 0.3f);

    [Header("Recado, balão, botões e marca")]
    public GameObject painelDoRecado;
    public Text textoDoRecado;
    public GameObject balao;
    public Text textoDoBalao;
    public GameObject botoesDeFaixa;
    public RectTransform marca;

    public void Roteiro(string titulo, string texto, string novidade, bool ouvir)
    {
        cartaoDoRoteiro.SetActive(!string.IsNullOrEmpty(titulo));
        tituloDoRoteiro.text = titulo ?? "";
        textoDoRoteiro.text = texto ?? "";
        novidadeDoRoteiro.text = novidade ?? "";
        botaoOuvirRoteiro.SetActive(ouvir);
    }

    public void Jogando(bool sim)
    {
        painelDoNivel.SetActive(sim);
        painelDoRecado.SetActive(sim);
    }

    public void Pedalando(bool sim)
    {
        botoesDeFaixa.SetActive(sim);
    }

    public void Nivel(string texto)
    {
        textoDoNivel.text = texto;
    }

    public void Criterios(bool[] ganhos, string[] rotulos)
    {
        for (int i = 0; i < estrelasDosCriterios.Length; i++)
        {
            bool g = i < ganhos.Length && ganhos[i];
            estrelasDosCriterios[i].color = g ? corGanha : corPerdida;
            rotulosDosCriterios[i].text = i < rotulos.Length ? rotulos[i] : "";
        }
    }

    // segundos negativos escondem o relógio.
    public void Tempo(float segundos)
    {
        textoDoTempo.gameObject.SetActive(segundos >= 0f);
        if (segundos >= 0f)
        {
            int s = Mathf.CeilToInt(segundos);
            textoDoTempo.text = (s / 60) + ":" + (s % 60).ToString("00");
        }
    }

    public void Recado(string texto)
    {
        textoDoRecado.text = texto ?? "";
    }

    public void Balao(string texto)
    {
        balao.SetActive(!string.IsNullOrEmpty(texto));
        textoDoBalao.text = texto ?? "";
    }

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
