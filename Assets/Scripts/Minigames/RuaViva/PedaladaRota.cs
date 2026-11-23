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

    [Header("Calçadas por onde os pedestres andam (espaço da rota)")]
    [Tooltip("Os pontos de todos os caminhos, um caminho depois do outro.")]
    public Vector3[] pontosDasCalcadas = new Vector3[0];
    [Tooltip("Onde cada caminho acaba em pontosDasCalcadas (índice exclusivo).")]
    public int[] fimDosCaminhos = new int[0];
    [Tooltip("Um por pedestre: em qual caminho ele anda, de onde parte (metros) e se anda de volta.")]
    public int[] caminhoDoPedestre = new int[0];
    public float[] partidaDoPedestre = new float[0];
    public bool[] pedestreDeVolta = new bool[0];
    public Transform setaDaDica;

    public Vector3 Mundo(float x, float z)
    {
        return transform.TransformPoint(new Vector3(x, 0f, z));
    }

    public int InicioDoCaminho(int c)
    {
        return c == 0 ? 0 : fimDosCaminhos[c - 1];
    }

    public float ComprimentoDoCaminho(int c)
    {
        float total = 0f;
        for (int i = InicioDoCaminho(c) + 1; i < fimDosCaminhos[c]; i++)
        {
            total += Vector3.Distance(pontosDasCalcadas[i - 1], pontosDasCalcadas[i]);
        }
        return total;
    }

    // Ponto (espaço da rota) a "d" metros do começo do caminho c, e a direção do caminho ali.
    public Vector3 PontoNoCaminho(int c, float d, out Vector3 direcao)
    {
        int ini = InicioDoCaminho(c), fim = fimDosCaminhos[c];
        direcao = Vector3.forward;
        for (int i = ini + 1; i < fim; i++)
        {
            Vector3 a = pontosDasCalcadas[i - 1], b = pontosDasCalcadas[i];
            float trecho = Vector3.Distance(a, b);
            if (trecho <= 0.0001f) continue;
            direcao = (b - a) / trecho;
            if (d <= trecho || i == fim - 1)
            {
                return a + direcao * Mathf.Min(d, trecho);
            }
            d -= trecho;
        }
        return pontosDasCalcadas[ini];
    }
}
