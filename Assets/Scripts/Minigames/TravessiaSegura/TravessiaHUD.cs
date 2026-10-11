using UnityEngine;
using UnityEngine.UI;

// Interface do "Travessia Segura" durante a partida: vidas (estrelas), nível, dica do momento,
// aviso de ambulância e o espelho do semáforo de pedestres. Só apresentação.
public class TravessiaHUD : MonoBehaviour
{
    public Image[] vidas;
    public Text textoDoNivel;
    public Text textoDaDica;
    public GameObject painelDaDica;
    public GameObject avisoDeAmbulancia;
    public GameObject painelDoSemaforo;
    public Image luzDoSemaforo;
    public Sprite spritePare;
    public Sprite spriteSiga;
    // Setas de andar pro lado na calçada: só fazem sentido antes de entrar na rua.
    public GameObject botoesDeLado;

    public Color vidaCheia = Color.white;
    public Color vidaPerdida = new Color(0.35f, 0.4f, 0.45f, 0.35f);

    private float dicaAte;

    private void Update()
    {
        if (painelDaDica != null && painelDaDica.activeSelf && dicaAte > 0f && Time.unscaledTime > dicaAte)
        {
            painelDaDica.SetActive(false);
        }
    }

    public void MostrarNivel(int nivel)
    {
        textoDoNivel.text = "Nível " + nivel;
    }

    public void MostrarVidas(int restantes)
    {
        for (int i = 0; i < vidas.Length; i++)
        {
            vidas[i].color = i < restantes ? vidaCheia : vidaPerdida;
        }
    }

    // segundos <= 0: a dica fica até a próxima.
    public void Dica(string texto, float segundos = 3.5f)
    {
        textoDaDica.text = texto;
        painelDaDica.SetActive(!string.IsNullOrEmpty(texto));
        dicaAte = segundos > 0f ? Time.unscaledTime + segundos : 0f;
    }

    public void Ambulancia(bool chegando)
    {
        if (avisoDeAmbulancia.activeSelf != chegando)
        {
            avisoDeAmbulancia.SetActive(chegando);
        }
    }

    public void Semaforo(bool ligado, bool siga)
    {
        if (painelDoSemaforo.activeSelf != ligado)
        {
            painelDoSemaforo.SetActive(ligado);
        }
        if (ligado)
        {
            luzDoSemaforo.sprite = siga ? spriteSiga : spritePare;
        }
    }

    public void NaCalcada(bool naCalcadaInicial)
    {
        if (botoesDeLado.activeSelf != naCalcadaInicial)
        {
            botoesDeLado.SetActive(naCalcadaInicial);
        }
    }
}
