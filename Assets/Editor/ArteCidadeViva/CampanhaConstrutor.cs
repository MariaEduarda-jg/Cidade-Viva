using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static CidadeViva.EditorArte.MenuConstrutor;

namespace CidadeViva.EditorArte
{
    // Campanha "Os 5 dias da reconstrução" (2026-11-22): dados (dias e missões), o que ela põe no Hub
    // (NPCs, consertos, trilha, interface) e o documento das falas pra coordenação.
    // Uma camada POR CIMA das mecânicas: nenhuma mecânica muda.
    // REGRA DAS FALAS: o NPC dá o MOTIVO da missão; nunca explica um verbete nem resume definição
    // (quem ensina o verbete é o cartão). Frases curtas, pra criança de 6 a 9 anos.
    public static class CampanhaConstrutor
    {
        const string PastaDosDados = "Assets/Campanha";
        const string CaminhoDosDados = "Assets/Resources/CampanhaDados.asset";
        const string CaminhoDasFalas = "Docs/Falas_Da_Campanha.md";

        [MenuItem("Cidade Viva/Campanha/Reconstruir a campanha (dados, Hub e falas)")]
        public static void Reconstruir()
        {
            Dados();
            HubConstrutor.Reconstruir(); // chama NoHub
            Falas();
        }

        [MenuItem("Cidade Viva/Campanha/Gerar o documento das falas")]
        public static void Falas()
        {
            var d = AssetDatabase.LoadAssetAtPath<CampanhaDados>(CaminhoDosDados);
            if (d == null) { Debug.LogError("[Campanha] Dados não encontrados: rode a reconstrução."); return; }
            File.WriteAllText(CaminhoDasFalas, DocumentoDasFalas(d), new UTF8Encoding(false));
            Debug.Log("[Campanha] Falas em " + CaminhoDasFalas);
        }

        // ------------------------------------------------------------------ dados

        // As missões dos 5 dias (tabela aprovada em 2026-11-22). Só o Dia 1 tem falas e entra no jogo nesta parte;
        // os dias 2 a 5 ficam só na tabela (documento das falas).
        struct LinhaDaTabela { public int dia; public QuemFala npc; public string modo; public int nivel; }
        static readonly (int dia, string titulo)[] TitulosDosDias =
        {
            (1, "Depois da enchente"), (2, "Os caminhos"), (3, "Os sinais"), (4, "As pessoas voltam às ruas"), (5, "A festa da reconstrução"),
        };
        static readonly LinhaDaTabela[] Tabela =
        {
            new LinhaDaTabela { dia = 1, npc = QuemFala.Mecanico, modo = "Oficina", nivel = 1 },
            new LinhaDaTabela { dia = 1, npc = QuemFala.Engenheira, modo = "Pontes e Passagens", nivel = 1 },
            new LinhaDaTabela { dia = 1, npc = QuemFala.Guarda, modo = "Motorista Mirim", nivel = 1 },
            new LinhaDaTabela { dia = 1, npc = QuemFala.Vizinha, modo = "Passeio de Carro", nivel = 1 },
            new LinhaDaTabela { dia = 2, npc = QuemFala.Engenheira, modo = "Pontes e Passagens", nivel = 2 },
            new LinhaDaTabela { dia = 2, npc = QuemFala.Engenheira, modo = "Planejar a rua", nivel = 1 },
            new LinhaDaTabela { dia = 2, npc = QuemFala.Mecanico, modo = "Oficina", nivel = 2 },
            new LinhaDaTabela { dia = 2, npc = QuemFala.Engenheira, modo = "Pontes e Passagens", nivel = 3 },
            new LinhaDaTabela { dia = 2, npc = QuemFala.Engenheira, modo = "Planejar a rua", nivel = 2 },
            new LinhaDaTabela { dia = 2, npc = QuemFala.Engenheira, modo = "Planejar a rua", nivel = 3 },
            new LinhaDaTabela { dia = 3, npc = QuemFala.Guarda, modo = "Sinaleiro", nivel = 1 },
            new LinhaDaTabela { dia = 3, npc = QuemFala.Guarda, modo = "Motorista Mirim", nivel = 2 },
            new LinhaDaTabela { dia = 3, npc = QuemFala.Mecanico, modo = "Oficina", nivel = 3 },
            new LinhaDaTabela { dia = 3, npc = QuemFala.Guarda, modo = "Sinaleiro", nivel = 2 },
            new LinhaDaTabela { dia = 3, npc = QuemFala.Engenheira, modo = "Pontes e Passagens", nivel = 4 },
            new LinhaDaTabela { dia = 3, npc = QuemFala.Guarda, modo = "Sinaleiro", nivel = 3 },
            new LinhaDaTabela { dia = 3, npc = QuemFala.Mecanico, modo = "Oficina", nivel = 4 },
            new LinhaDaTabela { dia = 4, npc = QuemFala.Vizinha, modo = "Passeio de Carro", nivel = 2 },
            new LinhaDaTabela { dia = 4, npc = QuemFala.Vizinha, modo = "Pedalada", nivel = 1 },
            new LinhaDaTabela { dia = 4, npc = QuemFala.Vizinha, modo = "Passeio de Carro", nivel = 3 },
            new LinhaDaTabela { dia = 4, npc = QuemFala.Engenheira, modo = "Planejar a rua", nivel = 4 },
            new LinhaDaTabela { dia = 4, npc = QuemFala.Vizinha, modo = "Pedalada", nivel = 2 },
            new LinhaDaTabela { dia = 4, npc = QuemFala.Guarda, modo = "Sinaleiro", nivel = 4 },
            new LinhaDaTabela { dia = 4, npc = QuemFala.Vizinha, modo = "Passeio de Carro", nivel = 4 },
            new LinhaDaTabela { dia = 5, npc = QuemFala.Engenheira, modo = "Pontes e Passagens", nivel = 5 },
            new LinhaDaTabela { dia = 5, npc = QuemFala.Mecanico, modo = "Oficina", nivel = 5 },
            new LinhaDaTabela { dia = 5, npc = QuemFala.Guarda, modo = "Motorista Mirim", nivel = 3 },
            new LinhaDaTabela { dia = 5, npc = QuemFala.Engenheira, modo = "Planejar a rua", nivel = 5 },
            new LinhaDaTabela { dia = 5, npc = QuemFala.Guarda, modo = "Sinaleiro", nivel = 5 },
            new LinhaDaTabela { dia = 5, npc = QuemFala.Vizinha, modo = "Passeio de Carro", nivel = 5 },
            new LinhaDaTabela { dia = 5, npc = QuemFala.Vizinha, modo = "Pedalada", nivel = 3 },
        };

