// Quem lê um texto em voz alta pra criança (botão "Ouvir" do cartão de verbete).
// O texto falado é sempre o que foi passado, sem mexer: no caso dos verbetes, a definição do glossário.
public interface IVozNarradora
{
    // Falso quando não dá pra narrar (ex.: navegador sem voz em português): quem usa esconde o botão "Ouvir".
    // No navegador as vozes demoram um pouco pra carregar, então pode começar falso e virar verdadeiro.
    bool Disponivel { get; }

    void Falar(string texto);

    void Parar();
}

// Entrega a voz certa pra plataforma: a do navegador no WebGL, a do Windows no Editor e no build
// de Desktop em Windows, e a silenciosa no resto.
public static class Narracao
{
    private static IVozNarradora voz;

    public static IVozNarradora Voz
    {
        get
        {
            if (voz == null)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                voz = new VozDoNavegador();
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                voz = new VozDoWindows();
#else
                voz = new VozSilenciosa();
#endif
            }
            return voz;
        }
    }
}
