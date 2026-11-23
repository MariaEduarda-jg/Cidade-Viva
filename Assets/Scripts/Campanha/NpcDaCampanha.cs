using UnityEngine;

// Um personagem da campanha no Hub (mãe, mecânico, engenheira, guarda, vizinha).
// Fica parado na animação de espera, vira pro jogador quando ele chega perto, acena ao começar a conversa
// e mostra o "!" (tem conversa nova) e, no tutorial, uma seta grande em cima.
public class NpcDaCampanha : MonoBehaviour
{
    public QuemFala quem;
    [Tooltip("Objeto Area_* do Hub de que este NPC cuida (vazio na mãe).")]
    public string areaDoHub;
    public Animator animador;
    public Transform exclamacao;
    public Transform setaGrande;
    public float raioDeConversa = 3.2f;
    [Tooltip("Pra onde ele olha quando ninguém está perto.")]
    public Vector3 olharPadrao = Vector3.back;

    private Transform jogador;
    private float acenando;
    private Vector3 baseDaExclamacao, baseDaSeta;

    private void Start()
    {
        PlayerMovement p = FindAnyObjectByType<PlayerMovement>();
        if (p != null) jogador = p.transform;
        if (exclamacao != null) baseDaExclamacao = exclamacao.localPosition;
        if (setaGrande != null) baseDaSeta = setaGrande.localPosition;
    }

    public bool Perto => jogador != null && Distancia < raioDeConversa;

    public float Distancia
    {
        get
        {
            if (jogador == null) return 999f;
            Vector3 d = jogador.position - transform.position;
            d.y = 0f;
            return d.magnitude;
        }
    }

    public void Marcar(bool exclama, bool seta)
    {
        if (exclamacao != null && exclamacao.gameObject.activeSelf != exclama) exclamacao.gameObject.SetActive(exclama);
        if (setaGrande != null && setaGrande.gameObject.activeSelf != seta) setaGrande.gameObject.SetActive(seta);
    }

    public void Acenar()
    {
        if (animador == null) return;
        foreach (string estado in new[] { "Victory", "emote-yes" })
        {
            if (animador.HasState(0, Animator.StringToHash(estado)))
            {
                animador.CrossFade(estado, 0.1f);
                acenando = 1.6f;
                return;
            }
        }
    }

    private void Update()
    {
        if (acenando > 0f)
        {
            acenando -= Time.deltaTime;
            if (acenando <= 0f && animador != null)
            {
                foreach (string estado in new[] { "Locomocao", "idle" })
                {
                    if (animador.HasState(0, Animator.StringToHash(estado)))
                    {
                        animador.CrossFade(estado, 0.2f);
                        break;
                    }
                }
            }
        }

        // Vira pro jogador quando ele está perto; senão, volta pro olhar de sempre.
        Vector3 alvo = olharPadrao;
        if (jogador != null && Distancia < raioDeConversa * 2f)
        {
            alvo = jogador.position - transform.position;
            alvo.y = 0f;
        }
        if (alvo.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(alvo), 5f * Time.deltaTime);
        }

        float t = Time.time;
        if (exclamacao != null && exclamacao.gameObject.activeSelf)
        {
            exclamacao.localPosition = baseDaExclamacao + Vector3.up * Mathf.Sin(t * 3f) * 0.12f;
            exclamacao.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        }
        if (setaGrande != null && setaGrande.gameObject.activeSelf)
        {
            setaGrande.localPosition = baseDaSeta + Vector3.up * Mathf.Abs(Mathf.Sin(t * 3.5f)) * 0.5f;
        }
    }
}
