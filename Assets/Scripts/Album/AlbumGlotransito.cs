using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Cena Album_Glotransito: as figurinhas dos verbetes em 5 abas (uma por GrupoDeVerbete), com o total
// desbloqueado no topo. Lê tudo do perfil ativo (inclusive o Visitante, que perde tudo ao fechar o jogo).
// A cena é montada por AlbumConstrutor.cs; não editar à mão.
public class AlbumGlotransito : MonoBehaviour
{
    [Header("Abas, na ordem do GrupoDeVerbete")]
    public Button[] abas;
    public Image[] frentesDasAbas;
    public Color corDaAbaAberta = Color.yellow;
    public Color corDaAbaFechada = Color.cyan;

    [Header("Figurinhas")]
    public RectTransform conteudo;
    // Fica desligado na cena; cada figurinha é uma cópia dele.
    public FigurinhaDoAlbum modeloDeFigurinha;

    [Header("Progresso")]
    public Text textoDoProgresso;
    public RectTransform barraDeProgresso;

    [Header("Recado embaixo")]
    public Text textoDaDica;
    public string dicaPadrao = "Toque numa figurinha colorida para ver o verbete.";
    public string dicaDeBloqueada = "Essa figurinha ainda está escondida. Jogue para encontrar!";

    private readonly List<FigurinhaDoAlbum> figurinhas = new List<FigurinhaDoAlbum>();
    private int abaAberta;

    private void OnEnable()
    {
        GameProgress.VerbetesMudaram += Atualizar;
    }

    private void OnDisable()
    {
        GameProgress.VerbetesMudaram -= Atualizar;
    }

    private void Start()
    {
        AbrirAba(0);
    }

    // Botões das abas.
    public void AbrirAba(int indice)
    {
        abaAberta = Mathf.Clamp(indice, 0, abas.Length - 1);
        textoDaDica.text = dicaPadrao;
        Atualizar();
    }

    // Refaz as figurinhas da aba aberta e o total do topo.
    public void Atualizar()
    {
        for (int i = 0; i < frentesDasAbas.Length; i++)
        {
            frentesDasAbas[i].color = i == abaAberta ? corDaAbaAberta : corDaAbaFechada;
        }

        foreach (FigurinhaDoAlbum f in figurinhas)
        {
            Destroy(f.gameObject);
        }
        figurinhas.Clear();

        foreach (Verbete v in VerbeteDatabase.Instancia.ListarPorGrupo((GrupoDeVerbete)abaAberta))
        {
            FigurinhaDoAlbum f = Instantiate(modeloDeFigurinha, conteudo);
            f.name = "Figurinha_" + v.id;
            f.gameObject.SetActive(true);
            f.Preencher(v, GameProgress.VerbeteDesbloqueado(v.id), this);
            figurinhas.Add(f);
        }

        int total = VerbeteDatabase.Instancia.Quantidade;
        int desbloqueados = GameProgress.ContarVerbetesDesbloqueados();
        textoDoProgresso.text = desbloqueados + " de " + total + " verbetes";
        barraDeProgresso.gameObject.SetActive(desbloqueados > 0);
        barraDeProgresso.anchorMax = new Vector2(total > 0 ? desbloqueados / (float)total : 0f, 1f);
        barraDeProgresso.offsetMax = Vector2.zero;
    }

    // Chamado pela figurinha tocada.
    public void Tocar(FigurinhaDoAlbum figurinha)
    {
        if (CartaoVerbete.EstaAberto)
        {
            return;
        }

        if (figurinha.Desbloqueado)
        {
            textoDaDica.text = dicaPadrao;
            Vector3 naTela = RectTransformUtility.WorldToScreenPoint(null, figurinha.transform.position);
            CartaoVerbete.Mostrar(figurinha.Verbete.id, null, naTela);
        }
        else
        {
            // Não é erro: só explica por que não abre, e a figurinha balança.
            textoDaDica.text = dicaDeBloqueada;
            StartCoroutine(Balancar(figurinha.transform));
        }
    }

    private IEnumerator Balancar(Transform alvo)
    {
        float t = 0f;
        while (t < 1f && alvo != null)
        {
            t += Time.unscaledDeltaTime / 0.35f;
            alvo.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * Mathf.PI * 4f) * 6f * (1f - t));
            yield return null;
        }
        if (alvo != null)
        {
            alvo.localEulerAngles = Vector3.zero;
        }
    }
}
