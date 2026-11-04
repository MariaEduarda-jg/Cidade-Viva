using UnityEngine;

// O percurso do Passeio de Carro: a rota, a câmera dentro do carro e os modelos inativos de tudo que
// é colocado na rua conforme o nível.
public class RuaVivaPercurso : MonoBehaviour
{
    [Header("Rota (gerada pelo construtor)")]
    public Vector3[] rota = new Vector3[0];
    public float passoDaRota = 0.25f;

    [Header("Modelos inativos")]
    public ObjetoDoSafari[] modelosDoSafari = new ObjetoDoSafari[0];
    public GameObject[] modelosDePedestre = new GameObject[0];
    public GameObject modeloDeCiclista;
    public GameObject[] modelosDeCasa = new GameObject[0];
    [Tooltip("Onde os objetos copiados ficam.")]
    public Transform colocados;

    public float S { get; set; }
    // Solavanco da freada: inclina a câmera pra frente.
    public float Tranco { get; set; }

    private Vector3 olhar;
    private bool olharIniciado;

    public Vector3 Posicao(float s)
    {
        float i = Mathf.Clamp(s / passoDaRota, 0f, rota.Length - 1.001f);
        int a = Mathf.FloorToInt(i);
        return Vector3.Lerp(rota[a], rota[a + 1], i - a);
    }

    public Vector3 Direcao(float s)
    {
        Vector3 d = Posicao(s + 1f) - Posicao(s - 1f);
        d.y = 0f;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
    }

    public Vector3 Direita(float s)
    {
        return Vector3.Cross(Vector3.up, Direcao(s));
    }

    public float Comprimento { get { return (rota.Length - 2) * passoDaRota; } }

    // true se o trecho em volta de "s" é reto (objeto e pessoa não ficam em curva).
    public bool EhReto(float s)
    {
        return Vector3.Angle(Direcao(s - 25f), Direcao(s + 25f)) < 3f;
    }

    public void ColocarCamera(Camera camera, float altura, float giro)
    {
        Vector3 alvo = Direcao(S + 3f);
        olhar = olharIniciado ? Vector3.Slerp(olhar, alvo, 1f - Mathf.Exp(-2.5f * Time.deltaTime)) : alvo;
        olharIniciado = true;
        camera.transform.position = Posicao(S) + Vector3.up * altura;
        camera.transform.rotation = Quaternion.LookRotation(olhar) * Quaternion.Euler(Tranco, giro, 0f);
    }
}
