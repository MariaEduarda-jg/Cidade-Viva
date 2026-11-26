using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Mapa do Hub (2026-11-26): botão "Mapa" (e a tecla M) abre o desenho do Plano Piloto visto de cima, com as
// áreas, os extras e "você está aqui". Tocar numa área leva o jogador até a frente dela, com um escurecer
// rápido. O mesmo escurecer serve pro botão "Ir" da campanha (Levar). Gerado pelo MapaDoHubConstrutor.
public class MapaDoHub : MonoBehaviour
{
    public static MapaDoHub Instancia { get; private set; }

    [System.Serializable]
    public class LugarNoMapa
    {
        public string nome;
        public Transform area;
        public Button botao;        // a bolinha no desenho
        public Button botaoDaLista; // o botão com o nome, na lista "Ir para:" ao lado do desenho
    }

    public Transform jogador;
    public Button botaoMapa;
    public GameObject painel;
    public Button botaoFechar;
    [Tooltip("A imagem do mapa: o mundo (retângulo 'mundo', em x e z) ocupa a imagem inteira.")]
    public RectTransform desenho;
    public Rect mundo;
    public RectTransform voceEstaAqui;
    public RectTransform setaDoJogador;
    public LugarNoMapa[] lugares = new LugarNoMapa[0];
    public Image escurecer;
    [Tooltip("A quantos metros na frente do marcador da área o jogador aparece.")]
    public float distanciaNaFrente = 3.4f;

    public bool Aberto => painel != null && painel.activeSelf;
    public bool Levando { get; private set; }

    private void Awake()
    {
        Instancia = this;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        if (Aberto) Time.timeScale = 1f;
    }

    private void Start()
    {
        painel.SetActive(false);
        escurecer.gameObject.SetActive(false);
        botaoMapa.onClick.AddListener(Abrir);
        botaoFechar.onClick.AddListener(Fechar);
        foreach (LugarNoMapa l in lugares)
        {
            LugarNoMapa lugar = l;
            if (lugar.botao != null) lugar.botao.onClick.AddListener(() => Ir(lugar));
            if (lugar.botaoDaLista != null) lugar.botaoDaLista.onClick.AddListener(() => Ir(lugar));
            if (lugar.botao != null && lugar.area != null) ((RectTransform)lugar.botao.transform).anchoredPosition = NoMapa(lugar.area.position);
        }
    }

    private void Update()
    {
        if (Levando) return;
        if (Aberto)
        {
            if (Teclado.Apertou(KeyCode.M) || Teclado.Apertou(KeyCode.Escape)) Fechar();
            else AtualizarVoce();
            return;
        }
        botaoMapa.gameObject.SetActive(!Conversando);
        if (Teclado.Apertou(KeyCode.M)) Abrir();
    }

    private static bool Conversando => CampanhaNoHub.Instancia != null && CampanhaNoHub.Instancia.Conversando;

    public void Abrir()
    {
        // Não abre por cima da loja (que para o jogo), do cartão do verbete nem de uma conversa.
        if (Aberto || Levando || Time.timeScale <= 0f || CartaoVerbete.EstaAberto || Conversando) return;
        CameraFollow.SoltarOMouse();
        painel.SetActive(true);
        painel.transform.SetAsLastSibling();
        Time.timeScale = 0f;
        AtualizarVoce();
    }

    public void Fechar()
    {
        if (!Aberto) return;
        painel.SetActive(false);
        Time.timeScale = 1f;
    }

    // Posição (na imagem do mapa) de um ponto do mundo.
    public Vector2 NoMapa(Vector3 p)
    {
        Rect r = desenho.rect;
        return new Vector2(((p.x - mundo.xMin) / mundo.width - 0.5f) * r.width, ((p.z - mundo.yMin) / mundo.height - 0.5f) * r.height);
    }

    private void AtualizarVoce()
    {
        if (jogador == null) return;
        voceEstaAqui.anchoredPosition = NoMapa(jogador.position);
        setaDoJogador.localEulerAngles = new Vector3(0f, 0f, -jogador.eulerAngles.y);
    }

    private void Ir(LugarNoMapa lugar)
    {
        if (lugar.area == null) return;
        Fechar();
        Vector3 frente = lugar.area.position + lugar.area.rotation * Vector3.back * distanciaNaFrente;
        Vector3 olhar = lugar.area.position - frente;
        olhar.y = 0f;
        Levar(frente, Quaternion.LookRotation(olhar));
    }

    // Escurece rápido, põe o jogador no lugar (olhando pra "giro") e clareia.
    public void Levar(Vector3 destino, Quaternion giro)
    {
        if (Levando || jogador == null) return;
        StartCoroutine(Levando_(destino, giro));
    }

    private IEnumerator Levando_(Vector3 destino, Quaternion giro)
    {
        Levando = true;
        escurecer.gameObject.SetActive(true);
        escurecer.transform.SetAsLastSibling();
        yield return Escurecer(0f, 1f, 0.18f);
        Teleportar(jogador, destino, giro);
        yield return new WaitForSecondsRealtime(0.08f);
        yield return Escurecer(1f, 0f, 0.25f);
        escurecer.gameObject.SetActive(false);
        Levando = false;
        if (CampanhaNoHub.Instancia != null) CampanhaNoHub.Instancia.Atualizar();
    }

    private IEnumerator Escurecer(float de, float ate, float tempo)
    {
        for (float t = 0f; t < tempo; t += Time.unscaledDeltaTime)
        {
            escurecer.color = new Color(0f, 0f, 0f, Mathf.Lerp(de, ate, t / tempo));
            yield return null;
        }
        escurecer.color = new Color(0f, 0f, 0f, ate);
    }

    // Põe o jogador no lugar e a câmera logo atrás dele, sem deslizar pelo mapa.
    public static void Teleportar(Transform quem, Vector3 destino, Quaternion giro)
    {
        CharacterController cc = quem.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        quem.SetPositionAndRotation(new Vector3(destino.x, quem.position.y, destino.z), giro);
        if (cc != null) cc.enabled = true;
        foreach (CameraFollow c in FindObjectsByType<CameraFollow>())
        {
            if (c.target != quem) continue;
            c.Giro = giro.eulerAngles.y;
            c.transform.position = c.PosicaoPara(quem.position);
            c.transform.LookAt(quem.position + Vector3.up * c.lookHeightOffset);
        }
    }
}
