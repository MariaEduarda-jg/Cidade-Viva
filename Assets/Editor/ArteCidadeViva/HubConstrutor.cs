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
    // Monta a cena Hub como uma mini-Brasília com os prefabs de arte.
    // Não mexe na lógica: Player, câmera, PontoDeRetorno e as 9 Area_* (com AreaTrigger) continuam
    // os mesmos objetos, nas mesmas posições. Pode ser rodado de novo: apaga o que gerou e refaz.
    //
    // Convenções da cena: a câmera olha pra +Z, então "fundo" é +Z e a frente dos objetos aponta pra -Z.
    // Quarteirões de 13 m centrados em -20/0/20; ruas de 7 m entre eles (eixos em ±10 e ±30).
    public static class HubConstrutor
    {
        const string CaminhoDaCena = "Assets/Scenes/Hub.unity";
        const string PastaCinza = "Assets/Art/Texturas_Paleta/Cinza";
        static readonly float[] Quadras = { -20f, 0f, 20f };
        static readonly float[] EixosDeRua = { -30f, -10f, 10f, 30f };

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

            Chao(cidade);
            Ruas(cidade);
            BairrosNovos(cidade);
            Vizinhanca(cidade);
            Limites(cidade);

            // Versão final (2026-11-18): do centro antigo ficam só os dois "Extras" (Travessia Segura e
            // Corre, Correndo!). As áreas dos outros 7 minigames antigos saem da cena (os arquivos dos
            // minigames continuam no projeto) e o quarteirão de cada uma vira uma pracinha.
            foreach (string nome in AreasQueSairam)
            {
                var velha = GameObject.Find(nome);
                if (velha != null) Object.DestroyImmediate(velha);
            }
            var extras = new (string objeto, System.Action<Transform, Transform, Transform> montar, int indice, string cor)[]
            {
                ("Area_TravessiaSegura", TravessiaSegura, 1, "Petroleo_Claro"),
                ("Area_CorreCorrendo", CorreCorrendo, 3, "Laranja"),
            };
            foreach (var e in extras)
            {
                var area = GameObject.Find(e.objeto);
                if (area == null) { Debug.LogError("[Hub] Área não encontrada: " + e.objeto); continue; }
                MontarArea(area, e.montar, e.indice, e.cor, texturasCinza);
            }
            Pracinhas(cidade);
            TirarDoBuild();

            foreach (var area in AreasDeVerbetes) MontarAreaDeVerbetes(area, texturasCinza);

            Jogador();
            MenuConstrutor.PainelDoHub();
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

        // ------------------------------------------------------------------ base da cidade

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

        static void Chao(Transform cidade)
        {
            var chao = F.Grupo("Chao", cidade).transform;
            var terreno = GameObject.Find("Terreno");
            if (terreno != null) terreno.GetComponent<MeshRenderer>().sharedMaterial = ArtePaleta.Mat("Verde_Claro");
            F.Plano(chao, "Gramado_Externo", "Verde_Claro", F.V(0, -.03f, 0), 700f, 700f);
            F.Caixa(chao, "Asfalto", "Asfalto", 0, -.03f, 0, 72f, .04f, 72f);
            foreach (float x in Quadras)
                foreach (float z in Quadras)
                {
                    F.Caixa(chao, "Calcada", "Cinza_Claro", x, 0, z, 13f, .08f, 13f);
                    F.Caixa(chao, "Praca", "Areia", x, 0, z, 11.6f, .1f, 11.6f);
                }
            F.Caixa(chao, "Calcada_Norte", "Cinza_Claro", 0, 0, 35.5f, 74f, .08f, 3f);
            F.Caixa(chao, "Calcada_Sul", "Cinza_Claro", 0, 0, -35.5f, 74f, .08f, 3f);
            F.Caixa(chao, "Calcada_Leste", "Cinza_Claro", 35.5f, 0, 0, 3f, .08f, 68f);
            F.Caixa(chao, "Calcada_Oeste", "Cinza_Claro", -35.5f, 0, 0, 3f, .08f, 68f);
            ChaoDosBairrosNovos(chao);
            foreach (var r in chao.GetComponentsInChildren<MeshRenderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static bool PertoDeCruzamento(float v)
        {
            foreach (float e in EixosDeRua) if (Mathf.Abs(v - e) < 4.6f) return true;
            return false;
        }

        static void Ruas(Transform cidade)
        {
            var ruas = F.Grupo("Ruas", cidade).transform;
            var faixas = F.Grupo("Faixas_Tracejadas", ruas).transform;
            foreach (float eixo in EixosDeRua)
                for (float t = -33f; t <= 33f; t += 3f)
                {
                    if (PertoDeCruzamento(t)) continue;
                    F.Cubo(faixas, "Traco", "Branco", F.V(t, .016f, eixo), F.V(1.4f, .012f, .16f));
                    F.Cubo(faixas, "Traco", "Branco", F.V(eixo, .016f, t), F.V(.16f, .012f, 1.4f));
                }

            // Faixa de pedestre em frente a cada quarteirão, nas ruas de dentro.
            var travessias = F.Grupo("Faixas_De_Pedestre", ruas).transform;
            foreach (float eixo in new[] { -10f, 10f })
                foreach (float q in Quadras)
                    for (int i = 0; i < 7; i++)
                    {
                        float d = -2.7f + i * .9f;
                        F.Cubo(travessias, "Listra", "Branco", F.V(q, .018f, eixo + d), F.V(2.2f, .014f, .45f));
                        F.Cubo(travessias, "Listra", "Branco", F.V(eixo + d, .018f, q), F.V(.45f, .014f, 2.2f));
                    }
            foreach (var r in ruas.GetComponentsInChildren<MeshRenderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Trânsito parado, só pra cidade parecer viva.
            var transito = F.Grupo("Transito", cidade).transform;
            P(transito, "Carros/Onibus", F.V(-21f, .02f, -31.7f), 90);
            P(transito, "Carros/Carro_Hatch", F.V(4.5f, .02f, 8.3f), -90);
            P(transito, "Carros/Carro_Van", F.V(11.7f, .02f, -4f), 0);
            P(transito, "Carros/Ambulancia", F.V(-11.7f, .02f, 25f), 180);
            P(transito, "Carros/Carro_Taxi", F.V(25f, .02f, 28.3f), -90);
            P(transito, "Carros/Onibus_Escolar", F.V(31.7f, .02f, 17f), 0);
            P(transito, "Carros/Carro_SUV", F.V(-28.3f, .02f, -6f), 180);
            P(transito, "Carros/CarroKK_Perua", F.V(-5f, .02f, -11.7f), 90);
        }

        static void Vizinhanca(Transform cidade)
        {
            var v = F.Grupo("Vizinhanca", cidade).transform;

            // Fundo modernista (norte): o Congresso no fim do "eixo", catedral e museu dos lados.
            // A catedral e os blocos de superquadra ficam nos bairros novos (ver BairrosNovos).
            P(v, "Predios/Brasilia_Congresso", F.V(0, 0, 112f), 180);
            P(v, "Predios/Brasilia_Museu_Cupula", F.V(82f, 0, 50f), 180);
            P(v, "Predios/Brasilia_Palacio_Arcos", F.V(-112f, 0, 44f), 90);
            P(v, "Predios/Brasilia_Bloco_Superquadra", F.V(-64f, 0, 14f), 90);
            P(v, "Predios/Brasilia_Bloco_Superquadra", F.V(66f, 0, 22f), -90);
            P(v, "Predios/Arranha_Ceu_A", F.V(-74f, 0, 112f), 180);
            P(v, "Predios/Arranha_Ceu_D", F.V(-96f, 0, 96f), 180);
            P(v, "Predios/Predio_Comercial_Torre", F.V(100f, 0, 30f), 180);

            // Lado oeste: escola e casas. Lado leste: padaria e comércio. Todos virados pra cidade.
            P(v, "Predios/Escola", F.V(-46f, 0, -18f), 90);
            P(v, "Predios/Casa_01", F.V(-44f, 0, 2f), 90);
            P(v, "Predios/Casa_03", F.V(-44f, 0, 13f), 90);
            P(v, "Predios/Casa_06", F.V(-44f, 0, 24f), 90);
            P(v, "Predios/Padaria", F.V(45f, 0, -22f), -90);
            P(v, "Predios/Predio_Comercial_Loja", F.V(46f, 0, -9f), -90);
            P(v, "Predios/Predio_KayKit_A", F.V(46f, 0, 4f), 90);
            P(v, "Predios/Predio_KayKit_C", F.V(46f, 0, 14f), 90);
            P(v, "Props/Ponto_De_Onibus", F.V(38.6f, .08f, -30f), -90);
            P(v, "Predios/Casa_05", F.V(-22f, 0, -58f));
            P(v, "Predios/Casa_08", F.V(-8f, 0, -57f));
            P(v, "Predios/Casa_02", F.V(8f, 0, -58f));
            P(v, "Predios/Casa_07", F.V(22f, 0, -57f));

            // Árvores em volta (posições fixas por semente, pra cena não mudar a cada reconstrução).
            string[] arvores = { "Arvore_Comum_1", "Arvore_Comum_2", "Arvore_Comum_3", "Arvore_Betula", "Arvore_Salgueiro", "Palmeira_3" };
            var sorteio = new System.Random(7);
            for (int i = 0; i < 34; i++)
            {
                float ao_longo = -44f + (float)sorteio.NextDouble() * 88f;
                float faixa = 38.4f + (float)sorteio.NextDouble() * 2.8f;
                // Sem árvores no sul: a câmera fica 6 m atrás do jogador e elas tapariam a visão.
                int lado = i % 3 == 1 ? 3 : i % 3;
                // No norte, deixa o "eixo" livre pra ver o Congresso.
                if (lado == 0 && Mathf.Abs(ao_longo) < 12f) ao_longo = 14f * Mathf.Sign(ao_longo + .01f) + ao_longo * .5f;
                Vector3 pos = lado == 0 ? F.V(ao_longo, 0, faixa) : lado == 1 ? F.V(ao_longo, 0, -faixa)
                    : lado == 2 ? F.V(faixa, 0, ao_longo) : F.V(-faixa, 0, ao_longo);
                string arvore = arvores[sorteio.Next(arvores.Length)];
                int giro = sorteio.Next(360);
                float escala = .75f + (float)sorteio.NextDouble() * .35f;
                // Nada em cima das ruas novas nem tapando a câmera nelas.
                if (NosBairrosNovos(pos, 7f)) continue;
                P(v, "Natureza/" + arvore, pos, giro, escala);
            }

            // As plantas dos cantos de cada quarteirão ficam na própria área (CantosDoQuarteirao):
            // secas enquanto o minigame não foi completado, verdes depois.
            foreach (float x in new[] { -13.2f, 13.2f })
                foreach (float z in new[] { -13.2f, 13.2f })
                    P(v, "Props/PosteKK", F.V(x, .08f, z), x > 0 ? 180 : 0);
            P(v, "Props/Banco", F.V(-33.5f, .08f, 35.6f), 180);
            P(v, "Props/Lixeira_De_Rua", F.V(-31f, .08f, 35.8f), 180);
            P(v, "Personagens/Idosa", F.V(27f, .08f, 35.4f), 200);
            P(v, "Personagens/Trabalhador", F.V(-27f, .08f, 35.4f), 160);
            P(v, "Natureza/Cachorro_Husky", F.V(28.2f, .08f, 35.2f), 220);
        }

        // Paredes invisíveis em volta de tudo que é caminhável (a união dos retângulos de Caminhavel) e um chão
        // por retângulo, porque o Terreno só vai até ±50.
        static void Limites(Transform cidade)
        {
            var limites = F.Grupo("Limites", cidade).transform;
            const float c = .5f, x0 = -100f, z0 = -50f;
            const int nx = 400, nz = 300;
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
            go.AddComponent<BoxCollider>().size = tam;
        }

        // ------------------------------------------------------------------ versão final: extras e pracinhas

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

        // Os quarteirões que tinham as áreas antigas viram pracinhas (árvores, bancos, flores e um chafariz);
        // a do meio é a "Praça dos Extras", com a placa que aponta pros dois minigames que ficaram.
        static readonly Vector3[] LugaresDasPracinhas =
        {
            new Vector3(-20, 0, -20), new Vector3(20, 0, -20), new Vector3(20, 0, 0), new Vector3(20, 0, 20),
            new Vector3(-20, 0, 20), new Vector3(0, 0, 20), new Vector3(0, 0, 0),
        };

        static void Pracinhas(Transform cidade)
        {
            var raiz = F.Grupo("Pracinhas", cidade).transform;
            string[] arvores = { "Natureza/Arvore_Comum_1", "Natureza/Arvore_Comum_2", "Natureza/Arvore_Simples_Grande", "Natureza/Arvore_Betula" };
            for (int i = 0; i < LugaresDasPracinhas.Length; i++)
            {
                bool extras = i == LugaresDasPracinhas.Length - 1;
                var g = F.Grupo(extras ? "Praca_Dos_Extras" : "Pracinha_" + (i + 1), raiz, LugaresDasPracinhas[i]).transform;
                // Árvores só nos cantos de trás (a câmera fica atrás do jogador, ao sul).
                P(g, arvores[i % arvores.Length], F.V(-4.6f, .1f, 4.6f), i * 40, 1f);
                P(g, arvores[(i + 1) % arvores.Length], F.V(4.6f, .1f, 4.6f), i * 70, .95f);
                P(g, "Natureza/Arbusto_2", F.V(-4.8f, .1f, -4.6f), i * 30, .7f);
                P(g, "Natureza/Flores", F.V(4.6f, .1f, -4.6f), 0, 1.3f);
                P(g, "Natureza/Flores", F.V(-4.6f, .1f, 1f), 90, 1.1f);
                P(g, "Props/Banco_De_Praca", F.V(4.4f, .1f, 0), -90);
                P(g, "Props/Lixeira_De_Rua", F.V(4.6f, .1f, -2.2f));
                if (extras)
                {
                    PlacaDosExtras(g);
                    continue;
                }
                // Chafariz baixo no meio.
                F.Cilindro(g, "Chafariz_Borda", "Branco_Concreto", F.V(0, .3f, 0), F.V(3.2f, .4f, 3.2f));
                F.Cilindro(g, "Chafariz_Agua", "Agua", F.V(0, .48f, 0), F.V(2.8f, .06f, 2.8f));
                F.Cilindro(g, "Chafariz_Pilar", "Branco_Concreto", F.V(0, .8f, 0), F.V(.4f, .7f, .4f));
                P(g, "Props/Banco_De_Praca", F.V(-1.8f, .1f, -3.2f), 0);
            }
        }

        // Placa grande "Extras" no meio do centro, mais duas plaquinhas apontando pros dois minigames.
        static void PlacaDosExtras(Transform praca)
        {
            var placa = F.Grupo("Placa_Extras", praca, F.V(0, .5f, 1.5f)).transform;
            PlacaDoLetreiro(placa, 0, out Text titulo, out Text linha);
            titulo.text = "Extras";
            linha.text = "Mais dois jogos pra brincar!";
            var aoSul = F.Grupo("Placa_Travessia", praca, F.V(0, .5f, -4.2f)).transform;
            Placinha(aoSul, "Travessia Segura", 180f);
            var aOeste = F.Grupo("Placa_Corre", praca, F.V(-4.2f, .5f, -1.5f)).transform;
            Placinha(aOeste, "Corre, Correndo!", -90f);
        }

        // Plaquinha com uma seta: a seta aponta pra direção "giro" (0 = +Z, 90 = +X, 180 = -Z, -90/270 = -X).
        static void Placinha(Transform pai, string texto, float giro)
        {
            PlacaDoLetreiro(pai, 0, out Text titulo, out Text linha);
            titulo.text = texto;
            linha.text = "por aqui";
            var letreiro = pai.Find("Letreiro");
            letreiro.localScale = Vector3.one * .6f;
            // A seta fica no chão, na frente da plaquinha, apontando pro minigame.
            var seta = F.Grupo("Seta", pai, F.V(0, -.38f, -.9f), F.V(0, giro, 0)).transform;
            F.Cubo(seta, "Haste", "Mostarda", F.V(0, 0, -.25f), F.V(.35f, .05f, .9f));
            F.M(seta, "Ponta", "Cone", "Mostarda", F.V(0, 0, .35f), F.V(.8f, .5f, .05f), F.V(90, 0, 0));
        }

        // ------------------------------------------------------------------ áreas dos minigames

        // Cada área tem três grupos, todos com o chão em y = 0:
        //   Visual   - o cenário do minigame; sempre aparece, cinza até completar (SaturacaoDaArea).
        //   Caos     - o estrago da enchente (lama, entulho, lixo, placas caídas, árvores secas); some ao completar.
        //   Arrumado - o que volta pro lugar (placas, semáforos, árvores verdes, flores); só aparece ao completar.
        // Quem liga e desliga Caos/Arrumado é o CaosDaArea, chamado pelo AreaTrigger.
        static void MontarArea(GameObject area, System.Action<Transform, Transform, Transform> montar, int indice, string corDoMarcador,
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
            CantosDoQuarteirao(caos, arrumado);
            EstragosNaRua(caos, indice);

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

            Letreiro(area);
            EditorUtility.SetDirty(area);
        }

        // Deita o objeto no chão (placas, semáforos e árvores derrubados pela enchente).
        static GameObject Tombar(GameObject go, float graus = 84f)
        {
            go.transform.Rotate(graus, 0, 0, Space.Self);
            return go;
        }

        // Cantos de trás do quarteirão: árvore; cantos da frente: arbusto e flores. Secos até a área ser completada.
        static void CantosDoQuarteirao(Transform c, Transform a)
        {
            P(c, "Enchente/Arvore_Seca_1", F.V(-5.5f, .08f, 5.5f), 20, .9f);
            P(c, "Enchente/Arvore_Seca_2", F.V(5.5f, .08f, 5.5f), 200, .85f);
            P(c, "Enchente/Arbusto_Seco", F.V(-5.3f, .1f, -5.3f), 30, .7f);

            P(a, "Natureza/Arvore_Simples_Grande", F.V(-5.5f, .08f, 5.5f), 0, 1.1f);
            P(a, "Natureza/Arvore_Simples_Pequena", F.V(5.5f, .08f, 5.5f), 0, 1.2f);
            P(a, "Natureza/Arbusto_2", F.V(-5.3f, .1f, -5.3f), 30, .7f);
            P(a, "Natureza/Flores", F.V(5.2f, .1f, -5.2f), 0, 1.4f);
        }

        // Estragos nas ruas em volta do quarteirão (o eixo das ruas fica a 10 m do centro da área).
        // O mesmo arranjo é girado de 90 em 90 graus e troca de peças conforme a área, pra não ficar repetido.
        static void EstragosNaRua(Transform c, int n)
        {
            Vector3 Girado(float x, float y, float z)
            {
                var p = F.V(x, y, z);
                for (int i = 0; i < n % 4; i++) p = F.V(p.z, p.y, -p.x);
                return p;
            }
            string[] cavaletes = { "Cavalete_Medio_1", "Cavalete_Pequeno_1", "Cavalete_Medio_2" };
            string[] cones = { "Cone_Quebrado_1", "Cone_Quebrado_2", "Cone_Quebrado_3" };
            string[] sacos = { "Saco_De_Lixo_Preto_1", "Saco_De_Lixo_Laranja", "Saco_De_Lixo_Verde", "Saco_De_Lixo_Preto_2" };

            // A lama cobre a faixa de pedestre da rua ao lado.
            P(c, "Enchente/Chao_De_Lama", Girado(10f, .03f, 0), n * 37);
            P(c, "Enchente/" + cavaletes[n % 3], Girado(7.7f, .02f, 3.2f), 15 + n * 101);
            P(c, "Enchente/" + cones[n % 3], Girado(7.6f, .02f, -2.4f), n * 50);
            P(c, "Enchente/Papeis_Espalhados", Girado(-3f, .03f, -7.8f), n * 70, 1.3f);
            P(c, "Lixo_Reciclagem/" + sacos[n % 4], Girado(-7.5f, .02f, -3.4f), n * 40);
            if (n % 2 == 0) P(c, "Enchente/Bloco_Concreto_Lascado_" + (1 + n / 2 % 2), Girado(-3.8f, .02f, 7.6f), 20 + n * 30);
            else P(c, "Enchente/Entulho_De_Concreto", Girado(-3.8f, .02f, 7.6f), n * 60, .45f);
        }

        // Placa com o nome e o recorde. Fica fora do "Visual": sempre colorida, pra criança achar a área.
        static void Letreiro(GameObject area)
        {
            var raiz = PlacaDoLetreiro(area.transform, 6.15f, out Text titulo, out Text recorde);
            titulo.text = area.GetComponent<AreaTrigger>().areaName;
            recorde.text = "Venha jogar!";

            var letreiro = raiz.gameObject.AddComponent<LetreiroDaArea>();
            letreiro.area = area.GetComponent<AreaTrigger>();
            letreiro.textoDoNome = titulo;
            letreiro.textoDoRecorde = recorde;
        }

        // Dois postes, a placa e as duas linhas de texto (nome em cima, linha menor embaixo).
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

        // Nas funções abaixo: v = Visual (sempre), c = Caos (até completar), a = Arrumado (depois de completar).

        static void OndeEuVou(Transform v, Transform c, Transform a)
        {
            P(v, "Props/Caixa_Papelao", F.V(-2.6f, .1f, 1.7f), 20);
            P(v, "Props/Cone", F.V(2.5f, .1f, 1.6f));
            P(v, "Props/Cone_Caido", F.V(-4.3f, .1f, .2f), 40);

            // A enchente levou a sinalização: placas no chão ou tortas, nenhuma no lugar.
            Tombar(P(c, "Props/Placa_Pare", F.V(-3.2f, .28f, 4.6f), 205, 1.15f));
            Tombar(P(c, "Props/Placa_Escola", F.V(1.6f, .28f, 5.2f), 140, 1.15f));
            P(c, "Props/Placa_Ciclovia", F.V(3.6f, .1f, 3.6f), 180, 1.15f).transform.Rotate(0, 0, 24);
            Tombar(P(c, "Props/Semaforo_Carros", F.V(5.2f, .3f, 3.4f), 250));
            P(c, "Enchente/Cone_Quebrado_1", F.V(4.4f, .1f, 1.2f), 30);
            P(c, "Enchente/Papeis_Espalhados", F.V(-.8f, .11f, 3.3f), 40, 1.2f);
            P(c, "Enchente/Triangulo_De_Sinalizacao", F.V(-4.6f, .1f, 2.2f), 200);

            P(a, "Props/Placa_Pare", F.V(-3.6f, .1f, 3.6f), 180, 1.15f);
            P(a, "Props/Placa_FaixaDePedestre", F.V(-1.2f, .1f, 4.3f), 180, 1.15f);
            P(a, "Props/Placa_Escola", F.V(1.2f, .1f, 4.3f), 180, 1.15f);
            P(a, "Props/Placa_Ciclovia", F.V(3.6f, .1f, 3.6f), 180, 1.15f);
            P(a, "Props/Semaforo_Carros", F.V(5f, .1f, 1.6f), 180);
        }

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

        static void ReciclaComigo(Transform v, Transform c, Transform a)
        {
            string[] lixeiras = { "Lixeira_Papel", "Lixeira_Plastico", "Lixeira_Vidro", "Lixeira_Organico" };
            for (int i = 0; i < 4; i++)
                P(v, "Lixo_Reciclagem/" + lixeiras[i], F.V(-2.7f + i * 1.8f, .1f, 3.8f), 180, 1.6f);

            // Lixo por todo lado, fora das lixeiras.
            P(c, "Lixo_Reciclagem/Garrafa_Plastica", F.V(-2.1f, .1f, 1.7f), 30, 2.4f);
            P(c, "Lixo_Reciclagem/Lata_Refrigerante", F.V(2.2f, .1f, 1.5f), 0, 2.4f);
            P(c, "Lixo_Reciclagem/Jornal", F.V(-3.4f, .1f, .3f), 25, 2.4f);
            P(c, "Lixo_Reciclagem/Casca_De_Banana", F.V(3.3f, .1f, .2f), 0, 2.6f);
            P(c, "Lixo_Reciclagem/Caixa_De_Leite", F.V(1.2f, .1f, 2.3f), 60, 2.2f);
            P(c, "Lixo_Reciclagem/Pote_De_Vidro_1", F.V(-.9f, .1f, 2.2f), 0, 2.2f);
            P(c, "Lixo_Reciclagem/Garrafa_Pet_Amassada", F.V(.4f, .1f, 1.1f), 110, 2.2f);
            P(c, "Lixo_Reciclagem/Papel_Amassado_1", F.V(-1.7f, .1f, .8f), 0, 2.4f);
            P(c, "Lixo_Reciclagem/Pilha_De_Lixo", F.V(4.7f, .1f, 4.6f), 200);
            P(c, "Lixo_Reciclagem/Lixeira_Transbordando", F.V(4.6f, .1f, 1.6f), 180);
            P(c, "Lixo_Reciclagem/Saco_De_Lixo_Preto_1", F.V(-4.7f, .1f, 2.8f));
            P(c, "Lixo_Reciclagem/Saco_De_Lixo_Laranja", F.V(-4.3f, .1f, 3.7f), 80);
            P(c, "Props/Saco_De_Lixo_A", F.V(4.9f, .1f, 2.8f));

            P(a, "Props/Vassoura", F.V(4.7f, .1f, 3.9f), 0).transform.Rotate(0, 0, -10);
            P(a, "Props/Vaso_De_Folhagem", F.V(-4.6f, .1f, 3.6f), 180);
            P(a, "Natureza/Flores", F.V(4.4f, .1f, 1.6f), 0, 1.3f);
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

        static void PedalSeguro(Transform v, Transform c, Transform a)
        {
            // Árvore caída e lama na ciclovia, placa no chão.
            Tombar(P(c, "Enchente/Arvore_Seca_3", F.V(-.4f, .45f, 1.9f), 10, .9f), 82);
            P(c, "Enchente/Chao_De_Lama", F.V(3.1f, .17f, 3.4f), 50, .5f);
            Tombar(P(c, "Props/Placa_Ciclovia", F.V(4.2f, .28f, 4.6f), 230));
            P(c, "Enchente/Cavalete_Pequeno_2", F.V(-4f, .1f, 5f), 170);

            P(a, "Props/Placa_Ciclovia", F.V(4.6f, .1f, 5f), 180);
            P(a, "Natureza/Flores", F.V(-4.4f, .1f, 5f), 0, 1.3f);

            F.Cubo(v, "Ciclovia", "Terracota", F.V(0, .13f, 3.4f), F.V(10.5f, .05f, 2.2f));
            for (int i = 0; i < 6; i++)
                F.Cubo(v, "Tracejado", "Branco", F.V(-4.4f + i * 1.75f, .16f, 3.4f), F.V(.9f, .01f, .1f));
            P(v, "Props/Bicicleta", F.V(-2.4f, .16f, 3.4f), 90, 1.1f);
            P(v, "Carros/Bicicleta_Q1", F.V(2.6f, .16f, 3.5f), -90, 1.1f);
            F.Cubo(v, "Pedestal", "Branco", F.V(0, .55f, 5f), F.V(.8f, .9f, .8f));
            F.M(v, "Capacete", "Semiesfera", "Mostarda", F.V(0, 1f, 5f), F.V(.7f, .7f, .85f));
            F.Cubo(v, "Capacete_Aba", "Mostarda", F.V(0, 1.03f, 4.6f), F.V(.55f, .05f, .3f));
            P(v, "Props/Pilarete", F.V(-4.9f, .1f, 1.9f));
            P(v, "Props/Pilarete", F.V(4.9f, .1f, 1.9f));
        }

        static void SobeSobe(Transform v, Transform c, Transform a)
        {
            F.Caixa(v, "Piscina_Borda", "Cinza", 0, .1f, 3.7f, 10f, .12f, 4.6f);
            F.Caixa(v, "Piscina_Fundo", "Petroleo_Claro", 0, .1f, 3.7f, 9.4f, .14f, 4f);
            var agua = F.Caixa(v, "Agua", "Agua", 0, .24f, 3.7f, 9.4f, .12f, 4f);
            agua.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < 6; i++)
            {
                float x = i % 2 == 0 ? 2.9f : 4.4f;
                F.Cubo(v, "Plataforma_" + (i + 1), "Marrom_Claro", F.V(x, .9f + i * .95f, 3.7f), F.V(1.8f, .22f, 1.1f));
                F.Cubo(v, "Plataforma_Grama_" + (i + 1), "Verde", F.V(x, 1.03f + i * .95f, 3.7f), F.V(1.8f, .06f, 1.1f));
            }
            P(v, "Personagens/Familia_Filha_Animado", F.V(4.4f, 2.01f, 3.7f), 180);
            // Móveis e lixo boiando na água.
            P(c, "Enchente/Mesa", F.V(-3.4f, .12f, 3.3f), 30, .9f).transform.Rotate(8, 0, 6);
            P(c, "Props/Caixa_Papelao", F.V(.4f, .2f, 3f), 20, .8f).transform.Rotate(10, 0, -8);
            P(c, "Enchente/Cadeira", F.V(-1.2f, .1f, 4.6f), 200).transform.Rotate(-12, 0, 20);
            P(c, "Lixo_Reciclagem/Saco_De_Lixo_Verde", F.V(-2.3f, .1f, 2.5f), 50);
            P(c, "Lixo_Reciclagem/Garrafa_Pet_Torcida", F.V(.9f, .3f, 4.6f), 0, 2.2f).transform.Rotate(80, 0, 0);
            P(c, "Enchente/Papeis_Espalhados", F.V(-.6f, .31f, 3.6f), 70);

            foreach (var pos in new[] { F.V(-3.3f, .31f, 3.1f), F.V(-1.5f, .31f, 4.5f), F.V(.5f, .31f, 2.8f) })
                P(a, "Natureza/Vitoria_Regia", pos, pos.x * 70, 1.4f);
        }

        static void SemaforoMagico(Transform v, Transform c, Transform a)
        {
            // O semáforo gigante fica torto até a área ser arrumada.
            P(c, "Props/Semaforo_Carros", F.V(.6f, .1f, 4.4f), 180, 1.8f).transform.Rotate(0, 0, 17);
            P(c, "Enchente/Defensa_Metalica_2", F.V(-3.2f, .1f, 5.4f), 15);
            P(c, "Enchente/Triangulo_De_Sinalizacao", F.V(2.6f, .1f, 3.9f), 190);
            P(c, "Enchente/Cone_Obra_2", F.V(3.5f, .1f, 5.2f));
            P(a, "Props/Semaforo_Carros", F.V(0, .1f, 4.4f), 180, 1.8f);

            string[] luzes = { "Luz_Vermelha", "Luz_Amarela", "Luz_Verde" };
            for (int i = 0; i < 3; i++)
            {
                F.Cilindro(v, "Disco_Borda_" + (i + 1), "Branco", F.V(-2.4f + i * 2.4f, .12f, 2.4f), F.V(1.7f, .04f, 1.7f));
                F.Cilindro(v, "Disco_" + (i + 1), luzes[i], F.V(-2.4f + i * 2.4f, .13f, 2.4f), F.V(1.45f, .05f, 1.45f));
            }
            P(v, "Carros/Carro_Taxi", F.V(-4.1f, .1f, 4.2f), 150, .8f);
            P(v, "Personagens/Mini_Homem_3", F.V(3.9f, .1f, 4f), 200, .9f);
        }

        static void GuardaOSinal(Transform v, Transform c, Transform a)
        {
            P(v, "Ruas/Rua_Cruzamento_ComFaixas", F.V(0, .1f, 3.4f), 0, .75f);

            // Cruzamento sem sinal: um semáforo no chão e a passagem cheia de obstáculos.
            Tombar(P(c, "Props/Semaforo_Carros", F.V(-3.4f, .3f, 1.1f), 100));
            P(c, "Enchente/Cavalete_Medio_2", F.V(.3f, .22f, 1.4f), 25, .8f);
            P(c, "Enchente/Cone_Obra_1", F.V(2.9f, .1f, 5.9f));
            P(c, "Enchente/Cone_Quebrado_1", F.V(-2.9f, .1f, 5.9f), 60);
            P(c, "Enchente/Bloco_Concreto", F.V(3.9f, .1f, 2.6f), 40, .8f);

            P(a, "Props/Semaforo_Carros", F.V(-2.75f, .1f, .85f), 180);
            P(a, "Props/Semaforo_Carros", F.V(2.75f, .1f, 5.9f), 90);

            P(v, "Carros/CarroKK_Taxi", F.V(-1.9f, .23f, 2.9f), 90, .65f);
            P(v, "Carros/Carro_Hatch", F.V(.7f, .23f, 5.2f), 180, .65f);
            P(v, "Personagens/Trabalhadora", F.V(2.6f, .16f, 1f), 190);
        }

        static void OlhoVivo(Transform v, Transform c, Transform a)
        {
            P(c, "Enchente/Barricada_De_Entulho", F.V(-.4f, .1f, 5.5f), 180, .75f);
            P(c, "Enchente/Papeis_Espalhados", F.V(-.7f, .11f, 3.3f), 20, 1.2f);
            P(c, "Lixo_Reciclagem/Saco_De_Lixo_Preto_2", F.V(-5.3f, .1f, 3.4f));
            P(c, "Lixo_Reciclagem/Caixa_Com_Garrafas", F.V(1.2f, .1f, 2.9f), 30, 1.3f);
            P(c, "Lixo_Reciclagem/Casca_De_Banana", F.V(-1.8f, .1f, 1.8f), 0, 2.6f);

            P(a, "Natureza/Flores", F.V(-1.8f, .1f, 1.8f), 0, 1.2f);
            P(a, "Props/Vaso_De_Folhagem", F.V(1.3f, .1f, 4.9f), 180);

            // Lupa gigante.
            F.Cilindro(v, "Lupa_Aro", "Mostarda", F.V(3.6f, 2.3f, 4.2f), F.V(2.6f, .2f, 2.6f), F.V(90, 0, 0));
            F.Cilindro(v, "Lupa_Lente", "Vidro", F.V(3.6f, 2.3f, 4.2f), F.V(2.2f, .24f, 2.2f), F.V(90, 0, 0));
            F.Barra(v, "Lupa_Cabo", "Marrom", F.V(4.55f, 1.35f, 4.2f), F.V(5.5f, .15f, 4.2f), .28f);
            P(v, "Props/Banco", F.V(-3.3f, .1f, 3.9f), 180);
            P(v, "Natureza/Cachorro_Caramelo", F.V(2.9f, .1f, 2.1f), 200);
            P(v, "Props/Lixeira_De_Rua", F.V(-5.2f, .1f, 4.4f), 180);
            P(v, "Personagens/Mini_Mulher_2", F.V(-4.6f, .1f, 2.2f), 150, .9f);
        }

        // ------------------------------------------------------------------ bairros novos (ao norte da cidade)

        // Por onde o jogador anda: o centro antigo e, ao norte, o Eixo, a Avenida e os lugares das áreas dos
        // verbetes. Os lados são o meio das paredes invisíveis (múltiplos de 0,5 m). As paredes e o chão
        // saem daqui (ver Limites), então pra abrir um caminho novo basta acrescentar um retângulo.
        static readonly Rect[] Caminhavel =
        {
            Rect.MinMaxRect(-37.5f, -37.5f, 37.5f, 37.5f),   // centro (os 9 quarteirões)
            Rect.MinMaxRect(-12.5f, 37.5f, 12.5f, 66.5f),    // Eixo, até o cruzamento com a Avenida
            Rect.MinMaxRect(-88.5f, 51f, 60.5f, 66.5f),      // Avenida, com a ciclovia do lado sul
            Rect.MinMaxRect(-54.5f, 41.5f, -35.5f, 51f),     // pracinha da Rua Viva
            Rect.MinMaxRect(38.5f, 66.5f, 53.5f, 75.5f),     // rua que acaba no rio (Obras da Cidade)
            Rect.MinMaxRect(-88.5f, 66.5f, -63.5f, 77f),     // pátio da Oficina
        };

        static bool NosBairrosNovos(Vector3 pos, float margem)
        {
            for (int i = 1; i < Caminhavel.Length; i++)
            {
                Rect r = Caminhavel[i];
                if (pos.x > r.xMin - margem && pos.x < r.xMax + margem && pos.z > r.yMin - margem && pos.z < r.yMax + margem) return true;
            }
            return false;
        }

        // Chão dos bairros novos: o Eixo (duas pistas com calçadão no meio, do centro até o Congresso),
        // a Avenida que cruza o Eixo, a ciclovia, a pracinha, o pátio da oficina e a rua que acaba no rio.
        static void ChaoDosBairrosNovos(Transform chao)
        {
            // Eixo: pistas em x = ±7, calçadão de 8 m no meio, calçadas por fora.
            foreach (float lado in new[] { -1f, 1f })
            {
                F.Caixa(chao, "Eixo_Pista", "Asfalto", lado * 7f, -.03f, 63.5f, 6f, .04f, 53f);
                // A pista passa por cima da calçada norte do centro pra encontrar a rua de lá.
                F.Caixa(chao, "Eixo_Ligacao", "Asfalto", lado * 7f, 0, 35.5f, 6f, .085f, 3.2f);
                F.Caixa(chao, "Eixo_Calcada", "Cinza_Claro", lado * 11f, 0, 45.5f, 2f, .08f, 17f);
                F.Caixa(chao, "Eixo_Calcada", "Cinza_Claro", lado * 11f, 0, 78f, 2f, .08f, 24f);
            }
            foreach (var (z, comprimento) in new[] { (46.5f, 19f), (77f, 26f) })
            {
                F.Caixa(chao, "Eixo_Calcadao", "Cinza_Claro", 0, 0, z, 8f, .08f, comprimento);
                F.Caixa(chao, "Eixo_Calcadao_Piso", "Areia", 0, 0, z, 6.6f, .1f, comprimento - .7f);
            }

            // Avenida: de oeste (oficina) a leste, cruzando o Eixo em z = 60.
            F.Caixa(chao, "Avenida", "Asfalto", -14f, -.03f, 60f, 149f, .04f, 8f);
            foreach (float z in new[] { 55f, 65f })
            {
                F.Caixa(chao, "Avenida_Calcada", "Cinza_Claro", -50.25f, 0, z, 76.5f, .08f, 2f);
                F.Caixa(chao, "Avenida_Calcada", "Cinza_Claro", 36.25f, 0, z, 48.5f, .08f, 2f);
            }

            // Ciclovia ao sul da Avenida, do Eixo até o fim (lado oeste).
            F.Caixa(chao, "Ciclovia", "Terracota", -50.5f, 0, 52.8f, 75f, .06f, 2.2f);
            for (float x = -87f; x < -13.5f; x += 1.75f)
                F.Cubo(chao, "Ciclovia_Tracejado", "Branco", F.V(x, .066f, 52.8f), F.V(.9f, .01f, .1f));

            // Pracinha da Rua Viva.
            F.Caixa(chao, "Praca_Rua_Viva", "Cinza_Claro", -45f, 0, 46.8f, 18f, .08f, 9.8f);
            F.Caixa(chao, "Praca_Rua_Viva_Piso", "Areia", -45f, 0, 46.6f, 16.6f, .1f, 8.6f);

            // Pátio da oficina.
            F.Caixa(chao, "Patio_Da_Oficina", "Cinza_Claro", -76f, 0, 75.6f, 24.6f, .08f, 19.2f);
            F.Caixa(chao, "Patio_Da_Oficina_Piso", "Cinza", -76f, 0, 75.6f, 23.4f, .1f, 18f);

            // Rua que sai da Avenida e acaba no rio; do outro lado ela continua até o túnel do morro.
            F.Caixa(chao, "Rua_Do_Rio", "Asfalto", 46f, -.03f, 70.3f, 7f, .04f, 10.6f);
            F.Caixa(chao, "Rua_Do_Rio_Outra_Margem", "Asfalto", 46f, -.03f, 90.5f, 7f, .04f, 12f);
            foreach (float x in new[] { 41.75f, 50.25f })
            {
                F.Caixa(chao, "Rua_Do_Rio_Calcada", "Cinza_Claro", x, 0, 70.5f, 1.5f, .08f, 9f);
                F.Caixa(chao, "Rua_Do_Rio_Calcada", "Cinza_Claro", x, 0, 90.5f, 1.5f, .08f, 12f);
            }

            // Rio: nasce num laguinho perto do Eixo e corre pro leste.
            F.Caixa(chao, "Rio", "Agua", 66f, -.02f, 80f, 88f, .04f, 8f);
            F.Cilindro(chao, "Rio_Laguinho", "Agua", F.V(22f, 0, 80f), F.V(13f, .04f, 11f));
            foreach (float z in new[] { 75.4f, 84.6f })
                F.Caixa(chao, "Rio_Margem", "Areia", 66f, -.02f, z, 88f, .035f, 1.3f);
            // Capim, plantas e pedras nas margens, vitórias-régias e reflexos na água (fora do trecho da ponte).
            ArteDetalhes.MargemDeRio(chao, "Margem_Sul_Do_Rio", 24f, 108f, 75.4f, 1f, .02f, 21, 40f, 52f);
            ArteDetalhes.MargemDeRio(chao, "Margem_Norte_Do_Rio", 24f, 108f, 84.6f, -1f, .02f, 22, 40f, 52f);
        }

        // Pintura das ruas novas e o que há em volta delas (prédios, árvores, postes).
        static void BairrosNovos(Transform cidade)
        {
            var pintura = F.Grupo("Pintura_Dos_Bairros_Novos", cidade).transform;
            for (float z = 39f; z <= 89f; z += 3f)
            {
                if (z > 51f && z < 69f) continue;
                F.Cubo(pintura, "Traco", "Branco", F.V(-7f, .016f, z), F.V(.16f, .012f, 1.4f));
                F.Cubo(pintura, "Traco", "Branco", F.V(7f, .016f, z), F.V(.16f, .012f, 1.4f));
            }
            for (float x = -87f; x <= 59f; x += 3f)
            {
                if (Mathf.Abs(x) < 15f) continue;
                F.Cubo(pintura, "Traco", "Branco", F.V(x, .016f, 60f), F.V(1.4f, .012f, .16f));
            }
            // Faixas de pedestre do cruzamento: no calçadão (atravessando a Avenida) e nas duas pistas do Eixo.
            for (int i = 0; i < 8; i++)
                F.Cubo(pintura, "Listra", "Branco", F.V(0, .018f, 56.85f + i * .9f), F.V(6.4f, .014f, .45f));
            foreach (float z in new[] { 53.2f, 66.8f })
                foreach (float lado in new[] { -1f, 1f })
                    for (int i = 0; i < 6; i++)
                        F.Cubo(pintura, "Listra", "Branco", F.V(lado * (4.75f + i * .9f), .018f, z), F.V(.45f, .014f, 2.2f));
            // Linha de parada antes de cada faixa.
            F.Cubo(pintura, "Parada", "Branco", F.V(7f, .018f, 51.4f), F.V(5.6f, .014f, .3f));
            F.Cubo(pintura, "Parada", "Branco", F.V(-7f, .018f, 68.6f), F.V(5.6f, .014f, .3f));
            F.Cubo(pintura, "Parada", "Branco", F.V(-11.6f, .018f, 58f), F.V(.3f, .014f, 3.6f));
            F.Cubo(pintura, "Parada", "Branco", F.V(11.6f, .018f, 62f), F.V(.3f, .014f, 3.6f));
            foreach (var r in pintura.GetComponentsInChildren<MeshRenderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var v = F.Grupo("Bairros_Novos", cidade).transform;

            // Eixo: palmeiras nas calçadas de fora e postes no calçadão.
            foreach (float lado in new[] { -1f, 1f })
            {
                foreach (float z in new[] { 40f, 47f, 72f, 79f, 86f })
                    P(v, "Natureza/Palmeira_1", F.V(lado * 11.2f, .08f, z), lado > 0 ? 40 : 200);
                P(v, "Props/PosteKK", F.V(lado * 3.6f, .1f, 44f), lado > 0 ? 180 : 0);
                P(v, "Props/PosteKK", F.V(lado * 3.6f, .1f, 74f), lado > 0 ? 180 : 0);
            }
            P(v, "Props/Banco_De_Praca", F.V(-3f, .1f, 40.5f), 90);
            P(v, "Props/Banco_De_Praca", F.V(3f, .1f, 40.5f), -90);

            // Esplanada (gramado entre o centro e a Avenida). Nada alto a menos de 6 m ao sul da Avenida,
            // porque a câmera fica atrás do jogador.
            P(v, "Natureza/Lago", F.V(34f, .02f, 44.5f), 0, 1.5f);
            P(v, "Natureza/Arvore_Comum_1", F.V(20f, 0, 43f), 30, .9f);
            P(v, "Natureza/Arvore_Salgueiro", F.V(48f, 0, 42.5f), 120, .9f);
            P(v, "Natureza/Arvore_Comum_3", F.V(56f, 0, 44f), 200, .9f);
            P(v, "Natureza/Arvore_Comum_2", F.V(-20f, 0, 43f), 80, .9f);
            P(v, "Natureza/Arvore_Betula", F.V(-29f, 0, 44f), 10, .9f);
            P(v, "Natureza/Flores", F.V(26f, 0, 48.5f), 0, 1.5f);
            P(v, "Natureza/Flores", F.V(-24f, 0, 48.5f), 0, 1.5f);

            // Avenida: postes na calçada do norte, casas depois da pracinha, superquadra e catedral ao fundo.
            for (float x = -80f; x <= 56f; x += 17f)
                if (Mathf.Abs(x) > 14f && !NosBairrosNovos(F.V(x, 0, 70f), 1f)) P(v, "Props/PosteKK", F.V(x, .08f, 65.6f), 180);
            P(v, "Predios/Casa_01", F.V(-64f, 0, 40f));
            P(v, "Predios/Casa_03", F.V(-74f, 0, 40f));
            P(v, "Predios/Casa_04", F.V(-84f, 0, 40f));
            P(v, "Predios/Brasilia_Bloco_Superquadra", F.V(-47f, 0, 80f), 180);
            P(v, "Predios/Brasilia_Bloco_Superquadra", F.V(-40f, 0, 100f), 180);
            P(v, "Natureza/Arvore_Comum_2", F.V(-60f, 0, 70f), 0, .9f);
            P(v, "Natureza/Arvore_Comum_1", F.V(-34f, 0, 70.5f), 140, .9f);
            P(v, "Predios/Brasilia_Catedral", F.V(-22f, 0, 80f));
            P(v, "Natureza/Arvore_Comum_3", F.V(16f, 0, 70f), 60, .9f);
            P(v, "Natureza/Arvore_Betula", F.V(31f, 0, 70.5f), 0, .9f);
            P(v, "Natureza/Arvore_Salgueiro", F.V(60f, 0, 71f), 200, .9f);
            P(v, "Natureza/Arvore_Pinheiro", F.V(74f, 0, 70f), 0, 1f);
            P(v, "Natureza/Arvore_Pinheiro", F.V(66f, 0, 90f), 90, 1f);
            P(v, "Natureza/Arvore_Comum_1", F.V(24f, 0, 90f), 0, .9f);
        }

        // ------------------------------------------------------------------ áreas dos verbetes do Glotrânsito

        // Quatro áreas ligadas a um grupo de verbetes, cada uma no lugar da cidade que combina com a mecânica:
        // o Cruzamento Central num cruzamento de verdade (Eixo x Avenida), a Rua Viva numa rua com ciclovia,
        // ponto de ônibus e engarrafamento, a Oficina num galpão com pátio no fim da Avenida e as Obras da
        // Cidade onde uma rua acaba no rio, com o morro e o túnel do outro lado.
        // O caos delas não depende de minigame: melhora por faixas conforme os verbetes do grupo são
        // desbloqueados (ChaosController). Só a Oficina tem cena; as outras mostram "em breve".
        class AreaDeVerbetes
        {
            public string objeto, nome, areaName, cena, marcador;
            public bool emBreve;
            public GrupoDeVerbete[] grupos;
            public Vector3 centro, luz;
            public float zDoLetreiro;
            public System.Action<Transform, Transform[], Transform[]> montar;
        }

        static readonly AreaDeVerbetes[] AreasDeVerbetes =
        {
            new AreaDeVerbetes { objeto = "Area_Oficina", nome = "Oficina", areaName = "Oficina de Veículos", cena = "Minigame_Oficina",
                grupos = new[] { GrupoDeVerbete.Veiculos }, centro = new Vector3(-76f, .5f, 70f), marcador = "Laranja",
                zDoLetreiro = 6.4f, luz = new Vector3(0, 6f, 5f), montar = Oficina },
            new AreaDeVerbetes { objeto = "Area_ObrasDaCidade", nome = "Obras da Cidade", areaName = ObrasConstrutor.NomeDaArea, cena = ObrasConstrutor.NomeDaCena,
                grupos = new[] { GrupoDeVerbete.ViasEEstruturas }, centro = new Vector3(46f, .5f, 69f), marcador = "Mostarda",
                zDoLetreiro = 4.4f, luz = new Vector3(0, 7f, 9f), montar = ObrasDaCidade },
            new AreaDeVerbetes { objeto = "Area_CruzamentoCentral", nome = "Cruzamento Central", areaName = "Cruzamento Central", cena = SinaisConstrutor.NomeDaCena,
                grupos = new[] { GrupoDeVerbete.Sinais }, centro = new Vector3(0f, .5f, 50f), marcador = "Vermelho",
                zDoLetreiro = 4.4f, luz = new Vector3(0, 7f, 9f), montar = CruzamentoCentral },
            new AreaDeVerbetes { objeto = "Area_RuaViva", nome = "Rua Viva", areaName = RuaVivaCarroConstrutor.NomeDaArea, cena = RuaVivaCarroConstrutor.NomeDaCena,
                grupos = new[] { GrupoDeVerbete.PessoasESeguranca, GrupoDeVerbete.Situacao }, centro = new Vector3(-45f, .5f, 48f), marcador = "Rosa",
                zDoLetreiro = 3f, luz = new Vector3(0, 6f, 7f), montar = RuaViva },
        };

        static void MontarAreaDeVerbetes(AreaDeVerbetes b, SaturacaoDaArea.ParDeTextura[] texturasCinza)
        {
            var velha = GameObject.Find(b.objeto);
            if (velha != null) Object.DestroyImmediate(velha);

            var area = F.Grupo(b.objeto, null, b.centro);
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
        // As posições são a partir do marcador da área; a câmera olha pra +Z, então o cenário fica em z > 0.

        // Galpão de oficina no fundo do pátio (chão do pátio em y = 0,1), com duas vagas abertas pra frente.
        static void Oficina(Transform v, Transform[] c, Transform[] o)
        {
            // Não há prefab de galpão: paredes, teto e faixa da fachada são formas da paleta.
            F.Cubo(v, "Galpao_Fundo", "Branco_Concreto", F.V(0, 2.4f, 14.6f), F.V(14.9f, 4.8f, .3f));
            foreach (float x in new[] { -7.3f, 7.3f })
                F.Cubo(v, "Galpao_Lado", "Branco_Concreto", F.V(x, 2.4f, 11.1f), F.V(.3f, 4.8f, 7.3f));
            F.Cubo(v, "Galpao_Teto", "Petroleo", F.V(0, 4.95f, 10.9f), F.V(15.8f, .3f, 8.2f));
            F.Cubo(v, "Galpao_Faixa", "Laranja", F.V(0, 4.3f, 7.4f), F.V(14.9f, 1f, .3f));
            F.Cubo(v, "Galpao_Pilar", "Branco_Concreto", F.V(0, 1.9f, 7.4f), F.V(.4f, 3.8f, .4f));
            F.Cubo(v, "Galpao_Piso", "Cinza_Escuro", F.V(0, .12f, 11f), F.V(14.3f, .04f, 7f));
            P(v, "Oficina/Bancada", F.V(-2.7f, .14f, 13.4f), 180);
            P(v, "Oficina/Prateleira", F.V(-5.8f, .14f, 14f), 180);
            P(v, "Oficina/Prateleira", F.V(5.8f, .14f, 14f), 180);
            P(v, "Oficina/Pronto_Caminhao_Trator", F.V(3.4f, .14f, 11.2f), 180);
            P(v, "Props/Barril", F.V(6.4f, .14f, 8.6f));
            P(v, "Props/Caixa_Papelao", F.V(-.9f, .14f, 13.6f), 15);
            for (int i = 0; i < 3; i++)
                F.Cilindro(v, "Pneu_" + (i + 1), "Grafite", F.V(-6.3f, .28f + i * .3f, 8.8f), F.V(1f, .28f, 1f));
            P(v, "Personagens/Trabalhador", F.V(1.7f, .1f, 6.3f), 190);
            P(v, "Props/Cacamba_De_Lixo", F.V(10.3f, .1f, 5.6f), 180);

            P(c[0], "Enchente/Papeis_Espalhados", F.V(-1.5f, .11f, 3f), 30, 1.3f);
            P(c[0], "Lixo_Reciclagem/Pilha_De_Lixo", F.V(8f, .1f, 2f), 200);
            P(c[0], "Lixo_Reciclagem/Saco_De_Lixo_Preto_1", F.V(-5f, .1f, 1.2f));
            P(c[0], "Lixo_Reciclagem/Lata_Amassada", F.V(2.5f, .1f, 1.5f), 40, 2.4f);
            P(c[0], "Lixo_Reciclagem/Garrafa_Pet_Amassada", F.V(-8f, .1f, -1f), 100, 2.2f);

            P(c[1], "Enchente/Chao_De_Lama", F.V(5f, .11f, 4f), 20, .8f);
            P(c[1], "Enchente/Chao_De_Lama", F.V(-6f, .11f, -1.5f), 70, .6f);
            P(c[1], "Enchente/Arvore_Seca_1", F.V(-10.6f, .1f, 5f), 40, .9f);
            P(c[1], "Enchente/Arbusto_Seco", F.V(10.6f, .1f, -2f), 0, .8f);

            Virar(P(c[2], "Carros/Carro_Hatch", F.V(-7f, 1.05f, 4.5f), 60));
            Virar(P(c[2], "Carros/Moto", F.V(-3f, .47f, 9.6f), 110), 84);
            Virar(P(c[2], "Carros/Bicicleta_Q1", F.V(6f, .3f, 6f), 30), 82);
            P(c[2], "Props/Cone_Caido", F.V(0, .1f, 5f), 70);

            P(c[3], "Enchente/Entulho_De_Concreto", F.V(9.6f, .1f, -1.5f), 30, .45f);
            P(c[3], "Enchente/Cavalete_Medio_1", F.V(-2.4f, .1f, 6.6f), 200);
            P(c[3], "Enchente/Cone_Quebrado_2", F.V(-9f, .1f, 2f), 50);
            P(c[3], "Enchente/Bloco_Concreto_Lascado_1", F.V(3.4f, .1f, 6.6f), 20);

            P(o[0], "Natureza/Flores", F.V(-5f, .1f, 1.2f), 0, 1.4f);
            P(o[0], "Props/Vaso_De_Folhagem", F.V(8f, .1f, 2f), 180);
            P(o[1], "Natureza/Arvore_Simples_Grande", F.V(-10.6f, .1f, 5f), 0, 1.05f);
            P(o[1], "Natureza/Arbusto_2", F.V(10.6f, .1f, -2f), 0, .8f);
            P(o[2], "Oficina/Pronto_Quadriciclo", F.V(-7f, .1f, 4.5f), 160);
            P(o[2], "Oficina/Pronto_Motoneta", F.V(-3f, .14f, 9.6f), 180);
            P(o[2], "Carros/Bicicleta_Q1", F.V(6f, .1f, 5.6f), 180);
            P(o[2], "Carros/Carro_Hatch", F.V(9.8f, .1f, 9.4f), 180);
            P(o[3], "Natureza/Passaro", F.V(-4f, 5.1f, 7.2f), 200, 1.6f);
            P(o[3], "Natureza/Passaro", F.V(5f, 5.1f, 7.2f), 150, 1.6f);
            P(o[3], "Props/Cone", F.V(-9f, .1f, 2f));
            P(o[3], "Props/Cone", F.V(9.6f, .1f, -1.5f));
        }

        // A rua acaba no rio (z de 7 a 15); do outro lado ela segue até o túnel do morro.
        // No começo não há como atravessar; com tudo arrumado, a ponte volta e o túnel fica livre.
        static void ObrasDaCidade(Transform v, Transform[] c, Transform[] o)
        {
            // Não há prefab de morro nem de túnel: são formas da paleta.
            F.Esfera(v, "Morro_Grande", "Verde_Escuro", F.V(2f, 0, 40f), F.V(48f, 26f, 24f));
            F.Esfera(v, "Morro_Esquerdo", "Verde", F.V(-16f, 0, 36f), F.V(16f, 10f, 12f));
            F.Esfera(v, "Morro_Direito", "Verde_Musgo", F.V(27f, 0, 39f), F.V(30f, 18f, 18f));
            // Boca do túnel em arco (ArteDetalhes) e o morro com árvores, arbustos e pedras (antes era só a esfera lisa).
            ArteDetalhes.BocaDeTunel(v, "Tunel", F.V(0, 0, 27f), 0, 6f, 5f, 2f);
            var livre = new Rect(-8f, 18f, 16f, 14f);
            ArteDetalhes.EnfeitarMorro(v, "Mato_Do_Morro_Grande", F.V(2f, 0, 40f), F.V(48f, 26f, 24f), 22, 11, livre);
            ArteDetalhes.EnfeitarMorro(v, "Mato_Do_Morro_Esquerdo", F.V(-16f, 0, 36f), F.V(16f, 10f, 12f), 6, 12, livre);
            ArteDetalhes.EnfeitarMorro(v, "Mato_Do_Morro_Direito", F.V(27f, 0, 39f), F.V(30f, 18f, 18f), 12, 13, livre);
            P(v, "Natureza/Pedra_1", F.V(-6f, .03f, 6.2f), 20, 1.8f);
            P(v, "Natureza/Pedra_Musgo", F.V(7f, .03f, 6.1f), 80, 1.6f);
            P(v, "Natureza/Pedra_3", F.V(-8f, .03f, 15.9f), 140, 1.6f);
            P(v, "Natureza/Pedra_2", F.V(6.5f, .03f, 16f), 30, 1.8f);
            P(v, "Personagens/Trabalhadora", F.V(4.2f, .08f, 3.4f), 200);

            P(c[0], "Lixo_Reciclagem/Garrafa_Pet", F.V(-3f, .06f, 10f), 40, 2.4f).transform.Rotate(85, 0, 0);
            P(c[0], "Lixo_Reciclagem/Saco_De_Lixo_Verde", F.V(2f, .03f, 12.5f), 30);
            P(c[0], "Props/Caixa_Papelao", F.V(5f, .06f, 9.5f), 20, .8f).transform.Rotate(10, 0, -8);
            P(c[0], "Enchente/Papeis_Espalhados", F.V(-6f, .05f, 11.5f), 60, 1.2f);
            P(c[0], "Lixo_Reciclagem/Pilha_De_Lixo", F.V(6.6f, .03f, 2f), 160);

            P(c[1], "Enchente/Chao_De_Lama", F.V(0, .03f, 2.6f), 15, .7f);
            P(c[1], "Enchente/Chao_De_Lama", F.V(-6f, .03f, 3.5f), 80, .6f);
            P(c[1], "Enchente/Arvore_Seca_2", F.V(-9.5f, .02f, 5f), 20, .9f);
            P(c[1], "Enchente/Arvore_Seca_Grande", F.V(10.5f, .02f, 4f), 0, .7f);

            Tombar(P(c[2], "Props/Sinalizador_Obra", F.V(2.6f, .2f, 4.6f), 220));
            P(c[2], "Enchente/Cavalete_Grande_1", F.V(-3f, .02f, 3.9f), 30).transform.Rotate(0, 0, 80);
            P(c[2], "Props/Cone_Caido", F.V(1f, .02f, 1.6f), 100);
            Virar(P(c[2], "Carros/Carro_Picape", F.V(-8f, .98f, 1.5f), 20));

            // Pedaços da ponte dentro do rio e o túnel entupido.
            P(c[3], "Enchente/Pedacos_De_Rua", F.V(0, .1f, 11f), 90, .9f);
            P(c[3], "Props/Barreira_Obra", F.V(-1.8f, .02f, 5.6f));
            P(c[3], "Props/Barreira_Obra", F.V(1.8f, .02f, 5.6f));
            P(c[3], "Enchente/Entulho_De_Concreto", F.V(0, .02f, 25.4f), 10, .8f);
            P(c[3], "Enchente/Bloco_Concreto", F.V(-2f, .02f, 16.8f), 30, .8f);
            P(c[3], "Enchente/Barricada_De_Entulho", F.V(1f, .02f, 18f), 185);
            P(c[3], "Enchente/Asfalto_Quebrado", F.V(0, .03f, 21f), 90, .7f);

            P(o[0], "Natureza/Flores", F.V(6.6f, .03f, 2f), 0, 1.4f);
            P(o[0], "Natureza/Vitoria_Regia", F.V(-5f, .05f, 11f), 30, 1.5f);
            P(o[0], "Natureza/Vitoria_Regia", F.V(6f, .05f, 12.6f), 120, 1.3f);
            P(o[1], "Natureza/Arvore_Comum_2", F.V(-9.5f, .02f, 5f), 0, .8f);
            P(o[1], "Natureza/Arvore_Simples_Pequena", F.V(10.5f, .02f, 4f), 0, 1.2f);
            P(o[1], "Natureza/Grama_Alta", F.V(-6f, .03f, 3.5f), 0, 1.3f);
            P(o[2], "Props/Sinalizador_Obra", F.V(4.3f, .08f, 5.2f), 180);
            P(o[2], "Props/Cone", F.V(-4.3f, .08f, 5.2f));
            P(o[2], "Carros/Carro_Picape", F.V(-8f, .02f, 1.5f));
            // A Ponte JK (a mesma do minigame, gerada pelo ObrasConstrutor), ligando as duas margens.
            P(o[3], "Obras/Ponte_JK", F.V(0, .02f, 11f), 0, 1.05f);
            P(o[3], "Carros/Carro_Sedan", F.V(1.75f, .02f, 21f));
            P(o[3], "Natureza/Passaro", F.V(-6f, 1.65f, 6.2f), 160, 1.6f);
            P(o[3], "Natureza/Passaro", F.V(7f, 1.6f, 6.1f), 220, 1.6f);
        }

        // O marcador fica no calçadão do Eixo; o cruzamento com a Avenida fica logo à frente (z de 6 a 14).
        // No começo não há nenhum sinal de pé; com tudo arrumado, cada pista ganha o seu semáforo.
        static void CruzamentoCentral(Transform v, Transform[] c, Transform[] o)
        {
            P(v, "Props/Caixa_De_Energia", F.V(3f, .1f, 3.2f), 180, 1.6f);
            P(v, "Personagens/Trabalhadora", F.V(2.1f, .1f, 2.6f), 200);
            P(v, "Carros/Carro_Taxi", F.V(7f, .02f, -1.5f));
            P(v, "Carros/Carro_Van", F.V(-7f, .02f, 21.5f), 180);
            P(v, "Carros/Onibus", F.V(-17.5f, .02f, 8f), 90);
            P(v, "Carros/Carro_Sedan", F.V(15.5f, .02f, 12f), -90);
            P(v, "Personagens/Mini_Mulher_3", F.V(-2.4f, .1f, 4.8f), 10, .9f);
            P(v, "Personagens/Mini_Homem_2", F.V(2.6f, .1f, 15.6f), 180, .9f);

            P(c[0], "Enchente/Papeis_Espalhados", F.V(0, .11f, 2.4f), 50, 1.3f);
            P(c[0], "Lixo_Reciclagem/Saco_De_Lixo_Laranja", F.V(-3.2f, .1f, 1.4f), 40);
            P(c[0], "Lixo_Reciclagem/Saco_De_Lixo_Preto_2", F.V(-2.6f, .1f, -2.2f));
            P(c[0], "Lixo_Reciclagem/Lata_Refrigerante", F.V(1f, .1f, -2f), 0, 2.4f);
            P(c[0], "Lixo_Reciclagem/Pilha_De_Lixo", F.V(3f, .1f, -4f), 100);

            P(c[1], "Enchente/Chao_De_Lama", F.V(-1f, .03f, 10f), 35, 1.2f);
            P(c[1], "Enchente/Chao_De_Lama", F.V(7f, .03f, 5f), 10, .6f);
            P(c[1], "Enchente/Arvore_Seca_1", F.V(-3.2f, .1f, -5.5f), 0, .9f);
            P(c[1], "Enchente/Arvore_Seca_2", F.V(3.2f, .1f, -9f), 60, .9f);

            Tombar(P(c[2], "Props/Semaforo_Carros", F.V(-4.6f, .3f, 4.8f), 110));
            Tombar(P(c[2], "Props/Semaforo_Carros", F.V(4.8f, .3f, 15.2f), 250));
            Tombar(P(c[2], "Props/Semaforo_Pedestres", F.V(2.8f, .28f, 5.2f), 240));
            P(c[2], "Props/Placa_FaixaDePedestre", F.V(-3.6f, .1f, 4.6f), 180, 1.15f).transform.Rotate(0, 0, 26);
            Virar(P(c[2], "Carros/Carro_Hatch", F.V(2f, 1.05f, 10.5f), 40));

            P(c[3], "Enchente/Barricada_De_Entulho", F.V(0, .02f, 7.2f), 175);
            P(c[3], "Enchente/Cone_Quebrado_3", F.V(-2.6f, .02f, 8.6f), 20);
            P(c[3], "Enchente/Bloco_Concreto", F.V(-6f, .02f, 12f), 60, .8f);
            P(c[3], "Enchente/Cavalete_Medio_2", F.V(7f, .02f, 13f), 100);
            P(c[3], "Enchente/Entulho_De_Concreto", F.V(-7.4f, .02f, 6.6f), 30, .45f);

            P(o[0], "Natureza/Flores", F.V(-3.2f, .1f, 1.4f), 0, 1.4f);
            P(o[0], "Natureza/Flores", F.V(3.2f, .1f, -2f), 0, 1.4f);
            P(o[0], "Props/Vaso_De_Laranjeira", F.V(3f, .1f, -4f));
            P(o[1], "Natureza/Arvore_Simples_Grande", F.V(-3.2f, .1f, -5.5f), 0, .9f);
            P(o[1], "Natureza/Arvore_Simples_Pequena", F.V(3.2f, .1f, -9f), 0, 1.1f);
            // Um semáforo virado pra cada pista que chega, e os de pedestre nas pontas da faixa do calçadão.
            P(o[2], "Props/Semaforo_Carros", F.V(10.6f, .08f, 1.2f), 180);
            P(o[2], "Props/Semaforo_Carros", F.V(-10.6f, .08f, 18.8f), 0);
            P(o[2], "Props/Semaforo_Carros", F.V(-12f, .08f, 5.4f), -90);
            P(o[2], "Props/Semaforo_Carros", F.V(12f, .08f, 14.6f), 90);
            P(o[2], "Props/Semaforo_Pedestres", F.V(-3.5f, .1f, 5.3f), 180);
            P(o[2], "Props/Semaforo_Pedestres", F.V(3.5f, .1f, 14.7f), 0);
            P(o[3], "Props/Placa_FaixaDePedestre", F.V(3.6f, .1f, 4.6f), 180, 1.15f);
            P(o[3], "Natureza/Passaro", F.V(3f, 2.2f, 3.2f), 200, 1.6f);
            P(o[3], "Natureza/Passaro", F.V(-1.6f, .1f, -3f), 40, 1.6f);
        }

        // Da pracinha (y = 0,1) se vê, de perto pra longe: a ciclovia com a família (z de 3,7 a 5,9), a calçada,
        // a Avenida engarrafada (pista de cá em z = 10, de lá em z = 14) e o ponto de ônibus com passageiros.
        static void RuaViva(Transform v, Transform[] c, Transform[] o)
        {
            P(v, "Personagens/Familia_Pai_Animado", F.V(-3.4f, .06f, 4.8f), 90);
            P(v, "Carros/Bicicleta_Q1", F.V(-2.3f, .06f, 4.8f), -90);
            P(v, "Personagens/Familia_Mae_Animado", F.V(-5f, .06f, 4.6f), 90);
            P(v, "Personagens/Familia_Filha_Animado", F.V(3f, .1f, 2.2f), 200);
            P(v, "Personagens/Familia_FilhoMaisNovo_Animado", F.V(4f, .1f, 2.4f), 160);
            P(v, "Natureza/Cachorro_Caramelo", F.V(4.9f, .1f, 1.7f), 200);
            P(v, "Props/Banco", F.V(-6f, .1f, 1f), 180);
            // Engarrafamento: fila de carros parados na pista de cá.
            string[] fila = { "Carro_Sedan", "Carro_Hatch", "Carro_Van", "Carro_Taxi", "Carro_SUV", "Carro_Compacto_Azul", "Carro_Picape", "Carro_Sedan_Azul", "Carro_Entregas" };
            for (int i = 0; i < fila.Length; i++)
                P(v, "Carros/" + fila[i], F.V(-22f + i * 4.6f, .02f, 10f), 90);
            // Ponto de ônibus do outro lado, com o ônibus chegando e os passageiros esperando.
            P(v, "Props/Ponto_De_Onibus", F.V(3f, .08f, 17.6f), 180);
            P(v, "Carros/Onibus_Urbano", F.V(4f, .02f, 14.2f), -90);
            P(v, "Personagens/Idosa", F.V(2.2f, .08f, 17f), 180);
            P(v, "Personagens/Pedestre_Mulher", F.V(3.8f, .08f, 17f), 170);
            P(v, "Personagens/Mini_Homem_4", F.V(5.6f, .08f, 16.8f), 190, .9f);

            P(c[0], "Enchente/Papeis_Espalhados", F.V(-1f, .08f, 4.8f), 20, 1.3f);
            P(c[0], "Lixo_Reciclagem/Saco_De_Lixo_Preto_1", F.V(6.6f, .1f, 1.2f));
            P(c[0], "Lixo_Reciclagem/Casca_De_Banana", F.V(1.6f, .08f, 5.2f), 0, 2.6f);
            P(c[0], "Lixo_Reciclagem/Lixeira_Transbordando", F.V(-7.4f, .1f, 2.6f), 180);
            P(c[0], "Lixo_Reciclagem/Embalagem_Batata", F.V(-3f, .1f, -2f), 50, 2.4f);

            P(c[1], "Enchente/Chao_De_Lama", F.V(6f, .08f, 4.8f), 40, .5f);
            P(c[1], "Enchente/Chao_De_Lama", F.V(-5f, .11f, -2.6f), 0, .6f);
            P(c[1], "Enchente/Arvore_Seca_2", F.V(-7.6f, .1f, -3.6f), 30, .9f);
            P(c[1], "Enchente/Arbusto_Seco", F.V(7.6f, .1f, -3.6f), 0, .8f);

            Tombar(P(c[2], "Props/Placa_Ciclovia", F.V(-7f, .28f, 2.4f), 130, 1.15f));
            Tombar(P(c[2], "Props/Placa_De_Ponto_De_Onibus", F.V(7.4f, .26f, 17.2f), 215));
            Virar(P(c[2], "Carros/Bicicleta_Azul", F.V(1f, .3f, 4.8f), 70), 82);
            P(c[2], "Props/Pilarete", F.V(7.8f, .1f, 3.2f)).transform.Rotate(0, 0, 40);

            Tombar(P(c[3], "Enchente/Arvore_Seca_3", F.V(-9.5f, .42f, 4.2f), 15, .9f), 82);
            P(c[3], "Enchente/Cavalete_Pequeno_2", F.V(8.4f, .06f, 4.8f), 100);
            P(c[3], "Enchente/Entulho_De_Concreto", F.V(-7.4f, .08f, 16.8f), 100, .45f);
            P(c[3], "Enchente/Cone_Quebrado_1", F.V(2.2f, .1f, 2.8f), 60);

            P(o[0], "Natureza/Flores", F.V(6.6f, .1f, 1.2f), 0, 1.4f);
            P(o[0], "Props/Lixeira_De_Rua", F.V(-7.4f, .1f, 2.6f), 180);
            P(o[0], "Props/Floreira_De_Madeira", F.V(0, .1f, -3.4f), 180);
            P(o[1], "Natureza/Arvore_Comum_1", F.V(-7.6f, .1f, -3.6f), 0, .7f);
            P(o[1], "Natureza/Arbusto_Frutinhas", F.V(7.6f, .1f, -3.6f), 0, .8f);
            P(o[2], "Props/Placa_Ciclovia", F.V(-7.8f, .1f, 3.2f), 180, 1.15f);
            P(o[2], "Props/Placa_De_Ponto_De_Onibus", F.V(.6f, .08f, 17.4f), 180);
            P(o[2], "Carros/Bicicleta_Azul", F.V(1f, .06f, 4.8f), 90);
            P(o[2], "Props/Pilarete", F.V(7.8f, .1f, 3.2f));
            P(o[3], "Natureza/Passaro", F.V(-6f, .62f, 1f), 180, 1.6f);
            P(o[3], "Natureza/Passaro", F.V(-2f, .1f, 1.4f), 30, 1.6f);
            P(o[3], "Props/Vaso_De_Laranjeira", F.V(2.4f, .1f, -3.4f));
        }

        // ------------------------------------------------------------------ jogador

        // Troca a cápsula pelo filho mais velho animado. O CharacterController e os scripts continuam os mesmos.
        static void Jogador()
        {
            var jogador = GameObject.Find("Player");
            if (jogador == null) { Debug.LogError("[Hub] Player não encontrado."); return; }
            var velho = jogador.transform.Find("Visual");
            if (velho != null) Object.DestroyImmediate(velho.gameObject);

            // Começa no cruzamento ao sul do centro, olhando pro norte (antes nascia na quina, colado numa árvore).
            jogador.transform.SetPositionAndRotation(new Vector3(10, 1, -12), Quaternion.identity);
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
