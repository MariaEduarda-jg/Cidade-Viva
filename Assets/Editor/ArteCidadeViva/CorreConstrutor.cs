using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CidadeViva.EditorArte
{
    // Monta do zero a cena 3D do "Corre, Correndo!" com os prefabs de arte.
    // A criança fica parada em z = 0 olhando pra +Z; a rua de bairro é feita de trechos iguais em
    // comprimento, que a CorrePista rola pra trás e devolve ao fim da fila. Os obstáculos são
    // modelos inativos ("Modelos"), copiados em tempo de jogo.
    public static class CorreConstrutor
    {
        const string CaminhoDaCena = "Assets/Scenes/Minigame_CorreCorrendo.unity";
        const string CaminhoDaConfig = "Assets/Configs/CorreCorrendo_Config.asset";
        const string CaminhoDoAsfaltoMolhado = "Assets/Materials/Asfalto_Molhado.mat";
        const float Trecho = 24f;
        const int Trechos = 5;
        const float MeiaRua = 3.9f;

        [MenuItem("Cidade Viva/Minigames/Reconstruir a cena do Corre, Correndo!")]
        public static void ReconstruirPeloMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Reconstruir();
        }

        static GameObject P(Transform pai, string prefab, Vector3 pos, float rotY = 0f, float escala = 1f)
        {
            return TravessiaConstrutor.P(pai, prefab, pos, rotY, escala);
        }

        static void SemSombra(GameObject go)
        {
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public static void Reconstruir()
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Depois de criar a cena: trocar de cena descarrega assets que ninguém está usando.
            MenuConstrutor.PrepararIcones();
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{ArteImportador.PastaPrefabs}/Props/Moeda.prefab") == null) ArteFormas.GerarItensDoRunner();
            var niveis = NiveisConstrutor.SobrescreverNiveisDoCorre();
            var config = PrepararConfig();
            var molhado = AsfaltoMolhado();

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 55f;
            cam.transform.position = new Vector3(0, 4.4f, -7.5f);
            cam.transform.LookAt(new Vector3(0, 1.2f, 7f));
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AmbienteConstrutor.CaminhoPrefab) == null) AmbienteConstrutor.CriarPrefab();
            var ambiente = AmbienteConstrutor.AplicarNaCenaAberta(true);
            // Neblina curta: esconde o ponto onde os trechos e os obstáculos aparecem.
            ambiente.neblinaInicio = 36f;
            ambiente.neblinaFim = 86f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(ambiente);
            ambiente.Aplicar();

            // ---------------- cenário ----------------
            var cenario = F.Grupo("Cenario").transform;
            SemSombra(F.Plano(cenario, "Gramado", "Verde_Claro", F.V(0, -.03f, 40), 400, 400));

            var trechos = new Transform[Trechos];
            var asfaltos = new List<Renderer>();
            for (int i = 0; i < Trechos; i++)
            {
                trechos[i] = CriarTrecho(cenario, i, asfaltos);
            }

            var modelos = F.Grupo("Modelos", cenario).transform;
            var listaDeModelos = CriarModelos(modelos);
            var objetos = F.Grupo("Objetos_Da_Pista").transform;

            var pista = F.Grupo("Pista").AddComponent<CorrePista>();
            pista.trechos = trechos;
            pista.comprimentoDoTrecho = Trecho;
            pista.modelos = listaDeModelos;
            pista.paiDosObjetos = objetos;

            // ---------------- jogador e efeitos ----------------
            var jogadorGo = F.Grupo("Jogador");
            var visual = F.Grupo("Visual", jogadorGo.transform).transform;
            var personagem = P(visual, "Personagens/Familia_FilhoMaisNovo_Animado", Vector3.zero, 0, 1.3f);
            var jogador = jogadorGo.AddComponent<CorreJogador>();
            jogador.visual = visual;
            jogador.animator = personagem.GetComponentInChildren<Animator>();

            var efeitos = F.Grupo("Efeitos").transform;
            var confete = TravessiaConstrutor.Particulas(efeitos, "Confete", 70, 1.6f, 6f, .22f, 2.5f,
                "Mostarda", "Vermelho", "Petroleo_Claro", "Verde", "Rosa", "Laranja");
            var estrelinhas = TravessiaConstrutor.Particulas(efeitos, "Estrelinhas", 14, .7f, 3.5f, .28f, .6f, "Mostarda", "Mostarda_Clara", "Branco");
            var brilho = TravessiaConstrutor.Particulas(efeitos, "Brilho", 8, .4f, 2.6f, .16f, 0f, "Mostarda", "Mostarda_Clara", "Branco");
            var respingo = TravessiaConstrutor.Particulas(efeitos, "Respingo", 18, .6f, 4.2f, .16f, 2f, "Vidro", "Petroleo_Claro", "Branco");
            var chuva = Chuva(efeitos);

            // ---------------- interface ----------------
            var canvas = MenuConstrutor.NovoCanvas("Canvas");
            MenuConstrutor.NovoEventSystem(false);
            var hud = Hud(canvas.transform, out var botoes);

            var fim = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabFim), canvas.transform);
            var selecao = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabSelecao), canvas.transform);

            // ---------------- gerente ----------------
            var gerenteGo = new GameObject("CorreManager", typeof(AudioSource));
            var somDoJogo = gerenteGo.GetComponent<AudioSource>();
            somDoJogo.playOnAwake = false;
            somDoJogo.volume = .8f;
            var musicaGo = F.Grupo("Musica", gerenteGo.transform);
            var musica = musicaGo.AddComponent<AudioSource>();
            musica.playOnAwake = false;
            musica.loop = true;
            musica.volume = .3f;

            var gerente = gerenteGo.AddComponent<CorreManager>();
            gerente.nomeDaArea = "Corre, Correndo!";
            gerente.telaFimDeJogo = fim.GetComponent<TelaFimDeJogo>();
            gerente.telaDeSelecao = selecao.GetComponent<TelaSelecaoDeNivel>();
            gerente.niveis = niveis;
            gerente.config = config;
            gerente.jogador = jogador;
            gerente.pista = pista;
            gerente.hud = hud;
            gerente.som = somDoJogo;
            gerente.musica = musica;
            gerente.confete = confete;
            gerente.estrelinhas = estrelinhas;
            gerente.brilho = brilho;
            gerente.respingo = respingo;
            gerente.chuva = chuva;
            gerente.asfalto = asfaltos.ToArray();
            gerente.asfaltoMolhado = molhado;
            gerente.sol = ambiente.sol;

            UnityEventTools.AddPersistentListener(botoes["esquerda"].onClick, gerente.IrParaAEsquerda);
            UnityEventTools.AddPersistentListener(botoes["direita"].onClick, gerente.IrParaADireita);
            UnityEventTools.AddPersistentListener(botoes["pular"].onClick, gerente.Pular);
            UnityEventTools.AddPersistentListener(botoes["deslizar"].onClick, gerente.Deslizar);

            EditorSceneManager.SaveScene(cena, CaminhoDaCena);
        }

        // ------------------------------------------------------------------ cenário

        static readonly string[] Casas = { "Casa_01", "Casa_05", "Casa_03", "Casa_07", "Casa_02", "Casa_04", "Padaria", "Casa_06", "Casa_08", "Casa_05" };
        static readonly string[] Arvores = { "Arvore_Comum_1", "Arvore_Betula", "Arvore_Comum_2", "Palmeira_3", "Arvore_Comum_3" };
        static readonly string[] Enfeites = { "Natureza/Arbusto_1", "Props/Banco", "Natureza/Flores", "Props/Lixeira_De_Rua", "Natureza/Arbusto_2", "Props/Hidrante", "Props/Caixa_De_Correio" };

        // Um trecho de 24 m de rua de bairro: asfalto com 3 faixas, calçadas, duas casas de cada lado,
        // árvores, postes e enfeites. Cada trecho usa casas diferentes, pra repetição não ficar na cara.
        static Transform CriarTrecho(Transform pai, int indice, List<Renderer> asfaltos)
        {
            var t = F.Grupo("Trecho_" + (indice + 1), pai, F.V(0, 0, -Trecho + indice * Trecho)).transform;

            var asfalto = F.Cubo(t, "Asfalto", "Asfalto", F.V(0, -.05f, Trecho / 2f), F.V(MeiaRua * 2f, .1f, Trecho));
            SemSombra(asfalto);
            asfaltos.Add(asfalto.GetComponent<Renderer>());
            foreach (float x in new[] { -1.2f, 1.2f })
                for (float z = 2f; z < Trecho; z += 4f)
                    SemSombra(F.Cubo(t, "Traco", "Branco", F.V(x, .008f, z), F.V(.14f, .012f, 2f)));

            for (int lado = -1; lado <= 1; lado += 2)
            {
                for (float z = 1.5f; z < Trecho; z += 3f)
                    P(t, "Ruas/Calcada_SemMeioFio", F.V(lado * (MeiaRua + 1.5f), 0, z));

                // Frente das casas virada pra rua.
                float frente = lado < 0 ? 90f : -90f;
                int n = indice * 2 + (lado < 0 ? 0 : 5);
                P(t, "Predios/" + Casas[n % Casas.Length], F.V(lado * 12.2f, 0, 6f), frente);
                P(t, "Predios/" + Casas[(n + 1) % Casas.Length], F.V(lado * 12.2f, 0, 18f), frente);

                P(t, "Natureza/" + Arvores[(indice + (lado < 0 ? 0 : 2)) % Arvores.Length], F.V(lado * 8.2f, 0, lado < 0 ? 12f : 0.8f), indice * 70, .8f);
                P(t, "Props/Poste_Curvo", F.V(lado * (MeiaRua + .5f), .15f, lado < 0 ? 1f : 13f), frente);
                P(t, Enfeites[(indice * 2 + (lado < 0 ? 0 : 3)) % Enfeites.Length], F.V(lado * (MeiaRua + 2.3f), .15f, lado < 0 ? 19.5f : 7f), frente);
                P(t, Enfeites[(indice * 2 + (lado < 0 ? 4 : 1)) % Enfeites.Length], F.V(lado * (MeiaRua + 2.3f), .15f, lado < 0 ? 7.5f : 20f), frente);
            }
            return t;
        }

        // ------------------------------------------------------------------ modelos de obstáculos e itens

        static CorrePista.Modelo[] CriarModelos(Transform pai)
        {
            var lista = new List<CorrePista.Modelo>();
            Transform Novo(string id)
            {
                var go = F.Grupo("Modelo_" + id, pai);
                lista.Add(new CorrePista.Modelo { id = id, objeto = go });
                return go.transform;
            }

            var buraco = Novo("buraco");
            P(buraco, "Props/Buraco", F.V(0, .005f, -.1f), 0, .95f);

            var poca = Novo("poca");
            SemSombra(F.Cilindro(poca, "Borda", "Asfalto_Escuro", F.V(0, .006f, 0), F.V(2.15f, .012f, 1.6f)));
            SemSombra(F.Cilindro(poca, "Agua", "Agua", F.V(0, .014f, 0), F.V(2f, .016f, 1.45f)));
            SemSombra(F.Cilindro(poca, "Agua_2", "Agua", F.V(.55f, .016f, .5f), F.V(1f, .016f, .8f)));
            SemSombra(F.Cilindro(poca, "Brilho", "Vidro", F.V(-.35f, .024f, -.15f), F.V(.7f, .01f, .22f), F.V(0, 25, 0)));

            var cones = Novo("cones");
            P(cones, "Props/Cone", F.V(-.75f, 0, .1f), 0, 1.15f);
            P(cones, "Props/Cone", F.V(0, 0, -.1f), 0, 1.15f);
            P(cones, "Props/Cone", F.V(.75f, 0, .1f), 0, 1.15f);

            var barreira = Novo("barreira");
            P(barreira, "Props/Barreira_Obra", Vector3.zero, 90, 1.15f);

            var carro = Novo("carro");
            P(carro, "Carros/Carro_Hatch", Vector3.zero);

            // Galho atravessado na altura do peito: só passa deslizando por baixo.
            var galho = Novo("galho");
            P(galho, "Natureza/Tronco_Caido", F.V(-.15f, .95f, 0), 90, .9f);
            P(galho, "Props/Pilarete", F.V(-1.05f, 0, 0), 0, 1.1f);
            P(galho, "Props/Pilarete", F.V(1.05f, 0, 0), 0, 1.1f);
            P(galho, "Natureza/Arbusto_1", F.V(.45f, 1.4f, 0), 30, .5f);
            P(galho, "Natureza/Arbusto_1", F.V(-.6f, 1.35f, 0), 100, .4f);

            // Vem de frente, pela contramão.
            var carroEmMovimento = Novo(CorreConfig.IdCarroEmMovimento);
            P(carroEmMovimento, "Carros/Carro_Taxi", Vector3.zero, 180);

            P(Novo("moeda"), "Props/Moeda", Vector3.zero);
            P(Novo("escudo"), "Props/Escudo", Vector3.zero);

            var chegada = Novo("chegada");
            foreach (float x in new[] { -MeiaRua - .3f, MeiaRua + .3f })
                F.Cilindro(chegada, "Poste", "Branco", F.V(x, 2.1f, 0), F.V(.3f, 4.2f, .3f));
            F.Cubo(chegada, "Faixa", "Mostarda", F.V(0, 3.9f, 0), F.V(MeiaRua * 2f + .9f, .9f, .16f));
            for (int i = 0; i < 12; i++)
            {
                float x = -MeiaRua + .325f + i * .65f;
                F.Cubo(chegada, "Quadrado", i % 2 == 0 ? "Grafite" : "Branco", F.V(x, 3.9f, -.09f), F.V(.65f, .45f, .02f));
                for (int fila = 0; fila < 2; fila++)
                    SemSombra(F.Cubo(chegada, "Piso", (i + fila) % 2 == 0 ? "Branco" : "Grafite", F.V(x, .01f, -.325f + fila * .65f), F.V(.65f, .014f, .65f)));
            }

            foreach (var m in lista) m.objeto.SetActive(false);
            return lista.ToArray();
        }

        // ------------------------------------------------------------------ chuva

        static Material AsfaltoMolhado()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(CaminhoDoAsfaltoMolhado);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, CaminhoDoAsfaltoMolhado);
            }
            Color c = ArtePaleta.Cor("Asfalto_Escuro");
            mat.SetColor("_BaseColor", new Color(c.r * .8f, c.g * .82f, c.b * .88f, 1f));
            mat.SetFloat("_Smoothness", .82f);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static ParticleSystem Chuva(Transform pai)
        {
            var go = F.Grupo("Chuva", pai, F.V(0, 13f, 14f), F.V(90, 0, 0));
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = .75f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(17f, 21f);
            main.startSize = new ParticleSystem.MinMaxCurve(.03f, .05f);
            main.startColor = new Color(.9f, .95f, 1f, .55f);
            main.maxParticles = 700;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emissao = ps.emission;
            emissao.rateOverTime = 420f;
            var forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(26f, 40f, 1f);

            var render = go.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Particula.mat");
            render.renderMode = ParticleSystemRenderMode.Stretch;
            render.lengthScale = 9f;
            render.velocityScale = 0f;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        // ------------------------------------------------------------------ interface

        static CorreHUD Hud(Transform canvas, out Dictionary<string, Button> botoes)
        {
            botoes = new Dictionary<string, Button>();
            var raiz = MenuConstrutor.Tela("HUD", canvas);
            var hud = raiz.gameObject.AddComponent<CorreHUD>();
            var voltarAoHub = raiz.gameObject.AddComponent<VoltarAoHub>();

            // Nível, escudos e moedas (canto de cima, à esquerda).
            var cartao = MenuConstrutor.Imagem("Painel_Dos_Escudos", raiz, MenuConstrutor.arredondado, MenuConstrutor.C("Branco", .92f), new Vector2(290, -90), new Vector2(520, 140));
            TravessiaConstrutor.Ancorar(cartao, new Vector2(0, 1));
            hud.textoDoNivel = MenuConstrutor.Texto("Nivel", cartao.transform, "Nível 1", 38, MenuConstrutor.C("Petroleo"), new Vector2(-105, 42), new Vector2(280, 48));
            var escudos = new Image[3];
            for (int i = 0; i < 3; i++)
                escudos[i] = MenuConstrutor.Imagem("Escudo_" + (i + 1), cartao.transform, MenuConstrutor.S("ui_escudo"), Color.white, new Vector2(-195 + i * 90, -22), new Vector2(80, 80));
            hud.escudos = escudos;
            MenuConstrutor.Imagem("Moeda", cartao.transform, MenuConstrutor.S("ui_moeda"), Color.white, new Vector2(110, 0), new Vector2(84, 84));
            hud.textoDasMoedas = MenuConstrutor.Texto("Moedas", cartao.transform, "0", 60, MenuConstrutor.C("Petroleo"), new Vector2(200, 0), new Vector2(110, 80));

            // Distância (em cima, no meio) e, logo abaixo, a dica do momento.
            const float larguraDaBarra = 760f;
            var trilho = MenuConstrutor.Imagem("Barra_De_Distancia", raiz, MenuConstrutor.arredondado, MenuConstrutor.C("Petroleo_Escuro", .9f), new Vector2(40, -46), new Vector2(larguraDaBarra + 12, 52));
            TravessiaConstrutor.Ancorar(trilho, new Vector2(.5f, 1));
            var cheio = MenuConstrutor.Imagem("Cheio", trilho.transform, MenuConstrutor.arredondado, MenuConstrutor.C("Mostarda"), new Vector2(6, 0), new Vector2(0, 40));
            var rtCheio = (RectTransform)cheio.transform;
            rtCheio.anchorMin = rtCheio.anchorMax = new Vector2(0, .5f);
            rtCheio.pivot = new Vector2(0, .5f);
            rtCheio.anchoredPosition = new Vector2(6, 0);
            hud.barraDeDistancia = rtCheio;
            hud.larguraDaBarra = larguraDaBarra;
            hud.textoDaDistancia = MenuConstrutor.Texto("Texto", trilho.transform, "0 m de 150 m", 30, MenuConstrutor.C("Branco"), Vector2.zero, new Vector2(larguraDaBarra, 44));

            var dica = MenuConstrutor.Imagem("Painel_Da_Dica", raiz, MenuConstrutor.arredondado, MenuConstrutor.C("Petroleo", .92f), new Vector2(40, -134), new Vector2(larguraDaBarra + 12, 96));
            TravessiaConstrutor.Ancorar(dica, new Vector2(.5f, 1));
            hud.textoDaDica = MenuConstrutor.Texto("Texto", dica.transform, "", 42, MenuConstrutor.C("Branco"), Vector2.zero, new Vector2(larguraDaBarra - 30, 86));
            hud.textoDaDica.resizeTextForBestFit = true; hud.textoDaDica.resizeTextMinSize = 26; hud.textoDaDica.resizeTextMaxSize = 42;
            hud.painelDaDica = dica.gameObject;

            var hubBotao = MenuConstrutor.Botao("Botao_VoltarAoHub", raiz, "", "Petroleo_Claro", new Vector2(-100, -95), new Vector2(120, 120), 0, MenuConstrutor.S("ui_seta_voltar"), MenuConstrutor.circulo);
            TravessiaConstrutor.Ancorar(hubBotao, new Vector2(1, 1));
            UnityEventTools.AddPersistentListener(hubBotao.onClick, voltarAoHub.Voltar);

            // Controles (embaixo): faixas à esquerda, pular e deslizar à direita. O teclado faz o mesmo.
            botoes["esquerda"] = MenuConstrutor.Botao("Botao_Esquerda", raiz, "", "Petroleo_Claro", new Vector2(140, 150), new Vector2(190, 190), 0, MenuConstrutor.S("ui_seta_voltar"), MenuConstrutor.circulo);
            TravessiaConstrutor.Ancorar(botoes["esquerda"], new Vector2(0, 0));
            botoes["direita"] = MenuConstrutor.Botao("Botao_Direita", raiz, "", "Petroleo_Claro", new Vector2(360, 150), new Vector2(190, 190), 0, MenuConstrutor.S("ui_seta_voltar"), MenuConstrutor.circulo);
            TravessiaConstrutor.Ancorar(botoes["direita"], new Vector2(0, 0));
            botoes["direita"].transform.Find("Frente/Icone").localScale = new Vector3(-1, 1, 1);

            botoes["pular"] = MenuConstrutor.Botao("Botao_Pular", raiz, "Pular", "Verde_Escuro", new Vector2(-210, 310), new Vector2(340, 160), 44, MenuConstrutor.S("ui_seta_cima"));
            TravessiaConstrutor.Ancorar(botoes["pular"], new Vector2(1, 0));
            botoes["deslizar"] = MenuConstrutor.Botao("Botao_Deslizar", raiz, "Deslizar", "Mostarda", new Vector2(-210, 125), new Vector2(340, 160), 44, MenuConstrutor.S("ui_seta_baixo"));
            TravessiaConstrutor.Ancorar(botoes["deslizar"], new Vector2(1, 0));

            var legenda = MenuConstrutor.Texto("Legenda_Do_Teclado", raiz, "Teclado: setas dos lados, para cima pula, para baixo desliza", 28, MenuConstrutor.C("Branco"), new Vector2(0, 28), new Vector2(1000, 40), FontStyle.Normal);
            TravessiaConstrutor.Ancorar(legenda, new Vector2(.5f, 0));
            var sombra = legenda.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0, 0, 0, .6f);
            sombra.effectDistance = new Vector2(2, -2);
            return hud;
        }

        // ------------------------------------------------------------------ config

        static TipoDeObstaculo T(string id, string regra, bool pulavel, bool deslizavel, float meio, float peso, string aviso)
        {
            return new TipoDeObstaculo { id = id, regra = regra, pulavel = pulavel, deslizavel = deslizavel, meioComprimento = meio, peso = peso, aviso = aviso };
        }

        static CorreConfig PrepararConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CorreConfig>(CaminhoDaConfig);
            if (config == null)
            {
                // O asset antigo com este nome era do runner 2D (RunnerConfig): sai pra dar lugar ao novo.
                if (AssetDatabase.LoadMainAssetAtPath(CaminhoDaConfig) != null) AssetDatabase.DeleteAsset(CaminhoDaConfig);
                config = ScriptableObject.CreateInstance<CorreConfig>();
                AssetDatabase.CreateAsset(config, CaminhoDaConfig);
            }
            // Só preenche os obstáculos se estiver vazio: não sobrescreve ajuste feito no Inspector.
            if (config.tipos.Count == 0)
            {
                config.tipos = new List<TipoDeObstaculo>
                {
                    T("buraco", "", true, false, .55f, 1f, "Buraco! Pule para não cair."),
                    T("cones", "", false, false, .4f, 1f, "Cones: tem obra. Desvie!"),
                    T("poca", "pocas", true, false, .6f, 1f, "Poça escorrega! Pule ou desvie."),
                    T("carro", "carros", false, false, 1.9f, .8f, "Carro parado! Passe pelo lado."),
                    T("barreira", "barreiras", false, false, .45f, .7f, "Barreira: rua em obra. Desvie!"),
                    T("galho", "galhos", false, true, .4f, 1.1f, "Galho baixo! Deslize por baixo."),
                    T(CorreConfig.IdCarroEmMovimento, CorrePista.RegraCarrosEmMovimento, false, false, 1.9f, 1f, "Carro vindo! Saia da frente."),
                };
            }
            config.musica = TravessiaConstrutor.Som("Musica/corre_correndo.wav");
            config.passos = new[] { "000", "001", "002" }.Select(n => TravessiaConstrutor.Som($"Efeitos/footstep_concrete_{n}.ogg")).ToArray();
            config.pulo = TravessiaConstrutor.Som("Efeitos/pulo.wav");
            config.deslizar = TravessiaConstrutor.Som("Efeitos/deslizar.wav");
            config.moeda = TravessiaConstrutor.Som("Efeitos/moeda.wav");
            config.escudo = TravessiaConstrutor.Som("Interface/confirmation_003.ogg");
            config.batida = new[] { "000", "001", "002" }.Select(n => TravessiaConstrutor.Som($"Efeitos/impactSoft_medium_{n}.ogg")).ToArray();
            config.tropeco = TravessiaConstrutor.Som("Efeitos/tropeco.wav");
            config.buzina = TravessiaConstrutor.Som("Efeitos/buzina.wav");
            config.acerto = TravessiaConstrutor.Som("Interface/confirmation_002.ogg");
            config.erro = TravessiaConstrutor.Som("Interface/error_004.ogg");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }
    }
}
