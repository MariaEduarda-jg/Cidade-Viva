using UnityEngine;
using UnityEngine.SceneManagement;

public class AreaTrigger : MonoBehaviour
{
    public string areaName;
    public string cenaDoMinigame = "Minigame_Template";

    private void Start()
    {
        AtualizarCor();
    }

    // Área já completada fica na cor original do material; senão, em tons de cinza.
    // Usa MaterialPropertyBlock para não alterar o material compartilhado (asset).
    private void AtualizarCor()
    {
        // Bagunça da enchente: some quando o minigame da área é completado.
        CaosDaArea caos = GetComponent<CaosDaArea>();
        if (caos != null)
        {
            caos.Aplicar(GameProgress.FoiCompletado(areaName));
        }

        // Área com visual montado (prefabs): quem cuida do cinza/colorido é o SaturacaoDaArea.
        SaturacaoDaArea saturacao = GetComponent<SaturacaoDaArea>();
        if (saturacao != null)
        {
            saturacao.Aplicar(GameProgress.FoiCompletado(areaName) ? 1f : saturacao.saturacaoSemCompletar);
            return;
        }

        Renderer rend = GetComponent<Renderer>();
        if (rend == null)
        {
            return;
        }

        var bloco = new MaterialPropertyBlock();
        if (!GameProgress.FoiCompletado(areaName))
        {
            Color original = rend.sharedMaterial.GetColor("_BaseColor");
            float cinza = original.grayscale;
            bloco.SetColor("_BaseColor", new Color(cinza, cinza, cinza, original.a));
        }
        rend.SetPropertyBlock(bloco);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerMovement>() == null)
        {
            return;
        }

        Debug.Log("Entrou na área: " + areaName);
        MinigameBase.EsquecerNivelEscolhido();
        SceneManager.LoadScene(cenaDoMinigame);
    }
}