        static readonly FalaDoNpc[] Npcs =
        {
            new FalaDoNpc { quem = QuemFala.Mae, nome = "Mãe", lugar = "perto do parquinho" },
            new FalaDoNpc { quem = QuemFala.Mecanico, nome = "o mecânico Zé", lugar = "na Oficina, no comércio" },
            new FalaDoNpc { quem = QuemFala.Engenheira, nome = "a engenheira Ana", lugar = "nas Obras, na beira do lago" },
            new FalaDoNpc { quem = QuemFala.Guarda, nome = "o guarda Beto", lugar = "no Cruzamento Central, no Eixinho" },
            new FalaDoNpc { quem = QuemFala.Vizinha, nome = "a vizinha Lúcia", lugar = "na Rua Viva, perto do ponto de ônibus" },
        };

        public static void Dados()
        {
            ArtePaleta.GarantirPasta(PastaDosDados);
            ArtePaleta.GarantirPasta("Assets/Resources");

            var oficina = Missao("d1_oficina", QuemFala.Mecanico, "Entre na Oficina e ajude o mecânico Zé.",
                new[] { "Oi! A água entrou na oficina e desmontou tudo.",
                        "Sem veículos, ninguém consegue ir trabalhar nem ir pra escola.",
                        "Me ajuda a montar os primeiros? Entre na oficina!" },
                "Ficaram ótimos! Já tem veículo pronto aqui na frente. Obrigado!",
                "Minigame_Oficina", -1, "Oficina de Veículos", 1, "Area_Oficina",
                "Os primeiros veículos ficam prontos e estacionados na frente da oficina.");
            var pontes = Missao("d1_pontes", QuemFala.Engenheira, "Entre nas Obras e ajude a engenheira Ana.",
                new[] { "Que bom que você veio! A enchente estragou os caminhos da cidade.",
                        "Tem gente que não consegue atravessar o rio nem passar pelo morro.",
                        "Me ajuda a escolher as obras? Entre na área das Obras!" },
                "Muito bem! O pessoal já consegue passar. E a obra do píer do lago começou!",
                "Minigame_Obras", 0, "Obras da Cidade", 1, "Area_ObrasDaCidade",
                "Na beira do lago, a bagunça some e aparece a obra organizada: cavaletes em pé e sinalização.");
            var motorista = Missao("d1_motorista", QuemFala.Guarda, "Entre no Cruzamento Central e ajude o guarda Beto.",
                new[] { "Olá! Os semáforos da cidade voltaram a ligar.",
                        "Mas antes de liberar o trânsito, eles precisam ser testados.",
                        "Me ajuda a dirigir o carrinho de teste pelo bairro? Entre no cruzamento!" },
                "Tudo testado! O primeiro semáforo já está de pé no cruzamento.",
                "Minigame_Sinais", 0, "Motorista Mirim", 1, "Area_CruzamentoCentral",
                "Um semáforo que estava caído volta a ficar de pé no cruzamento.");
            var passeio = Missao("d1_passeio", QuemFala.Vizinha, "Entre na Rua Viva e passeie com a vizinha Lúcia.",
                new[] { "Oi, vizinho! O parque reabriu hoje!",
                        "Vou levar a família pra passear de carro. Vem com a gente?",
                        "O carro está aqui na Rua Viva. É só entrar!" },
                "Que passeio bom! E a calçada da ciclovia já está ficando limpinha de novo.",
                "Minigame_RuaViva_Carro", -1, "Rua Viva", 1, "Area_RuaViva",
                "Na calçada da ciclovia, o lixo some e o banco volta a ficar em pé, com flores.");

            var dia1 = Carregar<DiaDaCampanha>($"{PastaDosDados}/Dia_1.asset");
            dia1.numero = 1;
            dia1.titulo = "Depois da enchente";
            dia1.falasDaMae = new[]
            {
                "Que bom que você chegou! A enchente bagunçou a cidade inteira.",
                "Hoje começa a reconstrução. Muita gente precisa da sua ajuda.",
                "Quem tem um ponto de exclamação em cima quer falar com você. Siga as setas no chão!",
            };
            dia1.falasDoFim = new[]
            {
                "Você ajudou o bairro todo hoje! Que orgulho!",
                "Amanhã a gente continua a reconstrução.",
            };
            dia1.missoes = new[] { oficina, pontes, motorista, passeio };
            EditorUtility.SetDirty(dia1);

            var d = Carregar<CampanhaDados>(CaminhoDosDados);
            d.dias = new[] { dia1 };
            d.npcs = Npcs;
            d.objetivoAndarAteAMae = "Ande até a sua mãe (setas ou W A S D).";
            d.objetivoFalarComAMae = "Fale com a sua mãe: toque nela ou aperte E.";
            d.objetivoAbrirOAlbum = "Toque no Álbum pra ver a sua figurinha nova!";
            d.falasDoAlbum = new[]
            {
                "Viu só? Cada ajuda que você dá vira uma figurinha nova.",
                "Toque no Álbum, lá no canto de cima, pra ver a sua!",
            };
            d.objetivoFalarCom = "Fale com {0} {1}.";
            d.objetivoVoltarAMae = "Volte e fale com a sua mãe.";
            d.objetivoLivre = "Jogue de novo o que quiser!";
            d.falasDaMaeComMissao = new[] { "Ainda tem gente precisando de ajuda!", "Fale com {0} {1}. Siga as setas!" };
            d.falasDoFimDaCampanha = new[]
            {
                "Por hoje é só! Os próximos dias da reconstrução estão chegando.",
                "Enquanto isso, você pode jogar de novo o que quiser.",
            };
            d.falaAntesDaMae = "Calma! Primeiro fale com a sua mãe. Ela está perto do parquinho.";
            d.falaSemMissaoHoje = "Ainda não é hora daqui. Fale com a sua mãe pra saber o que fazer hoje.";
            d.falaMissaoAceita = "É só entrar ali! Siga as setas.";
            d.falaJaAjudou = "Obrigado pela ajuda! Se quiser jogar de novo, é só entrar.";
            d.textoConversar = "Conversar (E)";
            d.textoSeguir = "Seguir";
            d.textoVamos = "Vamos!";
            d.textoOuvir = "Ouvir";
            d.textoPular = "Pular tutorial";
            d.textoMoedas = "+{0} moedas!";
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
        }

