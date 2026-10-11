using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Lixeira que aceita um tipo de resíduo (PAPEL, PLASTICO, VIDRO, ORGANICO).
// Cores e rótulo vêm da ReciclaConfig (aplicados pelo ReciclaManager).
public class Lixeira : MonoBehaviour, IDropHandler
{
    public string tipoAceito;
    public ReciclaManager manager;
    public Graphic tampa;
    public Text rotulo;

    public void Aplicar(LixeiraConfig aparencia)
    {
        GetComponent<Graphic>().color = aparencia.cor;
        if (tampa != null)
        {
            tampa.color = aparencia.corDaTampa;
        }

        if (rotulo != null)
        {
            rotulo.text = aparencia.rotulo;
        }
    }

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
