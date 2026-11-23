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
    // Monta as cenas "Abertura" e "Final" (a história do jogo em 3D), com os modelos que já existem:
    // prédios de Brasília, placas, semáforos, a família animada, água (cubo com a cor da paleta), chuva e confete.
    // Os textos da história NÃO vêm do glossário e não citam definições (lista em Temp/historia_textos.txt).
    public static class HistoriaConstrutor
    {
        const string PastaDasCenas = "Assets/Scenes/";

        // ------------------------------------------------------------------ textos (pra revisão da coordenação)
        public static readonly string[] LegendasDaAbertura =
        {
            "Era um dia comum em Brasília.",
            "De repente, veio uma tempestade muito forte!",
            "A água subiu e levou as placas e os semáforos.",
            "Quando a água baixou, a cidade estava toda bagunçada.",
            "A família olhou a cidade e decidiu: vamos reconstruir tudo juntos!",
        };
        public static readonly string[] LegendasDoFinal =
        {
            "Placa por placa, semáforo por semáforo, a cidade voltou a funcionar.",
            "A família se reuniu pra comemorar.",
        };
        public const string TituloDoFinal = "Você reconstruiu a cidade!";
        public const string TotalDoFinal = "{0} de {1} verbetes do Glotrânsito";
        public const string BotaoDoFinal = "Voltar ao Hub";

        [MenuItem("Cidade Viva/História/Reconstruir a abertura e o final")]
        public static void ReconstruirPeloMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Reconstruir();
        }

        public static void Reconstruir()
        {
            Cena(false);
            Cena(true);
            GravarTextos();
            // As duas cenas entram no Build Settings logo depois do Hub.
            var lista = EditorBuildSettings.scenes.Where(s => !s.path.EndsWith("/Abertura.unity") && !s.path.EndsWith("/Final.unity")).ToList();
            int depoisDoHub = lista.FindIndex(s => s.path.EndsWith("/Hub.unity")) + 1;
            lista.Insert(depoisDoHub, new EditorBuildSettingsScene(PastaDasCenas + "Abertura.unity", true));
            lista.Insert(depoisDoHub + 1, new EditorBuildSettingsScene(PastaDasCenas + "Final.unity", true));
            EditorBuildSettings.scenes = lista.ToArray();
        }

        static void GravarTextos()
        {
            var sb = new StringBuilder();
            sb.AppendLine("TEXTOS DA HISTÓRIA (não vêm do glossário; não citam definições). Pra revisão da coordenação.");
            sb.AppendLine().AppendLine("ABERTURA (legendas, na ordem):");
            foreach (var t in LegendasDaAbertura) sb.AppendLine("  - " + t);
            sb.AppendLine().AppendLine("FINAL (legendas, na ordem):");
            foreach (var t in LegendasDoFinal) sb.AppendLine("  - " + t);
            sb.AppendLine("  - Título da tela: " + TituloDoFinal);
            sb.AppendLine("  - Linha do total: " + TotalDoFinal.Replace("{0}", "X").Replace("{1}", "38"));
            sb.AppendLine("  - Botão: " + BotaoDoFinal);
            sb.AppendLine().AppendLine("BOTÕES: Pular, Ouvir; no menu: Ver abertura; no Álbum: Ver o final.");
            File.WriteAllText("Temp/historia_textos.txt", sb.ToString(), Encoding.UTF8);
        }

        static GameObject P(Transform pai, string prefab, Vector3 pos, float rotY = 0f, float escala = 1f)
        {
            return TravessiaConstrutor.P(pai, prefab, pos, rotY, escala);
        }

        static Transform Pose(Transform pai, string nome, Vector3 pos, Vector3 alvo)
        {
            var t = F.Grupo(nome, pai).transform;
            t.position = pos;
            t.rotation = Quaternion.LookRotation(alvo - pos);
            return t;
        }

        static PlanoDaHistoria Plano(Transform poses, int i, Vector3 de, Vector3 alvoDe, Vector3 ate, Vector3 alvoAte, float duracao, string legenda, AcaoDaHistoria acao)
        {
            return new PlanoDaHistoria
            {
                de = Pose(poses, $"Plano_{i + 1}_De", de, alvoDe),
                ate = Pose(poses, $"Plano_{i + 1}_Ate", ate, alvoAte),
                duracao = duracao, legenda = legenda, acao = acao,
            };
        }

        // ------------------------------------------------------------------ cena

        static void Cena(bool final)
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrepararIcones();
            VerbetesConstrutor.Atualizar();

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 55f;
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 500f;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AmbienteConstrutor.CaminhoPrefab) == null) AmbienteConstrutor.CriarPrefab();
            AmbienteConstrutor.AplicarNaCenaAberta(true);

            var raiz = F.Grupo("Cidade").transform;
            Cidade(raiz, final, out var levados, out var bagunca);
            var familia = Familia(raiz, final);

            var efeitos = F.Grupo("Efeitos").transform;
            var confete = TravessiaConstrutor.Particulas(efeitos, "Confete", 120, 2.2f, 8f, .35f, 1.6f, "Mostarda", "Vermelho", "Petroleo_Claro", "Verde", "Rosa", "Laranja");
            confete.transform.position = new Vector3(0, 4.5f, 10);
            confete.transform.eulerAngles = new Vector3(-90, 0, 0);
            ParticleSystem chuva = final ? null : Chuva(efeitos);
            Transform agua = null;
            if (!final)
            {
                agua = F.Cubo(raiz, "Agua_Da_Enchente", "Agua", F.V(0, -.4f, 15), F.V(320, .1f, 320)).transform;
                agua.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var gerente = new GameObject(final ? "Final" : "Abertura", typeof(AudioSource));
            var som = gerente.GetComponent<AudioSource>();
            som.playOnAwake = false;
            var historia = gerente.AddComponent<CenaDaHistoria>();
            historia.ehFinal = final;
            historia.cameraDaHistoria = cam;
            historia.chuva = chuva;
            historia.agua = agua;
            historia.levados = levados.ToArray();
            historia.bagunca = bagunca;
            historia.familia = familia;
            historia.confete = confete;
            historia.som = som;
            historia.somDoTrovao = TravessiaConstrutor.Som("Efeitos/impactPunch_medium_000.ogg");
            historia.somDaComemoracao = TravessiaConstrutor.Som("Interface/confirmation_003.ogg");
            historia.formatoDoTotal = TotalDoFinal;

            var poses = F.Grupo("Poses_Da_Camera").transform;
            if (!final)
            {
                var L = LegendasDaAbertura;
                historia.planos = new[]
                {
                    Plano(poses, 0, F.V(-34, 24, -66), F.V(0, 6, 50), F.V(-9, 10, -24), F.V(0, 5, 55), 7f, L[0], AcaoDaHistoria.Nada),
                    Plano(poses, 1, F.V(13, 6, -12), F.V(0, 7, 45), F.V(7, 7.5f, 2), F.V(0, 10, 55), 6f, L[1], AcaoDaHistoria.Tempestade),
                    Plano(poses, 2, F.V(15, 5, 5), F.V(1, 1, 10), F.V(13, 5.5f, 13.5f), F.V(-2, .6f, 16), 9f, L[2], AcaoDaHistoria.Enchente),
                    Plano(poses, 3, F.V(-14, 9, -2), F.V(2, 0, 14), F.V(-6, 11, 26), F.V(3, 0, 10), 6f, L[3], AcaoDaHistoria.AguaBaixa),
                    Plano(poses, 4, F.V(1, 2.8f, -44), F.V(0, 1.5f, -22), F.V(5.5f, 2.3f, -27.2f), F.V(.4f, 1f, -34.2f), 8f, L[4], AcaoDaHistoria.Familia),
                };
                cam.transform.SetPositionAndRotation(historia.planos[0].de.position, historia.planos[0].de.rotation);
            }
            else
            {
                var L = LegendasDoFinal;
                historia.planos = new[]
                {
                    Plano(poses, 0, F.V(-34, 24, -66), F.V(0, 6, 50), F.V(-7, 7, -10), F.V(0, 2.5f, 25), 8f, L[0], AcaoDaHistoria.Nada),
                    Plano(poses, 1, F.V(7, 3.2f, -1), F.V(0, 1.2f, 10), F.V(0, 1.9f, 4.5f), F.V(0, 1.2f, 10), 7f, L[1], AcaoDaHistoria.Comemora),
                };
                cam.transform.SetPositionAndRotation(historia.planos[0].de.position, historia.planos[0].de.rotation);
            }

            var canvas = NovoCanvas("Canvas");
            NovoEventSystem(false);
            Hud(canvas.transform, historia, final);

            OtimizacaoWebGL.TirarSombraDosPequenos();
            EditorSceneManager.SaveScene(cena, PastaDasCenas + (final ? "Final" : "Abertura") + ".unity");
        }

        // ------------------------------------------------------------------ cidade

        // Eixo (norte-sul, x = 0) cruzado por uma avenida (z = 10), com os prédios de Brasília em volta.
        // Na abertura, placas e semáforos estão no lugar (e a água leva); na bagunça (depois da água) há lama,
        // entulho e árvores secas. No final, tudo no lugar, com flores, árvores e gente.
        static void Cidade(Transform raiz, bool final, out List<Transform> levados, out GameObject bagunca)
        {
            levados = new List<Transform>();
            var chao = F.Grupo("Chao", raiz).transform;
            F.Plano(chao, "Gramado", "Verde_Claro", F.V(0, 0, 20), 500, 500);
            F.Caixa(chao, "Eixo", "Asfalto", 0, 0, 15, 14, .04f, 160);
            F.Caixa(chao, "Avenida", "Asfalto", 0, 0, 10, 180, .05f, 12);
            foreach (float x in new[] { -8.5f, 8.5f })
                F.Caixa(chao, "Calcada", "Cinza_Claro", x, 0, 15, 3, .1f, 160);
            for (float z = -60; z < 90; z += 5)
                if (Mathf.Abs(z - 10) > 8) F.Cubo(chao, "Traco", "Branco", F.V(0, .045f, z), F.V(.18f, .01f, 2.4f));
            for (float x = -6; x <= 6; x += 1.2f)
            {
                F.Cubo(chao, "Faixa_De_Pedestre", "Branco", F.V(x, .045f, 1.6f), F.V(.6f, .01f, 2.6f));
                F.Cubo(chao, "Faixa_De_Pedestre", "Branco", F.V(x, .055f, 18.4f), F.V(.6f, .01f, 2.6f));
            }
            foreach (var r in chao.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var predios = F.Grupo("Predios", raiz).transform;
            P(predios, "Predios/Brasilia_Congresso", F.V(0, 0, 84), 180);
            P(predios, "Predios/Brasilia_Catedral", F.V(-26, 0, 42));
            P(predios, "Predios/Brasilia_Museu_Cupula", F.V(28, 0, 44), 180);
            P(predios, "Predios/Brasilia_Palacio_Arcos", F.V(30, 0, 22), -90);
            P(predios, "Predios/Brasilia_Bloco_Superquadra", F.V(-26, 0, -6), 90);
            P(predios, "Predios/Brasilia_Bloco_Superquadra", F.V(26, 0, -14), -90);
            P(predios, "Predios/Escola", F.V(-22, 0, 22), 90);
            P(predios, "Predios/Padaria", F.V(19, 0, 0), -90);
            // Ao sul do Eixo, atrás da família no fim da abertura (a câmera olha pra lá): mais cidade.
            P(predios, "Predios/Brasilia_Palacio_Arcos", F.V(0, 0, -76), 0);
            P(predios, "Predios/Brasilia_Bloco_Superquadra", F.V(-14, 0, -82), 0);
            P(predios, "Predios/Brasilia_Bloco_Superquadra", F.V(24, 0, -62), 0);
            P(predios, "Predios/Comercio_B", F.V(-11, 0, -47), 0, .9f);
            P(predios, "Predios/Predio_Comercial_Loja", F.V(12, 0, -48), 0, .9f);
            P(predios, "Predios/Casa_03", F.V(-17, 0, -44), 30, .9f);

            var verde = F.Grupo("Arvores", raiz).transform;
            for (float z = -50; z <= 70; z += 12)
                foreach (float x in new[] { -11.5f, 11.5f })
                    if (Mathf.Abs(z - 10) > 9) P(verde, z % 24 == 0 ? "Natureza/Palmeira_1" : "Natureza/Arvore_Comum_2", F.V(x, 0, z), z * 7, .9f);

            // Sinalização: semáforos no cruzamento e placas ao longo do Eixo.
            var sinais = F.Grupo("Sinalizacao", raiz).transform;
            var lista = new List<GameObject>
            {
                P(sinais, "Props/Semaforo_Carros", F.V(7.4f, .1f, 3.4f), 180),
                P(sinais, "Props/Semaforo_Carros", F.V(-7.4f, .1f, 16.6f), 0),
                P(sinais, "Props/Semaforo_Pedestres", F.V(-7.4f, .1f, 3.4f), 180),
                P(sinais, "Props/Semaforo_Pedestres", F.V(7.4f, .1f, 16.6f), 0),
                P(sinais, "Props/Placa_Pare", F.V(8.2f, .1f, -10), 180),
                P(sinais, "Props/Placa_FaixaDePedestre", F.V(-8.2f, .1f, -4), 180),
                P(sinais, "Props/Placa_Escola", F.V(8.2f, .1f, -24), 180),
                P(sinais, "Props/Placa_Ciclovia", F.V(-8.2f, .1f, -18), 180),
                P(sinais, "Props/Placa_Pare", F.V(-8.2f, .1f, 28), 180),
            };
            if (!final) levados.AddRange(lista.Select(g => g.transform));

            var carros = F.Grupo("Carros", raiz).transform;
            P(carros, "Carros/Carro_Sedan", F.V(3.4f, .05f, -16), 0);
            P(carros, "Carros/Carro_Taxi", F.V(-3.4f, .05f, 34), 180);
            P(carros, "Carros/Onibus", F.V(-30, .05f, 12.5f), 90);

            bagunca = null;
            if (!final)
            {
                var b = F.Grupo("Bagunca", raiz).transform;
                for (float z = -12; z <= 30; z += 7)
                    P(b, "Enchente/Chao_De_Lama", F.V(z % 2 == 0 ? -2.5f : 2.5f, .07f, z), z * 13);
                P(b, "Enchente/Papeis_Espalhados", F.V(4, .07f, 6), 30, 1.3f);
                P(b, "Enchente/Cone_Quebrado_1", F.V(-5, .07f, -2), 60);
                P(b, "Enchente/Entulho_De_Concreto", F.V(5.5f, .07f, 24), 20, .5f);
                P(b, "Enchente/Arvore_Seca_1", F.V(-13.5f, 0, 27), 40, .9f);
                P(b, "Enchente/Arvore_Seca_2", F.V(13.5f, 0, 3.5f), 200, .9f);
                P(b, "Natureza/Tronco_Caido", F.V(-3, .07f, 13), 70);
                P(b, "Lixo_Reciclagem/Saco_De_Lixo_Preto_1", F.V(6.5f, .07f, 12));
                P(b, "Lixo_Reciclagem/Pilha_De_Lixo", F.V(-6, .07f, 26), 120);
                // Restos da enchente ao sul, atrás da família.
                P(b, "Enchente/Chao_De_Lama", F.V(-2.5f, .07f, -44), 20);
                P(b, "Enchente/Chao_De_Lama", F.V(3, .07f, -50), 80);
                P(b, "Enchente/Cone_Quebrado_2", F.V(-5.5f, .07f, -41), 30);
                P(b, "Enchente/Papeis_Espalhados", F.V(4, .07f, -42), 0, 1.3f);
                Tombar(P(b, "Props/Placa_Pare", F.V(-6.5f, .3f, -46), 200));
                P(b, "Enchente/Entulho_De_Concreto", F.V(6.5f, .07f, -45), 50, .45f);
                bagunca = b.gameObject;
            }
            else
            {
                // A cidade arrumada: flores, bandeirinhas e gente.
                var festa = F.Grupo("Festa", raiz).transform;
                foreach (float x in new[] { -8.5f, 8.5f })
                    for (float z = -30; z <= 40; z += 10)
                        if (Mathf.Abs(z - 10) > 6) P(festa, "Natureza/Flores", F.V(x, .1f, z + 2), z * 11, 1.3f);
                string[] cores = { "Vermelho", "Mostarda", "Petroleo_Claro", "Verde", "Rosa", "Laranja" };
                foreach (float z in new[] { 4f, 16f })
                {
                    foreach (float x in new[] { -7.6f, 7.6f })
                        F.Cilindro(festa, "Poste_Das_Bandeirinhas", "Branco", F.V(x, 2.2f, z), F.V(.15f, 4.4f, .15f));
                    for (int i = 0; i < 18; i++)
                        F.Cubo(festa, "Bandeirinha", cores[i % cores.Length], F.V(-7f + i * .82f, 4.1f - Mathf.Sin(i / 17f * Mathf.PI) * .5f, z), F.V(.42f, .45f, .04f), F.V(0, 0, 45));
                }
                string[] gente = { "Personagens/Pedestre_Mulher", "Personagens/Idoso", "Personagens/Trabalhadora", "Personagens/Medica", "Personagens/Idosa", "Personagens/Trabalhador", "Personagens/Medico" };
                var posicoes = new[] { F.V(-5.5f, .05f, 14), F.V(5.8f, .05f, 15), F.V(-6.2f, .05f, 6), F.V(6, .05f, 5.5f), F.V(-4, .05f, 17.5f), F.V(4.2f, .05f, 18), F.V(-8.5f, .1f, 9) };
                for (int i = 0; i < gente.Length; i++)
                    P(festa, gente[i], posicoes[i], 180 + (posicoes[i].x > 0 ? -35 : 35));
            }
        }

        // A família: na abertura, de costas pra câmera, olhando a cidade (no Eixo, ao sul);
        // no final, reunida no meio do cruzamento, de frente pra câmera.
        static GameObject Tombar(GameObject go)
        {
            go.transform.Rotate(84f, 0, 0, Space.Self);
            return go;
        }

        static Animator[] Familia(Transform raiz, bool final)
        {
            var g = F.Grupo("Familia", raiz).transform;
            string[] quem = { "Familia_Pai_Animado", "Familia_Mae_Animado", "Familia_FilhoMaisVelho_Animado", "Familia_Filha_Animado", "Familia_FilhoMaisNovo_Animado" };
            var animadores = new List<Animator>();
            for (int i = 0; i < quem.Length; i++)
            {
                Vector3 pos = final
                    ? F.V(-2.4f + i * 1.2f, .05f, 10.5f - Mathf.Abs(i - 2) * .45f)
                    : F.V(-1.6f + i * .95f, .05f, -34f - (i % 2) * .3f);
                float giro = final ? 180 + (i - 2) * 12 : (i - 2) * 6;
                var p = P(g, "Personagens/" + quem[i], pos, giro);
                var a = p.GetComponentInChildren<Animator>();
                if (a != null) animadores.Add(a);
            }
            P(g, "Natureza/Cachorro_Caramelo", final ? F.V(.4f, .05f, 8.8f) : F.V(3.2f, .05f, -33.6f), final ? 170 : 10);
            return animadores.ToArray();
        }

        static ParticleSystem Chuva(Transform pai)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Chuva.mat");
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, "Assets/Materials/Chuva.mat");
            }
            mat.SetColor("_BaseColor", new Color(.85f, .9f, 1f, .55f));
            mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            EditorUtility.SetDirty(mat);

            var go = F.Grupo("Chuva", pai);
            go.transform.position = new Vector3(0, 28, 10);
            go.transform.eulerAngles = new Vector3(90, 0, 0);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 1.3f;
            main.startSpeed = 24f;
            main.startSize = .05f;
            main.maxParticles = 6000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emissao = ps.emission;
            emissao.rateOverTime = 3500;
            var forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Box;
            forma.scale = new Vector3(90, 110, 1);
            var render = go.GetComponent<ParticleSystemRenderer>();
            render.renderMode = ParticleSystemRenderMode.Stretch;
            render.lengthScale = 3f;
            render.velocityScale = .05f;
            render.sharedMaterial = mat;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        // ------------------------------------------------------------------ interface

        static Image Cheia(Transform pai, string nome, Color cor)
        {
            var img = Imagem(nome, pai, null, cor, Vector2.zero, Vector2.zero);
            img.rectTransform.anchorMin = Vector2.zero; img.rectTransform.anchorMax = Vector2.one;
            img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
            img.raycastTarget = false;
            return img;
        }

        static void Hud(Transform canvas, CenaDaHistoria historia, bool final)
        {
            historia.escuro = Cheia(canvas, "Escuro", new Color(.06f, .08f, .16f, 0));
            historia.clarao = Cheia(canvas, "Clarao", new Color(1, 1, 1, 0));

            var legenda = Imagem("Painel_Da_Legenda", canvas, arredondado, C("Branco", .94f), new Vector2(0, 100), new Vector2(1500, 140));
            TravessiaConstrutor.Ancorar(legenda, new Vector2(.5f, 0));
            historia.painelDaLegenda = legenda.gameObject;
            historia.textoDaLegenda = Texto("Texto", legenda.transform, "", 48, C("Petroleo"), new Vector2(-110, 0), new Vector2(1220, 124));
            historia.textoDaLegenda.resizeTextForBestFit = true; historia.textoDaLegenda.resizeTextMinSize = 28; historia.textoDaLegenda.resizeTextMaxSize = 48;
            var ouvir = Botao("Botao_Ouvir", legenda.transform, "Ouvir", "Petroleo_Claro", new Vector2(615, 0), new Vector2(220, 110), 38);
            UnityEventTools.AddPersistentListener(ouvir.onClick, historia.Ouvir);
            historia.botaoOuvir = ouvir.gameObject;

            var pular = Botao("Botao_Pular", canvas, "Pular", "Branco", new Vector2(-150, -75), new Vector2(240, 96), 40);
            TravessiaConstrutor.Ancorar(pular, new Vector2(1, 1));
            UnityEventTools.AddPersistentListener(pular.onClick, historia.Pular);
            historia.botaoPular = pular.gameObject;

            if (final)
            {
                var tela = Imagem("Tela_Final", canvas, null, C("Petroleo", .95f), Vector2.zero, Vector2.zero, true);
                tela.rectTransform.anchorMin = Vector2.zero; tela.rectTransform.anchorMax = Vector2.one;
                tela.rectTransform.offsetMin = tela.rectTransform.offsetMax = Vector2.zero;
                historia.telaFinal = tela.gameObject;
                Texto("Titulo", tela.transform, TituloDoFinal, 92, C("Branco"), new Vector2(0, 450), new Vector2(1700, 120));
                historia.textoDoTotal = Texto("Total", tela.transform, "", 42, C("Mostarda_Clara"), new Vector2(0, 375), new Vector2(1400, 60), FontStyle.Normal);

                var grade = Ret("Verbetes", tela.transform, new Vector2(0, -20), new Vector2(1760, 740));
                var layout = grade.gameObject.AddComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(205, 138);
                layout.spacing = new Vector2(12, 10);
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = 8;
                historia.gradeDosVerbetes = grade;

                var modelo = Imagem("Modelo_Verbete", grade, arredondado, C("Branco", .96f), Vector2.zero, new Vector2(205, 138));
                var figura = Imagem("Figura", modelo.transform, null, Color.white, new Vector2(0, 18), new Vector2(170, 88));
                figura.preserveAspect = true;
                var palavra = Texto("Palavra", modelo.transform, "", 24, C("Petroleo"), new Vector2(0, -50), new Vector2(195, 34));
                palavra.resizeTextForBestFit = true; palavra.resizeTextMinSize = 14; palavra.resizeTextMaxSize = 24;
                modelo.gameObject.SetActive(false);
                historia.modeloDoVerbete = modelo.gameObject;

                var voltar = Botao("Botao_Voltar_Ao_Hub", tela.transform, BotaoDoFinal, "Verde", new Vector2(0, -455), new Vector2(460, 110), 48);
                UnityEventTools.AddPersistentListener(voltar.onClick, historia.VoltarAoHub);
                tela.gameObject.SetActive(false);
            }

            historia.preto = Cheia(canvas, "Preto", Color.black);
        }
    }
}
