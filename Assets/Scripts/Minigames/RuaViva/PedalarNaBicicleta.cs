using UnityEngine;

// O menino pedala de verdade: o pedivela gira conforme a velocidade, cada perna dobra pra alcançar o seu pedal
// (coxa e canela, por cálculo de dois ossos) e as rodas giram. Roda depois da animação (LateUpdate),
// então o resto do corpo continua com a animação parada ("Idle").
// Os braços vão pra frente, segurando o guidão.
public class PedalarNaBicicleta : MonoBehaviour
{
    [Tooltip("Velocidade da bicicleta (m/s). Quem anda é o manager; aqui é só a aparência.")]
    public float velocidade;

    [Header("Bicicleta")]
    public Transform pedivela;
    public Transform pedalEsquerdo;
    public Transform pedalDireito;
    public Transform[] rodas = new Transform[0];
    public float raioDaRoda = 0.18f;
    [Tooltip("Voltas do pedivela por volta da roda.")]
    public float relacao = 0.5f;

    [Header("Pernas (ossos do personagem)")]
    public Transform coxaEsquerda;
    public Transform canelaEsquerda;
    public Transform fimEsquerdo;
    public Transform peEsquerdo;
    public Transform coxaDireita;
    public Transform canelaDireita;
    public Transform fimDireito;
    public Transform peDireito;
    public Transform[] bracos = new Transform[0];
    public float anguloDosBracos = 55f;

    private float angulo;
    private float coxa, canela;

    private void Start()
    {
        if (coxaEsquerda != null)
        {
            coxa = Vector3.Distance(coxaEsquerda.position, canelaEsquerda.position);
            canela = Vector3.Distance(canelaEsquerda.position, fimEsquerdo.position);
        }
    }

    private void LateUpdate()
    {
        float giroDaRoda = velocidade / Mathf.Max(0.05f, raioDaRoda) * Mathf.Rad2Deg * Time.deltaTime;
        foreach (Transform r in rodas)
        {
            if (r != null) r.Rotate(transform.right, giroDaRoda, Space.World);
        }
        angulo += giroDaRoda * relacao;
        if (pedivela != null) pedivela.localRotation = Quaternion.Euler(angulo, 0f, 0f);

        if (coxaEsquerda == null || coxa <= 0f) return;
        Perna(coxaEsquerda, canelaEsquerda, fimEsquerdo, peEsquerdo, pedalEsquerdo.position);
        Perna(coxaDireita, canelaDireita, fimDireito, peDireito, pedalDireito.position);
        foreach (Transform b in bracos)
        {
            if (b != null) b.Rotate(transform.right, -anguloDosBracos, Space.World);
        }
    }

    // Pras fotos e pros testes: põe o pedal num ângulo e acerta as pernas (sem esperar um quadro).
    public void Posar(float anguloDoPedal)
    {
        if (coxa <= 0f) Start();
        angulo = anguloDoPedal;
        float v = velocidade;
        velocidade = 0f;
        LateUpdate();
        velocidade = v;
    }

    // Dois ossos: o joelho vai pra frente da bicicleta.
    private void Perna(Transform quadril, Transform joelho, Transform fim, Transform pe, Vector3 alvo)
    {
        Vector3 h = quadril.position;
        Vector3 ate = alvo - h;
        float d = Mathf.Clamp(ate.magnitude, Mathf.Abs(coxa - canela) + 0.001f, coxa + canela - 0.001f);
        Vector3 dir = ate.normalized;
        float x = (coxa * coxa - canela * canela + d * d) / (2f * d);
        float y = Mathf.Sqrt(Mathf.Max(0f, coxa * coxa - x * x));
        Vector3 frente = Vector3.ProjectOnPlane(transform.forward, dir).normalized;
        Vector3 k = h + dir * x + frente * y;
        Vector3 pePos = h + dir * d;

        quadril.rotation = Quaternion.FromToRotation(joelho.position - h, k - h) * quadril.rotation;
        joelho.rotation = Quaternion.FromToRotation(fim.position - joelho.position, pePos - joelho.position) * joelho.rotation;
        if (pe != null) pe.position = pePos;
    }
}
