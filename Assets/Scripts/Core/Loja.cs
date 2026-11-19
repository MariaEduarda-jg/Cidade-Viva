using System;
using System.Collections.Generic;
using UnityEngine;

public enum TipoDeItemDaLoja
{
    Roupa,      // troca a cor da camiseta
    Acessorio,  // algo na cabeça (boné, óculos, chapéu)
}

[Serializable]
public class ItemDaLoja
{
    public string id;
    public string nome;
    public TipoDeItemDaLoja tipo;
    public int preco = 20;
    [Tooltip("Roupa: o material da camiseta.")]
    public Material corDaRoupa;
    [Tooltip("Acessório: o prefab (formas da paleta).")]
    public GameObject acessorio;
    [Tooltip("Acessório: onde fica, a partir do osso da cabeça, no espaço do personagem (x = direita, y = cima, z = frente).")]
    public Vector3 posicaoNaCabeca;
    public Sprite icone;
}

// Loja mínima (versão final): moedas por estrela nova, compras e item vestido, tudo no perfil ativo.
public static class Loja
{
    public enum Resultado { Comprou, JaTinha, FaltamMoedas, NaoExiste }

    // Avisa o contador do Hub, o painel e o personagem.
    public static event Action Mudou;

    private static CatalogoDaLoja catalogo;

    public static CatalogoDaLoja Catalogo
    {
        get
        {
            if (catalogo == null) catalogo = Resources.Load<CatalogoDaLoja>("CatalogoDaLoja");
            return catalogo;
        }
    }

    public static int Moedas => Perfis.Ativo.moedas;

    // Chamado pelo GameProgress quando o nível ganhou estrelas que não tinha.
    public static void GanharPorEstrelas(int estrelasNovas)
    {
        if (estrelasNovas <= 0) return;
        int porEstrela = Catalogo != null ? Catalogo.moedasPorEstrela : 10;
        Perfis.Ativo.moedas += estrelasNovas * porEstrela;
        Avisar();
    }

    public static bool Tem(string id)
    {
        Perfil p = Perfis.Ativo;
        return p.itensComprados != null && p.itensComprados.Contains(id);
    }

    public static bool Vestido(string id)
    {
        Perfil p = Perfis.Ativo;
        return !string.IsNullOrEmpty(id) && (p.roupaVestida == id || p.acessorioVestido == id);
    }

    public static Resultado Comprar(string id)
    {
        ItemDaLoja item = Catalogo != null ? Catalogo.Buscar(id) : null;
        if (item == null) return Resultado.NaoExiste;
        if (Tem(id)) return Resultado.JaTinha;
        Perfil p = Perfis.Ativo;
        if (p.moedas < item.preco) return Resultado.FaltamMoedas;
        p.moedas -= item.preco;
        if (p.itensComprados == null) p.itensComprados = new List<string>();
        p.itensComprados.Add(id);
        Salvar();
        return Resultado.Comprou;
    }

    // Veste o item comprado; se já estava vestido, tira (volta a roupa de sempre / sem acessório).
    public static bool VestirOuTirar(string id)
    {
        ItemDaLoja item = Catalogo != null ? Catalogo.Buscar(id) : null;
        if (item == null || !Tem(id)) return false;
        Perfil p = Perfis.Ativo;
        if (item.tipo == TipoDeItemDaLoja.Roupa) p.roupaVestida = p.roupaVestida == id ? "" : id;
        else p.acessorioVestido = p.acessorioVestido == id ? "" : id;
        Salvar();
        return true;
    }

    public static ItemDaLoja RoupaVestida => Catalogo != null ? Catalogo.Buscar(Perfis.Ativo.roupaVestida) : null;
    public static ItemDaLoja AcessorioVestido => Catalogo != null ? Catalogo.Buscar(Perfis.Ativo.acessorioVestido) : null;

    private static void Salvar()
    {
        Perfis.SalvarAtivo();
        Avisar();
    }

    private static void Avisar()
    {
        Mudou?.Invoke();
    }
}
