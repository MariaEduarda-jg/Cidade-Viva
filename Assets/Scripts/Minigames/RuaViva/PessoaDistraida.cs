using UnityEngine;

// Um pedestre ou ciclista à direita da rua. Dois tipos:
//   Distraída: vem andando em direção à rua sem ter visto o carro. É a hora de buzinar. Se ouve, para na
//     beira, espera o carro passar e atravessa depois. Se ninguém buzina, o motorista freia e ela passa.
//   Atenta: anda pela calçada, ao longo da rua, e não entra nela. Ninguém precisa buzinar.
public class PessoaDistraida : MonoBehaviour
{
    public enum Fase { Longe, Vindo, Ouviu, Atravessando, Foi, NaCalcada }

    public Fase Etapa { get; private set; }
    public bool EmPerigo { get { return Etapa == Fase.Vindo; } }

    private RuaVivaPercurso percurso;
    private RuaVivaCarroManager manager;
    private float sDoEvento;
    private float distanciaDeComeco;
    private float distanciaDaFreada;
    private float lateral;
    private float beira;
    private float velocidadeVindo;
    private float velocidadeDeAtravessar;
    private float andouNaCalcada;
    private bool passou;
    private bool atenta;
    private Animator animador;
    private static readonly int Velocidade = Animator.StringToHash("Velocidade");

    public void Preparar(RuaVivaCarroManager manager, RuaVivaPercurso percurso, float s, bool atenta, float inicio, float beira,
        float janela, float distanciaDeFreada, float velocidadeDoCarro, float velocidadeDeAtravessar)
    {
        this.manager = manager;
        this.percurso = percurso;
        sDoEvento = s;
        this.atenta = atenta;
        lateral = atenta ? 4.6f : inicio;
        this.beira = beira;
        this.velocidadeDeAtravessar = velocidadeDeAtravessar;
        distanciaDaFreada = distanciaDeFreada;
        distanciaDeComeco = distanciaDeFreada + velocidadeDoCarro * janela;
        velocidadeVindo = (inicio - beira) / Mathf.Max(0.5f, janela);
        animador = GetComponentInChildren<Animator>();
        Etapa = Fase.Longe;
        Colocar(-1f);
        Andar(false);
    }

    private void Colocar(float paraOnde)
    {
        transform.position = percurso.Posicao(sDoEvento) + percurso.Direita(sDoEvento) * lateral;
        // paraOnde: -1 = pra rua, 2 = olha pro carro, 3 = ao longo da rua
        Vector3 frente = paraOnde < 0f ? -percurso.Direita(sDoEvento) : paraOnde > 2.5f ? percurso.Direcao(sDoEvento) : -percurso.Direcao(sDoEvento);
        transform.rotation = Quaternion.LookRotation(frente);
    }

    private void Andar(bool anda)
    {
        if (animador != null)
        {
            animador.SetFloat(Velocidade, anda ? 0.5f : 0f);
        }
    }

    public void OuviuABuzina()
    {
        if (Etapa == Fase.Vindo)
        {
            Etapa = Fase.Ouviu;
            Colocar(2f);
            Andar(false);
        }
    }

    private void Update()
    {
        if (percurso == null)
        {
            return;
        }
        float d = sDoEvento - percurso.S;
        switch (Etapa)
        {
            case Fase.Longe:
                if (d <= distanciaDeComeco)
                {
                    Etapa = atenta ? Fase.NaCalcada : Fase.Vindo;
                    Andar(true);
                }
                break;
            case Fase.Vindo:
                lateral = Mathf.MoveTowards(lateral, beira, velocidadeVindo * Time.deltaTime);
                Colocar(-1f);
                if (d <= distanciaDaFreada)
                {
                    Etapa = Fase.Atravessando;
                    manager.PerdeuABuzina(this);
                }
                break;
            case Fase.Ouviu:
                if (d < -6f)
                {
                    Etapa = Fase.Atravessando;
                    passou = true;
                    Andar(true);
                }
                break;
            case Fase.Atravessando:
                lateral -= velocidadeDeAtravessar * Time.deltaTime;
                Colocar(-1f);
                if (!passou && lateral < -3f)
                {
                    // Saiu da frente do carro.
                    passou = true;
                    manager.CaminhoLivre();
                }
                if (lateral < -9f)
                {
                    Etapa = Fase.Foi;
                    gameObject.SetActive(false);
                }
                break;
            case Fase.NaCalcada:
                andouNaCalcada += 1.4f * Time.deltaTime;
                sDoEvento += 1.4f * Time.deltaTime;
                Colocar(3f);
                if (andouNaCalcada > 25f)
                {
                    Etapa = Fase.Foi;
                    gameObject.SetActive(false);
                }
                break;
        }
    }
}
