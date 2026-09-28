using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(horizontal, 0f, vertical);
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        if (move.sqrMagnitude > 0.0001f)
        {
            transform.forward = move;
        }

        controller.Move(move * moveSpeed * Time.deltaTime);
    }
}
