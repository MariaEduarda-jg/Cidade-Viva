using UnityEngine;

// O "local destacado" de um problema das Obras da Cidade: a criança toca aqui pra construir.
// Tem um collider grande (fácil de acertar) e um sinal que sobe e desce enquanto o problema é o da vez.
public class ObraLocal : MonoBehaviour
{
    public int indiceDoProblema;
    public Transform sinal;
    public float altura = 0.5f;
    public float ritmo = 3f;

    private Vector3 base_;
    private bool daVez;

    private void Awake()
    {
        base_ = sinal.localPosition;
    }

    // daVez = é o problema escolhido agora (sinal grande e pulando); senão fica pequeno e parado.
    public void Destacar(bool daVez)
    {
        this.daVez = daVez;
        sinal.localScale = Vector3.one * (daVez ? 1f : 0.55f);
        sinal.localPosition = base_;
    }

    private void Update()
    {
        if (daVez)
        {
            sinal.localPosition = base_ + Vector3.up * (Mathf.Sin(Time.time * ritmo) * altura);
            sinal.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        }
    }
}
