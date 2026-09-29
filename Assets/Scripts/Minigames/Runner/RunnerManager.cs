using UnityEngine;
using UnityEngine.UI;

// Regras gerais do runner: velocidade que sobe com o tempo, pontuação (tempo + coletáveis),
// escudos e fim de jogo. Reaproveitável: cada minigame muda nomeDaArea e os números no Inspector.
public class RunnerManager : MonoBehaviour
{
    public string nomeDaArea = "Corre, Correndo!";
    public RunnerJogador jogador;
    public RunnerEscudos escudos;

    [Header("Velocidade")]
    public float velocidadeInicial = 350f;
    public float aumentoDeVelocidade = 50f;
    public float intervaloDeAumento = 10f;
    public float velocidadeMaxima = 1000f;

    [Header("Pontuação")]
    public float pontosPorSegundo = 10f;
    public Text textoPontuacao;

    public TelaFimDeJogo telaFimDeJogo;

    private float tempo;
    private float pontosDeTempo;
    private int pontosDeColetaveis;

    public bool Jogando { get; private set; }
    public float Velocidade { get; private set; }
    public int Pontuacao => Mathf.FloorToInt(pontosDeTempo) + pontosDeColetaveis;

    private void Start()
    {
        Jogando = true;
        Velocidade = velocidadeInicial;
        telaFimDeJogo.gameObject.SetActive(false);
        AtualizarTexto();
    }

    private void Update()
    {
        if (!Jogando)
        {
            return;
        }

        tempo += Time.deltaTime;
        pontosDeTempo += pontosPorSegundo * Time.deltaTime;
        int aumentos = Mathf.FloorToInt(tempo / intervaloDeAumento);
        Velocidade = Mathf.Min(velocidadeMaxima, velocidadeInicial + aumentos * aumentoDeVelocidade);
        AtualizarTexto();
    }

    // Chamado pelo RunnerObjeto quando encosta no jogador.
    public void Tocou(RunnerObjeto objeto)
    {
        // Desativa na hora: o Destroy só acontece no fim do frame.
        objeto.gameObject.SetActive(false);
        Destroy(objeto.gameObject);

        if (objeto.tipo == TipoRunnerObjeto.Coletavel)
        {
            pontosDeColetaveis += objeto.pontos;
            AtualizarTexto();
            return;
        }

        if (objeto.tipo == TipoRunnerObjeto.Protecao)
        {
            jogador.AtivarProtecao();
            return;
        }

        // A proteção (ex.: capacete) segura uma batida sem gastar escudo.
        if (jogador.UsarProtecao())
        {
            return;
        }

        escudos.Perder();
        jogador.MostrarBatida();
        if (escudos.Restantes == 0)
        {
            FimDeJogo();
        }
    }

    private void FimDeJogo()
    {
        Jogando = false;
        jogador.enabled = false;
        telaFimDeJogo.Mostrar(nomeDaArea, Pontuacao);
    }

    private void AtualizarTexto()
    {
        if (textoPontuacao != null)
        {
            textoPontuacao.text = "Pontos: " + Pontuacao;
        }
    }
}
