using UnityEngine;

// O caminho de um nível da Pedalada (gerado pelo construtor a partir do roteiro da config).
// Espaço da rota: origem no começo do caminho, +Z = pra frente, faixas em x = -larg, 0, +larg.
public class PedaladaRota : MonoBehaviour
{
    [Tooltip("Um por trecho: o cone/buraco no meio do trecho (nulo se o trecho não tem).")]
    public Transform[] obstaculos = new Transform[0];
    [Tooltip("Carros parados do engarrafamento (com a ambulância e o ônibus).")]
    public Transform[] engarrafamento = new Transform[0];
    public Transform ambulancia;
    [Tooltip("Luzes da sirene da ambulância (piscam).")]
    public Renderer[] luzesDaSirene = new Renderer[0];
    public Transform[] pedestres = new Transform[0];
    public Transform setaDaDica;

    public Vector3 Mundo(float x, float z)
    {
        return transform.TransformPoint(new Vector3(x, 0f, z));
    }
}
