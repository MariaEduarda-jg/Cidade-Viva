using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Cantinho do Hub que mostra quem está jogando e tem o botão "Trocar de perfil".
public class PainelDoPerfil : MonoBehaviour
{
    public IconesDePerfil icones;
    public Image avatar;
    public Text nome;
    // Aparece só pro Visitante ("Seu progresso não será guardado").
    public GameObject avisoDoVisitante;
    public string cenaDoMenu = "Menu_Inicial";

    private void Start()
    {
        Perfil perfil = Perfis.Ativo;
        nome.text = perfil.nome;
        avatar.sprite = icones.Avatar(perfil.avatar);
        avisoDoVisitante.SetActive(perfil.visitante);
    }

    public void TrocarDePerfil()
    {
        Perfis.Sair();
        SceneManager.LoadScene(cenaDoMenu);
    }
}
