using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Contador de moedas e painel da loja no Hub (gerados pelo MenuConstrutor.PainelDoHub).
// Cada item do catálogo vira um cartão (copiado do modelo inativo): figura, nome, preço e um botão
// que compra, veste ou tira. Com o painel aberto o jogo fica parado (Time.timeScale = 0).
public class LojaDoHub : MonoBehaviour
{
    public Text contadorDoHub;
    public GameObject painel;
    public Text moedasDoPainel;
    public Transform grade;
    public GameObject modeloDeItem;
    public Text mensagem;

    [Header("Textos")]
    public string textoComprar = "Comprar";
    public string textoVestir = "Vestir";
    public string textoTirar = "Tirar";
    [Tooltip("{0} = preço.")]
    public string textoPreco = "{0}";
    public string textoComprado = "Comprou! Toque em Vestir.";
    [Tooltip("{0} = moedas que faltam.")]
    public string textoFaltam = "Faltam {0} moedas. Ganhe estrelas novas nos jogos!";
    public string textoAjuda = "Cada estrela nova nos jogos vale 10 moedas.";
    public Color corComprar = new Color(0.33f, 0.6f, 0.35f);
    public Color corVestir = new Color(0.2f, 0.55f, 0.6f);
    public Color corTirar = new Color(0.55f, 0.55f, 0.6f);

    public bool Aberta => painel != null && painel.activeSelf;

    private readonly Dictionary<string, GameObject> cartoes = new Dictionary<string, GameObject>();

    private void OnEnable()
    {
        Loja.Mudou += Atualizar;
    }

    private void OnDisable()
    {
        Loja.Mudou -= Atualizar;
        if (Aberta) Time.timeScale = 1f;
    }

    private void Start()
    {
        painel.SetActive(false);
        modeloDeItem.SetActive(false);
        Atualizar();
    }

    private void Update()
    {
        if (Aberta && Teclado.Apertou(KeyCode.Escape)) Fechar();
    }

    public void Abrir()
    {
        if (cartoes.Count == 0) Montar();
        // Por cima de tudo do Hub (a linha de ajuda e o aviso vêm depois no Canvas).
        painel.transform.SetAsLastSibling();
        painel.SetActive(true);
        mensagem.text = textoAjuda;
        Time.timeScale = 0f;
        Atualizar();
    }

    public void Fechar()
    {
        painel.SetActive(false);
        Time.timeScale = 1f;
    }

    // Botão do cartão: compra se ainda não tem; se tem, veste ou tira.
    public void Tocar(string id)
    {
        ItemDaLoja item = Loja.Catalogo.Buscar(id);
        if (item == null) return;
        if (!Loja.Tem(id))
        {
            switch (Loja.Comprar(id))
            {
                case Loja.Resultado.Comprou: mensagem.text = textoComprado; break;
                case Loja.Resultado.FaltamMoedas: mensagem.text = string.Format(textoFaltam, item.preco - Loja.Moedas); break;
            }
        }
        else
        {
            Loja.VestirOuTirar(id);
            mensagem.text = item.nome;
        }
        Atualizar();
    }

    private void Montar()
    {
        foreach (ItemDaLoja item in Loja.Catalogo.itens)
        {
            GameObject c = Instantiate(modeloDeItem, grade);
            c.name = "Item_" + item.id;
            c.SetActive(true);
            c.transform.Find("Icone").GetComponent<Image>().sprite = item.icone;
            c.transform.Find("Nome").GetComponent<Text>().text = item.nome;
            c.transform.Find("Preco/Texto").GetComponent<Text>().text = string.Format(textoPreco, item.preco);
            string id = item.id;
            c.transform.Find("Botao").GetComponent<Button>().onClick.AddListener(() => Tocar(id));
            cartoes[id] = c;
        }
    }

    public void Atualizar()
    {
        if (contadorDoHub != null) contadorDoHub.text = Loja.Moedas.ToString();
        if (moedasDoPainel != null) moedasDoPainel.text = Loja.Moedas.ToString();
        foreach (var par in cartoes)
        {
            bool tem = Loja.Tem(par.Key);
            bool vestido = Loja.Vestido(par.Key);
            Transform botao = par.Value.transform.Find("Botao");
            botao.Find("Frente/Texto").GetComponent<Text>().text = !tem ? textoComprar : (vestido ? textoTirar : textoVestir);
            botao.Find("Frente").GetComponent<Image>().color = !tem ? corComprar : (vestido ? corTirar : corVestir);
            par.Value.transform.Find("Preco").gameObject.SetActive(!tem);
            par.Value.transform.Find("Vestido").gameObject.SetActive(vestido);
        }
    }

    // Pro piloto: o texto do botão de um item.
    public string TextoDoBotao(string id)
    {
        return cartoes.TryGetValue(id, out GameObject c) ? c.transform.Find("Botao/Frente/Texto").GetComponent<Text>().text : "";
    }
}
