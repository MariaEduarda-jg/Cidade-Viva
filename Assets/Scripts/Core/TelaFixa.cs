using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tela 16:9 fixa (versão final): em qualquer proporção de janela (Editor, WebGL, Windows) o jogo mostra
// um retângulo 16:9 no meio, com faixas pretas nas sobras. Vale pra todas as cenas, sem mexer nelas:
//   - câmeras: o "rect" de cada câmera que desenha na tela vira o retângulo 16:9; uma câmera de fundo pinta o resto de preto;
//   - interface: cada Canvas "Screen Space Overlay" raiz ganha um filho "Area_16x9" (do tamanho do retângulo) e
//     tudo que estava nele passa pra dentro; a escala do CanvasScaler é trocada por uma fixa, calculada de modo que
//     a área tenha o tamanho da resolução de referência (1920x1080). Assim os painéis ficam exatamente como em 16:9,
//     e quem posiciona coisas por ponto da tela (marcas, cartão do verbete) continua certo;
//   - faixas: um Canvas por cima de tudo, com 4 retângulos pretos fora da área (também seguram toques fora do jogo).
// Criado sozinho ao abrir o jogo (RuntimeInitializeOnLoadMethod); sobrevive às trocas de cena.
public class TelaFixa : MonoBehaviour
{
    public const float Proporcao = 16f / 9f;

    private static TelaFixa instancia;
    private Camera fundo;
    private Canvas faixas;
    private RectTransform[] barras = new RectTransform[4];
    private Vector2Int tamanhoAnterior;
    private float proximaVarredura;
    private readonly List<Canvas> processados = new List<Canvas>();

