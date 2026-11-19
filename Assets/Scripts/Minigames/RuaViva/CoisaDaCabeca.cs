using UnityEngine;

// Uma coisa que a criança pode pôr na cabeça antes de pedalar (capacete, boné, chapéu de festa).
public class CoisaDaCabeca : MonoBehaviour
{
    public string id;
    public string nome;
    [Tooltip("Altura (m) da coisa: na cabeça, ela desce esta fração pra encaixar.")]
    public float altura = 0.18f;
}
