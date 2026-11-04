using System;
using UnityEngine;

public enum CorDoSinal
{
    Verde,
    Amarelo,
    Vermelho,
}

// Um semáforo da Central de Sinais: acende uma luz de cada vez nos postes ligados a ele.
// Serve pro semáforo de veículos (três luzes) e pro de pedestre (duas: sem lista de luzes amarelas).
// Pode trocar sozinho (ciclo do Motorista Mirim) ou só quando mandam (painel do Sinaleiro).
public class SinalDeTransito : MonoBehaviour
{
    public Renderer[] luzesVermelhas = new Renderer[0];
    public Renderer[] luzesAmarelas = new Renderer[0];
    public Renderer[] luzesVerdes = new Renderer[0];
    [Tooltip("Materiais de luz acesa e apagada. Sem o apagado, a luz é ligada e desligada mostrando ou escondendo o objeto (semáforo de pedestre).")]
    public Material materialVermelho;
    public Material materialAmarelo;
    public Material materialVerde;
    public Material materialApagado;

    public CorDoSinal Cor { get; private set; } = CorDoSinal.Vermelho;
    // Cor que estava acesa antes da atual.
    public CorDoSinal CorAnterior { get; private set; } = CorDoSinal.Vermelho;
    public float TempoNaCor { get; private set; }
    public bool DePedestre { get { return luzesAmarelas.Length == 0; } }
    public bool Automatico { get; private set; }

    // Avisa quem quiser tocar o "tic" ou atualizar a tela.
    public event Action<SinalDeTransito> Mudou;

    private readonly float[] duracao = new float[3];

    // Quanto falta pra cor atual trocar (só no ciclo automático).
    public float TempoQueFalta
    {
        get { return Automatico ? Mathf.Max(0f, duracao[(int)Cor] - TempoNaCor) : float.MaxValue; }
    }

    private void Awake()
    {
        Mostrar();
    }

    public void Mudar(CorDoSinal cor)
    {
        if (cor == CorDoSinal.Amarelo && DePedestre)
        {
            return;
        }
        if (cor == Cor)
        {
            return;
        }
        CorAnterior = Cor;
        Cor = cor;
        TempoNaCor = 0f;
        Mostrar();
        if (Mudou != null)
        {
            Mudou(this);
        }
    }

    // Liga o ciclo verde -> amarelo -> vermelho -> verde, começando numa cor que já dura "jaPassou" segundos.
    public void LigarCiclo(float verde, float amarelo, float vermelho, CorDoSinal inicial, float jaPassou)
    {
        duracao[(int)CorDoSinal.Verde] = verde;
        duracao[(int)CorDoSinal.Amarelo] = amarelo;
        duracao[(int)CorDoSinal.Vermelho] = vermelho;
        Automatico = true;
        Cor = inicial;
        CorAnterior = inicial == CorDoSinal.Verde ? CorDoSinal.Vermelho : inicial == CorDoSinal.Amarelo ? CorDoSinal.Verde : CorDoSinal.Amarelo;
        TempoNaCor = Mathf.Clamp(jaPassou, 0f, duracao[(int)inicial] - 0.05f);
        Mostrar();
    }

    // Recomeça a contar o tempo da cor acesa, sem trocar de cor.
    public void RecontarACor()
    {
        TempoNaCor = 0f;
    }

    public void DesligarCiclo()
    {
        Automatico = false;
    }

    private void Update()
    {
        TempoNaCor += Time.deltaTime;
        if (Automatico && TempoNaCor >= duracao[(int)Cor])
        {
            Mudar(Cor == CorDoSinal.Verde ? CorDoSinal.Amarelo : Cor == CorDoSinal.Amarelo ? CorDoSinal.Vermelho : CorDoSinal.Verde);
        }
    }

    private void Mostrar()
    {
        Acender(luzesVermelhas, materialVermelho, Cor == CorDoSinal.Vermelho);
        Acender(luzesAmarelas, materialAmarelo, Cor == CorDoSinal.Amarelo);
        Acender(luzesVerdes, materialVerde, Cor == CorDoSinal.Verde);
    }

    private void Acender(Renderer[] luzes, Material aceso, bool ligada)
    {
        foreach (Renderer luz in luzes)
        {
            if (luz == null)
            {
                continue;
            }
            if (materialApagado != null && aceso != null)
            {
                luz.sharedMaterial = ligada ? aceso : materialApagado;
            }
            else
            {
                luz.enabled = ligada;
            }
        }
    }
}
