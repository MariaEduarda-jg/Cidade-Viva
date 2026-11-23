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
    // Monta do zero a mecânica "Obras da Cidade", com os dois modos: "Pontes e Passagens" (ponte, túnel,
    // viaduto, passarela, passagem subterrânea e faixa de pedestre) e "Planejar a rua" (via, via rural,
    // via urbana, calçada, ciclovia, ciclofaixa, retorno e estacionamento): prefabs das estruturas e dos
    // cenários, os problemas, a config, os níveis de cada modo e a cena Minigame_Obras.
    // Os problemas (Assets/Resources/Obras) e as regras das estruturas (na config) só são criados se ainda
    // não existirem: depois de revisados pela equipe, rodar de novo não desfaz a revisão. Toda vez, confere
    // se cada trecho citado existe mesmo na definição e se cada problema tem UMA só opção que serve.
    public static class ObrasConstrutor
    {
        const string CaminhoDaCena = "Assets/Scenes/Minigame_Obras.unity";
        const string CaminhoDaConfig = "Assets/Configs/Obras_Config.asset";
        const string CaminhoDosNiveis = "Assets/Configs/Niveis/Niveis_Obras.asset";
        const string PastaDosProblemas = "Assets/Resources/" + ObraProblema.PastaNoResources;
        const string Categoria = "Obras";
        public const string NomeDaArea = "Obras da Cidade";
        public const string NomeDaCena = "Minigame_Obras";
        const string CaminhoDosNiveisDeRuas = "Assets/Configs/Niveis/Niveis_ObrasRuas.asset";
        // Nome com que o progresso do segundo modo é guardado no perfil.
        public const string NomeDaAreaDeRuas = "Planejar a rua";
        static readonly string[] Estruturas =
        {
            "ponte", "tunel", "viaduto", "passarela", "passagem_subterranea", "faixa_de_pedestre",
            "via", "via_rural", "via_urbana", "calcada", "ciclovia", "ciclofaixa", "retorno", "estacionamento",
        };
        static readonly ObstaculoDaObra[] Situacoes =
        {
            ObstaculoDaObra.CampoECidade, ObstaculoDaObra.DentroDaCidade, ObstaculoDaObra.LugarQualquer, ObstaculoDaObra.BeiraDaRua,
            ObstaculoDaObra.SeparadoDoTransito, ObstaculoDaObra.MudarDeSentido, ObstaculoDaObra.GuardarVeiculos,
        };

        [MenuItem("Cidade Viva/Minigames/Reconstruir a cena das Obras da Cidade")]
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
            PrepararIcones();
            VerbetesConstrutor.Atualizar();
            GerarPrefabs();
            GerarPrefabsDeRuas();
            var config = PrepararConfig();
            CriarProblemasQueFaltam();
            ConferirProblemas();
            var niveis = SalvarNiveis();
            var niveisDeRuas = SalvarNiveisDeRuas();

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 45f;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AmbienteConstrutor.CaminhoPrefab) == null) AmbienteConstrutor.CriarPrefab();
            AmbienteConstrutor.AplicarNaCenaAberta(true);
            // Só pra cena não abrir olhando pro nada; em jogo quem posiciona é o ObrasManager.
            cam.transform.SetPositionAndRotation(F.V(0, 23f, -27f), Quaternion.Euler(42f, 0, 0));

            // O gramado embaixo de tudo; os lotes dos problemas são montados em cima dele em tempo de jogo.
            var cenario = F.Grupo("Cenario").transform;
            SemSombra(F.Plano(cenario, "Gramado", "Verde_Claro", F.V(0, -.16f, 30f), 500, 500));
            var mapa = F.Grupo("Mapa").transform;

            // Modelos inativos: o manager copia um lote, um obstáculo e as estruturas a cada problema.
            var modelos = F.Grupo("Modelos").transform;
            GameObject Modelo(string prefab)
            {
                var go = P(modelos, Categoria + "/" + prefab, Vector3.zero);
                go.name = "Modelo_" + prefab;
                go.SetActive(false);
                return go;
            }
            var estruturas = Estruturas.Select(id => Modelo("Estrutura_" + id).GetComponent<ObraEstrutura>()).ToArray();

            // ---------------- efeitos ----------------
            var efeitos = F.Grupo("Efeitos").transform;
            var estrelinhas = TravessiaConstrutor.Particulas(efeitos, "Estrelinhas", 14, .7f, 3f, .25f, .6f, "Mostarda", "Mostarda_Clara", "Branco");
            var confete = TravessiaConstrutor.Particulas(efeitos, "Confete", 70, 1.5f, 6.5f, .3f, 2.5f,
                "Mostarda", "Vermelho", "Petroleo_Claro", "Verde", "Rosa", "Laranja");
            var poeira = TravessiaConstrutor.Particulas(efeitos, "Poeira", 22, .9f, 3.2f, .5f, .3f, "Cinza_Claro", "Areia", "Branco");

            // ---------------- interface ----------------
            var canvas = NovoCanvas("Canvas");
            NovoEventSystem(false);
            var hud = Hud(canvas.transform, out var botaoOuvir, out var botaoDica);
            var fim = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabFim), canvas.transform);
            var selecao = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabSelecao), canvas.transform);
            // Por cima da seleção de níveis: as abas dos dois modos.
            var abas = Abas(canvas.transform, hud);

            // ---------------- gerente ----------------
            var gerenteGo = new GameObject("ObrasManager", typeof(AudioSource));
            var som = gerenteGo.GetComponent<AudioSource>();
            som.playOnAwake = false;
            som.volume = .8f;
            var gerente = gerenteGo.AddComponent<ObrasManager>();
            gerente.nomeDaArea = NomeDaArea;
            gerente.telaFimDeJogo = fim.GetComponent<TelaFimDeJogo>();
            gerente.telaDeSelecao = selecao.GetComponent<TelaSelecaoDeNivel>();
            gerente.niveis = niveis;
            gerente.config = config;
            gerente.hud = hud;
            gerente.som = som;
            gerente.cameraDoJogo = cam;
            gerente.mapa = mapa;
            gerente.modeloDoLote = Modelo("Lote");
            gerente.modeloDoRio = Modelo("Obstaculo_Rio");
            gerente.modeloDoMorro = Modelo("Obstaculo_Morro");
            gerente.modeloDaAvenida = Modelo("Obstaculo_Avenida");
            gerente.modeloDaRua = Modelo("Obstaculo_Rua");
            gerente.modeloDoLocal = Modelo("Local_Da_Obra").GetComponent<ObraLocal>();
            gerente.modeloDoHospital = Modelo("Hospital");
            gerente.modelosDeEstrutura = estruturas;
            foreach (var situacao in Situacoes)
                gerente.modelosDeSituacao.Add(new ObrasManager.ModeloDaSituacao { situacao = situacao, modelo = Modelo("Situacao_" + situacao) });
            gerente.niveisPorModo = new[] { niveis, niveisDeRuas };
            gerente.areasPorModo = new[] { NomeDaArea, NomeDaAreaDeRuas };
            for (int i = 0; i < abas.Length; i++)
                UnityEventTools.AddIntPersistentListener(abas[i].onClick, gerente.TrocarModo, i);
            gerente.estrelinhas = estrelinhas;
            gerente.confete = confete;
            gerente.poeira = poeira;
            foreach (var botao in hud.botoes) botao.manager = gerente;
            UnityEventTools.AddPersistentListener(botaoOuvir.onClick, gerente.OuvirDefinicao);
            UnityEventTools.AddPersistentListener(botaoDica.onClick, gerente.PedirDica);

            EditorSceneManager.SaveScene(cena, CaminhoDaCena);
            if (EditorBuildSettings.scenes.All(s => s.path != CaminhoDaCena))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(CaminhoDaCena, true)).ToArray();
        }

        // ------------------------------------------------------------------ regras e problemas (derivados das definições)

        static RegraDaEstrutura Regra(string id, ObstaculoDaObra[] obstaculos, string trechoDoObstaculo, QuemPassaNaObra[] quem, string trechoDeQuem,
            string verbeteDoTrechoDeQuem = "")
        {
            return new RegraDaEstrutura
            {
                idDoVerbete = id, obstaculos = obstaculos, trechoDoObstaculo = trechoDoObstaculo, quemPassa = quem, trechoDeQuemPassa = trechoDeQuem,
                verbeteDoTrechoDeQuem = verbeteDoTrechoDeQuem,
            };
        }

        // Cada regra vem de um trecho da definição do verbete, copiado letra por letra.
        static List<RegraDaEstrutura> RegrasDasDefinicoes()
        {
            const ObstaculoDaObra Rio = ObstaculoDaObra.Rio, Morro = ObstaculoDaObra.Morro, Via = ObstaculoDaObra.Via;
            const ObstaculoDaObra Campo = ObstaculoDaObra.CampoECidade, Cidade = ObstaculoDaObra.DentroDaCidade, Qualquer = ObstaculoDaObra.LugarQualquer;
            const ObstaculoDaObra Beira = ObstaculoDaObra.BeiraDaRua, Separado = ObstaculoDaObra.SeparadoDoTransito;
            const ObstaculoDaObra Voltar = ObstaculoDaObra.MudarDeSentido, Guardar = ObstaculoDaObra.GuardarVeiculos;
            const QuemPassaNaObra Veiculo = QuemPassaNaObra.Veiculo, Pedestre = QuemPassaNaObra.Pedestre, Animal = QuemPassaNaObra.Animal;
            const QuemPassaNaObra Ciclista = QuemPassaNaObra.Ciclista;
            return new List<RegraDaEstrutura>
            {
                Regra("ponte", new[] { Rio }, "sobre água, como de rios ou lagos", new[] { Veiculo, Pedestre, Animal }, "veículos, pedestres e animais"),
                Regra("tunel", new[] { Via, Morro }, "através de outras vias, morros e montanhas", new[] { Veiculo, Pedestre }, "veículos ou pessoas"),
                Regra("viaduto", new[] { Via }, "por cima de outras vias", new[] { Veiculo }, "para veículos"),
                Regra("passarela", new[] { Via }, "sobre vias", new[] { Pedestre }, "travessia de pedestres"),
                Regra("passagem_subterranea", new[] { Via }, "abaixo da via", new[] { Pedestre, Animal }, "para pedestre, ou animal"),
                Regra("faixa_de_pedestre", new[] { Via }, "Parte da via pintada com listras brancas", new[] { Pedestre }, "para o pedestre atravessar"),

                // ---- modo "Planejar a rua"
                // Via é a peça geral: serve em qualquer ligação entre dois lugares. O ciclista entra como veículo
                // porque a definição de bicicleta começa com "Veículo" (isso NÃO está na definição de via).
                Regra("via", new[] { Campo, Cidade, Qualquer }, "para se movimentarem de um lugar para o outro",
                    new[] { Veiculo, Pedestre, Animal, Ciclista }, "veículos, pessoas e animais"),
                // Via rural e via urbana dizem ONDE ficam, mas não quem passa: isso é herdado da definição de via.
                Regra("via_rural", new[] { Campo }, "liga propriedades rurais às cidades",
                    new[] { Veiculo, Pedestre, Animal, Ciclista }, "veículos, pessoas e animais", "via"),
                Regra("via_urbana", new[] { Cidade }, "dentro da cidade, que fica próxima a casas, prédios e lojas",
                    new[] { Veiculo, Pedestre, Animal, Ciclista }, "veículos, pessoas e animais", "via"),
                Regra("calcada", new[] { Beira }, "Parte de uma rua", new[] { Pedestre, Animal }, "pedestres ou animais"),
                Regra("ciclovia", new[] { Separado }, "está em via separada do trânsito dos veículos", new[] { Ciclista }, "circulação exclusiva para ciclista"),
                Regra("ciclofaixa", new[] { Beira }, "está ao lado da via dos outros veículos", new[] { Ciclista }, "circulação exclusiva de bicicletas"),
                Regra("retorno", new[] { Voltar }, "passar para se mover na via de sentido contrário ao que estava", new[] { Veiculo }, "permite o veículo mudar o lado"),
                Regra("estacionamento", new[] { Guardar }, "Local para guardar veículos temporariamente, de forma organizada", new[] { Veiculo }, "guardar veículos"),
            };
        }

        static void Problema(int nivel, int ordem, string id, ObstaculoDaObra obstaculo, QuemPassaNaObra quem, string nome, string modelo,
            string correta, string trecho, string[] outras, bool ruaComum = false, bool ambulancia = false,
            int modo = 0, string enunciado = "", string nota = "")
        {
            string caminho = $"{PastaDosProblemas}/Problema_{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<ObraProblema>(caminho) != null) return;
            var p = ScriptableObject.CreateInstance<ObraProblema>();
            p.modo = modo; p.nivel = nivel; p.ordem = ordem;
            p.enunciado = enunciado;
            p.obstaculo = obstaculo; p.ruaComum = ruaComum;
            p.quemPassa = quem; p.nomeDeQuemPassa = nome;
            p.modeloDeQuemPassa = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArteImportador.PastaPrefabs}/{modelo}.prefab");
            if (p.modeloDeQuemPassa == null) Debug.LogError("[Obras] Prefab não encontrado: " + modelo);
            p.correta = correta; p.trechoDaDefinicao = trecho;
            // A certa não fica sempre no mesmo lugar da barra.
            var opcoes = new List<string>(outras);
            opcoes.Insert((nivel + ordem) % (outras.Length + 1), correta);
            p.opcoes = opcoes.ToArray();
            p.rotaDaAmbulancia = ambulancia;
            p.observacao = string.IsNullOrEmpty(nota) ? "Gerado pelo ObrasConstrutor a partir das definições do glossário; pendente de revisão da coordenação." : nota;
            AssetDatabase.CreateAsset(p, caminho);
        }

        static void CriarProblemasQueFaltam()
        {
            ArtePaleta.GarantirPasta(PastaDosProblemas);
            const ObstaculoDaObra Rio = ObstaculoDaObra.Rio, Morro = ObstaculoDaObra.Morro, Via = ObstaculoDaObra.Via;
            const QuemPassaNaObra Veiculo = QuemPassaNaObra.Veiculo, Pedestre = QuemPassaNaObra.Pedestre, Animal = QuemPassaNaObra.Animal;

            // Nível 1: rio com ponte, morro com túnel; duas opções.
            Problema(1, 0, "n1_rio_carro", Rio, Veiculo, "O carro", "Carros/Carro_Sedan", "ponte",
                "atravessem sobre água, como de rios ou lagos", new[] { "tunel" });
            Problema(1, 1, "n1_morro_van", Morro, Veiculo, "A van", "Carros/Carro_Van", "tunel",
                "através de outras vias, morros e montanhas", new[] { "ponte" });

            // Nível 2: viaduto e passarela (quem passa é o que decide).
            Problema(2, 0, "n2_avenida_taxi", Via, Veiculo, "O táxi", "Carros/Carro_Taxi", "viaduto",
                "para veículos passarem por cima de outras vias", new[] { "passarela" });
            Problema(2, 1, "n2_avenida_menina", Via, Pedestre, "A menina", "Personagens/Familia_Filha_Animado", "passarela",
                "travessia de pedestres sobre vias", new[] { "viaduto" });

            // Nível 3: passagem subterrânea e faixa de pedestre.
            Problema(3, 0, "n3_avenida_cachorro", Via, Animal, "O cachorro", "Natureza/Cachorro_Caramelo", "passagem_subterranea",
                "para pedestre, ou animal atravessar", new[] { "passarela", "viaduto" });
            Problema(3, 1, "n3_rua_vovo", Via, Pedestre, "O vovô", "Personagens/Idoso", "faixa_de_pedestre",
                "para o pedestre atravessar com segurança quando o veículo para", new[] { "viaduto", "ponte" }, ruaComum: true);

            // Nível 4: três problemas, quatro opções parecidas em cada.
            Problema(4, 0, "n4_rio_moca", Rio, Pedestre, "A moça", "Personagens/Pedestre_Mulher", "ponte",
                "veículos, pedestres e animais atravessem sobre água", new[] { "passarela", "passagem_subterranea", "faixa_de_pedestre" });
            Problema(4, 1, "n4_morro_menino", Morro, Pedestre, "O menino", "Personagens/Familia_FilhoMaisNovo_Animado", "tunel",
                "para o transporte de veículos ou pessoas através de outras vias, morros e montanhas", new[] { "passarela", "ponte", "viaduto" });
            Problema(4, 2, "n4_avenida_caminhonete", Via, Veiculo, "A caminhonete", "Carros/Carro_Picape", "viaduto",
                "Via elevada para veículos", new[] { "passarela", "passagem_subterranea", "faixa_de_pedestre" });

            // Nível 5: a ambulância passa por três obras em fila até o hospital, e um cachorro atravessa a rua ao lado.
            Problema(5, 0, "n5_rio_ambulancia", Rio, Veiculo, "A ambulância", "Carros/Ambulancia", "ponte",
                "atravessem sobre água, como de rios ou lagos", new[] { "viaduto", "tunel", "passarela" }, ambulancia: true);
            Problema(5, 1, "n5_morro_ambulancia", Morro, Veiculo, "A ambulância", "Carros/Ambulancia", "tunel",
                "através de outras vias, morros e montanhas", new[] { "ponte", "viaduto", "passagem_subterranea" }, ambulancia: true);
            Problema(5, 2, "n5_avenida_ambulancia", Via, Veiculo, "A ambulância", "Carros/Ambulancia", "viaduto",
                "para veículos passarem por cima de outras vias", new[] { "passarela", "passagem_subterranea", "faixa_de_pedestre" }, ambulancia: true);
            Problema(5, 3, "n5_rua_cachorro", Via, Animal, "O cachorro", "Natureza/Cachorro_Husky", "passagem_subterranea",
                "para pedestre, ou animal atravessar", new[] { "passarela", "faixa_de_pedestre", "tunel" }, ruaComum: true);
            ProblemasDeRuas();
            AssetDatabase.SaveAssets();
        }

        // Modo "Planejar a rua". Em "nota" fica o que NÃO vem direto da definição (pra revisão da coordenação).
        static void ProblemasDeRuas()
        {
            const ObstaculoDaObra Campo = ObstaculoDaObra.CampoECidade, Cidade = ObstaculoDaObra.DentroDaCidade, Qualquer = ObstaculoDaObra.LugarQualquer;
            const ObstaculoDaObra Beira = ObstaculoDaObra.BeiraDaRua, Separado = ObstaculoDaObra.SeparadoDoTransito;
            const ObstaculoDaObra Voltar = ObstaculoDaObra.MudarDeSentido, Guardar = ObstaculoDaObra.GuardarVeiculos;
            const QuemPassaNaObra Veiculo = QuemPassaNaObra.Veiculo, Pedestre = QuemPassaNaObra.Pedestre, Animal = QuemPassaNaObra.Animal;
            const QuemPassaNaObra Ciclista = QuemPassaNaObra.Ciclista;
            const string herdado = "A definição de via rural/via urbana não diz quem passa: isso vem da definição de via. A via (peça geral) também serviria aqui, por isso não é oferecida.";
            const string beira = "\"Parte de uma rua\" / \"ao lado da via dos outros veículos\" foi encenado como a beira de uma rua com carros passando.";

            // Nível 1: uma ligação de cada tipo, duas opções.
            Problema(1, 0, "r1_fazenda_caminhao", Campo, Veiculo, "O caminhão", "Carros/Carro_Entregas", "via_rural",
                "liga propriedades rurais às cidades", new[] { "via_urbana" }, modo: 1,
                enunciado: "O caminhão vai da fazenda até a cidade.", nota: herdado + " Caminhão foi tratado como veículo.");
            Problema(1, 1, "r1_cidade_carro", Cidade, Veiculo, "O carro", "Carros/Carro_Sedan", "via_urbana",
                "dentro da cidade, que fica próxima a casas, prédios e lojas", new[] { "via_rural" }, modo: 1,
                enunciado: "O carro vai da casa até a loja, dentro da cidade.", nota: herdado);

            // Nível 2: a via geral e a calçada.
            Problema(2, 0, "r2_lugar_cachorro", Qualquer, Animal, "O cachorro", "Natureza/Cachorro_Caramelo", "via",
                "para se movimentarem de um lugar para o outro", new[] { "via_rural", "via_urbana" }, modo: 1,
                enunciado: "O cachorro vai do lago até a mata. Aqui não é cidade nem fazenda.",
                nota: "O cenário \"nem cidade nem fazenda\" (lago e mata) é invenção do jogo, pra a via geral ser a única que serve.");
            Problema(2, 1, "r2_rua_vovo", Beira, Pedestre, "A vovó", "Personagens/Idosa", "calcada",
                "destinada para a passagem de pedestres ou animais", new[] { "ciclofaixa", "estacionamento" }, modo: 1,
                enunciado: "A vovó vai a pé por esta rua, fora do caminho dos carros.", nota: beira);

            // Nível 3: ciclofaixa (ao lado dos carros) contra ciclovia (separada dos carros).
            Problema(3, 0, "r3_rua_ciclista", Beira, Ciclista, "O ciclista", "Obras/Ciclista", "ciclofaixa",
                "está ao lado da via dos outros veículos", new[] { "ciclovia", "calcada" }, modo: 1,
                enunciado: "O ciclista vai pedalar nesta rua, ao lado dos carros.", nota: beira);
            Problema(3, 1, "r3_parque_ciclista", Separado, Ciclista, "O ciclista", "Obras/Ciclista", "ciclovia",
                "está em via separada do trânsito dos veículos", new[] { "ciclofaixa", "calcada" }, modo: 1,
                enunciado: "O ciclista vai pedalar pelo parque, longe dos carros.",
                nota: "O parque com a rua dos carros ao longe é encenação do jogo pra \"via separada do trânsito dos veículos\".");

            // Nível 4: retorno, estacionamento e calçada pra animal; quatro opções.
            Problema(4, 0, "r4_avenida_retorno", Voltar, Veiculo, "O carro", "Carros/Carro_Hatch", "retorno",
                "passar para se mover na via de sentido contrário ao que estava", new[] { "estacionamento", "via_urbana", "ciclofaixa" }, modo: 1,
                enunciado: "O carro errou o caminho e precisa voltar pela outra pista.",
                nota: "A avenida de duas pistas com canteiro no meio é encenação do jogo; a definição só fala em mudar de lado e de sentido.");
            Problema(4, 1, "r4_loja_carros", Guardar, Veiculo, "O táxi", "Carros/Carro_Taxi", "estacionamento",
                "Local para guardar veículos temporariamente, de forma organizada", new[] { "retorno", "calcada", "via_rural" }, modo: 1,
                enunciado: "Três carros chegaram à loja e precisam ficar guardados.",
                nota: "\"temporariamente\" não aparece no jogo. \"De forma organizada\" virou vagas pintadas, uma pra cada carro.");
            Problema(4, 2, "r4_rua_cachorro", Beira, Animal, "O cachorro", "Natureza/Cachorro_Husky", "calcada",
                "pedestres ou animais", new[] { "ciclofaixa", "ciclovia", "retorno" }, modo: 1,
                enunciado: "O cachorro vai passear por esta rua, fora do caminho dos carros.", nota: beira);

            // Nível 5: quatro obras no mesmo mapa, com pedestre e ciclista na mesma rua, e tempo.
            Problema(5, 0, "r5_fazenda_caminhao", Campo, Veiculo, "O caminhão", "Carros/Caminhao_Lixo", "via_rural",
                "liga propriedades rurais às cidades", new[] { "via_urbana", "ciclovia", "calcada" }, modo: 1,
                enunciado: "O caminhão vai da fazenda até a cidade.", nota: herdado + " Caminhão foi tratado como veículo.");
            Problema(5, 1, "r5_cidade_taxi", Cidade, Veiculo, "O táxi", "Carros/Carro_Taxi", "via_urbana",
                "dentro da cidade, que fica próxima a casas, prédios e lojas", new[] { "via_rural", "estacionamento", "retorno" }, modo: 1,
                enunciado: "O táxi vai do prédio até a padaria, dentro da cidade.", nota: herdado);
            Problema(5, 2, "r5_rua_menina", Beira, Pedestre, "A menina", "Personagens/Familia_Filha_Animado", "calcada",
                "destinada para a passagem de pedestres ou animais", new[] { "ciclofaixa", "ciclovia", "estacionamento" }, modo: 1,
                enunciado: "A menina vai a pé para a escola por esta rua.", nota: beira);
            Problema(5, 3, "r5_rua_ciclista", Beira, Ciclista, "O ciclista", "Obras/Ciclista", "ciclofaixa",
                "está ao lado da via dos outros veículos", new[] { "calcada", "ciclovia", "retorno" }, modo: 1,
                enunciado: "O ciclista vai para a escola por esta rua, ao lado dos carros.", nota: beira);
        }


        // Confere regras e problemas com as definições e grava a lista em Temp/obras_problemas.txt.
        [MenuItem("Cidade Viva/Verbetes/Conferir problemas das Obras com as definições")]
        public static bool ConferirProblemas()
        {
            var config = AssetDatabase.LoadAssetAtPath<ObrasConfig>(CaminhoDaConfig);
            var banco = VerbeteDatabase.Instancia;
            var falhas = new List<string>();
            var lista = new StringBuilder();
            if (config == null) { Debug.LogError("[Obras] Falta a config: " + CaminhoDaConfig); return false; }

            lista.AppendLine("REGRAS DAS ESTRUTURAS");
            foreach (var r in config.regras)
            {
                var v = banco.Buscar(r.idDoVerbete);
                if (v == null) { falhas.Add("regra de verbete desconhecido: " + r.idDoVerbete); continue; }
                // Quem passa pode vir da definição de outro verbete (via rural e via urbana herdam de via).
                var deQuem = string.IsNullOrEmpty(r.verbeteDoTrechoDeQuem) ? v : banco.Buscar(r.verbeteDoTrechoDeQuem);
                if (!v.definicao.Contains(r.trechoDoObstaculo)) falhas.Add($"{r.idDoVerbete}: o trecho \"{r.trechoDoObstaculo}\" não está na definição");
                if (deQuem == null || !deQuem.definicao.Contains(r.trechoDeQuemPassa))
                    falhas.Add($"{r.idDoVerbete}: o trecho \"{r.trechoDeQuemPassa}\" não está na definição de {(deQuem != null ? deQuem.palavra : r.verbeteDoTrechoDeQuem)}");
                lista.AppendLine($"{v.palavra}: serve em {string.Join("/", r.obstaculos)} <= \"{r.trechoDoObstaculo}\"; passa {string.Join("/", r.quemPassa)} <= \"{r.trechoDeQuemPassa}\""
                    + (deQuem != v && deQuem != null ? $" (NÃO está na definição de {v.palavra}: vem da definição de {deQuem.palavra})" : ""));
                lista.AppendLine($"    definição: {v.definicao}");
            }

            var problemas = AssetDatabase.FindAssets("t:ObraProblema", new[] { PastaDosProblemas })
                .Select(g => AssetDatabase.LoadAssetAtPath<ObraProblema>(AssetDatabase.GUIDToAssetPath(g)))
                .OrderBy(p => p.modo).ThenBy(p => p.nivel).ThenBy(p => p.ordem).ToList();
            int modo = -1;
            foreach (var p in problemas)
            {
                if (p.modo != modo)
                {
                    modo = p.modo;
                    lista.AppendLine().AppendLine(modo == 0 ? "PROBLEMAS - PONTES E PASSAGENS" : "PROBLEMAS - PLANEJAR A RUA");
                }
                var servem = new List<string>();
                foreach (string id in p.opcoes)
                {
                    var regra = config.Regra(id);
                    if (regra == null || banco.Buscar(id) == null) { falhas.Add($"{p.name}: opção sem regra ou sem verbete: {id}"); continue; }
                    if (regra.Serve(p.obstaculo, p.quemPassa)) servem.Add(id);
                }
                if (servem.Count != 1) falhas.Add($"{p.name}: {servem.Count} opções servem ({string.Join(", ", servem)}); tem de ser só uma");
                else if (servem[0] != p.correta) falhas.Add($"{p.name}: a correta marcada é {p.correta}, mas pelas regras é {servem[0]}");
                var certa = banco.Buscar(p.correta);
                if (certa == null || !certa.definicao.Contains(p.trechoDaDefinicao)) falhas.Add($"{p.name}: o trecho \"{p.trechoDaDefinicao}\" não está na definição de {p.correta}");
                if (p.modeloDeQuemPassa == null) falhas.Add($"{p.name}: sem modelo de quem passa");
                string obstaculo = p.obstaculo == ObstaculoDaObra.Via ? (p.ruaComum ? "via (rua comum)" : "via (avenida com carros)") : p.obstaculo.ToString();
                lista.AppendLine($"N{p.nivel}.{p.ordem + 1} | {(string.IsNullOrEmpty(p.enunciado) ? "" : "\"" + p.enunciado + "\" | ")}situação: {obstaculo} | quem passa: {p.nomeDeQuemPassa} ({p.quemPassa})"
                    + $" | opções: {string.Join(", ", p.opcoes.Select(id => banco.Buscar(id) != null ? banco.Buscar(id).palavra : id))}"
                    + $" | correta: {(certa != null ? certa.palavra : p.correta)} | frase: \"{p.trechoDaDefinicao}\"{(p.rotaDaAmbulancia ? " | rota da ambulância" : "")}");
                if (p.modo != 0) lista.AppendLine($"      não vem direto da definição: {p.observacao}");
            }

            lista.AppendLine().AppendLine(falhas.Count == 0 ? "CONFERÊNCIA: tudo certo." : "CONFERÊNCIA: " + falhas.Count + " falha(s):");
            foreach (string f in falhas) lista.AppendLine("  - " + f);
            File.WriteAllText("Temp/obras_problemas.txt", lista.ToString(), Encoding.UTF8);
            if (falhas.Count == 0) Debug.Log($"[Obras] {problemas.Count} problemas conferidos com as definições. Lista em Temp/obras_problemas.txt.");
            else Debug.LogError("[Obras] Problemas que não batem com as definições:\n" + string.Join("\n", falhas));
            return falhas.Count == 0;
        }


        // ------------------------------------------------------------------ níveis e config

        static NivelConfig Nivel(string objetivo, float tempo, params string[] regras)
        {
            return new NivelConfig
            {
                objetivo = objetivo, tempoEmSegundos = tempo, regrasExtras = new List<string>(regras),
                // As estrelas são calculadas pelo ObrasManager (sem erros, no tempo, sem dica); isto só alimenta o texto da seleção.
                estrelas = new CriterioDeEstrelas { medida = MedidaDeEstrelas.Erros, para2Estrelas = 1, para3Estrelas = 0 },
            };
        }

        static LevelConfig SalvarNiveis()
        {
            ArtePaleta.GarantirPasta(Path.GetDirectoryName(CaminhoDosNiveis).Replace('\\', '/'));
            var asset = AssetDatabase.LoadAssetAtPath<LevelConfig>(CaminhoDosNiveis);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(asset, CaminhoDosNiveis);
            }
            asset.niveis = new List<NivelConfig>
            {
                Nivel("Leia a definição e ligue os dois lados. A dica ajuda!", 0f, ObrasManager.RegraDicaVisivel),
                Nivel("Passar por cima da avenida: quem é que vai passar?", 0f),
                Nivel("Por baixo ou pela faixa? Leia com atenção!", 0f),
                Nivel("Três obras, e as opções são parecidas.", 0f),
                Nivel("Leve a ambulância até o hospital antes de o tempo acabar!", 180f),
            };
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static LevelConfig SalvarNiveisDeRuas()
        {
            var asset = AssetDatabase.LoadAssetAtPath<LevelConfig>(CaminhoDosNiveisDeRuas);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(asset, CaminhoDosNiveisDeRuas);
            }
            asset.niveis = new List<NivelConfig>
            {
                Nivel("Fazenda ou cidade? Escolha a via certa. A dica ajuda!", 0f, ObrasManager.RegraDicaVisivel),
                Nivel("Uma via para todos e um lugar para andar a pé.", 0f),
                Nivel("Ciclovia ou ciclofaixa? Veja onde ficam os carros!", 0f),
                Nivel("Voltar, guardar os carros e passear: três obras.", 0f),
                Nivel("Planeje a rua inteira antes de o tempo acabar!", 200f),
            };
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static ObrasConfig PrepararConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ObrasConfig>(CaminhoDaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ObrasConfig>();
                AssetDatabase.CreateAsset(config, CaminhoDaConfig);
            }
            // Só entram as regras que faltam: as que já existem podem ter sido revisadas.
            foreach (var regra in RegrasDasDefinicoes())
                if (config.Regra(regra.idDoVerbete) == null) config.regras.Add(regra);
            // Medidas e textos acertados depois do primeiro teste.
            var padrao = ScriptableObject.CreateInstance<ObrasConfig>();
            config.distanciaDaCamera = padrao.distanciaDaCamera;
            config.inclinacaoDaCamera = padrao.inclinacaoDaCamera;
            config.limitesDaInclinacao = padrao.limitesDaInclinacao;
            config.deslocamentoDoFoco = padrao.deslocamentoDoFoco;
            config.balaoDeObstaculoErrado = padrao.balaoDeObstaculoErrado;
            config.balaoDeQuemErrado = padrao.balaoDeQuemErrado;
            Object.DestroyImmediate(padrao);
            config.somDeEscolher = TravessiaConstrutor.Som("Interface/select_001.ogg");
            config.somDeObra = TravessiaConstrutor.Som("Efeitos/impactPlank_medium_000.ogg");
            config.somDeAcerto = TravessiaConstrutor.Som("Interface/confirmation_002.ogg");
            config.somDeErro = TravessiaConstrutor.Som("Interface/error_004.ogg");
            config.somDeComemoracao = TravessiaConstrutor.Som("Interface/confirmation_003.ogg");
            config.somDeDica = TravessiaConstrutor.Som("Interface/question_001.ogg");
            config.somDeBloqueio = TravessiaConstrutor.Som("Interface/back_001.ogg");
            config.somDaSirene = TravessiaConstrutor.Som("Efeitos/sirene.wav");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        // ------------------------------------------------------------------ prefabs (Assets/Prefabs/Art/Obras)

        // Não há modelo dessas estruturas em Assets/Art: são formas simples da paleta.
        // Em todas, o caminho corre em Z (de -Z pra +Z) e o obstáculo ocupa z de -4,5 a 4,5.
        static Transform NovaEstrutura(string id, out GameObject raiz)
        {
            raiz = F.Grupo("Estrutura_" + id);
            var pecas = F.Grupo("Pecas", raiz.transform).transform;
            var estrutura = raiz.AddComponent<ObraEstrutura>();
            estrutura.idDoVerbete = id;
            estrutura.pecas = pecas;
            return pecas;
        }

        static void SalvarEstrutura(GameObject raiz, params Vector3[] caminho)
        {
            var pai = F.Grupo("Caminho", raiz.transform).transform;
            raiz.GetComponent<ObraEstrutura>().caminho = caminho.Select((p, i) => F.Grupo("Ponto_" + (i + 1), pai, p).transform).ToArray();
            F.Salvar(raiz, Categoria);
        }

        // Rampa reta entre dois pontos (z, y) do perfil da estrutura.
        static void Rampa(Transform pai, string nome, string mat, float largura, float espessura, float z0, float y0, float z1, float y1)
        {
            float dz = z1 - z0, dy = y1 - y0;
            float comprimento = Mathf.Sqrt(dz * dz + dy * dy);
            float angulo = -Mathf.Atan2(dy, dz) * Mathf.Rad2Deg;
            F.Cubo(pai, nome, mat, F.V(0, (y0 + y1) / 2f - espessura / 2f, (z0 + z1) / 2f), F.V(largura, espessura, comprimento + .15f), F.V(angulo, 0, 0));
        }

        static void GerarPrefabs()
        {
            GameObject raiz;
            Transform p, g;

            // ---- ponte: no desenho da Ponte JK, de Brasília (três arcos brancos cruzando o tabuleiro).
            p = NovaEstrutura("ponte", out raiz);
            MontarPonteJK(p, 10.6f, 4.6f);
            SalvarEstrutura(raiz, F.V(0, 0, -7f), F.V(0, .74f, -5.3f), F.V(0, .74f, 5.3f), F.V(0, 0, 7f));
            // A mesma ponte, só de enfeite (sem o script da estrutura), pra área das Obras no Hub.
            raiz = F.Grupo("Ponte_JK");
            MontarPonteJK(raiz.transform, 10.6f, 4.6f);
            F.Salvar(raiz, Categoria);

            // ---- túnel: dois portais com boca em arco (aduelas, zebrado, luminárias, muros de ala, grama em cima)
            // e a pista escura por dentro, com faixa amarela no meio.
            p = NovaEstrutura("tunel", out raiz);
            g = F.Grupo("Piso", p).transform;
            F.Cubo(g, "Pista", "Asfalto_Escuro", F.V(0, .03f, 0), F.V(4.6f, .04f, 10.6f));
            for (float z = -4.5f; z <= 4.6f; z += 1.5f)
                SemSombra(F.Cubo(g, "Traco", "Mostarda", F.V(0, .055f, z), F.V(.12f, .01f, .8f)));
            foreach (float s in new[] { -1f, 1f })
                ArteDetalhes.BocaDeTunel(p, s < 0 ? "Portal_Sul" : "Portal_Norte", F.V(0, 0, s * 5.15f), s < 0 ? 0f : 180f, 4.6f, 3.9f, 2.4f);
            SalvarEstrutura(raiz, F.V(0, 0, -6f), F.V(0, 0, 6f));

            // ---- viaduto: via elevada, larga, com rampas compridas.
            // Pilares redondos com travessa, laje com viga de borda, pista pintada, mureta de concreto
            // (também nas rampas), muro de aterro embaixo das rampas e postes de luz.
            p = NovaEstrutura("viaduto", out raiz);
            g = F.Grupo("Pilares", p).transform;
            foreach (float z in new[] { -4.3f, 0f, 4.3f })
            {
                foreach (float x in new[] { -1.5f, 1.5f })
                {
                    F.Cilindro(g, "Pilar", "Branco_Concreto", F.V(x, 1.4f, z), F.V(.7f, 1.4f, .7f));
                    F.Cilindro(g, "Base_Do_Pilar", "Cinza", F.V(x, .1f, z), F.V(1f, .1f, 1f));
                }
                F.Cubo(g, "Travessa", "Cinza", F.V(0, 2.65f, z), F.V(4.4f, .4f, .9f));
            }
            g = F.Grupo("Tabuleiro", p).transform;
            F.Cubo(g, "Laje", "Branco_Concreto", F.V(0, 3f, 0), F.V(4.8f, .35f, 9.7f));
            foreach (float x in new[] { -2.3f, 2.3f })
                F.Cubo(g, "Viga_De_Borda", "Cinza", F.V(x, 2.72f, 0), F.V(.2f, .3f, 9.7f));
            F.Cubo(g, "Pista", "Asfalto", F.V(0, 3.19f, 0), F.V(4.1f, .03f, 9.7f));
            foreach (float x in new[] { -1.85f, 1.85f })
                SemSombra(F.Cubo(g, "Linha_Da_Borda", "Branco", F.V(x, 3.215f, 0), F.V(.1f, .01f, 9.7f)));
            for (float z = -4.2f; z <= 4.3f; z += 1.4f)
                SemSombra(F.Cubo(g, "Traco_Do_Meio", "Mostarda", F.V(0, 3.215f, z), F.V(.12f, .01f, .8f)));
            foreach (int lado in new[] { -1, 1 })
            {
                g = F.Grupo(lado < 0 ? "Rampa_Sul" : "Rampa_Norte", p).transform;
                float z0 = lado < 0 ? -10f : 4.8f, y0 = lado < 0 ? 0f : 3.2f, z1 = lado < 0 ? -4.8f : 10f, y1 = lado < 0 ? 3.2f : 0f;
                Rampa(g, "Pista", "Asfalto", 4.8f, .3f, z0, y0, z1, y1);
                // Muro de aterro: degraus de concreto embaixo da rampa, pros lados não ficarem ocos.
                for (int i = 0; i < 4; i++)
                {
                    float k = (i + .5f) / 4f, zz = Mathf.Lerp(z0, z1, k), yy = Mathf.Lerp(y0, y1, k);
                    F.Caixa(g, "Aterro", "Cinza_Claro", 0, 0, zz, 4.7f, Mathf.Max(.05f, yy - .45f), (z1 - z0) / 4f);
                }
            }
            g = F.Grupo("Guarda_Corpos", p).transform;
            foreach (float x in new[] { -2.25f, 2.25f })
            {
                F.Cubo(g, "Mureta", "Cinza_Claro", F.V(x, 3.45f, 0), F.V(.3f, .5f, 9.7f));
                F.Cubo(g, "Mureta_Topo", "Branco", F.V(x, 3.73f, 0), F.V(.18f, .08f, 9.7f));
                foreach (int lado in new[] { -1, 1 })
                {
                    float z0 = lado < 0 ? -10f : 4.8f, y0 = lado < 0 ? 0f : 3.2f, z1 = lado < 0 ? -4.8f : 10f, y1 = lado < 0 ? 3.2f : 0f;
                    float dz = z1 - z0, dy = y1 - y0, comp = Mathf.Sqrt(dz * dz + dy * dy);
                    F.Cubo(g, "Mureta_Da_Rampa", "Cinza_Claro", F.V(x, (y0 + y1) / 2f + .25f, (z0 + z1) / 2f), F.V(.3f, .5f, comp), F.V(-Mathf.Atan2(dy, dz) * Mathf.Rad2Deg, 0, 0));
                }
            }
            g = F.Grupo("Postes", p).transform;
            foreach (float z in new[] { -3.2f, 3.2f })
                foreach (int lado in new[] { -1, 1 })
                {
                    float x = lado * 2.25f;
                    F.Cubo(g, "Poste", "Cinza_Escuro", F.V(x, 4.9f, z), F.V(.12f, 2.4f, .12f));
                    F.Cubo(g, "Braco", "Cinza_Escuro", F.V(x - lado * .4f, 6.05f, z), F.V(.8f, .08f, .08f));
                    F.Cubo(g, "Luminaria", "Luz_Amarela", F.V(x - lado * .78f, 5.97f, z), F.V(.4f, .1f, .22f));
                }
            SalvarEstrutura(raiz, F.V(0, 0, -10f), F.V(0, 3.22f, -4.8f), F.V(0, 3.22f, 4.8f), F.V(0, 0, 10f));

            // ---- passarela: estreita, alta, de pedestre, com corrimão.
            // Pilares em "Y" com sapata, piso com faixas antiderrapantes, painéis coloridos nas laterais,
            // corrimão também nas rampas, patamar no pé de cada rampa e luminárias.
            p = NovaEstrutura("passarela", out raiz);
            g = F.Grupo("Pilares", p).transform;
            foreach (float z in new[] { -4.3f, 4.3f })
            {
                F.Cilindro(g, "Pilar", "Branco_Concreto", F.V(0, 1.55f, z), F.V(.5f, 1.55f, .5f));
                F.Cubo(g, "Sapata", "Cinza", F.V(0, .1f, z), F.V(.9f, .2f, .9f));
                foreach (float x in new[] { -1f, 1f })
                    F.Barra(g, "Braco_Do_Y", "Branco_Concreto", F.V(0, 2.6f, z), F.V(x * .8f, 3.3f, z), .22f);
            }
            g = F.Grupo("Piso", p).transform;
            F.Cubo(g, "Laje", "Petroleo_Claro", F.V(0, 3.4f, 0), F.V(2f, .2f, 9.5f));
            for (float z = -4.5f; z <= 4.6f; z += .6f)
                SemSombra(F.Cubo(g, "Antiderrapante", "Petroleo", F.V(0, 3.505f, z), F.V(1.7f, .01f, .12f)));
            foreach (int lado in new[] { -1, 1 })
            {
                g = F.Grupo(lado < 0 ? "Rampa_Sul" : "Rampa_Norte", p).transform;
                float z0 = lado < 0 ? -10f : 4.7f, y0 = lado < 0 ? 0f : 3.5f, z1 = lado < 0 ? -4.7f : 10f, y1 = lado < 0 ? 3.5f : 0f;
                Rampa(g, "Pista", "Petroleo_Claro", 2f, .18f, z0, y0, z1, y1);
                F.Caixa(g, "Patamar", "Cinza_Claro", 0, -.02f, lado * 10.6f, 2.6f, .08f, 1.4f);
                F.Cilindro(g, "Apoio", "Branco_Concreto", F.V(0, .9f, lado * 6.9f), F.V(.3f, .9f, .3f));
            }
            g = F.Grupo("Corrimaos", p).transform;
            foreach (float x in new[] { -.95f, .95f })
            {
                F.Cubo(g, "Corrimao", "Mostarda", F.V(x, 4.45f, 0), F.V(.08f, .08f, 9.5f));
                for (int i = 0; i < 7; i++)
                    F.Cubo(g, "Balaustre", "Mostarda", F.V(x, 4f, -4.68f + i * 1.56f), F.V(.08f, .9f, .08f));
                for (int i = 0; i < 6; i++)
                    F.Cubo(g, "Painel", i % 2 == 0 ? "Laranja" : "Petroleo", F.V(x, 3.85f, -3.9f + i * 1.56f), F.V(.04f, .55f, 1.4f));
                foreach (int lado in new[] { -1, 1 })
                {
                    float z0 = lado < 0 ? -10f : 4.7f, y0 = lado < 0 ? 0f : 3.5f, z1 = lado < 0 ? -4.7f : 10f, y1 = lado < 0 ? 3.5f : 0f;
                    var a = F.V(x, y0 + .95f, z0); var b = F.V(x, y1 + .95f, z1);
                    F.Barra(g, "Corrimao_Da_Rampa", "Mostarda", a, b, .08f);
                    for (int i = 0; i <= 4; i++)
                    {
                        var q = Vector3.Lerp(a, b, i / 4f);
                        F.Cubo(g, "Balaustre", "Mostarda", q - Vector3.up * .45f, F.V(.07f, .9f, .07f));
                    }
                }
            }
            g = F.Grupo("Luzes", p).transform;
            foreach (float z in new[] { -2.3f, 2.3f })
            {
                F.Cubo(g, "Poste", "Cinza_Escuro", F.V(-.95f, 4.9f, z), F.V(.08f, .9f, .08f));
                F.Cubo(g, "Luminaria", "Luz_Amarela", F.V(-.75f, 5.35f, z), F.V(.4f, .08f, .2f));
            }
            SalvarEstrutura(raiz, F.V(0, 0, -10f), F.V(0, 3.52f, -4.7f), F.V(0, 3.52f, 4.7f), F.V(0, 0, 10f));

            // ---- passagem subterrânea: duas entradas com escada que desce; o caminho vai por baixo.
            p = NovaEstrutura("passagem_subterranea", out raiz);
            foreach (float s in new[] { -1f, 1f })
            {
                g = F.Grupo(s < 0 ? "Entrada_Sul" : "Entrada_Norte", p).transform;
                F.Cubo(g, "Boca", "Grafite", F.V(0, .1f, s * 6.3f), F.V(2.4f, .04f, 3.4f));
                foreach (float x in new[] { -1.3f, 1.3f })
                {
                    F.Cubo(g, "Mureta", "Branco_Concreto", F.V(x, .45f, s * 6.3f), F.V(.2f, .9f, 3.4f));
                    F.Cubo(g, "Poste", "Cinza_Escuro", F.V(x, 1.6f, s * 4.6f), F.V(.12f, 1.5f, .12f));
                    F.Cubo(g, "Poste", "Cinza_Escuro", F.V(x, 1.6f, s * 6f), F.V(.12f, 1.5f, .12f));
                }
                F.Cubo(g, "Mureta_Do_Fundo", "Branco_Concreto", F.V(0, .45f, s * 4.6f), F.V(2.8f, .9f, .2f));
                F.Cubo(g, "Cobertura", "Petroleo", F.V(0, 2.4f, s * 5.3f), F.V(3.2f, .14f, 2f));
            }
            SalvarEstrutura(raiz, F.V(0, 0, -8.2f), F.V(0, -2.6f, -5f), F.V(0, -2.6f, 5f), F.V(0, 0, 8.2f));

            // ---- faixa de pedestre: listras brancas pintadas na via.
            p = NovaEstrutura("faixa_de_pedestre", out raiz);
            for (int i = 0; i < 8; i++)
                SemSombra(F.Cubo(p, "Listra_" + (i + 1), "Branco", F.V(0, .04f, -3.15f + i * .9f), F.V(3.4f, .03f, .5f)));
            raiz.GetComponent<ObraEstrutura>().profundidade = 1.5f;
            SalvarEstrutura(raiz, F.V(0, 0, -5f), F.V(0, 0, 5f));

            // ---- lote: os dois lados do caminho (o meio é do obstáculo). 20 m de largura, 26 de comprimento.
            raiz = F.Grupo("Lote");
            var lote = raiz.transform;
            foreach (float s in new[] { -1f, 1f })
                SemSombra(F.Caixa(lote, s < 0 ? "Chao_Sul" : "Chao_Norte", "Verde_Claro", 0, -.1f, s * 8.75f, 20f, .1f, 8.5f));
            var caminhoDeVeiculo = F.Grupo("Caminho_Veiculo", lote).transform;
            var caminhoDePedestre = F.Grupo("Caminho_Pedestre", lote).transform;
            foreach (float s in new[] { -1f, 1f })
            {
                SemSombra(F.Caixa(caminhoDeVeiculo, "Asfalto", "Asfalto", 0, -.09f, s * 8.75f, 4.6f, .1f, 8.5f));
                foreach (float z in new[] { 6.2f, 8.8f, 11.4f })
                    SemSombra(F.Cubo(caminhoDeVeiculo, "Traco", "Branco", F.V(0, .016f, s * z), F.V(.14f, .012f, 1.2f)));
                SemSombra(F.Caixa(caminhoDePedestre, "Calcada", "Cinza_Claro", 0, -.02f, s * 8.75f, 2.4f, .1f, 8.5f));
            }
            // Coisas altas só do lado de lá (norte): do lado de cá tapariam a visão da câmera.
            // Ficam num grupo à parte: no modo Planejar a rua cada situação traz o seu próprio cenário.
            var enfeites = F.Grupo("Enfeites", lote).transform;
            P(enfeites, "Natureza/Arvore_Comum_1", F.V(-6.8f, 0, 9.8f), 30, .8f);
            P(enfeites, "Predios/Casa_02", F.V(6.4f, 0, 10f), 180, .8f);
            P(enfeites, "Natureza/Flores", F.V(-5f, 0, -8f), 0, 1.5f);
            P(enfeites, "Natureza/Arbusto_1", F.V(6f, 0, -9.2f), 0, .8f);
            F.Salvar(raiz, Categoria);

            // ---- obstáculo: rio.
            raiz = F.Grupo("Obstaculo_Rio");
            SemSombra(F.Caixa(raiz.transform, "Agua", "Agua", 0, -.12f, 0, 20f, .1f, 9f));
            foreach (float s in new[] { -1f, 1f })
                SemSombra(F.Caixa(raiz.transform, "Margem", "Areia", 0, -.1f, s * 4.3f, 20f, .12f, .5f));
            P(raiz.transform, "Natureza/Pedra_1", F.V(-7.4f, 0, 4f), 20, 1.6f);
            P(raiz.transform, "Natureza/Pedra_Musgo", F.V(6.6f, 0, -4f), 80, 1.5f);
            P(raiz.transform, "Natureza/Vitoria_Regia", F.V(-5f, -.01f, 1f), 30, 1.4f);
            P(raiz.transform, "Natureza/Vitoria_Regia", F.V(6f, -.01f, -1.4f), 120, 1.2f);
            F.Salvar(raiz, Categoria);

            // ---- obstáculo: morro.
            raiz = F.Grupo("Obstaculo_Morro");
            SemSombra(F.Caixa(raiz.transform, "Chao", "Verde_Claro", 0, -.1f, 0, 20f, .1f, 9f));
            F.Esfera(raiz.transform, "Morro", "Verde_Escuro", F.V(0, 0, 0), F.V(18.4f, 11f, 10.5f));
            F.Esfera(raiz.transform, "Morro_Esquerdo", "Verde", F.V(-6.8f, 0, .2f), F.V(6.2f, 7f, 9.6f));
            F.Esfera(raiz.transform, "Morro_Direito", "Verde_Musgo", F.V(6.8f, 0, -.2f), F.V(6.2f, 6f, 9.6f));
            P(raiz.transform, "Natureza/Pedra_2", F.V(-8.8f, 0, -4.2f), 0, 1.8f);
            P(raiz.transform, "Natureza/Pedra_3", F.V(8.6f, 0, 4.2f), 60, 1.6f);
            // Árvores e pedras no morro, longe do meio (por onde o túnel passa).
            ArteDetalhes.EnfeitarMorro(raiz.transform, "Mato_Do_Morro", F.V(0, 0, 0), F.V(18.4f, 11f, 10.5f), 9, 7, new Rect(-3.6f, -6f, 7.2f, 12f));
            ArteDetalhes.EnfeitarMorro(raiz.transform, "Mato_Do_Morro_Atras", F.V(0, 0, 0), F.V(18.4f, 11f, -10.5f), 6, 8, new Rect(-3.6f, -6f, 7.2f, 12f));
            F.Salvar(raiz, Categoria);

            // ---- obstáculo: avenida com carros (a via corre em X).
            raiz = Via("Obstaculo_Avenida", 8.4f, .3f, 4f, 2f,
                new[] { "Carro_Hatch", "Carro_SUV", "Carro_Compacto_Azul", "Carro_Sedan_Azul" }, new[] { -7f, 3f, -2f, 8f });
            for (float x = -9f; x <= 9f; x += 3f)
                SemSombra(F.Cubo(raiz.transform, "Traco", "Branco", F.V(x, .016f, 0), F.V(1.4f, .012f, .16f)));
            F.Salvar(raiz, Categoria);

            // ---- obstáculo: rua comum (mais estreita, com calçada, poucos carros e devagar).
            raiz = Via("Obstaculo_Rua", 6.4f, 1.3f, 2.6f, 1.5f, new[] { "Carro_Cupe_Branco", "Carro_Hatch" }, new[] { -6f, 5f });
            F.Salvar(raiz, Categoria);

            // ---- local destacado: sinal amarelo que sobe e desce em cima do obstáculo.
            raiz = F.Grupo("Local_Da_Obra");
            var local = raiz.AddComponent<ObraLocal>();
            var toque = raiz.AddComponent<BoxCollider>();
            toque.center = F.V(0, 3.5f, 0);
            toque.size = F.V(14f, 8f, 11f);
            SemSombra(F.Cilindro(raiz.transform, "Anel", "Mostarda_Clara", F.V(0, .06f, 0), F.V(6.4f, .04f, 6.4f)));
            var sinal = F.Grupo("Sinal", raiz.transform, F.V(0, 8f, 0)).transform;
            F.Esfera(sinal, "Bola", "Luz_Amarela", Vector3.zero, F.V(1.7f, 1.7f, 1.7f));
            F.Cubo(sinal, "Ponta", "Mostarda", F.V(0, -1.3f, 0), F.V(.75f, .75f, .75f), F.V(45, 0, 45));
            local.sinal = sinal;
            F.Salvar(raiz, Categoria);

            // ---- hospital (a origem fica onde a rua da ambulância acaba).
            raiz = F.Grupo("Hospital");
            SemSombra(F.Caixa(raiz.transform, "Patio", "Cinza_Claro", 0, -.02f, 1f, 14f, .1f, 6f));
            P(raiz.transform, "Predios/Predio_KayKit_A", F.V(0, 0, 8f), 180);
            F.Cubo(raiz.transform, "Placa", "Branco", F.V(0, 7.9f, 3.9f), F.V(2.8f, 2.8f, .2f));
            F.Cubo(raiz.transform, "Cruz_Horizontal", "Vermelho", F.V(0, 7.9f, 3.76f), F.V(1.9f, .55f, .1f));
            F.Cubo(raiz.transform, "Cruz_Vertical", "Vermelho", F.V(0, 7.9f, 3.76f), F.V(.55f, 1.9f, .1f));
            P(raiz.transform, "Personagens/Medica", F.V(2.6f, .08f, 2.6f), 200);
            F.Salvar(raiz, Categoria);
        }

        // Ponte no desenho da Ponte JK (Brasília): tabuleiro baixo e três arcos brancos que cruzam a pista em
        // diagonal, cada um nascendo de um lado e caindo do outro, com cabos descendo do arco até as bordas.
        // Não há modelo dela em Assets/Art: é feita de formas da paleta. O tabuleiro corre em Z, centrado na origem.
        static void MontarPonteJK(Transform p, float comprimento, float largura)
        {
            const float alturaDoArco = 3.5f, yDaPista = .74f;
            float meio = comprimento / 2f, vao = comprimento / 3f, pe = largura / 2f + .45f;

            var g = F.Grupo("Pilares", p).transform;
            foreach (float z in new[] { -vao / 2f, vao / 2f })
                F.Cubo(g, "Pilar", "Cinza", F.V(0, -1f, z), F.V(1.2f, 3f, .8f));
            // Um bloco no pé de cada arco, alternando os lados (4 pés pra 3 arcos).
            for (int i = 0; i <= 3; i++)
                foreach (float lado in i == 0 || i == 3 ? new[] { i == 0 ? -1f : 1f } : new[] { -1f, 1f })
                    F.Cubo(g, "Bloco_Do_Arco", "Branco_Concreto", F.V(lado * pe, -.9f, -meio + i * vao), F.V(1f, 2.7f, 1f));

            g = F.Grupo("Tabuleiro", p).transform;
            F.Cubo(g, "Laje", "Branco_Concreto", F.V(0, .55f, 0), F.V(largura, .3f, comprimento));
            F.Cubo(g, "Pista", "Asfalto", F.V(0, .715f, 0), F.V(largura - .8f, .04f, comprimento));
            foreach (float x in new[] { -1f, 1f })
                F.Cubo(g, "Guarda_Corpo", "Branco", F.V(x * (largura / 2f - .12f), .95f, 0), F.V(.1f, .32f, comprimento));
            Rampa(p, "Rampa_Sul", "Asfalto", largura, .14f, -meio - 1.7f, 0f, -meio, yDaPista);
            Rampa(p, "Rampa_Norte", "Asfalto", largura, .14f, meio, yDaPista, meio + 1.7f, 0f);

            for (int i = 0; i < 3; i++)
            {
                g = F.Grupo("Arco_" + (i + 1), p).transform;
                float z0 = -meio + i * vao, z1 = z0 + vao;
                float x0 = (i % 2 == 0 ? -1f : 1f) * pe;
                Vector3 Ponto(float t) { return F.V(Mathf.Lerp(x0, -x0, t), .3f + 4f * alturaDoArco * t * (1f - t), Mathf.Lerp(z0, z1, t)); }
                const int trechos = 14;
                for (int k = 0; k < trechos; k++)
                {
                    F.Barra(g, "Trecho", "Branco", Ponto(k / (float)trechos), Ponto((k + 1) / (float)trechos), .4f);
                    if (k > 0) F.Esfera(g, "Junta", "Branco", Ponto(k / (float)trechos), F.V(.4f, .4f, .4f));
                }
                // Cabos: de cada ponto do arco até as duas bordas da pista.
                for (int k = 1; k <= 5; k++)
                {
                    Vector3 alto = Ponto(k / 6f);
                    foreach (float x in new[] { -1f, 1f })
                    {
                        var cabo = F.Barra(g, "Cabo", "Cinza_Claro", alto, F.V(x * (largura / 2f - .15f), yDaPista + .1f, alto.z), .06f);
                        cabo.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
            }
        }

        // Via que corta o lote em X: asfalto, bordas (meio-fio ou calçada) e carros andando nas duas mãos.
        static GameObject Via(string nome, float largura, float borda, float velocidade, float zDaPista, string[] carros, float[] xs)
        {
            var raiz = F.Grupo(nome);
            SemSombra(F.Caixa(raiz.transform, "Asfalto", "Asfalto", 0, -.09f, 0, 20f, .1f, largura));
            foreach (float s in new[] { -1f, 1f })
                SemSombra(F.Caixa(raiz.transform, "Borda", "Cinza_Claro", 0, -.02f, s * (largura / 2f + borda / 2f), 20f, .1f, borda));
            // O resto da faixa de 9 m é grama.
            float sobra = (9f - largura - borda * 2f) / 2f;
            if (sobra > .05f)
                foreach (float s in new[] { -1f, 1f })
                    SemSombra(F.Caixa(raiz.transform, "Grama", "Verde_Claro", 0, -.1f, s * (4.5f - sobra / 2f), 20f, .1f, sobra));

            var transito = raiz.AddComponent<ObraTransito>();
            var grupo = F.Grupo("Carros", raiz.transform).transform;
            var lista = new List<Transform>();
            var sentidos = new List<float>();
            for (int i = 0; i < carros.Length; i++)
            {
                // Metade vai pra +X pela pista de cá (z negativo), metade volta pela de lá.
                float sentido = i < (carros.Length + 1) / 2 ? 1f : -1f;
                lista.Add(P(grupo, "Carros/" + carros[i], F.V(xs[i], .01f, -sentido * zDaPista), sentido * 90f).transform);
                sentidos.Add(sentido);
            }
            transito.carros = lista.ToArray();
            transito.sentidos = sentidos.ToArray();
            transito.velocidade = velocidade;
            return raiz;
        }

        // ------------------------------------------------------------------ prefabs do modo "Planejar a rua"

        // Chão da faixa do meio do lote (z de -4,5 a 4,5), que nas pontes é do obstáculo.
        static void Meio(Transform raiz, string mat)
        {
            SemSombra(F.Caixa(raiz, "Chao_Do_Meio", mat, 0, -.1f, 0, 20f, .1f, 9f));
        }

        // Carros andando ao longo do lote (em Z): o ObraTransito anda em X, então o grupo fica girado.
        // Mão direita: quem vai pra +Z anda do lado +X.
        static void TransitoAoLongo(Transform pai, float x, float zDaPista, float velocidade, string[] carros, float[] posicoes, float[] sentidos)
        {
            var grupo = F.Grupo("Transito", pai, F.V(x, 0, 0), F.V(0, -90, 0));
            var transito = grupo.AddComponent<ObraTransito>();
            var lista = new List<Transform>();
            for (int i = 0; i < carros.Length; i++)
                lista.Add(P(grupo.transform, "Carros/" + carros[i], F.V(posicoes[i], .01f, -sentidos[i] * zDaPista), sentidos[i] * 90f).transform);
            transito.carros = lista.ToArray();
            transito.sentidos = sentidos;
            transito.velocidade = velocidade;
            transito.meiaLargura = 13f;
        }

        static void SalvarEstruturaCompleta(GameObject raiz, Vector3[] vagas, params Vector3[] caminho)
        {
            var estrutura = raiz.GetComponent<ObraEstrutura>();
            estrutura.caminhoCompleto = true;
            if (vagas != null)
            {
                var pai = F.Grupo("Vagas", raiz.transform).transform;
                estrutura.vagas = vagas.Select((v, i) => F.Grupo("Vaga_" + (i + 1), pai, v).transform).ToArray();
            }
            SalvarEstrutura(raiz, caminho);
        }

        static void Tracejado(Transform pai, string nome, float x, float y, float zInicial, float zFinal, float passo, float largura, float comprimento)
        {
            var g = F.Grupo(nome, pai).transform;
            for (float z = zInicial; z <= zFinal + .01f; z += passo)
                SemSombra(F.Cubo(g, "Traco", "Branco", F.V(x, y, z), F.V(largura, .012f, comprimento)));
        }

        // As peças de via e o que se acrescenta numa rua pronta. Não há modelo de nenhuma em Assets/Art: são
        // formas da paleta. A APARÊNCIA de cada uma (terra na via rural, poste e meio-fio na via urbana...) é
        // escolha do jogo pra criança distinguir as peças; as definições não descrevem como elas são.
        static void GerarPrefabsDeRuas()
        {
            GameObject raiz;
            Transform p, g;

            // ---- via (peça geral): só o asfalto com a linha do meio.
            p = NovaEstrutura("via", out raiz);
            SemSombra(F.Cubo(p, "Asfalto", "Asfalto", F.V(0, -.04f, 0), F.V(4.6f, .1f, 9.2f)));
            Tracejado(p, "Tracejado", 0, .016f, -3f, 3f, 3f, .14f, 1.2f);
            SalvarEstrutura(raiz, F.V(0, 0, -5f), F.V(0, 0, 5f));

            // ---- via rural: estrada de terra com cerca de madeira dos dois lados.
            p = NovaEstrutura("via_rural", out raiz);
            SemSombra(F.Cubo(p, "Terra", "Marrom_Claro", F.V(0, -.04f, 0), F.V(4.6f, .1f, 9.2f)));
            g = F.Grupo("Trilhas", p).transform;
            foreach (float x in new[] { -.9f, .9f })
                SemSombra(F.Cubo(g, "Trilha", "Marrom", F.V(x, .014f, 0), F.V(.5f, .012f, 9.2f)));
            foreach (float lado in new[] { -1f, 1f })
            {
                g = F.Grupo(lado < 0 ? "Cerca_Esquerda" : "Cerca_Direita", p).transform;
                for (float z = -4f; z <= 4f; z += 2f)
                    F.Cubo(g, "Mourao", "Marrom_Escuro", F.V(lado * 2.7f, .5f, z), F.V(.16f, 1f, .16f));
                foreach (float y in new[] { .45f, .8f })
                    F.Cubo(g, "Tabua", "Marrom_Escuro", F.V(lado * 2.7f, y, 0), F.V(.08f, .1f, 8.4f));
            }
            SalvarEstrutura(raiz, F.V(0, 0, -5f), F.V(0, 0, 5f));

            // ---- via urbana: asfalto com meio-fio e postes de luz.
            p = NovaEstrutura("via_urbana", out raiz);
            SemSombra(F.Cubo(p, "Asfalto", "Asfalto", F.V(0, -.04f, 0), F.V(4.6f, .1f, 9.2f)));
            g = F.Grupo("Meio_Fios", p).transform;
            foreach (float x in new[] { -2.6f, 2.6f })
                F.Cubo(g, "Meio_Fio", "Cinza_Claro", F.V(x, .06f, 0), F.V(.6f, .2f, 9.2f));
            Tracejado(p, "Tracejado", 0, .016f, -3f, 3f, 3f, .14f, 1.2f);
            g = F.Grupo("Postes", p).transform;
            P(g, "Props/PosteKK", F.V(-2.7f, .16f, -2.6f), 0);
            P(g, "Props/PosteKK", F.V(2.7f, .16f, 2.6f), 180);
            SalvarEstrutura(raiz, F.V(0, 0, -5f), F.V(0, 0, 5f));

            // ---- calçada: faixa elevada, com meio-fio, na beira da rua (o lote inteiro).
            p = NovaEstrutura("calcada", out raiz);
            F.Cubo(p, "Piso", "Cinza_Claro", F.V(0, .08f, 0), F.V(1.9f, .16f, 26f));
            F.Cubo(p, "Meio_Fio", "Branco_Concreto", F.V(-.95f, .09f, 0), F.V(.18f, .19f, 26f));
            g = F.Grupo("Juntas", p).transform;
            for (float z = -12f; z <= 12f; z += 2f)
                SemSombra(F.Cubo(g, "Junta", "Cinza", F.V(.05f, .164f, z), F.V(1.75f, .012f, .06f)));
            SalvarEstruturaCompleta(raiz, null, F.V(0, .16f, -10.4f), F.V(0, .16f, 12.2f));

            // ---- ciclofaixa: PARTE DA VIA, no mesmo nível do asfalto e colada nele, pintada de vermelho,
            // separada dos carros só por uma linha branca com tachões.
            p = NovaEstrutura("ciclofaixa", out raiz);
            SemSombra(F.Cubo(p, "Asfalto", "Asfalto", F.V(0, -.04f, 0), F.V(1.9f, .1f, 26f)));
            SemSombra(F.Cubo(p, "Pintura_Vermelha", "Vermelho", F.V(.1f, .018f, 0), F.V(1.5f, .016f, 26f)));
            SemSombra(F.Cubo(p, "Linha", "Branco", F.V(-.78f, .02f, 0), F.V(.14f, .02f, 26f)));
            g = F.Grupo("Tachoes", p).transform;
            for (float z = -12f; z <= 12f; z += 2f)
                F.Cubo(g, "Tachao", "Mostarda", F.V(-.78f, .06f, z), F.V(.16f, .08f, .34f));
            Tracejado(p, "Marcas_De_Bicicleta", .1f, .03f, -9f, 9f, 6f, .5f, 1f);
            raiz.GetComponent<ObraEstrutura>().profundidade = 2f;
            SalvarEstruturaCompleta(raiz, null, F.V(.1f, .03f, -10.4f), F.V(.1f, .03f, 12.2f));

            // ---- ciclovia: VIA PRÓPRIA, separada: pista de outra cor entre dois canteiros com arbustos e placa.
            p = NovaEstrutura("ciclovia", out raiz);
            SemSombra(F.Cubo(p, "Pista", "Terracota", F.V(0, .03f, 0), F.V(2.2f, .06f, 26f)));
            g = F.Grupo("Canteiros", p).transform;
            foreach (float x in new[] { -1.5f, 1.5f })
                F.Cubo(g, "Canteiro", "Verde", F.V(x, .07f, 0), F.V(.8f, .14f, 26f));
            Tracejado(p, "Tracejado", 0, .066f, -11.7f, 11.7f, 2.6f, .1f, .9f);
            g = F.Grupo("Arbustos", p).transform;
            foreach (float x in new[] { -1.5f, 1.5f })
                for (float z = -9f; z <= 9f; z += 6f)
                    P(g, "Natureza/Arbusto_2", F.V(x, .2f, z + (x > 0 ? 3f : 0f)), 0, .65f);
            P(p, "Props/Placa_Ciclovia", F.V(1.5f, .14f, -11.6f), 180);
            SalvarEstruturaCompleta(raiz, null, F.V(0, .06f, -10.4f), F.V(0, .06f, 12.2f));

            // ---- retorno: abertura no canteiro do meio, com a seta em "U" pintada e uma placa.
            p = NovaEstrutura("retorno", out raiz);
            SemSombra(F.Cubo(p, "Abertura", "Asfalto", F.V(0, .03f, 3.3f), F.V(2.2f, .26f, 4.6f)));
            g = F.Grupo("Seta", p).transform;
            SemSombra(F.Cubo(g, "Sobe", "Branco", F.V(1.9f, .02f, 2.4f), F.V(.26f, .014f, 2f)));
            SemSombra(F.Cubo(g, "Vira", "Branco", F.V(0, .168f, 3.9f), F.V(4.06f, .014f, .26f)));
            SemSombra(F.Cubo(g, "Desce", "Branco", F.V(-1.9f, .02f, 2.6f), F.V(.26f, .014f, 2.4f)));
            SemSombra(F.Cubo(g, "Ponta", "Branco", F.V(-1.9f, .02f, 1.2f), F.V(.9f, .014f, .4f)));
            g = F.Grupo("Placa", p).transform;
            F.Cubo(g, "Poste", "Cinza_Escuro", F.V(0, 1.3f, 6.2f), F.V(.12f, 2.4f, .12f));
            F.Cubo(g, "Chapa", "Petroleo", F.V(0, 2.4f, 6.14f), F.V(1f, 1f, .08f));
            F.Cubo(g, "Seta_Da_Placa", "Branco", F.V(0, 2.5f, 6.09f), F.V(.6f, .14f, .03f));
            SalvarEstruturaCompleta(raiz, null, F.V(2.9f, 0, -10f), F.V(2.9f, 0, 1.2f), F.V(1.9f, 0, 3.3f), F.V(0, .16f, 3.9f),
                F.V(-1.9f, 0, 3.3f), F.V(-2.9f, 0, 1.2f), F.V(-2.9f, 0, -12.2f));

            // ---- estacionamento: pátio com vagas pintadas, uma pra cada veículo.
            p = NovaEstrutura("estacionamento", out raiz);
            SemSombra(F.Cubo(p, "Piso", "Asfalto", F.V(0, .02f, .4f), F.V(14.4f, .14f, 8.2f)));
            g = F.Grupo("Vagas_Pintadas", p).transform;
            for (float x = -6f; x <= 6f; x += 3f)
                SemSombra(F.Cubo(g, "Linha", "Branco", F.V(x, .096f, 1.6f), F.V(.14f, .012f, 4.6f)));
            SemSombra(F.Cubo(g, "Linha_Do_Fundo", "Branco", F.V(0, .096f, 3.9f), F.V(12.14f, .012f, .14f)));
            g = F.Grupo("Placa", p).transform;
            F.Cubo(g, "Poste", "Cinza_Escuro", F.V(-6.7f, 1.3f, -3.2f), F.V(.12f, 2.4f, .12f));
            F.Cubo(g, "Chapa", "Petroleo", F.V(-6.7f, 2.4f, -3.26f), F.V(1f, 1f, .08f));
            SalvarEstruturaCompleta(raiz, new[] { F.V(-4.5f, .09f, 1.4f), F.V(1.5f, .09f, 1.4f) },
                F.V(0, 0, -7.5f), F.V(-1.5f, .09f, -3.2f), F.V(-1.5f, .09f, 1.4f));

            // ---- ciclista: a bicicleta com o menino em cima (não há modelo de ciclista pedalando).
            raiz = F.Grupo("Ciclista");
            P(raiz.transform, "Carros/Bicicleta_Q2", Vector3.zero, 180);
            P(raiz.transform, "Personagens/Familia_FilhoMaisVelho_Animado", F.V(0, .42f, -.12f), 0, .85f);
            F.Salvar(raiz, Categoria);

            // ---------------- cenários: o que há em volta diz que lugar é aquele ----------------
            // Coisas altas só do lado de lá (norte) ou bem pros lados: a câmera olha de cá.

            // ---- fazenda de um lado, cidade do outro; no meio falta a ligação.
            raiz = F.Grupo("Situacao_" + ObstaculoDaObra.CampoECidade);
            Meio(raiz.transform, "Areia");
            g = F.Grupo("Fazenda", raiz.transform).transform;
            F.Cubo(g, "Celeiro", "Vermelho", F.V(-6.8f, 1.3f, -9.8f), F.V(4.4f, 2.6f, 3.6f));
            F.Cubo(g, "Celeiro_Telhado", "Marrom_Escuro", F.V(-6.8f, 2.85f, -9.8f), F.V(4.9f, .5f, 4.1f));
            F.Cubo(g, "Celeiro_Porta", "Branco", F.V(-4.58f, .95f, -9.8f), F.V(.06f, 1.9f, 1.5f));
            P(g, "Predios/Estufa", F.V(6.8f, 0, -10.2f), 0, .9f);
            P(g, "Props/Cerca_Pequena", F.V(6.6f, 0, -6.6f), 0, .8f);
            P(g, "Natureza/Grama_Alta", F.V(-4f, 0, -6.2f), 0, 1.4f);
            P(g, "Natureza/Grama_Alta_2", F.V(8.6f, 0, -7.6f), 0, 1.4f);
            g = F.Grupo("Cidade", raiz.transform).transform;
            P(g, "Predios/Predio_Comercial_Loja", F.V(-6.6f, 0, 10.2f), 180, .7f);
            P(g, "Predios/Padaria", F.V(6.6f, 0, 10.2f), 180, .75f);
            F.Salvar(raiz, Categoria);

            // ---- dentro da cidade: casas de cá, prédio e loja de lá.
            raiz = F.Grupo("Situacao_" + ObstaculoDaObra.DentroDaCidade);
            Meio(raiz.transform, "Areia");
            P(raiz.transform, "Predios/Casa_01", F.V(-6.8f, 0, -9.8f), 0, .8f);
            P(raiz.transform, "Predios/Casa_03", F.V(6.8f, 0, -9.8f), 0, .8f);
            P(raiz.transform, "Predios/Predio_KayKit_B", F.V(-6.6f, 0, 10f), 180, .75f);
            P(raiz.transform, "Predios/Padaria", F.V(6.6f, 0, 10.2f), 180, .75f);
            F.Salvar(raiz, Categoria);

            // ---- nem cidade nem fazenda: um lago de cá, a mata de lá.
            raiz = F.Grupo("Situacao_" + ObstaculoDaObra.LugarQualquer);
            Meio(raiz.transform, "Areia");
            P(raiz.transform, "Natureza/Lago", F.V(-6.2f, .01f, -9f), 0, .7f);
            P(raiz.transform, "Natureza/Pedra_1", F.V(6f, 0, -8.4f), 30, 1.6f);
            P(raiz.transform, "Natureza/Arvore_Pinheiro", F.V(-7f, 0, 9f), 0, .9f);
            P(raiz.transform, "Natureza/Arvore_Pinheiro", F.V(-4.4f, 0, 11.2f), 90, .8f);
            P(raiz.transform, "Natureza/Arvore_Pinheiro", F.V(6.4f, 0, 10f), 200, .9f);
            P(raiz.transform, "Natureza/Pedra_Musgo", F.V(8f, 0, 7.4f), 0, 1.5f);
            F.Salvar(raiz, Categoria);

            // ---- rua pronta, com carros nas duas mãos; a obra vai na beira direita.
            raiz = F.Grupo("Situacao_" + ObstaculoDaObra.BeiraDaRua);
            Meio(raiz.transform, "Verde_Claro");
            SemSombra(F.Caixa(raiz.transform, "Asfalto", "Asfalto", 0, -.09f, 0, 4.6f, .1f, 9f));
            Tracejado(raiz.transform, "Tracejado", 0, .016f, -3f, 3f, 3f, .14f, 1.2f);
            TransitoAoLongo(raiz.transform, 0, 1.15f, 3.2f, new[] { "Carro_Hatch", "Carro_SUV", "Carro_Sedan_Azul" }, new[] { -8f, 5f, 0f }, new[] { 1f, 1f, -1f });
            P(raiz.transform, "Predios/Casa_02", F.V(-7.4f, 0, 9.6f), 180, .8f);
            P(raiz.transform, "Natureza/Arvore_Comum_2", F.V(-6.6f, 0, -1f), 40, .7f);
            P(raiz.transform, "Natureza/Arbusto_1", F.V(-6f, 0, -9f), 0, .8f);
            F.Salvar(raiz, Categoria);

            // ---- parque: a rua dos carros fica lá longe, atrás das árvores.
            raiz = F.Grupo("Situacao_" + ObstaculoDaObra.SeparadoDoTransito);
            Meio(raiz.transform, "Verde_Claro");
            SemSombra(F.Caixa(raiz.transform, "Rua_Dos_Carros", "Asfalto", -8f, -.09f, 0, 3.8f, .105f, 26f));
            TransitoAoLongo(raiz.transform, -8f, .9f, 3.2f, new[] { "Carro_Taxi", "Carro_Hatch" }, new[] { -7f, 4f }, new[] { 1f, -1f });
            foreach (float z in new[] { -7f, 0f, 7f })
                P(raiz.transform, "Natureza/Arvore_Comum_1", F.V(-4.6f, 0, z), z * 20f, .6f);
            P(raiz.transform, "Natureza/Lago", F.V(6.6f, .01f, 3f), 90, .6f);
            P(raiz.transform, "Props/Banco_De_Praca", F.V(4f, 0, -5f), -90);
            P(raiz.transform, "Natureza/Flores", F.V(5f, 0, -9f), 0, 1.5f);
            P(raiz.transform, "Natureza/Arvore_Salgueiro", F.V(8f, 0, 10f), 0, .7f);
            F.Salvar(raiz, Categoria);

            // ---- avenida de duas pistas com canteiro no meio: a de cá sobe (+Z), a de lá desce.
            raiz = F.Grupo("Situacao_" + ObstaculoDaObra.MudarDeSentido);
            SemSombra(F.Caixa(raiz.transform, "Asfalto", "Asfalto", 0, -.09f, 0, 10.6f, .105f, 26f));
            foreach (float lado in new[] { -1f, 1f })
                SemSombra(F.Caixa(raiz.transform, "Grama", "Verde_Claro", lado * 7.65f, -.1f, 0, 4.7f, .1f, 9f));
            F.Caixa(raiz.transform, "Canteiro", "Verde", 0, 0, 0, 1.5f, .14f, 26f);
            foreach (float lado in new[] { -1f, 1f })
                F.Caixa(raiz.transform, "Meio_Fio", "Cinza_Claro", lado * .85f, 0, 0, .2f, .16f, 26f);
            g = F.Grupo("Setas_Das_Pistas", raiz.transform).transform;
            foreach (float z in new[] { -7f, 8.5f })
            {
                // Pista de cá: seta apontando pra +Z. Pista de lá: pra -Z.
                SemSombra(F.Cubo(g, "Seta", "Branco", F.V(2.9f, .02f, z), F.V(.22f, .012f, 1.8f)));
                SemSombra(F.Cubo(g, "Ponta", "Branco", F.V(2.9f, .02f, z + 1f), F.V(.8f, .012f, .36f)));
                SemSombra(F.Cubo(g, "Seta", "Branco", F.V(-2.9f, .02f, z), F.V(.22f, .012f, 1.8f)));
                SemSombra(F.Cubo(g, "Ponta", "Branco", F.V(-2.9f, .02f, z - 1f), F.V(.8f, .012f, .36f)));
            }
            P(raiz.transform, "Natureza/Arvore_Comum_3", F.V(-8f, 0, 9.6f), 0, .8f);
            P(raiz.transform, "Predios/Casa_05", F.V(8f, 0, 10f), 180, .8f);
            F.Salvar(raiz, Categoria);

            // ---- loja com um terreno vazio na frente e três carros esperando.
            raiz = F.Grupo("Situacao_" + ObstaculoDaObra.GuardarVeiculos);
            Meio(raiz.transform, "Areia");
            SemSombra(F.Caixa(raiz.transform, "Entrada", "Asfalto", 0, -.09f, -8.75f, 12f, .1f, 8.5f));
            g = F.Grupo("Figurantes", raiz.transform).transform;
            P(g, "Carros/Carro_Hatch", F.V(-3.4f, .01f, -11.4f));
            P(g, "Carros/Carro_SUV", F.V(3.4f, .01f, -11f));
            P(raiz.transform, "Predios/Predio_Comercial_Loja", F.V(0, 0, 10.8f), 180, .8f);
            P(raiz.transform, "Natureza/Arvore_Comum_1", F.V(-7.6f, 0, 9.4f), 0, .7f);
            P(raiz.transform, "Natureza/Arvore_Comum_2", F.V(7.6f, 0, 9.4f), 90, .7f);
            F.Salvar(raiz, Categoria);
        }

        // ------------------------------------------------------------------ interface

        // Duas abas no alto da seleção de níveis: "Pontes e Passagens" e "Planejar a rua".
        static Button[] Abas(Transform canvas, ObrasHUD hud)
        {
            var raiz = Tela("Abas_Dos_Modos", canvas);
            string[] nomes = { "Pontes e Passagens", "Planejar a rua" };
            var botoes = new Button[nomes.Length];
            var frentes = new Image[nomes.Length];
            for (int i = 0; i < nomes.Length; i++)
            {
                botoes[i] = Botao("Aba_" + (i + 1), raiz, nomes[i], "Branco", new Vector2((i - .5f) * 560f, -78), new Vector2(530, 116), 42);
                TravessiaConstrutor.Ancorar(botoes[i], new Vector2(.5f, 1));
                frentes[i] = botoes[i].transform.Find("Frente").GetComponent<Image>();
            }
            hud.abasDosModos = raiz.gameObject;
            hud.frentesDasAbas = frentes;
            hud.corDaAba = C("Branco");
            hud.corDaAbaEscolhida = C("Mostarda");
            return botoes;
        }

        static ObrasHUD Hud(Transform canvas, out Button botaoOuvir, out Button botaoDica)
        {
            var raiz = Tela("HUD", canvas);
            var hud = raiz.gameObject.AddComponent<ObrasHUD>();
            var voltarAoHub = raiz.gameObject.AddComponent<VoltarAoHub>();

            // O problema da vez (em cima, no meio): quem precisa atravessar o quê.
            var problema = Imagem("Painel_Do_Problema", raiz, arredondado, C("Branco", .96f), new Vector2(60, -100), new Vector2(1000, 160));
            TravessiaConstrutor.Ancorar(problema, new Vector2(.5f, 1));
            hud.textoDoContador = Texto("Contador", problema.transform, "", 28, C("Cinza_Escuro"), new Vector2(0, 52), new Vector2(600, 36), FontStyle.Normal);
            hud.textoDoEnunciado = Texto("Enunciado", problema.transform, "", 54, C("Petroleo"), new Vector2(0, -18), new Vector2(960, 90));
            hud.textoDoEnunciado.resizeTextForBestFit = true; hud.textoDoEnunciado.resizeTextMinSize = 34; hud.textoDoEnunciado.resizeTextMaxSize = 54;

            // Nível e os três critérios das estrelas (em cima, à esquerda).
            var cartao = Imagem("Painel_Do_Nivel", raiz, arredondado, C("Branco", .92f), new Vector2(240, -115), new Vector2(430, 200));
            TravessiaConstrutor.Ancorar(cartao, new Vector2(0, 1));
            hud.textoDoNivel = Texto("Nivel", cartao.transform, "", 30, C("Petroleo"), new Vector2(0, 66), new Vector2(410, 44));
            var estrelas = new Image[3];
            var rotulos = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                estrelas[i] = Imagem("Estrela_" + (i + 1), cartao.transform, S("figura_estrela"), Color.white, new Vector2(-135 + i * 135, 2), new Vector2(72, 72));
                rotulos[i] = Texto("Rotulo_" + (i + 1), cartao.transform, "", 30, C("Grafite"), new Vector2(-135 + i * 135, -62), new Vector2(140, 42), FontStyle.Normal);
                rotulos[i].resizeTextForBestFit = true; rotulos[i].resizeTextMinSize = 24; rotulos[i].resizeTextMaxSize = 30;
            }
            hud.estrelasDosCriterios = estrelas;
            hud.rotulosDosCriterios = rotulos;

            var hubBotao = Botao("Botao_VoltarAoHub", raiz, "", "Petroleo_Claro", new Vector2(-100, -95), new Vector2(120, 120), 0, S("ui_seta_voltar"), circulo);
            TravessiaConstrutor.Ancorar(hubBotao, new Vector2(1, 1));
            UnityEventTools.AddPersistentListener(hubBotao.onClick, voltarAoHub.Voltar);

            // Tempo (só no nível com relógio) e dica, logo abaixo do problema.
            var tempo = Imagem("Painel_Do_Tempo", raiz, arredondado, C("Petroleo_Escuro", .9f), new Vector2(60, -206), new Vector2(720, 40));
            TravessiaConstrutor.Ancorar(tempo, new Vector2(.5f, 1));
            var area = Ret("Area", tempo.transform, Vector2.zero, Vector2.zero);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(5, 5); area.offsetMax = new Vector2(-5, -5);
            var cheio = Imagem("Cheio", area, arredondado, C("Mostarda"), Vector2.zero, Vector2.zero);
            var rtCheio = (RectTransform)cheio.transform;
            rtCheio.anchorMin = Vector2.zero; rtCheio.anchorMax = Vector2.one; rtCheio.offsetMin = rtCheio.offsetMax = Vector2.zero;
            hud.painelDoTempo = tempo.gameObject;
            hud.barraDeTempo = rtCheio;

            var dica = Imagem("Painel_Da_Dica", raiz, arredondado, C("Mostarda_Clara", .97f), new Vector2(60, -272), new Vector2(1000, 72));
            TravessiaConstrutor.Ancorar(dica, new Vector2(.5f, 1));
            hud.painelDaDica = dica.gameObject;
            hud.textoDaDica = Texto("Texto", dica.transform, "", 34, C("Grafite"), Vector2.zero, new Vector2(970, 64));
            hud.textoDaDica.resizeTextForBestFit = true; hud.textoDaDica.resizeTextMinSize = 22; hud.textoDaDica.resizeTextMaxSize = 34;

            // Definição da estrutura escolhida (à esquerda, no meio): ilustração do glossário, palavra, texto e "Ouvir".
            var definicao = Imagem("Painel_Da_Definicao", raiz, arredondado, C("Branco", .96f), new Vector2(240, -55), new Vector2(430, 590));
            TravessiaConstrutor.Ancorar(definicao, new Vector2(0, .5f));
            hud.painelDaDefinicao = definicao.gameObject;
            var moldura = Imagem("Moldura_Da_Ilustracao", definicao.transform, arredondado, C("Vidro"), new Vector2(0, 190), new Vector2(390, 190));
            hud.ilustracao = Imagem("Ilustracao", moldura.transform, null, Color.white, Vector2.zero, new Vector2(370, 180));
            hud.ilustracao.preserveAspect = true;
            hud.textoDaPalavra = Texto("Palavra", definicao.transform, "", 44, C("Petroleo"), new Vector2(0, 64), new Vector2(400, 56));
            hud.textoDaPalavra.resizeTextForBestFit = true; hud.textoDaPalavra.resizeTextMinSize = 28; hud.textoDaPalavra.resizeTextMaxSize = 44;
            Imagem("Sublinhado", definicao.transform, arredondado, C("Mostarda"), new Vector2(0, 32), new Vector2(200, 8));
            hud.textoDaDefinicao = Texto("Definicao", definicao.transform, "", 30, C("Grafite"), new Vector2(0, -62), new Vector2(396, 172), FontStyle.Normal);
            hud.textoDaDefinicao.resizeTextForBestFit = true; hud.textoDaDefinicao.resizeTextMinSize = 20; hud.textoDaDefinicao.resizeTextMaxSize = 30;
            hud.textoDaDefinicao.verticalOverflow = VerticalWrapMode.Truncate;
            botaoOuvir = Botao("Botao_Ouvir", definicao.transform, "Ouvir", "Petroleo_Claro", new Vector2(0, -226), new Vector2(300, 120), 46);
            hud.botaoOuvir = botaoOuvir.gameObject;
            definicao.gameObject.SetActive(false);

            // Recado do momento (embaixo, no meio, acima da barra).
            var recado = Imagem("Painel_Do_Recado", raiz, arredondado, C("Petroleo", .92f), new Vector2(0, 266), new Vector2(900, 58));
            TravessiaConstrutor.Ancorar(recado, new Vector2(.5f, 0));
            hud.textoDoRecado = Texto("Texto", recado.transform, "", 38, C("Branco"), Vector2.zero, new Vector2(870, 52));
            hud.textoDoRecado.resizeTextForBestFit = true; hud.textoDoRecado.resizeTextMinSize = 22; hud.textoDoRecado.resizeTextMaxSize = 34;

            // Barra de ferramentas: até 4 estruturas, cada uma com o desenho do glossário e o nome.
            var barra = Tela("Barra_De_Ferramentas", raiz);
            hud.barra = barra.gameObject;
            var botoes = new BotaoDeEstrutura[4];
            for (int i = 0; i < 4; i++)
            {
                var botao = Botao("Botao_Estrutura_" + (i + 1), barra, "Estrutura", "Branco", new Vector2((i - 1.5f) * 340f, 125), new Vector2(320, 210), 34, S("figura_estrela"));
                TravessiaConstrutor.Ancorar(botao, new Vector2(.5f, 0));
                var b = botao.gameObject.AddComponent<BotaoDeEstrutura>();
                b.indice = i;
                b.frente = botao.transform.Find("Frente").GetComponent<Image>();
                b.icone = botao.transform.Find("Frente/Icone").GetComponent<Image>();
                b.nome = botao.transform.Find("Frente/Texto").GetComponent<Text>();
                var rtIcone = (RectTransform)b.icone.transform;
                rtIcone.anchoredPosition = new Vector2(0, 30);
                rtIcone.sizeDelta = new Vector2(280, 124);
                var rtNome = (RectTransform)b.nome.transform;
                rtNome.anchoredPosition = new Vector2(0, -68);
                rtNome.sizeDelta = new Vector2(300, 60);
                b.nome.resizeTextForBestFit = true; b.nome.resizeTextMinSize = 22; b.nome.resizeTextMaxSize = 34;
                b.nome.horizontalOverflow = HorizontalWrapMode.Wrap;
                UnityEventTools.AddPersistentListener(botao.onClick, b.Tocar);
                botoes[i] = b;
            }
            hud.botoes = botoes;
            hud.espacoEntreBotoes = 340f;
            hud.corDoBotao = C("Branco");
            hud.corDoBotaoEscolhido = C("Mostarda_Clara");

            botaoDica = Botao("Botao_Dica", raiz, "Dica", "Mostarda", new Vector2(-140, 125), new Vector2(230, 150), 52);
            TravessiaConstrutor.Ancorar(botaoDica, new Vector2(1, 0));
            hud.botaoDica = botaoDica.gameObject;

            // Balão de explicação quando a estrutura não serve (no meio da tela).
            var balao = Imagem("Balao", raiz, arredondado, C("Grafite"), new Vector2(0, 60), new Vector2(900, 250));
            Imagem("Papel", balao.transform, arredondado, C("Mostarda_Clara"), Vector2.zero, new Vector2(886, 236));
            hud.textoDoBalao = Texto("Texto", balao.transform, "", 42, C("Grafite"), Vector2.zero, new Vector2(840, 212), FontStyle.Normal);
            hud.textoDoBalao.resizeTextForBestFit = true; hud.textoDoBalao.resizeTextMinSize = 26; hud.textoDoBalao.resizeTextMaxSize = 42;
            hud.balao = balao.gameObject;
            balao.gameObject.SetActive(false);

            hud.corGanha = Color.white;
            hud.corPerdida = new Color(.4f, .45f, .5f, .3f);
            return hud;
        }
    }
}
