using UnityEngine;
using UnityEngine.UI;

// Interface do "Corre, Correndo!" durante a partida: escudos, moedas, nível, barra de distância
// e a dica do momento (é nela que aparece por que cada obstáculo é perigoso). Só apresentação.
public class CorreHUD : MonoBehaviour
{
    public Image[] escudos;
    public Text textoDoNivel;
    public Text textoDasMoedas;
    public Text textoDaDistancia;
    // Barra que cresce da esquerda pra direita até "larguraDaBarra".
    public RectTransform barraDeDistancia;
    public float larguraDaBarra = 600f;
    public Text textoDaDica;
    public GameObject painelDaDica;

    public Color escudoCheio = Color.white;
    public Color escudoPerdido = new Color(0.35f, 0.4f, 0.45f, 0.35f);

    public bool DicaVisivel
    {
        get { return painelDaDica.activeSelf; }
    }

    private float dicaAte;

    private void Update()
    {
        if (painelDaDica.activeSelf && dicaAte > 0f && Time.unscaledTime > dicaAte)
        {
            painelDaDica.SetActive(false);
        }
    }

    public void MostrarNivel(int nivel)
    {
        textoDoNivel.text = "Nível " + nivel;
    }

    public void MostrarEscudos(int restantes)
    {
        for (int i = 0; i < escudos.Length; i++)
        {
            escudos[i].color = i < restantes ? escudoCheio : escudoPerdido;
        }
    }

    public void MostrarMoedas(int moedas)
    {
        textoDasMoedas.text = moedas.ToString();
    }

    public void MostrarDistancia(float percorrida, float meta, string formato)
    {
        float fracao = meta > 0f ? Mathf.Clamp01(percorrida / meta) : 0f;
        barraDeDistancia.sizeDelta = new Vector2(larguraDaBarra * fracao, barraDeDistancia.sizeDelta.y);
        textoDaDistancia.text = string.Format(formato, Mathf.FloorToInt(Mathf.Min(percorrida, meta)), Mathf.RoundToInt(meta));
    }

    // segundos <= 0: a dica fica até a próxima.
    public void Dica(string texto, float segundos = 2.2f)
    {
        textoDaDica.text = texto;
        painelDaDica.SetActive(!string.IsNullOrEmpty(texto));
        dicaAte = segundos > 0f ? Time.unscaledTime + segundos : 0f;
    }
}
