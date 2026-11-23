using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static CidadeViva.EditorArte.MenuConstrutor;

namespace CidadeViva.EditorArte
{
    // Monta do zero a mecânica "Oficina de Veículos": prefabs das peças e dos chassis, as receitas dos
    // 4 primeiros veículos, a config, os níveis e a cena Minigame_Oficina.
    // As receitas (Assets/Resources/Veiculos) só são criadas se ainda não existirem: depois de revisadas
    // pela equipe, rodar de novo não desfaz a revisão. Toda vez, confere se cada trecho citado existe
    // mesmo na definição do verbete.
    public static class OficinaConstrutor
    {
        const string CaminhoDaCena = "Assets/Scenes/Minigame_Oficina.unity";
        const string CaminhoDaConfig = "Assets/Configs/Oficina_Config.asset";
        const string CaminhoDosNiveis = "Assets/Configs/Niveis/Niveis_Oficina.asset";
        const string PastaDasReceitas = "Assets/Resources/" + VeiculoReceita.PastaNoResources;
        const string CaminhoDoMotor = "Assets/Audio/Efeitos/motor_ligando.wav";
        const string Categoria = "Oficina";
        public const string NomeDaArea = "Oficina de Veículos";

        [MenuItem("Cidade Viva/Minigames/Reconstruir a cena da Oficina de Veículos")]
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
            GerarSomDoMotor();
            GerarPrefabs();
            CriarReceitasQueFaltam();
            ConferirReceitas();
            var niveis = SalvarNiveis();
            var config = PrepararConfig();

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 45f;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AmbienteConstrutor.CaminhoPrefab) == null) AmbienteConstrutor.CriarPrefab();
            AmbienteConstrutor.AplicarNaCenaAberta(true);

            // A cena fica na metade de baixo da tela: em cima vai o painel da definição.
            var poseNaOficina = F.Grupo("Camera_Na_Oficina", null, F.V(0, 3.1f, -8.2f)).transform;
            poseNaOficina.LookAt(F.V(0, 2.45f, 0));
            cam.transform.SetPositionAndRotation(poseNaOficina.position, poseNaOficina.rotation);

            // ---------------- oficina ----------------
            var cenario = F.Grupo("Cenario").transform;
            SemSombra(F.Cubo(cenario, "Piso", "Areia", F.V(0, -.05f, 1f), F.V(16f, .1f, 12f)));
            SemSombra(F.Cubo(cenario, "Faixa_Do_Piso", "Mostarda", F.V(0, .006f, -1.25f), F.V(4.4f, .012f, .16f)));
            F.Cubo(cenario, "Parede_Do_Fundo", "Petroleo_Claro", F.V(0, 2.5f, 3.2f), F.V(16f, 5f, .2f));
            F.Cubo(cenario, "Rodape", "Petroleo", F.V(0, .25f, 3.08f), F.V(16f, .5f, .06f));
            F.Cubo(cenario, "Parede_Esquerda", "Petroleo_Claro", F.V(-7.6f, 2.5f, 1f), F.V(.2f, 5f, 6f));
            F.Cubo(cenario, "Parede_Direita", "Petroleo_Claro", F.V(7.6f, 2.5f, 1f), F.V(.2f, 5f, 6f));
            F.Face(cenario, "Quadro_Pare", "Tex_placa_pare", F.V(-.9f, 3.3f, 3.08f), .9f, .9f);
            F.Face(cenario, "Quadro_Ciclovia", "Tex_placa_ciclovia", F.V(.9f, 3.3f, 3.08f), .9f, .9f);
            P(cenario, "Oficina/Bancada", Vector3.zero);
            P(cenario, "Oficina/Prateleira", F.V(-3.45f, 0, 1.5f));
            P(cenario, "Oficina/Prateleira", F.V(3.45f, 0, 1.5f));
            // Nada atrás da bancada: o veículo em montagem precisa aparecer limpo.
            P(cenario, "Personagens/Trabalhador", F.V(-5.9f, 0, .5f), 150);
            P(cenario, "Props/Barril", F.V(6.3f, 0, 2.4f));
            P(cenario, "Props/Caixa_Papelao", F.V(5.6f, 0, 2.1f), 25);
            P(cenario, "Props/Cone", F.V(5.9f, 0, .4f));
            P(cenario, "Props/Vassoura", F.V(-6.6f, 0, 2.7f), 0).transform.Rotate(-8, 0, 0);
            CaraDeOficina(cenario);

            // O chassi nasce aqui, de lado pra câmera (a frente dele, +Z, aponta pra direita).
            var bancada = F.Grupo("Bancada_Encaixe", null, F.V(0, .95f, 0), F.V(0, 90, 0)).transform;
            // Maior que o tamanho real, pra criança ver bem cada peça encaixada.
            bancada.localScale = Vector3.one * 1.3f;

            // 12 vagas: primeiro a fileira do meio (na altura dos olhos), depois a de cima e a de baixo.
            var vagas = new List<Transform>();
            var grupoDeVagas = F.Grupo("Vagas_Da_Prateleira").transform;
            foreach (float y in new[] { 1.47f, 2.22f, .72f })
                foreach (float centro in new[] { -3.45f, 3.45f })
                    foreach (float dx in new[] { -.6f, .6f })
                        vagas.Add(F.Grupo("Vaga_" + (vagas.Count + 1), grupoDeVagas, F.V(centro + dx, y, 1.3f)).transform);

            // Modelos inativos: o manager copia um chassi e as peças a cada veículo.
            var modelos = F.Grupo("Modelos").transform;
            var chassis = new List<ChassiDaOficina>();
            foreach (string id in new[] { "DuasRodas", "QuatroRodas", "UmEixo" })
            {
                var go = P(modelos, "Oficina/Chassi_" + id, Vector3.zero);
                go.name = "Modelo_Chassi_" + id;
                go.SetActive(false);
                chassis.Add(go.GetComponent<ChassiDaOficina>());
            }
            var pecas = new List<PecaDaOficina>();
            foreach (TipoDePeca tipo in System.Enum.GetValues(typeof(TipoDePeca)))
            {
                var go = P(modelos, "Oficina/Peca_" + tipo, Vector3.zero);
                go.name = "Modelo_Peca_" + tipo;
                go.SetActive(false);
                pecas.Add(go.GetComponent<PecaDaOficina>());
            }

            // ---------------- pista de teste (do lado de fora, longe da oficina) ----------------
            var pistaGo = F.Grupo("Pista", null, F.V(60, 0, 0));
            var pista = pistaGo.AddComponent<OficinaPista>();
            var p = pistaGo.transform;
            SemSombra(F.Plano(p, "Gramado", "Verde_Claro", F.V(0, -.04f, 0), 90, 70));
            SemSombra(F.Cubo(p, "Asfalto", "Asfalto", F.V(0, -.03f, 0), F.V(20.5f, .06f, 12f)));
            for (float x = -9.5f; x <= 9.5f; x += 1.9f)
                foreach (float z in new[] { -5.75f, 5.75f })
                    SemSombra(F.Cubo(p, "Borda", "Branco", F.V(x, .006f, z), F.V(1.1f, .012f, .16f)));
            foreach (float x in new[] { -10f, 10f })
                foreach (float z in new[] { -5.75f, 5.75f })
                    P(p, "Props/Cone", F.V(x, 0, z));
            P(p, "Predios/Casa_01", F.V(-9f, 0, 13f), 180);
            P(p, "Predios/Casa_05", F.V(3f, 0, 13.5f), 180);
            P(p, "Natureza/Arvore_Comum_1", F.V(-2.5f, 0, 9f), 30);
            P(p, "Natureza/Arvore_Comum_2", F.V(10.5f, 0, 9.5f), 120);
            P(p, "Natureza/Arbusto_1", F.V(-12f, 0, 2f));
            P(p, "Natureza/Flores", F.V(12f, 0, -2f), 0, 1.4f);
            pista.partida = F.Grupo("Partida", p, F.V(-7.5f, 0, -2f), F.V(0, 90, 0)).transform;
            pista.moedas = new[] { F.V(-2.5f, .1f, -2f), F.V(3f, .1f, 2.2f), F.V(7.5f, .1f, -1.5f) }
                .Select((pos, i) => { var m = P(p, "Props/Moeda", pos); m.name = "Moeda_" + (i + 1); m.SetActive(false); return m; }).ToArray();
            pista.meiaArea = new Vector2(9.3f, 5f);
            // Trilhos do bonde: só aparecem quando o veículo anda em trilhos.
            var trilhos = F.Grupo("Trilhos", p).transform;
            foreach (float z in new[] { -.5f, .5f })
                SemSombra(F.Cubo(trilhos, "Trilho", "Cinza_Escuro", F.V(0, .05f, z), F.V(19.5f, .1f, .13f)));
            for (float x = -9.4f; x <= 9.4f; x += 1.15f)
                SemSombra(F.Cubo(trilhos, "Dormente", "Marrom", F.V(x, .02f, 0), F.V(.32f, .05f, 1.5f)));
            trilhos.gameObject.SetActive(false);
            pista.trilhos = trilhos.gameObject;
            pista.partidaNosTrilhos = F.Grupo("Partida_Nos_Trilhos", p, F.V(-7.5f, 0, 0), F.V(0, 90, 0)).transform;
            var poseNaPista = F.Grupo("Camera_Na_Pista", null, F.V(60, 15f, -14f)).transform;
            poseNaPista.LookAt(F.V(60, 0, -.5f));

            // ---------------- efeitos ----------------
            var efeitos = F.Grupo("Efeitos").transform;
            var estrelinhas = TravessiaConstrutor.Particulas(efeitos, "Estrelinhas", 14, .7f, 3f, .2f, .6f, "Mostarda", "Mostarda_Clara", "Branco");
            var confete = TravessiaConstrutor.Particulas(efeitos, "Confete", 60, 1.5f, 5.5f, .2f, 2.5f,
                "Mostarda", "Vermelho", "Petroleo_Claro", "Verde", "Rosa", "Laranja");
            var poeira = TravessiaConstrutor.Particulas(efeitos, "Poeira", 8, .5f, 1.6f, .18f, .4f, "Cinza_Claro", "Areia");

            // ---------------- interface ----------------
            var canvas = NovoCanvas("Canvas");
            NovoEventSystem(false);
            var hud = Hud(canvas.transform, pista, out var botaoOuvir, out var botaoDica);
            var fim = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabFim), canvas.transform);
            var selecao = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabSelecao), canvas.transform);

            // ---------------- gerente ----------------
            var gerenteGo = new GameObject("OficinaManager", typeof(AudioSource));
            var som = gerenteGo.GetComponent<AudioSource>();
            som.playOnAwake = false;
            som.volume = .8f;
            var gerente = gerenteGo.AddComponent<OficinaManager>();
            gerente.nomeDaArea = NomeDaArea;
            gerente.telaFimDeJogo = fim.GetComponent<TelaFimDeJogo>();
            gerente.telaDeSelecao = selecao.GetComponent<TelaSelecaoDeNivel>();
            gerente.niveis = niveis;
            gerente.config = config;
            gerente.hud = hud;
            gerente.pista = pista;
            gerente.som = som;
            gerente.cameraDoJogo = cam;
            gerente.poseDaCameraNaOficina = poseNaOficina;
            gerente.poseDaCameraNaPista = poseNaPista;
            gerente.bancada = bancada;
            gerente.vagasDaPrateleira = vagas.ToArray();
            gerente.modelosDeChassi = chassis.ToArray();
            gerente.modelosDePeca = pecas.ToArray();
            gerente.materialDoMolde = MaterialDoMolde();
            gerente.estrelinhas = estrelinhas;
            gerente.confete = confete;
            gerente.poeira = poeira;
            UnityEventTools.AddPersistentListener(botaoOuvir.onClick, gerente.OuvirDefinicao);
            UnityEventTools.AddPersistentListener(botaoDica.onClick, gerente.PedirDica);
            UnityEventTools.AddPersistentListener(hud.botaoEngatar.GetComponent<Button>().onClick, gerente.Engatar);

            EditorSceneManager.SaveScene(cena, CaminhoDaCena);
            if (EditorBuildSettings.scenes.All(s => s.path != CaminhoDaCena))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(CaminhoDaCena, true)).ToArray();
        }

        // Material do veículo em montagem: cinza, com a cor de verdade só onde já tem peça (shader CidadeViva/Molde).
        static Material MaterialDoMolde()
        {
            const string caminho = "Assets/Materials/Molde.mat";
            var shader = Shader.Find("CidadeViva/Molde");
            if (shader == null) { Debug.LogError("[Oficina] Shader CidadeViva/Molde não encontrado."); return ArtePaleta.Mat("Cinza_Claro"); }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, caminho);
            }
            mat.shader = shader;
            mat.SetColor("_CorDoMolde", ArtePaleta.Cor("Cinza_Claro"));
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        // ------------------------------------------------------------------ receitas

        static ItemDaReceita Item(TipoDePeca peca, int quantidade, string trecho, string verbeteDoTrecho = "", bool ouMais = false, params TipoDePeca[] tambemAceita)
        {
            return new ItemDaReceita
            {
                peca = peca, quantidade = quantidade, trechoDaDefinicao = trecho, verbeteDoTrecho = verbeteDoTrecho, ouMais = ouMais,
                tambemAceita = new List<TipoDePeca>(tambemAceita),
            };
        }

        static ItemDaReceita Base(TipoDePeca peca, int quantidade, params TipoDePeca[] tambemAceita)
        {
            return new ItemDaReceita { peca = peca, quantidade = quantidade, trechoDaDefinicao = "", basePadrao = true, tambemAceita = new List<TipoDePeca>(tambemAceita) };
        }

        static ItemDaReceita Citado(this ItemDaReceita item, string comoEhCitado)
        {
            item.comoEhCitado = comoEhCitado;
            return item;
        }

        // Onde cada veículo aparece: (nível, ordem dentro do nível). Do mais fácil pro mais difícil.
        static readonly Dictionary<string, (int nivel, int ordem)> Distribuicao = new Dictionary<string, (int, int)>
        {
            // 1: duas e quatro rodas simples, com a dica visível.
            ["bicicleta"] = (1, 0), ["motocicleta"] = (1, 1), ["automovel"] = (1, 2),
            // 2: entram peças erradas, "ou", peça opcional e a primeira base padrão.
            ["quadriciclo"] = (2, 0), ["onibus"] = (2, 1), ["ambulancia"] = (2, 2),
            // 3: sem dica; sirene x luzes, e o bonde com trilhos e motor elétrico.
            ["viatura"] = (3, 0), ["caminhao_de_bombeiro"] = (3, 1), ["bonde"] = (3, 2),
            // 4: com tempo; os dois do engate (o trator puxa, o reboque é puxado).
            ["caminhao_trator"] = (4, 0), ["reboque"] = (4, 1),
            // 5: os parecidos com veículos já montados, com peças parecidas na prateleira.
            ["bicicleta_eletrica"] = (5, 0), ["motoneta"] = (5, 1), ["micro_onibus"] = (5, 2),
        };

        // As receitas dos 14 veículos. Cada peça vem com o trecho da definição que a justifica; o que a
        // definição não diz e o veículo precisa pra existir entra como base padrão (Base).
        static void CriarReceitasQueFaltam()
        {
            ArtePaleta.GarantirPasta(PastaDasReceitas);
            const TipoDePeca Roda = TipoDePeca.Roda, Motor = TipoDePeca.Motor, Eletrico = TipoDePeca.MotorEletrico;
            const string notaDoMotor = "A definição diz só \"motor\", sem dizer de que tipo: por isso o encaixe aceita o motor comum e o motor elétrico.";
            const string notaDaBase = "BASE PADRÃO (não vem da definição): ";

            Receita("bicicleta", "DuasRodas", "",
                Item(TipoDePeca.Pedal, 1, "se move quando a pessoa pedala"),
                Item(Roda, 2, "possui duas rodas"));

            Receita("bicicleta_eletrica", "DuasRodas",
                "A definição começa com \"Bicicleta\": as duas rodas e o pedal vêm da definição do verbete bicicleta. " + notaDoMotor
                + " \"menos potente que a moto\" e \"serve para se mover de um lugar para outro\" não viram peça.",
                Item(Roda, 2, "possui duas rodas", "bicicleta"),
                Item(TipoDePeca.Pedal, 1, "se move quando a pessoa pedala", "bicicleta"),
                Item(Motor, 1, "com motor", "", false, Eletrico));

            Receita("motocicleta", "DuasRodas",
                notaDoMotor + " \"para transporte do motoqueiro e mais uma pessoa\" fala de quem ela leva, não de assento: não virou peça.",
                Item(Motor, 1, "com motor", "", false, Eletrico),
                Item(Roda, 2, "duas rodas"));

            Receita("automovel", "QuatroRodas",
                notaDoMotor + " \"quatro ou mais rodas\": fica pronto com 4 e aceita até 6 (o chassi tem 6 encaixes). "
                + "\"transporte de pessoas ou cargas\" não virou assento nem carroceria.",
                Item(Motor, 1, "com motor", "", false, Eletrico),
                Item(Roda, 4, "com quatro ou mais rodas", "", true));

            Receita("viatura", "QuatroRodas",
                notaDoMotor + " \"faz um barulho\" não diz com o quê: não virou sirene obrigatória (a palavra sirene só aparece na definição de ambulância); ela entra como extra aceito. "
                + "São exatamente quatro rodas, então a quinta é recusada. \"usado por bombeiros e policiais\" não virou peça.",
                Item(Motor, 1, "com motor", "", false, Eletrico),
                Item(Roda, 4, "quatro rodas"),
                Item(TipoDePeca.Luzes, 1, "tem luzes que acendem e apagam"));

            var r = Receita("quadriciclo", "QuatroRodas",
                notaDoMotor + " \"Pode ter ou não uma parte de cima para cobrir\": a cobertura é opcional (encaixa sem erro, não é exigida). \"levar pessoas ou coisas\" não virou peça.",
                Item(Roda, 4, "tem quatro rodas"),
                Item(Motor, 1, "um motor", "", false, Eletrico),
                Item(TipoDePeca.Cobertura, 1, "Pode ter ou não uma parte de cima para cobrir"));
            if (r != null) r.itens[2].opcional = true;

            Receita("onibus", "QuatroRodas",
                notaDoMotor + " \"quatro ou seis rodas\": fica pronto com 4 e aceita até 6. ATENÇÃO: o jogo também deixa terminar com 5 (4 obrigatórias + 1), o que a definição não diz. "
                + "\"para transportar muitas pessoas\" não virou assento.",
                Item(Motor, 1, "com motor", "", false, Eletrico),
                Item(Roda, 4, "com quatro ou seis rodas", "", true));

            Receita("ambulancia", "QuatroRodas",
                "Só a sirene vem da definição. " + notaDaBase + "4 rodas e 1 motor, porque a definição não diz quantas rodas nem fala de motor. "
                + "\"transporta pessoas doentes\" e \"cuida delas\" não viraram peça. Luzes entram como extra aceito (a definição não fala de luzes).",
                Item(TipoDePeca.Sirene, 1, "tem sirene barulhenta"),
                Base(Roda, 4),
                Base(Motor, 1, Eletrico));

            Receita("caminhao_de_bombeiro", "QuatroRodas",
                "NENHUMA peça vem da definição: ela fala de cor (\"vermelho\"), tamanho (\"grande\"), velocidade (\"veloz\") e do que o caminhão leva, mas não de rodas, motor, sirene nem luzes. "
                + notaDaBase + "4 rodas e 1 motor. Sirene e luzes entram só como extras aceitos. O \"vermelho\" está no modelo do veículo.",
                Base(Roda, 4),
                Base(Motor, 1, Eletrico));

            r = Receita("bonde", "QuatroRodas",
                "Motor elétrico e trilhos vêm da definição; o motor comum é recusado citando \"com motor elétrico\". " + notaDaBase + "4 rodas (a definição não fala de rodas). "
                + "Na pista ele anda em cima dos trilhos. \"para transportar pessoas\" não virou assento.",
                Item(Eletrico, 1, "com motor elétrico"),
                Item(TipoDePeca.Trilho, 1, "anda por trilhos"),
                Base(Roda, 4));
            if (r != null)
            {
                r.andaEmTrilhos = true;
                r.proibidas.Add(Item(Motor, 1, "com motor elétrico"));
            }

            Receita("caminhao_trator", "QuatroRodas",
                "Só o engate vem da definição (\"serve para puxar carretas\"). \"Caminhão\" não é verbete do glossário, então não há definição de onde tirar rodas e motor. "
                + notaDaBase + "4 rodas e 1 motor.",
                Item(TipoDePeca.Engate, 1, "serve para puxar carretas"),
                Base(Roda, 4),
                Base(Motor, 1, Eletrico));

            r = Receita("reboque", "UmEixo",
                "Duas rodas e o engate vêm da definição; qualquer motor é recusado citando \"sem motor\". Depois de montado há o passo de engatar: "
                + "ele é \"arrastado por outro veículo com motor\", e o jogo usa o caminhão trator (que \"serve para puxar carretas\") pra puxar.",
                Item(Roda, 2, "com duas rodas"),
                Item(TipoDePeca.Engate, 1, "movimentado quando arrastado por outro veículo com motor"));
            if (r != null)
            {
                r.puxadoPor = "caminhao_trator";
                r.proibidas.Add(Item(Motor, 1, "sem motor", "", false, Eletrico));
            }

            r = Receita("motoneta", "DuasRodas",
                "\"Tipo de moto\": o glossário não tem o verbete \"moto\", mas a definição de motocicleta termina com \"Ver moto\"; por isso o motor e as duas rodas vêm da definição de motocicleta. "
                + notaDoMotor + " O assento vem de \"o motorista fica com as pernas juntas\" (formato próprio de assento). \"mais devagar\": na pista ela anda a 70% da velocidade. \"mais leve\" não virou peça.",
                Item(Motor, 1, "com motor", "motocicleta", false, Eletrico).Citado("moto"),
                Item(Roda, 2, "duas rodas", "motocicleta").Citado("moto"),
                Item(TipoDePeca.Assento, 1, "em que o motorista fica com as pernas juntas"));
            if (r != null)
            {
                r.variante = "Motoneta";
                r.velocidadeNaPista = .7f;
            }

            Receita("micro_onibus", "QuatroRodas",
                notaDoMotor + " São exatamente quatro rodas (a quinta é recusada): é o que separa do ônibus, que aceita seis. \"parece um ônibus menor\" e \"transporte de passageiros\" não viraram peça.",
                Item(Motor, 1, "com motor", "", false, Eletrico),
                Item(Roda, 4, "com quatro rodas"));

            AssetDatabase.SaveAssets();
            LigarModelosProntos();
            LigarExtras();
        }

        // Cria a receita se ela ainda não existe (devolve null se já existia: receita revisada não é mexida).
        static VeiculoReceita Receita(string idDoVerbete, string chassi, string observacao, params ItemDaReceita[] itens)
        {
            string caminho = $"{PastaDasReceitas}/Receita_{idDoVerbete}.asset";
            if (AssetDatabase.LoadAssetAtPath<VeiculoReceita>(caminho) != null) return null;
            var r = ScriptableObject.CreateInstance<VeiculoReceita>();
            r.idDoVerbete = idDoVerbete;
            (r.nivel, r.ordem) = Distribuicao[idDoVerbete];
            r.chassi = chassi;
            r.observacao = observacao;
            r.itens = new List<ItemDaReceita>(itens);
            AssetDatabase.CreateAsset(r, caminho);
            return r;
        }

        // Põe os 14 veículos nos níveis da tabela Distribuicao. Não roda sozinho ao reconstruir a cena,
        // pra não desfazer uma troca de nível feita no Inspector.
        [MenuItem("Cidade Viva/Verbetes/Aplicar a distribuição padrão dos veículos pelos níveis")]
        public static void AplicarDistribuicao()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:VeiculoReceita", new[] { PastaDasReceitas }))
            {
                var r = AssetDatabase.LoadAssetAtPath<VeiculoReceita>(AssetDatabase.GUIDToAssetPath(guid));
                if (!Distribuicao.TryGetValue(r.idDoVerbete, out var lugar)) continue;
                r.nivel = lugar.nivel;
                r.ordem = lugar.ordem;
                EditorUtility.SetDirty(r);
            }
            AssetDatabase.SaveAssets();
        }

        // Peças que o veículo tem na vida real, mas a definição não cita: encaixam como extra, sem erro.
        // ISTO NÃO VEM DO GLOSSÁRIO: é escolha da equipe, pra criança não ser corrigida por pôr um assento.
        // Só preenche receita com a lista vazia.
        static void LigarExtras()
        {
            const TipoDePeca Assento = TipoDePeca.Assento, Luzes = TipoDePeca.Luzes, Sirene = TipoDePeca.Sirene, Carroceria = TipoDePeca.Carroceria;
            var extras = new Dictionary<string, TipoDePeca[]>
            {
                ["bicicleta"] = new[] { Assento },
                ["bicicleta_eletrica"] = new[] { Assento },
                ["motocicleta"] = new[] { Assento, Luzes },
                ["automovel"] = new[] { Assento, Luzes, Carroceria },
                ["viatura"] = new[] { Assento, Sirene, Carroceria },
                ["quadriciclo"] = new[] { Assento, Luzes },
                ["onibus"] = new[] { Assento, Luzes },
                ["ambulancia"] = new[] { Assento, Luzes },
                ["caminhao_de_bombeiro"] = new[] { Assento, Sirene, Luzes },
                ["bonde"] = new[] { Assento, Luzes },
                ["caminhao_trator"] = new[] { Assento, Luzes },
                ["reboque"] = new[] { Carroceria, Luzes },
                ["motoneta"] = new[] { Luzes },
                ["micro_onibus"] = new[] { Assento, Luzes },
            };
            foreach (var guid in AssetDatabase.FindAssets("t:VeiculoReceita", new[] { PastaDasReceitas }))
            {
                var r = AssetDatabase.LoadAssetAtPath<VeiculoReceita>(AssetDatabase.GUIDToAssetPath(guid));
                if (r.extrasAceitos.Count > 0 || !extras.TryGetValue(r.idDoVerbete, out var lista)) continue;
                r.extrasAceitos = new List<TipoDePeca>(lista);
                EditorUtility.SetDirty(r);
            }
            AssetDatabase.SaveAssets();
        }

        // Modelo do veículo inteiro: é o molde cinza da bancada e o veículo do teste. É só aparência.
        // Só preenche receita que ainda não tem modelo, pra não desfazer escolha feita no Inspector.
        // giro: frente do modelo em +Z (conferido por foto). "Oficina/Pronto_*" são os feitos de formas simples.
        static void LigarModelosProntos()
        {
            var modelos = new Dictionary<string, (string prefab, float escala, float giro)>
            {
                ["bicicleta"] = ("Carros/Bicicleta_Q1", .9f, 180f), ["bicicleta_eletrica"] = ("Oficina/Pronto_Bicicleta_Eletrica", .9f, 180f),
                ["motocicleta"] = ("Carros/Moto", .95f, 180f), ["automovel"] = ("Carros/Carro_Sedan", 1f, 0f), ["viatura"] = ("Carros/Carro_Policia", 1f, 0f),
                ["quadriciclo"] = ("Oficina/Pronto_Quadriciclo", .85f, 0f), ["onibus"] = ("Carros/Onibus", 1.25f, 0f),
                ["ambulancia"] = ("Carros/Ambulancia", 1f, 0f), ["caminhao_de_bombeiro"] = ("Carros/Caminhao_Bombeiros", 1.15f, 0f),
                ["bonde"] = ("Carros/Metro", 1.25f, 0f), ["caminhao_trator"] = ("Oficina/Pronto_Caminhao_Trator", 1.05f, 0f),
                ["reboque"] = ("Oficina/Pronto_Reboque", .85f, 0f), ["motoneta"] = ("Oficina/Pronto_Motoneta", .8f, 0f),
                ["micro_onibus"] = ("Carros/Carro_Van", 1.05f, 0f),
            };
            foreach (var guid in AssetDatabase.FindAssets("t:VeiculoReceita", new[] { PastaDasReceitas }))
            {
                var r = AssetDatabase.LoadAssetAtPath<VeiculoReceita>(AssetDatabase.GUIDToAssetPath(guid));
                if (r.modeloPronto != null || !modelos.TryGetValue(r.idDoVerbete, out var m)) continue;
                r.modeloPronto = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArteImportador.PastaPrefabs}/{m.prefab}.prefab");
                r.escalaDoModeloPronto = m.escala;
                r.giroDoModeloPronto = m.giro;
                if (r.modeloPronto == null) Debug.LogWarning("[Oficina] Modelo pronto não encontrado: " + m.prefab);
                EditorUtility.SetDirty(r);
            }
            AssetDatabase.SaveAssets();
        }

        // Toda peça de toda receita precisa de um trecho que exista, letra por letra, na definição citada.
        [MenuItem("Cidade Viva/Verbetes/Conferir receitas da Oficina com as definições")]
        public static bool ConferirReceitas()
        {
            var banco = AssetDatabase.LoadAssetAtPath<VerbeteDatabase>(VerbetesConstrutor.CaminhoDoAsset);
            banco.Recarregar();
            bool tudoCerto = true;
            foreach (var guid in AssetDatabase.FindAssets("t:VeiculoReceita", new[] { PastaDasReceitas }))
            {
                var r = AssetDatabase.LoadAssetAtPath<VeiculoReceita>(AssetDatabase.GUIDToAssetPath(guid));
                var verbete = banco.Buscar(r.idDoVerbete);
                if (verbete == null) { Debug.LogError($"[Oficina] {r.name}: verbete \"{r.idDoVerbete}\" não existe."); tudoCerto = false; continue; }
                if (verbete.Grupo != GrupoDeVerbete.Veiculos) { Debug.LogError($"[Oficina] {r.name}: \"{verbete.palavra}\" não é do grupo Veículos."); tudoCerto = false; }
                foreach (var item in r.proibidas)
                {
                    if (string.IsNullOrEmpty(item.trechoDaDefinicao) || !verbete.definicao.Contains(item.trechoDaDefinicao))
                    {
                        Debug.LogError($"[Oficina] {r.name}: a peça proibida {item.peca} cita \"{item.trechoDaDefinicao}\", que NÃO está na definição de \"{verbete.palavra}\".");
                        tudoCerto = false;
                    }
                }
                foreach (var item in r.itens)
                {
                    // Base padrão: a definição não fala da peça, então não há trecho pra conferir.
                    if (item.basePadrao) continue;
                    var fonte = string.IsNullOrEmpty(item.verbeteDoTrecho) ? verbete : banco.Buscar(item.verbeteDoTrecho);
                    if (fonte == null) { Debug.LogError($"[Oficina] {r.name}: verbete citado \"{item.verbeteDoTrecho}\" não existe."); tudoCerto = false; continue; }
                    if (string.IsNullOrEmpty(item.trechoDaDefinicao) || !fonte.definicao.Contains(item.trechoDaDefinicao))
                    {
                        Debug.LogError($"[Oficina] {r.name}: a peça {item.peca} cita \"{item.trechoDaDefinicao}\", que NÃO está na definição de \"{fonte.palavra}\": {fonte.definicao}");
                        tudoCerto = false;
                    }
                    // Citar outro verbete só vale se a definição do veículo fala dele.
                    string citado = string.IsNullOrEmpty(item.comoEhCitado) ? fonte.palavra : item.comoEhCitado;
                    if (fonte != verbete && verbete.definicao.IndexOf(citado, System.StringComparison.CurrentCultureIgnoreCase) < 0)
                    {
                        Debug.LogError($"[Oficina] {r.name}: a peça {item.peca} usa a definição de \"{fonte.palavra}\", mas a definição de \"{verbete.palavra}\" não cita essa palavra.");
                        tudoCerto = false;
                    }
                }
            }
            if (tudoCerto) Debug.Log("[Oficina] Receitas conferidas: todo trecho citado existe na definição.");
            return tudoCerto;
        }

        // ------------------------------------------------------------------ níveis e config

        static NivelConfig Nivel(string objetivo, int pecasErradas, float tempo, params string[] regras)
        {
            return new NivelConfig
            {
                objetivo = objetivo, quantidadeDeItens = pecasErradas, tempoEmSegundos = tempo, regrasExtras = new List<string>(regras),
                // As estrelas são calculadas pelo OficinaManager (sem erro, no tempo, sem dica); isto só alimenta o texto da seleção.
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
            var revisao = Nivel("Peças parecidas! Escolha a que a definição diz.", 4, 60f, OficinaManager.RegraPecasParecidas);
            revisao.extras.Add(new ParametroExtra { nome = OficinaManager.ExtraFasesDeRevisao, valor = 2 });
            asset.niveis = new List<NivelConfig>
            {
                Nivel("Leia a definição e monte o veículo. A dica ajuda!", 0, 0f, OficinaManager.RegraDicaVisivel),
                Nivel("Agora tem peças que não servem. Leia com atenção!", 3, 0f),
                Nivel("Sem dica: só a definição ajuda você.", 4, 0f, OficinaManager.RegraSemDica),
                Nivel("Monte antes de o tempo acabar!", 5, 60f),
                revisao,
            };
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static OficinaConfig PrepararConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<OficinaConfig>(CaminhoDaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<OficinaConfig>();
                AssetDatabase.CreateAsset(config, CaminhoDaConfig);
            }
            // Textos e listas que mudaram quando entraram os extras e as peças erradas novas.
            var padrao = ScriptableObject.CreateInstance<OficinaConfig>();
            config.nomesDasPecas = padrao.nomesDasPecas;
            config.ordemDasPecasErradas = padrao.ordemDasPecasErradas;
            config.balaoForaDaDefinicao = padrao.balaoForaDaDefinicao;
            config.recadoDeExtra = padrao.recadoDeExtra;
            config.balaoDeExtraRepetido = padrao.balaoDeExtraRepetido;
            Object.DestroyImmediate(padrao);
            config.somDePegar = TravessiaConstrutor.Som("Interface/select_001.ogg");
            config.somDeEncaixe = TravessiaConstrutor.Som("Efeitos/impactMetal_light_000.ogg");
            config.somDeErro = TravessiaConstrutor.Som("Interface/error_004.ogg");
            config.somDeAcerto = TravessiaConstrutor.Som("Interface/confirmation_002.ogg");
            config.somDeDica = TravessiaConstrutor.Som("Interface/question_001.ogg");
            config.somDeMotorLigando = AssetDatabase.LoadAssetAtPath<AudioClip>(CaminhoDoMotor);
            config.somDeCampainha = TravessiaConstrutor.Som("Efeitos/campainha_bicicleta.wav");
            config.somDeMoeda = TravessiaConstrutor.Som("Efeitos/moeda.wav");
            config.somDeEngate = TravessiaConstrutor.Som("Efeitos/impactMetal_light_002.ogg");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        // Som de motor dando a partida (ronco grave que sobe e estabiliza), sintetizado: 1,5 s, mono, 22 kHz.
        static void GerarSomDoMotor()
        {
            if (File.Exists(CaminhoDoMotor)) return;
            const int taxa = 22050;
            int n = (int)(taxa * 1.5f);
            var amostras = new short[n];
            double fase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)taxa;
                float frequencia = Mathf.Lerp(38f, 95f, Mathf.SmoothStep(0f, 1f, t / .9f));
                fase += frequencia / taxa;
                float dente = (float)(fase % 1.0) * 2f - 1f;
                float ronco = dente * .5f + Mathf.Sin((float)fase * Mathf.PI * 4f) * .3f + Mathf.Sin((float)fase * Mathf.PI * 6f) * .12f;
                float tremor = 1f + .25f * Mathf.Sin(t * Mathf.PI * 2f * 13f);
                float volume = Mathf.Clamp01(t / .08f) * Mathf.Clamp01((1.5f - t) / .35f) * .32f;
                amostras[i] = (short)(Mathf.Clamp(ronco * tremor * volume, -1f, 1f) * short.MaxValue);
            }
            using (var arquivo = new BinaryWriter(File.Create(CaminhoDoMotor)))
            {
                arquivo.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                arquivo.Write(36 + n * 2);
                arquivo.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                arquivo.Write(16); arquivo.Write((short)1); arquivo.Write((short)1);
                arquivo.Write(taxa); arquivo.Write(taxa * 2); arquivo.Write((short)2); arquivo.Write((short)16);
                arquivo.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                arquivo.Write(n * 2);
                foreach (short a in amostras) arquivo.Write(a);
            }
            AssetDatabase.ImportAsset(CaminhoDoMotor, ImportAssetOptions.ForceSynchronousImport);
        }

        // ------------------------------------------------------------------ prefabs (Assets/Prefabs/Art/Oficina)

        // A sala da oficina (2026-11-19: a parede era lisa e vazia): parede de baixo mais escura com faixa
        // de segurança, janelas no alto, teto com vigas e luminárias penduradas, quadro de ferramentas,
        // pilha de pneus, carrinho de ferramentas, relógio e um macaco hidráulico.
        // Atrás da bancada (x de -2 a 2, até 3 m de altura) continua livre: o veículo tem de aparecer limpo.
        static void CaraDeOficina(Transform cenario)
        {
            var g = F.Grupo("Cara_De_Oficina", cenario).transform;
            // Parede: barra de baixo mais escura e faixa amarela e preta.
            F.Cubo(g, "Barra_Da_Parede", "Petroleo", F.V(0, .85f, 3.07f), F.V(15.2f, .7f, .04f));
            for (int i = 0; i < 37; i++)
                F.Cubo(g, "Faixa_De_Seguranca", i % 2 == 0 ? "Mostarda" : "Grafite", F.V(-7.4f + (i + .5f) * 0.4f, 1.27f, 3.05f), F.V(.4f, .14f, .03f));
            foreach (float x in new[] { -7.48f, 7.48f })
                F.Cubo(g, "Barra_Lateral", "Petroleo", F.V(x, .6f, 1f), F.V(.04f, 1.2f, 6f));

            // Janelas no alto da parede do fundo.
            foreach (float x in new[] { -5.6f, -2.6f, 2.6f, 5.6f })
            {
                F.Cubo(g, "Moldura", "Branco", F.V(x, 4.05f, 3.07f), F.V(2.1f, 1.1f, .05f));
                F.Cubo(g, "Vidro", "Vidro", F.V(x, 4.05f, 3.05f), F.V(1.9f, .9f, .05f));
                F.Cubo(g, "Travessa", "Branco", F.V(x, 4.05f, 3.03f), F.V(.06f, .9f, .04f));
            }

            // Teto com vigas e luminárias penduradas.
            F.Cubo(g, "Teto", "Cinza_Claro", F.V(0, 5.05f, 1f), F.V(16f, .1f, 6.4f));
            foreach (float x in new[] { -6f, -3f, 0f, 3f, 6f })
                F.Cubo(g, "Viga", "Cinza_Escuro", F.V(x, 4.85f, 1f), F.V(.25f, .3f, 6.2f));
            foreach (float x in new[] { -4.5f, 0f, 4.5f })
            {
                F.Cubo(g, "Fio", "Grafite", F.V(x, 4.75f, 1.2f), F.V(.03f, .6f, .03f));
                F.M(g, "Cupula", "Funil", "Mostarda", F.V(x, 4.35f, 1.2f), F.V(.7f, .3f, .7f), F.V(180, 0, 0));
                F.Esfera(g, "Lampada", "Luz_Amarela", F.V(x, 4.18f, 1.2f), F.V(.22f, .22f, .22f));
            }

            // Quadro de ferramentas (na parede da esquerda, acima do trabalhador).
            var q = F.Grupo("Quadro_De_Ferramentas", g, F.V(-6.1f, 2.85f, 3.04f)).transform;
            F.Cubo(q, "Painel", "Marrom_Claro", F.V(0, 0, 0), F.V(2.2f, 1.3f, .05f));
            F.Cubo(q, "Moldura", "Marrom", F.V(0, 0, .01f), F.V(2.3f, 1.4f, .03f));
            for (int i = 0; i < 4; i++)
            {
                float x = -.75f + i * .5f;
                // chave inglesa, martelo, chave de fenda, alicate (formas simples, de pé na parede)
                string cor = i % 2 == 0 ? "Cinza_Escuro" : "Vermelho";
                F.Cubo(q, "Cabo", cor, F.V(x, -.15f, -.05f), F.V(.08f, .55f, .04f));
                F.Cubo(q, "Ponta", i == 1 ? "Grafite" : "Cinza", F.V(x, .2f, -.05f), F.V(i == 1 ? .28f : .14f, .14f, .05f));
            }
            F.Cilindro(q, "Rolo_De_Fita", "Mostarda", F.V(.85f, .35f, -.06f), F.V(.25f, .04f, .25f), F.V(90, 0, 0));
            F.Cilindro(q, "Mangueira", "Verde", F.V(.85f, -.25f, -.06f), F.V(.4f, .05f, .4f), F.V(90, 0, 0));

            // Relógio no meio da parede, entre as janelas e acima dos quadros.
            F.Cilindro(g, "Relogio", "Branco", F.V(0, 3.85f, 3.05f), F.V(.55f, .03f, .55f), F.V(90, 0, 0));
            F.Cilindro(g, "Aro", "Vermelho", F.V(0, 3.85f, 3.07f), F.V(.62f, .02f, .62f), F.V(90, 0, 0));
            F.Cubo(g, "Ponteiro_Hora", "Grafite", F.V(0, 3.92f, 3f), F.V(.04f, .16f, .02f));
            F.Cubo(g, "Ponteiro_Minuto", "Grafite", F.V(.08f, 3.85f, 3f), F.V(.2f, .03f, .02f));

            // Pilha de pneus e carrinho de ferramentas vermelho (canto direito, perto do barril).
            for (int i = 0; i < 3; i++)
            {
                F.Cilindro(g, "Pneu", "Grafite", F.V(6.5f, .14f + i * .27f, .7f), F.V(.8f, .13f, .8f));
                F.Cilindro(g, "Aro_Do_Pneu", "Cinza", F.V(6.5f, .15f + i * .27f, .7f), F.V(.36f, .135f, .36f));
            }
            var c = F.Grupo("Carrinho_De_Ferramentas", g, F.V(4.9f, 0, 2.6f)).transform;
            F.Caixa(c, "Corpo", "Vermelho", 0, .12f, 0, .9f, .9f, .5f);
            for (int i = 0; i < 3; i++)
                F.Cubo(c, "Gaveta", "Cinza_Claro", F.V(0, .35f + i * .27f, -.26f), F.V(.6f, .04f, .02f));
            foreach (float x in new[] { -.35f, .35f })
                F.Cilindro(c, "Rodinha", "Grafite", F.V(x, .06f, 0), F.V(.12f, .03f, .12f), F.V(0, 0, 90));
            // Macaco hidráulico no chão, do lado esquerdo.
            var m = F.Grupo("Macaco", g, F.V(-4.6f, 0, -.2f), F.V(0, 25, 0)).transform;
            F.Caixa(m, "Base", "Vermelho", 0, 0, 0, .35f, .14f, .8f);
            F.Barra(m, "Alavanca", "Cinza_Escuro", F.V(0, .1f, -.35f), F.V(0, .55f, -.85f), .05f);
            F.Cilindro(m, "Prato", "Cinza", F.V(0, .2f, .3f), F.V(.22f, .04f, .22f));
        }

        static Transform NovaPeca(TipoDePeca tipo, Vector3 giroNaPrateleira, out GameObject raiz)
        {
            raiz = F.Grupo("Peca_" + tipo);
            var visual = F.Grupo("Visual", raiz.transform).transform;
            var peca = raiz.AddComponent<PecaDaOficina>();
            peca.tipo = tipo;
            peca.visual = visual;
            peca.giroNaPrateleira = giroNaPrateleira;
            // Área de toque folgada, pra dedo pequeno acertar.
            raiz.AddComponent<BoxCollider>().size = new Vector3(.95f, .85f, .95f);
            return visual;
        }

        static void GerarPrefabs()
        {
            GameObject raiz;
            Vector3 deitada = F.V(0, 0, 90);

            var v = NovaPeca(TipoDePeca.Roda, F.V(0, 90, 0), out raiz);
            F.Cilindro(v, "Pneu", "Grafite", Vector3.zero, F.V(.76f, .16f, .76f), deitada);
            F.Cilindro(v, "Aro", "Cinza_Claro", Vector3.zero, F.V(.44f, .18f, .44f), deitada);
            F.Cubo(v, "Raio", "Cinza", Vector3.zero, F.V(.19f, .05f, .5f));
            F.Cilindro(v, "Eixo", "Mostarda", Vector3.zero, F.V(.14f, .22f, .14f), deitada);
            // Roda de bicicleta e de moto: fina, com raios.
            var fina = DuasRodas(raiz);
            F.Cilindro(fina, "Pneu", "Grafite", Vector3.zero, F.V(.78f, .07f, .78f), deitada);
            F.Cilindro(fina, "Aro", "Cinza_Claro", Vector3.zero, F.V(.66f, .075f, .66f), deitada);
            for (int i = 0; i < 3; i++)
                F.Cubo(fina, "Raio_" + (i + 1), "Cinza_Escuro", Vector3.zero, F.V(.085f, .62f, .03f), F.V(i * 60f, 0, 0));
            F.Cilindro(fina, "Eixo", "Mostarda", Vector3.zero, F.V(.12f, .12f, .12f), deitada);
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.Pedal, F.V(0, 90, 0), out raiz);
            F.Cilindro(v, "Coroa", "Cinza_Escuro", Vector3.zero, F.V(.3f, .05f, .3f), deitada);
            F.Cubo(v, "Braco", "Cinza", Vector3.zero, F.V(.05f, .42f, .07f));
            F.Cubo(v, "Pedal_1", "Mostarda", F.V(.13f, .2f, 0), F.V(.24f, .05f, .13f));
            F.Cubo(v, "Pedal_2", "Mostarda", F.V(-.13f, -.2f, 0), F.V(.24f, .05f, .13f));
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.Motor, F.V(0, 25, 0), out raiz);
            F.Cubo(v, "Bloco", "Cinza_Escuro", Vector3.zero, F.V(.46f, .34f, .42f));
            F.Cubo(v, "Cabecote", "Vermelho", F.V(0, .22f, 0), F.V(.4f, .12f, .36f));
            F.Cilindro(v, "Vela_1", "Cinza_Claro", F.V(-.1f, .32f, 0), F.V(.07f, .12f, .07f));
            F.Cilindro(v, "Vela_2", "Cinza_Claro", F.V(.1f, .32f, 0), F.V(.07f, .12f, .07f));
            F.Cilindro(v, "Escapamento", "Cinza", F.V(.12f, -.08f, -.32f), F.V(.1f, .34f, .1f), F.V(90, 0, 0));
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.MotorEletrico, F.V(0, 25, 0), out raiz);
            F.Cilindro(v, "Corpo", "Petroleo", Vector3.zero, F.V(.4f, .46f, .4f), F.V(90, 0, 0));
            F.Cilindro(v, "Tampa_1", "Cinza_Claro", F.V(0, 0, .25f), F.V(.43f, .06f, .43f), F.V(90, 0, 0));
            F.Cilindro(v, "Tampa_2", "Cinza_Claro", F.V(0, 0, -.25f), F.V(.43f, .06f, .43f), F.V(90, 0, 0));
            F.Cubo(v, "Caixa_De_Ligacao", "Mostarda", F.V(0, .23f, 0), F.V(.2f, .1f, .2f));
            F.Barra(v, "Fio", "Mostarda", F.V(0, .27f, 0), F.V(.2f, .36f, .12f), .04f);
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.Trilho, F.V(0, 0, 0), out raiz);
            foreach (float z in new[] { -.14f, .14f })
                F.Barra(v, "Trilho", "Cinza_Escuro", F.V(-.45f, 0, z), F.V(.45f, 0, z), .06f);
            foreach (float x in new[] { -.3f, 0f, .3f })
                F.Cubo(v, "Dormente", "Marrom", F.V(x, -.04f, 0), F.V(.09f, .04f, .44f));
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.Sirene, Vector3.zero, out raiz);
            F.Cilindro(v, "Base", "Grafite", F.V(0, -.1f, 0), F.V(.34f, .08f, .34f));
            F.Esfera(v, "Cupula", "Luz_Vermelha", F.V(0, .03f, 0), F.V(.28f, .3f, .28f));
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.Luzes, Vector3.zero, out raiz);
            F.Cubo(v, "Barra", "Grafite", F.V(0, -.08f, 0), F.V(.7f, .08f, .2f));
            F.Cubo(v, "Luz_1", "Luz_Vermelha", F.V(-.2f, .03f, 0), F.V(.26f, .14f, .16f));
            F.Cubo(v, "Luz_2", "Luz_Amarela", F.V(.2f, .03f, 0), F.V(.26f, .14f, .16f));
            // Em bicicleta e moto: um farol redondo.
            var farol = DuasRodas(raiz);
            F.Cilindro(farol, "Corpo", "Grafite", Vector3.zero, F.V(.24f, .16f, .24f), F.V(90, 0, 0));
            F.Cilindro(farol, "Lente", "Luz_Amarela", F.V(0, 0, .08f), F.V(.19f, .03f, .19f), F.V(90, 0, 0));
            F.Cubo(farol, "Suporte", "Cinza_Escuro", F.V(0, -.13f, -.02f), F.V(.05f, .14f, .05f));
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.Carroceria, F.V(0, 20, 0), out raiz);
            F.Cubo(v, "Fundo", "Mostarda", F.V(0, -.17f, 0), F.V(.8f, .06f, .6f));
            foreach (float z in new[] { -.275f, .275f })
                F.Cubo(v, "Lateral", "Mostarda_Clara", F.V(0, 0, z), F.V(.8f, .34f, .05f));
            foreach (float x in new[] { -.375f, .375f })
                F.Cubo(v, "Tampa", "Mostarda_Clara", F.V(x, 0, 0), F.V(.05f, .34f, .6f));
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.Engate, F.V(0, 90, 0), out raiz);
            F.Barra(v, "Haste", "Cinza_Escuro", F.V(0, 0, .2f), F.V(0, 0, -.2f), .07f);
            F.Esfera(v, "Bola", "Cinza_Claro", F.V(0, .07f, -.22f), F.V(.16f, .16f, .16f));
            F.Cubo(v, "Placa", "Cinza_Escuro", F.V(0, 0, .2f), F.V(.22f, .22f, .05f));
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.Assento, F.V(0, 160, 0), out raiz);
            F.Cubo(v, "Assento", "Terracota", F.V(0, -.12f, 0), F.V(.42f, .1f, .42f));
            F.Cubo(v, "Encosto", "Terracota", F.V(0, .1f, -.19f), F.V(.42f, .42f, .08f));
            // Em bicicleta e moto o assento é um selim: comprido, estreito, com bico na frente.
            var selim = DuasRodas(raiz);
            F.Esfera(selim, "Selim", "Grafite", F.V(0, .03f, -.06f), F.V(.26f, .1f, .34f));
            F.Esfera(selim, "Bico", "Grafite", F.V(0, .03f, .14f), F.V(.13f, .08f, .26f));
            F.Barra(selim, "Canote", "Cinza", F.V(0, 0, -.03f), F.V(0, -.24f, -.07f), .045f);
            // Na motoneta: banco largo e plano, com o apoio dos pés na frente (senta-se com as pernas juntas).
            var banco = Variante(raiz, "Visual_Motoneta");
            raiz.GetComponent<PecaDaOficina>().visualDeMotoneta = banco;
            F.Cubo(banco, "Banco", "Grafite", F.V(0, .04f, -.12f), F.V(.34f, .1f, .5f));
            F.Esfera(banco, "Frente_Do_Banco", "Grafite", F.V(0, .04f, .13f), F.V(.34f, .1f, .16f));
            F.Cubo(banco, "Apoio_Dos_Pes", "Cinza", F.V(0, -.22f, .38f), F.V(.36f, .04f, .34f));
            F.Barra(banco, "Coluna", "Cinza", F.V(0, -.02f, .1f), F.V(0, -.2f, .28f), .05f);
            F.Salvar(raiz, Categoria);

            // "Parte de cima para cobrir" (quadriciclo): um teto em quatro colunas.
            v = NovaPeca(TipoDePeca.Cobertura, F.V(0, 25, 0), out raiz);
            F.Cubo(v, "Teto", "Vermelho", F.V(0, .12f, 0), F.V(.9f, .07f, .8f));
            foreach (float x in new[] { -.4f, .4f })
                foreach (float z in new[] { -.35f, .35f })
                    F.Barra(v, "Coluna", "Cinza_Escuro", F.V(x, .1f, z), F.V(x, -.3f, z), .05f);
            F.Salvar(raiz, Categoria);

            // Peças que nenhum veículo de rua tem: são as erradas.
            v = NovaPeca(TipoDePeca.Asa, F.V(0, 0, 0), out raiz);
            F.Cubo(v, "Asa", "Cinza_Claro", F.V(-.04f, 0, 0), F.V(.74f, .07f, .36f), F.V(0, 0, 6));
            F.Cubo(v, "Ponta", "Vermelho", F.V(.38f, .04f, 0), F.V(.16f, .08f, .3f), F.V(0, 0, 6));
            F.Cubo(v, "Encaixe", "Cinza_Escuro", F.V(-.42f, -.04f, 0), F.V(.1f, .14f, .4f));
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.Helice, F.V(0, 0, 0), out raiz);
            for (int i = 0; i < 3; i++)
            {
                var pa = F.Grupo("Giro_" + (i + 1), v, Vector3.zero, F.V(0, 0, i * 120f)).transform;
                F.Cubo(pa, "Pa", "Mostarda", F.V(0, .22f, 0), F.V(.13f, .4f, .04f));
            }
            F.Esfera(v, "Cubo_Central", "Cinza_Escuro", Vector3.zero, F.V(.18f, .18f, .18f));
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.Ancora, F.V(0, 0, 0), out raiz);
            F.Barra(v, "Haste", "Cinza_Escuro", F.V(0, -.28f, 0), F.V(0, .26f, 0), .07f);
            F.Barra(v, "Travessa", "Cinza_Escuro", F.V(-.16f, .18f, 0), F.V(.16f, .18f, 0), .06f);
            F.Barra(v, "Braco_1", "Cinza_Escuro", F.V(0, -.28f, 0), F.V(-.26f, -.1f, 0), .07f);
            F.Barra(v, "Braco_2", "Cinza_Escuro", F.V(0, -.28f, 0), F.V(.26f, -.1f, 0), .07f);
            F.Esfera(v, "Argola", "Cinza", F.V(0, .32f, 0), F.V(.14f, .14f, .06f));
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.VelaDeBarco, F.V(0, 0, 0), out raiz);
            F.Barra(v, "Mastro", "Marrom", F.V(-.14f, -.36f, 0), F.V(-.14f, .38f, 0), .05f);
            // Vela em degraus, pra parecer um triângulo.
            for (int i = 0; i < 5; i++)
                F.Cubo(v, "Vela_" + (i + 1), "Branco", F.V(-.11f + (.46f - i * .09f) / 2f, -.24f + i * .13f, 0), F.V(.46f - i * .09f, .13f, .03f));
            F.Cubo(v, "Listra", "Vermelho", F.V(.1f, -.24f, .001f), F.V(.44f, .04f, .035f));
            F.Salvar(raiz, Categoria);

            v = NovaPeca(TipoDePeca.Remo, F.V(0, 0, 0), out raiz);
            F.Barra(v, "Cabo", "Marrom_Claro", F.V(-.34f, .22f, 0), F.V(.16f, -.1f, 0), .05f);
            F.Cubo(v, "Pa", "Marrom", F.V(.27f, -.17f, 0), F.V(.3f, .14f, .03f), F.V(0, 0, -32));
            F.Salvar(raiz, Categoria);

            // Chassi de duas rodas em linha: uma barra neutra, sem nada que a definição não diga.
            var chassi = NovoChassi("DuasRodas", out var encaixes);
            F.Barra(chassi, "Quadro", "Petroleo", F.V(0, .38f, -.75f), F.V(0, .38f, .75f), .1f);
            F.Barra(chassi, "Reforco_1", "Petroleo_Claro", F.V(0, .38f, -.45f), F.V(0, .72f, .1f), .07f);
            F.Barra(chassi, "Reforco_2", "Petroleo_Claro", F.V(0, .72f, .1f), F.V(0, .38f, .6f), .07f);
            Encaixe(chassi, encaixes, TipoDePeca.Roda, F.V(0, .38f, .75f));
            Encaixe(chassi, encaixes, TipoDePeca.Roda, F.V(0, .38f, -.75f));
            Encaixe(chassi, encaixes, TipoDePeca.Pedal, F.V(0, .38f, .08f));
            Encaixe(chassi, encaixes, TipoDePeca.Motor, F.V(0, .6f, -.28f));
            Encaixe(chassi, encaixes, TipoDePeca.Assento, F.V(0, .98f, -.32f));
            Encaixe(chassi, encaixes, TipoDePeca.Sirene, F.V(0, .92f, .38f));
            Encaixe(chassi, encaixes, TipoDePeca.Luzes, F.V(0, .86f, .78f), 90);
            Encaixe(chassi, encaixes, TipoDePeca.Carroceria, F.V(0, 1.05f, -.85f), 90);
            Encaixe(chassi, encaixes, TipoDePeca.Engate, F.V(0, .38f, -1.2f));
            Encaixe(chassi, encaixes, TipoDePeca.Trilho, F.V(0, .03f, 0), 90);
            Encaixe(chassi, encaixes, TipoDePeca.Cobertura, F.V(0, 1.4f, 0));
            SalvarChassi(chassi, encaixes);

            // Chassi de plataforma: 4 rodas nos cantos e mais 2 encaixes no meio (pra "quatro ou mais" e "seis rodas").
            chassi = NovoChassi("QuatroRodas", out encaixes);
            F.Cubo(chassi, "Plataforma", "Petroleo", F.V(0, .45f, 0), F.V(.9f, .14f, 2.1f));
            foreach (float z in new[] { -.7f, 0f, .7f })
                F.Cubo(chassi, "Eixo", "Petroleo_Claro", F.V(0, .38f, z), F.V(1.14f, .1f, .12f));
            // Ordem: os 4 cantos primeiro (frente, trás), depois o par do meio; em cada eixo, o lado de cá (+X) antes.
            foreach (float z in new[] { .7f, -.7f, 0f })
                foreach (float x in new[] { .62f, -.62f })
                    Encaixe(chassi, encaixes, TipoDePeca.Roda, F.V(x, .38f, z));
            Encaixe(chassi, encaixes, TipoDePeca.Motor, F.V(0, .7f, .68f));
            Encaixe(chassi, encaixes, TipoDePeca.Pedal, F.V(0, .74f, .1f));
            Encaixe(chassi, encaixes, TipoDePeca.Assento, F.V(0, .7f, -.12f));
            Encaixe(chassi, encaixes, TipoDePeca.Carroceria, F.V(0, .72f, -.68f), 90);
            Encaixe(chassi, encaixes, TipoDePeca.Sirene, F.V(0, .66f, .3f));
            Encaixe(chassi, encaixes, TipoDePeca.Luzes, F.V(0, .62f, .98f));
            Encaixe(chassi, encaixes, TipoDePeca.Engate, F.V(0, .45f, -1.25f));
            Encaixe(chassi, encaixes, TipoDePeca.Trilho, F.V(0, .03f, 0), 90);
            Encaixe(chassi, encaixes, TipoDePeca.Cobertura, F.V(0, 1.3f, 0));
            SalvarChassi(chassi, encaixes);

            // Chassi de um eixo só (reboque): uma roda de cada lado e o engate na frente.
            chassi = NovoChassi("UmEixo", out encaixes);
            F.Cubo(chassi, "Plataforma", "Petroleo", F.V(0, .45f, 0), F.V(.9f, .14f, 1.5f));
            F.Cubo(chassi, "Eixo", "Petroleo_Claro", F.V(0, .38f, -.1f), F.V(1.14f, .1f, .12f));
            foreach (float x in new[] { .62f, -.62f })
                Encaixe(chassi, encaixes, TipoDePeca.Roda, F.V(x, .38f, -.1f));
            Encaixe(chassi, encaixes, TipoDePeca.Engate, F.V(0, .45f, .95f));
            Encaixe(chassi, encaixes, TipoDePeca.Motor, F.V(0, .7f, .3f));
            Encaixe(chassi, encaixes, TipoDePeca.Carroceria, F.V(0, .72f, -.1f), 90);
            Encaixe(chassi, encaixes, TipoDePeca.Luzes, F.V(0, .6f, -.75f));
            SalvarChassi(chassi, encaixes);

            var bancada = F.Grupo("Bancada");
            F.Cubo(bancada.transform, "Tampo", "Marrom_Claro", F.V(0, .89f, 0), F.V(3.6f, .12f, 1.5f));
            F.Cubo(bancada.transform, "Borda", "Mostarda", F.V(0, .89f, -.76f), F.V(3.6f, .13f, .04f));
            F.Cubo(bancada.transform, "Prateleira_De_Baixo", "Marrom", F.V(0, .3f, 0), F.V(3.2f, .06f, 1.2f));
            foreach (float x in new[] { -1.65f, 1.65f })
                foreach (float z in new[] { -.6f, .6f })
                    F.Cubo(bancada.transform, "Perna", "Cinza_Escuro", F.V(x, .415f, z), F.V(.14f, .83f, .14f));
            F.Salvar(bancada, Categoria);

            var prateleira = F.Grupo("Prateleira");
            F.Cubo(prateleira.transform, "Fundo", "Petroleo", F.V(0, 1.45f, .36f), F.V(2.6f, 2.9f, .06f));
            foreach (float y in new[] { .26f, 1.01f, 1.76f })
                F.Cubo(prateleira.transform, "Tabua", "Marrom_Claro", F.V(0, y, 0), F.V(2.6f, .08f, .72f));
            foreach (float x in new[] { -1.26f, 1.26f })
                foreach (float z in new[] { -.32f, .32f })
                    F.Cubo(prateleira.transform, "Coluna", "Cinza_Escuro", F.V(x, 1.45f, z), F.V(.1f, 2.9f, .1f));
            F.Salvar(prateleira, Categoria);

            GerarVeiculosDeFormas();

            // Não existe modelo de bicicleta elétrica nos pacotes: é a outra bicicleta com o motor elétrico preso no quadro.
            var bicicletaEletrica = F.Grupo("Pronto_Bicicleta_Eletrica");
            P(bicicletaEletrica.transform, "Carros/Bicicleta_Q2", Vector3.zero);
            var motorDaBicicleta = P(bicicletaEletrica.transform, "Oficina/Peca_MotorEletrico", F.V(0, .55f, -.05f), 0, .55f);
            Object.DestroyImmediate(motorDaBicicleta.GetComponent<Collider>());
            Object.DestroyImmediate(motorDaBicicleta.GetComponent<PecaDaOficina>());
            F.Salvar(bicicletaEletrica, Categoria);
            AssetDatabase.SaveAssets();
        }

        static Transform Variante(GameObject raiz, string nome)
        {
            var visual = F.Grupo(nome, raiz.transform);
            visual.SetActive(false);
            return visual.transform;
        }

        // Roda de formas simples, deitada no eixo X, pros veículos que não têm modelo nos pacotes.
        static void RodaSimples(Transform pai, Vector3 pos, float diametro, float largura)
        {
            F.Cilindro(pai, "Roda", "Grafite", pos, F.V(diametro, largura, diametro), F.V(0, 0, 90));
            F.Cilindro(pai, "Calota", "Cinza_Claro", pos, F.V(diametro * .5f, largura + .02f, diametro * .5f), F.V(0, 0, 90));
        }

        // Veículos que os pacotes de arte não têm: feitos de formas simples, com a frente em +Z.
        // São provisórios: quando aparecer um modelo melhor, é só trocar o "modeloPronto" da receita.
        static void GerarVeiculosDeFormas()
        {
            var q = F.Grupo("Pronto_Quadriciclo").transform;
            foreach (float x in new[] { -.52f, .52f })
                foreach (float z in new[] { -.62f, .62f })
                    RodaSimples(q, F.V(x, .36f, z), .72f, .3f);
            F.Cubo(q, "Corpo", "Verde", F.V(0, .62f, 0), F.V(.56f, .3f, 1.5f));
            F.Cubo(q, "Para_Lama_Da_Frente", "Verde_Escuro", F.V(0, .8f, .62f), F.V(1.2f, .07f, .5f));
            F.Cubo(q, "Para_Lama_De_Tras", "Verde_Escuro", F.V(0, .8f, -.62f), F.V(1.2f, .07f, .5f));
            F.Cubo(q, "Banco", "Grafite", F.V(0, .84f, -.2f), F.V(.4f, .12f, .6f));
            F.Barra(q, "Coluna_Do_Guidao", "Cinza_Escuro", F.V(0, .78f, .35f), F.V(0, 1.12f, .25f), .06f);
            F.Barra(q, "Guidao", "Cinza_Escuro", F.V(-.4f, 1.12f, .25f), F.V(.4f, 1.12f, .25f), .06f);
            F.Cubo(q, "Farol", "Luz_Amarela", F.V(0, .72f, .78f), F.V(.3f, .12f, .05f));
            F.Salvar(q.gameObject, Categoria);

            var rb = F.Grupo("Pronto_Reboque").transform;
            foreach (float x in new[] { -.72f, .72f })
                RodaSimples(rb, F.V(x, .36f, -.15f), .72f, .24f);
            F.Cubo(rb, "Assoalho", "Mostarda", F.V(0, .55f, -.1f), F.V(1.2f, .1f, 1.7f));
            foreach (float z in new[] { -.92f, .72f })
                F.Cubo(rb, "Tampa", "Mostarda_Clara", F.V(0, .78f, z), F.V(1.2f, .36f, .06f));
            foreach (float x in new[] { -.57f, .57f })
                F.Cubo(rb, "Lateral", "Mostarda_Clara", F.V(x, .78f, -.1f), F.V(.06f, .36f, 1.7f));
            F.Barra(rb, "Lanca", "Cinza_Escuro", F.V(0, .5f, .72f), F.V(0, .46f, 1.45f), .08f);
            F.Esfera(rb, "Ponta_Do_Engate", "Cinza_Claro", F.V(0, .46f, 1.48f), F.V(.16f, .16f, .16f));
            F.Salvar(rb.gameObject, Categoria);

            var ct = F.Grupo("Pronto_Caminhao_Trator").transform;
            foreach (float x in new[] { -.66f, .66f })
                foreach (float z in new[] { 1.05f, -.95f })
                    RodaSimples(ct, F.V(x, .44f, z), .88f, .3f);
            F.Cubo(ct, "Longarina", "Cinza_Escuro", F.V(0, .6f, -.1f), F.V(.9f, .2f, 3f));
            F.Cubo(ct, "Cabine", "Petroleo", F.V(0, 1.4f, .95f), F.V(1.4f, 1.4f, 1.1f));
            F.Cubo(ct, "Para_Brisa", "Vidro", F.V(0, 1.65f, 1.51f), F.V(1.2f, .5f, .04f));
            F.Cubo(ct, "Para_Choque", "Cinza_Claro", F.V(0, .6f, 1.55f), F.V(1.45f, .22f, .12f));
            foreach (float x in new[] { -.5f, .5f })
                F.Cubo(ct, "Farol", "Luz_Amarela", F.V(x, .95f, 1.51f), F.V(.22f, .14f, .04f));
            F.Cilindro(ct, "Prato_Do_Engate", "Cinza", F.V(0, .74f, -.85f), F.V(.8f, .08f, .8f));
            F.Barra(ct, "Chamine", "Cinza_Claro", F.V(.6f, .7f, .3f), F.V(.6f, 2.3f, .3f), .1f);
            F.Salvar(ct.gameObject, Categoria);

            var mt = F.Grupo("Pronto_Motoneta").transform;
            foreach (float z in new[] { -.62f, .62f })
                RodaSimples(mt, F.V(0, .24f, z), .48f, .12f);
            F.Cubo(mt, "Assoalho", "Petroleo_Claro", F.V(0, .27f, .05f), F.V(.3f, .07f, .7f));
            F.Cubo(mt, "Escudo_Da_Frente", "Petroleo_Claro", F.V(0, .62f, .46f), F.V(.36f, .72f, .07f), F.V(-12, 0, 0));
            F.Cubo(mt, "Corpo_De_Tras", "Petroleo_Claro", F.V(0, .5f, -.45f), F.V(.32f, .4f, .62f));
            F.Cubo(mt, "Banco", "Grafite", F.V(0, .74f, -.4f), F.V(.3f, .09f, .62f));
            F.Barra(mt, "Coluna_Do_Guidao", "Cinza_Escuro", F.V(0, .5f, .55f), F.V(0, 1.08f, .42f), .05f);
            F.Barra(mt, "Guidao", "Cinza_Escuro", F.V(-.28f, 1.08f, .42f), F.V(.28f, 1.08f, .42f), .05f);
            F.Cilindro(mt, "Farol", "Luz_Amarela", F.V(0, .98f, .5f), F.V(.16f, .05f, .16f), F.V(90, 0, 0));
            F.Salvar(mt.gameObject, Categoria);
        }

        // Segundo formato da mesma peça, usado quando o veículo em montagem tem duas rodas.
        static Transform DuasRodas(GameObject raiz)
        {
            var visual = F.Grupo("Visual_DuasRodas", raiz.transform);
            raiz.GetComponent<PecaDaOficina>().visualDeDuasRodas = visual.transform;
            visual.SetActive(false);
            return visual.transform;
        }

        static Transform NovoChassi(string id, out List<EncaixeDaOficina> encaixes)
        {
            var raiz = F.Grupo("Chassi_" + id);
            raiz.AddComponent<ChassiDaOficina>().id = id;
            encaixes = new List<EncaixeDaOficina>();
            return raiz.transform;
        }

        static void Encaixe(Transform chassi, List<EncaixeDaOficina> lista, TipoDePeca tipo, Vector3 pos, float rotY = 0f)
        {
            var go = F.Grupo("Encaixe_" + tipo + "_" + (lista.Count(e => e.tipo == tipo) + 1), chassi, pos, F.V(0, rotY, 0));
            var encaixe = go.AddComponent<EncaixeDaOficina>();
            encaixe.tipo = tipo;
            var marcador = F.Esfera(go.transform, "Marcador", "Mostarda_Clara", Vector3.zero, F.V(.17f, .17f, .17f));
            SemSombra(marcador);
            marcador.SetActive(false);
            encaixe.marcador = marcador;
            lista.Add(encaixe);
        }

        static void SalvarChassi(Transform chassi, List<EncaixeDaOficina> encaixes)
        {
            chassi.GetComponent<ChassiDaOficina>().encaixes = encaixes.ToArray();
            F.Salvar(chassi.gameObject, Categoria);
        }

        // ------------------------------------------------------------------ interface

        static OficinaHUD Hud(Transform canvas, OficinaPista pista, out Button botaoOuvir, out Button botaoDica)
        {
            var raiz = Tela("HUD", canvas);
            var hud = raiz.gameObject.AddComponent<OficinaHUD>();
            var voltarAoHub = raiz.gameObject.AddComponent<VoltarAoHub>();

            // Verbete: palavra e definição (em cima, no meio). É o que a criança lê pra montar.
            var definicao = Imagem("Painel_Da_Definicao", raiz, arredondado, C("Branco", .96f), new Vector2(60, -150), new Vector2(1120, 270));
            TravessiaConstrutor.Ancorar(definicao, new Vector2(.5f, 1));
            hud.painelDaDefinicao = definicao.gameObject;
            // À esquerda, a ilustração do glossário: é ela que mostra como o veículo é.
            var moldura = Imagem("Moldura_Da_Ilustracao", definicao.transform, arredondado, C("Vidro"), new Vector2(-420, 0), new Vector2(250, 236));
            hud.molduraDaIlustracao = moldura.gameObject;
            hud.ilustracao = Imagem("Ilustracao", moldura.transform, null, Color.white, Vector2.zero, new Vector2(230, 216));
            hud.ilustracao.preserveAspect = true;
            hud.textoDaPalavra = Texto("Palavra", definicao.transform, "", 64, C("Petroleo"), new Vector2(130, 88), new Vector2(810, 76));
            Imagem("Sublinhado", definicao.transform, arredondado, C("Mostarda"), new Vector2(130, 44), new Vector2(260, 10));
            hud.textoDaDefinicao = Texto("Definicao", definicao.transform, "", 40, C("Grafite"), new Vector2(130, -48), new Vector2(810, 160), FontStyle.Normal);
            hud.textoDaDefinicao.resizeTextForBestFit = true; hud.textoDaDefinicao.resizeTextMinSize = 28; hud.textoDaDefinicao.resizeTextMaxSize = 40;
            hud.textoDaDefinicao.verticalOverflow = VerticalWrapMode.Truncate;

            // Nível e os três critérios das estrelas (em cima, à esquerda).
            var cartao = Imagem("Painel_Do_Nivel", raiz, arredondado, C("Branco", .92f), new Vector2(240, -115), new Vector2(430, 200));
            TravessiaConstrutor.Ancorar(cartao, new Vector2(0, 1));
            hud.textoDoNivel = Texto("Nivel", cartao.transform, "", 30, C("Petroleo"), new Vector2(0, 66), new Vector2(410, 44));
            hud.textoDoNivel.resizeTextForBestFit = true; hud.textoDoNivel.resizeTextMinSize = 20; hud.textoDoNivel.resizeTextMaxSize = 30;
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

            // Tempo (só nos níveis com relógio) e dica, logo abaixo da definição.
            var tempo = Imagem("Painel_Do_Tempo", raiz, arredondado, C("Petroleo_Escuro", .9f), new Vector2(60, -312), new Vector2(720, 40));
            TravessiaConstrutor.Ancorar(tempo, new Vector2(.5f, 1));
            var area = Ret("Area", tempo.transform, Vector2.zero, Vector2.zero);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(5, 5); area.offsetMax = new Vector2(-5, -5);
            var cheio = Imagem("Cheio", area, arredondado, C("Mostarda"), Vector2.zero, Vector2.zero);
            var rtCheio = (RectTransform)cheio.transform;
            rtCheio.anchorMin = Vector2.zero; rtCheio.anchorMax = Vector2.one; rtCheio.offsetMin = rtCheio.offsetMax = Vector2.zero;
            hud.painelDoTempo = tempo.gameObject;
            hud.barraDeTempo = rtCheio;

            var dica = Imagem("Painel_Da_Dica", raiz, arredondado, C("Mostarda_Clara", .97f), new Vector2(60, -374), new Vector2(900, 68));
            TravessiaConstrutor.Ancorar(dica, new Vector2(.5f, 1));
            hud.painelDaDica = dica.gameObject;
            hud.textoDaDica = Texto("Texto", dica.transform, "", 36, C("Grafite"), Vector2.zero, new Vector2(870, 60));
            hud.textoDaDica.resizeTextForBestFit = true; hud.textoDaDica.resizeTextMinSize = 24; hud.textoDaDica.resizeTextMaxSize = 36;

            // Recado do momento (embaixo, no meio).
            var recado = Imagem("Painel_Do_Recado", raiz, arredondado, C("Petroleo", .92f), new Vector2(0, 76), new Vector2(980, 88));
            TravessiaConstrutor.Ancorar(recado, new Vector2(.5f, 0));
            hud.textoDoRecado = Texto("Texto", recado.transform, "", 38, C("Branco"), Vector2.zero, new Vector2(940, 78));
            hud.textoDoRecado.resizeTextForBestFit = true; hud.textoDoRecado.resizeTextMinSize = 26; hud.textoDoRecado.resizeTextMaxSize = 38;

            // Botões da montagem: ouvir a definição e pedir dica.
            var montagem = Tela("Controles_Da_Montagem", raiz);
            botaoOuvir = Botao("Botao_Ouvir", montagem, "Ouvir", "Petroleo_Claro", new Vector2(200, 120), new Vector2(320, 150), 58);
            TravessiaConstrutor.Ancorar(botaoOuvir, new Vector2(0, 0));
            botaoDica = Botao("Botao_Dica", montagem, "Dica", "Mostarda", new Vector2(-200, 120), new Vector2(320, 150), 58);
            TravessiaConstrutor.Ancorar(botaoDica, new Vector2(1, 0));
            hud.controlesDaMontagem = montagem.gameObject;
            hud.botaoOuvir = botaoOuvir.gameObject;
            hud.botaoDica = botaoDica.gameObject;

            // Botões do teste na pista (de segurar). O teclado faz o mesmo.
            var teste = Tela("Controles_Do_Teste", raiz);
            var volante = Tela("Volante", teste);
            var esquerda = Botao("Botao_Esquerda", volante, "", "Petroleo_Claro", new Vector2(140, 150), new Vector2(190, 190), 0, S("ui_seta_voltar"), circulo);
            TravessiaConstrutor.Ancorar(esquerda, new Vector2(0, 0));
            var direita = Botao("Botao_Direita", volante, "", "Petroleo_Claro", new Vector2(360, 150), new Vector2(190, 190), 0, S("ui_seta_voltar"), circulo);
            TravessiaConstrutor.Ancorar(direita, new Vector2(0, 0));
            direita.transform.Find("Frente/Icone").localScale = new Vector3(-1, 1, 1);
            var acelerar = Botao("Botao_Acelerar", teste, "Acelerar", "Verde_Escuro", new Vector2(-210, 140), new Vector2(340, 170), 44, S("ui_seta_cima"));
            TravessiaConstrutor.Ancorar(acelerar, new Vector2(1, 0));
            Segurar(esquerda, pista, -1f, false);
            Segurar(direita, pista, 1f, false);
            Segurar(acelerar, pista, 0f, true);
            var engatar = Botao("Botao_Engatar", teste, "Engatar", "Mostarda", new Vector2(0, 290), new Vector2(460, 180), 68);
            TravessiaConstrutor.Ancorar(engatar, new Vector2(.5f, 0));
            engatar.gameObject.SetActive(false);
            hud.botoesDeVolante = volante.gameObject;
            hud.botaoAcelerar = acelerar.gameObject;
            hud.botaoEngatar = engatar.gameObject;
            teste.gameObject.SetActive(false);
            hud.controlesDoTeste = teste.gameObject;

            // Balão de explicação: aparece perto da peça que voltou pra prateleira.
            var balao = Imagem("Balao", raiz, arredondado, C("Grafite"), Vector2.zero, new Vector2(640, 176));
            Imagem("Papel", balao.transform, arredondado, C("Mostarda_Clara"), Vector2.zero, new Vector2(628, 164));
            hud.textoDoBalao = Texto("Texto", balao.transform, "", 34, C("Grafite"), Vector2.zero, new Vector2(590, 146), FontStyle.Normal);
            hud.textoDoBalao.resizeTextForBestFit = true; hud.textoDoBalao.resizeTextMinSize = 24; hud.textoDoBalao.resizeTextMaxSize = 34;
            hud.balao = (RectTransform)balao.transform;
            balao.gameObject.SetActive(false);

            // Etiqueta com o nome da peça apontada (mouse em cima, dedo segurando ou escolhida pelo teclado).
            var etiqueta = Imagem("Etiqueta_Da_Peca", raiz, arredondado, C("Grafite", .94f), Vector2.zero, new Vector2(340, 72));
            hud.textoDaEtiqueta = Texto("Texto", etiqueta.transform, "", 40, C("Branco"), Vector2.zero, new Vector2(320, 62));
            hud.textoDaEtiqueta.resizeTextForBestFit = true; hud.textoDaEtiqueta.resizeTextMinSize = 26; hud.textoDaEtiqueta.resizeTextMaxSize = 40;
            hud.etiqueta = (RectTransform)etiqueta.transform;
            etiqueta.gameObject.SetActive(false);

            hud.corGanha = Color.white;
            hud.corPerdida = new Color(.4f, .45f, .5f, .3f);
            return hud;
        }

        static void Segurar(Button botao, OficinaPista pista, float direcao, bool acelera)
        {
            var b = botao.gameObject.AddComponent<BotaoDaPista>();
            b.pista = pista;
            b.direcao = direcao;
            b.acelera = acelera;
        }
    }
}