    private class CanvasNaTela : MonoBehaviour
    {
        public RectTransform area;
        public Vector2 referencia = new Vector2(1920f, 1080f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Criar()
    {
        if (instancia != null)
        {
            return;
        }
        var go = new GameObject("TelaFixa_16x9");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<TelaFixa>();
    }

    // Pra quem acabou de criar um Canvas (ex.: o cartão do verbete) não esperar a próxima varredura.
    public static void AjustarAgora()
    {
        if (instancia != null)
        {
            instancia.Ajustar(true);
        }
    }

    // O retângulo 16:9 em coordenadas de viewport (0 a 1).
    public static Rect Area
    {
        get
        {
            float aspecto = (float)Screen.width / Mathf.Max(1, Screen.height);
            if (aspecto > Proporcao)
            {
                float w = Proporcao / aspecto;
                return new Rect((1f - w) / 2f, 0f, w, 1f);
            }
            float h = aspecto / Proporcao;
            return new Rect(0f, (1f - h) / 2f, 1f, h);
        }
    }

    private void Awake()
    {
        fundo = new GameObject("Camera_Das_Faixas").AddComponent<Camera>();
        fundo.transform.SetParent(transform, false);
        fundo.depth = -100f;
        fundo.clearFlags = CameraClearFlags.SolidColor;
        fundo.backgroundColor = Color.black;
        fundo.cullingMask = 0;
        fundo.useOcclusionCulling = false;

        var go = new GameObject("Faixas_Pretas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        faixas = go.GetComponent<Canvas>();
        faixas.renderMode = RenderMode.ScreenSpaceOverlay;
        faixas.sortingOrder = 32767;
        for (int i = 0; i < 4; i++)
        {
            var b = new GameObject("Faixa_" + i, typeof(RectTransform), typeof(Image));
            b.transform.SetParent(go.transform, false);
            b.GetComponent<Image>().color = Color.black;
            barras[i] = (RectTransform)b.transform;
        }
        SceneManager.sceneLoaded += (cena, modo) => Ajustar(true);
    }

    private void LateUpdate()
    {
        var tamanho = new Vector2Int(Screen.width, Screen.height);
        bool mudou = tamanho != tamanhoAnterior;
        if (mudou || Time.unscaledTime >= proximaVarredura)
        {
            Ajustar(mudou);
        }
    }

    private void Ajustar(bool tudo)
    {
        tamanhoAnterior = new Vector2Int(Screen.width, Screen.height);
        proximaVarredura = Time.unscaledTime + 0.1f;
        Rect r = Area;
        fundo.enabled = r.width < 0.999f || r.height < 0.999f;

        foreach (Camera cam in Camera.allCameras)
        {
            if (cam != fundo && cam.targetTexture == null && cam.rect != r)
            {
                cam.rect = r;
            }
        }

        // Faixas: esquerda, direita, embaixo, em cima (em fração da tela).
        Faixa(barras[0], new Vector2(0f, 0f), new Vector2(r.xMin, 1f));
        Faixa(barras[1], new Vector2(r.xMax, 0f), new Vector2(1f, 1f));
        Faixa(barras[2], new Vector2(0f, 0f), new Vector2(1f, r.yMin));
        Faixa(barras[3], new Vector2(0f, r.yMax), new Vector2(1f, 1f));

        processados.RemoveAll(c => c == null);
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
        {
            if (c == faixas || !c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                continue;
            }
            CanvasNaTela marca = c.GetComponent<CanvasNaTela>();
            if (marca == null)
            {
                marca = Preparar(c);
                processados.Add(c);
            }
            Recolher(c, marca);
            Encaixar(c, marca, r);
        }
    }

    private static void Faixa(RectTransform b, Vector2 min, Vector2 max)
    {
        b.anchorMin = min;
        b.anchorMax = max;
        b.offsetMin = b.offsetMax = Vector2.zero;
        b.gameObject.SetActive(max.x - min.x > 0.0001f && max.y - min.y > 0.0001f);
    }

    private static CanvasNaTela Preparar(Canvas c)
    {
        var marca = c.gameObject.AddComponent<CanvasNaTela>();
        var escala = c.GetComponent<CanvasScaler>();
        if (escala != null)
        {
            if (escala.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                marca.referencia = escala.referenceResolution;
            }
            escala.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        }
        var area = new GameObject("Area_16x9", typeof(RectTransform));
        area.transform.SetParent(c.transform, false);
        marca.area = (RectTransform)area.transform;
        marca.area.anchorMin = marca.area.anchorMax = marca.area.pivot = new Vector2(0.5f, 0.5f);
        marca.area.anchoredPosition = Vector2.zero;
        return marca;
    }

    // Tudo que está direto no Canvas (menos a área) passa pra dentro da área, na mesma ordem.
    private static void Recolher(Canvas c, CanvasNaTela marca)
    {
        Transform raiz = c.transform;
        if (raiz.childCount <= 1)
        {
            return;
        }
        var filhos = new List<Transform>();
        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform t = raiz.GetChild(i);
            if (t != marca.area) filhos.Add(t);
        }
        foreach (Transform t in filhos)
        {
            t.SetParent(marca.area, false);
        }
    }

    private static void Encaixar(Canvas c, CanvasNaTela marca, Rect r)
    {
        float larguraPx = r.width * Screen.width;
        float alturaPx = r.height * Screen.height;
        float fator = Mathf.Max(0.01f, Mathf.Min(larguraPx / marca.referencia.x, alturaPx / marca.referencia.y));
        var escala = c.GetComponent<CanvasScaler>();
        if (escala != null)
        {
            if (!Mathf.Approximately(escala.scaleFactor, fator)) escala.scaleFactor = fator;
        }
        else
        {
            c.scaleFactor = fator;
        }
        var tamanho = new Vector2(larguraPx, alturaPx) / fator;
        if (marca.area.sizeDelta != tamanho) marca.area.sizeDelta = tamanho;
        // O centro da área segue o centro do retângulo 16:9 (sempre o meio da tela).
        marca.area.anchoredPosition = Vector2.zero;
    }
}
