using System;
using System.Collections;
using UnityEngine;

// A criança que atravessa. Só executa movimento e animação; quem decide se pode andar é o TravessiaManager.
// O visual é um personagem animado de Assets/Prefabs/Art/Personagens (controlador com o estado
// "Locomocao" e o parâmetro "Velocidade", mais estados soltos como "Roll" e "Victory").
public class TravessiaJogador : MonoBehaviour
{
    public Animator animator;
    // Filho que gira pra olhar pros lados e pra tropeçar, sem mexer na posição lógica.
    public Transform visual;
    [Tooltip("Estados de animação tentados no tropeço e na comemoração, na ordem.")]
    public string[] estadosDeTropeco = { "Roll", "RecieveHit", "Defeat" };
    public string[] estadosDeVitoria = { "Victory", "Jump" };

    public bool Ocupado { get; private set; }

    private static readonly int Velocidade = Animator.StringToHash("Velocidade");
    private float velocidadeDaAnimacao;

    private void Update()
    {
        if (animator != null)
        {
            animator.SetFloat(Velocidade, velocidadeDaAnimacao, 0.06f, Time.deltaTime);
        }
    }

    public void Colocar(Vector3 posicao)
    {
        StopAllCoroutines();
        Ocupado = false;
        velocidadeDaAnimacao = 0f;
        transform.position = posicao;
        transform.rotation = Quaternion.identity;
        visual.localRotation = Quaternion.identity;
        Tocar("Locomocao");
    }

    public void MoverPara(Vector3 destino, float duracao, Action aoChegar)
    {
        StartCoroutine(Mover(destino, duracao, aoChegar));
    }

    private IEnumerator Mover(Vector3 destino, float duracao, Action aoChegar)
    {
        Ocupado = true;
        Vector3 origem = transform.position;
        Vector3 direcao = destino - origem;
        direcao.y = 0f;
        if (direcao.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direcao);
        }

        velocidadeDaAnimacao = 0.5f;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(origem, destino, t / duracao);
            yield return null;
        }
        transform.position = destino;
        velocidadeDaAnimacao = 0f;
        Ocupado = false;
        if (aoChegar != null)
        {
            aoChegar();
        }
    }

    // Vira o corpo pra um lado e pro outro: é o gesto de "olhar para os dois lados".
    public void Olhar(float duracao)
    {
        StartCoroutine(OlharParaOsLados(duracao));
    }

    private IEnumerator OlharParaOsLados(float duracao)
    {
        transform.rotation = Quaternion.identity;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float angulo = -70f * Mathf.Sin(t / duracao * Mathf.PI * 2f);
            visual.localRotation = Quaternion.Euler(0f, angulo, 0f);
            yield return null;
        }
        visual.localRotation = Quaternion.identity;
    }

    // Tropeço cômico: cambalhota e um giro, depois "aoLevantar".
    public void Tropecar(float duracao, Action aoLevantar)
    {
        StopAllCoroutines();
        StartCoroutine(Tropeco(duracao, aoLevantar));
    }

    private IEnumerator Tropeco(float duracao, Action aoLevantar)
    {
        Ocupado = true;
        velocidadeDaAnimacao = 0f;
        TocarPrimeiro(estadosDeTropeco);
        Vector3 origem = transform.position;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = t / duracao;
            visual.localRotation = Quaternion.Euler(0f, 720f * k, 0f);
            // Um pulinho pra trás, pra sair da frente do veículo.
            transform.position = origem + new Vector3(0f, Mathf.Sin(k * Mathf.PI) * 0.6f, -k * 0.8f);
            yield return null;
        }
        visual.localRotation = Quaternion.identity;
        Ocupado = false;
        if (aoLevantar != null)
        {
            aoLevantar();
        }
    }

    public void Comemorar()
    {
        StopAllCoroutines();
        Ocupado = true;
        velocidadeDaAnimacao = 0f;
        visual.localRotation = Quaternion.Euler(0f, 180f, 0f);
        TocarPrimeiro(estadosDeVitoria);
    }

    private void TocarPrimeiro(string[] estados)
    {
        foreach (string estado in estados)
        {
            if (Tocar(estado))
            {
                return;
            }
        }
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
