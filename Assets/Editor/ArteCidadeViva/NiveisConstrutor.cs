using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CidadeViva.EditorArte
{
    // Infraestrutura de níveis: os assets de LevelConfig (um por minigame, 5 níveis), o prefab da
    // seleção de níveis, o prefab de fim de jogo com estrelas e "Próximo nível", e a ligação
    // disso tudo nas cenas dos minigames. Tela de referência: 1920x1080.
    public static class NiveisConstrutor
    {
        const string PastaNiveis = "Assets/Configs/Niveis";
        internal const string PrefabSelecao = "Assets/Prefabs/UI_SelecaoDeNivel.prefab";
        internal const string PrefabFim = "Assets/Prefabs/UI_FimDeJogo.prefab";

        static readonly Dictionary<string, string> AssetPorCena = new Dictionary<string, string>
        {
            ["Minigame_OndeEuVou"] = "Niveis_OndeEuVou",
            ["Minigame_ReciclaComigo"] = "Niveis_ReciclaComigo",
            ["Minigame_PedalSeguro"] = "Niveis_PedalSeguro", ["Minigame_SobeSobe"] = "Niveis_SobeSobe",
            ["Minigame_SemaforoMagico"] = "Niveis_SemaforoMagico", ["Minigame_OlhoVivo"] = "Niveis_OlhoVivo",
        };

        [MenuItem("Cidade Viva/Níveis/Recriar prefabs de níveis e ligar nas cenas")]
        public static void RecriarPeloMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RecriarTudo(false);
        }

        // Só cria os assets que faltam: não sobrescreve ajuste feito no Inspector.
        [MenuItem("Cidade Viva/Níveis/Criar os LevelConfig que faltam")]
        public static void CriarConfigsQueFaltam()
        {
            CriarConfigs(false);
        }

        public static void RecriarTudo(bool sobrescreverConfigs)
        {
            MenuConstrutor.PrepararIcones();
            CriarConfigs(sobrescreverConfigs);
            CriarPrefabDeSelecao();
            RefazerPrefabDeFim();
            LigarNasCenas();
        }

        // ------------------------------------------------------------------ LevelConfig

        static NivelConfig N(string objetivo, float velocidade, int obstaculos, float tempo, int itens, int erros,
            MedidaDeEstrelas medida, float para2, float para3, params string[] regras)
        {
            return new NivelConfig
            {
                objetivo = objetivo, velocidade = velocidade, quantidadeDeObstaculos = obstaculos, tempoEmSegundos = tempo,
                quantidadeDeItens = itens, errosPermitidos = erros, regrasExtras = new List<string>(regras),
                estrelas = new CriterioDeEstrelas { medida = medida, para2Estrelas = para2, para3Estrelas = para3 },
            };
        }

        static NivelConfig Com(this NivelConfig n, string extra, float valor)
        {
            n.extras.Add(new ParametroExtra { nome = extra, valor = valor });
            return n;
        }

        static void CriarConfigs(bool sobrescrever)
        {
            ArtePaleta.GarantirPasta(PastaNiveis);
            const MedidaDeEstrelas Pontos = MedidaDeEstrelas.Pontuacao;
            const MedidaDeEstrelas Erros = MedidaDeEstrelas.Erros;

            Salvar("Niveis_TravessiaSegura", sobrescrever, NiveisDaTravessia());

            // Os demais ainda não leem o nível (isso vem quando cada minigame for refeito). Os números já
            // dizem a intenção de cada nível; as estrelas usam Pontuacao porque é o que eles informam hoje.
            Salvar("Niveis_OndeEuVou", sobrescrever,
                N("Arraste cada placa para o lugar certo.", 1f, 0, 0, 3, -1, Pontos, 70, 100),
                N("Agora com mais placas.", 1f, 0, 0, 4, -1, Pontos, 70, 100),
                N("Cinco placas, e o tempo está contando.", 1f, 0, 60, 5, 4, Pontos, 80, 100),
                N("Algumas placas são parecidas. Olhe bem!", 1f, 0, 50, 6, 3, Pontos, 80, 100, "placas_parecidas"),
                N("Placas parecidas e lugares misturados.", 1f, 0, 40, 7, 2, Pontos, 90, 100, "placas_parecidas", "lugares_misturados"));

            Salvar("Niveis_ReciclaComigo", sobrescrever,
                N("Papel no azul, plástico no vermelho, vidro no verde, orgânico no marrom.", .8f, 0, 0, 6, -1, Pontos, 60, 90),
                N("Mais itens para separar.", 1f, 0, 0, 8, -1, Pontos, 60, 90),
                N("Os itens caem mais rápido.", 1.15f, 0, 0, 10, 4, Pontos, 70, 90),
                N("Itens difíceis e tempo contando.", 1.3f, 0, 60, 12, 3, Pontos, 70, 100, "itens_dificeis"),
                N("Dois itens de cada vez. Atenção!", 1.5f, 0, 60, 14, 3, Pontos, 80, 100, "itens_dificeis", "dois_itens_por_vez"));

            Salvar("Niveis_CorreCorrendo", sobrescrever, NiveisDoCorre());

            Salvar("Niveis_PedalSeguro", sobrescrever,
                    N("Troque de faixa para desviar dos obstáculos.", .8f, 2, 40, 0, 3, Pontos, 300, 600),
                    N("Mais obstáculos. Pegue as moedas!", 1f, 3, 50, 0, 3, Pontos, 350, 700),
                    N("Mais rápido, com obstáculos em dupla.", 1.15f, 4, 60, 0, 3, Pontos, 400, 800, "obstaculos_em_dupla"),
                    N("Alguns obstáculos se mexem. Só 2 escudos!", 1.3f, 5, 70, 0, 2, Pontos, 450, 900, "obstaculos_em_dupla", "obstaculos_moveis"),
                    N("Muito rápido e com pouca proteção.", 1.5f, 6, 80, 0, 2, Pontos, 500, 1000, "obstaculos_em_dupla", "obstaculos_moveis", "pouca_protecao"));

            Salvar("Niveis_SobeSobe", sobrescrever,
                N("Pule de plataforma em plataforma e fuja da água.", .8f, 0, 0, 15, 0, Pontos, 300, 600),
                N("A água sobe mais rápido.", 1f, 0, 0, 20, 0, Pontos, 350, 700),
                N("As plataformas ficaram menores.", 1.15f, 0, 0, 25, 0, Pontos, 400, 800, "plataformas_estreitas"),
                N("Algumas plataformas se mexem.", 1.3f, 0, 0, 30, 0, Pontos, 450, 900, "plataformas_estreitas", "plataformas_moveis"),
                N("Plataformas que se mexem e que somem!", 1.45f, 0, 0, 35, 0, Pontos, 500, 1000, "plataformas_estreitas", "plataformas_moveis", "plataformas_que_somem"));

            Salvar("Niveis_SemaforoMagico", sobrescrever,
                N("Verde: siga. Vermelho: pare.", .8f, 0, 0, 8, 3, Pontos, 60, 90).Com("janelaDeReacao", 2.5f),
                N("Mais semáforos pelo caminho.", 1f, 0, 0, 10, 3, Pontos, 80, 110).Com("janelaDeReacao", 2.2f),
                N("Agora tem amarelo: não aperte nada.", 1.1f, 0, 0, 12, 3, Pontos, 100, 140, "amarelo").Com("janelaDeReacao", 1.9f),
                N("O sinal pode mudar no caminho.", 1.25f, 0, 0, 15, 3, Pontos, 120, 170, "amarelo", "sinal_muda").Com("janelaDeReacao", 1.6f),
                N("Rápido, e com pedestre na faixa. Só 2 vidas!", 1.4f, 0, 0, 18, 2, Pontos, 150, 200, "amarelo", "sinal_muda", "pedestre_na_faixa").Com("janelaDeReacao", 1.3f));

            Salvar("Niveis_OlhoVivo", sobrescrever,
                N("Ache o que está errado na cena e toque.", 1f, 0, 45, 3, -1, Pontos, 60, 90),
                N("Agora são quatro erros.", 1f, 0, 40, 4, -1, Pontos, 60, 90),
                N("Menos tempo. Cuidado com toques errados!", 1f, 0, 35, 4, 5, Pontos, 70, 90),
                N("Os erros estão menores e mais escondidos.", 1f, 0, 30, 5, 4, Pontos, 70, 95, "erros_pequenos"),
                N("Cena cheia, seis erros e pouco tempo.", 1f, 0, 30, 6, 3, Pontos, 80, 100, "erros_pequenos", "cena_cheia"));

            AssetDatabase.SaveAssets();
        }

        // Travessia Segura (3D) lê tudo isto: pistas, velocidade, vidas e as regras extras.
        static NivelConfig[] NiveisDaTravessia()
        {
            const MedidaDeEstrelas Vidas = MedidaDeEstrelas.VidasRestantes;
            return new[]
            {
                N("Olhe para os dois lados e atravesse na faixa.", .55f, 1, 0, 0, 2, Vidas, 2, 3)
                    .Com("intervaloDeCarros", 5f),
                N("Agora são duas pistas. Vá até a faixa primeiro!", .75f, 2, 0, 0, 2, Vidas, 2, 3,
                    TravessiaManager.RegraComecarForaDaFaixa)
                    .Com("intervaloDeCarros", 4.2f),
                N("Tem semáforo: só atravesse com o bonequinho verde.", 1f, 2, 0, 0, 2, Vidas, 2, 3,
                    TravessiaManager.RegraComecarForaDaFaixa, TravessiaManager.RegraSemaforo)
                    .Com("intervaloDeCarros", 3.4f).Com("tempoVerde", 6f).Com("tempoVermelho", 6f),
                N("Três pistas e uma ciclovia, sem semáforo. Olhe bem!", 1.1f, 3, 0, 0, 2, Vidas, 2, 3,
                    TravessiaManager.RegraComecarForaDaFaixa, TravessiaManager.RegraCiclista)
                    .Com("intervaloDeCarros", 4f),
                N("Sinal, ciclista e ambulância. Deixe a ambulância passar!", 1.3f, 3, 0, 0, 2, Vidas, 2, 3,
                    TravessiaManager.RegraComecarForaDaFaixa, TravessiaManager.RegraSemaforo, TravessiaManager.RegraCiclista,
                    TravessiaManager.RegraAmbulancia, TravessiaManager.RegraImprevisiveis)
                    .Com("intervaloDeCarros", 3f).Com("tempoVerde", 6.5f).Com("tempoVermelho", 6f).Com("intervaloDaAmbulancia", 13f),
            };
        }

        // Corre, Correndo! (3D) lê: velocidade, escudos (errosPermitidos), regras e extras.
        // As estrelas são calculadas pelo CorreManager (chegar, chegar com escudo, maioria das moedas);
        // o critério de estrelas daqui não é usado.
        static NivelConfig[] NiveisDoCorre()
        {
            const MedidaDeEstrelas Vidas = MedidaDeEstrelas.VidasRestantes;
            return new[]
            {
                N("Rua tranquila: desvie dos cones e pule os buracos.", .8f, 0, 0, 0, 3, Vidas, 1, 1)
                    .Com("metaDeDistancia", 150f).Com("espacoEntreObstaculos", 19f),
                N("Agora tem poça d'água e carro parado.", .9f, 0, 0, 0, 3, Vidas, 1, 1, "pocas", "carros")
                    .Com("metaDeDistancia", 200f).Com("espacoEntreObstaculos", 18f),
                N("Mais rápido! Deslize por baixo dos galhos.", 1.05f, 0, 0, 0, 3, Vidas, 1, 1,
                    "pocas", "carros", "barreiras", "galhos", CorrePista.RegraDuasFaixas)
                    .Com("metaDeDistancia", 250f).Com("espacoEntreObstaculos", 17f),
                N("Está chovendo e a pista está molhada. Atenção!", 1.15f, 0, 0, 0, 3, Vidas, 1, 1,
                    "pocas", "carros", "barreiras", "galhos", CorrePista.RegraDuasFaixas, CorrePista.RegraFileiraCheia, CorreManager.RegraChuva)
                    .Com("metaDeDistancia", 300f).Com("espacoEntreObstaculos", 16f),
                N("Hora do rush: tudo junto e carros em movimento!", 1.3f, 0, 0, 0, 3, Vidas, 1, 1,
                    "pocas", "carros", "barreiras", "galhos", CorrePista.RegraDuasFaixas, CorrePista.RegraFileiraCheia,
                    CorrePista.RegraSequencias, CorrePista.RegraCarrosEmMovimento)
                    .Com("metaDeDistancia", 350f).Com("espacoEntreObstaculos", 16f),
            };
        }

        public static LevelConfig SobrescreverNiveisDoCorre()
        {
            ArtePaleta.GarantirPasta(PastaNiveis);
            Salvar("Niveis_CorreCorrendo", true, NiveisDoCorre());
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<LevelConfig>($"{PastaNiveis}/Niveis_CorreCorrendo.asset");
        }

        public static LevelConfig SobrescreverNiveisDaTravessia()
        {
            ArtePaleta.GarantirPasta(PastaNiveis);
            Salvar("Niveis_TravessiaSegura", true, NiveisDaTravessia());
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<LevelConfig>($"{PastaNiveis}/Niveis_TravessiaSegura.asset");
        }

        static void Salvar(string nome, bool sobrescrever, params NivelConfig[] niveis)
        {
            string caminho = $"{PastaNiveis}/{nome}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<LevelConfig>(caminho);
            if (asset != null && !sobrescrever) return;
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(asset, caminho);
            }
            asset.niveis = new List<NivelConfig>(niveis);
            EditorUtility.SetDirty(asset);
        }

        // ------------------------------------------------------------------ prefab: seleção de níveis

        static Image[] Estrelas(Transform pai, float y, float tamanho, float passo, float alturaDoMeio = 0f)
        {
            var lista = new Image[3];
            for (int i = 0; i < 3; i++)
                lista[i] = MenuConstrutor.Imagem("Estrela_" + (i + 1), pai, MenuConstrutor.S("figura_estrela"), Color.white,
                    new Vector2((i - 1) * passo, y + (i == 1 ? alturaDoMeio : 0f)), Vector2.one * tamanho);
            return lista;
        }

        static void CriarPrefabDeSelecao()
        {
            var raiz = MenuConstrutor.Tela("UI_SelecaoDeNivel", null).gameObject;
            var tela = raiz.AddComponent<TelaSelecaoDeNivel>();
            var voltarAoHub = raiz.AddComponent<VoltarAoHub>();

            var fundo = MenuConstrutor.Imagem("Fundo", raiz.transform, null, MenuConstrutor.C("Petroleo_Escuro", .93f), Vector2.zero, Vector2.zero, true);
            var rtFundo = (RectTransform)fundo.transform;
            rtFundo.anchorMin = Vector2.zero; rtFundo.anchorMax = Vector2.one; rtFundo.offsetMin = rtFundo.offsetMax = Vector2.zero;

            tela.titulo = MenuConstrutor.Texto("Titulo", raiz.transform, "Nome do minigame", 96, MenuConstrutor.C("Branco"), new Vector2(0, 400), new Vector2(1700, 130));
            MenuConstrutor.Texto("Subtitulo", raiz.transform, "Escolha um nível", 50, MenuConstrutor.C("Mostarda_Clara"), new Vector2(0, 300), new Vector2(1200, 70), FontStyle.Normal);

            string[] cores = { "Verde", "Petroleo_Claro", "Mostarda", "Laranja", "Terracota" };
            var botoes = new List<TelaSelecaoDeNivel.BotaoDeNivel>();
            for (int i = 0; i < LevelConfig.QuantidadePadrao; i++)
            {
                var botao = MenuConstrutor.Botao("Nivel_" + (i + 1), raiz.transform, "", cores[i], new Vector2(-680f + i * 340f, 10f), new Vector2(300, 380), 0);
                var frente = botao.transform.Find("Frente");
                Object.DestroyImmediate(frente.Find("Texto").gameObject);
                var conteudo = MenuConstrutor.Ret("Conteudo", frente, Vector2.zero, new Vector2(300, 380));
                MenuConstrutor.Texto("Numero", conteudo, (i + 1).ToString(), 190, MenuConstrutor.C("Branco"), new Vector2(0, 55), new Vector2(280, 220));
                var faixa = MenuConstrutor.Imagem("Faixa_Das_Estrelas", conteudo, MenuConstrutor.arredondado, MenuConstrutor.C("Branco", .9f), new Vector2(0, -120), new Vector2(264, 96));
                var estrelas = Estrelas(faixa.transform, 0, 74, 82);
                var cadeado = MenuConstrutor.Imagem("Cadeado", frente, MenuConstrutor.S("ui_cadeado"), Color.white, Vector2.zero, new Vector2(190, 190));
                UnityEventTools.AddIntPersistentListener(botao.onClick, tela.Escolher, i + 1);
                botoes.Add(new TelaSelecaoDeNivel.BotaoDeNivel { botao = botao, estrelas = estrelas, cadeado = cadeado.gameObject, conteudo = conteudo.gameObject });
            }
            tela.botoes = botoes.ToArray();

            tela.dica = MenuConstrutor.Texto("Dica", raiz.transform, "", 46, MenuConstrutor.C("Branco"), new Vector2(0, -275), new Vector2(1600, 120), FontStyle.Normal);
            var voltar = MenuConstrutor.Botao("Botao_VoltarAoHub", raiz.transform, "Voltar ao Hub", "Cinza", new Vector2(0, -430), new Vector2(460, 110), 46);
            UnityEventTools.AddPersistentListener(voltar.onClick, voltarAoHub.Voltar);

            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabSelecao);
            Object.DestroyImmediate(raiz);
        }

        // ------------------------------------------------------------------ prefab: fim de jogo

        // Refaz o conteúdo do prefab mantendo a raiz e o componente TelaFimDeJogo,
        // pra as instâncias que já estão nas cenas (e as referências dos managers) continuarem valendo.
        static void RefazerPrefabDeFim()
        {
            var raiz = PrefabUtility.LoadPrefabContents(PrefabFim);
            for (int i = raiz.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(raiz.transform.GetChild(i).gameObject);
            var imagemDaRaiz = raiz.GetComponent<Image>();
            if (imagemDaRaiz != null) imagemDaRaiz.enabled = false;

            var tela = raiz.GetComponent<TelaFimDeJogo>();
            if (tela == null) tela = raiz.AddComponent<TelaFimDeJogo>();
            var voltarAoHub = raiz.GetComponent<VoltarAoHub>();
            if (voltarAoHub == null) voltarAoHub = raiz.AddComponent<VoltarAoHub>();

            var fundo = MenuConstrutor.Imagem("Fundo", raiz.transform, null, MenuConstrutor.C("Petroleo_Escuro", .72f), Vector2.zero, Vector2.zero, true);
            var rtFundo = (RectTransform)fundo.transform;
            rtFundo.anchorMin = Vector2.zero; rtFundo.anchorMax = Vector2.one; rtFundo.offsetMin = rtFundo.offsetMax = Vector2.zero;

            var cartao = MenuConstrutor.Imagem("Painel", raiz.transform, MenuConstrutor.arredondado, MenuConstrutor.C("Branco", .98f), Vector2.zero, new Vector2(1080, 800), true).transform;
            tela.textoDoNivel = MenuConstrutor.Texto("Text_Nivel", cartao, "Nível 1", 46, MenuConstrutor.C("Petroleo_Claro"), new Vector2(0, 335), new Vector2(900, 60));
            tela.textoTitulo = MenuConstrutor.Texto("Text_Titulo", cartao, "Fim de jogo!", 68, MenuConstrutor.C("Petroleo"), new Vector2(0, 250), new Vector2(980, 100));
            tela.estrelas = Estrelas(cartao, 75, 180, 210, 26);
            tela.textoPontuacao = MenuConstrutor.Texto("Text_Pontuacao", cartao, "Pontuação: 0\nRecorde: 0", 46, MenuConstrutor.C("Grafite"), new Vector2(0, -110), new Vector2(900, 130), FontStyle.Normal);

            var hub = MenuConstrutor.Botao("Button_VoltarAoHub", cartao, "Voltar ao Hub", "Cinza", new Vector2(-340, -290), new Vector2(310, 130), 38);
            UnityEventTools.AddPersistentListener(hub.onClick, voltarAoHub.Voltar);
            var deNovo = MenuConstrutor.Botao("Button_TentarDeNovo", cartao, "Tentar de novo", "Mostarda", new Vector2(0, -290), new Vector2(310, 130), 38);
            UnityEventTools.AddPersistentListener(deNovo.onClick, tela.TentarDeNovo);
            var proximo = MenuConstrutor.Botao("Button_ProximoNivel", cartao, "Próximo nível", "Verde_Escuro", new Vector2(340, -290), new Vector2(310, 130), 38);
            UnityEventTools.AddPersistentListener(proximo.onClick, tela.ProximoNivel);
            tela.botaoProximoNivel = proximo;

            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabFim);
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        // ------------------------------------------------------------------ cenas

        static void LigarNasCenas()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSelecao);
            string atual = SceneManager.GetActiveScene().path;
            foreach (var par in AssetPorCena)
            {
                var cena = EditorSceneManager.OpenScene($"Assets/Scenes/{par.Key}.unity", OpenSceneMode.Single);
                var config = AssetDatabase.LoadAssetAtPath<LevelConfig>($"{PastaNiveis}/{par.Value}.asset");
                foreach (var raiz in cena.GetRootGameObjects())
                    foreach (var jogo in raiz.GetComponentsInChildren<MinigameBase>(true))
                    {
                        jogo.niveis = config;
                        if (jogo.telaFimDeJogo == null) { Debug.LogWarning($"[Níveis] {par.Key}: manager sem telaFimDeJogo."); continue; }
                        var canvas = jogo.telaFimDeJogo.transform.parent;
                        var antiga = canvas.GetComponentInChildren<TelaSelecaoDeNivel>(true);
                        if (antiga != null) Object.DestroyImmediate(antiga.gameObject);
                        var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
                        instancia.transform.SetAsLastSibling();
                        jogo.telaDeSelecao = instancia.GetComponent<TelaSelecaoDeNivel>();
                        // A tela de fim fica por cima de tudo, menos da seleção (as duas nunca aparecem juntas).
                        EditorUtility.SetDirty(jogo);
                        var escala = canvas.GetComponentInParent<CanvasScaler>();
                        Debug.Log($"[Níveis] {par.Key}: {jogo.GetType().Name} ligado a {par.Value}; canvas {(escala != null ? escala.uiScaleMode + " " + escala.referenceResolution : "sem CanvasScaler")}");
                    }
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }
            if (!string.IsNullOrEmpty(atual)) EditorSceneManager.OpenScene(atual, OpenSceneMode.Single);
        }
    }
}
