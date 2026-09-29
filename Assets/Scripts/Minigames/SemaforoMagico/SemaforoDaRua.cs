using UnityEngine;
using UnityEngine.UI;

// Semáforo que fica na rua: acende uma cor por vez (as outras ficam escurecidas).
public class SemaforoDaRua : MonoBehaviour
{
    public Image luzVermelha;
    public Image luzAmarela;
    public Image luzVerde;
    [Range(0f, 1f)] public float brilhoApagada = 0.25f;

    private Color corVermelha;
    private Color corAmarela;
    private Color corVerde;

    private void Awake()
    {
        corVermelha = luzVermelha.color;
        corAmarela = luzAmarela.color;
        corVerde = luzVerde.color;
    }

    // null apaga todas.
    public void Acender(CorDoSemaforo? cor)
    {
        luzVermelha.color = Brilho(corVermelha, cor == CorDoSemaforo.Vermelho);
        luzAmarela.color = Brilho(corAmarela, cor == CorDoSemaforo.Amarelo);
        luzVerde.color = Brilho(corVerde, cor == CorDoSemaforo.Verde);
    }

    private Color Brilho(Color original, bool acesa)
    {
        return acesa ? original : new Color(original.r * brilhoApagada, original.g * brilhoApagada, original.b * brilhoApagada, 1f);
    }
}
