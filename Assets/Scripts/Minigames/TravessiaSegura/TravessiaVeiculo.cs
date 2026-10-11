using UnityEngine;

// Um veículo na rua. Quem move é o TravessiaTrafego; aqui ficam só os dados.
public class TravessiaVeiculo : MonoBehaviour
{
    // Posição ao longo da pista, no sentido do movimento: negativa antes da faixa, positiva depois.
    [HideInInspector] public float s;
    [HideInInspector] public float velocidade;
    [HideInInspector] public float velocidadeAlvo;
    [HideInInspector] public float meioComprimento = 1.8f;
    [HideInInspector] public bool ambulancia;
    [HideInInspector] public bool ciclista;
    [HideInInspector] public float proximaMudanca;
    [HideInInspector] public Light sirene;

    public float Frente { get { return s + meioComprimento; } }
    public float Traseira { get { return s - meioComprimento; } }
}
