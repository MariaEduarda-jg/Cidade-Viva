using UnityEngine;

// O cinto de um banco: a tira sai da coluna (âncora, na altura do ombro), passa pela fivela e vai até a
// âncora de baixo (no quadril, do lado da porta). Prender = levar a fivela até o fecho, do outro lado do
// quadril: a tira fica atravessada no peito e no colo de quem está sentado.
// O objeto fica no espaço do carro (+Z = frente). As tiras são cubos esticados entre os pontos.
public class CintoDoAssento : MonoBehaviour
{
    public Transform ancora;
    public Transform ancoraBaixa;
    public Transform fecho;
    public Transform fivela;
    [Tooltip("Quatro pedaços de tira: âncora-peito, peito-fivela, fivela-colo, colo-âncora de baixo.")]
    public Transform[] tiras = new Transform[4];
    [Tooltip("Onde a fivela fica pendurada, solta (espaço do cinto).")]
    public Vector3 repouso;
    public float larguraDaTira = 0.05f;

    public bool Preso { get; private set; }
    public bool Ocupado { get; private set; }

    // Pontos do corpo de quem está sentado (espaço do cinto): a tira passa na frente deles.
    private Vector3 peito;
    private Vector3 colo;

    public Vector3 PeitoNoMundo { get { return transform.TransformPoint(Ocupado ? peito : (ancora.localPosition + repouso) / 2f); } }

    public void Ocupar(Vector3 peitoLocal, Vector3 coloLocal)
    {
        Ocupado = true;
        peito = peitoLocal;
        colo = coloLocal;
        Esticar();
    }

    public void Desocupar()
    {
        Ocupado = false;
        Soltar();
    }

    public void Soltar()
    {
        Preso = false;
        fivela.localPosition = repouso;
        fivela.localRotation = Quaternion.identity;
        Esticar();
    }

    public void Prender()
    {
        Preso = true;
        fivela.position = fecho.position;
        Esticar();
    }

    // Leva a fivela pra um ponto do mundo (o dedo arrastando).
    public void Levar(Vector3 mundo)
    {
        fivela.position = mundo;
        Esticar();
    }

    public float DistanciaAoFecho { get { return Vector3.Distance(fivela.position, fecho.position); } }

    public void Esticar()
    {
        Vector3 a = ancora.localPosition;
        Vector3 f = fivela.localPosition;
        Vector3 b = ancoraBaixa.localPosition;
        Vector3 m = Vector3.Lerp(a, f, 0.5f);
        Vector3 n = Vector3.Lerp(f, b, 0.5f);
        if (Ocupado)
        {
            // Onde a tira cruza o corpo, ela passa pela frente (não atravessa a pessoa).
            float pesoPeito = Mathf.Clamp01(1f - Mathf.Abs(m.x - peito.x) / 0.32f) * Mathf.Clamp01(1f - Mathf.Abs(m.y - peito.y) / 0.45f);
            m.z = Mathf.Max(m.z, Mathf.Lerp(m.z, peito.z, pesoPeito * 1.4f));
            float pesoColo = Mathf.Clamp01(1f - Mathf.Abs(n.x - colo.x) / 0.32f) * Mathf.Clamp01(1f - Mathf.Abs(n.y - colo.y) / 0.25f);
            n.z = Mathf.Max(n.z, Mathf.Lerp(n.z, colo.z, pesoColo * 1.4f));
        }
        Pedaco(0, a, m);
        Pedaco(1, m, f);
        Pedaco(2, f, n);
        Pedaco(3, n, b);
    }

    private void Pedaco(int i, Vector3 de, Vector3 ate)
    {
        Transform t = tiras[i];
        Vector3 d = ate - de;
        float tamanho = d.magnitude;
        t.localPosition = (de + ate) / 2f;
        t.localRotation = tamanho > 0.0001f ? Quaternion.LookRotation(d, Vector3.forward + Vector3.up * 0.01f) : Quaternion.identity;
        t.localScale = new Vector3(larguraDaTira, 0.008f, tamanho + 0.01f);
    }
}
