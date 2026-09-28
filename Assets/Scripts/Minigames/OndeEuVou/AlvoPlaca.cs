using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Lugar na rua onde uma placa específica deve ser colocada.
[RequireComponent(typeof(Image))]
public class AlvoPlaca : MonoBehaviour, IDropHandler
{
    public string tipoAceito;
    public OndeEuVouManager manager;
    public Color corAcerto = new Color(0.3f, 0.9f, 0.3f, 1f);
    public float duracaoPiscada = 0.6f;

    private Image imagem;
    private Color corOriginal;
    private bool ocupado;

    private void Awake()
    {
        imagem = GetComponent<Image>();
        corOriginal = imagem.color;
    }

    public void OnDrop(PointerEventData eventData)
    {
        PlacaArrastavel placa = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<PlacaArrastavel>() : null;
        if (placa == null || placa.encaixada || ocupado)
        {
            return;
        }

        if (placa.tipo != tipoAceito)
        {
            manager.RegistrarErro();
            return;
        }

        ocupado = true;
        placa.Encaixar((RectTransform)transform);
        StartCoroutine(PiscarVerde());
        manager.RegistrarAcerto();
    }

    private IEnumerator PiscarVerde()
    {
        imagem.color = corAcerto;
        yield return new WaitForSeconds(duracaoPiscada);
        imagem.color = corOriginal;
    }
}
