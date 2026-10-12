using UnityEngine;

// O que há no lugar de um problema das Obras da Cidade.
// Os três primeiros são os obstáculos do modo "Pontes e Passagens"; os outros são as situações do
// modo "Planejar a rua". Valores novos entram sempre no fim (os assets guardam o número).
public enum ObstaculoDaObra
{
    Rio,
    Morro,
    Via,
    // Ligação entre uma propriedade rural e a cidade.
    CampoECidade,
    // Ligação dentro da cidade, perto de casas, prédios e lojas.
    DentroDaCidade,
    // Ligação entre dois lugares que não são nem cidade nem propriedade rural.
    LugarQualquer,
    // A beira de uma rua por onde passam carros (a obra faz parte da rua).
    BeiraDaRua,
    // Um caminho longe do trânsito dos veículos.
    SeparadoDoTransito,
    // O veículo precisa passar a andar no sentido contrário.
    MudarDeSentido,
    // Vários veículos precisam ser guardados.
    GuardarVeiculos,
}

// Quem precisa passar (ou usar a obra).
public enum QuemPassaNaObra
{
    Veiculo,
    Pedestre,
    Animal,
    Ciclista,
}

// Um problema das Obras da Cidade: um caminho cortado por um obstáculo, alguém que precisa passar e as
// estruturas oferecidas na barra. Um asset por problema, em Assets/Resources/Obras.
// Regra de ouro: entre as opções, SÓ UMA serve pelas definições do glossário (o menu
// Cidade Viva > Verbetes > Conferir problemas das Obras acusa o problema que quebrar isso).
[CreateAssetMenu(fileName = "Problema_", menuName = "Cidade Viva/Obras: problema")]
public class ObraProblema : ScriptableObject
{
    public const string PastaNoResources = "Obras";

    [Tooltip("0 = Pontes e Passagens, 1 = Planejar a rua.")]
    public int modo;
    [Tooltip("Em qual nível aparece (1 a 5) e em que ordem dentro do nível.")]
    public int nivel = 1;
    public int ordem;

    [Header("O problema")]
    [TextArea]
    [Tooltip("Frase que aparece no alto da tela. Vazio = \"<quem passa> precisa atravessar <obstáculo>.\"")]
    public string enunciado;
    public ObstaculoDaObra obstaculo;
    [Tooltip("Só aparência: a via é uma rua comum (estreita, poucos carros) em vez de avenida. Não muda a regra.")]
    public bool ruaComum;
    public QuemPassaNaObra quemPassa;
    [Tooltip("Com artigo e maiúscula, como entra na frase: \"O carro\", \"A menina\".")]
    public string nomeDeQuemPassa = "O carro";
    [Tooltip("Prefab de quem passa (a frente dele aponta pra +Z).")]
    public GameObject modeloDeQuemPassa;

    [Header("Barra de ferramentas")]
    [Tooltip("Ids dos verbetes oferecidos (ex.: \"ponte\", \"tunel\"). Só um pode servir.")]
    public string[] opcoes = new string[0];
    [Tooltip("Id do verbete da estrutura certa.")]
    public string correta;
    [TextArea]
    [Tooltip("Trecho da definição da estrutura certa que justifica a escolha, copiado letra por letra.")]
    public string trechoDaDefinicao;

    [Header("Nível da ambulância")]
    [Tooltip("Este problema faz parte do caminho da ambulância até o hospital (os trechos ficam em fila).")]
    public bool rotaDaAmbulancia;

    [TextArea]
    [Tooltip("Anotações da equipe (de onde veio cada escolha). Não aparece no jogo.")]
    public string observacao;
}
