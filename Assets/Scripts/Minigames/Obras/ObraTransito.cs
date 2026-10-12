using UnityEngine;

// Carros passando pela via de um problema das Obras da Cidade (a via corre em X).
// Com "Parado", os carros que ainda não chegaram na faixa esperam antes dela; os que já estão em
// cima terminam de passar. É o "quando o veículo para" da definição de faixa de pedestre.
public class ObraTransito : MonoBehaviour
{
    public Transform[] carros = new Transform[0];
    [Tooltip("+1 = anda pra +X, -1 = anda pra -X (um por carro).")]
    public float[] sentidos = new float[0];
    public float velocidade = 4f;
    [Tooltip("O carro some numa ponta e volta na outra, a esta distância do meio.")]
    public float meiaLargura = 10f;
    [Tooltip("Metade da largura da faixa de pedestre, mais a folga pro carro parar antes.")]
    public float zonaDaFaixa = 4.6f;

    public bool Parado { get; set; }

    // true quando nenhum carro está em cima da travessia.
    public bool FaixaLivre
    {
        get
        {
            foreach (Transform c in carros)
            {
                if (Mathf.Abs(c.localPosition.x) < zonaDaFaixa - 0.3f)
                {
                    return false;
                }
            }
            return true;
        }
    }

    private void Update()
    {
        for (int i = 0; i < carros.Length; i++)
        {
            Vector3 p = carros[i].localPosition;
            float sentido = sentidos[i];
            float novo = p.x + sentido * velocidade * Time.deltaTime;
            bool indoPraFaixa = sentido * p.x < 0f;
            if (Parado && indoPraFaixa && Mathf.Abs(p.x) >= zonaDaFaixa && Mathf.Abs(novo) < zonaDaFaixa)
            {
                continue;
            }
            if (novo > meiaLargura)
            {
                novo -= meiaLargura * 2f;
            }
            else if (novo < -meiaLargura)
            {
                novo += meiaLargura * 2f;
            }
            p.x = novo;
            carros[i].localPosition = p;
        }
    }
}
