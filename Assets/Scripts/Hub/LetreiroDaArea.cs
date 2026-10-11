using UnityEngine;
using UnityEngine.UI;

// Placa com o nome do minigame e o recorde, em cima de cada área do Hub.
public class LetreiroDaArea : MonoBehaviour
{
    public AreaTrigger area;
    public Text textoDoNome;
    public Text textoDoRecorde;
    // {0} = recorde, {1} = total de estrelas, {2} = níveis concluídos
    public string formatoDoRecorde = "Recorde {0}  |  Nível {2}  |  {1} estrelas";
    public string textoSemRecorde = "Venha jogar!";

    private void Start()
    {
        if (area == null)
        {
            return;
        }

        if (textoDoNome != null)
        {
            textoDoNome.text = area.areaName;
        }
        if (textoDoRecorde != null)
        {
            textoDoRecorde.text = GameProgress.FoiCompletado(area.areaName)
                ? string.Format(formatoDoRecorde, GameProgress.ObterRecorde(area.areaName), GameProgress.ObterEstrelas(area.areaName),
                    GameProgress.ObterNiveisConcluidos(area.areaName))
                : textoSemRecorde;
        }
    }
}
