using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CidadeViva.EditorArte
{
    // Monta do zero a cena 3D do "Travessia Segura" com os prefabs de arte.
    // A rua corre ao longo de X; a criança atravessa de -Z (calçada de cá) pra +Z (calçada de lá).
    // A largura da rua muda com o nível: o asfalto, as pinturas e o grupo "Lado_De_La" são
    // ajustados em tempo de jogo pelo TravessiaManager.
    public static class TravessiaConstrutor
    {
        const string CaminhoDaCena = "Assets/Scenes/Minigame_TravessiaSegura.unity";
        const string CaminhoDaConfig = "Assets/Configs/TravessiaSegura_Config.asset";
        const string CaminhoDaParticula = "Assets/Materials/Particula.mat";
        const float MeioComprimento = 48f;

        [MenuItem("Cidade Viva/Minigames/Reconstruir a cena do Travessia Segura")]
        public static void ReconstruirPeloMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Reconstruir();
        }

        internal static GameObject P(Transform pai, string prefab, Vector3 pos, float rotY = 0f, float escala = 1f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArteImportador.PastaPrefabs}/{prefab}.prefab");
            if (asset == null) { Debug.LogError("[Travessia] Prefab não encontrado: " + prefab); return new GameObject("FALTOU_" + prefab); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, pai);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(0, rotY, 0);
            go.transform.localScale = Vector3.one * escala;
            return go;
        }

        public static void Reconstruir()
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Depois de criar a cena: trocar de cena descarrega assets que ninguém está usando.
            MenuConstrutor.PrepararIcones();
            var niveis = NiveisConstrutor.SobrescreverNiveisDaTravessia();
            var config = PrepararConfig();

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 42f;
            cam.transform.position = new Vector3(0, 13f, -10f);
            cam.transform.LookAt(new Vector3(0, 0, 3f));
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AmbienteConstrutor.CaminhoPrefab) == null) AmbienteConstrutor.CriarPrefab();
            AmbienteConstrutor.AplicarNaCenaAberta(true);

            // ---------------- cenário ----------------
            var cenario = F.Grupo("Cenario").transform;
            var chao = F.Plano(cenario, "Gramado", "Verde_Claro", F.V(0, -.03f, 20), 400, 400);
            chao.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Cubo de 1 m de profundidade: o gerente estica em Z até a largura da rua.
            var asfalto = F.Cubo(cenario, "Asfalto", "Asfalto", F.V(0, -.05f, 2f), F.V(MeioComprimento * 2f + 8f, .1f, 1f));
            asfalto.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var ladoDeCa = F.Grupo("Lado_De_Ca", cenario).transform;
            Calcada(ladoDeCa, -1.5f);
            Calcada(ladoDeCa, -4.5f);
            // Deste lado fica só coisa baixa, pra não tapar a visão da câmera.
            P(ladoDeCa, "Natureza/Arbusto_1", F.V(-9f, .15f, -4.2f), 20, .8f);
            P(ladoDeCa, "Natureza/Arbusto_2", F.V(9.5f, .15f, -4.4f), 0, .8f);
            P(ladoDeCa, "Natureza/Flores", F.V(-12f, .15f, -3.8f), 0, 1.3f);
            P(ladoDeCa, "Natureza/Flores", F.V(13f, .15f, -4f), 40, 1.3f);
            P(ladoDeCa, "Props/Banco", F.V(-15f, .15f, -3.6f));
            P(ladoDeCa, "Props/Lixeira_De_Rua", F.V(-17.5f, .15f, -3.6f));
            P(ladoDeCa, "Props/Pilarete", F.V(-3.1f, .15f, -.5f));
            P(ladoDeCa, "Props/Pilarete", F.V(3.1f, .15f, -.5f));
            var semaforoDeCa = P(ladoDeCa, "Props/Semaforo_Pedestres", F.V(-3.3f, .15f, -.9f), 0);

            var ladoDeLa = F.Grupo("Lado_De_La", cenario, F.V(0, 0, 4f)).transform;
            Calcada(ladoDeLa, 1.5f);
            Calcada(ladoDeLa, 4.5f);
            string[] casas = { "Casa_02", "Casa_05", "Padaria", "Casa_03", "Escola", "Casa_07", "Casa_01", "Predio_Comercial_Loja", "Casa_08" };
            float[] xDasCasas = { -42f, -31f, -20f, -9f, 6f, 20f, 29f, 39f, 50f };
            for (int i = 0; i < casas.Length; i++)
                P(ladoDeLa, "Predios/" + casas[i], F.V(xDasCasas[i], 0, 12.5f), 180);
            string[] arvores = { "Arvore_Comum_1", "Arvore_Betula", "Arvore_Comum_2", "Palmeira_3", "Arvore_Comum_3", "Arvore_Salgueiro" };
            float[] xDasArvores = { -36f, -25.5f, -14.5f, 13f, 24.5f, 34f };
            for (int i = 0; i < arvores.Length; i++)
                P(ladoDeLa, "Natureza/" + arvores[i], F.V(xDasArvores[i], .15f, 5.2f), i * 50, .85f);
            P(ladoDeLa, "Props/Placa_FaixaDePedestre", F.V(-3.4f, .15f, .8f), 180);
            P(ladoDeLa, "Props/Ponto_De_Onibus", F.V(17f, .15f, 2.6f), 180);
            P(ladoDeLa, "Props/Banco", F.V(-8.5f, .15f, 3.4f), 180);
            P(ladoDeLa, "Props/Lixeira_De_Rua", F.V(-11f, .15f, 3.4f), 180);
            P(ladoDeLa, "Props/Hidrante", F.V(8.5f, .15f, 1f), 180);
            // Quem espera do outro lado: é pra lá que a criança quer chegar.
            P(ladoDeLa, "Personagens/Familia_Mae_Animado", F.V(1.6f, .15f, 2.3f), 190);
            P(ladoDeLa, "Natureza/Cachorro_Caramelo", F.V(2.7f, .15f, 2.1f), 200);
            P(ladoDeLa, "Personagens/Idoso", F.V(-8.4f, .15f, 2.6f), 170);
            var semaforoDeLa = P(ladoDeLa, "Props/Semaforo_Pedestres", F.V(3.3f, .15f, .9f), 180);

            var pinturas = F.Grupo("Pinturas_Da_Rua", cenario).transform;

            // Modelos inativos: o gerente copia pra pintar a rua, mostrar as setas e criar ciclistas.
            var modelos = F.Grupo("Modelos", cenario).transform;
            var traco = Pintura(modelos, "Modelo_Traco", "Branco");
            var listra = Pintura(modelos, "Modelo_Listra", "Branco");
            var parada = Pintura(modelos, "Modelo_LinhaDeParada", "Branco");
            var ciclovia = Pintura(modelos, "Modelo_Ciclovia", "Terracota");
            var seta = new GameObject("Modelo_Seta").AddComponent<SpriteRenderer>();
            seta.transform.SetParent(modelos, false);
            seta.sprite = MenuConstrutor.S("ui_seta_larga");
            seta.transform.localScale = Vector3.one * .85f;
            seta.sortingOrder = 10;
            seta.gameObject.SetActive(false);
            var ciclista = Ciclista(modelos);

            // ---------------- jogador, trânsito, efeitos ----------------
            var jogadorGo = F.Grupo("Jogador", null, F.V(0, .15f, -1.3f));
            var visual = F.Grupo("Visual", jogadorGo.transform).transform;
            var personagem = P(visual, "Personagens/Familia_Filha_Animado", Vector3.zero);
            var jogador = jogadorGo.AddComponent<TravessiaJogador>();
            jogador.visual = visual;
            jogador.animator = personagem.GetComponentInChildren<Animator>();

            var trafego = F.Grupo("Transito").AddComponent<TravessiaTrafego>();

            var semaforo = F.Grupo("Semaforo_De_Pedestres").AddComponent<TravessiaSemaforo>();
            semaforo.postes = new[] { semaforoDeCa, semaforoDeLa };
            semaforo.luzesPare = Luzes("Luz_Pare", semaforoDeCa, semaforoDeLa);
            semaforo.luzesSiga = Luzes("Luz_Siga", semaforoDeCa, semaforoDeLa);

            var efeitos = F.Grupo("Efeitos").transform;
            var confete = Particulas(efeitos, "Confete", 70, 1.6f, 6f, .22f, 2.5f,
                "Mostarda", "Vermelho", "Petroleo_Claro", "Verde", "Rosa", "Laranja");
            var estrelinhas = Particulas(efeitos, "Estrelinhas", 14, .7f, 3.5f, .28f, .6f, "Mostarda", "Mostarda_Clara", "Branco");

            // ---------------- interface ----------------
            var canvas = MenuConstrutor.NovoCanvas("Canvas");
            MenuConstrutor.NovoEventSystem(false);
            var hud = Hud(canvas.transform, out var botoes);

            var fim = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabFim), canvas.transform);
            var selecao = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabSelecao), canvas.transform);

            // ---------------- gerente ----------------
            var gerenteGo = new GameObject("TravessiaManager", typeof(AudioSource));
            var somDoJogo = gerenteGo.GetComponent<AudioSource>();
            somDoJogo.playOnAwake = false;
            somDoJogo.volume = .8f;
            var gerente = gerenteGo.AddComponent<TravessiaManager>();
            gerente.nomeDaArea = "Travessia Segura";
            gerente.telaFimDeJogo = fim.GetComponent<TelaFimDeJogo>();
            gerente.telaDeSelecao = selecao.GetComponent<TelaSelecaoDeNivel>();
            gerente.niveis = niveis;
            gerente.config = config;
            gerente.jogador = jogador;
            gerente.trafego = trafego;
            gerente.semaforo = semaforo;
            gerente.hud = hud;
            gerente.cameraDoJogo = cam;
            gerente.som = somDoJogo;
            gerente.asfalto = asfalto.transform;
            gerente.ladoDeLa = ladoDeLa;
            gerente.pinturas = pinturas;
            gerente.modeloDeTraco = traco;
            gerente.modeloDeListra = listra;
            gerente.modeloDeLinhaDeParada = parada;
            gerente.modeloDeCiclovia = ciclovia;
            gerente.modeloDeCiclista = ciclista;
            gerente.modeloDeSeta = seta;
            gerente.spritePerigo = MenuConstrutor.S("ui_seta_larga");
            gerente.spriteLivre = MenuConstrutor.S("ui_livre");
            gerente.corPerigo = ArtePaleta.Cor("Vermelho");
            gerente.corLivre = ArtePaleta.Cor("Verde_Escuro");
            gerente.confete = confete;
            gerente.estrelinhas = estrelinhas;

            UnityEventTools.AddPersistentListener(botoes["andar"].onClick, gerente.Andar);
            UnityEventTools.AddPersistentListener(botoes["voltar"].onClick, gerente.Voltar);
            UnityEventTools.AddPersistentListener(botoes["olhar"].onClick, gerente.Olhar);
            UnityEventTools.AddPersistentListener(botoes["esquerda"].onClick, gerente.IrParaAEsquerda);
            UnityEventTools.AddPersistentListener(botoes["direita"].onClick, gerente.IrParaADireita);

            EditorSceneManager.SaveScene(cena, CaminhoDaCena);
        }

        // ------------------------------------------------------------------ peças do cenário

        static void Calcada(Transform pai, float z)
        {
            for (float x = -MeioComprimento; x <= MeioComprimento; x += 3f)
                P(pai, "Ruas/Calcada_SemMeioFio", F.V(x, 0, z));
        }

        // Pintura de rua: pai de escala livre (o gerente define comprimento e largura) com uma chapa fina dentro.
        static GameObject Pintura(Transform pai, string nome, string cor)
        {
            var go = F.Grupo(nome, pai);
            var chapa = F.Cubo(go.transform, "Chapa", cor, Vector3.zero, F.V(1f, .012f, 1f));
            chapa.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            return go;
        }

        static Renderer[] Luzes(string nome, params GameObject[] postes)
        {
            var lista = new List<Renderer>();
            foreach (var poste in postes)
                lista.AddRange(poste.GetComponentsInChildren<Renderer>(true).Where(r => r.name == nome));
            if (lista.Count != postes.Length) Debug.LogWarning($"[Travessia] Esperava {postes.Length} luzes '{nome}', achei {lista.Count}.");
            return lista.ToArray();
        }

        // Bicicleta com alguém sentado. A pose sentada é gravada aqui (sem Animator), pra pessoa não
        // voltar a ficar em pé quando o jogo começa.
        static GameObject Ciclista(Transform pai)
        {
            var raiz = F.Grupo("Modelo_Ciclista", pai);
            P(raiz.transform, "Props/Bicicleta", Vector3.zero, 0, 1.05f);
            var pessoa = P(raiz.transform, "Personagens/Familia_FilhoMaisVelho_Animado", F.V(0, .12f, -.2f));
            var animator = pessoa.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                var sentar = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == "SitDown");
                if (sentar != null) sentar.SampleAnimation(animator.gameObject, sentar.length);
                Object.DestroyImmediate(animator);
            }
            // Capacete: ciclista do jogo sempre anda de capacete.
            F.M(raiz.transform, "Capacete", "Semiesfera", "Mostarda", F.V(0, 1.42f, -.2f), F.V(.42f, .4f, .46f));
            raiz.SetActive(false);
            return raiz;
        }

        internal static ParticleSystem Particulas(Transform pai, string nome, int quantidade, float duracao, float velocidade, float tamanho, float gravidade, params string[] cores)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(CaminhoDaParticula);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, CaminhoDaParticula);
            }

            var go = F.Grupo(nome, pai);
            go.transform.localEulerAngles = new Vector3(-90, 0, 0);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = .4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(duracao * .7f, duracao);
            main.startSpeed = new ParticleSystem.MinMaxCurve(velocidade * .5f, velocidade);
            main.startSize = new ParticleSystem.MinMaxCurve(tamanho * .6f, tamanho);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2f);
            main.gravityModifier = gravidade;
            main.maxParticles = quantidade * 2;
            var gradiente = new Gradient();
            gradiente.mode = GradientMode.Fixed;
            gradiente.SetKeys(
                cores.Select((c, i) => new GradientColorKey(ArtePaleta.Cor(c), (i + 1f) / cores.Length)).ToArray(),
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(gradiente) { mode = ParticleSystemGradientMode.RandomColor };

            var emissao = ps.emission;
            emissao.rateOverTime = 0;
            emissao.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)quantidade) });
            var forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Cone;
            forma.angle = 38f;
            forma.radius = .3f;
            var some = ps.colorOverLifetime;
            some.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .7f), new GradientAlphaKey(0, 1) });
            some.color = fade;
            var giro = ps.rotationOverLifetime;
            giro.enabled = true;
            giro.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

            var render = go.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = mat;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        // ------------------------------------------------------------------ interface

        internal static void Ancorar(Component c, Vector2 ancora)
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = rt.anchorMax = ancora;
        }

        static TravessiaHUD Hud(Transform canvas, out Dictionary<string, Button> botoes)
        {
            botoes = new Dictionary<string, Button>();
            var raiz = MenuConstrutor.Tela("HUD", canvas);
            var hud = raiz.gameObject.AddComponent<TravessiaHUD>();
            var voltarAoHub = raiz.gameObject.AddComponent<VoltarAoHub>();

            // Vidas e nível (canto de cima, à esquerda).
            var cartao = MenuConstrutor.Imagem("Painel_Das_Vidas", raiz, MenuConstrutor.arredondado, MenuConstrutor.C("Branco", .92f), new Vector2(250, -90), new Vector2(440, 140));
            Ancorar(cartao, new Vector2(0, 1));
            hud.textoDoNivel = MenuConstrutor.Texto("Nivel", cartao.transform, "Nível 1", 40, MenuConstrutor.C("Petroleo"), new Vector2(0, 40), new Vector2(400, 50));
            var vidas = new Image[3];
            for (int i = 0; i < 3; i++)
                vidas[i] = MenuConstrutor.Imagem("Vida_" + (i + 1), cartao.transform, MenuConstrutor.S("figura_estrela"), Color.white, new Vector2(-110 + i * 110, -22), new Vector2(84, 84));
            hud.vidas = vidas;

            // Dica do momento (em cima, no meio).
            var dica = MenuConstrutor.Imagem("Painel_Da_Dica", raiz, MenuConstrutor.arredondado, MenuConstrutor.C("Petroleo", .92f), new Vector2(0, -80), new Vector2(980, 100));
            Ancorar(dica, new Vector2(.5f, 1));
            hud.textoDaDica = MenuConstrutor.Texto("Texto", dica.transform, "", 42, MenuConstrutor.C("Branco"), Vector2.zero, new Vector2(940, 90));
            hud.textoDaDica.resizeTextForBestFit = true; hud.textoDaDica.resizeTextMinSize = 26; hud.textoDaDica.resizeTextMaxSize = 42;
            hud.painelDaDica = dica.gameObject;

            var ambulancia = MenuConstrutor.Imagem("Aviso_Ambulancia", raiz, MenuConstrutor.arredondado, MenuConstrutor.C("Vermelho", .95f), new Vector2(0, -195), new Vector2(760, 96));
            Ancorar(ambulancia, new Vector2(.5f, 1));
            MenuConstrutor.Imagem("Icone", ambulancia.transform, MenuConstrutor.S("ui_aviso"), Color.white, new Vector2(-320, 0), new Vector2(70, 70));
            MenuConstrutor.Texto("Texto", ambulancia.transform, "Ambulância! Deixe ela passar.", 40, MenuConstrutor.C("Branco"), new Vector2(35, 0), new Vector2(640, 80));
            hud.avisoDeAmbulancia = ambulancia.gameObject;

            // Canto de cima, à direita: voltar ao Hub e o espelho do semáforo.
            var hubBotao = MenuConstrutor.Botao("Botao_VoltarAoHub", raiz, "", "Petroleo_Claro", new Vector2(-100, -95), new Vector2(120, 120), 0, MenuConstrutor.S("ui_seta_voltar"), MenuConstrutor.circulo);
            Ancorar(hubBotao, new Vector2(1, 1));
            UnityEventTools.AddPersistentListener(hubBotao.onClick, voltarAoHub.Voltar);
            var painelSinal = MenuConstrutor.Imagem("Painel_Do_Semaforo", raiz, MenuConstrutor.arredondado, MenuConstrutor.C("Grafite", .95f), new Vector2(-100, -260), new Vector2(150, 170));
            Ancorar(painelSinal, new Vector2(1, 1));
            hud.luzDoSemaforo = MenuConstrutor.Imagem("Luz", painelSinal.transform, MenuConstrutor.S("ui_pedestre_pare"), Color.white, Vector2.zero, new Vector2(126, 126));
            hud.painelDoSemaforo = painelSinal.gameObject;
            hud.spritePare = MenuConstrutor.S("ui_pedestre_pare");
            hud.spriteSiga = MenuConstrutor.S("ui_pedestre_siga");

            // Controles (embaixo): grandes, pra mouse e toque. O teclado faz o mesmo.
            var lados = MenuConstrutor.Ret("Botoes_De_Lado", raiz, new Vector2(230, 150), new Vector2(360, 170));
            Ancorar(lados, new Vector2(0, 0));
            botoes["esquerda"] = MenuConstrutor.Botao("Botao_Esquerda", lados, "", "Petroleo_Claro", new Vector2(-90, 0), new Vector2(160, 160), 0, MenuConstrutor.S("ui_seta_voltar"), MenuConstrutor.circulo);
            botoes["direita"] = MenuConstrutor.Botao("Botao_Direita", lados, "", "Petroleo_Claro", new Vector2(90, 0), new Vector2(160, 160), 0, MenuConstrutor.S("ui_seta_voltar"), MenuConstrutor.circulo);
            botoes["direita"].transform.Find("Frente/Icone").localScale = new Vector3(-1, 1, 1);
            hud.botoesDeLado = lados.gameObject;

            botoes["olhar"] = MenuConstrutor.Botao("Botao_Olhar", raiz, "Olhar", "Mostarda", new Vector2(-230, 150), new Vector2(400, 200), 50, MenuConstrutor.S("ui_olho"));
            Ancorar(botoes["olhar"], new Vector2(.5f, 0));
            botoes["andar"] = MenuConstrutor.Botao("Botao_Andar", raiz, "Andar", "Verde_Escuro", new Vector2(230, 150), new Vector2(400, 200), 50, MenuConstrutor.S("ui_seta_cima"));
            Ancorar(botoes["andar"], new Vector2(.5f, 0));
            botoes["voltar"] = MenuConstrutor.Botao("Botao_VoltarUmPasso", raiz, "Voltar um passo", "Cinza", new Vector2(-220, 130), new Vector2(360, 120), 38);
            Ancorar(botoes["voltar"], new Vector2(1, 0));

            var legenda = MenuConstrutor.Texto("Legenda_Do_Teclado", raiz, "Teclado: setas para andar, E para olhar", 28, MenuConstrutor.C("Branco"), new Vector2(0, 28), new Vector2(900, 40), FontStyle.Normal);
            Ancorar(legenda, new Vector2(.5f, 0));
            var sombra = legenda.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0, 0, 0, .6f);
            sombra.effectDistance = new Vector2(2, -2);
            return hud;
        }

        // ------------------------------------------------------------------ config

        internal static AudioClip Som(string caminho)
        {
            var clipe = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + caminho);
            if (clipe == null) Debug.LogWarning("[Travessia] Som não encontrado: " + caminho);
            return clipe;
        }

        static TravessiaConfig PrepararConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<TravessiaConfig>(CaminhoDaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<TravessiaConfig>();
                AssetDatabase.CreateAsset(config, CaminhoDaConfig);
            }
            config.carros = new[] { "Carro_Sedan", "Carro_Hatch", "Carro_SUV", "Carro_Taxi", "Carro_Van", "Carro_Picape", "Carro_Esportivo", "Carro_SUV_Luxo" }
                .Select(n => AssetDatabase.LoadAssetAtPath<GameObject>($"{ArteImportador.PastaPrefabs}/Carros/{n}.prefab")).Where(g => g != null).ToArray();
            config.ambulancia = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArteImportador.PastaPrefabs}/Carros/Ambulancia.prefab");
            config.passos = new[] { Som("Efeitos/footstep_concrete_000.ogg"), Som("Efeitos/footstep_concrete_001.ogg"), Som("Efeitos/footstep_concrete_002.ogg") };
            config.buzina = Som("Efeitos/buzina.wav");
            config.sirene = Som("Efeitos/sirene.wav");
            config.campainhaDeBicicleta = Som("Efeitos/campainha_bicicleta.wav");
            config.tropeco = Som("Efeitos/tropeco.wav");
            config.acerto = Som("Interface/confirmation_002.ogg");
            config.erro = Som("Interface/error_004.ogg");
            config.olhar = Som("Interface/select_001.ogg");
            config.bloqueado = Som("Interface/bong_001.ogg");
            config.sinalVerde = Som("Interface/confirmation_001.ogg");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }
    }
}