        static T Carregar<T>(string caminho) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(caminho);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(a, caminho);
            }
            return a;
        }

        static MissaoDaCampanha Missao(string id, QuemFala npc, string objetivo, string[] falas, string agradecimento,
            string cena, int modo, string area, int nivel, string areaDoHub, string conserto)
        {
            var m = Carregar<MissaoDaCampanha>($"{PastaDosDados}/Missao_{id}.asset");
            m.id = id; m.npc = npc; m.objetivo = objetivo; m.falas = falas; m.agradecimento = agradecimento;
            m.cena = cena; m.modo = modo; m.nomeDaArea = area; m.nivel = nivel; m.areaDoHub = areaDoHub; m.conserto = conserto;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ no Hub

        static GameObject P(Transform pai, string prefab, Vector3 pos, float rotY = 0f, float escala = 1f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArteImportador.PastaPrefabs}/{prefab}.prefab");
            if (asset == null) { Debug.LogError("[Campanha] Prefab não encontrado: " + prefab); return new GameObject("FALTOU_" + prefab); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, pai);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(0, rotY, 0);
            go.transform.localScale = Vector3.one * escala;
            return go;
        }

        static void SemSombra(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Onde cada um fica e pra onde olha. A mãe, no mundo (perto do parquinho, no meio da superquadra); os
        // ajudantes, no espaço da área deles (o mapa novo gira cada área: +Z = o cenário, -Z = de onde o jogador
        // chega), uns metros antes do marcador e olhando pra quem chega.
        static readonly (QuemFala quem, string prefab, string area, Vector3 pos, Vector3 olhar)[] OndeFicam =
        {
            (QuemFala.Mae, "Personagens/Familia_Mae_Animado", "", new Vector3(56.5f, 0, 1f), new Vector3(-.85f, 0, -.5f)),
            (QuemFala.Mecanico, "Personagens/Trabalhador", "Area_Oficina", new Vector3(-3f, 0, -8.5f), Vector3.back),
            (QuemFala.Engenheira, "Personagens/Trabalhadora", "Area_ObrasDaCidade", new Vector3(-3f, 0, -4.5f), Vector3.back),
            (QuemFala.Guarda, "Personagens/Mini_Homem_3", "Area_CruzamentoCentral", new Vector3(2.5f, 0, -4.5f), Vector3.back),
            (QuemFala.Vizinha, "Personagens/Pedestre_Mulher", "Area_RuaViva", new Vector3(2.5f, 0, -4.5f), Vector3.back),
        };

        // Do espaço da área pro mundo (no chão).
        static Vector3 NoMundo(string area, Vector3 local)
        {
            if (string.IsNullOrEmpty(area)) return local;
            var t = GameObject.Find(area);
            if (t == null) { Debug.LogError("[Campanha] Área não encontrada: " + area); return local; }
            var p = t.transform.position + t.transform.rotation * local;
            p.y = 0f;
            return p;
        }

        static Vector3 DirecaoNoMundo(string area, Vector3 local)
        {
            var t = string.IsNullOrEmpty(area) ? null : GameObject.Find(area);
            return t == null ? local : t.transform.rotation * local;
        }

        // Chamado pelo HubConstrutor depois do PainelDoHub (precisa do Canvas UI_Hub) e antes dos colisores.
        public static void NoHub()
        {
            var velho = GameObject.Find("Campanha");
            if (velho != null) Object.DestroyImmediate(velho);
            var dados = AssetDatabase.LoadAssetAtPath<CampanhaDados>(CaminhoDosDados);
            if (dados == null) { Dados(); dados = AssetDatabase.LoadAssetAtPath<CampanhaDados>(CaminhoDosDados); }

            var raiz = new GameObject("Campanha").transform;
            var c = raiz.gameObject.AddComponent<CampanhaNoHub>();
            var jogador = Object.FindAnyObjectByType<PlayerMovement>();
            c.jogador = jogador != null ? jogador.transform : null;

            // NPCs
            var npcs = new List<NpcDaCampanha>();
            var grupoNpcs = new GameObject("NPCs").transform;
            grupoNpcs.SetParent(raiz, false);
            foreach (var ondeFica in OndeFicam)
            {
                var o = (ondeFica.quem, ondeFica.prefab, ondeFica.area, pos: NoMundo(ondeFica.area, ondeFica.pos), olhar: DirecaoNoMundo(ondeFica.area, ondeFica.olhar));
                var go = P(grupoNpcs, o.prefab, o.pos, Quaternion.LookRotation(o.olhar).eulerAngles.y);
                go.name = "NPC_" + o.quem;
                var npc = go.AddComponent<NpcDaCampanha>();
                npc.quem = o.quem;
                npc.areaDoHub = o.area;
                npc.olharPadrao = o.olhar.normalized;
                npc.animador = go.GetComponentInChildren<Animator>();
                Bounds b = Medir(go);
                float altura = b.max.y - o.pos.y;
                var col = go.AddComponent<CapsuleCollider>();
                col.radius = .35f; col.height = Mathf.Max(1.2f, altura); col.center = new Vector3(0, col.height / 2f, 0);

                var ex = new GameObject("Exclamacao").transform;
                ex.SetParent(go.transform, false);
                ex.localPosition = new Vector3(0, altura + .75f, 0);
                SemSombra(F.Capsula(ex, "Traco", "Luz_Amarela", new Vector3(0, .22f, 0), new Vector3(.2f, .55f, .2f)));
                SemSombra(F.Esfera(ex, "Ponto", "Luz_Amarela", new Vector3(0, -.28f, 0), Vector3.one * .2f));
                npc.exclamacao = ex;

                if (o.quem == QuemFala.Mae)
                {
                    var seta = new GameObject("Seta_Grande").transform;
                    seta.SetParent(go.transform, false);
                    seta.localPosition = new Vector3(0, altura + 1.9f, 0);
                    SemSombra(F.M(seta, "Ponta", "Cone", "Mostarda", Vector3.zero, new Vector3(.8f, .7f, .8f), new Vector3(180, 0, 0)));
                    SemSombra(F.Cubo(seta, "Haste", "Mostarda", new Vector3(0, .65f, 0), new Vector3(.3f, .7f, .3f)));
                    npc.setaGrande = seta;
                }
                if (o.quem == QuemFala.Guarda)
                {
                    // Quepe de guarda (formas da paleta), em cima da cabeça.
                    var quepe = new GameObject("Quepe").transform;
                    quepe.SetParent(go.transform, false);
                    quepe.localPosition = new Vector3(0, altura - .1f, 0);
                    F.Cilindro(quepe, "Copa", "Petroleo_Escuro", new Vector3(0, .08f, 0), new Vector3(.5f, .08f, .5f));
                    F.Cilindro(quepe, "Faixa", "Branco", new Vector3(0, .02f, 0), new Vector3(.47f, .03f, .47f));
                    F.Cubo(quepe, "Aba", "Grafite", new Vector3(0, -.01f, .24f), new Vector3(.34f, .03f, .2f));
                    F.Esfera(quepe, "Distintivo", "Mostarda", new Vector3(0, .1f, .25f), Vector3.one * .08f);
                }
                npcs.Add(npc);
            }
            c.npcs = npcs.ToArray();

            Consertos(raiz, c);
            Trilha(raiz, c);

            // Partículas e sons
            c.confete = TravessiaConstrutor.Particulas(raiz, "Confete_Do_Conserto", 70, 1.6f, 6f, .18f, 1f, "Mostarda", "Rosa", "Petroleo_Claro", "Laranja", "Verde");
            c.poeira = TravessiaConstrutor.Particulas(raiz, "Poeira_Do_Conserto", 35, 1f, 2.5f, .5f, -.05f, "Cinza_Claro", "Areia");
            c.som = raiz.gameObject.AddComponent<AudioSource>();
            c.som.playOnAwake = false;
            c.somDoConserto = TravessiaConstrutor.Som("Interface/confirmation_003.ogg");
            c.somDasMoedas = TravessiaConstrutor.Som("Efeitos/moeda.wav");
            c.somDaConversa = TravessiaConstrutor.Som("Interface/select_002.ogg");

            Interface(c);
            Debug.Log("[Campanha] Montada no Hub: " + npcs.Count + " NPCs.");
        }

        static Bounds Medir(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // Cada missão do Dia 1 conserta um pedaço do Hub: Antes (estragado) some e Depois (arrumado) aparece.
        static void Consertos(Transform raiz, CampanhaNoHub c)
        {
            var g = new GameObject("Consertos").transform;
            g.SetParent(raiz, false);
            c.consertos = g;
            // "pos" no espaço da área (como os NPCs); o grupo fica alinhado com o mundo.
            Transform Grupo(string id, string area, Vector3 pos, out Transform antes, out Transform depois)
            {
                var t = new GameObject("Conserto_" + id).transform;
                t.SetParent(g, false);
                t.position = NoMundo(area, pos);
                antes = new GameObject("Antes").transform; antes.SetParent(t, false);
                depois = new GameObject("Depois").transform; depois.SetParent(t, false);
                depois.gameObject.SetActive(false);
                return t;
            }
            Transform a, d;

            // Oficina: peças e uma bicicleta tombada na varanda -> bicicleta, moto e carro estacionados.
            Grupo("d1_oficina", "Area_Oficina", new Vector3(5.5f, 0, 2f), out a, out d);
            P(a, "Carros/Bicicleta_Q1", new Vector3(-2.5f, .2f, 0), 30).transform.Rotate(0, 0, 80);
            P(a, "Enchente/Caixa_Aberta", new Vector3(0, 0, .5f), 20);
            P(a, "Enchente/Entulho_De_Concreto", new Vector3(2.5f, 0, 0), 60, .7f);
            P(d, "Carros/Bicicleta_Q1", new Vector3(-3.2f, 0, 0), 180);
            P(d, "Carros/Moto", new Vector3(-1.6f, 0, 0), 180);
            P(d, "Carros/Carro_Sedan", new Vector3(1.8f, 0, 0), 0);

            // Obras: cavaletes caídos e placa no chão na beira do lago -> obra organizada.
            Grupo("d1_pontes", "Area_ObrasDaCidade", new Vector3(5f, 0, .6f), out a, out d);
            P(a, "Enchente/Cavalete_Grande_1", new Vector3(0, .2f, 0), 30).transform.Rotate(0, 0, 80);
            P(a, "Enchente/Cavalete_Medio_1", new Vector3(.8f, .2f, -1.2f), 100).transform.Rotate(0, 0, -75);
            P(a, "Enchente/Papeis_Espalhados", new Vector3(-.5f, .02f, -.8f), 0);
            P(d, "Enchente/Cavalete_Grande_1", new Vector3(0, 0, .4f), 90);
            P(d, "Enchente/Cavalete_Grande_1", new Vector3(0, 0, -1.4f), 90);
            P(d, "Props/Sinalizador_Obra", new Vector3(.2f, 0, -2.8f), 90);

            // Cruzamento: semáforo caído -> de pé.
            Grupo("d1_motorista", "Area_CruzamentoCentral", new Vector3(-2.2f, 0, 2f), out a, out d);
            P(a, "Props/Semaforo_Carros", new Vector3(0, .3f, 0), 200).transform.Rotate(0, 0, 82);
            P(d, "Props/Semaforo_Carros", Vector3.zero, 180);

            // Rua Viva: lixo e banco virado na calçada da ciclovia -> banco em pé com flores.
            Grupo("d1_passeio", "Area_RuaViva", new Vector3(-4.5f, 0, -1.6f), out a, out d);
            P(a, "Props/Banco_De_Praca", new Vector3(0, .3f, 0), 90).transform.Rotate(0, 0, 85);
            P(a, "Lixo_Reciclagem/Pilha_De_Lixo", new Vector3(1.2f, 0, -1.2f), 30);
            P(d, "Props/Banco_De_Praca", Vector3.zero, 90);
            P(d, "Natureza/Flores", new Vector3(0, 0, 1.4f), 0, 1.4f);
            P(d, "Natureza/Flores", new Vector3(0, 0, -1.4f), 90, 1.4f);
        }

        static void Trilha(Transform raiz, CampanhaNoHub c)
        {

            // Seta no chão: um "V" amarelo achatado, apontando pra +Z.
            var seta = new GameObject("Modelo_Seta_Da_Trilha").transform;
            seta.SetParent(raiz, false);
            foreach (float lado in new[] { -1f, 1f })
            {
                var braco = F.Cubo(seta, "Braco", "Luz_Amarela", new Vector3(lado * .22f, 0, -.05f), new Vector3(.16f, .04f, .75f), new Vector3(0, -lado * 40f, 0));
                SemSombra(braco);
            }
            seta.gameObject.SetActive(false);
            c.modeloDaSeta = seta.gameObject;
        }

        // Grade do mapa pra trilha de setas (A* no jogo). Roda DEPOIS dos colisores do cenário: cada célula de
        // 0,6 m é livre se a cápsula do jogador (raio 0,45) cabe ali. Os consertos entram com Antes e Depois
        // ligados (fica livre só o que é livre nos dois).
        public static void AssarGrade()
        {
            var c = Object.FindAnyObjectByType<CampanhaNoHub>();
            if (c == null) return;
            var trocados = new List<GameObject>();
            foreach (Transform t in c.consertos.GetComponentsInChildren<Transform>(true))
                if ((t.name == "Antes" || t.name == "Depois") && !t.gameObject.activeSelf) { t.gameObject.SetActive(true); trocados.Add(t.gameObject); }
            // O mesmo com o caos e a ordem das áreas (Ordem_*, Arrumado): o que aparece quando a área se arruma
            // também tem colisor, e a trilha tem de desviar dele em qualquer estado.
            foreach (var area in Object.FindObjectsByType<AreaTrigger>(FindObjectsInactive.Exclude))
                foreach (Transform t in area.transform)
                    if ((t.name.StartsWith("Ordem_") || t.name.StartsWith("Caos") || t.name == "Arrumado") && !t.gameObject.activeSelf)
                    { t.gameObject.SetActive(true); trocados.Add(t.gameObject); }
            // Pelas caixas (bounds) de cada colisor, sem a física do editor (que só registra colisor novo depois):
            // célula bloqueada se a caixa, alargada pelo raio do jogador, cobre o centro dela na altura do corpo.
            const float passo = .6f, r = .65f, baixo = .35f, cima = 1.85f; // r: raio do jogador (0,5) + folga
            // Só a zona da superquadra (onde ficam os NPCs e as áreas) e a calçada até o Eixo Monumental.
            var origem = new Vector2(20f, -46f);
            int lx = Mathf.CeilToInt(70f / passo), lz = Mathf.CeilToInt(132f / passo);
            var livre = new byte[lx * lz];
            for (int i = 0; i < livre.Length; i++) livre[i] = 1;
            foreach (Collider col in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
            {
                if (!col.enabled || col.isTrigger || col is CharacterController) continue;
                Bounds b = col.bounds;
                if (b.max.y < baixo || b.min.y > cima) continue;
                int x0 = Mathf.Max(0, Mathf.FloorToInt((b.min.x - r - origem.x) / passo)), x1 = Mathf.Min(lx - 1, Mathf.CeilToInt((b.max.x + r - origem.x) / passo));
                int z0 = Mathf.Max(0, Mathf.FloorToInt((b.min.z - r - origem.y) / passo)), z1 = Mathf.Min(lz - 1, Mathf.CeilToInt((b.max.z + r - origem.y) / passo));
                bool redondo = col is CapsuleCollider || col is SphereCollider;
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float px = origem.x + x * passo, pz = origem.y + z * passo;
                        float dx = Mathf.Max(b.min.x - px, 0f, px - b.max.x), dz = Mathf.Max(b.min.z - pz, 0f, pz - b.max.z);
                        if (redondo)
                        {
                            float raio = Mathf.Max(b.extents.x, b.extents.z) + r;
                            if ((px - b.center.x) * (px - b.center.x) + (pz - b.center.z) * (pz - b.center.z) > raio * raio) continue;
                        }
                        else if (dx > r || dz > r) continue;
                        livre[z * lx + x] = 0;
                    }
            }
            // Os marcadores das áreas também ficam fora da trilha (raio 2 m + a folga): seguindo as setas pra outro
            // lugar, a criança não pisa numa área sem querer. Quando o destino é a área, a trilha chega na borda e o
            // último trecho vai reto até o marcador.
            foreach (var area in Object.FindObjectsByType<AreaTrigger>(FindObjectsInactive.Exclude))
            {
                Vector3 p = area.transform.position;
                float raio = 2f + r;
                for (int z = 0; z < lz; z++)
                    for (int x = 0; x < lx; x++)
                    {
                        float px = origem.x + x * passo - p.x, pz = origem.y + z * passo - p.z;
                        if (px * px + pz * pz <= raio * raio) livre[z * lx + x] = 0;
                    }
            }
            int livres = 0;
            foreach (byte v in livre) livres += v;
            foreach (var go in trocados) go.SetActive(false);
            c.livre = livre; c.largura = lx; c.altura = lz; c.origemDaGrade = origem; c.passoDaGrade = passo;
            EditorUtility.SetDirty(c);
            Debug.Log($"[Campanha] Grade da trilha: {lx}x{lz}, {livres} células livres.");
        }

        static void Interface(CampanhaNoHub c)
        {
            var canvas = GameObject.Find("UI_Hub").transform;
            RectTransform Ancorar(Component comp, Vector2 ancora)
            {
                var rt = (RectTransform)comp.transform;
                rt.anchorMin = rt.anchorMax = ancora;
                return rt;
            }

            // Objetivo fixo no alto (entre o painel do perfil e o Álbum).
            var barra = Imagem("Objetivo_Da_Campanha", canvas, arredondado, C("Petroleo_Escuro", .93f), new Vector2(0, -62), new Vector2(740, 96));
            Ancorar(barra, new Vector2(.5f, 1));
            Imagem("Borda", barra.transform, arredondado, C("Mostarda"), new Vector2(0, -44), new Vector2(700, 8));
            var texto = Texto("Texto", barra.transform, "Objetivo", 32, C("Branco"), new Vector2(0, 4), new Vector2(700, 80));
            texto.resizeTextForBestFit = true; texto.resizeTextMinSize = 24; texto.resizeTextMaxSize = 32;
            c.barraDoObjetivo = barra.gameObject;
            c.textoDoObjetivo = texto;

            // Conversa: balão grande embaixo, com o nome de quem fala, Ouvir e Seguir.
            var painel = Imagem("Conversa_Da_Campanha", canvas, arredondado, C("Branco", .97f), new Vector2(0, 210), new Vector2(1560, 340), true);
            Ancorar(painel, new Vector2(.5f, 0));
            var etiqueta = Imagem("Etiqueta", painel.transform, arredondado, C("Petroleo"), new Vector2(-520, 165), new Vector2(480, 76));
            c.nomeDeQuemFala = Texto("Nome", etiqueta.transform, "Mãe", 36, C("Branco"), Vector2.zero, new Vector2(450, 66));
            c.nomeDeQuemFala.resizeTextForBestFit = true; c.nomeDeQuemFala.resizeTextMinSize = 26; c.nomeDeQuemFala.resizeTextMaxSize = 36;
            c.textoDaFala = Texto("Fala", painel.transform, "...", 44, C("Grafite"), new Vector2(-150, -10), new Vector2(1160, 250), FontStyle.Normal);
            c.textoDaFala.alignment = TextAnchor.MiddleLeft;
            c.textoDaFala.resizeTextForBestFit = true; c.textoDaFala.resizeTextMinSize = 32; c.textoDaFala.resizeTextMaxSize = 44;
            c.botaoOuvir = Botao("Botao_Ouvir_Da_Conversa", painel.transform, "Ouvir", "Petroleo_Claro", new Vector2(600, 70), new Vector2(250, 100), 36);
            c.botaoSeguir = Botao("Botao_Seguir_Da_Conversa", painel.transform, "Seguir", "Laranja", new Vector2(600, -75), new Vector2(290, 116), 42);
            c.textoDoSeguir = c.botaoSeguir.transform.Find("Frente/Texto").GetComponent<Text>();
            c.painelDaConversa = painel.gameObject;

            c.botaoConversar = Botao("Botao_Conversar", canvas, "Conversar (E)", "Laranja", new Vector2(0, 210), new Vector2(460, 120), 42);
            Ancorar(c.botaoConversar, new Vector2(.5f, 0));

            // "Ir", à direita da barra do objetivo: leva pra perto de quem a criança precisa encontrar.
            c.botaoIr = Botao("Botao_Ir", barra.transform, "Ir", "Laranja", new Vector2(460, 4), new Vector2(150, 92), 46);

            c.botaoPular = Botao("Botao_Pular_Tutorial", canvas, "Pular tutorial", "Cinza", new Vector2(-200, 130), new Vector2(340, 96), 34);
            Ancorar(c.botaoPular, new Vector2(1, 0));

            // Seta pro Álbum (tutorial): à esquerda do botão, apontando pra ele.
            var seta = Imagem("Seta_Do_Album", canvas, S("ui_seta_voltar"), C("Mostarda"), new Vector2(-450, -110), new Vector2(130, 130));
            var rtSeta = Ancorar(seta, new Vector2(1, 1));
            rtSeta.localEulerAngles = new Vector3(0, 0, 180);
            c.setaDoAlbum = rtSeta;

            var moedas = Imagem("Moedas_Ganhas", canvas, arredondado, C("Mostarda"), new Vector2(-210, -360), new Vector2(360, 96));
            Ancorar(moedas, new Vector2(1, 1));
            c.textoDasMoedas = Texto("Texto", moedas.transform, "+10 moedas!", 42, C("Branco"), Vector2.zero, new Vector2(340, 86));
            c.avisoDasMoedas = moedas.gameObject;

            var dica = canvas.Find("Dica_Do_Olhar");
            c.dicaDoOlhar = dica != null ? dica.gameObject : null;
        }

        // ------------------------------------------------------------------ documento das falas

        static string NomeDoModo(MissaoDaCampanha m)
        {
            switch (m.cena)
            {
                case "Minigame_Oficina": return "Oficina";
                case "Minigame_Obras": return m.modo == 1 ? "Planejar a rua" : "Pontes e Passagens";
                case "Minigame_Sinais": return m.modo == 1 ? "Sinaleiro" : "Motorista Mirim";
                case "Minigame_RuaViva_Carro": return "Passeio de Carro";
                case "Minigame_RuaViva_Pedalada": return "Pedalada";
            }
            return m.cena;
        }

        static string DocumentoDasFalas(CampanhaDados d)
        {
            var sb = new StringBuilder();
            void L(string s = "") => sb.AppendLine(s);
            void Lista(IEnumerable<string> falas) { foreach (var f in falas) L($"- \"{f}\""); }
            L("# Falas da campanha \"Os 5 dias da reconstrução\"");
            L();
            L($"Gerado pelo `CampanhaConstrutor` (menu **Cidade Viva > Campanha > Gerar o documento das falas**) em {System.DateTime.Now:dd/MM/yyyy}. Pra a coordenação revisar.");
            L();
            L("**Regra das falas:** os personagens só dão o motivo da missão. Nenhuma fala explica um verbete nem resume uma definição: quem ensina o verbete é o cartão, com a definição copiada do glossário. Frases curtas, pra crianças de 6 a 9 anos. **Nada aqui vem do glossário.**");
            L();
            L("## Personagens");
            L();
            foreach (var n in d.npcs) L($"- **{n.nome}** ({n.lugar})");
            L();
            L("## Tutorial (Dia 1, com a mãe)");
            L();
            L($"- Objetivo 1: \"{d.objetivoAndarAteAMae}\"");
            L($"- Objetivo 2: \"{d.objetivoFalarComAMae}\"");
            L("- A mãe abre o Dia 1 (falas abaixo). Depois, a trilha de setas leva ao primeiro ajudante.");
            L("- Depois da primeira missão, a mãe fala do Álbum:");
            Lista(d.falasDoAlbum);
            L($"- Objetivo: \"{d.objetivoAbrirOAlbum}\"");
            L($"- Botão: \"{d.textoPular}\"");
            L();
            foreach (var dia in d.dias)
            {
                L($"## Dia {dia.numero}: {dia.titulo}");
                L();
                L("**A mãe abre o dia:**");
                Lista(dia.falasDaMae);
                L();
                foreach (var m in dia.missoes)
                {
                    L($"### {d.NomeDe(m.npc)} ({NomeDoModo(m)}, nível {m.nivel})");
                    L();
                    L($"- Objetivo no alto da tela: \"{m.objetivo}\"");
                    L("- Conversa:");
                    foreach (var f in m.falas) L($"  - \"{f}\"");
                    L($"- Agradecimento (na volta): \"{m.agradecimento}\"");
                    L($"- O que se conserta no Hub: {m.conserto}");
                    L();
                }
                L("**A mãe fecha o dia:**");
                Lista(dia.falasDoFim);
                L();
            }
            L("## Frases gerais");
            L();
            L($"- Ajudante, antes de falar com a mãe: \"{d.falaAntesDaMae}\"");
            L($"- Ajudante, sem missão no dia: \"{d.falaSemMissaoHoje}\"");
            L($"- Ajudante, missão já aceita: \"{d.falaMissaoAceita}\"");
            L($"- Ajudante, já ajudou: \"{d.falaJaAjudou}\"");
            L($"- Mãe, quando ainda há missão: {string.Join(" / ", d.falasDaMaeComMissao.Select(f => "\"" + f + "\""))} ({{0}} = quem, {{1}} = onde)");
            L($"- Mãe, depois do último dia que existe: {string.Join(" / ", d.falasDoFimDaCampanha.Select(f => "\"" + f + "\""))}");
            L($"- Objetivos: \"{d.objetivoFalarCom}\" / \"{d.objetivoVoltarAMae}\" / \"{d.objetivoLivre}\"");
            L($"- Botões: \"{d.textoConversar}\", \"{d.textoOuvir}\", \"{d.textoSeguir}\", \"{d.textoVamos}\"; moedas: \"{d.textoMoedas}\"");
            L();
            L("## Dias 2 a 5 (só a tabela; falas ainda não escritas)");
            L();
            L("| Dia | Tema | Personagem | Modo | Nível |");
            L("|---|---|---|---|---|");
            foreach (var t in Tabela.Where(t => t.dia >= 2))
            {
                string tema = TitulosDosDias.First(x => x.dia == t.dia).titulo;
                L($"| {t.dia} | {tema} | {Npcs.First(n => n.quem == t.npc).nome} | {t.modo} | {t.nivel} |");
            }
            L();
            L("O Dia 5 termina no final 3D.");
            return sb.ToString();
        }
    }
}
