using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static CidadeViva.EditorArte.MenuConstrutor;

namespace CidadeViva.EditorArte
{
    // Gera o prefab UI_CartaoVerbete (cartão que apresenta um verbete) e a cena Album_Glotransito.
    // Tela de referência: 1920x1080. Pode ser rodado de novo: refaz os dois do zero.
    public static class AlbumConstrutor
    {
        public const string CaminhoDoCartao = "Assets/Prefabs/UI_CartaoVerbete.prefab";
        public const string CaminhoDaCena = "Assets/Scenes/Album_Glotransito.unity";
        const string PastaDeSons = "Assets/Audio/Interface";
        // Na ordem do enum GrupoDeVerbete.
        static readonly string[] NomesDasAbas = { "Veículos", "Vias e Estruturas", "Sinais", "Pessoas e Segurança", "Situação" };

        [MenuItem("Cidade Viva/Verbetes/Reconstruir cartão de verbete e cena do álbum")]
        public static void ReconstruirPeloMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Reconstruir();
        }

        public static void Reconstruir()
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Depois de criar a cena: trocar de cena descarrega assets que ninguém está usando.
            PrepararIcones();
            VerbetesConstrutor.Atualizar();

            var prefab = CriarCartao();
            var banco = AssetDatabase.LoadAssetAtPath<VerbeteDatabase>(VerbetesConstrutor.CaminhoDoAsset);
            banco.prefabDoCartao = prefab;
            EditorUtility.SetDirty(banco);
            AssetDatabase.SaveAssets();

            MontarCena();
            EditorSceneManager.SaveScene(cena, CaminhoDaCena);

