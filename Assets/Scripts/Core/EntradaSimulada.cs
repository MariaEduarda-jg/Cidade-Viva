using UnityEngine;

// Roda antes de todos os scripts em cada quadro e "vira" os eventos simulados pro quadro atual.
[DefaultExecutionOrder(-10000)]
public class EntradaSimulada : MonoBehaviour
{
    private static EntradaSimulada instancia;

    public static void Garantir()
    {
        if (instancia != null) return;
        var go = new GameObject("EntradaSimulada");
        DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideInHierarchy;
        instancia = go.AddComponent<EntradaSimulada>();
    }

    private void Update()
    {
        Toque.Virar();
        Teclado.Virar();
    }
}
