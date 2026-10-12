using UnityEngine;

// Tudo que vale pra todos os níveis da Oficina de Veículos: textos, sons, pontuação e medidas.
// O que muda de um nível pro outro fica no LevelConfig (Niveis_Oficina); o que cada veículo pede
// fica na VeiculoReceita dele. Nenhum texto daqui repete nem resume definição de verbete.
[CreateAssetMenu(fileName = "Oficina_Config", menuName = "Cidade Viva/Oficina: config")]
public class OficinaConfig : ScriptableObject
{
    [Header("Nomes das peças, na ordem do TipoDePeca")]
    public string[] nomesDasPecas =
    {
        "roda", "pedal", "motor", "motor elétrico", "trilho", "sirene", "luzes", "carroceria", "engate", "assento",
        "asa", "hélice", "âncora", "vela de barco", "remo", "cobertura",
    };

    [Header("Peças erradas na prateleira, na ordem em que entram")]
    public TipoDePeca[] ordemDasPecasErradas =
    {
        TipoDePeca.Asa, TipoDePeca.Trilho, TipoDePeca.Helice, TipoDePeca.Ancora, TipoDePeca.VelaDeBarco, TipoDePeca.Remo,
        TipoDePeca.Sirene, TipoDePeca.Pedal, TipoDePeca.Motor,
    };

    [Header("Textos da montagem")]
    public string formatoDoNivel = "Nível {0}  ·  Veículo {1} de {2}";
    public string recadoInicial = "Leia a definição e arraste as peças para a bancada.";
    public string recadoDeAcerto = "Isso! Encaixou.";
    public string recadoDeVeiculoPronto = "Pronto! Agora vamos testar.";
    public string recadoDeTempoEsgotado = "O tempo acabou. Termine com calma!";
    // {0} = lista de peças (ex.: "2 × roda, 1 × pedal").
    public string formatoDaDica = "Peças: {0}";
    public string formatoDeItemDaDica = "{0} × {1}";
    public string sufixoOuMais = " (ou mais)";
    // Peça que o veículo não tem. {0} = nome da peça, {1} = palavra do verbete.
    public string balaoForaDaDefinicao = "{1} não tem {0}. Tente outra peça!";
    // Peça extra (o veículo tem, a definição não cita): encaixa sem erro. {0} = nome da peça.
    public string recadoDeExtra = "Encaixou! A definição não fala de {0}, mas pode ter.";
    public string balaoDeExtraRepetido = "Essa peça já está no veículo.";
    // Peça que a definição exclui (reboque "sem motor"). {0} = trecho da definição.
    public string balaoDeProibida = "A definição diz: \"{0}\". Essa peça não serve.";
    // Peça de base padrão (a definição não fala dela) que já está completa.
    public string balaoDeBaseCompleta = "Já tem todas as peças desse tipo!";
    // {0} = trecho da definição.
    public string balaoJaCompleto = "A definição diz: \"{0}\". Já está completo!";
    // {0} = palavra do verbete citado, {1} = trecho da definição dele.
    public string balaoJaCompletoDeOutroVerbete = "A definição de {0} diz: \"{1}\". Já está completo!";
    public string[] rotulosDosCriterios = { "Sem erro", "No tempo", "Sem dica" };

    [Header("Textos do teste na pista")]
    public string recadoDoTeste = "Dirija com as setas e pegue as 3 moedas!";
    public string recadoDoFimDoTeste = "Funcionou! Veja o que você montou.";
    public string recadoDoTesteNosTrilhos = "Acelere pelos trilhos e pegue as 3 moedas!";
    // {0} = palavra do veículo que puxa.
    public string recadoDeEngatar = "Sem motor ele não anda. Aperte Engatar!";
    public string recadoDeEngatado = "Engatou! Agora dirija e pegue as 3 moedas!";

    [Header("Montagem")]
    [Tooltip("Tempo, em segundos, que o balão de explicação fica na tela.")]
    public float tempoDoBalao = 4f;
    public float tempoDeVooDaPeca = 0.28f;
    [Tooltip("Comprimento do veículo em cima da bancada, em metros (o modelo é ajustado pra esse tamanho).")]
    public float comprimentoDoVeiculoNaBancada = 2.1f;

    [Header("Pontuação")]
    public int pontosPorVeiculo = 100;
    public int descontoPorErro = 10;
    public int minimoPorVeiculo = 40;

    [Header("Teste na pista")]
    public float velocidadeMaxima = 4.5f;
    public float aceleracao = 6f;
    public float giroEmGrausPorSegundo = 150f;
    public float distanciaPraPegarMoeda = 2f;
    [Tooltip("Se a criança não pegar as moedas nesse tempo, o teste termina do mesmo jeito (sem punição).")]
    public float tempoMaximoDoTeste = 40f;
    public float escalaDoVeiculoNaPista = 1.6f;

    [Header("Sons")]
    public AudioClip somDePegar;
    public AudioClip somDeEncaixe;
    public AudioClip somDeErro;
    public AudioClip somDeAcerto;
    public AudioClip somDeDica;
    public AudioClip somDeMotorLigando;
    public AudioClip somDeCampainha;
    public AudioClip somDeMoeda;
    public AudioClip somDeEngate;

    public string NomeDaPeca(TipoDePeca tipo)
    {
        int i = (int)tipo;
        return i >= 0 && i < nomesDasPecas.Length ? nomesDasPecas[i] : tipo.ToString();
    }
}
