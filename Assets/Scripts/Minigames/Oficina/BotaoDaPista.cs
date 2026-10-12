using UnityEngine;
using UnityEngine.EventSystems;

// Botão de segurar do teste na pista (virar ou acelerar): vale enquanto o dedo ou o mouse está em cima.
public class BotaoDaPista : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public OficinaPista pista;
    [Tooltip("-1 = virar à esquerda, +1 = virar à direita, 0 = este botão não vira.")]
    public float direcao;
    public bool acelera;

    public void OnPointerDown(PointerEventData dados)
    {
        Aplicar(true);
    }

    public void OnPointerUp(PointerEventData dados)
    {
        Aplicar(false);
    }

    public void OnPointerExit(PointerEventData dados)
    {
        Aplicar(false);
    }

    private void OnDisable()
    {
        Aplicar(false);
    }

    private void Aplicar(bool segurando)
    {
        if (pista == null)
        {
            return;
        }
        if (acelera)
        {
            pista.Acelerar(segurando);
        }
        if (direcao != 0f)
        {
            pista.DefinirDirecao(segurando ? direcao : 0f);
        }
    }
}
