using UnityEngine;

// Semáforo de pedestres do "Travessia Segura". Ciclo:
//   Vermelho (carros passam) -> Fechando (carros começam a parar; pedestre ainda espera)
//   -> Verde (pedestre atravessa) -> Piscando (termine de atravessar, não comece) -> Vermelho
// As "luzes" são os quadros acesos dos prefabs Semaforo_Pedestres da cena: ligar = mostrar o quadro.
public class TravessiaSemaforo : MonoBehaviour
{
    public enum Estado { Vermelho, Fechando, Verde, Piscando }

    // Os postes da cena (somem quando o nível não tem semáforo).
    public GameObject[] postes;
    public Renderer[] luzesPare;
    public Renderer[] luzesSiga;
    [Tooltip("Tempo entre os carros começarem a parar e o pedestre ganhar o verde.")]
    public float tempoFechando = 1.4f;

    public Estado EstadoAtual { get; private set; }
    public bool Ligado { get; private set; }

    // Verde firme: é quando pode começar a atravessar.
    public bool PedestrePodeComecar
    {
        get { return Ligado && EstadoAtual == Estado.Verde; }
    }

    public bool CarrosDevemParar
    {
        get { return Ligado && EstadoAtual != Estado.Vermelho; }
    }

    // true no quadro em que o verde abriu (pro manager tocar o som e avisar).
    public bool AcabouDeAbrir { get; private set; }

    private float tempoVerde, tempoVermelho, tempoPiscando, tempoNoEstado;

    public void Ligar(float verde, float vermelho, float piscando)
    {
        tempoVerde = verde;
        tempoVermelho = vermelho;
        tempoPiscando = piscando;
        Ligado = true;
        // Começa no vermelho, já na metade: a criança espera um pouco, mas não muito.
        EstadoAtual = Estado.Vermelho;
        tempoNoEstado = vermelho * 0.5f;
        foreach (GameObject poste in postes)
        {
            poste.SetActive(true);
        }
        Mostrar();
    }

    public void Desligar()
    {
        Ligado = false;
        foreach (GameObject poste in postes)
        {
            poste.SetActive(false);
        }
    }

    private void Update()
    {
        AcabouDeAbrir = false;
        if (!Ligado)
        {
            return;
        }

        tempoNoEstado += Time.deltaTime;
        float duracao = EstadoAtual == Estado.Vermelho ? tempoVermelho
            : EstadoAtual == Estado.Fechando ? tempoFechando
            : EstadoAtual == Estado.Verde ? tempoVerde : tempoPiscando;
        if (tempoNoEstado >= duracao)
        {
            tempoNoEstado = 0f;
            EstadoAtual = (Estado)(((int)EstadoAtual + 1) % 4);
            AcabouDeAbrir = EstadoAtual == Estado.Verde;
        }
        Mostrar();
    }

    private void Mostrar()
    {
        bool siga = EstadoAtual == Estado.Verde
            || (EstadoAtual == Estado.Piscando && Mathf.FloorToInt(tempoNoEstado * 4f) % 2 == 0);
        bool pare = EstadoAtual == Estado.Vermelho || EstadoAtual == Estado.Fechando;
        foreach (Renderer r in luzesSiga)
        {
            r.enabled = siga;
        }
        foreach (Renderer r in luzesPare)
        {
            r.enabled = pare;
        }
    }
}
