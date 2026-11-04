using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static CidadeViva.EditorArte.MenuConstrutor;

namespace CidadeViva.EditorArte
{
    // Monta do zero a Rua Viva, parte 1 (Passeio de Carro): config, níveis e a cena Minigame_RuaViva_Carro.
    // Verbetes da mecânica: cinto de segurança, passageiro e buzina.
    public static class RuaVivaCarroConstrutor
    {
        const string CaminhoDaCena = "Assets/Scenes/Minigame_RuaViva_Carro.unity";
        const string CaminhoDaConfig = "Assets/Configs/RuaVivaCarro_Config.asset";
        const string CaminhoDosNiveis = "Assets/Configs/Niveis/Niveis_RuaVivaCarro.asset";
        public const string NomeDaCena = "Minigame_RuaViva_Carro";
        public const string NomeDaArea = "Rua Viva";

        const float RaioDaCurva = 28f, LadoDaPista = 1.8f, PassoDaRota = .25f, Prolongamento = 300f, Reta = 220f;
        const int Retas = 5;
        static readonly Vector3 LugarDaGaragem = new Vector3(600f, 0f, 0f);

        [MenuItem("Cidade Viva/Minigames/Reconstruir a cena do Passeio de Carro (Rua Viva)")]
        public static void ReconstruirPeloMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Reconstruir();
        }

        static GameObject P(Transform pai, string prefab, Vector3 pos, float rotY = 0f, float escala = 1f)
        {
            return TravessiaConstrutor.P(pai, prefab, pos, rotY, escala);
        }

