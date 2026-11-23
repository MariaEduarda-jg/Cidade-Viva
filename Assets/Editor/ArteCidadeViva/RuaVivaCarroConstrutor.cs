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
    // Monta do zero a Rua Viva, parte 1 (Passeio de Carro): config (com as 5 missões), níveis e a cena
    // Minigame_RuaViva_Carro. Verbetes da mecânica: cinto de segurança, passageiro e buzina.
    // Refeito em 2026-11-04: missões depois da enchente, cinto de perto dentro do carro, buzina no volante,
    // safari ligado aos lugares do percurso.
    public static class RuaVivaCarroConstrutor
    {
        const string CaminhoDaCena = "Assets/Scenes/Minigame_RuaViva_Carro.unity";
        const string CaminhoDaConfig = "Assets/Configs/RuaVivaCarro_Config.asset";
        const string CaminhoDosNiveis = "Assets/Configs/Niveis/Niveis_RuaVivaCarro.asset";
        const string CaminhoDoRetrovisor = "Assets/Materials/Retrovisor.mat";
        const string PastaCartoonCity = "Assets/Art/Ruas/Modelos/CartoonCityFree/Prefabs/";
        public const string NomeDaCena = "Minigame_RuaViva_Carro";
        public const string NomeDaArea = "Rua Viva";
        const int CamadaDoJogador = 30;

        const float RaioDaCurva = 28f, LadoDaPista = 1.8f, PassoDaRota = .25f, Prolongamento = 300f, Reta = 220f;
        const int Retas = 5;
        static readonly Vector3 LugarDaCasa = new Vector3(600f, 0f, 0f);

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

        // Prefab do pacote Cartoon City Free (ithappy).
        static GameObject CC(Transform pai, string prefab, Vector3 pos, float rotY = 0f, float escala = 1f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PastaCartoonCity + prefab + ".prefab");
            if (asset == null) { Debug.LogError("[Rua Viva] Prefab do Cartoon City não encontrado: " + prefab); return new GameObject("FALTOU_" + prefab); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, pai);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(0, rotY, 0);
            go.transform.localScale = Vector3.one * escala;
            return go;
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
            cam.fieldOfView = 62f;
            cam.nearClipPlane = .05f;
            cam.farClipPlane = 600f;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AmbienteConstrutor.CaminhoPrefab) == null) AmbienteConstrutor.CriarPrefab();
            AmbienteConstrutor.AplicarNaCenaAberta(true);

            var percurso = Percurso();
            Casa(out var lugarDoCarro, out var poseDaCasa);
            var carro = Carro();
            var pessoas = Pessoas();
            var modelos = percurso.transform.Find("Modelos");
            var ursinho = Solto(modelos, "Enchente/Ursinho", .36f);
            var caixa = Solto(modelos, "Enchente/Caixa_Fechada", .62f);

            var efeitos = F.Grupo("Efeitos").transform;
            var estrelinhas = TravessiaConstrutor.Particulas(efeitos, "Estrelinhas", 14, .7f, 2.5f, .12f, .4f, "Mostarda", "Mostarda_Clara", "Branco");
            var confete = TravessiaConstrutor.Particulas(efeitos, "Confete", 70, 1.6f, 6.5f, .3f, 2.5f, "Mostarda", "Vermelho", "Petroleo_Claro", "Verde", "Rosa", "Laranja");
            var susto = TravessiaConstrutor.Particulas(efeitos, "Susto", 10, .5f, 1.2f, .04f, .2f, "Mostarda", "Branco");

            var canvas = NovoCanvas("Canvas");
            NovoEventSystem(false);
            var hud = Hud(canvas, out var botaoMissao, out var botaoOuvirMissao);
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
            gerente.carro = carro;
            gerente.lugarDoCarroEmCasa = lugarDoCarro;
            gerente.poseDaCasa = poseDaCasa;
            gerente.pessoas = pessoas;
            gerente.modeloDeUrsinho = ursinho;
            gerente.modeloDeCaixa = caixa;
            gerente.camadaDoJogador = CamadaDoJogador;
            gerente.estrelinhas = estrelinhas;
            gerente.confete = confete;
            gerente.susto = susto;
            cam.transform.SetPositionAndRotation(poseDaCasa.position, poseDaCasa.rotation);

            UnityEventTools.AddPersistentListener(hud.botaoOuvir.GetComponent<Button>().onClick, gerente.OuvirPedido);
            UnityEventTools.AddPersistentListener(botaoMissao.onClick, gerente.ComecarMissao);
            UnityEventTools.AddPersistentListener(botaoOuvirMissao.onClick, gerente.OuvirMissao);
            // Abas "Passeio de Carro" / "Pedalada" (a parte 2 da Rua Viva fica em outra cena).
            RuaVivaPedaladaConstrutor.AbasDaRuaViva(canvas.transform, gerente, 0);

            ConferirTrechos(percurso.lugares);
            OtimizacaoWebGL.TirarSombraDosPequenos();
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
            string[] predios = { "Buildings/Eco_Building_Terrace", "Buildings/Regular_Building_TwistedTower_Large", "Buildings/Eco_Building_Grid", "Buildings/Eco_Building_Slope" };

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

                // À direita ficam os lugares da missão (até uns 40 m da rua); o bairro fica atrás deles.
                // À esquerda, casas perto e os prédios do Cartoon City ao fundo.
                for (float d = 8f; d < Reta - 8f; d += 16f, n++)
                {
                    Vector3 onde = comeco + Frente(giro) * d;
                    P(bairro, "Predios/" + casas[n % casas.Length], onde + Direita(giro) * 58f, giro - 90);
                    P(bairro, "Natureza/" + arvores[n % arvores.Length], onde + Direita(giro) * (50f + n % 3), n * 47, .9f);
                    P(bairro, "Predios/" + casas[(n + 3) % casas.Length], onde + Direita(giro) * -12f, giro + 90);
                    if (n % 3 == 0) P(bairro, "Props/Poste_Reto", onde + Direita(giro) * -5.6f, giro + 90);
                    if (n % 4 == 1) CC(bairro, predios[(n / 4) % predios.Length], onde + Direita(giro) * -38f, giro + 90, .4f);
                    if (n % 4 == 3) CC(bairro, "Vegetation/Bush_06", onde + Direita(giro) * -6.4f, n * 31f);
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
            percurso.lugares = Lugares(modelos);
            percurso.modelosDePedestre = new[] { "Pedestre_Mulher", "Idoso", "Trabalhador", "Trabalhadora" }.Select(x => Inativo(P(modelos, "Personagens/" + x, Vector3.zero))).ToArray();
            var transito = new List<GameObject>();
            foreach (var c in new[] { "Cars/Car_06", "Cars/Car_13", "Cars/Car_16", "Cars/Car_19", "Cars/Van" }) transito.Add(Inativo(CC(modelos, c, Vector3.zero)));
            foreach (var c in new[] { "Carros/Carro_Taxi", "Carros/Carro_SUV_Branco", "Carros/Carro_Hatch" }) transito.Add(Inativo(P(modelos, c, Vector3.zero)));
            percurso.modelosDeTransito = transito.ToArray();
            var bola = F.Grupo("Bola", modelos).transform;
            F.Esfera(bola, "Bola_Vermelha", "Vermelho", Vector3.zero, Vector3.one * .3f);
            F.Esfera(bola, "Faixa", "Branco", Vector3.zero, F.V(.305f, .08f, .305f));
            percurso.modeloDaBola = Inativo(bola.gameObject);
            percurso.colocados = F.Grupo("Colocados", mundo).transform;
            return percurso;
        }

        static GameObject Inativo(GameObject go)
        {
            go.name = "Modelo_" + go.name;
            go.SetActive(false);
            return go;
        }

        // Uma coisa solta que vai no banco (ursinho, caixa): toca-se nela pra guardar no porta-malas.
        static GameObject Solto(Transform pai, string prefab, float escala)
        {
            var go = P(pai, prefab, Vector3.zero, 0f, escala);
            var b = Limites(go);
            var area = go.AddComponent<BoxCollider>();
            area.center = go.transform.InverseTransformPoint(b.center);
            area.size = Vector3.Max(b.size / escala, Vector3.one * (.3f / escala));
            return Inativo(go);
        }

        static Bounds Limites(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = new Bounds(go.transform.position, Vector3.zero);
            bool primeiro = true;
            foreach (var r in rs)
            {
                if (primeiro) { b = r.bounds; primeiro = false; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }

        // ------------------------------------------------------------------ lugares do percurso
        // Espaço do lugar: x = à direita da faixa do carro (a calçada vai de 1,8 a 3,8 m), z = adiante.

        static LugarDoPasseio NovoLugar(Transform pai, string id, string nome, string fala, float comprimento, float parada, Vector3 chegada)
        {
            var go = F.Grupo("Lugar_" + id, pai);
            var l = go.AddComponent<LugarDoPasseio>();
            l.id = id;
            l.nome = nome;
            l.fala = fala;
            l.comprimento = comprimento;
            l.parada = parada;
            l.chegada = F.Grupo("Chegada", go.transform, chegada).transform;
            return l;
        }

        static void Fechar(LugarDoPasseio l)
        {
            l.objetos = l.GetComponentsInChildren<ObjetoDoSafari>(true);
            l.gameObject.SetActive(false);
        }

        // Um objeto do safari: o modelo vem sem giro, ganha a área de toque e só então gira.
        static ObjetoDoSafari Obj(Transform pai, string id, string artigo, string prefab, Vector3 pos, float rotY, float escala = 1f, params string[] aceita)
        {
            return Marcar(P(pai, prefab, pos, 0f, escala), id, artigo, rotY, aceita);
        }

        static ObjetoDoSafari Marcar(GameObject go, string id, string artigo, float rotY, params string[] aceita)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(go)) PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var obra = go.GetComponent<ObraEstrutura>();
            if (obra != null) Object.DestroyImmediate(obra);
            var caminho = go.transform.Find("Caminho");
            if (caminho != null) Object.DestroyImmediate(caminho.gameObject);
            var o = go.AddComponent<ObjetoDoSafari>();
            o.idDoVerbete = id;
            o.artigo = artigo;
            o.aceita = aceita;
            o.dificuldade = 1;
            Quaternion giro = go.transform.localRotation;
            go.transform.localRotation = Quaternion.identity;
            o.Preparar(1.2f, 1.4f);
            go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f) * giro;
            go.name = "Safari_" + id;
            return o;
        }

        static void Placa(Transform lugar, string texto, float z)
        {
            var placa = F.Grupo("Placa", lugar, F.V(4.6f, 0f, z), F.V(0, 40f, 0)).transform;
            F.Barra(placa, "Poste_1", "Cinza_Escuro", F.V(-1.3f, 0, 0), F.V(-1.3f, 3.1f, 0), .1f);
            F.Barra(placa, "Poste_2", "Cinza_Escuro", F.V(1.3f, 0, 0), F.V(1.3f, 3.1f, 0), .1f);
            F.Cubo(placa, "Quadro", "Petroleo", F.V(0, 2.7f, 0), F.V(3f, 1f, .08f));
            var go = new GameObject("Texto_Da_Placa", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(placa, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(580, 190);
            rt.localPosition = F.V(0, 2.7f, -.05f);
            rt.localScale = Vector3.one * .005f;
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var t = Texto("Texto", go.transform, texto, 84, C("Branco"), Vector2.zero, new Vector2(560, 180));
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 40; t.resizeTextMaxSize = 84;
        }

        static SinalDeTransito Sinal(GameObject poste)
        {
            var sinal = F.Grupo("Sinal", poste.transform).AddComponent<SinalDeTransito>();
            var luzes = poste.GetComponentsInChildren<Renderer>(true);
            sinal.luzesVermelhas = luzes.Where(r => r.name == "Luz_Vermelha").ToArray();
            sinal.luzesAmarelas = luzes.Where(r => r.name == "Luz_Amarela").ToArray();
            sinal.luzesVerdes = luzes.Where(r => r.name == "Luz_Verde").ToArray();
            sinal.materialVermelho = ArtePaleta.Mat("Luz_Vermelha");
            sinal.materialAmarelo = ArtePaleta.Mat("Luz_Amarela");
            sinal.materialVerde = ArtePaleta.Mat("Luz_Verde");
            sinal.materialApagado = ArtePaleta.Mat("Luz_Apagada");
            return sinal;
        }

        static string[] Auto => new[] { "automovel" };

        static LugarDoPasseio[] Lugares(Transform pai)
        {
            var lista = new List<LugarDoPasseio>();
            LugarDoPasseio l;
            Transform t;

            // Bairro: veículos parados na frente das casas.
            l = NovoLugar(pai, "bairro", "Bairro", "Olha a rua do bairro!", 45f, 22f, F.V(8, 0, 22));
            t = l.transform;
            P(t, "Predios/Casa_01", F.V(15, 0, 8), -90);
            P(t, "Predios/Casa_05", F.V(15, 0, 32), -90);
            P(t, "Natureza/Arvore_Comum_2", F.V(9, 0, 2), 30);
            CC(t, "Vegetation/Bush_07", F.V(8.5f, 0, 18), 0);
            CC(t, "Props/Trash_Can_08", F.V(4.6f, 0, 19), -90);
            Obj(t, "automovel", "um", "Carros/Carro_Sedan", F.V(6.4f, 0, 12), 0f);
            Obj(t, "motocicleta", "uma", "Carros/Moto", F.V(5.6f, 0, 22), 0f);
            Obj(t, "motoneta", "uma", "Oficina/Pronto_Motoneta", F.V(5.6f, 0, 26), 0f);
            Obj(t, "bicicleta_eletrica", "uma", "Oficina/Pronto_Bicicleta_Eletrica", F.V(5.4f, 0, 31), 0f, 1f, "bicicleta");
            Fechar(l); lista.Add(l);

            // Parque: a ciclovia com o ciclista, a bicicleta perto do banco, a fonte.
            l = NovoLugar(pai, "parque", "Parque", "Olha o parque!", 45f, 30f, F.V(9, 0, 30));
            t = l.transform;
            SemSombra(F.Plano(t, "Gramado_Do_Parque", "Verde", F.V(24, -.06f, 22), 40, 46));
            P(t, "Natureza/Arvore_Comum_1", F.V(20, 0, 6), 10);
            P(t, "Natureza/Arvore_Betula", F.V(30, 0, 38), 70);
            P(t, "Natureza/Arvore_Comum_3", F.V(36, 0, 16), 150);
            P(t, "Natureza/Flores", F.V(15, 0, 12), 0);
            CC(t, "Props/Fountain_03", F.V(24, 0, 24), 0, .9f);
            P(t, "Props/Banco_De_Praca", F.V(13.5f, 0, 26), -90);
            Placa(t, "Parque", 3f);
            Obj(t, "ciclovia", "uma", "Obras/Estrutura_ciclovia", F.V(7.6f, 0, 22), 0f);
            Obj(t, "ciclista", "um", "Obras/Ciclista", F.V(7.6f, .05f, 16), 0f, 1f, "bicicleta");
            Obj(t, "bicicleta", "uma", "Carros/Bicicleta_Azul", F.V(12, 0, 30), 90f);
            Fechar(l); lista.Add(l);

            // Rodoviária: o ponto, o ônibus e o micro-ônibus.
            l = NovoLugar(pai, "rodoviaria", "Rodoviária", "Chegando na rodoviária!", 50f, 30f, F.V(5.2f, 0, 30));
            t = l.transform;
            SemSombra(F.Caixa(t, "Patio", "Cinza_Claro", 18, -.06f, 25, 28, .08f, 46));
            CC(t, "Props/Bus_Stop_02", F.V(5.6f, 0, 20), -90);
            CC(t, "Buildings/Eco_Building_Terrace", F.V(30, 0, 25), -90, .45f);
            Placa(t, "Rodoviária", 3f);
            Obj(t, "onibus", "um", "Carros/Onibus_Urbano", F.V(9, 0, 10), 0f, 1f, Auto);
            Obj(t, "micro_onibus", "um", "Carros/Carro_Van", F.V(9, 0, 38), 0f, 1f, Auto);
            Fechar(l); lista.Add(l);

            // Cruzamento: a rua de lado, a faixa, o semáforo dos carros (vermelho quando o carro chega) e o de pedestre.
            l = NovoLugar(pai, "cruzamento", "Cruzamento", "Olha o cruzamento!", 45f, 22f, F.V(8, 0, 22));
            t = l.transform;
            SemSombra(F.Caixa(t, "Rua_De_Lado", "Asfalto", -1.8f, -.085f, 21, 90, .1f, 8));
            SemSombra(F.Caixa(t, "Linha_De_Parada", "Branco", 0, .012f, 14.2f, 3.6f, .01f, .35f));
            for (int i = 0; i < 8; i++) SemSombra(F.Caixa(t, "Faixa", "Branco", -5f + i * .9f, .012f, 15.6f, .45f, .01f, 1.8f));
            CC(t, "Buildings/Regular_Building_TwistedTower_Large", F.V(24, 0, 5), -90, .35f);
            CC(t, "Buildings/Eco_Building_Grid", F.V(24, 0, 38), -90, .35f);
            CC(t, "Props/Trash_Can_06", F.V(4.6f, 0, 10), -90);
            var poste = P(t, "Props/Semaforo_Carros", F.V(2.9f, 0, 15.4f), 0f, 1.3f);
            var sinal = Sinal(poste);
            Marcar(poste, "semaforo", "um", 180f);
            l.sinal = sinal;
            l.linha = 14.2f;
            Obj(t, "semaforo_de_pedestre", "um", "Props/Semaforo_Pedestres", F.V(2.8f, 0, 17.6f), 180f, 1f, "semaforo");
            Fechar(l); lista.Add(l);

            // Escola: a faixa de pedestre na frente, o prédio, quem espera pra atravessar.
            l = NovoLugar(pai, "escola", "Escola", "Chegando na escola!", 45f, 30f, F.V(7.5f, 0, 31));
            t = l.transform;
            P(t, "Predios/Escola", F.V(17, 0, 30), -90);
            SemSombra(F.Caixa(t, "Patio", "Areia", 12, -.06f, 30, 14, .08f, 22));
            Placa(t, "Escola", 3f);
            var faixa = F.Grupo("Faixa_De_Pedestre", t, F.V(-1.8f, 0, 16)).transform;
            for (int i = 0; i < 8; i++) SemSombra(F.Caixa(faixa, "Listra", "Branco", -3.15f + i * .9f, .012f, 0, .5f, .01f, 3f));
            Marcar(faixa.gameObject, "faixa_de_pedestre", "uma", 0f);
            P(t, "Personagens/Pedestre_Mulher", F.V(4.3f, 0, 15.4f), -90);
            P(t, "Natureza/Arvore_Comum_2", F.V(9, 0, 6), 80);
            Fechar(l); lista.Add(l);

            // Quartel dos bombeiros.
            l = NovoLugar(pai, "bombeiros", "Bombeiros", "Olha o quartel dos bombeiros!", 40f, 20f, F.V(8, 0, 20));
            t = l.transform;
            P(t, "Predios/Comercio_C", F.V(15, 0, 20), -90);
            P(t, "Props/Hidrante_Vermelho", F.V(4.6f, 0, 12), 0);
            Placa(t, "Bombeiros", 3f);
            Obj(t, "caminhao_de_bombeiro", "um", "Carros/Caminhao_Bombeiros", F.V(7.6f, 0, 20), 0f, 1f, Auto);
            Fechar(l); lista.Add(l);

            // Hospital: a ambulância na frente.
            l = NovoLugar(pai, "hospital", "Hospital", "Chegando no hospital!", 45f, 28f, F.V(8, 0, 30));
            t = l.transform;
            P(t, "Obras/Hospital", F.V(22, 0, 20), -90);
            P(t, "Personagens/Medica", F.V(6.5f, 0, 32), -90);
            Placa(t, "Hospital", 3f);
            Obj(t, "ambulancia", "uma", "Carros/Ambulancia", F.V(7.6f, 0, 16), 0f, 1f, Auto);
            Fechar(l); lista.Add(l);

            // Delegacia: a viatura na frente.
            l = NovoLugar(pai, "delegacia", "Delegacia", "Olha a delegacia!", 40f, 20f, F.V(8, 0, 20));
            t = l.transform;
            P(t, "Predios/Comercio_F", F.V(15, 0, 20), -90);
            Placa(t, "Delegacia", 3f);
            Obj(t, "viatura", "uma", "Carros/Viatura_De_Policia", F.V(7, 0, 20), 0f, 1f, Auto);
            Fechar(l); lista.Add(l);

            // Lago Paranoá com a ponte.
            l = NovoLugar(pai, "lago", "Lago Paranoá", "Olha o Lago Paranoá!", 55f, 27f, F.V(6, 0, 27));
            t = l.transform;
            SemSombra(F.Plano(t, "Agua", "Agua", F.V(28, -.07f, 27), 40, 50));
            SemSombra(F.Plano(t, "Areia", "Areia", F.V(6.5f, -.08f, 27), 4, 50));
            P(t, "Natureza/Arvore_Salgueiro", F.V(6, 0, 4), 0);
            P(t, "Natureza/Palmeira_1", F.V(6, 0, 50), 60);
            Placa(t, "Lago Paranoá", 3f);
            Obj(t, "ponte", "uma", "Obras/Ponte_JK", F.V(26, 0, 27), 90f, 1.6f);
            Fechar(l); lista.Add(l);

            // Obra da reconstrução: entulho da enchente, cones, o caminhão trator e o reboque.
            l = NovoLugar(pai, "obra", "Obra da reconstrução", "Olha a obra da reconstrução!", 45f, 22f, F.V(8, 0, 22));
            t = l.transform;
            SemSombra(F.Caixa(t, "Terra", "Marrom_Claro", 16, -.06f, 22, 22, .08f, 40));
            P(t, "Props/Barreira_Obra", F.V(4.4f, 0, 6), 0);
            P(t, "Props/Barreira_Obra", F.V(4.4f, 0, 36), 0);
            P(t, "Props/Cone", F.V(4.6f, 0, 9), 0);
            P(t, "Props/Cone", F.V(4.6f, 0, 33), 0);
            P(t, "Enchente/Entulho_De_Concreto", F.V(18, 0, 30), 20);
            P(t, "Personagens/Trabalhador", F.V(12, 0, 18), -90);
            Placa(t, "Obra", 3f);
            Obj(t, "caminhao_trator", "um", "Oficina/Pronto_Caminhao_Trator", F.V(8, 0, 14), 0f, 1.3f);
            Obj(t, "reboque", "um", "Oficina/Pronto_Reboque", F.V(8, 0, 25), 0f, 1.3f);
            Fechar(l); lista.Add(l);

            // Festa da reconstrução: bandeirinhas, o estacionamento cheio, a mãe esperando.
            l = NovoLugar(pai, "festa", "Festa da reconstrução", "Chegamos na festa!", 50f, 24f, F.V(6, 0, 18));
            t = l.transform;
            SemSombra(F.Caixa(t, "Praca", "Areia", 20, -.06f, 22, 30, .08f, 40));
            string[] cores = { "Vermelho", "Mostarda", "Petroleo_Claro", "Verde", "Rosa", "Laranja" };
            for (int fio = 0; fio < 3; fio++)
            {
                float z = 4f + fio * 6f;
                F.Barra(t, "Mastro", "Branco", F.V(8, 0, z), F.V(8, 4f, z), .08f);
                F.Barra(t, "Mastro", "Branco", F.V(30, 0, z), F.V(30, 4f, z), .08f);
                for (int b = 0; b < 16; b++)
                {
                    float x = 8.6f + b * 1.35f;
                    float y = 3.9f - Mathf.Sin((b + .5f) / 16f * Mathf.PI) * .8f;
                    F.Cubo(t, "Bandeirinha", cores[(b + fio) % cores.Length], F.V(x, y - .2f, z), F.V(.5f, .45f, .02f), F.V(0, 0, 45));
                }
            }
            CC(t, "Props/Fountain_03", F.V(24, 0, 12), 0, .8f);
            CC(t, "Billboards/Billboard_4x1_03", F.V(33, 0, 22), -90, .8f);
            Placa(t, "Festa!", 3f);
            var estacionamento = Obj(t, "estacionamento", "um", "Obras/Estrutura_estacionamento", F.V(11, 0, 30), 90f);
            foreach (var (carroParado, z) in new[] { ("Carros/Carro_Hatch", 26f), ("Carros/Carro_SUV_Branco", 32f) })
                P(estacionamento.transform, carroParado, estacionamento.transform.InverseTransformPoint(t.TransformPoint(F.V(11, 0, z))), 90);
            Fechar(l); lista.Add(l);

            // Casas de quem entra no caminho.
            foreach (var (id, nome, casa) in new[] { ("casa_amiga", "Casa da amiga", "Casa_03"), ("casa_vovo", "Casa da vovó", "Casa_07") })
            {
                l = NovoLugar(pai, id, nome, "", 40f, 20f, F.V(6, 0, 20));
                t = l.transform;
                P(t, "Predios/" + casa, F.V(13, 0, 20), -90);
                SemSombra(F.Caixa(t, "Caminho", "Cinza_Claro", 7.5f, -.06f, 20, 7, .08f, 1.6f));
                P(t, "Natureza/Flores", F.V(8, 0, 16), 0);
                P(t, "Natureza/Flores", F.V(8, 0, 24), 90);
                P(t, "Props/Cerca_Pequena", F.V(4.6f, 0, 14), 90);
                Placa(t, nome, 3f);
                Fechar(l); lista.Add(l);
            }
            return lista.ToArray();
        }

        // ------------------------------------------------------------------ casa (onde a missão começa)

        static void Casa(out Transform lugarDoCarro, out Transform pose)
        {
            var g = F.Grupo("Casa", null, LugarDaCasa).transform;
            SemSombra(F.Plano(g, "Gramado", "Verde_Claro", F.V(0, -.12f, 0), 200, 200));
            SemSombra(F.Caixa(g, "Entrada", "Cinza_Claro", 0, -.1f, 1f, 4.4f, .1f, 15f));
            SemSombra(F.Caixa(g, "Rua", "Asfalto", 0, -.1f, 12f, 90f, .1f, 7f));
            P(g, "Predios/Casa_02", F.V(0, 0, -11f), 0);
            P(g, "Natureza/Arvore_Comum_1", F.V(-9f, 0, -2f), 30);
            P(g, "Natureza/Arvore_Comum_2", F.V(9f, 0, -4f), 120);
            P(g, "Natureza/Flores", F.V(-3.5f, 0, 3f), 0);
            P(g, "Natureza/Flores", F.V(3.5f, 0, 5f), 90);
            P(g, "Props/Cerca_Pequena", F.V(-5f, 0, 7.5f), 0);
            P(g, "Props/Cerca_Pequena", F.V(5f, 0, 7.5f), 0);
            CC(g, "Vegetation/Bush_10", F.V(-3.6f, 0, -4f), 0);
            lugarDoCarro = F.Grupo("Lugar_Do_Carro", g, F.V(0, 0, 0)).transform;
            pose = F.Grupo("Pose_Da_Casa", g, F.V(5.6f, 2.3f, 7f)).transform;
            pose.LookAt(LugarDaCasa + F.V(0, .8f, -.3f));
        }

        // ------------------------------------------------------------------ o carro da família

        static readonly Vector3[] Assentos =
        {
            F.V(-.42f, .55f, .22f), F.V(.42f, .55f, .22f), F.V(-.45f, .55f, -.72f), F.V(0f, .55f, -.72f), F.V(.45f, .55f, -.72f),
        };

        static Transform Pose(Transform pai, string nome, Vector3 pos, Vector3 alvo)
        {
            var g = F.Grupo(nome, pai, pos).transform;
            g.localRotation = Quaternion.LookRotation(alvo - pos);
            return g;
        }

        static CarroDaFamilia Carro()
        {
            var raiz = F.Grupo("Carro_Da_Familia").transform;
            var c = raiz.gameObject.AddComponent<CarroDaFamilia>();
            const string cor = "Vermelho";
            // Luz suave dentro da cabine: com o teto, os bancos e o cinto ficavam escuros de perto.
            var luz = new GameObject("Luz_Da_Cabine").AddComponent<Light>();
            luz.transform.SetParent(raiz, false);
            luz.transform.localPosition = new Vector3(0, 1.35f, -.3f);
            luz.type = LightType.Point;
            luz.range = 3.2f;
            luz.intensity = 1.4f;
            luz.color = new Color(1f, .95f, .86f);
            luz.shadows = LightShadows.None;

            // Por fora: formas da paleta (não há modelo de carro com interior).
            var fora = F.Grupo("Carroceria", raiz).transform;
            F.Caixa(fora, "Assoalho", "Grafite", 0, .22f, 0, 1.72f, .08f, 3.9f);
            foreach (float x in new[] { -.88f, .88f })
            {
                F.Caixa(fora, "Porta", cor, x, .22f, 0, .07f, .74f, 3.9f);
                F.Caixa(fora, "Friso", "Cinza_Claro", x * 1.01f, .6f, 0, .02f, .04f, 3.6f);
                F.Barra(fora, "Coluna_A", cor, F.V(x * .97f, .95f, 1.24f), F.V(x * .9f, 1.58f, .72f), .07f);
                F.Caixa(fora, "Coluna_B", cor, x * .96f, .95f, -.18f, .07f, .64f, .1f);
                F.Barra(fora, "Coluna_C", cor, F.V(x * .97f, .95f, -1.3f), F.V(x * .9f, 1.58f, -.98f), .09f);
                F.Esfera(fora, "Farol", "Mostarda_Clara", F.V(x * .68f, .74f, 1.99f), F.V(.24f, .16f, .08f));
                F.Esfera(fora, "Lanterna", "Vermelho", F.V(x * .7f, .78f, -1.99f), F.V(.22f, .12f, .06f));
            }
            F.Caixa(fora, "Capo", cor, 0, .3f, 1.62f, 1.76f, .64f, .72f);
            F.Caixa(fora, "Para_Choque_Frente", "Cinza_Escuro", 0, .22f, 2.0f, 1.8f, .34f, .14f);
            F.Caixa(fora, "Porta_Malas", cor, 0, .3f, -1.62f, 1.76f, .7f, .72f);
            F.Caixa(fora, "Para_Choque_Tras", "Cinza_Escuro", 0, .22f, -2.0f, 1.8f, .34f, .14f);
            F.Caixa(fora, "Parede_Do_Painel", "Grafite", 0, .3f, 1.22f, 1.7f, .55f, .1f);
            F.Caixa(fora, "Teto", cor, 0, 1.57f, -.13f, 1.7f, .06f, 1.76f);
            F.Caixa(fora, "Forro", "Areia", 0, 1.55f, -.13f, 1.6f, .02f, 1.7f);
            F.Caixa(fora, "Travessa_Do_Para_Brisa", cor, 0, 1.52f, .74f, 1.62f, .06f, .06f);
            F.Caixa(fora, "Prateleira", "Grafite", 0, .96f, -1.18f, 1.6f, .04f, .26f);
            var rodas = new List<Transform>();
            foreach (float x in new[] { -.82f, .82f })
                foreach (float z in new[] { -1.3f, 1.3f })
                {
                    var roda = F.Grupo("Roda", fora, F.V(x, .33f, z)).transform;
                    F.Cilindro(roda, "Pneu", "Grafite", Vector3.zero, F.V(.66f, .24f, .66f), F.V(0, 0, 90));
                    F.Cilindro(roda, "Calota", "Cinza_Claro", Vector3.zero, F.V(.34f, .25f, .34f), F.V(0, 0, 90));
                    rodas.Add(roda);
                }
            c.rodas = rodas.ToArray();

            // Por dentro: bancos, painel, câmbio, volante, rádio, chave e retrovisor.
            var dentro = F.Grupo("Interior", raiz).transform;
            foreach (float x in new[] { -.42f, .42f })
            {
                F.Caixa(dentro, "Banco_Frente_Assento", "Petroleo", x, .32f, .38f, .5f, .18f, .5f);
                F.Cubo(dentro, "Banco_Frente_Encosto", "Petroleo", F.V(x, .76f, .1f), F.V(.5f, .54f, .12f), F.V(-8, 0, 0));
                F.Cubo(dentro, "Banco_Frente_Cabeca", "Petroleo_Escuro", F.V(x, 1.11f, .07f), F.V(.22f, .13f, .08f));
            }
            F.Caixa(dentro, "Banco_Tras_Assento", "Petroleo", 0, .32f, -.62f, 1.52f, .18f, .52f);
            F.Cubo(dentro, "Banco_Tras_Encosto", "Petroleo", F.V(0, .8f, -.93f), F.V(1.52f, .62f, .12f), F.V(-8, 0, 0));
            foreach (float x in new[] { -.45f, 0f, .45f })
                F.Cubo(dentro, "Banco_Tras_Cabeca", "Petroleo_Escuro", F.V(x, 1.17f, -.98f), F.V(.24f, .16f, .08f));

            P(dentro, "Carros/Interior_Painel", F.V(0, .66f, 1.13f));
            P(dentro, "Carros/Interior_Cambio", F.V(0, .3f, .74f));
            var radio = P(dentro, "Carros/Interior_Radio", F.V(0, .84f, 1.04f), 180f);
            var areaDoRadio = radio.AddComponent<BoxCollider>();
            areaDoRadio.isTrigger = true;
            areaDoRadio.center = F.V(0, .03f, 0);
            areaDoRadio.size = F.V(.26f, .12f, .12f);
            c.radio = areaDoRadio;

            // Volante: o meio é o botão da buzina; a mão do motorista fica no aro.
            F.Barra(dentro, "Coluna_Do_Volante", "Grafite", F.V(-.42f, .84f, 1.14f), F.V(-.42f, .98f, .86f), .06f);
            var volante = F.Grupo("Volante", dentro, F.V(-.42f, 1.0f, .84f), F.V(-63f, 0, 0)).transform;
            P(volante, "Carros/Interior_Volante", F.V(0, -.05f, 0));
            c.botaoDaBuzina = F.Cilindro(volante, "Botao_Da_Buzina", "Mostarda", F.V(0, .012f, 0), F.V(.1f, .03f, .1f)).transform;
            var areaDaBuzina = F.Grupo("Area_Da_Buzina", volante, F.V(0, .02f, 0)).AddComponent<SphereCollider>();
            areaDaBuzina.isTrigger = true;
            areaDaBuzina.radius = .13f;
            c.centroDoVolante = areaDaBuzina;
            c.maoNoVolante = F.Grupo("Mao_No_Volante", volante, F.V(.17f, .06f, .02f)).transform;
            c.maoNaBuzina = F.Grupo("Mao_Na_Buzina", volante, F.V(.02f, .07f, 0)).transform;
            var mao = F.Grupo("Mao_Do_Motorista", dentro, Vector3.zero, F.V(-63f, 0, 0)).transform;
            F.Esfera(mao, "Palma", "Marrom_Claro", Vector3.zero, F.V(.09f, .05f, .1f));
            F.Capsula(mao, "Dedao", "Marrom_Claro", F.V(-.045f, 0, .02f), F.V(.025f, .06f, .025f), F.V(0, 0, 60));
            F.Cilindro(mao, "Manga", "Petroleo_Claro", F.V(.04f, -.02f, -.09f), F.V(.07f, .14f, .07f), F.V(60, 0, 0));
            c.maoDoMotorista = mao;

            // Chave, do lado da coluna do volante.
            F.Cilindro(dentro, "Ignicao", "Grafite", F.V(-.2f, .86f, 1.03f), F.V(.06f, .02f, .06f), F.V(90, 0, 0));
            var chave = F.Grupo("Chave", dentro, F.V(-.2f, .86f, 1.0f)).transform;
            F.Caixa(chave, "Cabeca", "Mostarda", 0, -.025f, -.035f, .05f, .05f, .014f);
            F.Caixa(chave, "Haste", "Cinza_Claro", 0, -.005f, 0, .012f, .01f, .05f);
            var areaDaChave = chave.gameObject.AddComponent<BoxCollider>();
            areaDaChave.isTrigger = true;
            areaDaChave.size = F.V(.14f, .14f, .14f);
            c.chave = areaDaChave;
            c.giroDaChave = chave;

            // Luzinhas do painel: acendem quando o carro liga.
            c.luzesDoPainel = new[]
            {
                F.Esfera(dentro, "Luz_Do_Painel", "Luz_Apagada", F.V(-.52f, .95f, 1.0f), Vector3.one * .03f).GetComponent<Renderer>(),
                F.Esfera(dentro, "Luz_Do_Painel", "Luz_Apagada", F.V(-.32f, .95f, 1.0f), Vector3.one * .03f).GetComponent<Renderer>(),
            };
            c.luzApagada = ArtePaleta.Mat("Luz_Apagada");
            c.luzAcesa = ArtePaleta.Mat("Luz_Verde");

            // Retrovisor: uma câmera olhando pra trás desenha numa textura.
            F.Caixa(dentro, "Retrovisor", "Grafite", 0, 1.42f, .76f, .27f, .09f, .03f);
            var espelho = new GameObject("Espelho", typeof(MeshFilter), typeof(MeshRenderer));
            espelho.transform.SetParent(dentro, false);
            espelho.transform.localPosition = F.V(0, 1.465f, .743f);
            espelho.transform.localScale = F.V(.25f, .075f, 1f);
            espelho.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var rEspelho = espelho.GetComponent<MeshRenderer>();
            rEspelho.sharedMaterial = MaterialDoRetrovisor();
            rEspelho.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            c.espelho = rEspelho;
            var camRetro = F.Grupo("Camera_Do_Retrovisor", dentro, F.V(0, 1.45f, .7f), F.V(12, 180, 0)).AddComponent<Camera>();
            camRetro.fieldOfView = 40f;
            camRetro.nearClipPlane = .05f;
            camRetro.farClipPlane = 80f;
            camRetro.depth = -5;
            camRetro.allowHDR = false;
            camRetro.allowMSAA = false;
            c.retrovisor = camRetro;

            c.portaMalas = F.Grupo("Porta_Malas_Dentro", raiz, F.V(0, .8f, -1.65f)).transform;

            // Bancos, cintos, poses, chão e portas.
            c.assentos = new Transform[5];
            c.cintos = new CintoDoAssento[5];
            c.posesDoCinto = new Transform[5];
            c.chaoDosAssentos = new Transform[5];
            c.portas = new Transform[5];
            // Bancos da frente: a câmera olha pela porta aberta, de cima pra baixo (dá pra ver o peito, o colo e o fecho).
            // Bancos de trás: vistos da frente, do vão entre os dois bancos da frente (abaixo das cabeças de quem vai na frente).
            Vector3[] olhos = { F.V(-1.25f, 1.42f, .3f), F.V(1.25f, 1.42f, .3f), F.V(-.11f, 1.13f, .0f), F.V(0, 1.18f, .02f), F.V(.11f, 1.13f, .0f) };
            for (int i = 0; i < 5; i++)
            {
                Vector3 a = Assentos[i];
                c.assentos[i] = F.Grupo("Assento_" + i, dentro, a).transform;
                c.cintos[i] = Cinto(dentro, i, a);
                c.posesDoCinto[i] = Pose(raiz, "Pose_Cinto_" + i, olhos[i], a + F.V(i == 3 ? 0f : -Mathf.Sign(a.x) * .05f, .2f, .04f));
                c.chaoDosAssentos[i] = F.Grupo("Chao_" + i, dentro, F.V(a.x, .31f, i < 2 ? .78f : -.2f)).transform;
                c.portas[i] = F.Grupo("Porta_" + i, raiz, F.V(1.45f, 0, a.z)).transform;
            }
            c.poseDaChave = Pose(raiz, "Pose_Da_Chave", F.V(.1f, 1.2f, .55f), F.V(-.25f, .88f, 1.0f));
            // Os olhos de quem vai no meio do banco de trás, inclinado pra frente entre os dois bancos da frente:
            // vê a rua pelo para-brisa, o volante do motorista e a janela da direita.
            c.poseDoPasseio = F.Grupo("Pose_Do_Passeio", raiz, F.V(.03f, 1.24f, -.1f), F.V(4f, 6f, 0)).transform;
            // Onde o que está solto vai parar na freada: voa por cima dos bancos e cai no painel, na frente da criança.
            c.pousoDosSoltos = F.Grupo("Pouso_Dos_Soltos", dentro, F.V(.24f, 1.05f, 1.02f)).transform;
            return c;
        }

        // O cinto de um banco: âncora na coluna (ombro), âncora de baixo (quadril, lado da porta), fecho
        // (quadril, lado de dentro), a fivela e quatro pedaços de tira.
        static CintoDoAssento Cinto(Transform dentro, int i, Vector3 a)
        {
            var g = F.Grupo("Cinto_" + i, dentro).transform;
            var cinto = g.gameObject.AddComponent<CintoDoAssento>();
            bool meio = i == 3;
            float o = meio ? 1f : Mathf.Sign(a.x);
            bool frente = i < 2;
            Vector3 ancora = meio ? F.V(.17f, 1.07f, -.9f) : F.V(o * .8f, frente ? 1.13f : 1.08f, a.z - .12f);
            Vector3 baixa = meio ? F.V(.25f, .58f, -.7f) : F.V(o * .72f, .58f, a.z + .02f);
            Vector3 fecho = meio ? F.V(-.22f, .6f, -.66f) : F.V(a.x - o * .24f, .6f, a.z + .06f);
            cinto.repouso = meio ? F.V(.25f, .86f, -.86f) : F.V(o * .74f, .84f, a.z);

            cinto.ancora = F.Grupo("Ancora", g, ancora).transform;
            F.Caixa(cinto.ancora, "Suporte", "Grafite", 0, -.04f, 0, .05f, .08f, .05f);
            cinto.ancoraBaixa = F.Grupo("Ancora_Baixa", g, baixa).transform;
            F.Caixa(cinto.ancoraBaixa, "Suporte", "Grafite", 0, -.03f, 0, .05f, .06f, .05f);
            cinto.fecho = F.Grupo("Fecho", g, fecho).transform;
            F.Caixa(cinto.fecho, "Receptor", "Grafite", 0, -.12f, 0, .05f, .12f, .045f);
            F.Caixa(cinto.fecho, "Botao", "Vermelho", 0, -.005f, 0, .036f, .012f, .03f);
            cinto.fivela = F.Grupo("Fivela", g, cinto.repouso).transform;
            P(cinto.fivela, "Carros/Interior_Fivela", F.V(0, -.06f, 0)).transform.localEulerAngles = F.V(90, 0, 0);
            cinto.tiras = new Transform[4];
            for (int k = 0; k < 4; k++)
            {
                var tira = F.Cubo(g, "Tira_" + k, "Grafite", Vector3.zero, Vector3.one);
                tira.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                cinto.tiras[k] = tira.transform;
            }
            cinto.Soltar();
            return cinto;
        }

        static Material MaterialDoRetrovisor()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(CaminhoDoRetrovisor);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, CaminhoDoRetrovisor);
            }
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ pessoas

        static OcupanteDoCarro[] Pessoas()
        {
            var raiz = F.Grupo("Pessoas").transform;
            var quem = new (string id, string nome, string prefab)[]
            {
                ("pai", "Pai", "Familia_Pai_Animado"), ("mae", "Mãe", "Familia_Mae_Animado"), ("voce", "Você", "Familia_FilhoMaisVelho_Animado"),
                ("filha", "Filha", "Familia_Filha_Animado"), ("cacula", "Caçula", "Familia_FilhoMaisNovo_Animado"), ("vovo", "Vovó", "Idosa"),
            };
            var lista = new List<OcupanteDoCarro>();
            foreach (var q in quem)
            {
                var go = P(raiz, "Personagens/" + q.prefab, Vector3.zero);
                go.name = "Pessoa_" + q.id;
                var o = go.AddComponent<OcupanteDoCarro>();
                o.id = q.id;
                o.nome = q.nome;
                o.animador = go.GetComponentInChildren<Animator>();
                // A pose sentada: o último quadro de "SitDown". Mede onde ficam o quadril e a cabeça.
                AnimationClip sentar = o.animador.runtimeAnimatorController.animationClips.FirstOrDefault(x => x.name == "SitDown");
                if (sentar == null) Debug.LogError("[Rua Viva] " + q.prefab + " não tem SitDown.");
                else sentar.SampleAnimation(o.animador.gameObject, sentar.length);
                var ossos = go.GetComponentsInChildren<Transform>();
                var quadril = ossos.FirstOrDefault(x => x.name.ToLower().Contains("hips") || x.name.ToLower().Contains("pelvis"));
                var cabeca = ossos.FirstOrDefault(x => x.name.ToLower().Contains("head"));
                o.quadril = quadril != null ? go.transform.InverseTransformPoint(quadril.position) : F.V(0, .3f, -.2f);
                o.cabeca = cabeca != null ? go.transform.InverseTransformPoint(cabeca.position) : F.V(0, .9f, -.3f);
                if (q.id == "voce") o.Camada(CamadaDoJogador);
                go.SetActive(false);
                lista.Add(o);
            }
            return lista.ToArray();
        }

        // ------------------------------------------------------------------ regras (conferência com as definições)

        // O que NÃO vem direto das definições (lista da conferência e do Docs/Revisao_Coordenacao.md).

        public static readonly string[] NaoVemDasDefinicoes =

        {
                         "AS MISSÕES DEPOIS DA ENCHENTE: 1 o parque que reabriu; 2 buscar a vovó na rodoviária; 3 levar o caçula à escola (buscando a filha na casa da amiga); 4 levar doações ao hospital; 5 a festa da reconstrução. Os lugares do percurso (parque, rodoviária, cruzamento, escola, bombeiros, hospital, delegacia, Lago Paranoá, obra, festa, casa da amiga, casa da vovó), os nomes nas placas e as notícias do rádio também são do jogo.",
                         "O safari ligado aos lugares: ambulância no hospital, ônibus na rodoviária, viatura na delegacia, ponte no lago etc. É associação do jogo, não das definições.",
                         "QUEM APERTA A BUZINA É O MOTORISTA. A definição diz \"quando alguém apertado\", sem dizer quem. A criança toca no meio do volante pra pedir, e a mão do motorista aperta. A mão é uma forma simples, solta do corpo do personagem.",
                         "O motorista prende o próprio cinto sozinho, e a criança puxa a fivela dos outros (adultos também). Na vida real cada adulto prende o seu e ajuda as crianças.",
                         "A chave só liga o carro depois de todos de cinto (\"o carro só sai com todos de cinto\"): a definição diz que a gente deve prender ao entrar, não fala da chave.",
                         "A criança vai no meio do banco de trás: vem da regra de trânsito (criança no banco de trás), não do glossário. Pelo mesmo motivo nenhuma criança vai na frente; o banco da frente fica vazio na maior parte das missões (também pra cabeça de quem vai na frente não tapar a rua), e só a vovó senta lá, no fim da missão 2.",
                         "Coisas soltas (ursinho, caixas) voam na freada e vão pro chão do carro; nos níveis 4 e 5 a criança guarda tudo no porta-malas antes de sair. A definição fala de não machucar quem está de cinto, não de objetos.",
                         "A bola que atravessa a rua e faz o motorista frear (níveis 2 e 4), e quem está de cinto inclinar um pouco e ficar no banco.",
                         "Buzinar à toa ser errado e assustar a família: a definição diz para que serve a buzina, não proíbe outros usos.",
                         "A hora certa de buzinar (pessoa vindo pra rua sem ter visto o carro) e que ela para ao ouvir; sem buzina o motorista freia; a pessoa atenta, que anda pela calçada, não precisa de buzina.",
                         "No cruzamento, o verde abre depois de alguns segundos com o carro parado.",
                         "Desafivelar ao descer (escola): a definição não fala de soltar o cinto.",
                         "\"Automóvel\" aceitar ônibus, ambulância, bombeiro, viatura e micro-ônibus (vem da letra: \"Veículo com motor, com quatro ou mais rodas\"); o semáforo de pedestre também responde \"semáforo\"; ciclista e bicicleta elétrica também respondem \"bicicleta\".",
                         "Modelos substitutos: micro-ônibus = van; ciclista = bicicleta com menino parado em cima. O carro da família, os bancos e a cabine são formas da paleta.",
                         "O retrovisor, o rádio, os carros que vêm na outra mão (níveis 2 a 5, mais cheio no 4 e no 5).",
                         "Passageiro, cinto de segurança e buzina não aparecem como pedido do safari: são a própria mecânica. Os outros verbetes fora dos pedidos: via, via rural, via urbana, calçada, ciclofaixa, retorno, túnel, viaduto, passarela, passagem subterrânea, engarrafamento, bonde, quadriciclo, sinal verde, sinal amarelo e sinal vermelho (este aparece no cruzamento, sem pedido).",

        };

        // Regras da última conferência: {verbete, trecho literal, regra} (usadas pelo RevisaoConstrutor).

        public static readonly List<string[]> RegrasDaRevisao = new List<string[]>();


        [MenuItem("Cidade Viva/Verbetes/Conferir regras do Passeio de Carro com as definições")]
        public static void ConferirPeloMenu()
        {
            ConferirTrechos(Object.FindObjectsByType<LugarDoPasseio>(FindObjectsInactive.Include));
        }

        public static bool ConferirTrechos(LugarDoPasseio[] lugares)
        {
            var config = AssetDatabase.LoadAssetAtPath<RuaVivaConfig>(CaminhoDaConfig);
            var banco = VerbeteDatabase.Instancia;
            var falhas = new List<string>();
            var lista = new StringBuilder();
            RegrasDaRevisao.Clear();
            void Regra(string id, string trecho, string regra)
            {
                RegrasDaRevisao.Add(new[] { id, trecho, regra });
                var v = banco.Buscar(id);
                if (v == null) { falhas.Add("verbete desconhecido: " + id); return; }
                if (!v.definicao.Contains(trecho)) falhas.Add($"{id}: o trecho \"{trecho}\" não está na definição");
                lista.AppendLine($"{v.palavra}: {regra} <= \"{trecho}\"");
            }
            lista.AppendLine("REGRAS DERIVADAS DAS DEFINIÇÕES");
            Regra("cinto_de_seguranca", config.trechoDaFivela, "prender é puxar a fivela até o fecho (\"clique\")");
            Regra("cinto_de_seguranca", config.trechoDoCorpo, "a tira passa pelo peito e pelo colo de quem está sentado e fica presa no banco");
            Regra("cinto_de_seguranca", config.trechoDeQuando, "o cinto é preso logo que se entra no carro: no começo da missão e quando alguém entra numa parada");
            Regra("cinto_de_seguranca", "a gente deve prender", "todos prendem, inclusive o motorista");
            Regra("cinto_de_seguranca", config.trechoDaFreada, "na freada, quem está de cinto fica no banco; o balão cita o trecho");
            Regra("passageiro", config.trechoDeDentro, "quem entra no carro numa parada vira passageiro (e prende o cinto)");
            Regra("passageiro", config.trechoDoPassageiro, "a criança não dirige: o carro anda sozinho");
            Regra("passageiro", config.trechoDoMotorista, "o motorista vai junto e é quem faz os pedidos");
            Regra("buzina", config.trechoDoBotao, "a buzina é um botão do carro: o meio do volante");
            Regra("buzina", config.trechoDeQuemAperta, "alguém aperta o botão: aqui, a mão do motorista, quando a criança toca no meio do volante");
            Regra("buzina", config.trechoDaBuzina, "buzinar serve pra alguém perceber: a pessoa distraída ouve e para");
            Regra("sinal_vermelho", config.trechoDoVermelho, "no cruzamento, o carro para no sinal vermelho");

            lista.AppendLine().AppendLine("O QUE NÃO VEM DIRETO DAS DEFINIÇÕES (escolhas do jogo, pra revisão)");
            foreach (string item in NaoVemDasDefinicoes)
                lista.AppendLine("  - " + item);

            // Pedidos de cada missão: o lugar tem de ter o objeto.
            lista.AppendLine().AppendLine("PEDIDOS DO SAFARI POR NÍVEL");
            var porId = (lugares ?? new LugarDoPasseio[0]).GroupBy(l => l.id).ToDictionary(x => x.Key, x => x.First());
            for (int n = 0; n < config.missoes.Length; n++)
            {
                var m = config.missoes[n];
                var pedidos = new List<string>();
                foreach (var e in m.eventos)
                {
                    if (e.tipo == TipoDeEvento.Lugar || e.tipo == TipoDeEvento.Parada || e.tipo == TipoDeEvento.Destino)
                    {
                        if (!porId.TryGetValue(e.lugar, out var l)) { falhas.Add($"nível {n + 1}: lugar desconhecido {e.lugar}"); continue; }
                        if (string.IsNullOrEmpty(e.pedido)) continue;
                        if (banco.Buscar(e.pedido) == null) falhas.Add($"nível {n + 1}: verbete desconhecido {e.pedido}");
                        if (l.Objeto(e.pedido) == null) falhas.Add($"nível {n + 1}: o lugar {e.lugar} não tem {e.pedido}");
                        pedidos.Add($"{e.pedido} ({l.nome})");
                    }
                }
                lista.AppendLine($"  Nível {n + 1} ({m.titulo}): " + string.Join(", ", pedidos));
            }

            lista.AppendLine().AppendLine(falhas.Count == 0 ? "CONFERÊNCIA: tudo certo." : "CONFERÊNCIA: " + falhas.Count + " falha(s):");
            foreach (string f in falhas) lista.AppendLine("  - " + f);
            File.WriteAllText("Temp/ruaviva_carro_regras.txt", lista.ToString(), Encoding.UTF8);
            if (falhas.Count == 0) Debug.Log("[Rua Viva] Regras conferidas. Lista em Temp/ruaviva_carro_regras.txt.");
            else Debug.LogError("[Rua Viva] Problemas:\n" + string.Join("\n", falhas));
            return falhas.Count == 0;
        }

        // ------------------------------------------------------------------ missões, níveis e config

        static OcupanteDaMissao O(string quem, int assento) { return new OcupanteDaMissao { quem = quem, assento = assento }; }
        static EventoDaMissao Lugar(string lugar, string pedido = "") { return new EventoDaMissao { tipo = TipoDeEvento.Lugar, lugar = lugar, pedido = pedido }; }
        static EventoDaMissao Parada(string lugar, string entra, int assento) { return new EventoDaMissao { tipo = TipoDeEvento.Parada, lugar = lugar, entra = entra, assento = assento }; }
        static EventoDaMissao Destino(string lugar, string pedido) { return new EventoDaMissao { tipo = TipoDeEvento.Destino, lugar = lugar, pedido = pedido }; }
        static EventoDaMissao Tipo(TipoDeEvento tipo) { return new EventoDaMissao { tipo = tipo }; }

        static MissaoDoPasseio[] Missoes()
        {
            var distraida = TipoDeEvento.Distraida;
            var atenta = TipoDeEvento.Atenta;
            var bola = TipoDeEvento.Bola;
            var rodoviaria = Destino("rodoviaria", "onibus");
            rodoviaria.entra = "vovo";
            rodoviaria.assento = 1;
            var escola = Destino("escola", "faixa_de_pedestre");
            escola.descem = new[] { "cacula", "filha" };
            var festa = Destino("festa", "estacionamento");
            festa.espera = "mae";
            return new[]
            {
                new MissaoDoPasseio
                {
                    titulo = "Missão 1: o parque reabriu!",
                    texto = "A água da enchente baixou e o parque abriu de novo. A família vai passear lá!",
                    novidade = "Novo: prender o cinto de segurança.",
                    ocupantes = new[] { O("pai", 0), O("voce", 3), O("filha", 4) },
                    eventos = new[] { Lugar("bairro", "automovel"), Destino("parque", "bicicleta") },
                    radio = new[] { "O parque da cidade reabriu hoje!", "A água baixou e as ruas estão secando." },
                    recadoDeChegada = "Chegamos ao parque! Todo mundo de cinto, do começo ao fim.",
                },
                new MissaoDoPasseio
                {
                    titulo = "Missão 2: buscar a vovó",
                    texto = "A vovó chega de ônibus na rodoviária. Vamos buscar!",
                    novidade = "Novo: a freada e a buzina.",
                    ocupantes = new[] { O("pai", 0), O("mae", 2), O("voce", 3) },
                    soltos = new[] { O("ursinho", 4) },
                    eventos = new[] { Lugar("bairro", "motocicleta"), Tipo(bola), Lugar("cruzamento", "semaforo"), Tipo(distraida), rodoviaria },
                    radio = new[] { "Os ônibus voltaram a chegar na rodoviária!", "Os semáforos do cruzamento já funcionam de novo." },
                    recadoDeChegada = "A vovó chegou! E já está de cinto.",
                },
                new MissaoDoPasseio
                {
                    titulo = "Missão 3: dia de escola",
                    texto = "A escola abriu de novo! O pai leva as crianças, e no caminho busca a filha na casa da amiga.",
                    novidade = "Novo: gente entrando no carro no meio do caminho.",
                    ocupantes = new[] { O("pai", 0), O("voce", 3), O("cacula", 2) },
                    eventos = new[] { Parada("casa_amiga", "filha", 4), Lugar("bombeiros", "caminhao_de_bombeiro"), Tipo(distraida), Lugar("bairro", "motoneta"), Tipo(distraida), escola },
                    radio = new[] { "As escolas abrem de novo hoje!", "Os bombeiros ajudaram muita gente na enchente." },
                    recadoDeChegada = "Chegamos na escola! Boa aula, crianças!",
                },
                new MissaoDoPasseio
                {
                    titulo = "Missão 4: doações ao hospital",
                    texto = "A família leva brinquedos e roupas pro hospital. Primeiro, guarde as coisas soltas!",
                    novidade = "Novo: coisa solta no banco voa na freada. E a rua está mais cheia.",
                    ocupantes = new[] { O("pai", 0), O("mae", 2), O("voce", 3) },
                    soltos = new[] { O("caixa", 1), O("ursinho", 4) },
                    guardarSoltos = true,
                    eventos = new[] { Lugar("obra", "caminhao_trator"), Tipo(bola), Lugar("lago", "ponte"), Tipo(distraida), Lugar("delegacia", "viatura"), Tipo(atenta), Tipo(distraida), Destino("hospital", "ambulancia") },
                    radio = new[] { "O hospital pede doações de roupas e brinquedos.", "A ponte do lago já foi consertada." },
                    recadoDeChegada = "As doações chegaram ao hospital!",
                },
                new MissaoDoPasseio
                {
                    titulo = "Missão 5: a festa da reconstrução",
                    texto = "A cidade está quase toda arrumada! A mãe espera na festa. No caminho, a família busca a vovó.",
                    novidade = "Agora é tudo junto: a viagem inteira.",
                    ocupantes = new[] { O("pai", 0), O("voce", 3), O("filha", 2) },
                    soltos = new[] { O("caixa", 1) },
                    guardarSoltos = true,
                    eventos = new[]
                    {
                        Parada("casa_vovo", "vovo", 4), Lugar("parque", "ciclista"), Tipo(distraida), Lugar("obra", "reboque"), Tipo(atenta),
                        Lugar("cruzamento", "semaforo_de_pedestre"), Tipo(distraida), Lugar("rodoviaria", "micro_onibus"), Tipo(atenta), Tipo(distraida), festa,
                    },
                    radio = new[] { "Hoje tem festa da reconstrução!", "A cidade está quase toda arrumada." },
                    recadoDeChegada = "Chegamos na festa! A mãe estava esperando.",
                },
            };
        }

        static NivelConfig Nivel(string objetivo, float velocidade, int pedidos, int buzinas, float janela, float tempoPorPedido, float transito, params string[] regras)
        {
            var n = new NivelConfig
            {
                objetivo = objetivo, velocidade = velocidade, quantidadeDeItens = pedidos, quantidadeDeObstaculos = buzinas,
                regrasExtras = new List<string>(regras),
                // As estrelas são calculadas pelo manager; isto só alimenta o texto da seleção.
                estrelas = new CriterioDeEstrelas { medida = MedidaDeEstrelas.Erros, para2Estrelas = 2, para3Estrelas = 0 },
            };
            void E(string nome, float valor) { n.extras.Add(new ParametroExtra { nome = nome, valor = valor }); }
            E("janelaDaBuzina", janela); E("tempoPorPedido", tempoPorPedido); E("transito", transito);
            return n;
        }

        static LevelConfig SalvarNiveis()
        {
            const string figura = RuaVivaCarroManager.RegraFigura, para = RuaVivaCarroManager.RegraCarroPara, devagar = RuaVivaCarroManager.RegraCarroDevagar,
                dica = RuaVivaCarroManager.RegraDicaBuzina;
            ArtePaleta.GarantirPasta("Assets/Configs/Niveis");
            var asset = AssetDatabase.LoadAssetAtPath<LevelConfig>(CaminhoDosNiveis);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(asset, CaminhoDosNiveis);
            }
            asset.niveis = new List<NivelConfig>
            {
                Nivel("O parque reabriu! Prenda o cinto de todos.", 5f, 2, 0, 5f, 0f, 0f, figura, para),
                Nivel("Buscar a vovó: a freada e a buzina.", 5.5f, 3, 1, 5f, 0f, 160f, devagar, dica),
                Nivel("Dia de escola: gente entra no caminho.", 6f, 3, 2, 4.5f, 0f, 110f, devagar),
                Nivel("Doações ao hospital: guarde o que está solto.", 6.5f, 4, 2, 4f, 0f, 55f),
                Nivel("A festa da reconstrução: a viagem inteira!", 7.5f, 5, 3, 3.5f, 8f, 40f),
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
            else
            {
                // Volta tudo pro que está no código (textos e medidas da versão nova).
                var novo = ScriptableObject.CreateInstance<RuaVivaConfig>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(novo), config);
                Object.DestroyImmediate(novo);
            }
            config.missoes = Missoes();
            config.somDaFivela = TravessiaConstrutor.Som("Efeitos/impactMetal_light_001.ogg"); // clique metálico (o switch_001 era 10 dB mais baixo que os outros sons)
            config.somDeSoltar = TravessiaConstrutor.Som("Interface/click_001.ogg");
            config.somDoMotor = TravessiaConstrutor.Som("Efeitos/motor_ligando.wav");
            config.somDaBuzina = TravessiaConstrutor.Som("Efeitos/buzina.wav");
            config.somDeAcerto = TravessiaConstrutor.Som("Interface/confirmation_002.ogg");
            config.somDeOutroObjeto = TravessiaConstrutor.Som("Interface/select_002.ogg");
            config.somDoSusto = TravessiaConstrutor.Som("Efeitos/tropeco.wav");
            config.somDaFreada = TravessiaConstrutor.Som("Interface/error_004.ogg");
            config.somDePedido = TravessiaConstrutor.Som("Interface/select_001.ogg");
            config.somDeComemoracao = TravessiaConstrutor.Som("Interface/confirmation_003.ogg");
            config.somDoRadio = TravessiaConstrutor.Som("Interface/tick_001.ogg");
            config.somDoPortaMalas = TravessiaConstrutor.Som("Interface/drop_001.ogg");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        // ------------------------------------------------------------------ interface

        static RuaVivaHUD Hud(Canvas canvas, out Button botaoMissao, out Button botaoOuvirMissao)
        {
            var raiz = Tela("HUD", canvas.transform);
            var hud = raiz.gameObject.AddComponent<RuaVivaHUD>();
            var voltarAoHub = raiz.gameObject.AddComponent<VoltarAoHub>();

            // Pedido do motorista.
            var pedido = Imagem("Balao_Do_Pedido", raiz, arredondado, C("Branco", .96f), new Vector2(60, -105), new Vector2(1000, 130));
            TravessiaConstrutor.Ancorar(pedido, new Vector2(.5f, 1));
            hud.balaoDoPedido = pedido.gameObject;
            hud.textoDoPedido = Texto("Texto", pedido.transform, "", 46, C("Petroleo"), new Vector2(-50, 0), new Vector2(680, 118));
            hud.textoDoPedido.resizeTextForBestFit = true; hud.textoDoPedido.resizeTextMinSize = 26; hud.textoDoPedido.resizeTextMaxSize = 46;
            hud.figuraDoPedido = Imagem("Figura", pedido.transform, null, Color.white, new Vector2(-430, 0), new Vector2(118, 118));
            var ouvir = Botao("Botao_Ouvir", pedido.transform, "Ouvir", "Petroleo_Claro", new Vector2(380, 0), new Vector2(190, 110), 34);
            hud.botaoOuvir = ouvir.gameObject;
            var tempo = Imagem("Painel_Do_Tempo", raiz, arredondado, C("Petroleo_Escuro", .9f), new Vector2(60, -190), new Vector2(720, 32));
            TravessiaConstrutor.Ancorar(tempo, new Vector2(.5f, 1));
            var area = Ret("Area", tempo.transform, Vector2.zero, Vector2.zero);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(5, 5); area.offsetMax = new Vector2(-5, -5);
            var cheio = Imagem("Cheio", area, arredondado, C("Mostarda"), Vector2.zero, Vector2.zero);
            cheio.rectTransform.anchorMin = Vector2.zero; cheio.rectTransform.anchorMax = Vector2.one;
            cheio.rectTransform.offsetMin = cheio.rectTransform.offsetMax = Vector2.zero;
            hud.painelDoTempo = tempo.gameObject;
            hud.barraDoTempo = cheio.rectTransform;

            // Figurinha do acerto.
            var figurinha = Imagem("Figurinha", raiz, arredondado, C("Branco", .98f), new Vector2(-250, 40), new Vector2(380, 450));
            TravessiaConstrutor.Ancorar(figurinha, new Vector2(1, .5f));
            hud.figurinha = figurinha.gameObject;
            hud.imagemDaFigurinha = Imagem("Imagem", figurinha.transform, null, Color.white, new Vector2(0, 45), new Vector2(330, 330));
            hud.palavraDaFigurinha = Texto("Palavra", figurinha.transform, "", 44, C("Petroleo"), new Vector2(0, -175), new Vector2(350, 80));
            hud.palavraDaFigurinha.resizeTextForBestFit = true; hud.palavraDaFigurinha.resizeTextMinSize = 24; hud.palavraDaFigurinha.resizeTextMaxSize = 44;

            // Rádio (embaixo, à esquerda).
            var radio = Imagem("Painel_Do_Radio", raiz, arredondado, C("Petroleo_Escuro", .9f), new Vector2(360, 200), new Vector2(640, 96));
            TravessiaConstrutor.Ancorar(radio, new Vector2(0, 0));
            hud.painelDoRadio = radio.gameObject;
            hud.textoDoRadio = Texto("Texto", radio.transform, "", 30, C("Branco"), Vector2.zero, new Vector2(610, 86), FontStyle.Normal);
            hud.textoDoRadio.resizeTextForBestFit = true; hud.textoDoRadio.resizeTextMinSize = 20; hud.textoDoRadio.resizeTextMaxSize = 30;

            var sinal = Texto("Sinal_Da_Buzina", raiz, "BIIII!", 72, C("Mostarda"), new Vector2(0, 160), new Vector2(420, 96));
            hud.sinalDaBuzina = sinal.gameObject;

            hud.marca = Imagem("Marca", raiz, S("figura_estrela"), C("Mostarda"), Vector2.zero, new Vector2(110, 110)).rectTransform;

            // Nível e critérios, voltar, recado.
            var painel = Imagem("Painel_Do_Nivel", raiz, arredondado, C("Branco", .92f), new Vector2(240, -135), new Vector2(430, 220));
            TravessiaConstrutor.Ancorar(painel, new Vector2(0, 1));
            hud.painelDoNivel = painel.gameObject;
            hud.textoDoNivel = Texto("Nivel", painel.transform, "", 30, C("Petroleo"), new Vector2(0, 80), new Vector2(400, 44));
            var estrelas = new Image[3];
            var rotulos = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                estrelas[i] = Imagem("Estrela_" + (i + 1), painel.transform, S("figura_estrela"), Color.white, new Vector2(-135 + i * 135, 20), new Vector2(66, 66));
                rotulos[i] = Texto("Rotulo_" + (i + 1), painel.transform, "", 30, C("Grafite"), new Vector2(-135 + i * 135, -35), new Vector2(140, 42), FontStyle.Normal);
                rotulos[i].resizeTextForBestFit = true; rotulos[i].resizeTextMinSize = 24; rotulos[i].resizeTextMaxSize = 30;
            }
            hud.estrelasDosCriterios = estrelas;
            hud.rotulosDosCriterios = rotulos;
            hud.textoDoSafari = Texto("Safari", painel.transform, "", 28, C("Cinza_Escuro"), new Vector2(0, -80), new Vector2(400, 40), FontStyle.Normal);

            var hubBotao = Botao("Botao_VoltarAoHub", raiz, "", "Petroleo_Claro", new Vector2(-100, -95), new Vector2(120, 120), 0, S("ui_seta_voltar"), circulo);
            TravessiaConstrutor.Ancorar(hubBotao, new Vector2(1, 1));
            UnityEventTools.AddPersistentListener(hubBotao.onClick, voltarAoHub.Voltar);

            var recado = Imagem("Painel_Do_Recado", raiz, arredondado, C("Branco", .94f), new Vector2(0, 70), new Vector2(1180, 86));
            TravessiaConstrutor.Ancorar(recado, new Vector2(.5f, 0));
            hud.painelDoRecado = recado.gameObject;
            hud.textoDoRecado = Texto("Texto", recado.transform, "", 36, C("Petroleo"), Vector2.zero, new Vector2(1150, 76));
            hud.textoDoRecado.resizeTextForBestFit = true; hud.textoDoRecado.resizeTextMinSize = 22; hud.textoDoRecado.resizeTextMaxSize = 36;

            var balao = Imagem("Balao", raiz, arredondado, C("Grafite"), new Vector2(0, 170), new Vector2(1000, 280));
            Imagem("Papel", balao.transform, arredondado, C("Mostarda_Clara"), Vector2.zero, new Vector2(986, 266));
            hud.textoDoBalao = Texto("Texto", balao.transform, "", 40, C("Grafite"), Vector2.zero, new Vector2(940, 244), FontStyle.Normal);
            hud.textoDoBalao.resizeTextForBestFit = true; hud.textoDoBalao.resizeTextMinSize = 24; hud.textoDoBalao.resizeTextMaxSize = 40;
            hud.balao = balao.gameObject;
            balao.gameObject.SetActive(false);

            // Cartão da missão (no começo, com o carro visto de fora).
            var missao = Imagem("Cartao_Da_Missao", raiz, arredondado, C("Branco", .96f), new Vector2(0, -20), new Vector2(1160, 600), true);
            hud.cartaoDaMissao = missao.gameObject;
            Imagem("Faixa", missao.transform, arredondado, C("Petroleo"), new Vector2(0, 225), new Vector2(1100, 110));
            hud.tituloDaMissao = Texto("Titulo", missao.transform, "", 56, C("Branco"), new Vector2(0, 225), new Vector2(1060, 100));
            hud.tituloDaMissao.resizeTextForBestFit = true; hud.tituloDaMissao.resizeTextMinSize = 30; hud.tituloDaMissao.resizeTextMaxSize = 56;
            hud.textoDaMissao = Texto("Texto", missao.transform, "", 40, C("Grafite"), new Vector2(0, 70), new Vector2(1040, 180), FontStyle.Normal);
            hud.textoDaMissao.resizeTextForBestFit = true; hud.textoDaMissao.resizeTextMinSize = 24; hud.textoDaMissao.resizeTextMaxSize = 40;
            var novidade = Imagem("Novidade", missao.transform, arredondado, C("Mostarda_Clara"), new Vector2(0, -80), new Vector2(1040, 80));
            hud.novidadeDaMissao = Texto("Texto", novidade.transform, "", 34, C("Petroleo_Escuro"), Vector2.zero, new Vector2(1010, 70));
            hud.novidadeDaMissao.resizeTextForBestFit = true; hud.novidadeDaMissao.resizeTextMinSize = 20; hud.novidadeDaMissao.resizeTextMaxSize = 34;
            botaoMissao = Botao("Botao_Vamos", missao.transform, "Vamos!", "Verde", new Vector2(160, -215), new Vector2(340, 110), 46);
            botaoOuvirMissao = Botao("Botao_Ouvir_Missao", missao.transform, "Ouvir", "Petroleo_Claro", new Vector2(-230, -215), new Vector2(220, 110), 36);
            hud.botaoOuvirMissao = botaoOuvirMissao.gameObject;
            missao.gameObject.SetActive(false);

            var preta = Imagem("Tela_Preta", raiz, null, new Color(0, 0, 0, 0), Vector2.zero, Vector2.zero);
            preta.rectTransform.anchorMin = Vector2.zero; preta.rectTransform.anchorMax = Vector2.one;
            preta.rectTransform.offsetMin = preta.rectTransform.offsetMax = Vector2.zero;
            preta.raycastTarget = false;
            hud.telaPreta = preta;
            preta.gameObject.SetActive(false);

            hud.corGanha = Color.white;
            hud.corPerdida = new Color(.4f, .45f, .5f, .3f);
            return hud;
        }
    }
}
