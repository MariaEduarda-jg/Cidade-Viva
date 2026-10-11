using UnityEngine;

// Liga o movimento do jogador no Hub à animação do personagem (parado / andando / correndo).
[RequireComponent(typeof(CharacterController))]
public class AnimacaoDoJogador : MonoBehaviour
{
    public Animator animator;
    [Tooltip("Velocidade (m/s) em que a animação já é de corrida.")]
    public float velocidadeDeCorrida = 5f;

    private static readonly int Velocidade = Animator.StringToHash("Velocidade");
    private Vector3 posicaoAnterior;

    private void OnEnable()
    {
        posicaoAnterior = transform.position;
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        // Mede pelo deslocamento real no chão (e não por CharacterController.velocity, que só
        // vale pro último Move): funciona igual com teclado, toque ou qualquer outro jeito de mover.
        Vector3 deslocamento = transform.position - posicaoAnterior;
        deslocamento.y = 0f;
        posicaoAnterior = transform.position;
        if (Time.deltaTime <= 0f)
        {
            return;
        }

        float metrosPorSegundo = deslocamento.magnitude / Time.deltaTime;
        float alvo = Mathf.Clamp01(metrosPorSegundo / Mathf.Max(velocidadeDeCorrida, 0.01f));
        animator.SetFloat(Velocidade, alvo, 0.08f, Time.deltaTime);
    }
}
