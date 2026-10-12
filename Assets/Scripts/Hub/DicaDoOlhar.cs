using UnityEngine;
using UnityEngine.UI;

// Linha de ajuda no pé da tela do Hub: ensina a prender e a soltar o mouse (ver CameraFollow).
public class DicaDoOlhar : MonoBehaviour
{
    public Text texto;
    public string comMouseSolto = "Clique no cenário para olhar em volta com o mouse";
    public string comMousePreso = "Mexa o mouse para olhar  •  Esc solta o mouse";

    private bool preso;

    private void Start()
    {
        texto.text = comMouseSolto;
    }

    private void Update()
    {
        if (CameraFollow.MousePreso != preso)
        {
            preso = CameraFollow.MousePreso;
            texto.text = preso ? comMousePreso : comMouseSolto;
        }
    }
}
