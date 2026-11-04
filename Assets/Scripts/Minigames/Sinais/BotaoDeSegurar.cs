using UnityEngine;
using UnityEngine.EventSystems;

// Botão de segurar do Motorista Mirim (acelerar ou frear): vale enquanto o dedo está em cima.
public class BotaoDeSegurar : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public SinaisMotorista motorista;
    public bool acelera;

    public void OnPointerDown(PointerEventData dados)
    {
        Segurar(true);
    }

    public void OnPointerUp(PointerEventData dados)
    {
        Segurar(false);
    }

    public void OnPointerExit(PointerEventData dados)
    {
        Segurar(false);
    }

    private void OnDisable()
    {
        Segurar(false);
    }

    private void Segurar(bool segurando)
    {
        if (motorista == null)
        {
            return;
        }
        if (acelera)
        {
            motorista.Acelerar(segurando);
        }
        else
        {
            motorista.Frear(segurando);
        }
    }
}
