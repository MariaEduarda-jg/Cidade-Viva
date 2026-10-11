using System.Collections;
using UnityEngine;

// A criança que corre. Fica parada em z = 0 (quem anda é a pista); aqui ela só troca de faixa,
// pula, desliza e faz as cenas de batida, queda e comemoração. Quem decide as regras é o CorreManager.
// O visual é um personagem animado de Assets/Prefabs/Art/Personagens (estado "Locomocao" com o
// parâmetro "Velocidade", mais estados soltos como "Jump", "Roll" e "Victory").
public class CorreJogador : MonoBehaviour
{
    public Animator animator;
    // Filho que gira e encolhe nas cenas, sem mexer na posição lógica.
    public Transform visual;
    public string[] estadosDePulo = { "Jump" };
    public string[] estadosDeDeslize = { "Roll" };
    public string[] estadosDeBatida = { "RecieveHit" };
    public string[] estadosDeQueda = { "Defeat", "SitDown", "Death" };
    public string[] estadosDeVitoria = { "Victory", "Jump" };

    public int Faixa { get; private set; }
    // Altura dos pés acima do chão.
    public float Altura { get; private set; }
    public bool Pulando { get; private set; }
    public bool Deslizando { get; private set; }
    public bool Invencivel { get; private set; }
    public float X
    {
        get { return transform.position.x; }
    }

    private static readonly int Velocidade = Animator.StringToHash("Velocidade");

    private CorreConfig config;
    private bool correndo;
    private bool emCena;
    private float tempoDoPulo;
    private float tempoDoDeslize;
    private float velocidadeLateral;
    private float alturaDoChao;
    private Renderer[] renderers;
    private Coroutine cenaDaBatida;

    public void Preparar(CorreConfig config)
    {
        this.config = config;
        alturaDoChao = transform.position.y;
        renderers = visual.GetComponentsInChildren<Renderer>(true);
        Faixa = 0;
    }

    public void Correr()
    {
        correndo = true;
        Tocar("Locomocao");
    }

    private void Update()
    {
        if (config == null)
        {
            return;
        }

        if (animator != null)
        {
            animator.SetFloat(Velocidade, correndo && !emCena ? 1f : 0f, 0.08f, Time.deltaTime);
        }

        Vector3 p = transform.position;
        p.x = Mathf.SmoothDamp(p.x, Faixa * config.larguraDaFaixa, ref velocidadeLateral, config.tempoDeTrocaDeFaixa * 0.5f);

        if (Pulando)
        {
            tempoDoPulo += Time.deltaTime;
            float k = tempoDoPulo / config.duracaoDoPulo;
            if (k >= 1f)
            {
                Pulando = false;
                Altura = 0f;
                VoltarACorrer();
            }
            else
            {
                Altura = 4f * config.alturaDoPulo * k * (1f - k);
            }
        }

        if (Deslizando)
        {
            tempoDoDeslize += Time.deltaTime;
            if (tempoDoDeslize >= config.duracaoDoDeslize)
            {
                PararDeDeslizar();
                VoltarACorrer();
            }
        }

        p.y = alturaDoChao + Altura;
        transform.position = p;
    }

    // ------------------------------------------------------------------ ações

    public bool MudarDeFaixa(int lado)
    {
        int nova = Mathf.Clamp(Faixa + lado, -1, 1);
        if (emCena || nova == Faixa)
        {
            return false;
        }
        Faixa = nova;
        return true;
    }

    public bool Pular()
    {
        if (emCena || Pulando)
        {
            return false;
        }
        if (Deslizando)
        {
            PararDeDeslizar();
        }
        Pulando = true;
        tempoDoPulo = 0f;
        TocarPrimeiro(estadosDePulo);
        return true;
    }

    public bool Deslizar()
    {
        if (emCena || Pulando || Deslizando)
        {
            return false;
        }
        Deslizando = true;
        tempoDoDeslize = 0f;
        // Sem animação de rolar, o corpo só se abaixa.
        if (!TocarPrimeiro(estadosDeDeslize))
        {
            visual.localScale = new Vector3(1f, 0.5f, 1f);
        }
        return true;
    }

    private void PararDeDeslizar()
    {
        Deslizando = false;
        visual.localScale = Vector3.one;
    }

    private void VoltarACorrer()
    {
        if (!emCena && !Pulando && !Deslizando)
        {
            Tocar("Locomocao");
        }
    }

    // ------------------------------------------------------------------ cenas

    // Batida cômica: um rodopio e um tempo piscando, sem bater de novo. A corrida continua.
    public void Bater()
    {
        if (cenaDaBatida != null)
        {
            StopCoroutine(cenaDaBatida);
        }
        cenaDaBatida = StartCoroutine(Batida());
    }

    private IEnumerator Batida()
    {
        Invencivel = true;
        if (!Pulando && !Deslizando)
        {
            TocarPrimeiro(estadosDeBatida);
        }

        const float giro = 0.45f;
        for (float t = 0f; t < config.tempoInvencivel; t += Time.deltaTime)
        {
            visual.localRotation = t < giro ? Quaternion.Euler(0f, 360f * t / giro, 0f) : Quaternion.identity;
            Mostrar(Mathf.FloorToInt(t / 0.1f) % 2 == 0);
            if (t >= giro && t - Time.deltaTime < giro)
            {
                VoltarACorrer();
            }
            yield return null;
        }
        visual.localRotation = Quaternion.identity;
        Mostrar(true);
        Invencivel = false;
        cenaDaBatida = null;
    }

    // Acabaram os escudos: senta no chão, sem drama.
    public void Cair()
    {
        EntrarEmCena();
        TocarPrimeiro(estadosDeQueda);
    }

    public void Comemorar()
    {
        EntrarEmCena();
        visual.localRotation = Quaternion.Euler(0f, 180f, 0f);
        TocarPrimeiro(estadosDeVitoria);
    }

    private void EntrarEmCena()
    {
        StopAllCoroutines();
        cenaDaBatida = null;
        emCena = true;
        Invencivel = true;
        Pulando = false;
        Altura = 0f;
        PararDeDeslizar();
        visual.localRotation = Quaternion.identity;
        Mostrar(true);
    }

    private void Mostrar(bool visivel)
    {
        foreach (Renderer r in renderers)
        {
            r.enabled = visivel;
        }
    }

    private bool TocarPrimeiro(string[] estados)
    {
        foreach (string estado in estados)
        {
            if (Tocar(estado))
            {
                return true;
            }
        }
        return false;
    }

    private bool Tocar(string estado)
    {
        if (animator == null || !animator.isActiveAndEnabled || !animator.HasState(0, Animator.StringToHash(estado)))
        {
            return false;
        }
        animator.CrossFade(estado, 0.08f, 0);
        return true;
    }
}
