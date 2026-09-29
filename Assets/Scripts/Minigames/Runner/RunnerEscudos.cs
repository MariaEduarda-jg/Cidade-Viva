using UnityEngine;
using UnityEngine.UI;

// Escudos do jogador (no lugar de vidas). Cada ícone apagado é um escudo perdido.
public class RunnerEscudos : MonoBehaviour
{
    public int maximo = 3;
    public Image[] icones;
    public Color corDoIconeVazio = new Color(1f, 1f, 1f, 0.25f);

    private int restantes;
    private Color[] coresOriginais;

    public int Restantes => restantes;

    private void Awake()
    {
        restantes = maximo;
        coresOriginais = new Color[icones.Length];
        for (int i = 0; i < icones.Length; i++)
        {
            coresOriginais[i] = icones[i].color;
        }

        AtualizarIcones();
    }

    public void Perder()
    {
        restantes = Mathf.Max(0, restantes - 1);
        AtualizarIcones();
    }

    // Para power-ups como o capacete do Pedal Seguro.
    public void Ganhar()
    {
        restantes = Mathf.Min(maximo, restantes + 1);
        AtualizarIcones();
    }

    private void AtualizarIcones()
    {
        for (int i = 0; i < icones.Length; i++)
        {
            icones[i].color = i < restantes ? coresOriginais[i] : corDoIconeVazio;
        }
    }
}
