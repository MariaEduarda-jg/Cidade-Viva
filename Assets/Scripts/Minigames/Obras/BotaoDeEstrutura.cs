using UnityEngine;
using UnityEngine.UI;

// Um botão da barra de ferramentas das Obras da Cidade (ícone e nome de uma estrutura).
// O ObrasHUD preenche; o clique avisa o ObrasManager qual opção foi tocada.
public class BotaoDeEstrutura : MonoBehaviour
{
    public ObrasManager manager;
    public int indice;
    public Image frente;
    public Image icone;
    public Text nome;

    public void Tocar()
    {
        manager.EscolherOpcao(indice);
    }
}
