using UnityEngine;

// Carrinho que anda da esquerda pra direita na sua raia e reaparece do lado esquerdo.
public class Carrinho : MonoBehaviour
{
    public float velocidade = 300f;

    private RectTransform rectTransform;
    private RectTransform area;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        area = (RectTransform)transform.parent;
    }

    private void Update()
    {
        float limite = area.rect.width / 2f + rectTransform.rect.width / 2f;
        Vector2 pos = rectTransform.anchoredPosition;
        pos.x += velocidade * Time.deltaTime;
        if (pos.x > limite)
        {
            pos.x = -limite;
        }

        rectTransform.anchoredPosition = pos;
    }
}
