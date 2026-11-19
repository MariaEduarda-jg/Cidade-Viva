using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Um plano da história: a câmera vai da pose "de" até a pose "ate" no tempo "duracao", com uma legenda,
// e dispara uma ação no começo (tempestade, enchente, a água baixa, a família...).
[Serializable]
public class PlanoDaHistoria
{
    public Transform de;
    public Transform ate;
    public float duracao = 6f;
    [TextArea] public string legenda;
    public AcaoDaHistoria acao;
}

public enum AcaoDaHistoria
{
    Nada,
    Tempestade,  // escurece, começa a chover, relâmpagos
    Enchente,    // a água sobe e leva placas e semáforos
    AguaBaixa,   // a água baixa, a chuva para, aparece a bagunça
    Familia,     // o céu abre; a família olha a cidade
    Comemora,    // a família comemora, com confete
}

// Abertura e final do jogo (cenas Abertura e Final, geradas pelo HistoriaConstrutor).
// Legendas curtas com "Ouvir" (nada é narrado sozinho) e "Pular". A abertura termina no Hub;
// o final termina na tela "Você reconstruiu a cidade!" com os verbetes e o botão de voltar ao Hub.
public class CenaDaHistoria : MonoBehaviour
{
    public bool ehFinal;
    public Camera cameraDaHistoria;
    public PlanoDaHistoria[] planos = new PlanoDaHistoria[0];

    [Header("Interface")]
    public Text textoDaLegenda;
    public GameObject painelDaLegenda;
    public GameObject botaoOuvir;
    public GameObject botaoPular;
    public Image escuro;
    public Image clarao;
    public Image preto;

    [Header("Cena")]
    public ParticleSystem chuva;
    public Transform agua;
    public float aguaBaixa = -0.4f;
    public float aguaAlta = 1.1f;
    [Tooltip("Placas e semáforos que a água leva (abertura).")]
    public Transform[] levados = new Transform[0];
    public GameObject bagunca;
    public Animator[] familia = new Animator[0];
    public ParticleSystem confete;
    public AudioSource som;
    public AudioClip somDoTrovao;
    public AudioClip somDaComemoracao;

    [Header("Tela final")]
    public GameObject telaFinal;
    public Transform gradeDosVerbetes;
    public GameObject modeloDoVerbete;
    public Text textoDoTotal;
    [Tooltip("{0} = verbetes, {1} = total.")]
    public string formatoDoTotal = "{0} de {1} verbetes do Glotrânsito";

    public int PlanoAtual { get; private set; } = -1;
    public string LegendaAtual { get; private set; } = "";
    public bool NaTelaFinal { get; private set; }
    public bool Acabou { get; private set; }

    private static readonly int Velocidade = Animator.StringToHash("Velocidade");
    private Coroutine roteiro;

    private void Start()
    {
        Time.timeScale = 1f;
        if (chuva != null) chuva.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (agua != null) AlturaDaAgua(aguaBaixa);
        if (bagunca != null) bagunca.SetActive(false);
        if (telaFinal != null) telaFinal.SetActive(false);
        Alfa(escuro, 0f);
        Alfa(clarao, 0f);
        Alfa(preto, 1f);
        Legenda("");
        foreach (Animator a in familia)
        {
            if (a != null) a.SetFloat(Velocidade, 0f);
        }
        roteiro = StartCoroutine(Roteiro());
    }

    private void Update()
    {
        if (botaoOuvir != null)
        {
            botaoOuvir.SetActive(Narracao.Voz.Disponivel && !string.IsNullOrEmpty(LegendaAtual));
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Pular();
        }
    }

    // ------------------------------------------------------------------ botões

    public void Ouvir()
    {
        if (!string.IsNullOrEmpty(LegendaAtual) && Narracao.Voz.Disponivel)
        {
            Narracao.Voz.Falar(LegendaAtual);
        }
    }

    // Abertura: vai direto pro Hub (ou pro menu). Final: vai direto pra tela dos verbetes.
    public void Pular()
    {
        if (Acabou || NaTelaFinal)
        {
            return;
        }
        if (roteiro != null) StopCoroutine(roteiro);
        StopAllCoroutines();
        Narracao.Voz.Parar();
        if (ehFinal)
        {
            Alfa(escuro, 0f);
            Alfa(preto, 0f);
            MostrarTelaFinal();
        }
        else
        {
            Terminar();
        }
    }

