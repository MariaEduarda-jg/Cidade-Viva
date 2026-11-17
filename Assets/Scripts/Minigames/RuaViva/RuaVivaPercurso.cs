using UnityEngine;

// O percurso do Passeio de Carro: a rota, o carro andando nela e os modelos inativos de tudo que é posto
// na rua conforme a missão (lugares, pedestres, carros do trânsito, bola).
public class RuaVivaPercurso : MonoBehaviour
{
    [Header("Rota (gerada pelo construtor)")]
    public Vector3[] rota = new Vector3[0];
    public float passoDaRota = 0.25f;

    [Header("Modelos inativos")]
    public LugarDoPasseio[] lugares = new LugarDoPasseio[0];
    public GameObject[] modelosDePedestre = new GameObject[0];
    public GameObject[] modelosDeTransito = new GameObject[0];
    public GameObject modeloDaBola;
    [Tooltip("Onde os objetos copiados ficam.")]
    public Transform colocados;

    public float S { get; set; }
    // Solavanco da freada: o carro inclina pra frente.
    public float Tranco { get; set; }

    private Vector3 olhar;
    private bool olharIniciado;

    public LugarDoPasseio Lugar(string id)
    {
        foreach (LugarDoPasseio l in lugares)
        {
            if (l != null && l.id == id) return l;
        }
        return null;
    }

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

    // true se o trecho em volta de "s" é reto (lugar e pessoa não ficam em curva).
    public bool EhReto(float s)
    {
        return Vector3.Angle(Direcao(s - 25f), Direcao(s + 25f)) < 3f;
    }

    public bool EhRetoEntre(float de, float ate)
    {
        for (float s = de; s <= ate; s += 10f)
        {
            if (!EhReto(s)) return false;
        }
        return EhReto(ate);
    }

    // Ponto do mundo no espaço de um trecho da rota: x = à direita da faixa, z = adiante.
    public Vector3 Ponto(float s, float lateral, float altura = 0f)
    {
        return Posicao(s) + Direita(s) * lateral + Vector3.up * altura;
    }

    public void Reiniciar()
    {
        olharIniciado = false;
    }

    public void PosicionarCarro(Transform carro)
    {
        Vector3 alvo = Direcao(S + 2f);
        olhar = olharIniciado ? Vector3.Slerp(olhar, alvo, 1f - Mathf.Exp(-3f * Time.deltaTime)) : alvo;
        olharIniciado = true;
        Vector3 p = Posicao(S);
        p.y = 0f;
        carro.position = p;
        carro.rotation = Quaternion.LookRotation(olhar) * Quaternion.Euler(Tranco, 0f, 0f);
    }
}
