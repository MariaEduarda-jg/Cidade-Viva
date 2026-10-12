using UnityEngine;
using UnityEngine.UI;

// Uma figurinha do Álbum Glotrânsito: colorida com a ilustração se o verbete já foi desbloqueado,
// em silhueta com "?" se ainda não.
public class FigurinhaDoAlbum : MonoBehaviour
{
    public Button botao;
    public Image fundo;
    public Image ilustracao;
    // Verbete desbloqueado que ainda não tem ilustração no glossário.
    public GameObject desenhoProvisorio;
    // Silhueta com "?".
    public GameObject bloqueado;
    public Text nome;
    public Color corDesbloqueada = Color.white;
    public Color corBloqueada = Color.gray;

    public Verbete Verbete { get; private set; }
    public bool Desbloqueado { get; private set; }

    public void Preencher(Verbete verbete, bool desbloqueado, AlbumGlotransito album)
    {
        Verbete = verbete;
        Desbloqueado = desbloqueado;

        fundo.color = desbloqueado ? corDesbloqueada : corBloqueada;
        bloqueado.SetActive(!desbloqueado);
        ilustracao.sprite = verbete.ilustracao;
        ilustracao.gameObject.SetActive(desbloqueado && verbete.ilustracao != null);
        desenhoProvisorio.SetActive(desbloqueado && verbete.ilustracao == null);
        // Figurinha escondida não entrega a palavra.
        nome.text = desbloqueado ? verbete.palavra : "???";

        botao.onClick.RemoveAllListeners();
        botao.onClick.AddListener(() => album.Tocar(this));
    }
}
