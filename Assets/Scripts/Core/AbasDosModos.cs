using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Abas no alto da seleção de níveis quando os modos de um minigame ficam em cenas diferentes
// (Rua Viva: "Passeio de Carro" e "Pedalada"). Tocar numa aba abre a outra cena já na seleção de níveis.
// Cada cena tem a sua cópia das abas, com "atual" apontando pra ela mesma.
public class AbasDosModos : MonoBehaviour
{
    public MinigameBase gerente;
    public string[] cenas = new string[0];
    public int atual;
    public Image[] frentes = new Image[0];
    public Color cor = Color.white;
    public Color corEscolhida = Color.yellow;

    private void Start()
    {
        for (int i = 0; i < frentes.Length; i++)
        {
            frentes[i].color = i == atual ? corEscolhida : cor;
        }
        gameObject.SetActive(gerente != null && gerente.EscolhendoNivel);
    }

    private void Update()
    {
        if (gerente != null && !gerente.EscolhendoNivel)
        {
            gameObject.SetActive(false);
        }
    }

    public void Trocar(int modo)
    {
        if (gerente == null || !gerente.EscolhendoNivel || modo == atual || modo < 0 || modo >= cenas.Length)
        {
            return;
        }
        MinigameBase.EsquecerNivelEscolhido();
        SceneManager.LoadScene(cenas[modo]);
    }
}
