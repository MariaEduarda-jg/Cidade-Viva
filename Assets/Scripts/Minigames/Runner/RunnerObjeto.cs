using UnityEngine;

public enum TipoRunnerObjeto
{
    Obstaculo,
    Coletavel,
    Protecao
}

// Algo que desce pela pista: obstáculo (tira escudo), coletável (dá pontos)
// ou proteção (power-up que segura uma batida).
// Criado pelo RunnerSpawner, na mesma pista (pai) do jogador.
public class RunnerObjeto : MonoBehaviour
{
    public TipoRunnerObjeto tipo = TipoRunnerObjeto.Obstaculo;
    public int pontos = 10;

    [HideInInspector] public RunnerManager manager;

    private RectTransform rectTransform;
    private RectTransform pista;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        pista = (RectTransform)transform.parent;
    }

    private void Update()
    {
        if (!manager.Jogando)
        {
            return;
        }

        Vector2 pos = rectTransform.anchoredPosition;
        pos.y -= manager.Velocidade * Time.deltaTime;
        rectTransform.anchoredPosition = pos;

        if (pos.y < -(pista.rect.height / 2f + rectTransform.rect.height / 2f))
        {
            Destroy(gameObject);
            return;
        }

        if (RectNoPai(rectTransform).Overlaps(RectNoPai(manager.jogador.RectTransform)))
        {
            manager.Tocou(this);
        }
    }

    // Objeto e jogador ficam no mesmo pai, então dá pra comparar as posições direto.
    private static Rect RectNoPai(RectTransform rt)
    {
        Vector2 tamanho = rt.rect.size;
        return new Rect(rt.anchoredPosition - tamanho / 2f, tamanho);
    }
}
