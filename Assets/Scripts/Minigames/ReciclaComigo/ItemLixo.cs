using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Item de lixo que o jogador arrasta até a lixeira certa.
[RequireComponent(typeof(CanvasGroup))]
public class ItemLixo : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string tipo;
    public float duracaoSumir = 0.3f;

    // Marcado pela lixeira quando o item é solto no lugar errado; conta pra pontuação de primeira tentativa.
    [HideInInspector] public bool errou;
    [HideInInspector] public bool descartado;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private Vector2 posicaoInicial;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void Configurar(string nome, string tipoDoLixo, Color cor, Vector2 posicao)
    {
        tipo = tipoDoLixo;
        GetComponent<Image>().color = cor;
        GetComponentInChildren<Text>().text = nome;
        rectTransform.anchoredPosition = posicao;
        posicaoInicial = posicao;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (descartado)
        {
            return;
        }

        // Sem bloquear raycasts, a lixeira embaixo do item consegue receber o OnDrop.
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (descartado)
        {
            return;
        }

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    // Chamado depois do OnDrop da lixeira, então aqui já se sabe se o item foi descartado.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (descartado)
        {
            return;
        }

        canvasGroup.blocksRaycasts = true;
        rectTransform.anchoredPosition = posicaoInicial;
    }

    public void Descartar(RectTransform lixeira)
    {
        descartado = true;
        canvasGroup.blocksRaycasts = false;
        StartCoroutine(Sumir(lixeira.position));
    }

    // Encolhe e desliza pra dentro da lixeira, depois se destrói.
    private IEnumerator Sumir(Vector3 destino)
    {
        Vector3 origem = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracaoSumir;
            transform.position = Vector3.Lerp(origem, destino, t);
            transform.localScale = Vector3.one * (1f - t);
            canvasGroup.alpha = 1f - t;
            yield return null;
        }

        Destroy(gameObject);
    }
}
