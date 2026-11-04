using UnityEngine;

// Um botão do painel do Sinaleiro: acende uma cor num semáforo.
// grupo: 0 = via A, 1 = via B, 2 = semáforo de pedestre.
public class BotaoDeSinal : MonoBehaviour
{
    public SinaisSinaleiro sinaleiro;
    public int grupo;
    public CorDoSinal cor;
    [Tooltip("Aro que aparece em volta do botão da luz que está acesa.")]
    public GameObject aro;

    public void Tocar()
    {
        if (grupo == 2)
        {
            sinaleiro.MudarSinalDePedestre(cor == CorDoSinal.Verde);
        }
        else
        {
            sinaleiro.MudarSinal(grupo, cor);
        }
    }

    public void Destacar(bool acesa)
    {
        aro.SetActive(acesa);
        transform.localScale = Vector3.one * (acesa ? 1.12f : 0.92f);
    }
}
