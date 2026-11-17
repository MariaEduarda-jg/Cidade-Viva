using System.Collections;
using UnityEngine;

// Uma pessoa da família (ou a vovó) que pode andar na rua e sentar num banco do carro.
// Sentada, usa o último quadro da animação "SitDown"; "quadril" e "cabeca" são as posições do quadril e da
// cabeça nessa pose (medidas pelo construtor), pra pôr o quadril em cima do assento.
public class OcupanteDoCarro : MonoBehaviour
{
    public string id;
    public string nome;
    public Vector3 quadril;
    public Vector3 cabeca;
    public Animator animador;

    public int Assento { get; set; } = -1;
    public bool Andando { get; private set; }

    private static readonly int Velocidade = Animator.StringToHash("Velocidade");

    // Altura do peito acima do assento, na pose sentada (o cinto passa por ali).
    public Vector3 PeitoNoAssento { get { return (cabeca - quadril) * 0.55f + new Vector3(0f, 0f, 0.17f); } }

    public void Sentar(Transform assento, int indice)
    {
        gameObject.SetActive(true);
        transform.SetParent(assento, false);
        transform.localRotation = Quaternion.identity;
        transform.localPosition = -quadril;
        transform.localScale = Vector3.one;
        Assento = indice;
        Andando = false;
        if (animador != null)
        {
            animador.Play("SitDown", 0, 1f);
            animador.Update(0f);
        }
    }

    // De pé, fora do carro, parado ou andando.
    public void FicarDePe(Vector3 onde, Vector3 olhandoPara)
    {
        gameObject.SetActive(true);
        transform.SetParent(null, true);
        transform.position = onde;
        Vector3 d = olhandoPara - onde;
        d.y = 0f;
        if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
        transform.localScale = Vector3.one;
        Assento = -1;
        if (animador != null)
        {
            animador.Play("Locomocao", 0, 0f);
            animador.SetFloat(Velocidade, 0f);
        }
    }

    public IEnumerator Andar(Vector3 ate, float velocidade = 1.6f)
    {
        Andando = true;
        if (animador != null) animador.SetFloat(Velocidade, 0.5f);
        while (true)
        {
            Vector3 d = ate - transform.position;
            d.y = 0f;
            if (d.magnitude < 0.05f) break;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-10f * Time.deltaTime));
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(ate.x, transform.position.y, ate.z), velocidade * Time.deltaTime);
            yield return null;
        }
        if (animador != null) animador.SetFloat(Velocidade, 0f);
        Andando = false;
    }

    public void Comemorar()
    {
        if (animador != null) animador.Play("Victory", 0, 0f);
    }

    // Todos os renderizadores numa camada (a do jogador fica fora da câmera dentro do carro).
    public void Camada(int camada)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = camada;
    }
}
