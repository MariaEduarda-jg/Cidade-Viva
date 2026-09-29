using UnityEngine;

// Personagem do "Sobe, Sobe!": pula sozinho ao cair em cima de uma plataforma;
// o jogador só controla o movimento horizontal (setas ou A/D).
public class SobeJogador : MonoBehaviour
{
    public SobeSobeManager manager;
    public float velocidadeHorizontal = 650f;
    public float forcaDoPulo = 1150f;
    public float gravidade = 2000f;

    private RectTransform rectTransform;
    private float velocidadeVertical;

    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (!manager.Jogando)
        {
            return;
        }

        float dt = Time.deltaTime;
        Vector2 tamanho = rectTransform.rect.size;
        Vector2 pos = rectTransform.anchoredPosition;

        float limiteX = manager.larguraDaArea / 2f - tamanho.x / 2f;
        pos.x = Mathf.Clamp(pos.x + Input.GetAxisRaw("Horizontal") * velocidadeHorizontal * dt, -limiteX, limiteX);

        float peAntes = pos.y - tamanho.y / 2f;
        velocidadeVertical -= gravidade * dt;
        pos.y += velocidadeVertical * dt;

        // Só quica descendo, pra atravessar as plataformas por baixo ao subir.
        if (velocidadeVertical <= 0f)
        {
            RectTransform plataforma = manager.PlataformaEmbaixo(pos.x, tamanho.x, peAntes, pos.y - tamanho.y / 2f);
            if (plataforma != null)
            {
                pos.y = plataforma.anchoredPosition.y + plataforma.rect.height / 2f + tamanho.y / 2f;
                velocidadeVertical = forcaDoPulo;
            }
        }

        rectTransform.anchoredPosition = pos;
    }
}
