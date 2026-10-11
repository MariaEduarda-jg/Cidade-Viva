using UnityEngine;
using UnityEngine.UI;

// Regras gerais do runner: velocidade que sobe com o tempo, pontuação (tempo + coletáveis),
// escudos e fim de jogo. Reaproveitável: cada minigame muda nomeDaArea e a RunnerConfig.
public class RunnerManager : MinigameBase
{
    public RunnerConfig config;
    public RunnerJogador jogador;
    public RunnerEscudos escudos;
    public Text textoPontuacao;

    private float tempo;
    private float pontosDeTempo;
    private int pontosDeColetaveis;

    public bool Jogando { get; private set; }
    public float Velocidade { get; private set; }
    public int Pontuacao => Mathf.FloorToInt(pontosDeTempo) + pontosDeColetaveis;

    private void Start()
    {
        Jogando = true;
        Velocidade = config.velocidadeInicial;
        jogador.Configurar(config.corDaBatida, config.duracaoDaBatida);
        AtualizarTexto();
    }

    private void Update()
    {
        if (!Jogando)
        {
            return;
        }

        tempo += Time.deltaTime;
        pontosDeTempo += config.pontosPorSegundo * Time.deltaTime;
        int aumentos = Mathf.FloorToInt(tempo / config.intervaloDeAumento);
        Velocidade = Mathf.Min(config.velocidadeMaxima, config.velocidadeInicial + aumentos * config.aumentoDeVelocidade);
        AtualizarTexto();
    }

    // Chamado pelo RunnerObjeto quando encosta no jogador.
    public void Tocou(RunnerObjeto objeto)
    {
        // Desativa na hora: o Destroy só acontece no fim do frame.
        objeto.gameObject.SetActive(false);
        Destroy(objeto.gameObject);

        if (objeto.Tipo == TipoRunnerObjeto.Coletavel)
        {
            pontosDeColetaveis += objeto.Pontos;
            AtualizarTexto();
            return;
        }

        if (objeto.Tipo == TipoRunnerObjeto.Protecao)
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
        FinalizarJogo(Pontuacao, config.tituloDoFim);
    }

    private void AtualizarTexto()
    {
        if (textoPontuacao != null)
        {
            textoPontuacao.text = string.Format(config.formatoPontuacao, Pontuacao);
        }
    }
}
