using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Fim de jogo")]
    public GameObject telaFimDeJogo;
    public Text textoFimDeJogo;

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
        if (telaFimDeJogo != null)
        {
            telaFimDeJogo.SetActive(false);
        }

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

    public void TentarDeNovo()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void FimDeJogo()
    {
        Jogando = false;
        jogador.enabled = false;

        int pontuacao = Pontuacao;
        bool novoRecorde = pontuacao > GameProgress.ObterRecorde(nomeDaArea);
        GameProgress.FinalizarMinigame(nomeDaArea, pontuacao);

        if (textoFimDeJogo != null)
        {
            textoFimDeJogo.text = "Fim de jogo!\nPontuação: " + pontuacao
                + (novoRecorde ? "\nNovo recorde!" : "\nRecorde: " + GameProgress.ObterRecorde(nomeDaArea));
        }

        if (telaFimDeJogo != null)
        {
            telaFimDeJogo.SetActive(true);
        }
    }

    private void AtualizarTexto()
    {
        if (textoPontuacao != null)
        {
            textoPontuacao.text = "Pontos: " + Pontuacao;
        }
    }
}
