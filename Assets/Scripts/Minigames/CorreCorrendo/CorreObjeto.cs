using UnityEngine;

// Qualquer coisa que vem pela pista do "Corre, Correndo!": obstáculo, moeda, escudo ou a chegada.
// Quem cria e move é a CorrePista; aqui ficam só os dados.
public class CorreObjeto : MonoBehaviour
{
    public enum Categoria { Obstaculo, Moeda, Escudo, Chegada }

    [HideInInspector] public Categoria categoria;
    // Só pra obstáculos.
    [HideInInspector] public TipoDeObstaculo tipo;
    // Posição lateral (x) e distância até o jogador (z; negativo = já passou).
    [HideInInspector] public float x;
    [HideInInspector] public float z;
    // Moedas no alto só são pegas pulando.
    [HideInInspector] public bool noAlto;
    // Já bateu, já foi coletado ou já avisou: não conta de novo.
    [HideInInspector] public bool resolvido;
    [HideInInspector] public bool avisou;

    // Carro em movimento: vem mais rápido e troca de faixa no caminho.
    [HideInInspector] public float velocidadePropria;
    [HideInInspector] public float xDeDestino;
    [HideInInspector] public float trocaComecaEmZ;
    [HideInInspector] public bool trocando;
}
