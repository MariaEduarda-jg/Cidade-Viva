using UnityEngine;

// Um lugar do percurso (parque, rodoviária, escola, hospital...). Fica à direita da rua.
// Espaço do lugar: origem no meio da faixa do carro, +Z = sentido da rua, +X = lado direito.
public class LugarDoPasseio : MonoBehaviour
{
    public string id;
    public string nome;
    [Tooltip("O que o motorista diz ao chegar perto (começo do pedido do safari).")]
    public string fala;
    [Tooltip("Quanto de rua o lugar ocupa (m).")]
    public float comprimento = 45f;
    [Tooltip("Onde o carro para (z), se este lugar for parada ou destino.")]
    public float parada = 22f;
    [Tooltip("Onde fica quem espera pra entrar ou pra onde vai quem desce.")]
    public Transform chegada;
    public ObjetoDoSafari[] objetos = new ObjetoDoSafari[0];
    [Tooltip("Semáforo do cruzamento, virado pro carro (nulo se não houver).")]
    public SinalDeTransito sinal;
    [Tooltip("Linha de parada do semáforo (z).")]
    public float linha;

    // O objeto que responde ao verbete (o de mesmo id primeiro).
    public ObjetoDoSafari Objeto(string idDoVerbete)
    {
        foreach (ObjetoDoSafari o in objetos)
        {
            if (o != null && o.idDoVerbete == idDoVerbete) return o;
        }
        foreach (ObjetoDoSafari o in objetos)
        {
            if (o != null && o.Aceita(idDoVerbete)) return o;
        }
        return null;
    }
}
