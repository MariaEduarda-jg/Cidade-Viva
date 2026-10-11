using UnityEngine;

// Estado de uma área do Hub depois da enchente: bagunçada enquanto o minigame não foi completado,
// arrumada depois. Só liga e desliga dois grupos de objetos; quem monta os grupos é o HubConstrutor.
public class CaosDaArea : MonoBehaviour
{
    [Tooltip("Ligado enquanto o minigame não foi completado: lama, entulho, lixo, placas caídas, árvores secas.")]
    public GameObject caos;

    [Tooltip("Ligado depois de completar: placas e semáforos no lugar, árvores verdes, flores.")]
    public GameObject arrumado;

    public void Aplicar(bool completado)
    {
        if (caos != null)
        {
            caos.SetActive(!completado);
        }
        if (arrumado != null)
        {
            arrumado.SetActive(completado);
        }
    }
}
