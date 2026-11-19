using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Gera o texto da tela "Créditos" do menu inicial a partir de Assets/Art/CREDITOS.md:
    // pacote, autor e licença de cada asset usado (tabelas e listas das seções), agrupados por licença.
    // Pula o que o CREDITOS.md diz que foi baixado e não usado, e as notas de "o que foi alterado".
    // Quem chama é o MenuConstrutor (ao montar a tela); o menu abaixo só grava a prévia em Temp/creditos.txt.
    public static class CreditosConstrutor
    {
        const string CaminhoDoArquivo = "Assets/Art/CREDITOS.md";
        public const string Desenvolvimento = "Maria Eduarda de Jezus Guimarães";

        class Item
        {
            public string pacote, autor, licenca;
        }

        [MenuItem("Cidade Viva/Menu/Prévia do texto dos créditos")]
        public static void Previa()
        {
            File.WriteAllText("Temp/creditos.txt", GerarTexto(false), Encoding.UTF8);
            Debug.Log("[Créditos] Prévia em Temp/creditos.txt.");
        }

        // comCores = true devolve o texto com rich text (tamanhos e cores) pra tela.
        public static string GerarTexto(bool comCores = true)
        {
            var itens = Ler();
            var sb = new StringBuilder();
            string Titulo(string t) => comCores ? $"<size=46><color=#1F5A66>{t}</color></size>" : "== " + t + " ==";
            string Fraco(string t) => comCores ? $"<color=#5A6570>{t}</color>" : t;

            sb.AppendLine(Titulo("Desenvolvimento"));
            sb.AppendLine(Desenvolvimento);
            sb.AppendLine();
            sb.AppendLine(Titulo("Projeto"));
            sb.AppendLine("Projeto Glotec, Universidade de Brasília");
            sb.AppendLine();
            sb.AppendLine(Titulo("Definições e ilustrações"));
            sb.AppendLine("Definições e ilustrações: glossário Glotrânsito (glotransito.com)");
            sb.AppendLine();
            sb.AppendLine(Titulo("Arte, sons e fontes"));
            foreach (var grupo in itens.GroupBy(i => i.licenca))
            {
                sb.AppendLine();
                sb.AppendLine(comCores ? $"<size=38><color=#C8781E>{grupo.Key}</color></size>" : "-- " + grupo.Key + " --");
                foreach (var i in grupo)
                    sb.AppendLine(string.IsNullOrEmpty(i.autor) ? i.pacote : i.pacote + Fraco(" — " + i.autor));
            }
            sb.AppendLine();
            sb.AppendLine(Titulo("Feito pela equipe"));
            sb.AppendLine("Placas, semáforos, peças da oficina, estruturas das obras, bancos e cabine do carro, capacete, ícones e os sons sintetizados: feitos pela equipe com formas simples.");
            return sb.ToString().TrimEnd();
        }

        static List<Item> Ler()
        {
            var itens = new List<Item>();
            string secao = "", sub = "";
            string[] colunas = null;
            bool pular = false;
            foreach (string bruta in File.ReadAllLines(CaminhoDoArquivo, Encoding.UTF8))
            {
                string linha = bruta.Trim();
                if (linha.StartsWith("## "))
                {
                    secao = linha.Substring(3); sub = ""; colunas = null;
                    pular = Contem(secao, "não usado", "O que foi alterado", "Feito pela equipe");
                    continue;
                }
                if (linha.StartsWith("### "))
                {
                    sub = linha.Substring(4); colunas = null;
                    pular = Contem(secao, "não usado", "O que foi alterado", "Feito pela equipe") || Contem(sub, "não usado", "O que foi alterado");
                    continue;
                }
                if (pular || linha.Length == 0) { if (linha.Length == 0) colunas = null; continue; }

                if (linha.StartsWith("|"))
                {
                    var celulas = linha.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
                    if (colunas == null) { colunas = celulas; continue; }
                    if (celulas.All(c => c.StartsWith("-"))) continue;
                    string Col(params string[] nomes)
                    {
                        for (int k = 0; k < colunas.Length && k < celulas.Length; k++)
                            if (nomes.Any(n => colunas[k].StartsWith(n))) return celulas[k];
                        return "";
                    }
                    itens.Add(new Item
                    {
                        pacote = Limpar(Col("Pacote", "Asset")),
                        autor = Autor(Col("Autor")),
                        licenca = Licenca(Col("Licença"), Col("Origem"), sub, secao),
                    });
                    continue;
                }

                if (linha.StartsWith("- "))
                {
                    string t = linha.Substring(2);
                    var item = new Item { licenca = Licenca("", "", sub, secao) };
                    var cc = Regex.Match(t, "This work is based on \"(.+?)\" \\((<?)(https?://[^>)]+)>?\\) by (.+?) \\(");
                    if (cc.Success)
                    {
                        item.pacote = $"\"{cc.Groups[1].Value}\" ({cc.Groups[3].Value})";
                        item.autor = cc.Groups[4].Value;
                        item.licenca = "CC BY 4.0 (crédito obrigatório)";
                    }
                    else
                    {
                        // Só o nome em negrito do começo (ou "**A** e **B**"); o resto em negrito é explicação.
                        var inicio = Regex.Match(t, "^\\*\\*(.+?)\\*\\*(?: e \\*\\*(.+?)\\*\\*)?");
                        if (!inicio.Success) continue;
                        item.pacote = inicio.Groups[2].Success ? inicio.Groups[1].Value + " e " + inicio.Groups[2].Value : inicio.Groups[1].Value;
                        var de = Regex.Match(t, ", de ([^(]+?) \\(");
                        var copy = Regex.Match(t, "Copyright (?:\\(c\\) )?(?:\\d{4} )?(.+?)(?: \\(|, with|\\.)");
                        var travessao = Regex.Match(t, "\\*\\* — ([^—(.]+?)(?: \\(|\\.|,|$)");
                        if (de.Success) item.autor = de.Groups[1].Value.Trim();
                        else if (copy.Success) item.autor = copy.Groups[1].Value.Trim();
                        else if (travessao.Success && !Contem(secao + sub, "não identificada")) item.autor = travessao.Groups[1].Value.Trim();
                        if (Contem(t, "CC-BY 3.0")) item.licenca = "CC BY 3.0 (crédito obrigatório)";
                    }
                    itens.Add(item);
                }
            }
            return itens;
        }

        static bool Contem(string s, params string[] partes) => partes.Any(p => s.IndexOf(p, System.StringComparison.OrdinalIgnoreCase) >= 0);

        // Tira links (<...>), crases e negrito; "Kenney — <https://www.kenney.nl>" vira "Kenney".
        static string Limpar(string s)
        {
            s = Regex.Replace(s, "<[^>]*>", "");
            s = s.Replace("`", "").Replace("**", "");
            s = Regex.Replace(s, "\\s+—\\s*$", "");
            return s.Trim().TrimEnd('—', ' ');
        }

        // Só o nome do autor, sem as observações entre parênteses.
        static string Autor(string s)
        {
            s = Limpar(s);
            int p = s.IndexOf(" (");
            if (p > 0) s = s.Substring(0, p);
            return s.Trim();
        }

        static string Licenca(string coluna, string origem, string sub, string secao)
        {
            if (!string.IsNullOrEmpty(coluna)) return Normalizar(coluna);
            if (!string.IsNullOrEmpty(origem) && Contem(origem, "confirmar")) return "Origem e licença a confirmar";
            string t = sub.Length > 0 ? sub : secao;
            if (Contem(t, "não identificada")) return "Licença a confirmar";
            if (Contem(t, "Open Font")) return "SIL Open Font License 1.1";
            return Normalizar(t);
        }

        static string Normalizar(string s)
        {
            if (Contem(s, "CC0")) return "CC0 (domínio público)";
            if (Contem(s, "CC-BY 4.0", "CC BY 4.0")) return "CC BY 4.0 (crédito obrigatório)";
            if (Contem(s, "CC-BY")) return "CC BY (crédito obrigatório)";
            return Limpar(s);
        }
    }
}
