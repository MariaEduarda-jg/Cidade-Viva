using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Gera Docs/Revisao_Coordenacao.md: tudo que a coordenação precisa revisar, num arquivo só, por mecânica,
    // separando o que vem da definição (com o trecho literal) do que NÃO vem (escolhas do jogo). Depois, os
    // textos da história, da loja e os nomes dos botões. Lê os mesmos dados que o jogo usa (receitas, problemas,
    // configs e as listas das conferências), então é só rodar o menu de novo depois de qualquer mudança.
    public static class RevisaoConstrutor
    {
        const string Caminho = "Docs/Revisao_Coordenacao.md";

        [MenuItem("Cidade Viva/Verbetes/Gerar o documento de revisão da coordenação")]
        public static void Gerar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string cenaAberta = EditorSceneManager.GetActiveScene().path;
            var banco = VerbeteDatabase.Instancia;
            var md = new StringBuilder();
            md.AppendLine("# Cidade Viva: revisão da coordenação");
            md.AppendLine();
            md.AppendLine($"Gerado em {System.DateTime.Now:dd/MM/yyyy}. Este documento junta, por jogo, as regras que o Cidade Viva usa para ensinar os 38 verbetes do Glotrânsito.");
            md.AppendLine();
            md.AppendLine("Em cada jogo há duas listas:");
            md.AppendLine("- **Vem da definição**: a regra e o trecho da definição, copiado letra por letra, que a justifica.");
            md.AppendLine("- **Não vem da definição**: escolhas da equipe para o jogo funcionar (lugares, histórias, quantidades, tempos, aparência). É isto que mais precisa de revisão.");
            md.AppendLine();
            md.AppendLine("No fim estão os textos da história (abertura e final), os da loja e os nomes dos botões. Nenhum desses textos vem do glossário, e nenhum cita definição.");
            md.AppendLine();

            Oficina(md, banco);
            Obras(md, banco);
            Sinais(md, banco);
            Carro(md, banco);
            Pedalada(md, banco);
            Historia(md);
            Loja(md);
            Botoes(md);

            Directory.CreateDirectory("Docs");
            File.WriteAllText(Caminho, md.ToString(), new UTF8Encoding(false));
            Debug.Log("[Revisão] Documento gerado em " + Caminho);
            if (!string.IsNullOrEmpty(cenaAberta)) EditorSceneManager.OpenScene(cenaAberta);
        }

        static string Palavra(VerbeteDatabase banco, string id)
        {
            var v = banco.Buscar(id);
            return v != null ? v.palavra : id;
        }

        static void Definicao(StringBuilder md, VerbeteDatabase banco, string id)
        {
            var v = banco.Buscar(id);
            if (v != null) md.AppendLine($"> **{Maiuscula(v.palavra)}**: {v.definicao.Trim()}");
        }

        static string Maiuscula(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        static void Lista(StringBuilder md, IEnumerable<string> itens)
        {
            foreach (string i in itens) md.AppendLine("- " + i);
            md.AppendLine();
        }

        // Regras de uma conferência (verbete, trecho, regra), agrupadas por verbete, com a definição completa.
        static void RegrasPorVerbete(StringBuilder md, VerbeteDatabase banco, List<string[]> regras)
        {
            foreach (var grupo in regras.GroupBy(r => r[0]))
            {
                md.AppendLine($"**{Maiuscula(Palavra(banco, grupo.Key))}**");
                md.AppendLine();
                Definicao(md, banco, grupo.Key);
                md.AppendLine();
                foreach (var r in grupo) md.AppendLine($"- {Maiuscula(r[2])}. Trecho: \"{r[1]}\".");
                md.AppendLine();
            }
        }

        // ------------------------------------------------------------------ 1. Oficina

        static void Oficina(StringBuilder md, VerbeteDatabase banco)
        {
            var config = AssetDatabase.LoadAssetAtPath<OficinaConfig>("Assets/Configs/Oficina_Config.asset");
            string Peca(TipoDePeca t) => config != null ? config.NomeDaPeca(t) : t.ToString();
            md.AppendLine("## 1. Oficina de Veículos (14 veículos)");
            md.AppendLine();
            md.AppendLine("A criança lê a definição e monta o veículo com as peças que a definição pede. Peça que a definição não pede volta para a prateleira com um balão.");
            md.AppendLine();
            md.AppendLine("**Não vem da definição (vale para todos os veículos):**");
            md.AppendLine();
            Lista(md, new[]
            {
                "As peças \"extras aceitas\" (assento, luzes, carroceria, sirene): o veículo tem na vida real, mas a definição não cita. Elas encaixam sem contar erro e não são exigidas. A lista de cada veículo está abaixo.",
                "As peças erradas oferecidas na prateleira (asa, hélice, âncora, vela de barco, remo, trilho, sirene, pedal, motor): nenhuma definição pede; servem só para a criança escolher.",
                "Onde a definição diz só \"motor\", sem dizer o tipo, o jogo aceita o motor comum e o motor elétrico.",
                "Em que nível cada veículo aparece, o tempo de cada nível e o teste na pista (dirigir e pegar 3 moedas).",
                "Os modelos usados quando não havia um próprio: o bonde usa um metrô e o micro-ônibus usa uma van.",
            });
            var receitas = Resources.LoadAll<VeiculoReceita>(VeiculoReceita.PastaNoResources).OrderBy(r => r.nivel).ThenBy(r => r.ordem).ToList();
            foreach (var r in receitas)
            {
                md.AppendLine($"### {Maiuscula(Palavra(banco, r.idDoVerbete))} (nível {r.nivel})");
                md.AppendLine();
                Definicao(md, banco, r.idDoVerbete);
                md.AppendLine();
                var vem = new List<string>();
                var naoVem = new List<string>();
                foreach (var item in r.itens)
                {
                    string qtd = item.quantidade + (item.ouMais ? " ou mais" : "");
                    string nome = $"{Maiuscula(Peca(item.peca))} ({qtd})";
                    if (item.basePadrao)
                    {
                        naoVem.Add($"{nome}: o veículo precisa, mas a definição não cita.");
                        continue;
                    }
                    string extra = "";
                    if (!string.IsNullOrEmpty(item.verbeteDoTrecho)) extra += $" Esse trecho é da definição de {Palavra(banco, item.verbeteDoTrecho)}, que a definição cita.";
                    if (item.tambemAceita.Count > 0) extra += " Também aceita: " + string.Join(", ", item.tambemAceita.Select(Peca)) + ".";
                    if (item.opcional) extra += " É opcional (a definição diz que pode ter ou não).";
                    vem.Add($"{nome}. Trecho: \"{item.trechoDaDefinicao}\".{extra}");
                }
                foreach (var item in r.proibidas)
                    vem.Add($"Não pode ter {Peca(item.peca)}. Trecho: \"{item.trechoDaDefinicao}\".");
                if (r.extrasAceitos.Count > 0)
                    naoVem.Add("Encaixa sem erro (extra aceito): " + string.Join(", ", r.extrasAceitos.Select(Peca)) + ".");
                if (r.andaEmTrilhos) naoVem.Add("No teste da pista, anda sobre os trilhos (sem volante).");
                if (!string.IsNullOrEmpty(r.puxadoPor)) naoVem.Add($"No teste da pista, é puxado pelo {Palavra(banco, r.puxadoPor)}: a criança aperta \"Engatar\".");
                if (r.velocidadeNaPista < 1f) naoVem.Add("No teste da pista, anda mais devagar.");
                if (!string.IsNullOrEmpty(r.observacao)) naoVem.Add("Observação: " + r.observacao.Trim());
                md.AppendLine("**Vem da definição:**");
                md.AppendLine();
                Lista(md, vem.Count > 0 ? vem : new List<string> { "Nenhuma peça (todas as peças deste veículo são da lista de baixo)." });
                if (naoVem.Count > 0)
                {
                    md.AppendLine("**Não vem da definição:**");
                    md.AppendLine();
                    Lista(md, naoVem);
                }
            }
        }

        // ------------------------------------------------------------------ 2. Obras

        static readonly Dictionary<ObstaculoDaObra, string> Situacoes = new Dictionary<ObstaculoDaObra, string>
        {
            [ObstaculoDaObra.Rio] = "um rio", [ObstaculoDaObra.Morro] = "um morro", [ObstaculoDaObra.Via] = "uma via",
            [ObstaculoDaObra.CampoECidade] = "ligar uma fazenda à cidade", [ObstaculoDaObra.DentroDaCidade] = "um caminho dentro da cidade",
            [ObstaculoDaObra.LugarQualquer] = "ligar dois lugares que não são cidade nem fazenda", [ObstaculoDaObra.BeiraDaRua] = "a beira de uma rua com carros",
            [ObstaculoDaObra.SeparadoDoTransito] = "um caminho longe do trânsito dos carros", [ObstaculoDaObra.MudarDeSentido] = "mudar de sentido numa avenida",
            [ObstaculoDaObra.GuardarVeiculos] = "guardar veículos",
        };

        static readonly Dictionary<QuemPassaNaObra, string> Quem = new Dictionary<QuemPassaNaObra, string>
        {
            [QuemPassaNaObra.Veiculo] = "veículos", [QuemPassaNaObra.Pedestre] = "pedestres", [QuemPassaNaObra.Animal] = "animais", [QuemPassaNaObra.Ciclista] = "ciclistas",
        };

        static void Obras(StringBuilder md, VerbeteDatabase banco)
        {
            var config = AssetDatabase.LoadAssetAtPath<ObrasConfig>("Assets/Configs/Obras_Config.asset");
            md.AppendLine("## 2. Obras da Cidade (14 vias e estruturas)");
            md.AppendLine();
            md.AppendLine("A criança vê um caminho cortado e quem precisa passar, escolhe uma estrutura e a constrói. Quem decide se a estrutura serve são as regras abaixo: o que ela atravessa e quem passa por ela.");
            md.AppendLine();
            md.AppendLine("### Regras das estruturas");
            md.AppendLine();
            foreach (var r in config.regras)
            {
                md.AppendLine($"**{Maiuscula(Palavra(banco, r.idDoVerbete))}**");
                md.AppendLine();
                Definicao(md, banco, r.idDoVerbete);
                md.AppendLine();
                md.AppendLine($"- Serve para: {string.Join(", ", r.obstaculos.Select(o => Situacoes.TryGetValue(o, out var t) ? t : o.ToString()))}. Trecho: \"{r.trechoDoObstaculo}\".");
                string deOutro = string.IsNullOrEmpty(r.verbeteDoTrechoDeQuem) ? "" : $" (este trecho é da definição de {Palavra(banco, r.verbeteDoTrechoDeQuem)}, não da própria: ver a lista de baixo)";
                md.AppendLine($"- Quem passa: {string.Join(", ", r.quemPassa.Select(q => Quem[q]))}. Trecho: \"{r.trechoDeQuemPassa}\"{deOutro}.");
                md.AppendLine();
            }
            md.AppendLine("**Não vem da definição (vale para todos os problemas):**");
            md.AppendLine();
            Lista(md, new[]
            {
                "Em cada problema, só uma das opções pode servir. Por isso o túnel nunca aparece junto com viaduto, passarela, passagem subterrânea ou faixa quando o caminho é uma via (ele também serviria), e a via geral nunca aparece junto com a via rural ou a urbana onde elas servem.",
                "Na via rural e na via urbana, quem passa vem da definição de via (as duas definições começam com \"Via\").",
                "O ciclista conta como veículo na via, porque a definição de bicicleta começa com \"Veículo\".",
                "\"Parte de uma rua\" (calçada) e \"ao lado da via\" (ciclofaixa) viraram a beira de uma rua com carros; a ciclovia fica num parque, com os carros ao longe.",
                "Os cenários (rio, morro, avenida, fazenda, cidade, lago e mata, avenida com canteiro para o retorno, loja com terreno para o estacionamento) e quem passa em cada um.",
                "Rua comum ou avenida é só aparência: as definições não distinguem as duas.",
                "\"Temporariamente\" (estacionamento) não aparece no jogo.",
                "A aparência das peças (terra e cerca na via rural, meio-fio e poste na via urbana, a ponte com o desenho da Ponte JK).",
                "No nível 5 das pontes, a mesma ambulância atravessa as três obras até o hospital, com tempo.",
            });
            var problemas = Resources.LoadAll<ObraProblema>("Obras").OrderBy(p => p.modo).ThenBy(p => p.nivel).ThenBy(p => p.ordem).ToList();
            foreach (int modo in new[] { 0, 1 })
            {
                md.AppendLine(modo == 0 ? "### Problemas: Pontes e Passagens" : "### Problemas: Planejar a rua");
                md.AppendLine();
                foreach (var p in problemas.Where(x => x.modo == modo))
                {
                    string situacao = Situacoes.TryGetValue(p.obstaculo, out var t) ? t : p.obstaculo.ToString();
                    if (p.obstaculo == ObstaculoDaObra.Via) situacao = p.ruaComum ? "uma rua" : "uma avenida com carros";
                    string frase = string.IsNullOrEmpty(p.enunciado) ? $"{p.nomeDeQuemPassa} precisa passar: {situacao}." : $"\"{p.enunciado}\" ({situacao}; quem passa: {p.nomeDeQuemPassa.ToLower()}).";
                    md.AppendLine($"- **Nível {p.nivel}, problema {p.ordem + 1}**: {frase} Opções: {string.Join(", ", p.opcoes.Select(o => Palavra(banco, o)))}. Certa: **{Palavra(banco, p.correta)}**, trecho \"{p.trechoDaDefinicao}\".");
                    if (p.modo != 0 && !string.IsNullOrEmpty(p.observacao) && !p.observacao.StartsWith("Gerado"))
                        md.AppendLine($"  - Não vem da definição: {p.observacao.Trim()}");
                }
                md.AppendLine();
            }
        }

        // ------------------------------------------------------------------ 3, 4, 5

        static void Sinais(StringBuilder md, VerbeteDatabase banco)
        {
            SinaisConstrutor.ConferirTrechos();
            md.AppendLine("## 3. Central de Sinais (5 sinais)");
            md.AppendLine();
            md.AppendLine("Dois modos: \"Motorista Mirim\" (a criança dirige e obedece os semáforos) e \"Sinaleiro\" (a criança controla os semáforos de um cruzamento).");
            md.AppendLine();
            md.AppendLine("### Vem da definição");
            md.AppendLine();
            RegrasPorVerbete(md, banco, SinaisConstrutor.RegrasDaRevisao);
            md.AppendLine("### Não vem da definição");
            md.AppendLine();
            Lista(md, SinaisConstrutor.NaoVemDasDefinicoes);
        }

        static readonly Dictionary<string, string> Pessoas = new Dictionary<string, string>
        {
            ["pai"] = "o pai", ["mae"] = "a mãe", ["voce"] = "você (a criança)", ["filha"] = "a filha", ["cacula"] = "o caçula", ["vovo"] = "a vovó",
            ["ursinho"] = "um ursinho", ["caixa"] = "uma caixa",
        };

        static string P(string id) => Pessoas.TryGetValue(id, out var n) ? n : id;

        static void Carro(StringBuilder md, VerbeteDatabase banco)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + RuaVivaCarroConstrutor.NomeDaCena + ".unity");
            var lugares = Object.FindObjectsByType<LugarDoPasseio>(FindObjectsInactive.Include);
            var nomes = lugares.GroupBy(l => l.id).ToDictionary(g => g.Key, g => g.First().nome);
            RuaVivaCarroConstrutor.ConferirTrechos(lugares);
            var config = AssetDatabase.LoadAssetAtPath<RuaVivaConfig>("Assets/Configs/RuaVivaCarro_Config.asset");
            md.AppendLine("## 4. Rua Viva, parte 1: Passeio de Carro (passageiro, cinto de segurança, buzina)");
            md.AppendLine();
            md.AppendLine("A criança vai no banco de trás do carro da família. Antes de sair, prende o cinto de cada um; no caminho, pede pro motorista buzinar quando alguém vem pra rua sem ver o carro, e procura coisas da cidade (o \"safari\").");
            md.AppendLine();
            md.AppendLine("### Vem da definição");
            md.AppendLine();
            RegrasPorVerbete(md, banco, RuaVivaCarroConstrutor.RegrasDaRevisao);
            md.AppendLine("### Não vem da definição");
            md.AppendLine();
            Lista(md, RuaVivaCarroConstrutor.NaoVemDasDefinicoes);
            md.AppendLine("### As 5 missões (textos do jogo, não vêm do glossário)");
            md.AppendLine();
            for (int i = 0; i < config.missoes.Length; i++)
            {
                var m = config.missoes[i];
                md.AppendLine($"**{m.titulo}** (nível {i + 1})");
                md.AppendLine();
                md.AppendLine($"- Texto: \"{m.texto}\"");
                md.AppendLine($"- Novidade: \"{m.novidade}\"");
                md.AppendLine($"- No carro: {string.Join(", ", m.ocupantes.Select(o => P(o.quem)))}" + (m.soltos.Length > 0 ? $"; soltos nos bancos: {string.Join(", ", m.soltos.Select(o => P(o.quem)))}" + (m.guardarSoltos ? " (a criança guarda no porta-malas antes de sair)" : "") : "") + ".");
                var caminho = new List<string>();
                foreach (var e in m.eventos)
                {
                    string lugar = nomes.TryGetValue(e.lugar ?? "", out var n) ? n : e.lugar;
                    string pedido = string.IsNullOrEmpty(e.pedido) ? "" : $" (pedido do safari: {Palavra(banco, e.pedido)})";
                    switch (e.tipo)
                    {
                        case TipoDeEvento.Lugar: caminho.Add($"passa por {lugar}{pedido}"); break;
                        case TipoDeEvento.Parada: caminho.Add($"para em {lugar}: entra {P(e.entra)}{pedido}"); break;
                        case TipoDeEvento.Destino:
                            string d = $"chega em {lugar}{pedido}";
                            if (!string.IsNullOrEmpty(e.entra)) d += $"; entra {P(e.entra)}";
                            if (e.descem.Length > 0) d += $"; descem {string.Join(" e ", e.descem.Select(P))}";
                            if (!string.IsNullOrEmpty(e.espera)) d += $"; {P(e.espera)} está esperando";
                            caminho.Add(d); break;
                        case TipoDeEvento.Distraida: caminho.Add("alguém vem pra rua sem ver o carro (hora da buzina)"); break;
                        case TipoDeEvento.Atenta: caminho.Add("alguém anda pela calçada, olhando (não precisa buzinar)"); break;
                        case TipoDeEvento.Bola: caminho.Add("uma bola atravessa a rua (o motorista freia)"); break;
                    }
                }
                md.AppendLine($"- Caminho: {string.Join("; ", caminho)}.");
                md.AppendLine($"- Rádio: {string.Join(" / ", m.radio.Select(r => "\"" + r + "\""))}");
                md.AppendLine($"- Na chegada: \"{m.recadoDeChegada}\"");
                md.AppendLine();
            }
        }

        static void Pedalada(StringBuilder md, VerbeteDatabase banco)
        {
            RuaVivaPedaladaConstrutor.ConferirTrechos();
            var config = AssetDatabase.LoadAssetAtPath<PedaladaConfig>("Assets/Configs/RuaVivaPedalada_Config.asset");
            md.AppendLine("## 5. Rua Viva, parte 2: Pedalada (ciclista, engarrafamento)");
            md.AppendLine();
            md.AppendLine("A criança põe o capacete e pedala; a bicicleta anda sozinha e ela troca de faixa. Onde há ciclovia ou ciclofaixa, pedala nela. No nível 3, os carros se acumulam até a ambulância não passar, e ela toca no engarrafamento.");
            md.AppendLine();
            md.AppendLine("### Vem da definição");
            md.AppendLine();
            RegrasPorVerbete(md, banco, RuaVivaPedaladaConstrutor.RegrasDaRevisao);
            md.AppendLine("### Não vem da definição");
            md.AppendLine();
            Lista(md, RuaVivaPedaladaConstrutor.NaoVemDasDefinicoes);
            md.AppendLine("### Os 3 roteiros (textos do jogo, não vêm do glossário)");
            md.AppendLine();
            for (int i = 0; i < config.roteiros.Length; i++)
            {
                var r = config.roteiros[i];
                md.AppendLine($"**Nível {i + 1}: {r.titulo}**");
                md.AppendLine();
                md.AppendLine($"- Texto: \"{r.texto}\"");
                md.AppendLine($"- Novidade: \"{r.novidade}\"");
                md.AppendLine($"- Na cabeça, pra escolher: {string.Join(", ", r.coisasDaCabeca.Select(c => c == "bone" ? "boné" : c == "chapeu_de_festa" ? "chapéu de festa" : c))}.");
                var avisos = r.trechos.Where(t => !string.IsNullOrEmpty(t.aviso)).Select(t => "\"" + t.aviso + "\"").ToList();
                if (avisos.Count > 0) md.AppendLine($"- Avisos no caminho: {string.Join(" / ", avisos)}");
                md.AppendLine($"- Na chegada: \"{r.recadoDeChegada}\"");
                md.AppendLine();
            }
            md.AppendLine("**Balões e recados da Pedalada** (citam trechos das definições, sempre literais):");
            md.AppendLine();
            Lista(md, new[]
            {
                config.recadoDoCapacete, config.balaoDaCabecaErrada.Replace("\n", " ").Replace("{0}", "Boné").Replace("{1}", config.trechoDoCapacete),
                config.balaoNaPistaDosCarros.Replace("\n", " ").Replace("{0}", "ciclofaixa").Replace("{1}", config.trechoDaCiclovia),
                config.balaoNaCalcada.Replace("\n", " ").Replace("{0}", config.trechoDaCalcada).Replace("{1}", config.naCiclofaixa),
                config.recadoDaBatida, config.recadoDoCanteiro, config.recadoDoEngarrafamento,
                config.balaoDoEngarrafamento.Replace("\n", " ").Replace("{0}", config.trechoDoAcumulo).Replace("{1}", config.trechoDaPassagem),
            }.Select(t => "\"" + t + "\""));
        }

        // ------------------------------------------------------------------ 6, 7, 8

        static void Historia(StringBuilder md)
        {
            md.AppendLine("## 6. Textos da história");
            md.AppendLine();
            md.AppendLine("**Abertura** (na primeira vez que o perfil entra; legendas, na ordem):");
            md.AppendLine();
            Lista(md, HistoriaConstrutor.LegendasDaAbertura.Select(t => "\"" + t + "\""));
            md.AppendLine("**Final** (quando o perfil completa os 38 verbetes):");
            md.AppendLine();
            Lista(md, HistoriaConstrutor.LegendasDoFinal.Select(t => "\"" + t + "\"").Concat(new[]
            {
                $"Título da tela: \"{HistoriaConstrutor.TituloDoFinal}\"",
                $"Linha embaixo do título: \"{HistoriaConstrutor.TotalDoFinal.Replace("{0}", "38").Replace("{1}", "38")}\"",
            }));
        }

        static void Loja(StringBuilder md)
        {
            var go = new GameObject("Revisao_Loja");
            var padrao = go.AddComponent<LojaDoHub>();
            md.AppendLine("## 7. Textos da loja");
            md.AppendLine();
            md.AppendLine("Cada estrela nova conquistada num nível vale 10 moedas. Com as moedas, a criança compra roupas e acessórios para o personagem que anda pela cidade.");
            md.AppendLine();
            md.AppendLine("**Itens (nome e preço em moedas):**");
            md.AppendLine();
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDaLoja>("Assets/Resources/CatalogoDaLoja.asset");
            Lista(md, catalogo.itens.Select(i => $"{i.nome}: {i.preco}"));
            md.AppendLine("**Recados:**");
            md.AppendLine();
            Lista(md, new[]
            {
                padrao.textoAjuda, padrao.textoComprado, padrao.textoFaltam.Replace("{0}", "20"), "vestido (selo no item que está vestido)",
            }.Select(t => "\"" + t + "\""));
            Object.DestroyImmediate(go);
        }

        static void Botoes(StringBuilder md)
        {
            md.AppendLine("## 8. Nomes dos botões");
            md.AppendLine();
            Lista(md, new[]
            {
                "Menu inicial: \"Entrar\", \"Criar meu perfil\", \"Jogar sem conta\", \"Créditos\", \"Ver abertura\"; na criação do perfil: \"Pronto!\", \"Apagar\", \"Quero criar um perfil\", \"Vamos jogar!\".",
                "Cidade (Hub): \"Álbum\", \"Loja\", \"Trocar de perfil\".",
                "Loja: \"Comprar\", \"Vestir\", \"Tirar\".",
                "Abertura e final: \"Pular\", \"Ouvir\", \"Voltar ao Hub\"; no Álbum: \"Voltar\", \"Ver o final\".",
                "Cartão do verbete: \"Ouvir\", \"Continuar\".",
                "Escolha de nível: \"Voltar ao Hub\"; abas dos modos: \"Pontes e Passagens\" e \"Planejar a rua\" (Obras), \"Motorista Mirim\" e \"Sinaleiro\" (Central de Sinais), \"Passeio de Carro\" e \"Pedalada\" (Rua Viva).",
                "Fim de nível: \"Voltar ao Hub\", \"Tentar de novo\", \"Próximo nível\".",
                "Dentro dos jogos: \"Ouvir\", \"Dica\" (Oficina e Obras), \"Engatar\" (Oficina), \"Acelerar\" e \"Frear\" (Motorista Mirim), \"Vamos!\" (Rua Viva).",
            });
        }
    }
}
