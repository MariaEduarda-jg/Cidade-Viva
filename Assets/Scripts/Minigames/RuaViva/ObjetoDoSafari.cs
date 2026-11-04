using System.Collections;
using UnityEngine;

// Um objeto que aparece pela janela. "aceita" lista os verbetes que este objeto responde: um ônibus
// também é automóvel ("com quatro ou mais rodas"), um semáforo com a luz verde também é sinal verde.
public class ObjetoDoSafari : MonoBehaviour
{
    public string idDoVerbete;
    public string[] aceita = new string[0];
    [Tooltip("\"um\" ou \"uma\": o artigo do pedido.")]
    public string artigo = "um";
    [Tooltip("1 = grande e fácil; 2 = médio; 3 = pequeno.")]
    public int dificuldade = 1;
    [Tooltip("Giro em relação à rua (o modelo pode vir com a frente em outro eixo).")]
    public float giroExtra;
    [Tooltip("Metade da largura, medida na direção de quem olha da rua.")]
    public float meiaLargura = 1f;
    public SinalDeTransito sinal;
    public CorDoSinal corDoSinal;

    private bool comemorando;

    public bool Aceita(string id)
    {
        if (id == idDoVerbete)
        {
            return true;
        }
        foreach (string a in aceita)
        {
            if (a == id) return true;
        }
        return false;
    }

    // Área de toque generosa: objeto pequeno e distante ainda dá pra tocar com o dedo.
    public void Preparar(float margem, float tamanhoMinimo)
    {
        if (sinal != null)
        {
            sinal.Mudar(corDoSinal);
        }
        Bounds caixa = new Bounds(transform.position, Vector3.zero);
        bool primeiro = true;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (primeiro) { caixa = r.bounds; primeiro = false; }
            else caixa.Encapsulate(r.bounds);
        }
        // As contas são no espaço do objeto, que nesta hora ainda está sem giro.
        BoxCollider area = gameObject.AddComponent<BoxCollider>();
        area.isTrigger = true;
        area.center = transform.InverseTransformPoint(caixa.center);
        Vector3 t = caixa.size * margem;
        area.size = new Vector3(Mathf.Max(t.x, tamanhoMinimo), Mathf.Max(t.y, tamanhoMinimo), Mathf.Max(t.z, tamanhoMinimo));
    }

    public void Comemorar()
    {
        if (!comemorando && gameObject.activeInHierarchy)
        {
            StartCoroutine(Pular());
        }
    }

    private IEnumerator Pular()
    {
        comemorando = true;
        Vector3 inicio = transform.localScale;
        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            transform.localScale = inicio * (1f + Mathf.Sin(t / 0.45f * Mathf.PI) * 0.18f);
            yield return null;
        }
        transform.localScale = inicio;
        comemorando = false;
    }
}
