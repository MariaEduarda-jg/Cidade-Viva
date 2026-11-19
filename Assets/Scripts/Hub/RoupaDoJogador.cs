using UnityEngine;

// Veste no personagem do Hub o que o perfil escolheu na loja: a cor da camiseta (material 0 do corpo)
// e um acessório na cabeça (preso ao osso "Head", então acompanha a animação).
public class RoupaDoJogador : MonoBehaviour
{
    public Transform visual;
    public SkinnedMeshRenderer corpo;
    [Tooltip("Qual material do corpo é a camiseta.")]
    public int indiceDaCamiseta;
    public Transform cabeca;

    private Material camisetaOriginal;
    private GameObject acessorio;
    private string acessorioAtual = "";

    private void Awake()
    {
        if (corpo != null) camisetaOriginal = corpo.sharedMaterials[indiceDaCamiseta];
    }

    private void OnEnable()
    {
        Loja.Mudou += Aplicar;
    }

    private void OnDisable()
    {
        Loja.Mudou -= Aplicar;
    }

    private void Start()
    {
        Aplicar();
    }

    public void Aplicar()
    {
        if (corpo != null)
        {
            ItemDaLoja roupa = Loja.RoupaVestida;
            Material[] mats = corpo.sharedMaterials;
            mats[indiceDaCamiseta] = roupa != null && roupa.corDaRoupa != null ? roupa.corDaRoupa : camisetaOriginal;
            corpo.sharedMaterials = mats;
        }

        ItemDaLoja item = Loja.AcessorioVestido;
        string id = item != null ? item.id : "";
        if (id == acessorioAtual)
        {
            return;
        }
        if (acessorio != null) Destroy(acessorio);
        acessorio = null;
        acessorioAtual = id;
        if (item == null || item.acessorio == null || cabeca == null)
        {
            return;
        }
        // Posiciona no espaço do personagem (frente = +Z do visual) e prende no osso da cabeça.
        acessorio = Instantiate(item.acessorio);
        acessorio.name = "Acessorio_" + item.id;
        acessorio.transform.SetPositionAndRotation(cabeca.position + visual.rotation * item.posicaoNaCabeca, visual.rotation);
        acessorio.transform.localScale = Vector3.Scale(acessorio.transform.localScale, visual.lossyScale);
        acessorio.transform.SetParent(cabeca, true);
    }

    public string AcessorioNaCabeca => acessorio != null ? acessorioAtual : "";
    public Material CamisetaAgora => corpo != null ? corpo.sharedMaterials[indiceDaCamiseta] : null;
}
