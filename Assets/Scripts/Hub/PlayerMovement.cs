using UnityEngine;

// Jogador do Hub: setas/WASD andam em relação ao olhar, que é escolhido com o mouse (CameraFollow.Giro).
// "Pra cima" vai pra onde o personagem olha, "pros lados" anda de lado. Andando, ele vira pro lado
// em que anda; parado, vira pra onde o mouse aponta.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    [Tooltip("Quão rápido o personagem vira (maior = mais seco).")]
    public float velocidadeDeVirar = 12f;

    private CharacterController controller;
    private CameraFollow olhar;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        foreach (CameraFollow cameraFollow in FindObjectsByType<CameraFollow>())
        {
            if (cameraFollow.target == transform)
            {
                olhar = cameraFollow;
            }
        }
    }

    private void Update()
    {
        float horizontal = Teclado.Eixo("Horizontal");
        float vertical = Teclado.Eixo("Vertical");

        Quaternion giro = Quaternion.Euler(0f, olhar != null ? olhar.Giro : 0f, 0f);
        Vector3 move = giro * new Vector3(horizontal, 0f, vertical);
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        Vector3 frente = move.sqrMagnitude > 0.0001f ? move : giro * Vector3.forward;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(frente), velocidadeDeVirar * Time.deltaTime);

        controller.Move(move * moveSpeed * Time.deltaTime);
    }
}
