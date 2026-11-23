using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Camada fina de entrada (toque/mouse e teclado). No jogo normal lê o Input do Unity, igualzinho a antes.
// Existe pra o teste automático poder "tocar", "arrastar" e "apertar teclas" de verdade: o piloto chama
// Toque.Simular / Teclado.Simular e os managers recebem a entrada pelo mesmo caminho de uma criança jogando.
// Enquanto há simulação ligada, o mouse e o teclado de verdade são ignorados.
public static class Toque
{
    public static bool Simulando { get; private set; }
    private static Vector2 posicao;
    private static bool apertado;
    private static int frameApertou = -1;
    private static int frameSoltou = -1;
    private static bool apertarNoProximo;
    private static bool soltarNoProximo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        Simulando = false;
        apertado = false;
        frameApertou = frameSoltou = -1;
        apertarNoProximo = soltarNoProximo = false;
    }

    // Liga a simulação e põe o dedo em "posicao" (pixels da tela), apertado ou não.
    // A mudança de estado vale a partir do próximo quadro (como um dedo de verdade).
    public static void Simular(Vector2 pos, bool pressionado)
    {
        Simulando = true;
        posicao = pos;
        if (pressionado && !apertado) apertarNoProximo = true;
        if (!pressionado && apertado) soltarNoProximo = true;
        apertado = pressionado;
        EntradaSimulada.Garantir();
    }

    public static void PararDeSimular()
    {
        Simulando = false;
        apertado = false;
        Teclado.PararDeSimular();
    }

    // Chamado no começo de cada quadro (EntradaSimulada roda antes de todo mundo).
    internal static void Virar()
    {
        if (apertarNoProximo) { frameApertou = Time.frameCount; apertarNoProximo = false; }
        if (soltarNoProximo) { frameSoltou = Time.frameCount; soltarNoProximo = false; }
    }

    public static bool Apertou => Simulando ? frameApertou == Time.frameCount : Input.GetMouseButtonDown(0);
    public static bool Segurando => Simulando ? apertado : Input.GetMouseButton(0);
    public static bool Soltou => Simulando ? frameSoltou == Time.frameCount : Input.GetMouseButtonUp(0);
    public static Vector3 Posicao => Simulando ? (Vector3)posicao : Input.mousePosition;

    // Movimento do mouse (girar a câmera do Hub): parado durante a simulação.
    public static float EixoDoMouse(string eixo) => Simulando ? 0f : Input.GetAxis(eixo);

    // O dedo está em cima de algum botão/painel da interface?
    public static bool SobreAInterface()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;
        if (!Simulando) return es.IsPointerOverGameObject();
        var dados = new PointerEventData(es) { position = posicao };
        var resultados = new List<RaycastResult>();
        es.RaycastAll(dados, resultados);
        return resultados.Count > 0;
    }
}

public static class Teclado
{
    private static readonly HashSet<KeyCode> seguradas = new HashSet<KeyCode>();
    private static readonly Dictionary<KeyCode, int> apertouNoFrame = new Dictionary<KeyCode, int>();
    private static readonly HashSet<KeyCode> apertarNoProximo = new HashSet<KeyCode>();
    private static string textoNoProximo = "";
    private static string texto = "";
    private static int frameDoTexto = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        PararDeSimular();
    }

    public static void PararDeSimular()
    {
        seguradas.Clear();
        apertouNoFrame.Clear();
        apertarNoProximo.Clear();
        textoNoProximo = texto = "";
    }

    // Aperta (pressionada = true) ou solta uma tecla.
    public static void Simular(KeyCode tecla, bool pressionada)
    {
        if (pressionada)
        {
            if (seguradas.Add(tecla)) apertarNoProximo.Add(tecla);
        }
        else
        {
            seguradas.Remove(tecla);
        }
        EntradaSimulada.Garantir();
    }

    // Digita um texto (vale pro quadro seguinte, como o Input.inputString).
    public static void Digitar(string t)
    {
        textoNoProximo += t;
        EntradaSimulada.Garantir();
    }

    internal static void Virar()
    {
        foreach (KeyCode k in apertarNoProximo) apertouNoFrame[k] = Time.frameCount;
        apertarNoProximo.Clear();
        if (textoNoProximo.Length > 0) { texto = textoNoProximo; textoNoProximo = ""; frameDoTexto = Time.frameCount; }
    }

    public static bool Apertou(KeyCode k) => Input.GetKeyDown(k) || (apertouNoFrame.TryGetValue(k, out int f) && f == Time.frameCount);
    public static bool Segurando(KeyCode k) => Input.GetKey(k) || seguradas.Contains(k);
    public static string TextoDigitado => Input.inputString + (frameDoTexto == Time.frameCount ? texto : "");

    public static float Eixo(string eixo)
    {
        float real = Input.GetAxisRaw(eixo);
        if (Mathf.Abs(real) > 0.01f || seguradas.Count == 0) return real;
        if (eixo == "Horizontal") return (Seg(KeyCode.RightArrow, KeyCode.D) ? 1f : 0f) - (Seg(KeyCode.LeftArrow, KeyCode.A) ? 1f : 0f);
        if (eixo == "Vertical") return (Seg(KeyCode.UpArrow, KeyCode.W) ? 1f : 0f) - (Seg(KeyCode.DownArrow, KeyCode.S) ? 1f : 0f);
        return 0f;
    }

    private static bool Seg(KeyCode a, KeyCode b) => seguradas.Contains(a) || seguradas.Contains(b);
}
