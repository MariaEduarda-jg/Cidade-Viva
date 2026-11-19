using System.Collections.Generic;
using UnityEngine;

// Itens da loja do Hub (gerado pelo LojaConstrutor em Assets/Resources/CatalogoDaLoja.asset).
[CreateAssetMenu(fileName = "CatalogoDaLoja", menuName = "Cidade Viva/Loja: catálogo")]
public class CatalogoDaLoja : ScriptableObject
{
    [Tooltip("Moedas por estrela nova (só quando a estrela do nível melhora).")]
    public int moedasPorEstrela = 10;
    public List<ItemDaLoja> itens = new List<ItemDaLoja>();

    public ItemDaLoja Buscar(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return itens.Find(i => i.id == id);
    }
}
