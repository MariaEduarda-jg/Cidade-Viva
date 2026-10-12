using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Botão "Álbum" fixo na tela do Hub: mostra quantos verbetes o perfil ativo já tem e abre o Álbum Glotrânsito.
public class BotaoDoAlbum : MonoBehaviour
{
    public Text contador;
    public string cenaDoAlbum = "Album_Glotransito";

    private void OnEnable()
    {
        GameProgress.VerbetesMudaram += Atualizar;
    }

    private void OnDisable()
    {
        GameProgress.VerbetesMudaram -= Atualizar;
    }

    private void Start()
    {
        Atualizar();
    }

    public void Atualizar()
    {
        contador.text = GameProgress.ContarVerbetesDesbloqueados() + "/" + VerbeteDatabase.Instancia.Quantidade;
    }

    public void Abrir()
    {
        SceneManager.LoadScene(cenaDoAlbum);
    }
}