    public void VoltarAoHub()
    {
        Terminar();
    }

    private void Terminar()
    {
        if (Acabou)
        {
            return;
        }
        Acabou = true;
        Narracao.Voz.Parar();
        Historia.Terminou(ehFinal);
    }

    // ------------------------------------------------------------------ roteiro

    private IEnumerator Roteiro()
    {
        yield return Fade(preto, 1f, 0f, 0.8f);
        for (int i = 0; i < planos.Length; i++)
        {
            PlanoAtual = i;
            PlanoDaHistoria p = planos[i];
            Legenda(p.legenda);
            StartCoroutine(Acao(p.acao, p.duracao));
            yield return MoverCamera(p.de, p.ate, p.duracao);
        }
        Legenda("");
        if (ehFinal)
        {
            yield return Fade(preto, 0f, 1f, 0.6f);
            MostrarTelaFinal();
            yield return Fade(preto, 1f, 0f, 0.5f);
        }
        else
        {
            yield return Fade(preto, 0f, 1f, 1f);
            Terminar();
        }
    }

    private IEnumerator MoverCamera(Transform de, Transform ate, float duracao)
    {
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duracao);
            cameraDaHistoria.transform.SetPositionAndRotation(Vector3.Lerp(de.position, ate.position, k), Quaternion.Slerp(de.rotation, ate.rotation, k));
            yield return null;
        }
        cameraDaHistoria.transform.SetPositionAndRotation(ate.position, ate.rotation);
    }

    private IEnumerator Acao(AcaoDaHistoria acao, float duracao)
    {
        switch (acao)
        {
            case AcaoDaHistoria.Tempestade:
                StartCoroutine(Fade(escuro, 0f, 0.45f, 2f));
                if (chuva != null) chuva.Play();
                yield return new WaitForSeconds(1.2f);
                yield return Relampago();
                yield return new WaitForSeconds(1.8f);
                yield return Relampago();
                break;

            case AcaoDaHistoria.Enchente:
                if (chuva != null && !chuva.isPlaying) chuva.Play();
                Alfa(escuro, 0.45f);
                StartCoroutine(Relampago());
                yield return Enchente(duracao * 0.8f);
                break;

            case AcaoDaHistoria.AguaBaixa:
                if (chuva != null) chuva.Stop();
                if (bagunca != null) bagunca.SetActive(true);
                StartCoroutine(Fade(escuro, 0.45f, 0.15f, duracao * 0.8f));
                for (float t = 0f; t < duracao * 0.7f; t += Time.deltaTime)
                {
                    AlturaDaAgua(Mathf.Lerp(aguaAlta, aguaBaixa, t / (duracao * 0.7f)));
                    yield return null;
                }
                AlturaDaAgua(aguaBaixa);
                break;

            case AcaoDaHistoria.Familia:
                if (chuva != null) chuva.Stop();
                yield return Fade(escuro, escuro != null ? escuro.color.a : 0f, 0f, 1.5f);
                break;

            case AcaoDaHistoria.Comemora:
                yield return new WaitForSeconds(0.6f);
                foreach (Animator a in familia)
                {
                    if (a != null && a.HasState(0, Animator.StringToHash("Victory"))) a.CrossFade("Victory", 0.15f);
                }
                if (confete != null) confete.Play();
                if (som != null && somDaComemoracao != null) som.PlayOneShot(somDaComemoracao);
                yield return new WaitForSeconds(2.2f);
                if (confete != null) confete.Play();
                break;
        }
    }

    // A água sobe; placas e semáforos tombam e são levados, boiando.
    private IEnumerator Enchente(float duracao)
    {
        var inicio = new Vector3[levados.Length];
        var giroInicial = new Quaternion[levados.Length];
        var destino = new Vector3[levados.Length];
        var giroFinal = new Quaternion[levados.Length];
        for (int i = 0; i < levados.Length; i++)
        {
            inicio[i] = levados[i].position;
            giroInicial[i] = levados[i].rotation;
            destino[i] = inicio[i] + new Vector3(3f + i % 3 * 1.5f, 0f, (i % 2 == 0 ? 1f : -1f) * (1f + i % 4 * 0.6f));
            giroFinal[i] = Quaternion.Euler(80f, 40f * i, 0f) * giroInicial[i];
        }
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = t / duracao;
            float nivel = Mathf.Lerp(aguaBaixa, aguaAlta, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, k * 1.4f)));
            AlturaDaAgua(nivel);
            // Começam a cair quando a água chega neles.
            float solto = Mathf.Clamp01((k - 0.25f) / 0.75f);
            for (int i = 0; i < levados.Length; i++)
            {
                Vector3 p = Vector3.Lerp(inicio[i], destino[i], Mathf.SmoothStep(0f, 1f, solto));
                p.y = Mathf.Lerp(inicio[i].y, Mathf.Max(inicio[i].y, nivel - 0.15f), solto) + Mathf.Sin(Time.time * 2f + i) * 0.08f * solto;
                levados[i].SetPositionAndRotation(p, Quaternion.Slerp(giroInicial[i], giroFinal[i], solto));
            }
            yield return null;
        }
        // Quando a água baixar, ficam deitados no chão, longe de onde estavam.
        for (int i = 0; i < levados.Length; i++)
        {
            Vector3 p = destino[i];
            p.y = inicio[i].y + 0.15f;
            levados[i].SetPositionAndRotation(p, giroFinal[i]);
        }
    }

    private IEnumerator Relampago()
    {
        if (som != null && somDoTrovao != null) som.PlayOneShot(somDoTrovao, 0.6f);
        yield return Fade(clarao, 0f, 0.75f, 0.06f);
        yield return Fade(clarao, 0.75f, 0f, 0.35f);
    }

    private void AlturaDaAgua(float y)
    {
        if (agua == null) return;
        Vector3 p = agua.position;
        p.y = y;
        agua.position = p;
        agua.gameObject.SetActive(y > aguaBaixa + 0.01f);
    }

    // ------------------------------------------------------------------ tela final

    private void MostrarTelaFinal()
    {
        NaTelaFinal = true;
        PlanoAtual = planos.Length;
        Legenda("");
        if (painelDaLegenda != null) painelDaLegenda.SetActive(false);
        telaFinal.SetActive(true);
        if (botaoPular != null) botaoPular.SetActive(false);
        foreach (Transform filho in gradeDosVerbetes)
        {
            if (filho.gameObject != modeloDoVerbete) Destroy(filho.gameObject);
        }
        int abertos = 0;
        foreach (Verbete v in VerbeteDatabase.Instancia.Todos)
        {
            GameObject item = Instantiate(modeloDoVerbete, gradeDosVerbetes);
            item.name = "Verbete_" + v.id;
            item.SetActive(true);
            bool tem = GameProgress.VerbeteDesbloqueado(v.id);
            if (tem) abertos++;
            Image figura = item.transform.Find("Figura").GetComponent<Image>();
            figura.sprite = v.ilustracao;
            figura.gameObject.SetActive(v.ilustracao != null);
            figura.color = tem ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.6f);
            item.transform.Find("Palavra").GetComponent<Text>().text = v.palavra;
        }
        if (textoDoTotal != null)
        {
            textoDoTotal.text = string.Format(formatoDoTotal, abertos, VerbeteDatabase.Instancia.Quantidade);
        }
    }

    // ------------------------------------------------------------------ ajudas

    private void Legenda(string texto)
    {
        LegendaAtual = texto ?? "";
        if (textoDaLegenda != null) textoDaLegenda.text = LegendaAtual;
        if (painelDaLegenda != null) painelDaLegenda.SetActive(!string.IsNullOrEmpty(LegendaAtual));
    }

    private static void Alfa(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
        img.gameObject.SetActive(a > 0.001f);
    }

    private static IEnumerator Fade(Image img, float de, float ate, float duracao)
    {
        if (img == null) yield break;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            Alfa(img, Mathf.Lerp(de, ate, t / duracao));
            yield return null;
        }
        Alfa(img, ate);
    }
}
