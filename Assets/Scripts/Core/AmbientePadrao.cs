using UnityEngine;

// Look padrão do jogo. Fica na raiz do prefab Ambiente_Padrao (sol + volume de pós-processamento)
// e aplica na cena o que não cabe em prefab: céu, luz ambiente e neblina (RenderSettings).
[ExecuteAlways]
public class AmbientePadrao : MonoBehaviour
{
    public Material ceu;
    public Light sol;

    [Header("Luz ambiente em gradiente")]
    public Color ambienteCeu = new Color(0.40f, 0.47f, 0.56f);
    public Color ambienteHorizonte = new Color(0.50f, 0.42f, 0.36f);
    public Color ambienteChao = new Color(0.28f, 0.26f, 0.24f);

    [Header("Neblina (dá profundidade nas cenas 3D)")]
    public bool neblina = true;
    public Color corDaNeblina = new Color(0.96f, 0.86f, 0.76f);
    public float neblinaInicio = 60f;
    public float neblinaFim = 260f;

    private void OnEnable()
    {
        Aplicar();
    }

    public void Aplicar()
    {
        if (ceu != null)
        {
            RenderSettings.skybox = ceu;
        }
        RenderSettings.sun = sol;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambienteCeu;
        RenderSettings.ambientEquatorColor = ambienteHorizonte;
        RenderSettings.ambientGroundColor = ambienteChao;
        RenderSettings.fog = neblina;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = corDaNeblina;
        RenderSettings.fogStartDistance = neblinaInicio;
        RenderSettings.fogEndDistance = neblinaFim;
    }
}
