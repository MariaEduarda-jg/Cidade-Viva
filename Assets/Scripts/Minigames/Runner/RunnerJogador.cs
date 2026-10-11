using UnityEngine;
using UnityEngine.UI;

// Personagem do runner: fica numa faixa por vez e troca com as setas esquerda/direita (ou A/D).
public class RunnerJogador : MonoBehaviour
{
    public RunnerFaixas faixas;
    public int faixaInicial = 1;
    // Efeito ligado enquanto a proteção está ativa (ex.: um Outline colorido). Opcional.
    public Behaviour efeitoDeProtecao;

    // Vêm da RunnerConfig, pelo RunnerManager.
    private Color corDaBatida = new Color(1f, 0.3f, 0.3f);
    private float duracaoDaBatida = 0.3f;
    private RectTransform rectTransform;
    private Graphic grafico;
    private Color corOriginal;
    private int faixaAtual;
    private float fimDaBatida;

    public RectTransform RectTransform => rectTransform;
    public bool Protegido { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        grafico = GetComponent<Graphic>();
        if (grafico != null)
        {
            corOriginal = grafico.color;
        }

        faixaAtual = faixaInicial;
        PosicionarNaFaixa();
        AtualizarEfeitoDeProtecao();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            TrocarDeFaixa(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            TrocarDeFaixa(1);
        }

        if (grafico != null)
        {
            grafico.color = Time.time < fimDaBatida ? corDaBatida : corOriginal;
        }
    }

    public void Configurar(Color corAoBater, float duracaoAoBater)
    {
        corDaBatida = corAoBater;
        duracaoDaBatida = duracaoAoBater;
    }

    // Feedback visual rápido quando bate num obstáculo.
    public void MostrarBatida()
    {
        fimDaBatida = Time.time + duracaoDaBatida;
    }

    public void AtivarProtecao()
    {
        Protegido = true;
        AtualizarEfeitoDeProtecao();
    }

    // Gasta a proteção, se houver. Retorna true se ela segurou a batida.
    public bool UsarProtecao()
    {
        if (!Protegido)
        {
            return false;
        }

        Protegido = false;
        AtualizarEfeitoDeProtecao();
        return true;
    }

    private void AtualizarEfeitoDeProtecao()
    {
        if (efeitoDeProtecao != null)
        {
            efeitoDeProtecao.enabled = Protegido;
        }
    }

    private void TrocarDeFaixa(int direcao)
    {
        faixaAtual = Mathf.Clamp(faixaAtual + direcao, 0, faixas.quantidade - 1);
        PosicionarNaFaixa();
    }

    private void PosicionarNaFaixa()
    {
        Vector2 pos = rectTransform.anchoredPosition;
        pos.x = faixas.PosicaoX(faixaAtual);
        rectTransform.anchoredPosition = pos;
    }
}
