using System.Collections;
using UnityEngine;

// O carro da família, por dentro e por fora (gerado pelo construtor). Guarda os pontos que o jogo usa:
// bancos, cintos, poses da câmera, volante (o meio é a buzina), mão do motorista, chave, rádio e retrovisor.
// Espaço do carro: origem no chão, no meio do carro; +Z = frente; o motorista vai à esquerda.
public class CarroDaFamilia : MonoBehaviour
{
    [Header("Bancos: 0 motorista, 1 frente, 2 atrás esq., 3 atrás meio, 4 atrás dir.")]
    public Transform[] assentos = new Transform[5];
    public CintoDoAssento[] cintos = new CintoDoAssento[5];
    [Tooltip("A câmera olhando cada banco (pra prender o cinto).")]
    public Transform[] posesDoCinto = new Transform[5];
    [Tooltip("Onde uma coisa solta cai, na freada (no chão, na frente de cada banco).")]
    public Transform[] chaoDosAssentos = new Transform[5];
    [Tooltip("Onde o que estava solto vai parar na freada: em cima do painel, na frente de quem vai atrás.")]
    public Transform pousoDosSoltos;
    [Tooltip("Onde fica a porta de cada banco, do lado de fora (quem entra e sai passa por aí).")]
    public Transform[] portas = new Transform[5];

    [Header("Câmera")]
    [Tooltip("Os olhos de quem vai no meio do banco de trás (o jogador).")]
    public Transform poseDoPasseio;
    public Transform poseDaChave;

    [Header("Volante, buzina e motorista")]
    public Collider centroDoVolante;
    public Transform botaoDaBuzina;
    public Transform maoDoMotorista;
    public Transform maoNoVolante;
    public Transform maoNaBuzina;

    [Header("Chave, painel e rádio")]
    public Collider chave;
    public Transform giroDaChave;
    public Renderer[] luzesDoPainel = new Renderer[0];
    public Material luzApagada;
    public Material luzAcesa;
    public Collider radio;
    public Transform portaMalas;

    [Header("Retrovisor")]
    public Camera retrovisor;
    public Renderer espelho;

    [Header("Rodas")]
    public Transform[] rodas = new Transform[0];

    public bool Ligado { get; private set; }
    private bool apertando;

    public void Preparar()
    {
        Ligado = false;
        Luzes(false);
        if (retrovisor != null && espelho != null)
        {
            RenderTexture rt = new RenderTexture(320, 128, 16);
            rt.name = "Retrovisor";
            retrovisor.targetTexture = rt;
            Material m = espelho.material;
            m.SetTexture("_BaseMap", rt);
            m.mainTexture = rt;
            // Espelho: a imagem fica invertida da esquerda pra direita.
            m.SetTextureScale("_BaseMap", new Vector2(-1f, 1f));
            m.SetTextureOffset("_BaseMap", new Vector2(1f, 0f));
        }
        if (maoDoMotorista != null && maoNoVolante != null)
        {
            maoDoMotorista.SetPositionAndRotation(maoNoVolante.position, maoNoVolante.rotation);
        }
    }

    public void Luzes(bool acesas)
    {
        foreach (Renderer r in luzesDoPainel)
        {
            if (r != null) r.sharedMaterial = acesas ? luzAcesa : luzApagada;
        }
    }

    public IEnumerator GirarChave()
    {
        Quaternion de = giroDaChave.localRotation;
        Quaternion ate = de * Quaternion.Euler(0f, 0f, -80f);
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            giroDaChave.localRotation = Quaternion.Slerp(de, ate, t / 0.35f);
            yield return null;
        }
        giroDaChave.localRotation = ate;
        Ligado = true;
        Luzes(true);
    }

    // A mão do motorista vai ao meio do volante, aperta e volta.
    public IEnumerator ApertarBuzina()
    {
        if (apertando || maoDoMotorista == null)
        {
            yield break;
        }
        apertando = true;
        Vector3 botao = botaoDaBuzina.localPosition;
        for (float t = 0f; t < 0.12f; t += Time.deltaTime)
        {
            maoDoMotorista.position = Vector3.Lerp(maoNoVolante.position, maoNaBuzina.position, t / 0.12f);
            yield return null;
        }
        maoDoMotorista.position = maoNaBuzina.position;
        botaoDaBuzina.localPosition = botao - botaoDaBuzina.parent.InverseTransformDirection(botaoDaBuzina.up) * 0.012f;
        yield return new WaitForSeconds(0.35f);
        botaoDaBuzina.localPosition = botao;
        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            maoDoMotorista.position = Vector3.Lerp(maoNaBuzina.position, maoNoVolante.position, t / 0.2f);
            yield return null;
        }
        maoDoMotorista.position = maoNoVolante.position;
        apertando = false;
    }

    public void GirarRodas(float metros)
    {
        float graus = metros / (2f * Mathf.PI * 0.33f) * 360f;
        foreach (Transform r in rodas)
        {
            if (r != null) r.Rotate(Vector3.right * graus, Space.Self);
        }
    }
}
