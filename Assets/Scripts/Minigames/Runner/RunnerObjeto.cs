using UnityEngine;
using UnityEngine.UI;

public enum TipoRunnerObjeto
{
    Obstaculo,
    Coletavel,
    Protecao
}

// Algo que desce pela pista: obstáculo (tira escudo), coletável (dá pontos)
// ou proteção (power-up que segura uma batida).
// Criado pelo RunnerSpawner, na mesma pista (pai) do jogador. O que ele é e como aparece
// (tipo, pontos, cor, rótulo) vem da entrada da RunnerConfig com o mesmo id.
public class RunnerObjeto : MonoBehaviour
{
    public string id;
    // Opcional: texto em cima do objeto.
    public Text rotulo;

    [HideInInspector] public RunnerManager manager;

    private RectTransform rectTransform;
    private RectTransform pista;

    public TipoRunnerObjeto Tipo { get; private set; }
    public int Pontos { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        pista = (RectTransform)transform.parent;
    }

    public void Aplicar(RunnerObjetoConfig dados)
    {
        if (dados == null)
        {
            Debug.LogWarning("RunnerObjeto '" + id + "' não tem entrada na RunnerConfig; vai ser tratado como obstáculo.", this);
            return;
        }

        Tipo = dados.tipo;
        Pontos = dados.pontos;

        Graphic grafico = GetComponent<Graphic>();
        if (grafico != null)
        {
            grafico.color = dados.cor;
        }

        if (rotulo != null)
        {
            rotulo.text = dados.rotulo;
        }
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
