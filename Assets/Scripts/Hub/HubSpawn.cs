using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HubSpawn : MonoBehaviour
{
    public Transform pontoDeRetorno;

    private void Start()
    {
        if (!VoltarAoHub.voltandoDeMinigame || pontoDeRetorno == null)
        {
            return;
        }

        VoltarAoHub.voltandoDeMinigame = false;

        // O CharacterController sobrescreve mudanças diretas de posição enquanto está ativo, socorroo
        CharacterController controller = GetComponent<CharacterController>();
        controller.enabled = false;
        transform.SetPositionAndRotation(pontoDeRetorno.position, pontoDeRetorno.rotation);
        controller.enabled = true;

        // Posiciona a câmera direto no jogador, sem atravessar o mapa deslizando.
        foreach (CameraFollow cameraFollow in FindObjectsByType<CameraFollow>())
        {
            if (cameraFollow.target == transform)
            {
                cameraFollow.transform.position = transform.position + cameraFollow.offset;
                cameraFollow.transform.LookAt(transform.position + Vector3.up * cameraFollow.lookHeightOffset);
            }
        }
    }
}