        static GameObject SemSombra(GameObject go)
        {
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public static void Reconstruir()
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            PrepararIcones();
            VerbetesConstrutor.Atualizar();
            var config = PrepararConfig();
            var niveis = SalvarNiveis();

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 55f;
            cam.nearClipPlane = .1f;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AmbienteConstrutor.CaminhoPrefab) == null) AmbienteConstrutor.CriarPrefab();
            AmbienteConstrutor.AplicarNaCenaAberta(true);

            var percurso = Percurso();
            var pug = P(cam.transform, "Natureza/Cachorro_Pug", F.V(-1.05f, -.5f, 1.6f), 150f);
            pug.name = "Bichinho";

            // Garagem.
            var garagem = Garagem(out var pose, out var carro, out var familia, out var porta);

            var efeitos = F.Grupo("Efeitos").transform;
            var estrelinhas = TravessiaConstrutor.Particulas(efeitos, "Estrelinhas", 14, .7f, 3.5f, .3f, .6f, "Mostarda", "Mostarda_Clara", "Branco");
            var confete = TravessiaConstrutor.Particulas(efeitos, "Confete", 70, 1.6f, 6.5f, .3f, 2.5f, "Mostarda", "Vermelho", "Petroleo_Claro", "Verde", "Rosa", "Laranja");
            var susto = TravessiaConstrutor.Particulas(efeitos, "Susto", 10, .5f, 1.6f, .06f, .2f, "Mostarda", "Branco");

            var canvas = NovoCanvas("Canvas");
            NovoEventSystem(false);
            var hud = Hud(canvas);
            var fim = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabFim), canvas.transform);
            var selecao = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabSelecao), canvas.transform);

            var gerenteGo = new GameObject("RuaVivaCarroManager", typeof(AudioSource));
            var som = gerenteGo.GetComponent<AudioSource>();
            som.playOnAwake = false;
            som.volume = .8f;
            var gerente = gerenteGo.AddComponent<RuaVivaCarroManager>();
            gerente.nomeDaArea = NomeDaArea;
            gerente.telaFimDeJogo = fim.GetComponent<TelaFimDeJogo>();
            gerente.telaDeSelecao = selecao.GetComponent<TelaSelecaoDeNivel>();
            gerente.niveis = niveis;
            gerente.config = config;
            gerente.hud = hud;
            gerente.som = som;
            gerente.cameraDoJogo = cam;
            gerente.percurso = percurso;
            gerente.poseDaGaragem = pose;
            gerente.carroDaGaragem = carro;
            gerente.familia = familia;
            gerente.portaDoCarro = porta;
            gerente.bichinho = pug.transform;
            gerente.estrelinhas = estrelinhas;
            gerente.confete = confete;
            gerente.susto = susto;
            hud.fivela.manager = gerente;
            hud.fivela.canvas = canvas;

            // Os botões do passeio.
            var buzina = hud.botaoDaBuzina.GetComponent<Button>();
            UnityEventTools.AddPersistentListener(buzina.onClick, gerente.Buzinar);
            UnityEventTools.AddPersistentListener(hud.botaoOuvir.GetComponent<Button>().onClick, gerente.OuvirPedido);

            ConferirTrechos();
            EditorSceneManager.SaveScene(cena, CaminhoDaCena);
            if (EditorBuildSettings.scenes.All(s => s.path != CaminhoDaCena))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(CaminhoDaCena, true)).ToArray();
        }

        // ------------------------------------------------------------------ percurso

        static Vector3 Frente(float giro) { float r = giro * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(r), 0, Mathf.Cos(r)); }
        static Vector3 Direita(float giro) { float r = giro * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(r), 0, -Mathf.Sin(r)); }

        static void Pedaco(Transform pai, string nome, string mat, Vector3 a, Vector3 b, float largura, float yBase, float altura)
        {
            var go = SemSombra(F.Cubo(pai, nome, mat, (a + b) / 2f + Vector3.up * (yBase + altura / 2f), F.V(largura, altura, Vector3.Distance(a, b) + .5f)));
            go.transform.rotation = Quaternion.LookRotation(b - a);
        }

        static void PistaEntre(Transform chao, Vector3 a, Vector3 b, float giroA, float giroB)
        {
            Pedaco(chao, "Pista", "Asfalto", a, b, 7.2f, -.09f, .1f);
            foreach (float lado in new[] { -4.6f, 4.6f })
                Pedaco(chao, "Calcada", "Cinza_Claro", a + Direita(giroA) * lado, b + Direita(giroB) * lado, 2f, -.02f, .1f);
        }

        static RuaVivaPercurso Percurso()
        {
            var mundo = F.Grupo("Passeio").transform;
            var chao = F.Grupo("Chao", mundo).transform;
            var tracos = F.Grupo("Tracejado", chao).transform;
            var bairro = F.Grupo("Bairro", mundo).transform;
            var percurso = mundo.gameObject.AddComponent<RuaVivaPercurso>();

            string[] casas = { "Casa_01", "Casa_02", "Casa_03", "Casa_05", "Casa_06", "Casa_07", "Casa_08", "Padaria" };
            string[] arvores = { "Arvore_Comum_1", "Arvore_Comum_2", "Arvore_Comum_3", "Arvore_Betula" };

            Vector3 pos = F.V(0, 0, 0);
            float giro = 0f;
            int n = 0;
            float paraOTraco = 0f;
            var lista = new List<Vector3>();
            var limites = new Bounds(pos, Vector3.zero);

            void Ponto()
            {
                lista.Add(pos + Direita(giro) * LadoDaPista + Vector3.up * .01f);
                limites.Encapsulate(pos);
                paraOTraco -= 1f;
                if (paraOTraco <= 0f)
                {
                    paraOTraco = 4f;
                    var traco = SemSombra(F.Cubo(tracos, "Traco", "Branco", pos + Vector3.up * .016f, F.V(.14f, .012f, 1.6f)));
                    traco.transform.rotation = Quaternion.LookRotation(Frente(giro));
                }
            }

            PistaEntre(chao, pos + Vector3.back * Prolongamento, pos, 0f, 0f);
            for (int t = 0; t < Retas; t++)
            {
                Vector3 comeco = pos;
                PistaEntre(chao, comeco, comeco + Frente(giro) * Reta, giro, giro);

                // O lado direito fica aberto perto da rua: é onde aparecem os objetos do safari.
                // As casas ficam atrás deles; as de perto são postas pelo jogo, onde não escondem nada.
                for (float d = 8f; d < Reta - 8f; d += 16f, n++)
                {
                    Vector3 onde = comeco + Frente(giro) * d;
                    P(bairro, "Predios/" + casas[n % casas.Length], onde + Direita(giro) * 44f, giro - 90);
                    P(bairro, "Natureza/" + arvores[n % arvores.Length], onde + Direita(giro) * (36f + n % 3), n * 47, .9f);
                    P(bairro, "Predios/" + casas[(n + 3) % casas.Length], onde + Direita(giro) * -12f, giro + 90);
                    // Postes só do outro lado: deste, tapariam a janela.
                    if (n % 3 == 0) P(bairro, "Props/Poste_Reto", onde + Direita(giro) * -5.6f, giro + 90);
                }

                for (float d = 0f; d < Reta; d += 1f)
                {
                    Ponto();
                    pos += Frente(giro);
                }
                if (t == Retas - 1) break;

                int curva = t % 2 == 0 ? 1 : -1;
                const float degrau = 6f;
                Vector3 eixo = pos + Direita(giro) * RaioDaCurva * curva;
                for (float a = 0f; a < 90f; a += degrau)
                {
                    Vector3 antes = pos;
                    float giroAntes = giro;
                    Ponto();
                    giro += degrau * curva;
                    pos = eixo - Direita(giro) * RaioDaCurva * curva;
                    PistaEntre(chao, antes, pos, giroAntes, giro);
                    // Ponto a mais no meio do arco: a curva fica lisa na rota.
                    Vector3 guardado = pos;
                    float giroGuardado = giro;
                    giro = giroAntes + degrau * curva / 2f;
                    pos = eixo - Direita(giro) * RaioDaCurva * curva;
                    Ponto();
                    pos = guardado;
                    giro = giroGuardado;
                }
            }
            PistaEntre(chao, pos, pos + Frente(giro) * Prolongamento, giro, giro);
            SemSombra(F.Plano(chao, "Gramado", "Verde_Claro", limites.center + Vector3.down * .12f, limites.size.x + 1400f, limites.size.z + 1400f));

            var rota = new List<Vector3> { lista[0] };
            float sobra = 0f;
            for (int i = 1; i < lista.Count; i++)
            {
                Vector3 a = lista[i - 1], b = lista[i];
                float tam = Vector3.Distance(a, b);
                float d = PassoDaRota - sobra;
                while (d <= tam)
                {
                    rota.Add(Vector3.Lerp(a, b, d / tam));
                    d += PassoDaRota;
                }
                sobra = tam - (d - PassoDaRota);
            }
            percurso.rota = rota.ToArray();
            percurso.passoDaRota = PassoDaRota;

            var modelos = F.Grupo("Modelos", mundo).transform;
            percurso.modelosDoSafari = ModelosDoSafari(modelos);
            percurso.modelosDePedestre = new[] { "Pedestre_Mulher", "Idoso", "Trabalhador", "Idosa", "Trabalhadora" }.Select(x => Inativo(P(modelos, "Personagens/" + x, Vector3.zero))).ToArray();
            percurso.modeloDeCiclista = Inativo(P(modelos, "Obras/Ciclista", Vector3.zero));
            percurso.modelosDeCasa = new[] { "Casa_01", "Casa_03", "Casa_06", "Casa_08" }.Select(x => Inativo(P(modelos, "Predios/" + x, Vector3.zero))).ToArray();
            percurso.colocados = F.Grupo("Colocados", mundo).transform;
            return percurso;
        }

        static GameObject Inativo(GameObject go)
        {
            go.name = "Modelo_" + go.name;
            go.SetActive(false);
            return go;
        }

        // ------------------------------------------------------------------ catálogo do safari

        // id, artigo, dificuldade, prefab, giro extra, o que mais ele responde.
        // "aceita" sai das definições: automóvel é "Veículo com motor, com quatro ou mais rodas";
        // bicicleta elétrica é "Bicicleta com motor"; ciclista "conduz uma bicicleta".
        static readonly string[] Automovel = { "automovel" };
        static readonly (string id, string artigo, int dif, string prefab, float giro, string[] aceita)[] Modelos3D =
        {
            ("automovel", "um", 1, "Carros/Carro_Sedan", 0f, new string[0]),
            ("ambulancia", "uma", 1, "Carros/Ambulancia", 0f, Automovel),
            ("onibus", "um", 1, "Carros/Onibus_Urbano", 0f, Automovel),
            ("caminhao_de_bombeiro", "um", 1, "Carros/Caminhao_Bombeiros", 0f, Automovel),
            ("ponte", "uma", 1, "Obras/Ponte_JK", 0f, new string[0]),
            ("viatura", "uma", 2, "Carros/Viatura_De_Policia", 0f, Automovel),
            ("micro_onibus", "um", 2, "Carros/Carro_Van", 0f, Automovel),
            ("bonde", "um", 2, "Carros/Metro", 0f, new string[0]),
            ("motocicleta", "uma", 2, "Carros/Moto", 0f, new string[0]),
            ("ciclista", "um", 2, "Obras/Ciclista", 0f, new[] { "bicicleta" }),
            ("caminhao_trator", "um", 2, "Oficina/Pronto_Caminhao_Trator", 0f, new string[0]),
            ("passarela", "uma", 2, "Obras/Estrutura_passarela", 0f, new string[0]),
            ("viaduto", "um", 2, "Obras/Estrutura_viaduto", 0f, new string[0]),
            ("tunel", "um", 2, "Obras/Estrutura_tunel", 0f, new string[0]),
            ("estacionamento", "um", 2, "Obras/Estrutura_estacionamento", 90f, new string[0]),
            ("ciclovia", "uma", 2, "Obras/Estrutura_ciclovia", 0f, new string[0]),
            ("quadriciclo", "um", 3, "Oficina/Pronto_Quadriciclo", 0f, new string[0]),
            ("reboque", "um", 3, "Oficina/Pronto_Reboque", 0f, new string[0]),
            ("motoneta", "uma", 3, "Oficina/Pronto_Motoneta", 0f, new string[0]),
            ("bicicleta", "uma", 3, "Carros/Bicicleta_Azul", 90f, new string[0]),
            ("bicicleta_eletrica", "uma", 3, "Oficina/Pronto_Bicicleta_Eletrica", 0f, new[] { "bicicleta" }),
            ("semaforo_de_pedestre", "um", 3, "Props/Semaforo_Pedestres", 180f, new string[0]),
        };

        // Sem modelo que sirva sozinho: um outdoor com a ilustração do glossário.
        static readonly (string id, string artigo, int dif)[] Outdoors =
        {
            ("via", "uma", 2), ("via_rural", "uma", 3), ("via_urbana", "uma", 3), ("calcada", "uma", 2), ("faixa_de_pedestre", "uma", 2),
            ("ciclofaixa", "uma", 3), ("retorno", "um", 3), ("passagem_subterranea", "uma", 3),
            ("cinto_de_seguranca", "um", 2), ("buzina", "uma", 2), ("engarrafamento", "um", 3),
        };

        // O semáforo com cada luz acesa.
        static readonly (string id, string artigo, CorDoSinal cor, string[] aceita)[] Semaforos =
        {
            ("sinal_verde", "um", CorDoSinal.Verde, new[] { "semaforo" }),
            ("sinal_amarelo", "um", CorDoSinal.Amarelo, new[] { "semaforo" }),
            ("sinal_vermelho", "um", CorDoSinal.Vermelho, new[] { "semaforo" }),
        };

        static ObjetoDoSafari[] ModelosDoSafari(Transform pai)
        {
            var lista = new List<ObjetoDoSafari>();
            ObjetoDoSafari Marcar(GameObject go, string id, string artigo, int dif, float giro, string[] aceita)
            {
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                // As estruturas das Obras trazem o script e o caminho da travessia, que aqui não servem.
                var obra = go.GetComponent<ObraEstrutura>();
                if (obra != null) Object.DestroyImmediate(obra);
                var caminho = go.transform.Find("Caminho");
                if (caminho != null) Object.DestroyImmediate(caminho.gameObject);

                var o = go.AddComponent<ObjetoDoSafari>();
                o.idDoVerbete = id;
                o.artigo = artigo;
                o.dificuldade = dif;
                o.giroExtra = giro;
                o.aceita = aceita;
                var r = go.GetComponentsInChildren<Renderer>(true);
                var b = new Bounds(go.transform.position, Vector3.zero);
                foreach (var x in r) b.Encapsulate(x.bounds);
                // Depois do giro, o que vai pra perpendicular à rua é o eixo X (ou Z, se girado 90).
                o.meiaLargura = (Mathf.Abs(Mathf.Repeat(giro, 180f) - 90f) < 1f ? b.size.z : b.size.x) / 2f;
                go.name = "Modelo_" + id;
                go.SetActive(false);
                lista.Add(o);
                return o;
            }

            foreach (var m in Modelos3D)
                Marcar(P(pai, m.prefab, Vector3.zero), m.id, m.artigo, m.dif, m.giro, m.aceita);

            foreach (var s in Semaforos)
            {
                var poste = P(pai, "Props/Semaforo_Carros", Vector3.zero, 0f, 1.5f);
                var o = Marcar(poste, s.id, s.artigo, 2, 180f, s.aceita);
                var sinal = F.Grupo("Sinal", poste.transform).AddComponent<SinalDeTransito>();
                var luzes = poste.GetComponentsInChildren<Renderer>(true);
                sinal.luzesVermelhas = luzes.Where(r => r.name == "Luz_Vermelha").ToArray();
                sinal.luzesAmarelas = luzes.Where(r => r.name == "Luz_Amarela").ToArray();
                sinal.luzesVerdes = luzes.Where(r => r.name == "Luz_Verde").ToArray();
                sinal.materialVermelho = ArtePaleta.Mat("Luz_Vermelha");
                sinal.materialAmarelo = ArtePaleta.Mat("Luz_Amarela");
                sinal.materialVerde = ArtePaleta.Mat("Luz_Verde");
                sinal.materialApagado = ArtePaleta.Mat("Luz_Apagada");
                o.sinal = sinal;
                o.corDoSinal = s.cor;
            }

            var banco = VerbeteDatabase.Instancia;
            foreach (var d in Outdoors)
            {
                var placa = P(pai, "Props/Outdoor", Vector3.zero, 0f, 1.1f);
                var figura = new GameObject("Ilustracao", typeof(SpriteRenderer));
                figura.transform.SetParent(placa.transform, false);
                var sr = figura.GetComponent<SpriteRenderer>();
                sr.sprite = banco.Buscar(d.id).ilustracao;
                float lado = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                // A frente do outdoor é o lado -Z (é pra lá que saem os braços das lâmpadas).
                // A placa original (malha "b") tem uma barra de lâmpadas na frente, que cortaria a ilustração:
                // fica só o poste, com um cartaz branco que a ilustração ocupa quase inteiro.
                foreach (var malha in placa.GetComponentsInChildren<MeshRenderer>(true))
                    if (malha.name == "b") malha.gameObject.SetActive(false);
                F.Cubo(placa.transform, "Cartaz", "Branco", F.V(0f, 4.2f, -.06f), F.V(4.4f, 4.4f, .08f));
                figura.transform.localPosition = F.V(0f, 4.2f, -.12f);
                figura.transform.localScale = Vector3.one * (4.1f / lado);
                // Giro 40: a frente fica virada pra quem vem pela rua.
                Marcar(placa, d.id, d.artigo, d.dif, 40f, new string[0]);
            }
            return lista.ToArray();
        }

        // ------------------------------------------------------------------ garagem

        static GameObject Garagem(out Transform pose, out Transform carro, out Transform[] familia, out Transform porta)
        {
            var g = F.Grupo("Garagem", null, LugarDaGaragem).transform;
            SemSombra(F.Plano(g, "Gramado", "Verde_Claro", F.V(0, -.12f, 0), 200, 200));
            SemSombra(F.Caixa(g, "Piso", "Cinza_Claro", 0, -.1f, 0, 14f, .1f, 14f));
            SemSombra(F.Caixa(g, "Rua", "Asfalto", 0, -.1f, 16f, 90f, .1f, 8f));
            F.Caixa(g, "Parede_Fundo", "Areia", 0, 0, -7f, 14f, 5f, .3f);
            F.Caixa(g, "Parede_Esquerda", "Areia", -7f, 0, 0, .3f, 5f, 14f);
            F.Caixa(g, "Parede_Direita", "Areia", 7f, 0, 0, .3f, 5f, 14f);
            P(g, "Oficina/Prateleira", F.V(-6.2f, 0, -5f), 90);
            P(g, "Props/CaixaKK_A", F.V(5.8f, 0, -5.8f), 20);
            P(g, "Props/Caixa_Papelao", F.V(5f, 0, -6f), 60);
            P(g, "Natureza/Arvore_Comum_1", F.V(-14f, 0, 12f), 30);
            P(g, "Natureza/Arvore_Comum_2", F.V(14f, 0, 12f), 120);
            P(g, "Predios/Casa_02", F.V(0f, 0, 34f), 180);

            carro = P(g, "Carros/Carro_SUV", F.V(1.6f, .01f, -1f)).transform;
            carro.name = "Carro_Da_Familia";
            porta = F.Grupo("Porta_Do_Carro", carro, F.V(-.6f, 0, 0)).transform;

            string[] quem = { "Familia_FilhoMaisVelho_Animado", "Familia_Pai_Animado", "Familia_Mae_Animado", "Familia_Filha_Animado", "Familia_FilhoMaisNovo_Animado" };
            familia = new Transform[quem.Length];
            for (int i = 0; i < quem.Length; i++)
                familia[i] = P(g, "Personagens/" + quem[i], F.V(-.9f - i * 1.15f, 0f, -.4f + (i % 2) * .7f), 180).transform;

            pose = F.Grupo("Pose_Da_Camera", g, F.V(-2f, 3.6f, 10f)).transform;
            pose.LookAt(LugarDaGaragem + F.V(-.2f, 1f, -.6f));
            return g.gameObject;
        }

        // ------------------------------------------------------------------ regras (conferência com as definições)

        [MenuItem("Cidade Viva/Verbetes/Conferir regras do Passeio de Carro com as definições")]
        public static bool ConferirTrechos()
        {
            var config = AssetDatabase.LoadAssetAtPath<RuaVivaConfig>(CaminhoDaConfig);
            var banco = VerbeteDatabase.Instancia;
            var falhas = new List<string>();
            var lista = new StringBuilder();
            void Regra(string id, string trecho, string regra)
            {
                var v = banco.Buscar(id);
                if (v == null) { falhas.Add("verbete desconhecido: " + id); return; }
                if (!v.definicao.Contains(trecho)) falhas.Add($"{id}: o trecho \"{trecho}\" não está na definição");
                lista.AppendLine($"{v.palavra}: {regra} <= \"{trecho}\"");
            }
            lista.AppendLine("REGRAS DERIVADAS DAS DEFINIÇÕES");
            Regra("cinto_de_seguranca", config.trechoDaFivela, "prender é levar a fivela até o fecho");
            Regra("cinto_de_seguranca", config.trechoDeQuando, "o cinto é preso ao entrar no carro, antes do passeio");
            Regra("cinto_de_seguranca", "a gente deve prender", "todo mundo prende, inclusive o motorista");
            Regra("cinto_de_seguranca", config.trechoDaFreada, "na freada, o balão lembra pra que serve o cinto");
            Regra("passageiro", config.trechoDoPassageiro, "o jogador não dirige: o carro anda sozinho");
            Regra("passageiro", config.trechoDoMotorista, "o motorista está junto e é quem faz os pedidos");
            Regra("buzina", config.trechoDaBuzina, "buzinar serve pra alguém perceber: pessoa distraída ouve e para");
            Regra("buzina", "Botão do veículo", "a buzina é um botão, tocado uma vez");

            lista.AppendLine().AppendLine("O QUE NÃO VEM DIRETO DAS DEFINIÇÕES (escolhas do jogo, pra revisão)");
            foreach (string item in new[]
                     {
                         "O PASSAGEIRO APERTA A BUZINA. A definição diz \"quando alguém apertado\", sem dizer quem; na vida real quem buzina é o motorista. Foi pedido da mecânica. REVISAR com cuidado: pode ensinar à criança que o passageiro buzina.",
                         "\"O carro só sai com todos de cinto\": a definição diz que a gente deve prender ao entrar, não fala em o carro esperar.",
                         "Buzinar à toa ser errado e assustar o bichinho: a definição diz para que serve a buzina, não proíbe outros usos.",
                         "A hora certa de buzinar (pessoa vindo pra rua sem ter visto o carro) e que ela para ao ouvir.",
                         "Sem buzina o motorista freia e a pessoa passa: a definição não fala disso.",
                         "A pessoa atenta, que anda pela calçada, não precisar de buzina.",
                         "Crianças prendendo o cinto dos adultos e do motorista: ritual do jogo.",
                         "\"Automóvel\" aceitar ônibus, ambulância, bombeiro, viatura e micro-ônibus: vem da letra da definição (\"Veículo com motor, com quatro ou mais rodas\"), mas a criança talvez não pense assim. Motocicleta, bicicleta e reboque não contam.",
                         "Modelos substitutos: bonde = metrô; micro-ônibus = van; ciclista = bicicleta com menino parado em cima.",
                         "11 verbetes aparecem como outdoor com a ilustração do glossário, porque não há modelo que funcione sozinho na rua: via, via rural, via urbana, calçada, faixa de pedestre, ciclofaixa, retorno, passagem subterrânea, cinto de segurança, buzina e engarrafamento.",
                         "Passageiro fica fora do safari: não tem modelo nem ilustração.",
                         "Viaduto, passarela, túnel, ponte e ciclovia ficam parados no gramado, sem ligar nada: são modelos pra reconhecer, não mostram a função descrita na definição.",
                         "O cachorro no carro, os artigos \"um/uma\" e os textos curtos dos recados.",
                     })
                lista.AppendLine("  - " + item);

            // Catálogo do safari.
            lista.AppendLine().AppendLine("CATÁLOGO DO SAFARI");
            var ids = new List<string>();
            foreach (var m in Modelos3D) { ids.Add(m.id); lista.AppendLine($"  {m.id}: dificuldade {m.dif}, modelo {m.prefab}" + (m.aceita.Length > 0 ? ", também responde " + string.Join(", ", m.aceita) : "")); }
            foreach (var s in Semaforos) { ids.Add(s.id); lista.AppendLine($"  {s.id}: dificuldade 2, semáforo aceso em {s.cor}, também responde semaforo"); }
            foreach (var d in Outdoors) { ids.Add(d.id); lista.AppendLine($"  {d.id}: dificuldade {d.dif}, outdoor com a ilustração"); }
            foreach (string id in ids)
                if (banco.Buscar(id) == null) falhas.Add("catálogo com verbete desconhecido: " + id);
            foreach (var d in Outdoors)
                if (banco.Buscar(d.id)?.ilustracao == null) falhas.Add("outdoor sem ilustração: " + d.id);
            var fora = banco.Todos.Where(v => !ids.Contains(v.id) && v.id != "semaforo").Select(v => v.id).ToList();
            lista.AppendLine("Fora do safari como pedido próprio: " + string.Join(", ", fora) + " (\"semaforo\" é aceito nos três semáforos acesos, mas não é pedido sozinho).");

            lista.AppendLine().AppendLine(falhas.Count == 0 ? "CONFERÊNCIA: tudo certo." : "CONFERÊNCIA: " + falhas.Count + " falha(s):");
            foreach (string f in falhas) lista.AppendLine("  - " + f);
            File.WriteAllText("Temp/ruaviva_carro_regras.txt", lista.ToString(), Encoding.UTF8);
            if (falhas.Count == 0) Debug.Log("[Rua Viva] Regras conferidas. Lista em Temp/ruaviva_carro_regras.txt.");
            else Debug.LogError("[Rua Viva] Problemas:\n" + string.Join("\n", falhas));
            return falhas.Count == 0;
        }

        // ------------------------------------------------------------------ níveis e config

        static NivelConfig Nivel(string objetivo, float velocidade, int pedidos, int buzinas, int ocupantes, int atentas, int distracoes,
            int difMin, int difMax, float distancia, float janela, float tempoPorPedido, params string[] regras)
        {
            var n = new NivelConfig
            {
                objetivo = objetivo, velocidade = velocidade, quantidadeDeItens = pedidos, quantidadeDeObstaculos = buzinas,
                regrasExtras = new List<string>(regras),
                // As estrelas são calculadas pelo manager; isto só alimenta o texto da seleção.
                estrelas = new CriterioDeEstrelas { medida = MedidaDeEstrelas.Erros, para2Estrelas = 2, para3Estrelas = 0 },
            };
            void E(string nome, float valor) { n.extras.Add(new ParametroExtra { nome = nome, valor = valor }); }
            E("ocupantes", ocupantes); E("atentas", atentas); E("distracoes", distracoes);
            E("dificuldadeMinima", difMin); E("dificuldadeMaxima", difMax); E("distanciaMaxima", distancia);
            E("janelaDaBuzina", janela); E("tempoPorPedido", tempoPorPedido);
            return n;
        }

        static LevelConfig SalvarNiveis()
        {
            const string figura = RuaVivaCarroManager.RegraFigura, para = RuaVivaCarroManager.RegraCarroPara, devagar = RuaVivaCarroManager.RegraCarroDevagar,
                dica = RuaVivaCarroManager.RegraDicaBuzina, ciclistas = RuaVivaCarroManager.RegraCiclistas;
            ArtePaleta.GarantirPasta("Assets/Configs/Niveis");
            var asset = AssetDatabase.LoadAssetAtPath<LevelConfig>(CaminhoDosNiveis);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(asset, CaminhoDosNiveis);
            }
            asset.niveis = new List<NivelConfig>
            {
                Nivel("Prenda o cinto, olhe pela janela e buzine na hora!", 5f, 3, 1, 2, 0, 0, 1, 1, 9f, 5f, 0f, figura, para, devagar, dica),
                Nivel("Agora o motorista só diz o nome. Procure!", 5.5f, 4, 2, 3, 0, 2, 1, 2, 12f, 4.5f, 0f, devagar, dica),
                Nivel("Nem todo mundo precisa de buzina. Olhe bem!", 6.5f, 5, 3, 4, 1, 3, 1, 2, 18f, 4f, 0f, ciclistas),
                Nivel("Coisas pequenas e longe. Atenção!", 7f, 6, 4, 5, 2, 4, 2, 3, 26f, 3.5f, 0f, ciclistas),
                Nivel("Passeio longo e cada pedido com tempo!", 8f, 8, 5, 5, 3, 5, 2, 3, 32f, 3f, 7f, ciclistas),
            };
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static RuaVivaConfig PrepararConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<RuaVivaConfig>(CaminhoDaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<RuaVivaConfig>();
                AssetDatabase.CreateAsset(config, CaminhoDaConfig);
            }
            config.somDaFivela = TravessiaConstrutor.Som("Interface/confirmation_001.ogg");
            config.somDeSoltar = TravessiaConstrutor.Som("Interface/click_001.ogg");
            config.somDoMotor = TravessiaConstrutor.Som("Efeitos/motor_ligando.wav");
            config.somDaBuzina = TravessiaConstrutor.Som("Efeitos/buzina.wav");
            config.somDeAcerto = TravessiaConstrutor.Som("Interface/confirmation_002.ogg");
            config.somDeOutroObjeto = TravessiaConstrutor.Som("Interface/select_002.ogg");
            config.somDoSusto = TravessiaConstrutor.Som("Efeitos/tropeco.wav");
            config.somDaFreada = TravessiaConstrutor.Som("Interface/error_004.ogg");
            config.somDePedido = TravessiaConstrutor.Som("Interface/select_001.ogg");
            config.somDeComemoracao = TravessiaConstrutor.Som("Interface/confirmation_003.ogg");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        // ------------------------------------------------------------------ interface

        static RuaVivaHUD Hud(Canvas canvas)
        {
            var raiz = Tela("HUD", canvas.transform);
            var hud = raiz.gameObject.AddComponent<RuaVivaHUD>();
            var voltarAoHub = raiz.gameObject.AddComponent<VoltarAoHub>();

            // ---------------- moldura da janela do carro (só no passeio)
            var passeio = Tela("Passeio", raiz);
            hud.painelDoPasseio = passeio.gameObject;
            var teto = Imagem("Teto", passeio, null, C("Grafite"), Vector2.zero, new Vector2(0, 36));
            AncorarFaixa(teto, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -18));
            var porta = Imagem("Porta", passeio, null, C("Grafite"), Vector2.zero, new Vector2(0, 220));
            AncorarFaixa(porta, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 110));
            var frisoDaPorta = Imagem("Friso", porta.transform, null, C("Cinza_Escuro"), Vector2.zero, new Vector2(0, 14));
            AncorarFaixa(frisoDaPorta, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -7));
            foreach (var (nome, x, ancora) in new[] { ("Coluna_Esquerda", 18f, 0f), ("Coluna_Direita", -18f, 1f) })
            {
                var coluna = Imagem(nome, passeio, null, C("Grafite"), Vector2.zero, new Vector2(36, 0));
                var rt = coluna.rectTransform;
                rt.anchorMin = new Vector2(ancora, 0); rt.anchorMax = new Vector2(ancora, 1);
                rt.anchoredPosition = new Vector2(x, 0); rt.sizeDelta = new Vector2(36, 0);
            }

            // Pedido do motorista.
            var pedido = Imagem("Balao_Do_Pedido", passeio, arredondado, C("Branco", .96f), new Vector2(60, -105), new Vector2(980, 130));
            TravessiaConstrutor.Ancorar(pedido, new Vector2(.5f, 1));
            hud.balaoDoPedido = pedido.gameObject;
            hud.textoDoPedido = Texto("Texto", pedido.transform, "", 50, C("Petroleo"), new Vector2(-60, 0), new Vector2(660, 118));
            hud.textoDoPedido.resizeTextForBestFit = true; hud.textoDoPedido.resizeTextMinSize = 28; hud.textoDoPedido.resizeTextMaxSize = 50;
            hud.figuraDoPedido = Imagem("Figura", pedido.transform, null, Color.white, new Vector2(-420, 0), new Vector2(118, 118));
            var ouvir = Botao("Botao_Ouvir", pedido.transform, "Ouvir", "Petroleo_Claro", new Vector2(370, 0), new Vector2(190, 110), 34);
            hud.botaoOuvir = ouvir.gameObject;
            var tempo = Imagem("Painel_Do_Tempo", passeio, arredondado, C("Petroleo_Escuro", .9f), new Vector2(60, -190), new Vector2(720, 32));
            TravessiaConstrutor.Ancorar(tempo, new Vector2(.5f, 1));
            var area = Ret("Area", tempo.transform, Vector2.zero, Vector2.zero);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(5, 5); area.offsetMax = new Vector2(-5, -5);
            var cheio = Imagem("Cheio", area, arredondado, C("Mostarda"), Vector2.zero, Vector2.zero);
            cheio.rectTransform.anchorMin = Vector2.zero; cheio.rectTransform.anchorMax = Vector2.one;
            cheio.rectTransform.offsetMin = cheio.rectTransform.offsetMax = Vector2.zero;
            hud.painelDoTempo = tempo.gameObject;
            hud.barraDoTempo = cheio.rectTransform;

            // Buzina, no meio da porta.
            var buzina = Botao("Botao_Buzina", passeio, "", "Mostarda", new Vector2(0, 118), new Vector2(210, 210), 0, null, circulo);
            TravessiaConstrutor.Ancorar(buzina, new Vector2(.5f, 0));
            Imagem("Ilustracao", buzina.transform.Find("Frente") ?? buzina.transform, VerbeteDatabase.Instancia.Buscar("buzina").ilustracao, Color.white, Vector2.zero, new Vector2(150, 150));
            hud.botaoDaBuzina = (RectTransform)buzina.transform;
            var rotulo = Texto("Rotulo_Buzina", passeio, "Buzina", 40, C("Branco"), new Vector2(210, 60), new Vector2(200, 60));
            TravessiaConstrutor.Ancorar(rotulo, new Vector2(.5f, 0));
            var sinal = Texto("Sinal_Da_Buzina", passeio, "BIIII!", 72, C("Mostarda"), new Vector2(0, 280), new Vector2(420, 96));
            TravessiaConstrutor.Ancorar(sinal, new Vector2(.5f, 0));
            hud.sinalDaBuzina = sinal.gameObject;

            // Figurinha do acerto.
            var figurinha = Imagem("Figurinha", passeio, arredondado, C("Branco", .98f), new Vector2(-250, 40), new Vector2(380, 450));
            TravessiaConstrutor.Ancorar(figurinha, new Vector2(1, .5f));
            hud.figurinha = figurinha.gameObject;
            hud.imagemDaFigurinha = Imagem("Imagem", figurinha.transform, null, Color.white, new Vector2(0, 45), new Vector2(330, 330));
            hud.palavraDaFigurinha = Texto("Palavra", figurinha.transform, "", 44, C("Petroleo"), new Vector2(0, -175), new Vector2(350, 80));
            hud.palavraDaFigurinha.resizeTextForBestFit = true; hud.palavraDaFigurinha.resizeTextMinSize = 24; hud.palavraDaFigurinha.resizeTextMaxSize = 44;

            // Marca que aponta o objeto (nível com dica).
            hud.marca = Imagem("Marca", passeio, S("figura_estrela"), C("Mostarda"), Vector2.zero, new Vector2(120, 120)).rectTransform;

            // ---------------- garagem: a fivela e o fecho
            var garagem = Tela("Garagem", raiz);
            hud.painelDaGaragem = garagem.gameObject;
            var cartao = Imagem("Painel_Do_Cinto", garagem, arredondado, C("Branco", .95f), new Vector2(0, 165), new Vector2(1300, 290));
            TravessiaConstrutor.Ancorar(cartao, new Vector2(.5f, 0));
            hud.textoDoOcupante = Texto("Ocupante", cartao.transform, "", 44, C("Petroleo"), new Vector2(0, 100), new Vector2(1100, 64));
            var saida = Imagem("Saida_Do_Cinto", cartao.transform, arredondado, C("Grafite"), new Vector2(-540, -35), new Vector2(70, 120));
            var correia = Imagem("Correia", cartao.transform, null, C("Petroleo"), new Vector2(-540, -35), new Vector2(120, 34));
            correia.rectTransform.pivot = new Vector2(0, .5f);
            var fecho = Imagem("Fecho", cartao.transform, arredondado, C("Vermelho"), new Vector2(490, -35), new Vector2(190, 150));
            Texto("Texto", fecho.transform, "Fecho", 34, C("Branco"), Vector2.zero, new Vector2(170, 60));
            var fivelaImg = Imagem("Fivela", cartao.transform, arredondado, C("Mostarda"), new Vector2(-300, -35), new Vector2(190, 150), true);
            Texto("Texto", fivelaImg.transform, "Fivela", 34, C("Grafite"), Vector2.zero, new Vector2(170, 60));
            var fivela = fivelaImg.gameObject.AddComponent<FivelaArrastavel>();
            fivela.fecho = fecho.rectTransform;
            fivela.correia = correia.rectTransform;
            hud.fivela = fivela;

            // ---------------- sempre: nível e critérios, voltar, recado
            var painel = Imagem("Painel_Do_Nivel", raiz, arredondado, C("Branco", .92f), new Vector2(240, -135), new Vector2(430, 220));
            TravessiaConstrutor.Ancorar(painel, new Vector2(0, 1));
            hud.painelDoNivel = painel.gameObject;
            hud.textoDoNivel = Texto("Nivel", painel.transform, "", 30, C("Petroleo"), new Vector2(0, 80), new Vector2(400, 44));
            var estrelas = new Image[3];
            var rotulos = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                estrelas[i] = Imagem("Estrela_" + (i + 1), painel.transform, S("figura_estrela"), Color.white, new Vector2(-135 + i * 135, 20), new Vector2(66, 66));
                rotulos[i] = Texto("Rotulo_" + (i + 1), painel.transform, "", 22, C("Grafite"), new Vector2(-135 + i * 135, -35), new Vector2(135, 32), FontStyle.Normal);
            }
            hud.estrelasDosCriterios = estrelas;
            hud.rotulosDosCriterios = rotulos;
            hud.textoDoSafari = Texto("Safari", painel.transform, "", 28, C("Cinza_Escuro"), new Vector2(0, -80), new Vector2(400, 40), FontStyle.Normal);

            var hubBotao = Botao("Botao_VoltarAoHub", raiz, "", "Petroleo_Claro", new Vector2(-100, -95), new Vector2(120, 120), 0, S("ui_seta_voltar"), circulo);
            TravessiaConstrutor.Ancorar(hubBotao, new Vector2(1, 1));
            UnityEventTools.AddPersistentListener(hubBotao.onClick, voltarAoHub.Voltar);

            var recado = Imagem("Painel_Do_Recado", raiz, arredondado, C("Branco", .92f), new Vector2(0, 275), new Vector2(760, 76));
            TravessiaConstrutor.Ancorar(recado, new Vector2(.5f, 0));
            recado.rectTransform.anchoredPosition = new Vector2(520, 275);
            hud.painelDoRecado = recado.rectTransform;
            hud.textoDoRecado = Texto("Texto", recado.transform, "", 34, C("Petroleo"), Vector2.zero, new Vector2(730, 66));
            hud.textoDoRecado.resizeTextForBestFit = true; hud.textoDoRecado.resizeTextMinSize = 22; hud.textoDoRecado.resizeTextMaxSize = 34;

            var balao = Imagem("Balao", raiz, arredondado, C("Grafite"), new Vector2(0, 80), new Vector2(980, 300));
            Imagem("Papel", balao.transform, arredondado, C("Mostarda_Clara"), Vector2.zero, new Vector2(966, 286));
            hud.textoDoBalao = Texto("Texto", balao.transform, "", 40, C("Grafite"), Vector2.zero, new Vector2(920, 262), FontStyle.Normal);
            hud.textoDoBalao.resizeTextForBestFit = true; hud.textoDoBalao.resizeTextMinSize = 24; hud.textoDoBalao.resizeTextMaxSize = 40;
            hud.balao = balao.gameObject;
            balao.gameObject.SetActive(false);

            var preta = Imagem("Tela_Preta", raiz, null, new Color(0, 0, 0, 0), Vector2.zero, Vector2.zero);
            preta.rectTransform.anchorMin = Vector2.zero; preta.rectTransform.anchorMax = Vector2.one;
            preta.rectTransform.offsetMin = preta.rectTransform.offsetMax = Vector2.zero;
            hud.telaPreta = preta;
            preta.gameObject.SetActive(false);

            hud.corGanha = Color.white;
            hud.corPerdida = new Color(.4f, .45f, .5f, .3f);
            return hud;
        }

        static void AncorarFaixa(Graphic g, Vector2 min, Vector2 max, Vector2 pos)
        {
            var rt = g.rectTransform;
            float altura = rt.sizeDelta.y;
            rt.anchorMin = min; rt.anchorMax = max;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(0, altura);
        }
    }
}
