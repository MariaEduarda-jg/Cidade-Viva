using UnityEngine;

// Um lugar do chassi onde cabe uma peça (roda da frente, motor, pedal...).
public class EncaixeDaOficina : MonoBehaviour
{
    public TipoDePeca tipo;
    // Marca clara que aparece quando a dica está ligada, mostrando onde falta peça.
    public GameObject marcador;

    public PecaDaOficina Ocupante { get; set; }

    public bool Livre
    {
        get { return Ocupante == null; }
    }
}
