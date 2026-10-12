using UnityEngine;
using UnityEngine.UI;

// Recado curto no meio da tela do Hub (ex.: "Obras da Cidade: em breve!"). Some sozinho.
public class AvisoDoHub : MonoBehaviour
{
    public GameObject painel;
    public Text texto;
    public float duracao = 2.5f;

    private static AvisoDoHub instancia;
    private float ate;

    private void Awake()
    {
        instancia = this;
        painel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instancia == this)
        {
            instancia = null;
        }
    }

    public static void Mostrar(string recado)
    {
        if (instancia == null)
        {
            Debug.Log(recado);
            return;
        }
        instancia.texto.text = recado;
        instancia.painel.SetActive(true);
        instancia.ate = Time.unscaledTime + instancia.duracao;
    }

    public static bool Visivel
    {
        get { return instancia != null && instancia.painel.activeSelf; }
    }

    private void Update()
    {
        if (painel.activeSelf && Time.unscaledTime >= ate)
        {
            painel.SetActive(false);
        }
    }
}
