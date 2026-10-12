#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// Narração no WebGL: usa a síntese de voz do próprio navegador (speechSynthesis), em português do Brasil.
// O código do navegador fica em Assets/Plugins/WebGL/CidadeVivaVoz.jslib.
// Se o navegador não tiver voz em português, Disponivel fica falso e nada é falado (sem erro).
public class VozDoNavegador : IVozNarradora
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int CidadeVivaVozDisponivel();

    [DllImport("__Internal")]
    private static extern void CidadeVivaVozFalar(string texto);

    [DllImport("__Internal")]
    private static extern void CidadeVivaVozParar();

    public bool Disponivel
    {
        get { return CidadeVivaVozDisponivel() == 1; }
    }

    public void Falar(string texto)
    {
        if (!string.IsNullOrEmpty(texto))
        {
            CidadeVivaVozFalar(texto);
        }
    }

    public void Parar()
    {
        CidadeVivaVozParar();
    }
#else
    public bool Disponivel
    {
        get { return false; }
    }

    public void Falar(string texto)
    {
    }

    public void Parar()
    {
    }
#endif
}
