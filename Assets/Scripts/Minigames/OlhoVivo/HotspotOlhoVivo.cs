using UnityEngine;
using UnityEngine.UI;

// Um hotspot clicável do "Olho Vivo!": área invisível por cima de um elemento da cena.
// A CenaOlhoVivo cria um por ponto de erro e por elemento certo, a partir do modelo inativo da cena.
public class HotspotOlhoVivo : MonoBehaviour
{
    public Button botao;
    // Selo que aparece em cima do erro depois de corrigido.
    public GameObject marca;

    public void Configurar(Vector2 posicao, Vector2 tamanho)
    {
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
        marca.SetActive(false);
    }

    public void MarcarCorrigido()
    {
        marca.SetActive(true);
    }
}
