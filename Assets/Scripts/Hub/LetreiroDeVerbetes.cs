using UnityEngine;
using UnityEngine.UI;

// Placa sobre a entrada de uma área nova do Hub: nome da área e "X de Y verbetes" do grupo dela,
// lidos do ChaosController (que lê do perfil ativo).
public class LetreiroDeVerbetes : MonoBehaviour
{
    public ChaosController controle;
    public Text textoDoNome;
    public Text textoDoProgresso;
    // {0} = desbloqueados, {1} = total do grupo.
    public string formato = "{0} de {1} verbetes";

    private void OnEnable()
    {
        if (controle != null)
        {
            controle.Mudou += Atualizar;
        }
    }

    private void OnDisable()
    {
        if (controle != null)
        {
            controle.Mudou -= Atualizar;
        }
    }

    private void Start()
    {
        Atualizar();
    }

    public void Atualizar()
    {
        if (controle == null)
        {
            return;
        }
        textoDoNome.text = controle.nomeDaArea;
        textoDoProgresso.text = string.Format(formato, controle.Desbloqueados, controle.Total);
    }
}
