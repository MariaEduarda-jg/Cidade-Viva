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
            Vizinhanca(cidade);
            Limites(cidade);

            var areas = new Dictionary<string, System.Action<Transform, Transform, Transform>>
            {
                ["Area_OndeEuVou"] = OndeEuVou, ["Area_TravessiaSegura"] = TravessiaSegura, ["Area_ReciclaComigo"] = ReciclaComigo,
                ["Area_CorreCorrendo"] = CorreCorrendo, ["Area_PedalSeguro"] = PedalSeguro, ["Area_SobeSobe"] = SobeSobe,
                ["Area_SemaforoMagico"] = SemaforoMagico, ["Area_GuardaOSinal"] = GuardaOSinal, ["Area_OlhoVivo"] = OlhoVivo,
            };
            string[] coresDoMarcador = { "Mostarda", "Petroleo_Claro", "Verde", "Laranja", "Terracota", "Petroleo", "Vermelho", "Mostarda_Clara", "Rosa" };
            int n = 0;
            foreach (var par in areas)
            {
                var area = GameObject.Find(par.Key);
                if (area == null) { Debug.LogError("[Hub] Área não encontrada: " + par.Key); continue; }
                MontarArea(area, par.Value, n, coresDoMarcador[n % coresDoMarcador.Length], texturasCinza);
                n++;
            }

            Jogador();
            MenuConstrutor.PainelDoHub();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
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
            P(v, "Predios/Brasilia_Congresso", F.V(0, 0, 66f), 180);
            P(v, "Predios/Brasilia_Catedral", F.V(-36f, 0, 52f));
            P(v, "Predios/Brasilia_Museu_Cupula", F.V(40f, 0, 56f), 180);
            P(v, "Predios/Brasilia_Palacio_Arcos", F.V(-78f, 0, 70f), 150);
            P(v, "Predios/Brasilia_Bloco_Superquadra", F.V(-64f, 0, 22f), 90);
            P(v, "Predios/Brasilia_Bloco_Superquadra", F.V(66f, 0, 26f), -90);
            P(v, "Predios/Arranha_Ceu_A", F.V(-24f, 0, 84f), 180);
            P(v, "Predios/Arranha_Ceu_D", F.V(26f, 0, 88f), 180);
            P(v, "Predios/Predio_Comercial_Torre", F.V(70f, 0, 70f), 180);
            foreach (float x in new[] { -9f, 9f })
                foreach (float z in new[] { 39.5f, 45f })
                    P(v, "Natureza/Palmeira_1", F.V(x, 0, z), x > 0 ? 40 : 200);

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
                P(v, "Natureza/" + arvores[sorteio.Next(arvores.Length)], pos, sorteio.Next(360), .75f + (float)sorteio.NextDouble() * .35f);
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

        // Paredes invisíveis pro jogador não sair da cidade.
        static void Limites(Transform cidade)
        {
            var limites = F.Grupo("Limites", cidade).transform;
            foreach (var (pos, tam) in new[]
                     {
                         (F.V(0, 2, 37.5f), F.V(76, 4, 1)), (F.V(0, 2, -37.5f), F.V(76, 4, 1)),
                         (F.V(37.5f, 2, 0), F.V(1, 4, 76)), (F.V(-37.5f, 2, 0), F.V(1, 4, 76)),
                     })
            {
                var go = F.Grupo("Parede", limites, pos);
                go.AddComponent<BoxCollider>().size = tam;
            }
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
            var raiz = F.Grupo("Letreiro", area.transform, F.V(0, -.5f, 6.15f)).transform;
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
            var nomeDaArea = area.GetComponent<AreaTrigger>().areaName;
            var titulo = NovoTexto("Nome", 46, ArtePaleta.Cor("Branco"), .42f, 1f, FontStyle.Bold);
            titulo.text = nomeDaArea;
            var recorde = NovoTexto("Recorde", 24, ArtePaleta.Cor("Mostarda_Clara"), .04f, .44f, FontStyle.Normal);
            recorde.text = "Venha jogar!";

            var letreiro = raiz.gameObject.AddComponent<LetreiroDaArea>();
            letreiro.area = area.GetComponent<AreaTrigger>();
            letreiro.textoDoNome = titulo;
            letreiro.textoDoRecorde = recorde;
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

        // ------------------------------------------------------------------ jogador

        // Troca a cápsula pelo filho mais velho animado. O CharacterController e os scripts continuam os mesmos.
        static void Jogador()
        {
            var jogador = GameObject.Find("Player");
            if (jogador == null) { Debug.LogError("[Hub] Player não encontrado."); return; }
            var velho = jogador.transform.Find("Visual");
            if (velho != null) Object.DestroyImmediate(velho.gameObject);

            var capsula = jogador.GetComponent<MeshRenderer>();
            if (capsula != null) capsula.enabled = false;
            var visual = P(jogador.transform, "Personagens/Familia_FilhoMaisVelho_Animado", F.V(0, -1f, 0));
            visual.name = "Visual";

            var anim = jogador.GetComponent<AnimacaoDoJogador>();
            if (anim == null) anim = jogador.AddComponent<AnimacaoDoJogador>();
            anim.animator = visual.GetComponentInChildren<Animator>();
            anim.velocidadeDeCorrida = jogador.GetComponent<PlayerMovement>().moveSpeed;
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
