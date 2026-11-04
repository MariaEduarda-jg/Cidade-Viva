using UnityEngine;
using UnityEngine.EventSystems;

// A fivela do cinto: arrastar até o fecho. Perto o bastante, encaixa sozinha. Soltar longe só faz
// ela voltar pro lugar, sem contar erro.
public class FivelaArrastavel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RuaVivaCarroManager manager;
    public RectTransform fecho;
    [Tooltip("A correia, que estica do ponto de saída até a fivela.")]
    public RectTransform correia;
    public Canvas canvas;

    private RectTransform eu;
    private Vector2 inicio;
    private bool iniciada;

    private void Iniciar()
    {
        if (!iniciada)
        {
            iniciada = true;
            eu = (RectTransform)transform;
            inicio = eu.anchoredPosition;
        }
    }

    public void Voltar()
    {
        Iniciar();
        eu.anchoredPosition = inicio;
        EsticarCorreia();
    }

    public void OnBeginDrag(PointerEventData dados)
    {
        Iniciar();
    }

    public void OnDrag(PointerEventData dados)
    {
        Iniciar();
        eu.anchoredPosition += dados.delta / canvas.scaleFactor;
        EsticarCorreia();
        if (manager != null && Vector2.Distance(eu.anchoredPosition, fecho.anchoredPosition) <= manager.config.encaixeDaFivela)
        {
            eu.anchoredPosition = fecho.anchoredPosition;
            EsticarCorreia();
            dados.pointerDrag = null;
            manager.PrenderCinto();
        }
    }

    public void OnEndDrag(PointerEventData dados)
    {
        Iniciar();
        if (manager != null && manager.EsperandoCinto)
        {
            manager.SoltouAFivela();
            Voltar();
        }
    }

    // Leva a fivela até o fecho (teclado, ou encaixe chamado pelo jogo).
    public void Encaixar()
    {
        Iniciar();
        eu.anchoredPosition = fecho.anchoredPosition;
        EsticarCorreia();
    }

    private void EsticarCorreia()
    {
        if (correia == null)
        {
            return;
        }
        Vector2 de = correia.anchoredPosition;
        Vector2 ate = eu.anchoredPosition;
        Vector2 diferenca = ate - de;
        correia.sizeDelta = new Vector2(diferenca.magnitude, correia.sizeDelta.y);
        correia.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diferenca.y, diferenca.x) * Mathf.Rad2Deg);
    }
}