            if (EditorBuildSettings.scenes.All(s => s.path != CaminhoDaCena))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(CaminhoDaCena, true)).ToArray();
        }

        // O cartão e o álbum ocupam a tela toda: em tela mais larga ou mais estreita que 16:9,
        // a escala diminui até os 1920x1080 de referência caberem inteiros, em vez de cortar as bordas.
        static void CaberInteiro(Canvas canvas)
        {
            canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        // Quadro cinza com um "sol" e um "chão": o lugar de um desenho que o glossário ainda não tem.
        static GameObject DesenhoProvisorio(Transform pai, Vector2 tamanho, int tamanhoDoTexto)
        {
            var raiz = Ret("Desenho_Provisorio", pai, Vector2.zero, tamanho);
            Imagem("Quadro", raiz, arredondado, C("Cinza_Claro"), Vector2.zero, tamanho);
            Imagem("Silhueta_Sol", raiz, circulo, C("Cinza"), new Vector2(-tamanho.x * .2f, tamanho.y * .2f), Vector2.one * tamanho.y * .22f);
            Imagem("Silhueta_Chao", raiz, arredondado, C("Cinza"), new Vector2(0, -tamanho.y * .12f), new Vector2(tamanho.x * .7f, tamanho.y * .22f));
            Texto("Texto", raiz, "desenho em breve", tamanhoDoTexto, C("Cinza_Escuro"), new Vector2(0, -tamanho.y * .36f), new Vector2(tamanho.x - 20, tamanho.y * .2f), FontStyle.Normal);
            return raiz.gameObject;
        }

        // ------------------------------------------------------------------ cartão do verbete

        static CartaoVerbete CriarCartao()
        {
            var canvas = NovoCanvas("UI_CartaoVerbete");
            CaberInteiro(canvas);
            // Por cima de qualquer tela do jogo (HUD, seleção de nível, fim de jogo).
            canvas.sortingOrder = 500;
            var raiz = canvas.gameObject;
            var script = raiz.AddComponent<CartaoVerbete>();
            raiz.AddComponent<AudioSource>().playOnAwake = false;

            // Escurece a tela e segura os toques enquanto o cartão está aberto.
            var fundo = Imagem("Fundo", raiz.transform, null, C("Grafite", .66f), Vector2.zero, Vector2.zero, true);
            var rtFundo = (RectTransform)fundo.transform;
            rtFundo.anchorMin = Vector2.zero; rtFundo.anchorMax = Vector2.one; rtFundo.offsetMin = rtFundo.offsetMax = Vector2.zero;
            script.fundo = fundo.gameObject.AddComponent<CanvasGroup>();

            script.brilho = Imagem("Brilho", raiz.transform, circulo, C("Mostarda_Clara", .8f), Vector2.zero, new Vector2(900, 900));

            var cartao = Ret("Cartao", raiz.transform, Vector2.zero, new Vector2(1560, 920));
            script.cartao = cartao;
            Imagem("Borda", cartao, arredondado, C("Petroleo"), Vector2.zero, new Vector2(1560, 920), true);
            Imagem("Papel", cartao, arredondado, C("Branco"), Vector2.zero, new Vector2(1524, 884));

            // Esquerda: a ilustração do glossário, inteira, dentro de uma moldura.
            var moldura = Imagem("Moldura", cartao, arredondado, C("Vidro"), new Vector2(-405, 70), new Vector2(640, 580));
            script.ilustracao = Imagem("Ilustracao", moldura.transform, null, Color.white, Vector2.zero, new Vector2(596, 536));
            script.ilustracao.preserveAspect = true;
            script.desenhoProvisorio = DesenhoProvisorio(moldura.transform, new Vector2(596, 536), 46);

            // Direita: a palavra em destaque e a definição.
            script.textoDaPalavra = Texto("Palavra", cartao, "palavra", 100, C("Petroleo"), new Vector2(345, 335), new Vector2(760, 150));
            script.textoDaPalavra.resizeTextForBestFit = true;
            script.textoDaPalavra.resizeTextMinSize = 52;
            script.textoDaPalavra.resizeTextMaxSize = 100;
            script.textoDaPalavra.horizontalOverflow = HorizontalWrapMode.Wrap;
            Imagem("Sublinhado", cartao, arredondado, C("Mostarda"), new Vector2(345, 248), new Vector2(360, 14));
            script.textoDaDefinicao = Texto("Definicao", cartao, "definição", 48, C("Grafite"), new Vector2(345, 20), new Vector2(760, 410), FontStyle.Normal);
            script.textoDaDefinicao.alignment = TextAnchor.MiddleLeft;
            script.textoDaDefinicao.lineSpacing = 1.05f;
            script.textoDaDefinicao.resizeTextForBestFit = true;
            script.textoDaDefinicao.resizeTextMinSize = 34;
            script.textoDaDefinicao.resizeTextMaxSize = 50;
            script.textoDaDefinicao.verticalOverflow = VerticalWrapMode.Truncate;

            script.botaoOuvir = Botao("Botao_Ouvir", cartao, "Ouvir", "Petroleo_Claro", new Vector2(-405, -335), new Vector2(460, 140), 62);
            script.botaoContinuar = Botao("Botao_Continuar", cartao, "Continuar", "Verde_Escuro", new Vector2(345, -335), new Vector2(640, 140), 62);
            UnityEventTools.AddPersistentListener(script.botaoOuvir.onClick, script.Ouvir);
            UnityEventTools.AddPersistentListener(script.botaoContinuar.onClick, script.Continuar);

            var selo = Ret("Selo_Novo", cartao, new Vector2(-690, 395), new Vector2(230, 230));
            selo.localEulerAngles = new Vector3(0, 0, 14);
            Imagem("Fundo", selo, circulo, C("Branco"), Vector2.zero, new Vector2(230, 230));
            Imagem("Cor", selo, circulo, C("Vermelho"), Vector2.zero, new Vector2(204, 204));
            Texto("Texto", selo, "Novo!", 64, C("Branco"), Vector2.zero, new Vector2(220, 90));
            script.seloNovo = selo;

            // Estrelinhas e bolinhas que saem de trás do cartão quando ele chega.
            var particulas = Ret("Particulas", raiz.transform, Vector2.zero, Vector2.zero);
            string[] cores = { "Mostarda_Clara", "Branco", "Petroleo_Claro", "Rosa", "Verde_Claro" };
            var lista = new List<RectTransform>();
            for (int i = 0; i < 18; i++)
            {
                bool estrela = i % 2 == 0;
                var p = Imagem("Particula_" + (i + 1), particulas, estrela ? S("figura_estrela") : circulo,
                    estrela ? Color.white : C(cores[i % cores.Length]), Vector2.zero, Vector2.one * (estrela ? 64 : 30));
                lista.Add((RectTransform)p.transform);
            }
            script.particulas = lista.ToArray();

            script.somAoAbrir = AssetDatabase.LoadAssetAtPath<AudioClip>($"{PastaDeSons}/maximize_001.ogg");
            script.somDeNovo = AssetDatabase.LoadAssetAtPath<AudioClip>($"{PastaDeSons}/confirmation_002.ogg");

            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoDoCartao);
            Object.DestroyImmediate(raiz);
            return prefab.GetComponent<CartaoVerbete>();
        }

        // ------------------------------------------------------------------ cena do álbum

        static void MontarCena()
        {
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0, 3f, -10f);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AmbienteConstrutor.CaminhoPrefab) == null) AmbienteConstrutor.CriarPrefab();
            AmbienteConstrutor.AplicarNaCenaAberta(true);

            var canvas = NovoCanvas("Canvas");
            CaberInteiro(canvas);
            NovoEventSystem(false);
            var album = canvas.gameObject.AddComponent<AlbumGlotransito>();
            var voltarAoHub = canvas.gameObject.AddComponent<VoltarAoHub>();

            var fundo = Imagem("Fundo", canvas.transform, null, C("Petroleo", .9f), Vector2.zero, Vector2.zero);
            var rtFundo = (RectTransform)fundo.transform;
            rtFundo.anchorMin = Vector2.zero; rtFundo.anchorMax = Vector2.one; rtFundo.offsetMin = rtFundo.offsetMax = Vector2.zero;

            // Topo: voltar, título e progresso.
            var voltar = Botao("Botao_Voltar", canvas.transform, "Voltar", "Mostarda", new Vector2(-770, 452), new Vector2(320, 116), 52);
            UnityEventTools.AddPersistentListener(voltar.onClick, voltarAoHub.Voltar);
            Texto("Titulo", canvas.transform, "Álbum Glotrânsito", 82, C("Branco"), new Vector2(-110, 458), new Vector2(900, 120));
            album.textoDoProgresso = Texto("Progresso", canvas.transform, "0 de 38 verbetes", 46, C("Mostarda_Clara"), new Vector2(640, 488), new Vector2(560, 60));
            var trilho = Imagem("Barra_De_Progresso", canvas.transform, arredondado, C("Petroleo_Escuro"), new Vector2(640, 428), new Vector2(540, 48));
            var area = Ret("Area", trilho.transform, Vector2.zero, Vector2.zero);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(6, 6); area.offsetMax = new Vector2(-6, -6);
            var cheio = Imagem("Cheio", area, arredondado, C("Verde"), Vector2.zero, Vector2.zero);
            var rtCheio = (RectTransform)cheio.transform;
            rtCheio.anchorMin = Vector2.zero; rtCheio.anchorMax = new Vector2(.5f, 1f); rtCheio.offsetMin = rtCheio.offsetMax = Vector2.zero;
            album.barraDeProgresso = rtCheio;

            // Página do álbum.
            var pagina = Imagem("Pagina", canvas.transform, arredondado, C("Areia"), new Vector2(0, -95), new Vector2(1860, 870), true);

            var abas = new List<Button>();
            var frentes = new List<Image>();
            for (int i = 0; i < NomesDasAbas.Length; i++)
            {
                var aba = Botao("Aba_" + ((GrupoDeVerbete)i), pagina.transform, NomesDasAbas[i], "Petroleo_Claro", new Vector2(-720 + i * 360, 360), new Vector2(344, 104), 40);
                var texto = aba.transform.Find("Frente/Texto").GetComponent<Text>();
                texto.resizeTextForBestFit = true; texto.resizeTextMinSize = 24; texto.resizeTextMaxSize = 40;
                UnityEventTools.AddIntPersistentListener(aba.onClick, album.AbrirAba, i);
                abas.Add(aba);
                frentes.Add(aba.transform.Find("Frente").GetComponent<Image>());
            }
            album.abas = abas.ToArray();
            album.frentesDasAbas = frentes.ToArray();
            album.corDaAbaAberta = C("Mostarda");
            album.corDaAbaFechada = C("Petroleo_Claro");

            // Grade de figurinhas: 7 por linha (o maior grupo tem 14).
            var conteudo = Ret("Figurinhas", pagina.transform, new Vector2(0, -40), new Vector2(1780, 640));
            var grade = conteudo.gameObject.AddComponent<GridLayoutGroup>();
            grade.cellSize = new Vector2(236, 300);
            grade.spacing = new Vector2(18, 26);
            grade.childAlignment = TextAnchor.UpperCenter;
            grade.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grade.constraintCount = 7;
            album.conteudo = conteudo;
            album.modeloDeFigurinha = ModeloDeFigurinha(conteudo);

            var final = Botao("Botao_Ver_Final", canvas.transform, "Ver o final", "Verde", new Vector2(760, -470), new Vector2(320, 100), 44);
            UnityEventTools.AddPersistentListener(final.onClick, album.VerFinal);
            album.botaoDoFinal = final.gameObject;
            final.gameObject.SetActive(false);
            album.textoDaDica = Texto("Dica", pagina.transform, album.dicaPadrao, 40, C("Cinza_Escuro"), new Vector2(0, -392), new Vector2(1700, 60), FontStyle.Normal);
        }

        static FigurinhaDoAlbum ModeloDeFigurinha(Transform pai)
        {
            var botao = Botao("Modelo_Figurinha", pai, "", "Branco", Vector2.zero, new Vector2(236, 290), 0);
            Object.DestroyImmediate(botao.transform.Find("Frente/Texto").gameObject);
            var frente = botao.transform.Find("Frente");
            var f = botao.gameObject.AddComponent<FigurinhaDoAlbum>();
            f.botao = botao;
            f.fundo = frente.GetComponent<Image>();
            f.corDesbloqueada = C("Branco");
            f.corBloqueada = C("Cinza_Claro");

            f.ilustracao = Imagem("Ilustracao", frente, null, Color.white, new Vector2(0, 34), new Vector2(208, 196));
            f.ilustracao.preserveAspect = true;
            f.desenhoProvisorio = DesenhoProvisorio(frente, new Vector2(208, 196), 22);
            ((RectTransform)f.desenhoProvisorio.transform).anchoredPosition = new Vector2(0, 34);

            var bloqueado = Ret("Bloqueado", frente, new Vector2(0, 34), new Vector2(208, 196));
            Imagem("Silhueta", bloqueado, circulo, C("Cinza"), Vector2.zero, new Vector2(176, 176));
            Texto("Interrogacao", bloqueado, "?", 130, C("Branco"), new Vector2(0, 4), new Vector2(170, 170));
            f.bloqueado = bloqueado.gameObject;

            f.nome = Texto("Nome", frente, "palavra", 32, C("Grafite"), new Vector2(0, -102), new Vector2(216, 64));
            f.nome.resizeTextForBestFit = true; f.nome.resizeTextMinSize = 18; f.nome.resizeTextMaxSize = 32;
            botao.gameObject.SetActive(false);
            return f;
        }
    }
}
