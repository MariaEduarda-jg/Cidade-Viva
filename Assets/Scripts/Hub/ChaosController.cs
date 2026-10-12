using System;
using UnityEngine;

// Caos de uma área do Hub que melhora conforme a criança desbloqueia os verbetes do grupo daquela área.
// O progresso (verbetes desbloqueados / total do grupo, lido do perfil ativo) cai numa de quatro faixas:
//   0-25%   tudo bagunçado: os 4 grupos de caos ligados, nenhum de ordem;
//   25-50%  some o 1º grupo de caos, entra o 1º de ordem;
//   50-75%  somem dois de caos, entram dois de ordem;
//   75-100% só resta o último de caos; com 100% some tudo e entram os 4 de ordem.
// Junto, a cor do cenário da área (SaturacaoDaArea) e a luz da área sobem aos poucos com o progresso.
// Os grupos de objetos são montados pelo HubConstrutor; este script só liga e desliga.
public class ChaosController : MonoBehaviour
{
    [Tooltip("Nome da área, como aparece no letreiro.")]
    public string nomeDaArea;

    [Tooltip("Grupos de verbetes que contam pro progresso desta área.")]
    public GrupoDeVerbete[] grupos = new GrupoDeVerbete[0];

    [Header("Objetos (um grupo por faixa)")]
    [Tooltip("Lixo, lama, carros virados, placas caídas... O grupo 1 é o primeiro a sumir.")]
    public GameObject[] caosPorFaixa = new GameObject[0];
    [Tooltip("Flores, placas no lugar, árvores, pássaros... O grupo 1 é o primeiro a aparecer.")]
    public GameObject[] ordemPorFaixa = new GameObject[0];

    [Header("Cor e luz")]
    public SaturacaoDaArea saturacao;
    public Light luzDaArea;
    public float intensidadeDaLuzCheia = 1.2f;
    [Tooltip("Quanto a cor e a luz andam por segundo até chegar ao valor do progresso.")]
    public float velocidadeDaTransicao = 0.6f;

    public int Desbloqueados { get; private set; }
    public int Total { get; private set; }
    // 0 a 1.
    public float Progresso { get; private set; }
    // 0 a 3 pelas faixas; 4 quando a área está completa.
    public int Faixa { get; private set; }
    public bool Simulando { get { return simulado >= 0f; } }

    // Avisa o letreiro da área.
    public event Action Mudou;

    private float simulado = -1f;
    private float corAtual = -1f;

    private void OnEnable()
    {
        GameProgress.VerbetesMudaram += Atualizar;
    }

    private void OnDisable()
    {
        GameProgress.VerbetesMudaram -= Atualizar;
    }

    private void Start()
    {
        Atualizar();
    }

    // Só pra testes (menu Cidade Viva > Teste): finge um progresso de 0 a 1 nesta área.
    public void Simular(float progresso)
    {
        simulado = Mathf.Clamp01(progresso);
        Atualizar();
    }

    public void PararDeSimular()
    {
        simulado = -1f;
        Atualizar();
    }

    // Reconta os verbetes do perfil ativo e liga/desliga os grupos de objetos.
    public void Atualizar()
    {
        Total = 0;
        Desbloqueados = 0;
        foreach (GrupoDeVerbete grupo in grupos)
        {
            foreach (Verbete v in VerbeteDatabase.Instancia.ListarPorGrupo(grupo))
            {
                Total++;
                if (GameProgress.VerbeteDesbloqueado(v.id))
                {
                    Desbloqueados++;
                }
            }
        }

        Progresso = Total > 0 ? Desbloqueados / (float)Total : 0f;
        if (Simulando)
        {
            Progresso = simulado;
            Desbloqueados = Mathf.RoundToInt(simulado * Total);
        }
        Faixa = Progresso >= 1f ? 4 : Mathf.Clamp(Mathf.FloorToInt(Progresso * 4f), 0, 3);

        for (int i = 0; i < caosPorFaixa.Length; i++)
        {
            if (caosPorFaixa[i] != null)
            {
                caosPorFaixa[i].SetActive(Faixa <= i);
            }
        }
        for (int i = 0; i < ordemPorFaixa.Length; i++)
        {
            if (ordemPorFaixa[i] != null)
            {
                ordemPorFaixa[i].SetActive(Faixa > i);
            }
        }

        // Na primeira vez a cor já nasce no valor certo; depois muda aos poucos (ver Update).
        if (corAtual < 0f)
        {
            AplicarCor(Progresso);
        }
        if (Mudou != null)
        {
            Mudou();
        }
    }

    private void Update()
    {
        if (corAtual >= 0f && !Mathf.Approximately(corAtual, Progresso))
        {
            AplicarCor(Mathf.MoveTowards(corAtual, Progresso, velocidadeDaTransicao * Time.deltaTime));
        }
    }

    private void AplicarCor(float valor)
    {
        corAtual = valor;
        if (saturacao != null)
        {
            saturacao.Aplicar(valor);
        }
        if (luzDaArea != null)
        {
            luzDaArea.intensity = intensidadeDaLuzCheia * valor;
        }
    }
}
