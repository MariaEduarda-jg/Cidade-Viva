using UnityEngine;

// Define as faixas fixas da pista (centralizadas no pai) e a posição X de cada uma.
public class RunnerFaixas : MonoBehaviour
{
    public int quantidade = 3;
    public float larguraDaFaixa = 200f;

    public float PosicaoX(int faixa)
    {
        return (faixa - (quantidade - 1) / 2f) * larguraDaFaixa;
    }

    public int FaixaAleatoria()
    {
        return Random.Range(0, quantidade);
    }
}
