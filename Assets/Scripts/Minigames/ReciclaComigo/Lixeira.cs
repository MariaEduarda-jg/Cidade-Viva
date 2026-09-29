using UnityEngine;
using UnityEngine.EventSystems;

// Lixeira que aceita um tipo de resíduo (PAPEL, PLASTICO, VIDRO, ORGANICO).
public class Lixeira : MonoBehaviour, IDropHandler
{
    public string tipoAceito;
    public ReciclaManager manager;

    public void OnDrop(PointerEventData eventData)
    {
        ItemLixo item = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<ItemLixo>() : null;
        if (item == null || item.descartado)
        {
            return;
        }

        if (item.tipo != tipoAceito)
        {
            // Sem punição: o item volta pro topo e só perde o bônus de primeira tentativa.
            item.errou = true;
            return;
        }

        item.Descartar((RectTransform)transform);
        manager.ItemDescartado(item);
    }
}
