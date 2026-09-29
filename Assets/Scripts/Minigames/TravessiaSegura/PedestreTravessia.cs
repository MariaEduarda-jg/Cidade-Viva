using UnityEngine;

// Personagem que atravessa a rua uma raia por vez (espaço ou seta para cima).
// Se encostar num carrinho, volta para a calçada inicial.
public class PedestreTravessia : MonoBehaviour
{
    public TravessiaManager manager;
    public Carrinho[] carrinhos;
    public float tamanhoDoPasso = 120f;
    public int passosAteOOutroLado = 4;
    public float duracaoDoPasso = 0.15f;

    private RectTransform rectTransform;
    private Vector2 posicaoInicial;
    private int passoAtual;
    private float destinoY;
    private bool terminou;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        posicaoInicial = rectTransform.anchoredPosition;
        destinoY = posicaoInicial.y;
    }

    private void Update()
    {
        if (terminou)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            Andar();
        }

        // Desliza até o destino do passo, pra não "teletransportar" entre raias.
        Vector2 pos = rectTransform.anchoredPosition;
        pos.y = Mathf.MoveTowards(pos.y, destinoY, tamanhoDoPasso / duracaoDoPasso * Time.deltaTime);
        rectTransform.anchoredPosition = pos;

        if (EncostouEmCarrinho())
        {
            VoltarAoInicio();
            manager.RegistrarBatida();
            return;
        }

        if (passoAtual >= passosAteOOutroLado && Mathf.Approximately(pos.y, destinoY))
        {
            terminou = true;
            manager.TravessiaCompleta();
        }
    }

    private void Andar()
    {
        if (passoAtual >= passosAteOOutroLado)
        {
            return;
        }

        passoAtual++;
        destinoY = posicaoInicial.y + passoAtual * tamanhoDoPasso;
        manager.IniciarCronometro();
    }

    private void VoltarAoInicio()
    {
        passoAtual = 0;
        destinoY = posicaoInicial.y;
        rectTransform.anchoredPosition = posicaoInicial;
    }

    private bool EncostouEmCarrinho()
    {
        Rect meu = RectNoPai(rectTransform);
        foreach (Carrinho carrinho in carrinhos)
        {
            if (meu.Overlaps(RectNoPai((RectTransform)carrinho.transform)))
            {
                return true;
            }
        }

        return false;
    }

    // Pedestre e carrinhos ficam no mesmo pai, então dá pra comparar as posições direto.
    private static Rect RectNoPai(RectTransform rt)
    {
        Vector2 tamanho = rt.rect.size;
        return new Rect(rt.anchoredPosition - tamanho / 2f, tamanho);
    }
}
