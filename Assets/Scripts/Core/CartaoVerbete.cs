using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Cartão que apresenta um verbete do Glotrânsito: palavra, ilustração, definição, "Ouvir" e "Continuar".
// Qualquer cena abre com CartaoVerbete.Mostrar(id, aoFechar); o prefab é o Assets/Prefabs/UI_CartaoVerbete
// (gerado por AlbumConstrutor.cs; não editar à mão), que traz o próprio Canvas e fica por cima de tudo.
// A definição mostrada e narrada é a do glossário, do jeito que está no verbetes.json. Nunca reescrever.
public class CartaoVerbete : MonoBehaviour
{
    [Header("Peças do cartão")]
    public RectTransform cartao;
    public CanvasGroup fundo;
    public Text textoDaPalavra;
    public Image ilustracao;
    // Aparece no lugar da ilustração quando o glossário ainda não tem desenho pro verbete.
    public GameObject desenhoProvisorio;
    public Text textoDaDefinicao;
    public Button botaoOuvir;
    public Button botaoContinuar;
    // Selo "Novo!": só na primeira vez que a criança vê o verbete.
    public RectTransform seloNovo;

    [Header("Entrada")]
    public Image brilho;
    public RectTransform[] particulas;
    public float duracaoDaEntrada = 0.6f;
    public AudioClip somAoAbrir;
    public AudioClip somDeNovo;

    private static CartaoVerbete aberto;

    private Verbete verbete;
    private Action aoFechar;
    private IVozNarradora voz;
    private Vector2 posicaoFinal;
    private bool fechando;

    public static bool EstaAberto
    {
        get { return aberto != null; }
    }

    // Abre o cartão do verbete. Se a criança ainda não tinha esse verbete, ele é desbloqueado no perfil
    // ativo e o cartão mostra o selo "Novo!". "aoFechar" é chamado quando ela toca em "Continuar".
    public static void Mostrar(string idDoVerbete, Action aoFechar)
    {
        Mostrar(idDoVerbete, aoFechar, new Vector2(Screen.width * 0.5f, -Screen.height * 0.3f));
    }

    // Igual, mas o cartão sai voando de um ponto da tela (em pixels), ex.: a figurinha tocada no álbum.
    public static void Mostrar(string idDoVerbete, Action aoFechar, Vector2 origemNaTela)
    {
        VerbeteDatabase banco = VerbeteDatabase.Instancia;
        Verbete v = banco.Buscar(idDoVerbete);
        if (v == null || banco.prefabDoCartao == null)
        {
            Debug.LogWarning(v == null ? "Verbete desconhecido: " + idDoVerbete : "VerbeteDatabase sem o prefab UI_CartaoVerbete.");
            if (aoFechar != null)
            {
                aoFechar();
            }
            return;
        }

        if (aberto != null)
        {
            aberto.FecharNaHora();
        }

        bool novo = !GameProgress.VerbeteDesbloqueado(v.id);
        if (novo)
        {
            GameProgress.DesbloquearVerbete(v.id);
        }

        aberto = Instantiate(banco.prefabDoCartao);
        aberto.Preencher(v, novo, aoFechar, origemNaTela);
        TelaFixa.AjustarAgora();
    }

    private void Preencher(Verbete v, bool novo, Action quandoFechar, Vector2 origemNaTela)
    {
        verbete = v;
        aoFechar = quandoFechar;
        posicaoFinal = cartao.anchoredPosition;

        textoDaPalavra.text = v.palavra;
        textoDaDefinicao.text = v.definicao;
        ilustracao.sprite = v.ilustracao;
        ilustracao.gameObject.SetActive(v.ilustracao != null);
        desenhoProvisorio.SetActive(v.ilustracao == null);
        seloNovo.gameObject.SetActive(novo);
        seloNovo.localScale = Vector3.zero;

        // Cena sem EventSystem não receberia o toque nos botões.
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var eventos = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventos.transform.SetParent(transform, false);
        }

        voz = Narracao.Voz;
        botaoOuvir.gameObject.SetActive(voz.Disponivel);
        if (!voz.Disponivel)
        {
            StartCoroutine(EsperarVoz());
        }

        brilho.gameObject.SetActive(false);
        foreach (RectTransform p in particulas)
        {
            p.gameObject.SetActive(false);
        }

