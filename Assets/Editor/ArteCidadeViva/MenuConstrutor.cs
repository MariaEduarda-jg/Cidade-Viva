using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CidadeViva.EditorArte
{
    // Monta a cena Menu_Inicial (perfis) e o painel "quem está jogando / Trocar de perfil" do Hub.
    // Tela de referência: 1920x1080. Pode ser rodado de novo: recria a cena do zero.
    public static class MenuConstrutor
    {
        const string CaminhoDaCena = "Assets/Scenes/Menu_Inicial.unity";
        const string PastaIcones = "Assets/Art/UI/Perfis";
        const string CaminhoIcones = "Assets/Configs/Icones_De_Perfil.asset";
        internal const string CaminhoDaFonte = "Assets/Art/UI/Fontes/Baloo2.ttf";
        internal const string CaminhoDaFonteNegrito = "Assets/Art/UI/Fontes/LilitaOne-Regular.ttf";
        static readonly string[] Avatares = { "gato", "cachorro", "coelho", "urso", "sapo", "pintinho", "porquinho", "macaco" };
        static readonly string[] Figuras = { "estrela", "coracao", "sol", "lua", "flor", "gota" };

        internal static Sprite arredondado, circulo;
        internal static Font fonte;
        // Títulos e botões usam uma fonte que já é grossa: o negrito "sintético" do Unity aperta os espaços.
        internal static Font fonteNegrito;

        [MenuItem("Cidade Viva/Menu/Reconstruir a cena Menu_Inicial")]
        public static void ReconstruirPeloMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Reconstruir();
        }

        // ------------------------------------------------------------------ ícones

        public static IconesDePerfil PrepararIcones()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PastaIcones }))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
                bool fatiado = caminho.EndsWith("ui_arredondado.png");
                var borda = fatiado ? new Vector4(44, 44, 44, 44) : Vector4.zero;
                if (imp.textureType == TextureImporterType.Sprite && imp.spriteBorder == borda && imp.spriteImportMode == SpriteImportMode.Single) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spriteBorder = borda;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            arredondado = S("ui_arredondado");
            circulo = S("ui_circulo");
            // Fonte arredondada do jogo (Baloo 2, licença OFL). Sem ela, cai na fonte embutida do Unity.
            fonte = AssetDatabase.LoadAssetAtPath<Font>(CaminhoDaFonte);
            if (fonte == null) fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fonteNegrito = AssetDatabase.LoadAssetAtPath<Font>(CaminhoDaFonteNegrito);

            ArtePaleta.GarantirPasta("Assets/Configs");
            var icones = AssetDatabase.LoadAssetAtPath<IconesDePerfil>(CaminhoIcones);
            if (icones == null)
            {
                icones = ScriptableObject.CreateInstance<IconesDePerfil>();
                AssetDatabase.CreateAsset(icones, CaminhoIcones);
            }
            icones.avatares = Avatares.Select(a => S("avatar_" + a)).ToArray();
            icones.figuras = Figuras.Select(f => S("figura_" + f)).ToArray();
            icones.avatarDoVisitante = S("figura_sol");
            EditorUtility.SetDirty(icones);
            AssetDatabase.SaveAssets();
            return icones;
        }

        internal static Sprite S(string nome)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{PastaIcones}/{nome}.png");
            if (s == null) Debug.LogError("[Menu] Ícone não encontrado: " + nome);
            return s;
        }

        // ------------------------------------------------------------------ peças de UI

        internal static Color C(string nome, float alfa = 1f)
        {
            var c = ArtePaleta.Cor(nome);
            c.a = alfa;
            return c;
        }

        internal static RectTransform Ret(string nome, Transform pai, Vector2 pos, Vector2 tam)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = tam;
            return rt;
        }

        internal static RectTransform Tela(string nome, Transform pai)
        {
            var rt = Ret(nome, pai, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        internal static Image Imagem(string nome, Transform pai, Sprite sprite, Color cor, Vector2 pos, Vector2 tam, bool recebeToque = false)
        {
            var rt = Ret(nome, pai, pos, tam);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = cor;
            img.type = sprite == arredondado ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = sprite != arredondado && sprite != null;
            img.raycastTarget = recebeToque;
            return img;
        }

        internal static Text Texto(string nome, Transform pai, string texto, int tamanho, Color cor, Vector2 pos, Vector2 tam, FontStyle estilo = FontStyle.Bold)
        {
            var rt = Ret(nome, pai, pos, tam);
            var t = rt.gameObject.AddComponent<Text>();
            bool negrito = estilo == FontStyle.Bold && fonteNegrito != null;
            t.font = negrito ? fonteNegrito : fonte;
            t.text = texto;
            t.fontSize = tamanho;
            t.fontStyle = negrito ? FontStyle.Normal : estilo;
            t.color = cor;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        // Botão com "degrau" embaixo (sombra da mesma cor, mais escura), fácil de enxergar e de acertar.
        internal static Button Botao(string nome, Transform pai, string texto, string cor, Vector2 pos, Vector2 tam, int tamanhoDoTexto, Sprite icone = null, Sprite forma = null)
        {
            forma = forma ?? arredondado;
            Color c = C(cor);
            var sombra = Imagem(nome, pai, forma, new Color(c.r * .68f, c.g * .68f, c.b * .68f, 1f), pos, tam, true);
            var frente = Imagem("Frente", sombra.transform, forma, c, new Vector2(0, 10), tam, true);
            var botao = sombra.gameObject.AddComponent<Button>();
            botao.targetGraphic = frente;
            var cores = botao.colors;
            cores.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            cores.pressedColor = new Color(.8f, .8f, .8f, 1f);
            cores.disabledColor = new Color(.75f, .75f, .75f, .45f);
            botao.colors = cores;
            botao.navigation = new Navigation { mode = Navigation.Mode.None };

            bool claro = cor == "Branco" || cor == "Mostarda_Clara" || cor == "Vidro" || cor == "Areia" || cor == "Cinza_Claro";
            Color corDoTexto = claro ? C("Grafite") : C("Branco");
            if (icone != null && !string.IsNullOrEmpty(texto))
            {
                Imagem("Icone", frente.transform, icone, Color.white, new Vector2(0, tam.y * .16f), Vector2.one * tam.y * .5f);
                Texto("Texto", frente.transform, texto, tamanhoDoTexto, corDoTexto, new Vector2(0, -tam.y * .3f), new Vector2(tam.x - 30, tam.y * .34f));
            }
            else if (icone != null)
                Imagem("Icone", frente.transform, icone, Color.white, Vector2.zero, tam * .78f);
            else
                Texto("Texto", frente.transform, texto, tamanhoDoTexto, corDoTexto, Vector2.zero, tam - new Vector2(30, 10));
            return botao;
        }

        internal static Canvas NovoCanvas(string nome)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var escala = go.GetComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920, 1080);
            escala.matchWidthOrHeight = .5f;
            return canvas;
        }

        internal static EventSystem NovoEventSystem(bool navegacaoPorTeclado)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var es = go.GetComponent<EventSystem>();
            es.sendNavigationEvents = navegacaoPorTeclado;
            return es;
        }

        // Cartão claro no meio da tela, com título e botão de voltar.
        static RectTransform Cartao(RectTransform tela, MenuInicial menu, string titulo, out Text textoDoTitulo)
        {
            var cartao = Imagem("Cartao", tela, arredondado, C("Branco", .93f), new Vector2(0, -10), new Vector2(1560, 960), true);
            textoDoTitulo = Texto("Titulo", cartao.transform, titulo, 62, C("Petroleo"), new Vector2(0, 400), new Vector2(1200, 100));
            var voltar = Botao("Botao_Voltar", cartao.transform, "", "Petroleo_Claro", new Vector2(-690, 395), new Vector2(120, 120), 0, S("ui_seta_voltar"), circulo);
            UnityEventTools.AddPersistentListener(voltar.onClick, menu.Voltar);
            return (RectTransform)cartao.transform;
        }

        // ------------------------------------------------------------------ cena do menu

        public static void Reconstruir()
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Depois de criar a cena: trocar de cena descarrega assets que ninguém está usando.
            var icones = PrepararIcones();

            Cenario();
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 50f;
            cam.transform.position = new Vector3(0, 3.4f, -12f);
            cam.transform.eulerAngles = new Vector3(4f, 0, 0);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AmbienteConstrutor.CaminhoPrefab) == null) AmbienteConstrutor.CriarPrefab();
            AmbienteConstrutor.AplicarNaCenaAberta(true);

            var canvas = NovoCanvas("Canvas");
            NovoEventSystem(false);
            var menu = canvas.gameObject.AddComponent<MenuInicial>();
            menu.icones = icones;

            TelaInicio(canvas.transform, menu);
            TelaLista(canvas.transform, menu);
            TelaNome(canvas.transform, menu);
            TelaAvatar(canvas.transform, menu, icones);
            TelaCodigo(canvas.transform, menu, icones);
            TelaVisitante(canvas.transform, menu);
            TelaCreditos(canvas.transform, menu);
            foreach (var tela in new[] { menu.telaLista, menu.telaNome, menu.telaAvatar, menu.telaCodigo, menu.telaVisitante, menu.telaCreditos })
                tela.SetActive(false);

            EditorSceneManager.SaveScene(cena, CaminhoDaCena);

            // Primeira cena do jogo.
            var cenas = EditorBuildSettings.scenes.Where(s => s.path != CaminhoDaCena).ToList();
            cenas.Insert(0, new EditorBuildSettingsScene(CaminhoDaCena, true));
            EditorBuildSettings.scenes = cenas.ToArray();
        }

        // Pedaço de cidade atrás do menu, com a mesma cara do Hub.
        static void Cenario()
        {
            var c = F.Grupo("Cenario").transform;
            GameObject P(string prefab, Vector3 pos, float rotY = 0, float escala = 1f)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArteImportador.PastaPrefabs}/{prefab}.prefab");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, c);
                go.transform.localPosition = pos;
                go.transform.localEulerAngles = new Vector3(0, rotY, 0);
                go.transform.localScale = Vector3.one * escala;
                return go;
            }
            F.Plano(c, "Gramado", "Verde_Claro", F.V(0, 0, 60), 700, 700);
            F.Caixa(c, "Rua", "Asfalto", 0, 0, 9f, 200f, .02f, 7f);
            F.Caixa(c, "Calcada", "Cinza_Claro", 0, 0, 2.2f, 200f, .08f, 6.4f);
            F.Caixa(c, "Calcada_Fundo", "Cinza_Claro", 0, 0, 14.2f, 200f, .08f, 3f);
            for (float x = -60f; x <= 60f; x += 3f)
                F.Cubo(c, "Traco", "Branco", F.V(x, .026f, 9f), F.V(1.4f, .012f, .16f));
            P("Predios/Brasilia_Congresso", F.V(0, 0, 74f), 180);
            P("Predios/Brasilia_Catedral", F.V(-34f, 0, 44f));
            P("Predios/Brasilia_Museu_Cupula", F.V(38f, 0, 50f), 180);
            P("Predios/Escola", F.V(-22f, 0, 22f), 180);
            P("Predios/Padaria", F.V(22f, 0, 21f), 180);
            P("Predios/Casa_03", F.V(34f, 0, 20f), 180);
            foreach (float x in new[] { -11f, 11f })
                foreach (float z in new[] { 30f, 40f })
                    P("Natureza/Palmeira_1", F.V(x, 0, z), x > 0 ? 40 : 200);
            P("Natureza/Arvore_Comum_1", F.V(-9.5f, 0, 16f), 0, .9f);
            P("Natureza/Arvore_Comum_2", F.V(11f, 0, 17f), 60, .9f);
            P("Natureza/Arvore_Betula", F.V(-34f, 0, 17f), 20);
            P("Carros/Onibus", F.V(13f, .02f, 9.8f), -90);
            P("Carros/Carro_Taxi", F.V(-12f, .02f, 7.6f), 90);
            P("Props/Semaforo_Carros", F.V(7.6f, .08f, 5f), 180);
            P("Props/Placa_FaixaDePedestre", F.V(-7.4f, .08f, 5f), 180);
            // A família, de frente pra câmera.
            P("Personagens/Familia_Pai_Animado", F.V(-5.6f, .08f, 2.4f), 170);
            P("Personagens/Familia_Mae_Animado", F.V(-4.4f, .08f, 2.2f), 185);
            P("Personagens/Familia_FilhoMaisVelho_Animado", F.V(4.4f, .08f, 2.3f), 175);
            P("Personagens/Familia_Filha_Animado", F.V(5.5f, .08f, 2.1f), 190);
            P("Personagens/Familia_FilhoMaisNovo_Animado", F.V(6.4f, .08f, 2.4f), 200);
            P("Natureza/Cachorro_Caramelo", F.V(-3.3f, .08f, 2.6f), 200);
        }

        static void TelaInicio(Transform canvas, MenuInicial menu)
        {
            var tela = Tela("Tela_Inicio", canvas);
            menu.telaInicio = tela.gameObject;
            var sombra = Texto("Logo_Sombra", tela, "Cidade Viva", 170, C("Petroleo_Escuro", .55f), new Vector2(6, 322), new Vector2(1500, 220));
            sombra.transform.SetAsFirstSibling();
            Texto("Logo", tela, "Cidade Viva", 170, C("Branco"), new Vector2(0, 330), new Vector2(1500, 220));
            var faixa = Imagem("Faixa", tela, arredondado, C("Petroleo", .88f), new Vector2(0, 195), new Vector2(900, 76));
            Texto("Texto", faixa.transform, "Vamos reconstruir a cidade juntos!", 40, C("Branco"), Vector2.zero, new Vector2(880, 70), FontStyle.Normal);

            var entrar = Botao("Botao_Entrar", tela, "Entrar", "Verde_Escuro", new Vector2(-600, -310), new Vector2(540, 300), 58, S("avatar_gato"));
            var criar = Botao("Botao_Criar", tela, "Criar meu perfil", "Mostarda", new Vector2(0, -310), new Vector2(540, 300), 58, S("avatar_coelho"));
            var visitante = Botao("Botao_Visitante", tela, "Jogar sem conta", "Petroleo_Claro", new Vector2(600, -310), new Vector2(540, 300), 58, S("figura_sol"));
            UnityEventTools.AddPersistentListener(entrar.onClick, menu.AbrirEntrar);
            UnityEventTools.AddPersistentListener(criar.onClick, menu.AbrirCriar);
            UnityEventTools.AddPersistentListener(visitante.onClick, menu.AbrirVisitante);
            var creditos = Botao("Botao_Creditos", tela, "Créditos", "Branco", new Vector2(-150, -75), new Vector2(240, 96), 38);
            TravessiaConstrutor.Ancorar(creditos, new Vector2(1, 1));
            UnityEventTools.AddPersistentListener(creditos.onClick, menu.AbrirCreditos);
            var abertura = Botao("Botao_Ver_Abertura", tela, "Ver abertura", "Branco", new Vector2(-440, -75), new Vector2(300, 96), 38);
            TravessiaConstrutor.Ancorar(abertura, new Vector2(1, 1));
            UnityEventTools.AddPersistentListener(abertura.onClick, menu.VerAbertura);
        }

        // Créditos: texto gerado do Assets/Art/CREDITOS.md (CreditosConstrutor), numa lista que rola.
        static void TelaCreditos(Transform canvas, MenuInicial menu)
        {
            var tela = Tela("Tela_Creditos", canvas);
            menu.telaCreditos = tela.gameObject;
            var cartao = Cartao(tela, menu, "Créditos", out _);
            var rolagem = Ret("Rolagem", cartao, new Vector2(0, -60), new Vector2(1440, 780));
            rolagem.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0);
            var scroll = rolagem.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 50f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var janela = Ret("Janela", rolagem, Vector2.zero, Vector2.zero);
            janela.anchorMin = Vector2.zero; janela.anchorMax = Vector2.one; janela.offsetMin = janela.offsetMax = Vector2.zero;
            janela.gameObject.AddComponent<RectMask2D>();
            var texto = Texto("Texto", janela, CreditosConstrutor.GerarTexto(), 32, C("Grafite"), Vector2.zero, new Vector2(1380, 100), FontStyle.Normal);
            texto.alignment = TextAnchor.UpperCenter;
            texto.supportRichText = true;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.lineSpacing = 1.05f;
            var rt = texto.rectTransform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(.5f, 1);
            rt.offsetMin = new Vector2(30, 0); rt.offsetMax = new Vector2(-30, 0);
            rt.anchoredPosition = new Vector2(0, -10);
            texto.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = janela;
            scroll.content = rt;
        }

        static void TelaLista(Transform canvas, MenuInicial menu)
        {
            var tela = Tela("Tela_Lista", canvas);
            menu.telaLista = tela.gameObject;
            var cartao = Cartao(tela, menu, "Quem é você?", out _);

            var rolagem = Ret("Lista", cartao, new Vector2(0, -60), new Vector2(1400, 760));
            var fundoDaRolagem = rolagem.gameObject.AddComponent<Image>();
            fundoDaRolagem.color = new Color(1, 1, 1, 0);
            var scroll = rolagem.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40f;
            var janela = Ret("Janela", rolagem, Vector2.zero, Vector2.zero);
            janela.anchorMin = Vector2.zero; janela.anchorMax = Vector2.one; janela.offsetMin = janela.offsetMax = Vector2.zero;
            janela.gameObject.AddComponent<RectMask2D>();
            var conteudo = Ret("Conteudo", janela, Vector2.zero, new Vector2(0, 0));
            conteudo.anchorMin = new Vector2(0, 1); conteudo.anchorMax = new Vector2(1, 1); conteudo.pivot = new Vector2(.5f, 1);
            var grade = conteudo.gameObject.AddComponent<GridLayoutGroup>();
            grade.cellSize = new Vector2(310, 340);
            grade.spacing = new Vector2(30, 30);
            grade.padding = new RectOffset(25, 25, 20, 30);
            grade.childAlignment = TextAnchor.UpperCenter;
            grade.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grade.constraintCount = 4;
            var ajuste = conteudo.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = janela;
            scroll.content = conteudo;

            var modelo = Botao("Modelo_Perfil", conteudo, "", "Vidro", Vector2.zero, new Vector2(310, 330), 0);
            Object.DestroyImmediate(modelo.transform.Find("Frente/Texto").gameObject);
            // O código do menu procura "Avatar" e "Nome" como filhos diretos do item.
            var frente = modelo.transform.Find("Frente");
            var avatar = Imagem("Avatar", modelo.transform, S("avatar_gato"), Color.white, new Vector2(0, 50), new Vector2(210, 210));
            var nome = Texto("Nome", modelo.transform, "Nome", 42, C("Grafite"), new Vector2(0, -105), new Vector2(290, 70));
            nome.resizeTextForBestFit = true; nome.resizeTextMinSize = 24; nome.resizeTextMaxSize = 42;
            frente.SetAsFirstSibling();
            modelo.gameObject.SetActive(false);

            var vazio = Texto("Aviso_Lista_Vazia", cartao, "Ainda não tem ninguém aqui.\nVolte e toque em \"Criar meu perfil\"!", 52, C("Cinza_Escuro"), new Vector2(0, -40), new Vector2(1200, 300), FontStyle.Normal);

            menu.conteudoDaLista = conteudo;
            menu.modeloDeItem = modelo.gameObject;
            menu.avisoListaVazia = vazio.gameObject;
        }

        static void TelaNome(Transform canvas, MenuInicial menu)
        {
            var tela = Tela("Tela_Nome", canvas);
            menu.telaNome = tela.gameObject;
            var cartao = Cartao(tela, menu, "Como você se chama?", out _);

            var caixa = Imagem("Caixa_Do_Nome", cartao, arredondado, C("Vidro"), new Vector2(0, 255), new Vector2(980, 140));
            menu.textoDoNome = Texto("Nome", caixa.transform, "", 84, C("Petroleo_Escuro"), Vector2.zero, new Vector2(940, 130));

            string[] linhas = { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
            for (int l = 0; l < linhas.Length; l++)
            {
                float largura = linhas[l].Length * 128f - 12f;
                for (int i = 0; i < linhas[l].Length; i++)
                {
                    string letra = linhas[l][i].ToString();
                    var tecla = Botao("Tecla_" + letra, cartao, letra, "Petroleo_Claro",
                        new Vector2(-largura / 2f + 58f + i * 128f - (l == 2 ? 140f : 0f), 90f - l * 134f), new Vector2(116, 116), 60);
                    UnityEventTools.AddStringPersistentListener(tecla.onClick, menu.Letra, letra);
                }
            }
            var apagar = Botao("Tecla_Apagar", cartao, "Apagar", "Vermelho", new Vector2(470, -178), new Vector2(300, 116), 46);
            UnityEventTools.AddPersistentListener(apagar.onClick, menu.ApagarLetra);
            var espaco = Botao("Tecla_Espaco", cartao, "Espaço", "Cinza", new Vector2(-160, -322), new Vector2(620, 110), 44);
            UnityEventTools.AddStringPersistentListener(espaco.onClick, menu.Letra, " ");
            var pronto = Botao("Botao_Pronto", cartao, "Pronto!", "Verde_Escuro", new Vector2(500, -330), new Vector2(420, 130), 58);
            UnityEventTools.AddPersistentListener(pronto.onClick, menu.NomePronto);
            menu.botaoNomePronto = pronto;
        }

        static void TelaAvatar(Transform canvas, MenuInicial menu, IconesDePerfil icones)
        {
            var tela = Tela("Tela_Avatar", canvas);
            menu.telaAvatar = tela.gameObject;
            var cartao = Cartao(tela, menu, "Escolha seu bichinho", out _);
            string[] fundos = { "Petroleo_Claro", "Mostarda_Clara", "Verde", "Rosa", "Vidro", "Petroleo", "Verde_Claro", "Mostarda" };
            var botoes = new List<RectTransform>();
            for (int i = 0; i < icones.avatares.Length; i++)
            {
                var b = Botao("Avatar_" + Avatares[i], cartao, "", fundos[i % fundos.Length],
                    new Vector2(-510f + (i % 4) * 340f, 170f - (i / 4) * 300f), new Vector2(250, 250), 0, icones.avatares[i], circulo);
                UnityEventTools.AddIntPersistentListener(b.onClick, menu.EscolherAvatar, i);
                botoes.Add((RectTransform)b.transform);
            }
            var pronto = Botao("Botao_Pronto", cartao, "Pronto!", "Verde_Escuro", new Vector2(500, -360), new Vector2(420, 130), 58);
            UnityEventTools.AddPersistentListener(pronto.onClick, menu.AvatarPronto);
            menu.botoesDeAvatar = botoes.ToArray();
            menu.botaoAvatarPronto = pronto;
        }

        static void TelaCodigo(Transform canvas, MenuInicial menu, IconesDePerfil icones)
        {
            var tela = Tela("Tela_Codigo", canvas);
            menu.telaCodigo = tela.gameObject;
            var cartao = Cartao(tela, menu, menu.tituloAoCriar, out var titulo);
            titulo.fontSize = 54;
            menu.tituloDoCodigo = titulo;

            Imagem("Avatar_Fundo", cartao, circulo, C("Vidro"), new Vector2(-560, 215), new Vector2(190, 190));
            menu.avatarNoCodigo = Imagem("Avatar", cartao, S("avatar_gato"), Color.white, new Vector2(-560, 215), new Vector2(160, 160));
            menu.nomeNoCodigo = Texto("Nome", cartao, "Nome", 44, C("Grafite"), new Vector2(-560, 85), new Vector2(380, 60));

            var espacos = new List<Image>();
            for (int i = 0; i < Perfis.TamanhoDoCodigo; i++)
            {
                var caixa = Imagem("Espaco_" + (i + 1), cartao, arredondado, C("Cinza_Claro"), new Vector2(-150f + i * 230f, 215f), new Vector2(200, 200));
                var figura = Imagem("Figura", caixa.transform, null, Color.white, Vector2.zero, new Vector2(160, 160));
                figura.enabled = false;
                espacos.Add(figura);
            }
            menu.espacosDoCodigo = espacos.ToArray();
            menu.mensagemDoCodigo = Texto("Mensagem", cartao, "", 44, C("Terracota"), new Vector2(80, 60), new Vector2(900, 70));

            for (int i = 0; i < icones.figuras.Length; i++)
            {
                var b = Botao("Figura_" + Figuras[i], cartao, "", "Branco", new Vector2(-575f + i * 230f, -130f), new Vector2(200, 200), 0, icones.figuras[i]);
                UnityEventTools.AddIntPersistentListener(b.onClick, menu.EscolherFigura, i);
            }
            var apagar = Botao("Botao_Apagar", cartao, "Apagar", "Vermelho", new Vector2(-470, -360), new Vector2(360, 120), 50);
            UnityEventTools.AddPersistentListener(apagar.onClick, menu.ApagarFigura);
            var pronto = Botao("Botao_Pronto", cartao, "Pronto!", "Verde_Escuro", new Vector2(500, -360), new Vector2(420, 130), 58);
            UnityEventTools.AddPersistentListener(pronto.onClick, menu.CodigoPronto);
            menu.botaoCodigoPronto = pronto;
        }

        static void TelaVisitante(Transform canvas, MenuInicial menu)
        {
            var tela = Tela("Tela_Visitante", canvas);
            menu.telaVisitante = tela.gameObject;
            var cartao = Cartao(tela, menu, "Jogar sem conta", out _);
            Imagem("Aviso", cartao, S("ui_aviso"), Color.white, new Vector2(0, 170), new Vector2(280, 280));
            Texto("Texto", cartao, "Seu progresso não será guardado", 70, C("Petroleo_Escuro"), new Vector2(0, -40), new Vector2(1400, 100));
            Texto("Explicacao", cartao, "Quando você fechar o jogo, tudo volta para o começo.", 44, C("Cinza_Escuro"), new Vector2(0, -130), new Vector2(1400, 70), FontStyle.Normal);
            var voltar = Botao("Botao_Criar_Perfil", cartao, "Quero criar um perfil", "Mostarda", new Vector2(-360, -330), new Vector2(620, 140), 50);
            UnityEventTools.AddPersistentListener(voltar.onClick, menu.AbrirCriar);
            var jogar = Botao("Botao_Jogar", cartao, "Vamos jogar!", "Verde_Escuro", new Vector2(360, -330), new Vector2(620, 140), 56);
            UnityEventTools.AddPersistentListener(jogar.onClick, menu.JogarComoVisitante);
        }

        // ------------------------------------------------------------------ painel do Hub

        // Chamado pelo HubConstrutor com a cena Hub aberta.
        public static void PainelDoHub()
        {
            var icones = PrepararIcones();
            foreach (var nome in new[] { "UI_Hub", "EventSystem" })
            {
                var velho = GameObject.Find(nome);
                if (velho != null) Object.DestroyImmediate(velho);
            }
            var canvas = NovoCanvas("UI_Hub");
            // Sem navegação por teclado: as setas e o espaço são do jogador, não podem acionar o botão.
            NovoEventSystem(false);

            var cartao = Imagem("Painel_Do_Perfil", canvas.transform, arredondado, C("Branco", .92f), new Vector2(300, -105), new Vector2(560, 170), true);
            var rt = (RectTransform)cartao.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            Imagem("Avatar_Fundo", cartao.transform, circulo, C("Vidro"), new Vector2(-195, 0), new Vector2(140, 140));
            var avatar = Imagem("Avatar", cartao.transform, icones.avatares[0], Color.white, new Vector2(-195, 0), new Vector2(118, 118));
            var nomeDoPerfil = Texto("Nome", cartao.transform, "Nome", 44, C("Petroleo_Escuro"), new Vector2(75, 42), new Vector2(380, 60));
            nomeDoPerfil.resizeTextForBestFit = true; nomeDoPerfil.resizeTextMinSize = 26; nomeDoPerfil.resizeTextMaxSize = 44;
            var trocar = Botao("Botao_Trocar_De_Perfil", cartao.transform, "Trocar de perfil", "Petroleo", new Vector2(75, -38), new Vector2(360, 72), 32);

            var aviso = Imagem("Aviso_Visitante", canvas.transform, arredondado, C("Mostarda_Clara", .95f), new Vector2(300, -240), new Vector2(560, 76));
            var rtAviso = (RectTransform)aviso.transform;
            rtAviso.anchorMin = rtAviso.anchorMax = new Vector2(0, 1);
            Imagem("Icone", aviso.transform, S("ui_aviso"), Color.white, new Vector2(-240, 0), new Vector2(56, 56));
            Texto("Texto", aviso.transform, "Seu progresso não será guardado", 28, C("Grafite"), new Vector2(28, 0), new Vector2(480, 60));

            var painel = cartao.gameObject.AddComponent<PainelDoPerfil>();
            painel.icones = icones;
            painel.avatar = avatar;
            painel.nome = nomeDoPerfil;
            painel.avisoDoVisitante = aviso.gameObject;
            UnityEventTools.AddPersistentListener(trocar.onClick, painel.TrocarDePerfil);

            // Botão "Álbum", fixo no canto de cima à direita: abre o Álbum Glotrânsito.
            var album = Botao("Botao_Album", canvas.transform, "Álbum", "Mostarda", new Vector2(-210, -110), new Vector2(340, 150), 64);
            var rtAlbum = (RectTransform)album.transform;
            rtAlbum.anchorMin = rtAlbum.anchorMax = new Vector2(1, 1);
            ((RectTransform)album.transform.Find("Frente/Texto")).anchoredPosition = new Vector2(0, 16);
            var contador = Imagem("Contador", album.transform.Find("Frente"), arredondado, C("Petroleo"), new Vector2(0, -44), new Vector2(150, 50));
            var botaoDoAlbum = album.gameObject.AddComponent<BotaoDoAlbum>();
            botaoDoAlbum.contador = Texto("Texto", contador.transform, "0/38", 34, C("Branco"), Vector2.zero, new Vector2(140, 46));
            UnityEventTools.AddPersistentListener(album.onClick, botaoDoAlbum.Abrir);

            // Moedas e loja (versão final), embaixo do Álbum.
            LojaConstrutor.Painel(canvas);

            // Linha de ajuda no pé da tela: como olhar em volta com o mouse.
            var ajuda = Imagem("Dica_Do_Olhar", canvas.transform, arredondado, C("Petroleo_Escuro", .78f), new Vector2(0, 52), new Vector2(900, 60));
            var rtAjuda = (RectTransform)ajuda.transform;
            rtAjuda.anchorMin = rtAjuda.anchorMax = new Vector2(.5f, 0);
            var dicaDoOlhar = ajuda.gameObject.AddComponent<DicaDoOlhar>();
            dicaDoOlhar.texto = Texto("Texto", ajuda.transform, "", 30, C("Branco"), Vector2.zero, new Vector2(870, 52), FontStyle.Normal);
            dicaDoOlhar.texto.resizeTextForBestFit = true; dicaDoOlhar.texto.resizeTextMinSize = 20; dicaDoOlhar.texto.resizeTextMaxSize = 30;

            // Recado no alto da tela quando a criança pisa numa área que ainda não tem jogo ("em breve").
            var recado = Imagem("Aviso_Em_Breve", canvas.transform, arredondado, C("Petroleo", .95f), new Vector2(0, -330), new Vector2(980, 140));
            var rtRecado = (RectTransform)recado.transform;
            rtRecado.anchorMin = rtRecado.anchorMax = new Vector2(.5f, 1);
            var avisoDoHub = canvas.gameObject.AddComponent<AvisoDoHub>();
            avisoDoHub.painel = recado.gameObject;
            avisoDoHub.texto = Texto("Texto", recado.transform, "Em breve!", 60, C("Branco"), Vector2.zero, new Vector2(920, 120));
            avisoDoHub.texto.resizeTextForBestFit = true; avisoDoHub.texto.resizeTextMinSize = 36; avisoDoHub.texto.resizeTextMaxSize = 60;
        }

        // ------------------------------------------------------------------ estrelas

        // Limites de pontuação pra 2 e 3 estrelas em cada minigame, conforme a escala de pontos de cada um.
        [MenuItem("Cidade Viva/Menu/Aplicar limites de estrelas nos minigames")]
        public static void AplicarEstrelas()
        {
            var limites = new Dictionary<string, (int, int)>
            {
                ["Minigame_OndeEuVou"] = (70, 100), ["Minigame_TravessiaSegura"] = (60, 90), ["Minigame_ReciclaComigo"] = (60, 90),
                ["Minigame_CorreCorrendo"] = (300, 800), ["Minigame_PedalSeguro"] = (300, 800), ["Minigame_SobeSobe"] = (400, 1000),
                ["Minigame_SemaforoMagico"] = (100, 200), ["Minigame_OlhoVivo"] = (60, 90),
            };
            string atual = SceneManager.GetActiveScene().path;
            foreach (var par in limites)
            {
                var cena = EditorSceneManager.OpenScene($"Assets/Scenes/{par.Key}.unity", OpenSceneMode.Single);
                foreach (var raiz in cena.GetRootGameObjects())
                    foreach (var jogo in raiz.GetComponentsInChildren<MinigameBase>(true))
                    {
                        jogo.pontosPara2Estrelas = par.Value.Item1;
                        jogo.pontosPara3Estrelas = par.Value.Item2;
                        EditorUtility.SetDirty(jogo);
                    }
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }

            // A tela de fim de jogo ganhou uma terceira linha (estrelas): deixa o texto crescer pra baixo.
            const string prefab = "Assets/Prefabs/UI_FimDeJogo.prefab";
            var conteudo = PrefabUtility.LoadPrefabContents(prefab);
            var telaFim = conteudo.GetComponentInChildren<TelaFimDeJogo>(true);
            if (telaFim != null && telaFim.textoPontuacao != null)
            {
                telaFim.textoPontuacao.verticalOverflow = VerticalWrapMode.Overflow;
                PrefabUtility.SaveAsPrefabAsset(conteudo, prefab);
            }
            PrefabUtility.UnloadPrefabContents(conteudo);
            if (!string.IsNullOrEmpty(atual)) EditorSceneManager.OpenScene(atual, OpenSceneMode.Single);
        }
    }
}
