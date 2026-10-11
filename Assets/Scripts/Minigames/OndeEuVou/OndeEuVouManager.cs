using UnityEngine;

// Conta acertos e erros do "Onde Eu Vou?" e finaliza o minigame quando todas as placas estão no lugar.
public class OndeEuVouManager : MinigameBase
{
    public OndeEuVouConfig config;
    public PlacaArrastavel[] placas;

    private int acertos;
    private int erros;

    private void Start()
    {
        foreach (PlacaArrastavel placa in placas)
        {
            PlacaConfig aparencia = config.BuscarPlaca(placa.tipo);
            if (aparencia != null)
            {
                placa.Aplicar(aparencia);
            }
        }
    }

    public void RegistrarErro()
    {
        erros++;
    }

    public void RegistrarAcerto()
    {
        acertos++;
        if (acertos < placas.Length)
        {
            return;
        }

        int pontuacao = Mathf.Max(0, config.pontuacaoMaxima - erros * config.penalidadePorErro);
        FinalizarJogo(pontuacao, config.tituloDoFim);
    }
}
