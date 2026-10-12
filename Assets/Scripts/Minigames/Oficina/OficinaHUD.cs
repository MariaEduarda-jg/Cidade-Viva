using UnityEngine;
using UnityEngine.UI;

// Interface da Oficina de Veículos. Só apresentação: quem decide tudo é o OficinaManager.
public class OficinaHUD : MonoBehaviour
{
    [Header("Verbete (em cima, no meio)")]
    public Text textoDaPalavra;
    // Recebe a definição do glossário, sem alteração.
    public Text textoDaDefinicao;
    public GameObject painelDaDefinicao;
    // Ilustração do verbete no glossário (some quando o verbete não tem).
    public Image ilustracao;
    public GameObject molduraDaIlustracao;

    [Header("Nível e estrelas (em cima, à esquerda)")]
    public Text textoDoNivel;
    // Na ordem: sem erro, no tempo, sem dica.
    public Image[] estrelasDosCriterios;
    public Text[] rotulosDosCriterios;
    public Color corGanha = Color.white;
    public Color corPerdida = new Color(0.4f, 0.45f, 0.5f, 0.35f);

    [Header("Tempo")]
    public GameObject painelDoTempo;
    public RectTransform barraDeTempo;

    [Header("Dica, recado e balão")]
    public GameObject painelDaDica;
    public Text textoDaDica;
    public Text textoDoRecado;
    public RectTransform balao;
    public Text textoDoBalao;
    // Nome da peça debaixo do mouse ou do dedo.
    public RectTransform etiqueta;
    public Text textoDaEtiqueta;

    [Header("Botões")]
    public GameObject controlesDaMontagem;
    public GameObject botaoDica;
    public GameObject botaoOuvir;
    public GameObject controlesDoTeste;
    // Dentro dos controles do teste: setas de virar, acelerar e o botão de engatar o reboque.
    public GameObject botoesDeVolante;
    public GameObject botaoAcelerar;
    public GameObject botaoEngatar;

    private float balaoAte;

    public void MostrarVerbete(string palavra, string definicao, string nivel, Sprite desenho)
    {
        textoDaPalavra.text = palavra;
        textoDaDefinicao.text = definicao;
        textoDoNivel.text = nivel;
        ilustracao.sprite = desenho;
        molduraDaIlustracao.SetActive(desenho != null);
    }

    // Mostra o nome da peça logo acima dela (posição em pixels da tela).
    public void Etiqueta(Vector2 naTela, string texto)
    {
        Vector2 local;
        var tela = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(tela, naTela, null, out local);
        Vector2 meio = tela.rect.size * 0.5f - etiqueta.sizeDelta * 0.5f - new Vector2(10f, 10f);
        local.y += 95f;
        etiqueta.anchoredPosition = new Vector2(Mathf.Clamp(local.x, -meio.x, meio.x), Mathf.Clamp(local.y, -meio.y, meio.y));
        textoDaEtiqueta.text = texto;
        etiqueta.gameObject.SetActive(true);
    }

    public void EsconderEtiqueta()
    {
        etiqueta.gameObject.SetActive(false);
    }

    public void Recado(string texto)
    {
        textoDoRecado.text = texto;
    }

    public void Dica(string texto)
    {
        painelDaDica.SetActive(!string.IsNullOrEmpty(texto));
        textoDaDica.text = texto ?? "";
    }

    // Os três critérios das estrelas. Sem limite de tempo no nível, a estrela "No tempo" já aparece ganha.
    public void Criterios(bool semErro, bool noTempo, bool semDica, string[] rotulos)
    {
        bool[] ganhos = { semErro, noTempo, semDica };
        for (int i = 0; i < estrelasDosCriterios.Length && i < ganhos.Length; i++)
        {
            estrelasDosCriterios[i].color = ganhos[i] ? corGanha : corPerdida;
            if (rotulos != null && i < rotulos.Length && i < rotulosDosCriterios.Length)
            {
                rotulosDosCriterios[i].text = rotulos[i];
            }
        }
    }

    // fracao: 1 = tempo cheio, 0 = acabou.
    public void Tempo(bool temTempo, float fracao)
    {
        painelDoTempo.SetActive(temTempo);
        barraDeTempo.anchorMax = new Vector2(Mathf.Clamp01(fracao), 1f);
        barraDeTempo.offsetMax = Vector2.zero;
    }

    // Balãozinho de explicação, perto de onde a peça voltou (posição em pixels da tela).
    public void Balao(Vector2 naTela, string texto, float duracao)
    {
        Vector2 local;
        var tela = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(tela, naTela, null, out local);
        // Não deixa o balão sair da tela.
        Vector2 meio = tela.rect.size * 0.5f - balao.sizeDelta * 0.5f - new Vector2(20f, 20f);
        local.y += balao.sizeDelta.y * 0.75f;
        balao.anchoredPosition = new Vector2(Mathf.Clamp(local.x, -meio.x, meio.x), Mathf.Clamp(local.y, -meio.y, meio.y));
        textoDoBalao.text = texto;
        balao.gameObject.SetActive(true);
        balaoAte = Time.time + duracao;
    }

    public void EsconderBalao()
    {
        balao.gameObject.SetActive(false);
    }

    public void ModoMontagem(bool temBotaoDeDica, bool temVoz)
    {
        painelDaDefinicao.SetActive(true);
        controlesDaMontagem.SetActive(true);
        controlesDoTeste.SetActive(false);
        botaoDica.SetActive(temBotaoDeDica);
        botaoOuvir.SetActive(temVoz);
    }

    public void ModoTeste()
    {
        // A definição cobriria o fundo da pista; ela volta no cartão do verbete, logo depois.
        painelDaDefinicao.SetActive(false);
        controlesDaMontagem.SetActive(false);
        controlesDoTeste.SetActive(true);
        painelDaDica.SetActive(false);
        painelDoTempo.SetActive(false);
        EsconderBalao();
        EsconderEtiqueta();
    }

    // esperandoEngate: só o botão "Engatar" (reboque parado). comVolante: falso nos trilhos, onde não se vira.
    public void ControlesDaPista(bool esperandoEngate, bool comVolante)
    {
        botaoEngatar.SetActive(esperandoEngate);
        botaoAcelerar.SetActive(!esperandoEngate);
        botoesDeVolante.SetActive(!esperandoEngate && comVolante);
    }

    private void Update()
    {
        if (balao.gameObject.activeSelf && Time.time >= balaoAte)
        {
            balao.gameObject.SetActive(false);
        }
    }
}
