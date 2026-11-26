using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CidadeViva.EditorArte
{
    // Monta a cena Hub (refeita em 2026-11-26, a pedido da coordenação: o Hub fiel a Brasília).
    // Duas zonas (ver HubZonas.cs):
    //  - zona monumental (de passear e reconhecer): Eixo Monumental de leste a oeste, cruzando o Eixo Rodoviário
    //    na Rodoviária; a leste, Museu Nacional e Catedral, a Esplanada com os ministérios em duas fileiras, o
    //    Congresso e a Praça dos Três Poderes; a oeste, a Torre de TV. Sem padaria, escola nem casa.
    //  - zona da superquadra (onde se joga): uma superquadra da Asa Sul (blocos sobre pilotis, Escola Classe,
    //    parquinho), o comércio local na entrequadra, o Eixinho com uma tesourinha, a ciclovia e o ponto de ônibus,
    //    e o lago na borda, com a Ponte JK ao fundo. As 4 áreas dos verbetes e os 2 extras ficam aqui.
    // Convenções: +X = leste, +Z = norte. Cada área é girada pra que o cenário dela fique no +Z local
    // (o jogador chega pelo -Z local e lê a placa). Pode ser rodado de novo: apaga o que gerou e refaz.
    public static partial class HubConstrutor
    {
        const string CaminhoDaCena = "Assets/Scenes/Hub.unity";
        const string PastaCinza = "Assets/Art/Texturas_Paleta/Cinza";

        // Onde o jogador começa e volta dos extras: no meio da superquadra.
        public static readonly Vector3 Inicio = new Vector3(50f, 1f, -3f);

        [MenuItem("Cidade Viva/Hub/Reconstruir a cidade do Hub")]
        public static void ReconstruirPeloMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Reconstruir();
        }

        public static void Reconstruir()
        {
            var cena = EditorSceneManager.OpenScene(CaminhoDaCena, OpenSceneMode.Single);
            var texturasCinza = GerarTexturasCinza();

            AmbienteConstrutor.AplicarNaCenaAberta(true);

            var antiga = GameObject.Find("Cidade");
            if (antiga != null) Object.DestroyImmediate(antiga);
            var cidade = F.Grupo("Cidade").transform;
            // O chão agora sai de cada zona; o terreno antigo (100 x 100 no meio) ficaria por cima do Eixão.
            var terreno = GameObject.Find("Terreno");
            if (terreno != null) terreno.SetActive(false);

            MapaDoHubConstrutor.Comecar();
            Zonas(cidade);
            Limites(cidade);

            // Versão final (2026-11-18): do centro antigo ficam só os dois "Extras" (Travessia Segura e
            // Corre, Correndo!). As áreas dos outros minigames antigos saem da cena (os arquivos ficam no projeto).
            foreach (string nome in AreasQueSairam)
            {
                var velha = GameObject.Find(nome);
                if (velha != null) Object.DestroyImmediate(velha);
            }
            foreach (var e in Extras)
            {
                var area = GameObject.Find(e.objeto);
                if (area == null) { Debug.LogError("[Hub] Área não encontrada: " + e.objeto); continue; }
                area.transform.SetPositionAndRotation(e.centro, Quaternion.Euler(0, e.giro, 0));
                MontarArea(area, e.montar, e.cor, e.zDoLetreiro, texturasCinza);
            }
            TirarDoBuild();

            foreach (var area in AreasDeVerbetes) MontarAreaDeVerbetes(area, texturasCinza);

            Jogador();
            MenuConstrutor.PainelDoHub();
            MapaDoHubConstrutor.Interface(MapaDoHubConstrutor.Desenhar(), LugaresDoMapa);
            CampanhaConstrutor.NoHub();
            OtimizacaoWebGL.TirarSombraDosPequenos();
            Colisores();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            // A grade da trilha da campanha é assada com a cena reaberta: colisores criados agora mesmo
            // ainda não estão na física do editor (a trilha passaria por dentro deles).
            cena = EditorSceneManager.OpenScene(CaminhoDaCena, OpenSceneMode.Single);
            CampanhaConstrutor.AssarGrade();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
        }

        // As 4 áreas e os 2 extras no mapa (botão, cor e de que lado fica o nome, pra não encavalar).
        static readonly MapaDoHubConstrutor.LugarDoMapa[] LugaresDoMapa =
        {
            new MapaDoHubConstrutor.LugarDoMapa { nome = "Oficina", objeto = "Area_Oficina", cor = "Laranja" },
            new MapaDoHubConstrutor.LugarDoMapa { nome = "Obras da Cidade", objeto = "Area_ObrasDaCidade", cor = "Mostarda" },
            new MapaDoHubConstrutor.LugarDoMapa { nome = "Cruzamento Central", objeto = "Area_CruzamentoCentral", cor = "Vermelho" },
            new MapaDoHubConstrutor.LugarDoMapa { nome = "Rua Viva", objeto = "Area_RuaViva", cor = "Rosa" },
            new MapaDoHubConstrutor.LugarDoMapa { nome = "Travessia Segura", objeto = "Area_TravessiaSegura", cor = "Petroleo_Claro" },
            new MapaDoHubConstrutor.LugarDoMapa { nome = "Corre, Correndo!", objeto = "Area_CorreCorrendo", cor = "Laranja" },
        };

        // A criança não atravessa prédios, árvores, carros, objetos e pessoas (2026-11-22). O jogador tem
        // CharacterController; aqui o cenário ganha os colisores (ver ColisoresDoCenario). O marcador de cada
        // área fica livre num raio de 2,8 m.
        static void Colisores()
        {
            var livres = Object.FindObjectsByType<AreaTrigger>(FindObjectsInactive.Include).Select(a => a.transform.position).ToList();
            int n = 0;
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (go.GetComponent<CharacterController>() != null || go.GetComponentInChildren<Camera>(true) != null || go.GetComponent<Canvas>() != null) continue;
                n += ColisoresDoCenario.Criar(go.transform, livres);
            }
            Debug.Log($"[Hub] Colisores no cenário: {n}");
        }

        // ------------------------------------------------------------------ peças

        static GameObject P(Transform pai, string prefab, Vector3 pos, float rotY = 0f, float escala = 1f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArteImportador.PastaPrefabs}/{prefab}.prefab");
            if (asset == null) { Debug.LogError("[Hub] Prefab não encontrado: " + prefab); return new GameObject("FALTOU_" + prefab); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, pai);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(0, rotY, 0);
            go.transform.localScale = Vector3.one * escala;
            return go;
        }

        // Uma caixa só, do tamanho do objeto inteiro (monumentos: a câmera também não passa por dentro).
        static void ColisorInteiro(GameObject go)
        {
            var t = go.transform;
            bool primeiro = true;
            var local = new Bounds();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var canto = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    var p = t.InverseTransformPoint(canto);
                    if (primeiro) { local = new Bounds(p, Vector3.zero); primeiro = false; }
                    else local.Encapsulate(p);
                }
            }
            var caixa = go.AddComponent<BoxCollider>();
            caixa.center = local.center;
            caixa.size = local.size;
        }

        // Paredes invisíveis em volta de tudo que é caminhável (a união dos retângulos de Caminhavel) e um chão
        // por retângulo. Ficam na camada "Ignore Raycast": a câmera e os toques não esbarram nelas.
        static void Limites(Transform cidade)
        {
            var limites = F.Grupo("Limites", cidade).transform;
            const float c = .5f, x0 = -100f, z0 = -60f;
            const int nx = 760, nz = 400;
            var livre = new bool[nx, nz];
            foreach (var r in Caminhavel)
            {
                for (int i = Mathf.RoundToInt((r.xMin - x0) / c); i < Mathf.RoundToInt((r.xMax - x0) / c); i++)
                    for (int j = Mathf.RoundToInt((r.yMin - z0) / c); j < Mathf.RoundToInt((r.yMax - z0) / c); j++)
                        livre[i, j] = true;
                Parede(limites, F.V(r.center.x, -.5f, r.center.y), F.V(r.width + 1f, 1f, r.height + 1f));
            }

            // Parede em cada trecho de fronteira entre o que é caminhável e o que não é.
            for (int i = 1; i < nx; i++)
            {
                int inicio = -1;
                for (int j = 0; j <= nz; j++)
                {
                    bool borda = j < nz && livre[i - 1, j] != livre[i, j];
                    if (borda && inicio < 0) inicio = j;
                    if (borda || inicio < 0) continue;
                    float a = z0 + inicio * c, b = z0 + j * c;
                    Parede(limites, F.V(x0 + i * c, 2f, (a + b) / 2f), F.V(1f, 4f, b - a + 1f));
                    inicio = -1;
                }
            }
            for (int j = 1; j < nz; j++)
            {
                int inicio = -1;
                for (int i = 0; i <= nx; i++)
                {
                    bool borda = i < nx && livre[i, j - 1] != livre[i, j];
                    if (borda && inicio < 0) inicio = i;
                    if (borda || inicio < 0) continue;
                    float a = x0 + inicio * c, b = x0 + i * c;
                    Parede(limites, F.V((a + b) / 2f, 2f, z0 + j * c), F.V(b - a + 1f, 4f, 1f));
                    inicio = -1;
                }
            }
        }

        static void Parede(Transform limites, Vector3 pos, Vector3 tam)
        {
            var go = F.Grupo("Parede", limites, pos);
            go.layer = 2; // Ignore Raycast
            go.AddComponent<BoxCollider>().size = tam;
        }

        // ------------------------------------------------------------------ versão final: extras

        static readonly string[] AreasQueSairam =
        {
            "Area_OndeEuVou", "Area_ReciclaComigo", "Area_PedalSeguro", "Area_SobeSobe", "Area_SemaforoMagico", "Area_OlhoVivo", "Area_GuardaOSinal",
        };

        // Cenas que saem do Build Settings na versão final (os arquivos ficam no projeto).
        public static readonly string[] CenasForaDoBuild =
        {
            "Minigame_Template", "Minigame_OndeEuVou", "Minigame_ReciclaComigo", "Minigame_PedalSeguro", "Minigame_SobeSobe",
            "Minigame_SemaforoMagico", "Minigame_OlhoVivo",
        };

        [MenuItem("Cidade Viva/Hub/Tirar os minigames antigos do Build Settings")]
        public static void TirarDoBuild()
        {
            var lista = new List<EditorBuildSettingsScene>();
            foreach (var cena in EditorBuildSettings.scenes)
            {
                if (System.Array.IndexOf(CenasForaDoBuild, Path.GetFileNameWithoutExtension(cena.path)) < 0) lista.Add(cena);
            }
            EditorBuildSettings.scenes = lista.ToArray();
        }

        // Os dois extras, na superquadra: a Travessia na faixa de pedestre da entrequadra (perto do comércio)
        // e o Corre, Correndo! no gramado entre o bloco do norte e o lago.
        static readonly (string objeto, System.Action<Transform, Transform, Transform> montar, string cor, Vector3 centro, float giro, float zDoLetreiro)[] Extras =
        {
            ("Area_TravessiaSegura", TravessiaSegura, "Petroleo_Claro", new Vector3(60f, .5f, -29.5f), 180f, 8.2f),
            ("Area_CorreCorrendo", CorreCorrendo, "Laranja", new Vector3(74f, .5f, 6f), 0f, 6.15f),
        };

        // Cada extra tem três grupos, todos com o chão em y = 0:
        //   Visual   - o cenário do minigame; sempre aparece, cinza até completar (SaturacaoDaArea).
        //   Caos     - o estrago da enchente; some ao completar.
        //   Arrumado - o que volta pro lugar; só aparece ao completar.
        // Quem liga e desliga Caos/Arrumado é o CaosDaArea, chamado pelo AreaTrigger.
        static void MontarArea(GameObject area, System.Action<Transform, Transform, Transform> montar, string corDoMarcador, float zDoLetreiro,
            SaturacaoDaArea.ParDeTextura[] texturasCinza)
        {
            foreach (var nome in new[] { "Visual", "Caos", "Arrumado", "Letreiro" })
            {
                var velho = area.transform.Find(nome);
                if (velho != null) Object.DestroyImmediate(velho.gameObject);
            }

            // O cubo antigo vira só o gatilho: some da tela, e fica um pouco maior pra ser fácil de pisar.
            var cubo = area.GetComponent<MeshRenderer>();
            if (cubo != null) cubo.enabled = false;
            var gatilho = area.GetComponent<BoxCollider>();
            if (gatilho != null) gatilho.size = new Vector3(2.6f, 2f, 2.6f);

            // O pivô da área fica 0,5 m acima do chão; o visual desce pra y=0.
            var visual = F.Grupo("Visual", area.transform, F.V(0, -.5f, 0)).transform;
            F.Cilindro(visual, "Marcador_Borda", "Branco", F.V(0, .12f, 0), F.V(3.1f, .04f, 3.1f));
            F.Cilindro(visual, "Marcador", corDoMarcador, F.V(0, .13f, 0), F.V(2.6f, .05f, 2.6f));
            var caos = F.Grupo("Caos", area.transform, F.V(0, -.5f, 0)).transform;
            var arrumado = F.Grupo("Arrumado", area.transform, F.V(0, -.5f, 0)).transform;
            montar(visual, caos, arrumado);

            var saturacao = area.GetComponent<SaturacaoDaArea>();
            if (saturacao == null) saturacao = area.AddComponent<SaturacaoDaArea>();
            saturacao.raiz = visual;
            saturacao.saturacaoSemCompletar = 0f;
            saturacao.texturasCinza = texturasCinza;

            // Na cena salva a cidade fica no estado inicial do jogo: tudo bagunçado.
            arrumado.gameObject.SetActive(false);
            var estado = area.GetComponent<CaosDaArea>();
            if (estado == null) estado = area.AddComponent<CaosDaArea>();
            estado.caos = caos.gameObject;
            estado.arrumado = arrumado.gameObject;

            Letreiro(area, zDoLetreiro);
            EditorUtility.SetDirty(area);
        }

        // Deita o objeto no chão (placas, semáforos e árvores derrubados pela enchente).
        static GameObject Tombar(GameObject go, float graus = 84f)
        {
            go.transform.Rotate(graus, 0, 0, Space.Self);
            return go;
        }

        // Placa com o nome e o recorde. Fica fora do "Visual": sempre colorida, pra criança achar a área.
        static void Letreiro(GameObject area, float z)
        {
            var raiz = PlacaDoLetreiro(area.transform, z, out Text titulo, out Text recorde);
            titulo.text = area.GetComponent<AreaTrigger>().areaName;
            recorde.text = "Venha jogar!";

            var letreiro = raiz.gameObject.AddComponent<LetreiroDaArea>();
            letreiro.area = area.GetComponent<AreaTrigger>();
            letreiro.textoDoNome = titulo;
            letreiro.textoDoRecorde = recorde;
        }

        // Dois postes, a placa e as duas linhas de texto (nome em cima, linha menor embaixo).
        // Fica em (0, -0,5, z) do pai (pivô 0,5 m acima do chão) e se lê de quem está no -Z do pai.
        static Transform PlacaDoLetreiro(Transform area, float z, out Text titulo, out Text linha)
        {
            var raiz = F.Grupo("Letreiro", area, F.V(0, -.5f, z)).transform;
            foreach (float x in new[] { -1.9f, 1.9f })
                F.Cilindro(raiz, "Poste", "Cinza_Escuro", F.V(x, 1.6f, 0), F.V(.14f, 3.2f, .14f));
            F.Cubo(raiz, "Placa", "Petroleo", F.V(0, 3.05f, 0), F.V(4.6f, 1.4f, .12f));
            F.Cubo(raiz, "Moldura", "Branco", F.V(0, 3.05f, .02f), F.V(4.8f, 1.6f, .1f));

            var canvasGo = new GameObject("Texto", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(raiz, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440, 130);
            rt.localScale = Vector3.one * .01f;
            rt.localPosition = F.V(0, 3.05f, -.07f);

            var fonte = AssetDatabase.LoadAssetAtPath<Font>(MenuConstrutor.CaminhoDaFonte);
            if (fonte == null) fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var fonteNegrito = AssetDatabase.LoadAssetAtPath<Font>(MenuConstrutor.CaminhoDaFonteNegrito);
            Text NovoTexto(string nome, int tamanho, Color cor, float yMin, float yMax, FontStyle estilo)
            {
                var go = new GameObject(nome, typeof(RectTransform), typeof(Text));
                go.transform.SetParent(canvasGo.transform, false);
                var r = go.GetComponent<RectTransform>();
                r.anchorMin = new Vector2(0, yMin); r.anchorMax = new Vector2(1, yMax);
                r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
                var t = go.GetComponent<Text>();
                bool negrito = estilo == FontStyle.Bold && fonteNegrito != null;
                t.font = negrito ? fonteNegrito : fonte; t.fontSize = tamanho; t.fontStyle = negrito ? FontStyle.Normal : estilo; t.color = cor;
                t.alignment = TextAnchor.MiddleCenter;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.raycastTarget = false;
                return t;
            }
            titulo = NovoTexto("Nome", 46, ArtePaleta.Cor("Branco"), .42f, 1f, FontStyle.Bold);
            linha = NovoTexto("Recorde", 24, ArtePaleta.Cor("Mostarda_Clara"), .04f, .44f, FontStyle.Normal);
            return raiz;
        }

        // Placa de lugar (monumentos): fica em "pos" e se lê de quem olha na direção "giro" (0 = de quem está ao sul).
        static void PlacaDeLugar(Transform pai, Vector3 pos, float giro, string nome, string linha)
        {
            var g = F.Grupo("Placa_" + nome.Replace(' ', '_'), pai, pos + Vector3.up * .5f, F.V(0, giro, 0)).transform;
            var raiz = PlacaDoLetreiro(g, 0, out Text titulo, out Text baixo);
            raiz.localScale = Vector3.one * 1.2f;
            titulo.text = nome;
            titulo.resizeTextForBestFit = true; titulo.resizeTextMinSize = 26; titulo.resizeTextMaxSize = 46;
            titulo.horizontalOverflow = HorizontalWrapMode.Wrap;
            baixo.text = linha;
        }

        // Nas funções dos extras: v = Visual (sempre), c = Caos (até completar), a = Arrumado (depois de completar).

        static void TravessiaSegura(Transform v, Transform c, Transform a)
        {
            P(v, "Ruas/Rua_FaixaDePedestre", F.V(0, .1f, 3.4f), 0, .75f);
            P(v, "Carros/Carro_Sedan", F.V(-1.9f, .22f, 3.9f), 90, .7f);
            P(v, "Personagens/Familia_Filha_Animado", F.V(.9f, .16f, 1.5f), 160);
            P(v, "Personagens/Familia_Mae_Animado", F.V(1.6f, .16f, 1.4f), 170);

            // Lama em cima da faixa, semáforo de pedestres derrubado.
            P(c, "Enchente/Chao_De_Lama", F.V(1.2f, .2f, 3.6f), 15, .7f);
            Tombar(P(c, "Props/Semaforo_Pedestres", F.V(-3.3f, .28f, 1.4f), 110));
            P(c, "Enchente/Cavalete_Pequeno_1", F.V(3.2f, .1f, 1.2f), 200);
            P(c, "Enchente/Cone_Quebrado_2", F.V(-2.4f, .1f, .9f), 70);
            P(c, "Enchente/Bloco_Concreto_Lascado_1", F.V(-4.7f, .1f, 3.4f), 30);

            P(a, "Props/Placa_FaixaDePedestre", F.V(2.7f, .1f, 1.2f), 180);
            P(a, "Props/Semaforo_Pedestres", F.V(-2.7f, .1f, 1.2f), 180);
        }

        static void CorreCorrendo(Transform v, Transform c, Transform a)
        {
            P(c, "Enchente/Buraco_No_Chao", F.V(-3.7f, .1f, 2.1f), 30, .8f);
            P(c, "Enchente/Cone_Quebrado_3", F.V(3.3f, .1f, 1.5f), 40);
            P(c, "Enchente/Entulho_De_Concreto", F.V(4.5f, .1f, 4.6f), 120, .45f);
            P(c, "Enchente/Cavalete_Mini", F.V(-3.6f, .1f, 4.8f), 160).transform.Rotate(0, 0, 14);

            P(a, "Props/Vaso_De_Laranjeira", F.V(-3.8f, .1f, 4.6f));
            P(a, "Natureza/Flores", F.V(3.9f, .1f, 2f), 0, 1.3f);

            string[] cores = { "Petroleo_Claro", "Vidro", "Petroleo_Claro" };
            for (int i = 0; i < 3; i++)
                F.Cubo(v, "Faixa_" + (i + 1), cores[i], F.V(-1.5f + i * 1.5f, .13f, 3.9f), F.V(1.42f, .05f, 4.6f));
            P(v, "Props/Cone", F.V(-1.5f, .15f, 3f));
            P(v, "Props/Buraco", F.V(0f, .16f, 4.3f), 0, .8f);
            P(v, "Props/Barreira_Obra", F.V(1.5f, .15f, 5.2f), 90, .8f);
            foreach (var pos in new[] { F.V(1.5f, .8f, 2.6f), F.V(1.5f, .8f, 3.5f), F.V(-1.5f, .8f, 4.6f), F.V(0, .8f, 2.6f) })
                F.Cilindro(v, "Moeda", "Mostarda", pos, F.V(.5f, .08f, .5f), F.V(90, 0, 0));
            P(v, "Personagens/Familia_FilhoMaisNovo_Animado", F.V(0, .15f, 1.8f), 0, 1.15f);
        }

        // ------------------------------------------------------------------ áreas dos verbetes do Glotrânsito

        // Quatro áreas ligadas a um grupo de verbetes, cada uma no lugar da superquadra que combina com a mecânica:
        // a Oficina no comércio local, o Cruzamento Central num cruzamento do Eixinho, a Rua Viva na ciclovia e no
        // ponto de ônibus e as Obras na borda da quadra, na beira do lago. O caos delas não depende de minigame:
        // melhora por faixas conforme os verbetes do grupo são desbloqueados (ChaosController).
        class AreaDeVerbetes
        {
            public string objeto, nome, areaName, cena, marcador;
            public bool emBreve;
            public GrupoDeVerbete[] grupos;
            public Vector3 centro, luz;
            public float giro, zDoLetreiro;
            public System.Action<Transform, Transform[], Transform[]> montar;
        }

        static readonly AreaDeVerbetes[] AreasDeVerbetes =
        {
            new AreaDeVerbetes { objeto = "Area_Oficina", nome = "Oficina", areaName = "Oficina de Veículos", cena = "Minigame_Oficina",
                grupos = new[] { GrupoDeVerbete.Veiculos }, centro = new Vector3(40f, .5f, -38.5f), giro = 180f, marcador = "Laranja",
                zDoLetreiro = 3f, luz = new Vector3(0, 4f, 7f), montar = Oficina },
            new AreaDeVerbetes { objeto = "Area_ObrasDaCidade", nome = "Obras da Cidade", areaName = ObrasConstrutor.NomeDaArea, cena = ObrasConstrutor.NomeDaCena,
                grupos = new[] { GrupoDeVerbete.ViasEEstruturas }, centro = new Vector3(81.5f, .5f, -6f), giro = 90f, marcador = "Mostarda",
                zDoLetreiro = 3.6f, luz = new Vector3(0, 7f, 7f), montar = ObrasDaCidade },
            new AreaDeVerbetes { objeto = "Area_CruzamentoCentral", nome = "Cruzamento Central", areaName = "Cruzamento Central", cena = SinaisConstrutor.NomeDaCena,
                grupos = new[] { GrupoDeVerbete.Sinais }, centro = new Vector3(30.5f, .5f, -5.5f), giro = -90f, marcador = "Vermelho",
                zDoLetreiro = 3.6f, luz = new Vector3(0, 7f, 8f), montar = CruzamentoCentral },
            new AreaDeVerbetes { objeto = "Area_RuaViva", nome = "Rua Viva", areaName = RuaVivaCarroConstrutor.NomeDaArea, cena = RuaVivaCarroConstrutor.NomeDaCena,
                grupos = new[] { GrupoDeVerbete.PessoasESeguranca, GrupoDeVerbete.Situacao }, centro = new Vector3(30.5f, .5f, 16f), giro = -90f, marcador = "Rosa",
                zDoLetreiro = 3f, luz = new Vector3(0, 6f, 7f), montar = RuaViva },
        };

        static void MontarAreaDeVerbetes(AreaDeVerbetes b, SaturacaoDaArea.ParDeTextura[] texturasCinza)
        {
            var velha = GameObject.Find(b.objeto);
            if (velha != null) Object.DestroyImmediate(velha);

            var area = F.Grupo(b.objeto, null, b.centro, F.V(0, b.giro, 0));
            var gatilho = area.AddComponent<BoxCollider>();
            gatilho.isTrigger = true;
            gatilho.size = new Vector3(2.6f, 2f, 2.6f);
            var entrada = area.AddComponent<AreaTrigger>();
            entrada.areaName = b.areaName;
            entrada.cenaDoMinigame = b.cena;
            entrada.emBreve = b.emBreve;

            // O pivô fica 0,5 m acima do chão, como nas outras áreas; os grupos descem pra y = 0.
            var visual = F.Grupo("Visual", area.transform, F.V(0, -.5f, 0)).transform;
            F.Cilindro(visual, "Marcador_Borda", "Branco", F.V(0, .12f, 0), F.V(3.1f, .04f, 3.1f));
            F.Cilindro(visual, "Marcador", b.marcador, F.V(0, .13f, 0), F.V(2.6f, .05f, 2.6f));
            var caos = new Transform[4];
            var ordem = new Transform[4];
            string[] oQueSome = { "Lixo", "Lama_E_Secas", "Tombados", "Entulho" };
            string[] oQueEntra = { "Flores", "Arvores", "No_Lugar", "Passaros" };
            for (int i = 0; i < 4; i++)
            {
                caos[i] = F.Grupo($"Caos_{i + 1}_{oQueSome[i]}", area.transform, F.V(0, -.5f, 0)).transform;
                ordem[i] = F.Grupo($"Ordem_{i + 1}_{oQueEntra[i]}", area.transform, F.V(0, -.5f, 0)).transform;
            }
            b.montar(visual, caos, ordem);

            var saturacao = area.AddComponent<SaturacaoDaArea>();
            saturacao.raiz = visual;
            saturacao.saturacaoSemCompletar = 0f;
            saturacao.texturasCinza = texturasCinza;

            // Luz só da área: apagada com 0% e cheia com 100%.
            var luz = F.Grupo("Luz", area.transform, b.luz).AddComponent<Light>();
            luz.type = LightType.Point;
            luz.range = 20f;
            luz.color = new Color(1f, .93f, .75f);
            luz.shadows = LightShadows.None;
            luz.intensity = 0f;

            var controle = area.AddComponent<ChaosController>();
            controle.nomeDaArea = b.nome;
            controle.grupos = b.grupos;
            controle.caosPorFaixa = System.Array.ConvertAll(caos, t => t.gameObject);
            controle.ordemPorFaixa = System.Array.ConvertAll(ordem, t => t.gameObject);
            controle.saturacao = saturacao;
            controle.luzDaArea = luz;
            controle.intensidadeDaLuzCheia = 2.5f;
            // Na cena salva a área fica no estado inicial do jogo: tudo bagunçado.
            foreach (var t in ordem) t.gameObject.SetActive(false);

            var raiz = PlacaDoLetreiro(area.transform, b.zDoLetreiro, out Text titulo, out Text linha);
            // Placa maior que a dos minigames, com a contagem de verbetes bem legível.
            raiz.localScale = Vector3.one * 1.25f;
            linha.fontSize = 34;
            linha.resizeTextForBestFit = true; linha.resizeTextMinSize = 22; linha.resizeTextMaxSize = 34;
            linha.horizontalOverflow = HorizontalWrapMode.Wrap;
            titulo.text = b.nome;
            titulo.resizeTextForBestFit = true; titulo.resizeTextMinSize = 30; titulo.resizeTextMaxSize = 46;
            titulo.horizontalOverflow = HorizontalWrapMode.Wrap;
            var letreiro = raiz.gameObject.AddComponent<LetreiroDeVerbetes>();
            letreiro.controle = controle;
            letreiro.textoDoNome = titulo;
            letreiro.textoDoProgresso = linha;
            if (b.emBreve) letreiro.formato = "{0} de {1} verbetes - em breve";
            linha.text = string.Format(letreiro.formato, 0, 0);
            EditorUtility.SetDirty(area);
        }

        // Veículo virado de lado (a enchente arrastou).
        static GameObject Virar(GameObject go, float graus = 96f)
        {
            go.transform.Rotate(0, 0, graus, Space.Self);
            return go;
        }

        // Nas funções abaixo: v = Visual (sempre; cinza -> colorido), c[0..3] = caos que some a cada faixa,
        // o[0..3] = ordem que entra no lugar. Em todas: 1 = lixo -> flores; 2 = lama e plantas secas -> árvores;
        // 3 = veículos virados e placas caídas -> tudo no lugar; 4 = entulho -> pássaros e o toque final.
        // Posições no espaço da área (girada): +Z = pra onde fica o cenário, -Z = de onde o jogador chega.
        // Um carro andando pro norte do mundo tem giro local = -(giro da área).

        // Oficina (giro 180, cenário ao sul): marcador na varanda do comércio local; a oficina é a loja maior da
        // fileira, com duas vagas abertas pra varanda (galpão de z = 4 a 12). Atrás do jogador, a rua da entrequadra.
        static void Oficina(Transform v, Transform[] c, Transform[] o)
        {
            // Não há prefab de galpão: paredes, teto e faixa da fachada são formas da paleta.
            F.Cubo(v, "Galpao_Fundo", "Branco_Concreto", F.V(0, 2.4f, 12.1f), F.V(14.9f, 4.8f, .3f));
            foreach (float x in new[] { -7.3f, 7.3f })
                F.Cubo(v, "Galpao_Lado", "Branco_Concreto", F.V(x, 2.4f, 8.1f), F.V(.3f, 4.8f, 8.3f));
            F.Cubo(v, "Galpao_Teto", "Petroleo", F.V(0, 4.95f, 8.1f), F.V(15.6f, .3f, 8.9f));
            F.Cubo(v, "Galpao_Faixa", "Laranja", F.V(0, 4.3f, 4.1f), F.V(14.9f, 1f, .3f));
            F.Cubo(v, "Galpao_Pilar", "Branco_Concreto", F.V(0, 1.9f, 4.1f), F.V(.4f, 3.8f, .4f));
            F.Cubo(v, "Galpao_Piso", "Cinza_Escuro", F.V(0, .1f, 8.1f), F.V(14.3f, .04f, 8f));
            P(v, "Oficina/Bancada", F.V(-2.7f, .12f, 10.6f), 180);
            P(v, "Oficina/Prateleira", F.V(-5.8f, .12f, 11.3f), 180);
            P(v, "Oficina/Prateleira", F.V(5.8f, .12f, 11.3f), 180);
            P(v, "Oficina/Pronto_Caminhao_Trator", F.V(3.4f, .12f, 8.4f), 180);
            P(v, "Props/Barril", F.V(6.4f, .12f, 5.4f));
            P(v, "Props/Caixa_Papelao", F.V(-.9f, .12f, 11f), 15);
            for (int i = 0; i < 3; i++)
                F.Cilindro(v, "Pneu_" + (i + 1), "Grafite", F.V(-6.2f, .28f + i * .3f, 5.6f), F.V(1f, .28f, 1f));

            P(c[0], "Enchente/Papeis_Espalhados", F.V(-1.5f, .11f, 1.2f), 30, 1.3f);
            P(c[0], "Lixo_Reciclagem/Pilha_De_Lixo", F.V(9f, .1f, 1.2f), 200);
            P(c[0], "Lixo_Reciclagem/Saco_De_Lixo_Preto_1", F.V(-5f, .1f, .8f));
            P(c[0], "Lixo_Reciclagem/Lata_Amassada", F.V(2.5f, .1f, 1.5f), 40, 2.4f);
            P(c[0], "Lixo_Reciclagem/Garrafa_Pet_Amassada", F.V(-8f, .1f, -1f), 100, 2.2f);

            P(c[1], "Enchente/Chao_De_Lama", F.V(3.5f, .11f, 6f), 20, .7f);
            P(c[1], "Enchente/Chao_De_Lama", F.V(-6f, .03f, -4f), 70, .6f);
            P(c[1], "Enchente/Arvore_Seca_1", F.V(-10.6f, .1f, 2.4f), 40, .9f);
            P(c[1], "Enchente/Arbusto_Seco", F.V(10.6f, .1f, 2.2f), 0, .8f);

            Virar(P(c[2], "Carros/Carro_Hatch", F.V(-8f, 1.05f, -4.5f), 60));
            Virar(P(c[2], "Carros/Moto", F.V(-3f, .47f, 7f), 110), 84);
            Virar(P(c[2], "Carros/Bicicleta_Q1", F.V(5f, .3f, 5.4f), 30), 82);
            P(c[2], "Props/Cone_Caido", F.V(0, .1f, 2.6f), 70);

            P(c[3], "Enchente/Entulho_De_Concreto", F.V(9.6f, .03f, -1.5f), 30, .45f);
            P(c[3], "Enchente/Cavalete_Medio_1", F.V(-2.4f, .12f, 5.6f), 200);
            P(c[3], "Enchente/Cone_Quebrado_2", F.V(-9f, .1f, 1.6f), 50);
            P(c[3], "Enchente/Bloco_Concreto_Lascado_1", F.V(3.4f, .12f, 10.4f), 20);

            P(o[0], "Natureza/Flores", F.V(-5f, .1f, .8f), 0, 1.4f);
            P(o[0], "Props/Vaso_De_Folhagem", F.V(9f, .1f, 1.2f), 180);
            P(o[1], "Natureza/Arvore_Simples_Grande", F.V(-10.6f, .1f, 2.4f), 0, .95f);
            P(o[1], "Natureza/Arbusto_2", F.V(10.6f, .1f, 2.2f), 0, .8f);
            P(o[2], "Oficina/Pronto_Quadriciclo", F.V(-3.6f, .12f, 7.6f), 160);
            P(o[2], "Oficina/Pronto_Motoneta", F.V(-5.6f, .12f, 6f), 180);
            P(o[2], "Carros/Bicicleta_Q1", F.V(5f, .12f, 5.2f), 180);
            P(o[2], "Carros/Carro_Hatch", F.V(-8f, .02f, -4.5f), 90);
            P(o[3], "Natureza/Passaro", F.V(-4f, 4.85f, 4.1f), 200, 1.6f);
            P(o[3], "Natureza/Passaro", F.V(5f, 4.85f, 4.1f), 150, 1.6f);
            P(o[3], "Props/Cone", F.V(-9f, .1f, 1.6f));
            P(o[3], "Props/Cone", F.V(9.6f, .1f, -1.5f));
        }

        // Obras (giro 90, cenário a leste): a borda da quadra na beira do lago. A enchente derrubou o píer;
        // com tudo arrumado, ele volta inteiro. A Ponte JK fica ao fundo, do outro lado do lago (HubZonas).
        // A margem fica em z = 2,5 (areia até 4,5) e a água começa em z = 4,5.
        static void ObrasDaCidade(Transform v, Transform[] c, Transform[] o)
        {
            P(v, "Natureza/Pedra_1", F.V(-6f, .03f, 3.6f), 20, 1.8f);
            P(v, "Natureza/Pedra_Musgo", F.V(6.5f, .03f, 3.8f), 80, 1.6f);
            P(v, "Natureza/Pedra_3", F.V(-3.4f, .03f, 4.4f), 140, 1.4f);
            P(v, "Natureza/Pedra_2", F.V(3.4f, .03f, 4.3f), 30, 1.4f);
            P(v, "Personagens/Mini_Homem_5", F.V(4.6f, .05f, 1.4f), 200, .9f);
            // Cabeceira do píer (fica sempre; o deck é que cai e volta).
            F.Caixa(v, "Cabeceira_Do_Pier", "Cinza", 0, 0, 4f, 4f, .5f, 1.4f);

            P(c[0], "Lixo_Reciclagem/Garrafa_Pet", F.V(-3f, .06f, 7f), 40, 2.4f).transform.Rotate(85, 0, 0);
            P(c[0], "Lixo_Reciclagem/Saco_De_Lixo_Verde", F.V(2f, .03f, 3.2f), 30);
            P(c[0], "Props/Caixa_Papelao", F.V(5f, .06f, 8f), 20, .8f).transform.Rotate(10, 0, -8);
            P(c[0], "Enchente/Papeis_Espalhados", F.V(-6f, .05f, 1.6f), 60, 1.2f);
            P(c[0], "Lixo_Reciclagem/Pilha_De_Lixo", F.V(6.6f, .03f, 1f), 160);

            P(c[1], "Enchente/Chao_De_Lama", F.V(0, .03f, 1.4f), 15, .7f);
            P(c[1], "Enchente/Chao_De_Lama", F.V(-5f, .03f, 2.6f), 80, .6f);
            P(c[1], "Enchente/Arvore_Seca_2", F.V(-9.5f, .02f, .5f), 20, .9f);
            P(c[1], "Enchente/Arvore_Seca_Grande", F.V(10f, .02f, 0f), 0, .7f);

            Tombar(P(c[2], "Props/Sinalizador_Obra", F.V(2.6f, .2f, 2.6f), 220));
            P(c[2], "Enchente/Cavalete_Grande_1", F.V(-3f, .02f, 2.2f), 30).transform.Rotate(0, 0, 80);
            P(c[2], "Props/Cone_Caido", F.V(1f, .02f, .6f), 100);
            Virar(P(c[2], "Carros/Carro_Picape", F.V(-8f, .98f, -2.5f), 20));

            // Pedaços do píer na água: tábuas boiando e estacas tortas.
            P(c[3], "Enchente/Pedacos_De_Rua", F.V(0, .06f, 9f), 90, .7f);
            F.Cubo(c[3], "Tabua_Boiando", "Marrom_Claro", F.V(-1.6f, .05f, 11f), F.V(.5f, .08f, 3f), F.V(4, 20, 6));
            F.Cubo(c[3], "Tabua_Boiando", "Marrom_Claro", F.V(2.2f, .05f, 13f), F.V(.5f, .08f, 2.6f), F.V(-3, -35, 4));
            F.Cubo(c[3], "Tabua_Boiando", "Marrom_Claro", F.V(.4f, .05f, 15.5f), F.V(.5f, .08f, 2.2f), F.V(5, 70, -4));
            foreach (var pos in new[] { F.V(-1.4f, .3f, 7.5f), F.V(1.4f, .3f, 10f), F.V(-1.4f, .3f, 12.5f) })
                F.Cilindro(c[3], "Estaca_Torta", "Marrom_Escuro", pos, F.V(.25f, 1.1f, .25f), F.V(14, pos.x * 30, 10));
            P(c[3], "Enchente/Barricada_De_Entulho", F.V(1f, .02f, 3.2f), 185);
            P(c[3], "Enchente/Bloco_Concreto", F.V(-2f, .02f, 2.4f), 30, .8f);

            P(o[0], "Natureza/Flores", F.V(6.6f, .03f, 1f), 0, 1.4f);
            P(o[0], "Natureza/Vitoria_Regia", F.V(-5f, .02f, 9f), 30, 1.5f);
            P(o[0], "Natureza/Vitoria_Regia", F.V(6f, .02f, 11f), 120, 1.3f);
            P(o[1], "Natureza/Arvore_Comum_2", F.V(-9.5f, .02f, .5f), 0, .8f);
            P(o[1], "Natureza/Arvore_Simples_Pequena", F.V(10f, .02f, 0f), 0, 1.2f);
            P(o[1], "Natureza/Grama_Alta", F.V(-5f, .03f, 2.6f), 0, 1.3f);
            P(o[2], "Props/Sinalizador_Obra", F.V(4.3f, .05f, 2.6f), 180);
            P(o[2], "Props/Cone", F.V(-4.3f, .05f, 2.6f));
            P(o[2], "Carros/Carro_Picape", F.V(-8f, .02f, -2.5f));
            // O píer de volta: deck de madeira sobre estacas, guarda-corpo e um mirante na ponta.
            F.Caixa(o[3], "Pier_Deck", "Marrom_Claro", 0, .35f, 11f, 3.2f, .2f, 13f);
            F.Caixa(o[3], "Pier_Mirante", "Marrom_Claro", 0, .35f, 18.5f, 6f, .2f, 3f);
            for (float z = 5.5f; z <= 19.5f; z += 2.5f)
                foreach (float x in new[] { -1.5f, 1.5f })
                    F.Cilindro(o[3], "Pier_Estaca", "Marrom_Escuro", F.V(x, .05f, z), F.V(.25f, 1.1f, .25f));
            foreach (float x in new[] { -1.55f, 1.55f })
                F.Caixa(o[3], "Pier_Corrimao", "Branco", x, .55f, 11f, .1f, .7f, 13f);
            F.Caixa(o[3], "Pier_Corrimao_Ponta", "Branco", 0, .55f, 20f, 6f, .7f, .1f);
            P(o[3], "Props/Banco_De_Praca", F.V(0, .55f, 19f), 0);
            P(o[3], "Natureza/Passaro", F.V(-1.55f, 1.25f, 9f), 160, 1.6f);
            P(o[3], "Natureza/Passaro", F.V(1.55f, 1.25f, 14f), 220, 1.6f);
        }

        // Cruzamento Central (giro -90, cenário a oeste): o marcador fica na calçada da quadra, de frente pro
        // cruzamento do Eixinho com a via de acesso da quadra. Ciclovia em z de 3,3 a 5,5, Eixinho de 6,5 a 14,5
        // (faixas em z = 8,5, sentido norte, e 12,5, sentido sul); a via de acesso ocupa x de 2,5 a 8,5.
        // No começo não há nenhum sinal de pé; com tudo arrumado, cada pista ganha o seu semáforo.
        static void CruzamentoCentral(Transform v, Transform[] c, Transform[] o)
        {
            P(v, "Props/Caixa_De_Energia", F.V(-2.6f, .08f, 1.4f), 180, 1.6f);
            P(v, "Carros/Carro_Taxi", F.V(-3f, .02f, 8.5f), 90);
            P(v, "Personagens/Mini_Mulher_3", F.V(.8f, .08f, 1.8f), 0, .9f);
            P(v, "Personagens/Mini_Homem_2", F.V(10.5f, .05f, 16f), 180, .9f);

            P(c[0], "Enchente/Papeis_Espalhados", F.V(.5f, .1f, -1.5f), 50, 1.3f);
            P(c[0], "Lixo_Reciclagem/Saco_De_Lixo_Laranja", F.V(-3.2f, .08f, .4f), 40);
            P(c[0], "Lixo_Reciclagem/Saco_De_Lixo_Preto_2", F.V(-2.6f, .05f, -2.6f));
            P(c[0], "Lixo_Reciclagem/Lata_Refrigerante", F.V(1.6f, .05f, -2.4f), 0, 2.4f);
            P(c[0], "Lixo_Reciclagem/Pilha_De_Lixo", F.V(-4.2f, .05f, -3.6f), 100);

            P(c[1], "Enchente/Chao_De_Lama", F.V(5.5f, .03f, 10.5f), 35, 1.2f);
            P(c[1], "Enchente/Chao_De_Lama", F.V(3f, .07f, 4.4f), 10, .6f);
            P(c[1], "Enchente/Arvore_Seca_1", F.V(-4f, .05f, -1f), 0, .9f);
            P(c[1], "Enchente/Arvore_Seca_2", F.V(-7f, .08f, 1.6f), 60, .9f);

            Tombar(P(c[2], "Props/Semaforo_Carros", F.V(1.8f, .3f, 7f), 110));
            Tombar(P(c[2], "Props/Semaforo_Carros", F.V(9.6f, .3f, 14.2f), 250));
            Tombar(P(c[2], "Props/Semaforo_Pedestres", F.V(9f, .28f, 2.2f), 240));
            P(c[2], "Props/Placa_FaixaDePedestre", F.V(-1.5f, .08f, 1.9f), 180, 1.15f).transform.Rotate(0, 0, 26);
            Virar(P(c[2], "Carros/Carro_Hatch", F.V(6.5f, 1.05f, 10.5f), 40));

            P(c[3], "Enchente/Barricada_De_Entulho", F.V(5.5f, .02f, 8.6f), 175);
            P(c[3], "Enchente/Cone_Quebrado_3", F.V(3.5f, .02f, 9.6f), 20);
            P(c[3], "Enchente/Bloco_Concreto", F.V(7.6f, .02f, 13.2f), 60, .8f);
            P(c[3], "Enchente/Cavalete_Medio_2", F.V(9.6f, .02f, 7f), 100);
            P(c[3], "Enchente/Entulho_De_Concreto", F.V(1.6f, .02f, 12.4f), 30, .45f);

            P(o[0], "Natureza/Flores", F.V(-3.2f, .08f, .4f), 0, 1.4f);
            P(o[0], "Natureza/Flores", F.V(1.6f, .05f, -2.4f), 0, 1.4f);
            P(o[0], "Props/Vaso_De_Laranjeira", F.V(-4.2f, .05f, -3.6f));
            P(o[1], "Natureza/Arvore_Simples_Grande", F.V(-4f, .05f, -1f), 0, .9f);
            P(o[1], "Natureza/Arvore_Simples_Pequena", F.V(-7f, .08f, 1.6f), 0, 1.1f);
            // Um semáforo virado pra cada pista que chega, e os de pedestre nas pontas da faixa da ciclovia.
            P(o[2], "Props/Semaforo_Carros", F.V(1.8f, .08f, 6.2f), -90);
            P(o[2], "Props/Semaforo_Carros", F.V(9.2f, .08f, 14.8f), 90);
            P(o[2], "Props/Semaforo_Carros", F.V(9.2f, .08f, 6.2f), 180);
            P(o[2], "Props/Semaforo_Carros", F.V(1.8f, .08f, 14.8f), 0);
            P(o[2], "Props/Semaforo_Pedestres", F.V(1.9f, .08f, 2f), 90);
            P(o[2], "Props/Semaforo_Pedestres", F.V(9.1f, .08f, 2f), -90);
            P(o[3], "Props/Placa_FaixaDePedestre", F.V(-1.5f, .08f, 1.9f), 180, 1.15f);
            P(o[3], "Natureza/Passaro", F.V(-2.6f, 1.45f, 1.4f), 200, 1.6f);
            P(o[3], "Natureza/Passaro", F.V(-1.6f, .08f, -3f), 40, 1.6f);
        }

        // Rua Viva (giro -90, cenário a oeste): da calçada da quadra se vê, de perto pra longe: a ciclovia com a
        // família (z de 3,3 a 5,5), o Eixinho engarrafado no sentido sul (z = 12,5) e o ponto de ônibus do outro
        // lado (z = 16), com o ônibus parado e os passageiros esperando.
        static void RuaViva(Transform v, Transform[] c, Transform[] o)
        {
            P(v, "Personagens/Familia_Pai_Animado", F.V(-3.4f, .06f, 4.4f), 90);
            P(v, "Carros/Bicicleta_Q1", F.V(-2.3f, .06f, 4.4f), -90);
            P(v, "Personagens/Familia_Mae_Animado", F.V(-5f, .06f, 4.2f), 90);
            P(v, "Personagens/Familia_Filha_Animado", F.V(3f, .08f, 1.6f), 200);
            P(v, "Personagens/Familia_FilhoMaisNovo_Animado", F.V(4f, .08f, 1.8f), 160);
            P(v, "Natureza/Cachorro_Caramelo", F.V(4.9f, .08f, 1.2f), 200);
            P(v, "Props/Banco", F.V(-6f, .05f, -.6f), 180);
            // Engarrafamento no sentido sul (giro local -90 = sul do mundo), com o ônibus parado no ponto.
            var fila = new (string carro, float x)[]
            {
                ("Carro_Sedan", -7.5f), ("Carro_Hatch", -2.9f), ("Onibus_Urbano", 4.6f), ("Carro_Van", 12.6f), ("Carro_Taxi", 17.2f),
                ("Carro_SUV", 21.8f), ("Carro_Compacto_Azul", 26.4f), ("Carro_Picape", 31f), ("Carro_Entregas", 35.6f),
            };
            foreach (var f in fila) P(v, "Carros/" + f.carro, F.V(f.x, .02f, 12.5f), -90);
            P(v, "Props/Ponto_De_Onibus", F.V(3f, .08f, 16.3f), 180);
            P(v, "Personagens/Idosa", F.V(2.2f, .08f, 17.2f), 180);
            P(v, "Personagens/Pedestre_Mulher", F.V(3.8f, .08f, 17.2f), 170);
            P(v, "Personagens/Mini_Homem_4", F.V(5.6f, .08f, 17f), 190, .9f);

            P(c[0], "Enchente/Papeis_Espalhados", F.V(-1f, .08f, 4.4f), 20, 1.3f);
            P(c[0], "Lixo_Reciclagem/Saco_De_Lixo_Preto_1", F.V(6.6f, .08f, .8f));
            P(c[0], "Lixo_Reciclagem/Casca_De_Banana", F.V(1.6f, .08f, 4.8f), 0, 2.6f);
            P(c[0], "Lixo_Reciclagem/Lixeira_Transbordando", F.V(-7.4f, .08f, 1.4f), 180);
            P(c[0], "Lixo_Reciclagem/Embalagem_Batata", F.V(-3f, .05f, -2f), 50, 2.4f);

            P(c[1], "Enchente/Chao_De_Lama", F.V(6f, .08f, 4.4f), 40, .5f);
            P(c[1], "Enchente/Chao_De_Lama", F.V(-5f, .05f, -2.6f), 0, .6f);
            P(c[1], "Enchente/Arvore_Seca_2", F.V(-7.6f, .05f, -3.6f), 30, .9f);
            P(c[1], "Enchente/Arbusto_Seco", F.V(7.6f, .05f, -3.6f), 0, .8f);

            Tombar(P(c[2], "Props/Placa_Ciclovia", F.V(-7f, .28f, 2.2f), 130, 1.15f));
            Tombar(P(c[2], "Props/Placa_De_Ponto_De_Onibus", F.V(7.4f, .26f, 16.8f), 215));
            Virar(P(c[2], "Carros/Bicicleta_Azul", F.V(1f, .3f, 4.4f), 70), 82);
            P(c[2], "Props/Pilarete", F.V(7.8f, .08f, 2.6f)).transform.Rotate(0, 0, 40);

            Tombar(P(c[3], "Enchente/Arvore_Seca_3", F.V(-9.5f, .42f, 3.8f), 15, .9f), 82);
            P(c[3], "Enchente/Cavalete_Pequeno_2", F.V(8.4f, .06f, 4.4f), 100);
            P(c[3], "Enchente/Entulho_De_Concreto", F.V(-7.4f, .08f, 16.8f), 100, .45f);
            P(c[3], "Enchente/Cone_Quebrado_1", F.V(2.2f, .08f, 2.4f), 60);

            P(o[0], "Natureza/Flores", F.V(6.6f, .08f, .8f), 0, 1.4f);
            P(o[0], "Props/Lixeira_De_Rua", F.V(-7.4f, .08f, 1.4f), 180);
            P(o[0], "Props/Floreira_De_Madeira", F.V(6f, .05f, -2.4f), 180);
            P(o[1], "Natureza/Arvore_Comum_1", F.V(-7.6f, .05f, -3.6f), 0, .7f);
            P(o[1], "Natureza/Arbusto_Frutinhas", F.V(7.6f, .05f, -3.6f), 0, .8f);
            P(o[2], "Props/Placa_Ciclovia", F.V(-7.8f, .08f, 2.6f), 180, 1.15f);
            P(o[2], "Props/Placa_De_Ponto_De_Onibus", F.V(.4f, .08f, 17f), 180);
            P(o[2], "Carros/Bicicleta_Azul", F.V(1f, .06f, 4.4f), 90);
            P(o[2], "Props/Pilarete", F.V(7.8f, .08f, 2.6f));
            P(o[3], "Natureza/Passaro", F.V(-6f, .6f, -.6f), 180, 1.6f);
            P(o[3], "Natureza/Passaro", F.V(-2f, .08f, 1f), 30, 1.6f);
            P(o[3], "Props/Vaso_De_Laranjeira", F.V(2.4f, .05f, -3.4f));
        }

        // ------------------------------------------------------------------ jogador

        // Troca a cápsula pelo filho mais velho animado. O CharacterController e os scripts continuam os mesmos.
        static void Jogador()
        {
            var jogador = GameObject.Find("Player");
            if (jogador == null) { Debug.LogError("[Hub] Player não encontrado."); return; }
            var velho = jogador.transform.Find("Visual");
            if (velho != null) Object.DestroyImmediate(velho.gameObject);

            // Começa no meio da superquadra, olhando pro norte (o bloco do norte e, ao fundo, o Museu).
            jogador.transform.SetPositionAndRotation(Inicio, Quaternion.identity);
            var retorno = GameObject.Find("PontoDeRetorno");
            if (retorno != null) retorno.transform.SetPositionAndRotation(Inicio, Quaternion.identity);
            var camera = Object.FindAnyObjectByType<CameraFollow>();
            if (camera != null) camera.transform.position = Inicio + new Vector3(0, 4f, -6f);
            var capsula = jogador.GetComponent<MeshRenderer>();
            if (capsula != null) capsula.enabled = false;
            var visual = P(jogador.transform, "Personagens/Familia_FilhoMaisVelho_Animado", F.V(0, -1f, 0));
            visual.name = "Visual";

            var anim = jogador.GetComponent<AnimacaoDoJogador>();
            if (anim == null) anim = jogador.AddComponent<AnimacaoDoJogador>();
            anim.animator = visual.GetComponentInChildren<Animator>();
            anim.velocidadeDeCorrida = jogador.GetComponent<PlayerMovement>().moveSpeed;
            LojaConstrutor.ConfigurarJogador(jogador, visual);
            EditorUtility.SetDirty(jogador);
        }

        // ------------------------------------------------------------------ texturas cinza

        // Pra cada textura usada nos prefabs das áreas, gera uma cópia em tons de cinza.
        // O SaturacaoDaArea troca uma pela outra enquanto a área não foi completada.
        static SaturacaoDaArea.ParDeTextura[] GerarTexturasCinza()
        {
            ArtePaleta.GarantirPasta(PastaCinza);
            var fontes = new List<string>
            {
                $"{ArtePaleta.PastaTexturas}/Atlas_Carros.png", $"{ArtePaleta.PastaTexturas}/Atlas_Comida.png",
                $"{ArtePaleta.PastaTexturas}/Atlas_Comercial.png", $"{ArtePaleta.PastaTexturas}/Fonte_KayKit.png",
                $"{ArtePaleta.PastaTexturas}/Fonte_MiniPersonagens.png",
            };
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Props/Texturas" }))
                fontes.Add(AssetDatabase.GUIDToAssetPath(guid));

            foreach (var fonte in fontes)
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(File.ReadAllBytes(fonte));
                var px = tex.GetPixels();
                for (int i = 0; i < px.Length; i++) px[i] = SaturacaoDaArea.Dessaturar(px[i], 0f);
                tex.SetPixels(px);
                tex.Apply();
                File.WriteAllBytes($"{PastaCinza}/{Path.GetFileName(fonte)}", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var pares = new List<SaturacaoDaArea.ParDeTextura>();
            foreach (var fonte in fontes)
            {
                string destino = $"{PastaCinza}/{Path.GetFileName(fonte)}";
                var origem = (TextureImporter)AssetImporter.GetAtPath(fonte);
                ArtePaleta.ConfigurarTextura(destino, origem.alphaIsTransparency, origem.mipmapEnabled, origem.maxTextureSize,
                    origem.textureCompression == TextureImporterCompression.Uncompressed);
                pares.Add(new SaturacaoDaArea.ParDeTextura
                {
                    colorida = AssetDatabase.LoadAssetAtPath<Texture2D>(fonte),
                    cinza = AssetDatabase.LoadAssetAtPath<Texture2D>(destino),
                });
            }
            return pares.ToArray();
        }
    }
}
