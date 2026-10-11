using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Placa de trânsito que o jogador arrasta até o alvo certo na rua.
[RequireComponent(typeof(CanvasGroup))]
public class PlacaArrastavel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string tipo;

    [HideInInspector] public bool encaixada;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private Vector2 posicaoInicial;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        posicaoInicial = rectTransform.anchoredPosition;
    }

    // Cor e texto vêm da config do minigame.
    public void Aplicar(PlacaConfig aparencia)
    {
        GetComponent<Graphic>().color = aparencia.cor;
        Text rotulo = GetComponentInChildren<Text>();
        if (rotulo != null)
        {
            rotulo.text = aparencia.rotulo;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (encaixada)
        {
            return;
        }

        // Sem bloquear raycasts, o alvo embaixo da placa consegue receber o OnDrop.
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (encaixada)
        {
            return;
        }

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    // Chamado depois do OnDrop do alvo, então aqui já se sabe se a placa encaixou.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (encaixada)
        {
            return;
        }

        canvasGroup.blocksRaycasts = true;
        rectTransform.anchoredPosition = posicaoInicial;
    }

    public void Encaixar(RectTransform alvo)
    {
        encaixada = true;
        canvasGroup.blocksRaycasts = true;
        rectTransform.position = alvo.position;
    }
}