        Vector2 origem;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, origemNaTela, null, out origem);
        Tocar(somAoAbrir);
        StartCoroutine(Entrar(origem, novo));
    }

    // Botão "Ouvir": lê a definição do glossário, sem mudar nada.
    public void Ouvir()
    {
        if (verbete != null && voz != null)
        {
            voz.Falar(verbete.definicao);
        }
    }

    // Botão "Continuar".
    public void Continuar()
    {
        if (!fechando)
        {
            fechando = true;
            StartCoroutine(Sair());
        }
    }

    private void Update()
    {
        // O selo "Novo!" pulsa de leve pra chamar a atenção.
        if (seloNovo.gameObject.activeSelf && seloNovo.localScale.x > 0.9f && !fechando)
        {
            seloNovo.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 5f));
        }
    }

    // O cartão vem voando em arco até o centro, crescendo e desentortando; ao chegar, brilho e estrelinhas.
    // Tudo em tempo real (unscaled): funciona mesmo com o minigame pausado.
    private IEnumerator Entrar(Vector2 origem, bool novo)
    {
        botaoContinuar.interactable = false;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / Mathf.Max(0.05f, duracaoDaEntrada));
            float suave = 1f - Mathf.Pow(1f - t, 3f);
            cartao.anchoredPosition = Vector2.Lerp(origem, posicaoFinal, suave) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * 110f);
            cartao.localScale = Vector3.one * Mathf.LerpUnclamped(0.12f, 1f, ComMola(t));
            cartao.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-14f, 0f, suave));
            fundo.alpha = t;
            yield return null;
        }

        cartao.anchoredPosition = posicaoFinal;
        cartao.localScale = Vector3.one;
        cartao.localEulerAngles = Vector3.zero;
        botaoContinuar.interactable = true;

        StartCoroutine(Brilhar());
        for (int i = 0; i < particulas.Length; i++)
        {
            StartCoroutine(SoltarParticula(particulas[i], i));
        }

        if (novo)
        {
            Tocar(somDeNovo);
            float s = 0f;
            while (s < 1f)
            {
                s = Mathf.Min(1f, s + Time.unscaledDeltaTime / 0.3f);
                seloNovo.localScale = Vector3.one * ComMola(s);
                yield return null;
            }
        }
    }

    private IEnumerator Brilhar()
    {
        brilho.gameObject.SetActive(true);
        Color cor = brilho.color;
        float alfa = cor.a;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.7f);
            brilho.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.1f, 1f - Mathf.Pow(1f - t, 2f));
            cor.a = alfa * (1f - t);
            brilho.color = cor;
            yield return null;
        }
        brilho.gameObject.SetActive(false);
        cor.a = alfa;
        brilho.color = cor;
    }

    private IEnumerator SoltarParticula(RectTransform p, int indice)
    {
        var imagem = p.GetComponent<Image>();
        Color cor = imagem.color;
        float angulo = (indice / (float)particulas.Length) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
        var direcao = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
        float distancia = UnityEngine.Random.Range(560f, 900f);
        float giro = UnityEngine.Random.Range(-220f, 220f);
        float duracao = UnityEngine.Random.Range(0.8f, 1.2f);
        float tamanho = UnityEngine.Random.Range(0.7f, 1.3f);

        p.gameObject.SetActive(true);
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / duracao);
            float suave = 1f - Mathf.Pow(1f - t, 2f);
            // Sai do centro, vai longe e cai um pouco no fim, como confete leve.
            p.anchoredPosition = direcao * (distancia * suave) + Vector2.down * (140f * t * t);
            p.localEulerAngles = new Vector3(0f, 0f, giro * t);
            p.localScale = Vector3.one * (tamanho * Mathf.Lerp(0.4f, 1f, Mathf.Min(1f, t * 4f)));
            cor.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            imagem.color = cor;
            yield return null;
        }
        p.gameObject.SetActive(false);
    }

    private IEnumerator Sair()
    {
        voz.Parar();
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.18f);
            cartao.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, t);
            fundo.alpha = 1f - t;
            yield return null;
        }
        Encerrar();
    }

    // Outro cartão vai abrir por cima: este some sem animação, mas quem o abriu ainda é avisado.
    private void FecharNaHora()
    {
        fechando = true;
        StopAllCoroutines();
        if (voz != null)
        {
            voz.Parar();
        }
        Encerrar();
    }

    private void Encerrar()
    {
        if (aberto == this)
        {
            aberto = null;
        }

        Action avisar = aoFechar;
        aoFechar = null;
        Destroy(gameObject);
        if (avisar != null)
        {
            avisar();
        }
    }

    private void OnDestroy()
    {
        if (aberto == this)
        {
            aberto = null;
        }
    }

    // No navegador as vozes chegam um pouco depois da página: o "Ouvir" aparece quando houver voz em português.
    private IEnumerator EsperarVoz()
    {
        for (int i = 0; i < 16 && !voz.Disponivel; i++)
        {
            yield return new WaitForSecondsRealtime(0.25f);
        }
        botaoOuvir.gameObject.SetActive(voz.Disponivel);
    }

    private void Tocar(AudioClip som)
    {
        var fonte = GetComponent<AudioSource>();
        if (som != null && fonte != null)
        {
            fonte.PlayOneShot(som);
        }
    }

    // Passa um pouco do ponto e volta, como uma mola.
    private static float ComMola(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
