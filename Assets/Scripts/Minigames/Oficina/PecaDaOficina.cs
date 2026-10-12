using System.Collections;
using UnityEngine;

// Uma peça da prateleira da Oficina (roda, pedal, motor...). Sabe onde mora na prateleira e voa
// até a bancada ou de volta. Quem decide se ela serve é o OficinaManager.
public class PecaDaOficina : MonoBehaviour
{
    public TipoDePeca tipo;
    // Parte que gira quando o veículo anda (roda e pedal).
    public Transform visual;
    // Formato da mesma peça num veículo de duas rodas (roda fina, selim, farol). Vazio = o formato é um só.
    public Transform visualDeDuasRodas;
    // Formato da peça na motoneta (o assento largo, de sentar com as pernas juntas).
    public Transform visualDeMotoneta;
    [Tooltip("Giro na prateleira, pra peça aparecer de frente pra câmera.")]
    public Vector3 giroNaPrateleira;

    // Vaga da prateleira de onde a peça saiu.
    public Transform Vaga { get; private set; }
    public EncaixeDaOficina Encaixe { get; private set; }
    public bool EmMovimento { get; private set; }

    public bool NaPrateleira
    {
        get { return Encaixe == null; }
    }

    // Escolhe o formato da peça conforme o veículo em montagem.
    public void UsarVisual(bool duasRodas, string variante)
    {
        Transform escolhido = null;
        if (variante == "Motoneta" && visualDeMotoneta != null)
        {
            escolhido = visualDeMotoneta;
        }
        else if (duasRodas && visualDeDuasRodas != null)
        {
            escolhido = visualDeDuasRodas;
        }
        if (escolhido == null)
        {
            return;
        }

        visual.gameObject.SetActive(false);
        escolhido.gameObject.SetActive(true);
        visual = escolhido;
    }

    public void MorarEm(Transform vaga)
    {
        Vaga = vaga;
        transform.SetParent(vaga, false);
        transform.localPosition = Vector3.zero;
        transform.localEulerAngles = giroNaPrateleira;
    }

    public void VoltarPraPrateleira(float duracao)
    {
        Encaixe = null;
        StopAllCoroutines();
        StartCoroutine(Voar(Vaga, Quaternion.Euler(giroNaPrateleira), duracao));
    }

    public void EncaixarEm(EncaixeDaOficina encaixe, float duracao)
    {
        Encaixe = encaixe;
        StopAllCoroutines();
        StartCoroutine(Voar(encaixe.transform, Quaternion.identity, duracao));
    }

    private IEnumerator Voar(Transform destino, Quaternion giroLocal, float duracao)
    {
        EmMovimento = true;
        transform.SetParent(destino, true);
        Vector3 deOnde = transform.localPosition;
        Quaternion giroInicial = transform.localRotation;
        Vector3 escalaInicial = transform.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, duracao));
            float suave = 1f - (1f - t) * (1f - t);
            transform.localPosition = Vector3.Lerp(deOnde, Vector3.zero, suave);
            transform.localRotation = Quaternion.Slerp(giroInicial, giroLocal, suave);
            transform.localScale = Vector3.Lerp(escalaInicial, Vector3.one, suave);
            yield return null;
        }
        EmMovimento = false;
    }
}
