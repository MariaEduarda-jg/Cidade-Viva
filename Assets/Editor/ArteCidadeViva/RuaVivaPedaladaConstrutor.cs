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
    // Monta do zero a Rua Viva, parte 2 (Pedalada): config (com os 3 roteiros), níveis e a cena
    // Minigame_RuaViva_Pedalada. Verbetes da mecânica: ciclista e engarrafamento.
    // É o segundo modo da Rua Viva: as abas da seleção de níveis (aqui e no Passeio de Carro) trocam de cena.
    public static class RuaVivaPedaladaConstrutor
    {
        const string CaminhoDaCena = "Assets/Scenes/Minigame_RuaViva_Pedalada.unity";
        const string CaminhoDaConfig = "Assets/Configs/RuaVivaPedalada_Config.asset";
        const string CaminhoDosNiveis = "Assets/Configs/Niveis/Niveis_RuaVivaPedalada.asset";
        public const string NomeDaCena = "Minigame_RuaViva_Pedalada";
        public const string NomeDaArea = "Pedalada";
        const float L = 2.4f; // largura da faixa
        static readonly string[] NomesDasAbas = { "Passeio de Carro", "Pedalada" };

        [MenuItem("Cidade Viva/Minigames/Reconstruir a cena da Pedalada (Rua Viva, parte 2)")]
        public static void ReconstruirPeloMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Reconstruir();
        }

        static GameObject P(Transform pai, string prefab, Vector3 pos, float rotY = 0f, float escala = 1f)
        {
            return TravessiaConstrutor.P(pai, prefab, pos, rotY, escala);
        }

        static GameObject SemSombra(GameObject go)
        {
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public static void Reconstruir()
        {
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            PrepararIcones();
            VerbetesConstrutor.Atualizar();
            var config = PrepararConfig();
            var niveis = SalvarNiveis();

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 60f;
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 400f;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AmbienteConstrutor.CaminhoPrefab) == null) AmbienteConstrutor.CriarPrefab();
            AmbienteConstrutor.AplicarNaCenaAberta(true);

            var rotas = new PedaladaRota[config.roteiros.Length];
            for (int i = 0; i < rotas.Length; i++)
                rotas[i] = Rota(i, config.roteiros[i], new Vector3(i * 300f, 0, 0));

            // O ciclista: o mesmo das Obras (bicicleta com o menino em cima).
            var ciclista = F.Grupo("Ciclista").transform;
            var visual = Ciclista(ciclista, out var pedalar);
            var banquinho = Banquinho(out var coisas);

            var transito = F.Grupo("Transito").transform;
            string[] modelos = { "Carro_Sedan", "Carro_Hatch", "Carro_SUV", "Carro_Taxi", "Carro_Van", "Carro_Picape", "Carro_Entregas", "Carro_Esportivo", "Carro_Hatch", "Carro_SUV_Luxo" };
            var carros = new List<Transform>();
            foreach (string m in modelos)
            {
                var c = P(transito, "Carros/" + m, Vector3.zero);
                c.SetActive(false);
                carros.Add(c.transform);
            }

            var efeitos = F.Grupo("Efeitos").transform;
            var estrelinhas = TravessiaConstrutor.Particulas(efeitos, "Estrelinhas", 14, .7f, 2.5f, .12f, .4f, "Mostarda", "Mostarda_Clara", "Branco");
            var confete = TravessiaConstrutor.Particulas(efeitos, "Confete", 70, 1.6f, 6.5f, .3f, 2.5f, "Mostarda", "Vermelho", "Petroleo_Claro", "Verde", "Rosa", "Laranja");

            var canvas = NovoCanvas("Canvas");
            NovoEventSystem(false);
            var hud = Hud(canvas, out var botaoRoteiro, out var botaoOuvirRoteiro, out var esquerda, out var direita);
            var fim = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabFim), canvas.transform);
            var selecao = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabSelecao), canvas.transform);

            var gerenteGo = new GameObject("PedaladaManager", typeof(AudioSource));
            var som = gerenteGo.GetComponent<AudioSource>();
            som.playOnAwake = false;
            som.volume = .8f;
            var sirene = new GameObject("Sirene", typeof(AudioSource)).GetComponent<AudioSource>();
            sirene.transform.SetParent(gerenteGo.transform);
            sirene.playOnAwake = false;
            sirene.volume = .25f;
            var gerente = gerenteGo.AddComponent<PedaladaManager>();
            gerente.nomeDaArea = NomeDaArea;
            gerente.telaFimDeJogo = fim.GetComponent<TelaFimDeJogo>();
            gerente.telaDeSelecao = selecao.GetComponent<TelaSelecaoDeNivel>();
            gerente.niveis = niveis;
            gerente.config = config;
            gerente.hud = hud;
            gerente.som = som;
            gerente.somDaSirene = sirene;
            gerente.cameraDoJogo = cam;
            gerente.rotas = rotas;
            gerente.ciclista = ciclista;
            gerente.visualDoCiclista = visual.transform;
            gerente.animadorDoCiclista = visual.GetComponentInChildren<Animator>();
            gerente.pedalar = pedalar;
            gerente.banquinho = banquinho;
            gerente.coisasDaCabeca = coisas;
            gerente.carrosDoTransito = carros.ToArray();
            gerente.estrelinhas = estrelinhas;
            gerente.confete = confete;

            UnityEventTools.AddPersistentListener(botaoRoteiro.onClick, gerente.ComecarRoteiro);
            UnityEventTools.AddPersistentListener(botaoOuvirRoteiro.onClick, gerente.OuvirRoteiro);
            UnityEventTools.AddIntPersistentListener(esquerda.onClick, gerente.BotaoDeFaixa, -1);
            UnityEventTools.AddIntPersistentListener(direita.onClick, gerente.BotaoDeFaixa, 1);
            AbasDaRuaViva(canvas.transform, gerente, 1);
            cam.transform.SetPositionAndRotation(new Vector3(2, 2, -4), Quaternion.Euler(10, -20, 0));

            ConferirTrechos();
            OtimizacaoWebGL.TirarSombraDosPequenos();
            EditorSceneManager.SaveScene(cena, CaminhoDaCena);
            if (EditorBuildSettings.scenes.All(s => s.path != CaminhoDaCena))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(CaminhoDaCena, true)).ToArray();
        }

        // Calçadas contínuas e os pedestres que andam nelas (2026-11-22: as calçadas do parque começavam e
        // acabavam do nada e os pedestres iam e voltavam num pedaço só).
        //  - Parque: a calçada de cada trecho liga na do trecho seguinte por uma passagem em diagonal (com faixa
        //    de pedestre pintada onde cruza a ciclovia); quando o parque acaba e começa a rua, ela vai até a
        //    calçada da rua. Começa antes da largada e vai até depois da chegada.
        //  - Rua: as calçadas dos dois lados são caminhos inteiros; se a rua começa depois do parque, a calçada
        //    que não veio do parque sai de uma pracinha. A calçada que alarga sobre a faixa da direita ganha meio-fio
        //    atravessado e floreira nas pontas (é um alargamento de propósito, não um pedaço solto).
        //  - Pedestres a cada ~13 m, metade indo e metade voltando, na altura do piso da calçada.
        static void Calcadas(Transform raiz, Transform chao, RoteiroDaPedalada r, PedaladaRota rota, Transform pessoas, List<Transform> pedestres, string[] quem)
        {
            const float yC = .12f, meio = 3.5f;
            var g = F.Grupo("Calcadas_Ligadas", raiz).transform;
            int n = r.trechos.Length;
            var ini = new float[n + 1];
            for (int k = 0; k < n; k++) ini[k + 1] = ini[k] + r.trechos[k].comprimento;
            float total = ini[n];
            int FaixaK(TrechoDaPedalada t) { for (int i = -1; i <= 1; i++) if (t.Faixa(i) == TipoDeFaixa.Calcada) return i; return 99; }
            var caminhos = new List<List<Vector3>>();
            var ligacoes = new List<(Vector3 de, Vector3 ate)>();
            int ladoQueVeioDoParque = 0;

            // ---- parque
            List<Vector3> aberto = null;
            int faixa = 99;
            for (int k = 0; k < n; k++)
            {
                var t = r.trechos[k];
                int kl = t.parque ? FaixaK(t) : 99;
                if (aberto != null && kl != faixa)
                {
                    var antes = r.trechos[k - 1];
                    float curva = TrechoDaPedalada.Curva(antes, t);
                    int a = antes.FaixaDaBicicleta, b = t.FaixaDaBicicleta;
                    bool naCurva = curva > 0f && faixa >= Mathf.Min(a, b) && faixa <= Mathf.Max(a, b);
                    var fim = F.V(faixa * L, yC, ini[k] - (naCurva ? curva : meio));
                    aberto.Add(fim);
                    if (kl != 99)
                    {
                        var comeco = F.V(kl * L, yC, ini[k] + meio);
                        ligacoes.Add((fim, comeco));
                        aberto.Add(comeco);
                        faixa = kl;
                        continue;
                    }
                    if (!t.parque)
                    {
                        float lado = faixa > 0 ? 1f : -1f;
                        var comeco = F.V(lado * 5.4f, yC, ini[k] + 1f);
                        ligacoes.Add((fim, comeco));
                        aberto.Add(comeco);
                        aberto.Add(F.V(lado * 5.4f, yC, total + 60f));
                        ladoQueVeioDoParque = (int)lado;
                    }
                    caminhos.Add(aberto);
                    aberto = null;
                    faixa = 99;
                }
                if (aberto == null && kl != 99 && kl != faixa)
                {
                    aberto = new List<Vector3> { F.V(kl * L, yC, k == 0 ? -24f : ini[k] + meio) };
                    faixa = kl;
                }
            }
            if (aberto != null)
            {
                aberto.Add(F.V(faixa * L, yC, total + 60f));
                caminhos.Add(aberto);
            }

            // ---- rua: as duas calçadas de fora, em cada trecho seguido de rua
            for (int k = 0; k < n; k++)
            {
                if (r.trechos[k].parque || (k > 0 && !r.trechos[k - 1].parque)) continue;
                int kb = k;
                while (kb + 1 < n && !r.trechos[kb + 1].parque) kb++;
                float zs = k == 0 ? -24f : ini[k], ze = kb == n - 1 ? total + 60f : ini[kb + 1];
                foreach (int lado in new[] { 1, -1 })
                {
                    if (lado == ladoQueVeioDoParque && k > 0) continue;
                    var cam = new List<Vector3>();
                    if (k > 0)
                    {
                        // A calçada sai de uma pracinha do parque.
                        var praca = F.V(lado * 10f, yC, ini[k] - 7f);
                        Pracinha(g, praca, lado);
                        var comeco = F.V(lado * 5.4f, yC, ini[k] + 1f);
                        ligacoes.Add((praca, comeco));
                        cam.Add(praca);
                        cam.Add(comeco);
                    }
                    else cam.Add(F.V(lado * 5.4f, yC, zs));
                    cam.Add(F.V(lado * 5.4f, yC, ze));
                    caminhos.Add(cam);
                }
            }

            // ---- rua: alargamento da calçada sobre a faixa da direita (meio-fio atravessado e floreira nas pontas)
            for (int k = 0; k < n; k++)
            {
                var t = r.trechos[k];
                int kl = FaixaK(t);
                if (t.parque || kl == 99) continue;
                float x = kl * L;
                foreach (var (z, s) in new[] { (ini[k], 1f), (ini[k + 1], -1f) })
                {
                    F.Cubo(g, "Meio_Fio_Da_Ponta", "Branco_Concreto", F.V(x, .07f, z + s * .08f), F.V(L, .15f, .16f));
                    P(g, "Props/Floreira_De_Madeira", F.V(x - .3f, .12f, z + s * .9f), 90, .8f);
                    P(g, "Props/Pilarete", F.V(x - 1f, .12f, z + s * .3f), 0, 1f);
                }
            }

            // ---- as passagens (piso mais baixo que a ciclovia, que passa por cima) e a faixa de pedestre na ciclovia
            var curvas = chao.GetComponentsInChildren<Transform>().Where(x => x.name == "Ciclovia_Curva").ToList();
            foreach (var (de, ate) in ligacoes)
            {
                Vector3 a = new Vector3(de.x, .025f, de.z), b = new Vector3(ate.x, .025f, ate.z);
                var piso = F.Cubo(g, "Passagem", "Cinza_Claro", (a + b) / 2f, F.V(2f, .05f, Vector3.Distance(a, b) + 1.6f));
                piso.transform.rotation = Quaternion.LookRotation(b - a);
                SemSombra(piso);
                foreach (var cv in curvas)
                {
                    Vector3 c1 = cv.position - cv.forward * cv.localScale.z / 2f, c2 = cv.position + cv.forward * cv.localScale.z / 2f;
                    c1 = raiz.InverseTransformPoint(c1); c2 = raiz.InverseTransformPoint(c2);
                    if (!Cruza(a, b, c1, c2, out Vector3 cruz)) continue;
                    // Listras no sentido da ciclovia, lado a lado de uma beira à outra dela.
                    Vector3 dirCiclo = (c2 - c1); dirCiclo.y = 0f; dirCiclo.Normalize();
                    Vector3 atravessado = new Vector3(dirCiclo.z, 0f, -dirCiclo.x);
                    Quaternion rot = Quaternion.LookRotation(dirCiclo);
                    for (int i = -2; i <= 2; i++)
                    {
                        var listra = F.Cubo(g, "Faixa_De_Pedestre", "Branco", new Vector3(cruz.x, .07f, cruz.z) + atravessado * i * .42f, F.V(.26f, .012f, 1.1f));
                        listra.transform.rotation = rot;
                        SemSombra(listra);
                    }
                }
            }

            // ---- pedestres
            var pontos = new List<Vector3>();
            var fins = new List<int>();
            var caminhoDe = new List<int>();
            var partida = new List<float>();
            var volta = new List<bool>();
            foreach (var cam in caminhos) { pontos.AddRange(cam); fins.Add(pontos.Count); }
            rota.pontosDasCalcadas = pontos.ToArray();
            rota.fimDosCaminhos = fins.ToArray();
            int q = 0;
            for (int c = 0; c < caminhos.Count; c++)
            {
                float comp = rota.ComprimentoDoCaminho(c);
                // No parque a calçada é uma só: mais gente. Na rua são duas: um pouco mais espaçado (e mais leve).
                float passo = caminhos.Count > 1 ? 18f : 13f;
                for (float d = 6f + c * 4f; d < comp - 4f; d += passo)
                {
                    Vector3 local = rota.PontoNoCaminho(c, d, out Vector3 dir);
                    bool deVolta = q % 2 == 1;
                    var p = P(pessoas, quem[q % quem.Length], local, 0);
                    p.transform.localRotation = Quaternion.LookRotation(deVolta ? -dir : dir);
                    pedestres.Add(p.transform);
                    caminhoDe.Add(c); partida.Add(d); volta.Add(deVolta);
                    q++;
                }
            }
            rota.caminhoDoPedestre = caminhoDe.ToArray();
            rota.partidaDoPedestre = partida.ToArray();
            rota.pedestreDeVolta = volta.ToArray();
        }

        // Cruzamento de dois segmentos no plano do chão (x, z).
        static bool Cruza(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out Vector3 ponto)
        {
            ponto = Vector3.zero;
            Vector2 p = new Vector2(a.x, a.z), r = new Vector2(b.x - a.x, b.z - a.z);
            Vector2 q = new Vector2(c.x, c.z), s = new Vector2(d.x - c.x, d.z - c.z);
            float rxs = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(rxs) < 1e-5f) return false;
            Vector2 qp = q - p;
            float t = (qp.x * s.y - qp.y * s.x) / rxs, u = (qp.x * r.y - qp.y * r.x) / rxs;
            if (t < 0f || t > 1f || u < 0f || u > 1f) return false;
            Vector2 x = p + r * t;
            ponto = new Vector3(x.x, 0f, x.y);
            return true;
        }

        // Pracinha redonda no parque, de onde sai uma calçada.
        static void Pracinha(Transform pai, Vector3 centro, float lado)
        {
            var g = F.Grupo("Pracinha", pai, new Vector3(centro.x, 0, centro.z)).transform;
            SemSombra(F.Cilindro(g, "Piso", "Cinza_Claro", F.V(0, .03f, 0), F.V(5f, .03f, 5f)));
            F.Cilindro(g, "Canteiro", "Verde", F.V(0, .1f, 0), F.V(1.6f, .1f, 1.6f));
            P(g, "Natureza/Arvore_Comum_2", F.V(0, .18f, 0), 30, .8f);
            P(g, "Props/Banco_De_Praca", F.V(lado * 1.9f, .06f, 0), lado > 0 ? 90 : -90);
            P(g, "Props/Poste_Curvo", F.V(-lado * 1.9f, .06f, 1.2f), 0);
        }

        // O ciclista: uma bicicleta infantil (a Bicicleta_Q2 em escala menor, pra as pernas do menino alcançarem
        // os pedais) com o filho mais velho sentado no selim, um pedivela com dois pedais e o PedalarNaBicicleta,
        // que gira o pedivela e as rodas e dobra as pernas até os pedais.
        static GameObject Ciclista(Transform pai, out PedalarNaBicicleta pedalar)
        {
            const float escala = .55f;
            var visual = F.Grupo("Visual", pai);
            var bici = P(visual.transform, "Carros/Bicicleta_Q2", Vector3.zero, 180, escala);
            Transform Parte(string nome) => bici.GetComponentsInChildren<Transform>().First(t => t.name == nome);
            Bounds Medida(string nome) => Parte(nome).GetComponent<Renderer>().bounds;
            var selim = Medida("Saddle");
            var traseira = Medida("BackWheel");
            var dianteira = Medida("FrontWheel");
            var guidao = Medida("Handle");

            var menino = P(visual.transform, "Personagens/Familia_FilhoMaisVelho_Animado", Vector3.zero, 0, .85f);
            Transform Osso(string nome) => menino.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == nome);
            // Quadril em cima do selim.
            Vector3 quadril = (Osso("UpperLeg.L").position + Osso("UpperLeg.R").position) / 2f;
            menino.transform.position += new Vector3(0, selim.max.y + .01f - quadril.y, selim.center.z - quadril.z);

            // Pedivela no eixo do meio (um pouco à frente do selim, na altura das rodas).
            float raio = .065f;
            var eixo = F.Grupo("Pedivela", visual.transform).transform;
            // Um pouco acima do eixo das rodas, pra as pernas curtas do menino alcançarem o pedal de baixo.
            eixo.position = new Vector3(traseira.center.x, traseira.center.y + .06f, Mathf.Lerp(traseira.center.z, dianteira.center.z, .44f));
            F.Cilindro(eixo, "Eixo", "Grafite", Vector3.zero, F.V(.05f, .09f, .05f), F.V(0, 0, 90));
            Transform Pedal(string nome, float lado)
            {
                var braco = F.Cubo(eixo, "Braco_" + nome, "Cinza_Escuro", F.V(lado * .055f, lado * raio / 2f, 0), F.V(.015f, raio, .02f));
                var pedal = F.Cubo(eixo, "Pedal_" + nome, "Grafite", F.V(lado * .085f, lado * raio, 0), F.V(.05f, .015f, .03f));
                return pedal.transform;
            }
            var pedalE = Pedal("Esquerdo", -1f);
            var pedalD = Pedal("Direito", 1f);

            pedalar = visual.AddComponent<PedalarNaBicicleta>();
            pedalar.pedivela = eixo;
            pedalar.pedalEsquerdo = pedalE;
            pedalar.pedalDireito = pedalD;
            pedalar.rodas = new[] { Parte("BackWheel"), Parte("FrontWheel") };
            pedalar.raioDaRoda = traseira.extents.y;
            pedalar.coxaEsquerda = Osso("UpperLeg.L"); pedalar.canelaEsquerda = Osso("LowerLeg.L"); pedalar.fimEsquerdo = Osso("LowerLeg.L_end"); pedalar.peEsquerdo = Osso("Foot.L");
            pedalar.coxaDireita = Osso("UpperLeg.R"); pedalar.canelaDireita = Osso("LowerLeg.R"); pedalar.fimDireito = Osso("LowerLeg.R_end"); pedalar.peDireito = Osso("Foot.R");
            pedalar.bracos = menino.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("UpperArm")).ToArray();
            foreach (var r in visual.GetComponentsInChildren<MeshRenderer>()) if (r.transform.IsChildOf(eixo)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return visual;
        }

        // Abas "Passeio de Carro" e "Pedalada" no alto da seleção de níveis (usadas nas duas cenas da Rua Viva).
        public static void AbasDaRuaViva(Transform canvas, MinigameBase gerente, int atual)
        {
            var raiz = Tela("Abas_Dos_Modos", canvas);
            var abas = raiz.gameObject.AddComponent<AbasDosModos>();
            abas.gerente = gerente;
            abas.atual = atual;
            abas.cenas = new[] { RuaVivaCarroConstrutor.NomeDaCena, NomeDaCena };
            abas.cor = C("Branco");
            abas.corEscolhida = C("Mostarda");
            var frentes = new Image[NomesDasAbas.Length];
            for (int i = 0; i < NomesDasAbas.Length; i++)
            {
                var b = Botao("Aba_" + (i + 1), raiz, NomesDasAbas[i], "Branco", new Vector2((i - .5f) * 560f, -78), new Vector2(530, 116), 42);
                TravessiaConstrutor.Ancorar(b, new Vector2(.5f, 1));
                frentes[i] = b.transform.Find("Frente").GetComponent<Image>();
                UnityEventTools.AddIntPersistentListener(b.onClick, abas.Trocar, i);
            }
            abas.frentes = frentes;
        }

        // ------------------------------------------------------------------ rota (um caminho por nível)

        static PedaladaRota Rota(int indice, RoteiroDaPedalada r, Vector3 origem)
        {
            var raiz = F.Grupo("Rota_" + (indice + 1), null, origem).transform;
            var rota = raiz.gameObject.AddComponent<PedaladaRota>();
            var chao = F.Grupo("Chao", raiz).transform;
            var volta = F.Grupo("Em_Volta", raiz).transform;
            var pessoas = F.Grupo("Pedestres", raiz).transform;
            var obstaculos = new Transform[r.trechos.Length];
            var pedestres = new List<Transform>();
            var aleatorio = new System.Random(indice * 31 + 5);
            string[] quem = { "Personagens/Pedestre_Mulher", "Personagens/Idoso", "Personagens/Trabalhadora", "Personagens/Idosa", "Personagens/Trabalhador" };
            int q = 0;

            float z = 0f;
            float total = r.trechos.Sum(t => t.comprimento);
            // Um pouco antes do começo e depois do fim, o caminho continua (pra não acabar no vazio).
            Trecho(chao, volta, r.trechos[0], -24f, 0f, aleatorio, true);
            for (int k = 0; k < r.trechos.Length; k++)
            {
                var t = r.trechos[k];
                var g = F.Grupo("Trecho_" + k, chao).transform;
                Trecho(g, volta, t, z, z + t.comprimento, aleatorio, false, k + 1 < r.trechos.Length ? r.trechos[k + 1] : null);
                if (t.obstaculo)
                {
                    var o = F.Grupo("Obstaculo_" + k, raiz, F.V(t.FaixaCerta * L, 0, z + t.comprimento * .5f)).transform;
                    P(o, "Enchente/Buraco_No_Chao", F.V(0, .02f, .3f), 0, .9f);
                    P(o, "Enchente/Cone_Obra_1", F.V(0, 0, -.4f), 0, 1f);
                    obstaculos[k] = o;
                }
                if (t.engarrafamento)
                    Engarrafamento(raiz, rota, z, z + t.comprimento);
                z += t.comprimento;
            }
            Trecho(chao, volta, r.trechos[r.trechos.Length - 1], total, total + 60f, aleatorio, true);
            Chegada(raiz, indice, total);
            Calcadas(raiz, chao, r, rota, pessoas, pedestres, quem);

            var seta = F.Grupo("Seta_Da_Dica", raiz).transform;
            F.M(seta, "Ponta", "Cone", "Mostarda", F.V(0, 0, 0), F.V(.7f, .6f, .7f), F.V(180, 0, 0));
            F.Cubo(seta, "Haste", "Mostarda", F.V(0, .55f, 0), F.V(.22f, .6f, .22f));
            foreach (var mr in seta.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            seta.gameObject.SetActive(false);

            rota.obstaculos = obstaculos;
            rota.pedestres = pedestres.ToArray(); // (os caminhos de cada um já foram gravados por Calcadas)
            rota.setaDaDica = seta;
            return rota;
        }

        static void Trecho(Transform g, Transform volta, TrechoDaPedalada t, float z0, float z1, System.Random aleatorio, bool extra, TrechoDaPedalada proximo = null)
        {
            float zc = (z0 + z1) / 2f, c = z1 - z0;
            // A ciclovia muda de faixa no trecho que vem: nos últimos metros ela faz uma curva até lá (fica contínua).
            float curva = TrechoDaPedalada.Curva(t, proximo);
            int a = t.FaixaDaBicicleta, b = proximo != null ? proximo.FaixaDaBicicleta : 2;
            float zt = z1 - curva;
            SemSombra(F.Cubo(g, "Chao", t.parque ? "Verde_Claro" : "Verde", F.V(0, -.16f, zc), F.V(90f, .1f, c + .02f)));
            for (int i = -1; i <= 1; i++)
            {
                float x = i * L;
                bool naCurva = curva > 0f && i >= Mathf.Min(a, b) && i <= Mathf.Max(a, b);
                float fimDaFaixa = naCurva ? zt : z1, zci = (z0 + fimDaFaixa) / 2f, ci = fimDaFaixa - z0;
                switch (t.Faixa(i))
                {
                    case TipoDeFaixa.Ciclovia:
                        SemSombra(F.Cubo(g, "Ciclovia", "Terracota", F.V(x, .03f, zci), F.V(2.2f, .06f, ci)));
                        SemSombra(F.Cubo(g, "Beira", "Verde", F.V(x, .0f, zci), F.V(L, .04f, ci)));
                        for (float d = z0 + 1.3f; d < fimDaFaixa; d += 2.6f)
                            SemSombra(F.Cubo(g, "Tracejado", "Branco", F.V(x, .066f, d), F.V(.1f, .01f, .9f)));
                        if (!extra)
                            P(g, "Props/Placa_Ciclovia", F.V(x <= 0 ? -4.4f : 4.4f, 0, z0 + 2f), 180);
                        break;
                    case TipoDeFaixa.Ciclofaixa:
                        SemSombra(F.Cubo(g, "Asfalto", "Asfalto", F.V(x, -.04f, zci), F.V(L, .1f, ci)));
                        SemSombra(F.Cubo(g, "Pintura_Vermelha", "Vermelho", F.V(x, .018f, zci), F.V(1.7f, .016f, ci)));
                        foreach (int lado in new[] { -1, 1 })
                        {
                            int viz = i + lado;
                            if (viz < -1 || viz > 1 || t.Faixa(viz) != TipoDeFaixa.Carros) continue;
                            float lx = x + lado * 1.08f;
                            SemSombra(F.Cubo(g, "Linha", "Branco", F.V(lx, .02f, zci), F.V(.14f, .02f, ci)));
                            for (float d = z0 + 1f; d < fimDaFaixa; d += 2f)
                                F.Cubo(g, "Tachao", "Mostarda", F.V(lx, .06f, d), F.V(.16f, .08f, .34f));
                        }
                        for (float d = z0 + 4f; d < fimDaFaixa; d += 9f)
                            SemSombra(F.Cubo(g, "Marca_De_Bicicleta", "Branco", F.V(x, .03f, d), F.V(.5f, .012f, 1f)));
                        break;
                    case TipoDeFaixa.Carros:
                        SemSombra(F.Cubo(g, "Asfalto", "Asfalto", F.V(x, -.04f, zci), F.V(L, .1f, ci)));
                        if (i < 1 && t.Faixa(i + 1) == TipoDeFaixa.Carros)
                            for (float d = z0 + 1f; d < fimDaFaixa; d += 4f)
                                SemSombra(F.Cubo(g, "Tracejado", "Branco", F.V(x + L / 2f, .02f, d), F.V(.14f, .012f, 2f)));
                        break;
                    case TipoDeFaixa.Calcada:
                        F.Cubo(g, "Calcada", "Cinza_Claro", F.V(x, .06f, zci), F.V(L, .12f, ci));
                        foreach (int lado in new[] { -1, 1 })
                        {
                            int viz = i + lado;
                            if (viz < -1 || viz > 1) continue;
                            var f = t.Faixa(viz);
                            if (f == TipoDeFaixa.Carros || f == TipoDeFaixa.Ciclofaixa)
                                F.Cubo(g, "Meio_Fio", "Branco_Concreto", F.V(x + lado * 1.12f, .07f, zci), F.V(.16f, .15f, ci));
                        }
                        for (float d = z0 + 1f; d < fimDaFaixa; d += 2f)
                            SemSombra(F.Cubo(g, "Junta", "Cinza", F.V(x, .122f, d), F.V(2.2f, .01f, .06f)));
                        break;
                    case TipoDeFaixa.Grama:
                        F.Cubo(g, "Canteiro", "Verde", F.V(x, .07f, zci), F.V(2.2f, .14f, ci - .2f));
                        for (float d = z0 + .9f; d < fimDaFaixa - 1f; d += 6f)
                            P(g, "Natureza/Arbusto_2", F.V(x, .14f, d), (float)aleatorio.NextDouble() * 360f, .7f);
                        break;
                }
            }

            if (curva > 0f)
            {
                for (int i = Mathf.Min(a, b); i <= Mathf.Max(a, b); i++)
                    SemSombra(F.Cubo(g, "Beira_Da_Curva", "Verde", F.V(i * L, .0f, (zt + z1) / 2f), F.V(L, .04f, curva)));
                Vector3 de = F.V(a * L, 0, zt - 1f), ate = F.V(b * L, 0, z1 + 1f);
                var tira = SemSombra(F.Cubo(g, "Ciclovia_Curva", "Terracota", (de + ate) / 2f + Vector3.up * .03f, F.V(2.2f, .06f, Vector3.Distance(de, ate))));
                tira.transform.rotation = Quaternion.LookRotation(ate - de);
                for (float k = .15f; k < .95f; k += .2f)
                {
                    var traco = SemSombra(F.Cubo(g, "Tracejado", "Branco", Vector3.Lerp(de, ate, k) + Vector3.up * .066f, F.V(.1f, .01f, .9f)));
                    traco.transform.rotation = tira.transform.rotation;
                }
            }

            // O que fica em volta: parque (gramado, árvores, bancos) ou rua (calçadas, postes e prédios).
            if (t.parque)
            {
                string[] arvores = { "Natureza/Arvore_Comum_1", "Natureza/Arvore_Comum_2", "Natureza/Arvore_Comum_3", "Natureza/Arvore_Simples_Grande", "Natureza/Arvore_Betula" };
                foreach (float lado in new[] { -1f, 1f })
                    for (float d = z0 + 3f; d < z1; d += 7f)
                    {
                        float x = lado * (5.8f + (float)aleatorio.NextDouble() * 7f);
                        P(volta, arvores[aleatorio.Next(arvores.Length)], F.V(x, -.1f, d + (float)aleatorio.NextDouble() * 3f), (float)aleatorio.NextDouble() * 360f, .9f);
                    }
                if (!extra)
                    P(volta, "Props/Banco_De_Praca", F.V(4.6f, -.1f, z0 + c * .5f), -90);
            }
            else
            {
                SemSombra(F.Cubo(g, "Calcada_Esquerda", "Cinza_Claro", F.V(-5.4f, .06f, zc), F.V(3.6f, .12f, c)));
                F.Cubo(g, "Meio_Fio_Esquerdo", "Branco_Concreto", F.V(-3.62f, .07f, zc), F.V(.16f, .15f, c));
                SemSombra(F.Cubo(g, "Calcada_Direita", "Cinza_Claro", F.V(5.4f, .06f, zc), F.V(3.6f, .12f, c)));
                string[] predios = { "Predios/Casa_01", "Predios/Comercio_A", "Predios/Casa_03", "Predios/Comercio_C", "Predios/Casa_05", "Predios/Comercio_F", "Predios/Casa_07", "Predios/Comercio_H" };
                foreach (float lado in new[] { -1f, 1f })
                {
                    for (float d = z0 + 2f; d < z1; d += 13f)
                        P(volta, predios[aleatorio.Next(predios.Length)], F.V(lado * 11.5f, .1f, d + 5f), lado < 0 ? 90 : -90, .85f);
                    for (float d = z0 + 6f; d < z1; d += 18f)
                        P(volta, "Props/PosteKK", F.V(lado * 4.1f, .12f, d), lado < 0 ? 90 : -90);
                }
            }
        }

        // Carros parados nas duas faixas dos carros, com a ambulância presa no meio e um ônibus.
        static void Engarrafamento(Transform raiz, PedaladaRota rota, float z0, float z1)
        {
            var g = F.Grupo("Engarrafamento", raiz).transform;
            var lista = new List<Transform>();
            string[] carros = { "Carro_Sedan", "Carro_SUV", "Carro_Hatch", "Carro_Taxi", "Carro_Van", "Carro_Picape", "Carro_SUV_Luxo" };
            int n = 0;
            foreach (int faixa in new[] { 0, -1 })
            {
                float z = z0 + 4f;
                int pos = 0;
                while (z < z1 - 4f)
                {
                    string modelo = "Carros/" + carros[n++ % carros.Length];
                    float passo = 6.2f;
                    if (faixa == 0 && pos == 2) { modelo = "Carros/Ambulancia"; passo = 7f; }
                    if (faixa == -1 && pos == 1) { modelo = "Carros/Onibus_Urbano"; passo = 11.5f; z += 2.6f; }
                    var c = P(g, modelo, F.V(faixa * L, 0, z));
                    Colisor(c);
                    lista.Add(c.transform);
                    if (modelo.EndsWith("Ambulancia"))
                    {
                        rota.ambulancia = c.transform;
                        var b = Limites(c);
                        float topo = b.max.y + .06f;
                        var l1 = F.Cubo(c.transform, "Sirene_Vermelha", "Vermelho", c.transform.InverseTransformPoint(new Vector3(b.center.x - .3f, topo, b.center.z + b.extents.z * .5f)), F.V(.32f, .16f, .2f));
                        var l2 = F.Cubo(c.transform, "Sirene_Azul", "Petroleo_Claro", c.transform.InverseTransformPoint(new Vector3(b.center.x + .3f, topo, b.center.z + b.extents.z * .5f)), F.V(.32f, .16f, .2f));
                        rota.luzesDaSirene = new[] { l1.GetComponent<Renderer>(), l2.GetComponent<Renderer>() };
                    }
                    z += passo;
                    pos++;
                }
            }
            rota.engarrafamento = lista.ToArray();
        }

        static Bounds Limites(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static void Colisor(GameObject go)
        {
            var b = Limites(go);
            var col = go.AddComponent<BoxCollider>();
            col.center = go.transform.InverseTransformPoint(b.center);
            col.size = b.size + Vector3.one * .3f;
        }

        // Fim do caminho: um portal colorido; cada nível tem o seu lugar de chegada.
        static void Chegada(Transform raiz, int indice, float z)
        {
            var g = F.Grupo("Chegada", raiz, F.V(0, 0, z)).transform;
            foreach (float x in new[] { -4.3f, 4.3f })
                F.Cilindro(g, "Poste", "Branco", F.V(x, 2f, 0), F.V(.25f, 4f, .25f));
            F.Cubo(g, "Faixa", indice == 2 ? "Rosa" : "Mostarda", F.V(0, 3.9f, 0), F.V(8.8f, .7f, .15f));
            string[] cores = { "Vermelho", "Mostarda", "Petroleo_Claro", "Verde", "Rosa", "Laranja" };
            for (int i = 0; i < 12; i++)
                F.Cubo(g, "Bandeirinha", cores[i % cores.Length], F.V(-4f + i * .73f, 3.35f, 0), F.V(.35f, .4f, .04f), F.V(0, 0, 45));
            if (indice == 0)
            {
                P(g, "Natureza/Flores", F.V(-3f, 0, 4f), 0, 1.4f);
                P(g, "Natureza/Flores", F.V(3f, 0, 5f), 0, 1.4f);
            }
            else if (indice == 1)
                P(g, "Predios/Padaria", F.V(11f, .1f, 6f), -90, .9f);
            else
                foreach (float x in new[] { -6.5f, 6.5f })
                {
                    P(g, "Props/Guarda_Sol_A", F.V(x, .1f, 6f), 0);
                    P(g, "Props/Guarda_Sol_B", F.V(x, .1f, 11f), 0);
                }
        }

        // Banquinho com as coisas da cabeça (capacete, boné e chapéu de festa), ao lado da bicicleta.
        static Transform Banquinho(out CoisaDaCabeca[] coisas)
        {
            var b = F.Grupo("Banquinho").transform;
            F.Caixa(b, "Tampo", "Marrom_Claro", 0, .38f, 0, 1.5f, .07f, .55f);
            foreach (float x in new[] { -.65f, .65f })
                foreach (float zz in new[] { -.2f, .2f })
                    F.Caixa(b, "Perna", "Marrom", x, 0, zz, .07f, .38f, .07f);

            var lista = new List<CoisaDaCabeca>();
            var cap = Coisa(b, "capacete", "Capacete", .2f);
            // Capacete de ciclista: casco amarelo um pouco alongado pra trás, faixa central azul, respiros escuros,
            // viseira curta na frente, borda e a tira do queixo (as formas são da paleta).
            F.M(cap.transform, "Casco", "Semiesfera", "Mostarda", F.V(0, 0, -.01f), F.V(.36f, .38f, .44f));
            F.M(cap.transform, "Borda", "Semiesfera", "Petroleo_Escuro", F.V(0, -.012f, -.01f), F.V(.37f, .05f, .45f));
            F.M(cap.transform, "Faixa", "Semiesfera", "Petroleo", F.V(0, .006f, -.01f), F.V(.1f, .385f, .445f));
            for (int i = 0; i < 3; i++)
                foreach (float x in new[] { -.085f, .085f })
                    F.Cubo(cap.transform, "Respiro", "Grafite", F.V(x, .155f - i * .012f, .07f - i * .075f), F.V(.035f, .03f, .06f), F.V(-25 + i * 25, 0, 0));
            F.Cubo(cap.transform, "Viseira", "Petroleo_Escuro", F.V(0, .03f, .22f), F.V(.28f, .02f, .08f), F.V(-12, 0, 0));
            foreach (float x in new[] { -.17f, .17f })
                F.Cubo(cap.transform, "Tira", "Grafite", F.V(x, -.09f, .03f), F.V(.015f, .17f, .025f));
            F.Cubo(cap.transform, "Fivela", "Cinza_Escuro", F.V(0, -.18f, .05f), F.V(.07f, .03f, .03f));
            lista.Add(cap);

            var bone = Coisa(b, "bone", "Boné", .12f);
            F.M(bone.transform, "Copa", "Semiesfera", "Vermelho", F.V(0, 0, 0), F.V(.3f, .28f, .32f));
            F.Cubo(bone.transform, "Aba", "Vermelho", F.V(0, .01f, .2f), F.V(.24f, .02f, .16f));
            lista.Add(bone);

            var festa = Coisa(b, "chapeu_de_festa", "Chapéu de festa", .3f);
            F.M(festa.transform, "Cone", "Cone", "Rosa", F.V(0, 0, 0), F.V(.2f, .6f, .2f));
            F.Esfera(festa.transform, "Pompom", "Mostarda", F.V(0, .31f, 0), F.V(.08f, .08f, .08f));
            lista.Add(festa);

            coisas = lista.ToArray();
            return b;
        }

        static CoisaDaCabeca Coisa(Transform pai, string id, string nome, float altura)
        {
            var go = F.Grupo("Coisa_" + id, pai, F.V(0, .45f, 0));
            var c = go.AddComponent<CoisaDaCabeca>();
            c.id = id;
            c.nome = nome;
            c.altura = altura;
            var col = go.AddComponent<BoxCollider>();
            col.center = F.V(0, .15f, 0);
            col.size = F.V(.42f, .4f, .45f);
            return c;
        }

        // ------------------------------------------------------------------ conferência com as definições

        // O que NÃO vem direto das definições (lista da conferência e do Docs/Revisao_Coordenacao.md).

        public static readonly string[] NaoVemDasDefinicoes =

        {
                         "OS ROTEIROS DEPOIS DA ENCHENTE: 1 passeio no parque que reabriu com ciclovia nova; 2 ir até a padaria pela rua; 3 atravessar o parque e a avenida até a festa da reconstrução. Lugares, textos e recados são do jogo.",
                         "Boné e chapéu de festa como coisas erradas pra cabeça: a definição só diz que o capacete é equipamento de segurança; dizer que o boné \"não é capacete\" é do jogo. O capacete de primeira vale estrela nos níveis 1 e 2.",
                         "Onde não há ciclofaixa, a bicicleta vai na faixa dos carros da direita (\"pedale na rua, perto da calçada\"). A definição só diz \"quando disponíveis\"; ir mais pra direita é regra de trânsito, não do glossário. A faixa da esquerda também não perde estrela.",
                         "A calçada vale como lugar errado pela definição de calçada (\"destinada para a passagem de pedestres ou animais\"), que é de outro verbete.",
                         "Há uma tolerância de 1,3 s no lugar errado (dá tempo de trocar de faixa) e nada conta quando a bicicleta já está indo pra faixa certa do trecho que vem.",
                         "O canteiro (grama com arbustos) entre as faixas do parque, e os cones e buracos da enchente na rua: bater é \"batida\" (a bicicleta para um pouco). Nenhuma definição fala de batida.",
                         "Os carros que vêm atrás desviam da bicicleta pela outra faixa, ou esperam; se ela está na pista dos carros onde tinha ciclofaixa, um deles buzina baixinho.",
                         "Engarrafamento: a ambulância é que fica presa (podia ser o ônibus, que também está na fila). A causa do acúmulo não é mostrada. A bicicleta para pra criança olhar e segue pela ciclofaixa, passando pela fila.",
                         "O tempo do nível 3 (o relógio corre também enquanto ela olha o engarrafamento) e as setas de dica nos níveis 1 e 2.",
                         "Ciclista = bicicleta infantil (Bicicleta_Q2 em escala menor) com o menino sentado, pedalando por código (pernas presas aos pedais). Capacete, boné, chapéu, canteiros, ciclovia e ciclofaixa são formas da paleta.",

        };

        // Regras da última conferência: {verbete, trecho literal, regra} (usadas pelo RevisaoConstrutor).

        public static readonly List<string[]> RegrasDaRevisao = new List<string[]>();


        [MenuItem("Cidade Viva/Verbetes/Conferir regras da Pedalada com as definições")]
        public static void ConferirPeloMenu()
        {
            ConferirTrechos();
        }

        public static bool ConferirTrechos()
        {
            var config = AssetDatabase.LoadAssetAtPath<PedaladaConfig>(CaminhoDaConfig);
            var banco = VerbeteDatabase.Instancia;
            var falhas = new List<string>();
            var lista = new StringBuilder();
            RegrasDaRevisao.Clear();
            void Regra(string id, string trecho, string regra)
            {
                RegrasDaRevisao.Add(new[] { id, trecho, regra });
                var v = banco.Buscar(id);
                if (v == null) { falhas.Add("verbete desconhecido: " + id); return; }
                if (!v.definicao.Contains(trecho)) falhas.Add($"{id}: o trecho \"{trecho}\" não está na definição");
                lista.AppendLine($"{v.palavra}: {regra} <= \"{trecho}\"");
            }
            lista.AppendLine("REGRAS DERIVADAS DAS DEFINIÇÕES");
            Regra("ciclista", config.trechoDoCiclista, "a criança é a ciclista: ela conduz a bicicleta (troca de faixa)");
            Regra("ciclista", config.trechoDoCapacete, "antes de sair, põe o capacete; o que não é capacete (boné, chapéu de festa) é recusado com o balão");
            Regra("ciclista", config.trechoDaCiclovia, "onde há ciclovia ou ciclofaixa, pedala nela; na pista dos carros perde a estrela \"lugar certo\"; onde não há, pedala na rua sem perder nada");
            Regra("ciclovia", config.trechoDaViaSeparada, "a ciclovia fica no parque, separada, sem carros (níveis 1 e 3)");
            Regra("ciclofaixa", config.trechoDoLadoDosCarros, "a ciclofaixa fica na rua, colada nas faixas dos carros, pintada de vermelho (níveis 2 e 3)");
            Regra("ciclofaixa", "pode estar pintada de vermelho", "a ciclofaixa é vermelha");
            Regra("calcada", config.trechoDaCalcada, "a bicicleta não vai pela calçada: ficar nela perde a estrela \"lugar certo\"");
            Regra("engarrafamento", config.trechoDoAcumulo, "no nível 3, os carros se acumulam nas duas faixas e os que chegam param atrás da fila");
            Regra("engarrafamento", config.trechoDaPassagem, "a ambulância fica presa na fila; a criança toca no engarrafamento e abre o cartão");

            lista.AppendLine().AppendLine("O QUE NÃO VEM DIRETO DAS DEFINIÇÕES (escolhas do jogo, pra revisão)");
            foreach (string item in NaoVemDasDefinicoes)
                lista.AppendLine("  - " + item);

            lista.AppendLine().AppendLine("TRECHOS POR NÍVEL");
            for (int n = 0; n < config.roteiros.Length; n++)
            {
                var r = config.roteiros[n];
                lista.AppendLine($"  Nível {n + 1} ({r.titulo}): " + string.Join(" | ", r.trechos.Select(t =>
                    $"{t.esquerda}/{t.meio}/{t.direita}{(t.obstaculo ? " +obstáculo" : "")}{(t.engarrafamento ? " +engarrafamento" : "")}")));
                if (r.coisasDaCabeca.Length == 0 || r.coisasDaCabeca[0] != "capacete") falhas.Add($"nível {n + 1}: a primeira coisa da cabeça tem de ser o capacete");
                foreach (var t in r.trechos)
                {
                    if (t.FaixaDaBicicleta != 2 && t.engarrafamento && t.Faixa(t.FaixaDaBicicleta) != TipoDeFaixa.Ciclofaixa)
                        falhas.Add($"nível {n + 1}: engarrafamento sem ciclofaixa ao lado");
                    if (t.FaixaCerta == 2 || t.Faixa(t.FaixaCerta) == TipoDeFaixa.Grama || t.Faixa(t.FaixaCerta) == TipoDeFaixa.Calcada)
                        falhas.Add($"nível {n + 1}: trecho sem lugar pra bicicleta");
                    if (t.parque && (t.esquerda == TipoDeFaixa.Carros || t.meio == TipoDeFaixa.Carros || t.direita == TipoDeFaixa.Carros))
                        falhas.Add($"nível {n + 1}: carros no parque (a ciclovia é separada dos carros)");
                }
            }

            lista.AppendLine().AppendLine(falhas.Count == 0 ? "CONFERÊNCIA: tudo certo." : "CONFERÊNCIA: " + falhas.Count + " falha(s):");
            foreach (string f in falhas) lista.AppendLine("  - " + f);
            File.WriteAllText("Temp/ruaviva_pedalada_regras.txt", lista.ToString(), Encoding.UTF8);
            if (falhas.Count == 0) Debug.Log("[Pedalada] Regras conferidas. Lista em Temp/ruaviva_pedalada_regras.txt.");
            else Debug.LogError("[Pedalada] Problemas:\n" + string.Join("\n", falhas));
            return falhas.Count == 0;
        }

        // ------------------------------------------------------------------ roteiros, níveis e config

        static TrechoDaPedalada T(TipoDeFaixa e, TipoDeFaixa m, TipoDeFaixa d, float comprimento, bool parque, string aviso = "", bool obstaculo = false, bool engarrafamento = false)
        {
            return new TrechoDaPedalada { esquerda = e, meio = m, direita = d, comprimento = comprimento, parque = parque, aviso = aviso, obstaculo = obstaculo, engarrafamento = engarrafamento };
        }

        static RoteiroDaPedalada[] Roteiros()
        {
            const TipoDeFaixa G = TipoDeFaixa.Grama, V = TipoDeFaixa.Ciclovia, F_ = TipoDeFaixa.Ciclofaixa, C_ = TipoDeFaixa.Carros, K = TipoDeFaixa.Calcada;
            return new[]
            {
                new RoteiroDaPedalada
                {
                    titulo = "Passeio no parque",
                    texto = "O parque reabriu depois da enchente, com ciclovia nova! Antes de pedalar, escolha o que vai na cabeça.",
                    novidade = "Novo: o capacete e a ciclovia, que muda de lado.",
                    coisasDaCabeca = new[] { "capacete", "bone" },
                    trechos = new[]
                    {
                        T(G, V, K, 30, true),
                        T(G, K, V, 36, true, "A ciclovia vai pra direita!"),
                        T(K, V, G, 36, true, "A ciclovia volta pro meio!"),
                        T(V, K, G, 36, true, "Agora a ciclovia vai pra esquerda!"),
                        T(V, K, G, 30, true, "Quase lá: o mirante do parque!"),
                    },
                    recadoDeChegada = "Chegamos ao mirante! De capacete e na ciclovia o tempo todo.",
                },
                new RoteiroDaPedalada
                {
                    titulo = "Pão fresquinho",
                    texto = "A padaria abriu de novo! O caminho é pela rua: em parte dela tem ciclofaixa, pintada de vermelho, ao lado dos carros.",
                    novidade = "Novo: a ciclofaixa e os trechos sem ciclofaixa.",
                    coisasDaCabeca = new[] { "capacete", "bone", "chapeu_de_festa" },
                    trechos = new[]
                    {
                        T(C_, C_, F_, 30, false),
                        T(C_, C_, F_, 30, false),
                        T(C_, C_, K, 36, false, "A ciclofaixa acabou: pedale na rua, perto da calçada."),
                        T(C_, C_, F_, 36, false, "Tem ciclofaixa de novo! Volte pra ela."),
                        T(C_, C_, K, 36, false, "Sem ciclofaixa de novo. Cuidado com o buraco!", obstaculo: true),
                        T(C_, C_, F_, 30, false, "Ciclofaixa! A padaria é logo ali."),
                    },
                    recadoDeChegada = "Chegamos na padaria! Pão fresquinho pra família.",
                },
                new RoteiroDaPedalada
                {
                    titulo = "A festa da reconstrução",
                    texto = "Hoje tem festa! Atravesse o parque e a avenida antes do tempo acabar.",
                    novidade = "Novo: o engarrafamento e o relógio.",
                    coisasDaCabeca = new[] { "capacete", "chapeu_de_festa", "bone" },
                    trechos = new[]
                    {
                        T(G, V, K, 30, true),
                        T(V, K, G, 30, true, "A ciclovia vai pra esquerda!"),
                        T(C_, C_, F_, 36, false, "Saindo do parque: agora é ciclofaixa, à direita."),
                        T(C_, C_, K, 30, false, "Sem ciclofaixa: pedale na rua. Olhe o buraco!", obstaculo: true),
                        T(C_, C_, F_, 50, false, "Ciclofaixa de novo! E olhe a avenida...", engarrafamento: true),
                        T(C_, C_, F_, 30, false, "A festa é logo ali!"),
                    },
                    recadoDeChegada = "Chegamos na festa da reconstrução!",
                },
            };
        }

        static NivelConfig Nivel(string objetivo, float velocidade, float tempo, params string[] regras)
        {
            return new NivelConfig
            {
                objetivo = objetivo, velocidade = velocidade, tempoEmSegundos = tempo,
                regrasExtras = new List<string>(regras),
                // As estrelas são calculadas pelo manager; isto só alimenta o texto da seleção.
                estrelas = new CriterioDeEstrelas { medida = MedidaDeEstrelas.Erros, para2Estrelas = 1, para3Estrelas = 0 },
            };
        }

        static LevelConfig SalvarNiveis()
        {
            ArtePaleta.GarantirPasta("Assets/Configs/Niveis");
            var asset = AssetDatabase.LoadAssetAtPath<LevelConfig>(CaminhoDosNiveis);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(asset, CaminhoDosNiveis);
            }
            // 3 níveis (como o Motorista Mirim): exceção combinada à regra de 5 do padrão.
            asset.niveis = new List<NivelConfig>
            {
                Nivel("Capacete e ciclovia, no parque.", 5f, 0f, PedaladaManager.RegraDica),
                Nivel("Ciclofaixa ao lado dos carros, e trechos sem ela.", 5.5f, 0f, PedaladaManager.RegraDica, PedaladaManager.RegraCarros),
                Nivel("Tudo junto, com engarrafamento e tempo.", 6f, 50f, PedaladaManager.RegraCarros),
            };
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static PedaladaConfig PrepararConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<PedaladaConfig>(CaminhoDaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PedaladaConfig>();
                AssetDatabase.CreateAsset(config, CaminhoDaConfig);
            }
            else
            {
                var novo = ScriptableObject.CreateInstance<PedaladaConfig>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(novo), config);
                Object.DestroyImmediate(novo);
            }
            config.roteiros = Roteiros();
            config.somDoCapacete = TravessiaConstrutor.Som("Interface/confirmation_002.ogg");
            config.somDeErro = TravessiaConstrutor.Som("Interface/error_004.ogg");
            config.somDeTrocarDeFaixa = TravessiaConstrutor.Som("Efeitos/deslizar.wav");
            config.somDaBatida = TravessiaConstrutor.Som("Efeitos/tropeco.wav");
            config.somDaCampainha = TravessiaConstrutor.Som("Efeitos/campainha_bicicleta.wav");
            config.somDaSirene = TravessiaConstrutor.Som("Efeitos/sirene.wav");
            config.somDaBuzina = TravessiaConstrutor.Som("Efeitos/buzina.wav");
            config.somDeComemoracao = TravessiaConstrutor.Som("Interface/confirmation_003.ogg");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        // ------------------------------------------------------------------ interface

        static PedaladaHUD Hud(Canvas canvas, out Button botaoRoteiro, out Button botaoOuvirRoteiro, out Button esquerda, out Button direita)
        {
            var raiz = Tela("HUD", canvas.transform);
            var hud = raiz.gameObject.AddComponent<PedaladaHUD>();
            var voltarAoHub = raiz.gameObject.AddComponent<VoltarAoHub>();

            // Nível e critérios, com o relógio do nível 3.
            var painel = Imagem("Painel_Do_Nivel", raiz, arredondado, C("Branco", .92f), new Vector2(240, -135), new Vector2(430, 220));
            TravessiaConstrutor.Ancorar(painel, new Vector2(0, 1));
            hud.painelDoNivel = painel.gameObject;
            hud.textoDoNivel = Texto("Nivel", painel.transform, "", 30, C("Petroleo"), new Vector2(0, 80), new Vector2(400, 44));
            var estrelas = new Image[3];
            var rotulos = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                estrelas[i] = Imagem("Estrela_" + (i + 1), painel.transform, S("figura_estrela"), Color.white, new Vector2(-135 + i * 135, 20), new Vector2(66, 66));
                rotulos[i] = Texto("Rotulo_" + (i + 1), painel.transform, "", 30, C("Grafite"), new Vector2(-135 + i * 135, -35), new Vector2(140, 42), FontStyle.Normal);
                rotulos[i].resizeTextForBestFit = true; rotulos[i].resizeTextMinSize = 24; rotulos[i].resizeTextMaxSize = 30;
            }
            hud.estrelasDosCriterios = estrelas;
            hud.rotulosDosCriterios = rotulos;
            hud.textoDoTempo = Texto("Tempo", painel.transform, "", 34, C("Petroleo"), new Vector2(0, -80), new Vector2(400, 44));

            var hubBotao = Botao("Botao_VoltarAoHub", raiz, "", "Petroleo_Claro", new Vector2(-100, -95), new Vector2(120, 120), 0, S("ui_seta_voltar"), circulo);
            TravessiaConstrutor.Ancorar(hubBotao, new Vector2(1, 1));
            UnityEventTools.AddPersistentListener(hubBotao.onClick, voltarAoHub.Voltar);

            var recado = Imagem("Painel_Do_Recado", raiz, arredondado, C("Branco", .94f), new Vector2(0, 70), new Vector2(1000, 86));
            TravessiaConstrutor.Ancorar(recado, new Vector2(.5f, 0));
            hud.painelDoRecado = recado.gameObject;
            hud.textoDoRecado = Texto("Texto", recado.transform, "", 36, C("Petroleo"), Vector2.zero, new Vector2(970, 76));
            hud.textoDoRecado.resizeTextForBestFit = true; hud.textoDoRecado.resizeTextMinSize = 22; hud.textoDoRecado.resizeTextMaxSize = 36;

            // Botões grandes de trocar de faixa, nos cantos de baixo.
            var botoes = Tela("Botoes_De_Faixa", raiz);
            hud.botoesDeFaixa = botoes.gameObject;
            esquerda = Botao("Botao_Esquerda", botoes, "", "Petroleo_Claro", new Vector2(130, 150), new Vector2(200, 200), 0, S("ui_seta_voltar"), circulo);
            TravessiaConstrutor.Ancorar(esquerda, new Vector2(0, 0));
            direita = Botao("Botao_Direita", botoes, "", "Petroleo_Claro", new Vector2(-130, 150), new Vector2(200, 200), 0, S("ui_seta_voltar"), circulo);
            TravessiaConstrutor.Ancorar(direita, new Vector2(1, 0));
            direita.transform.Find("Frente/Icone").localScale = new Vector3(-1, 1, 1);

            var balao = Imagem("Balao", raiz, arredondado, C("Grafite"), new Vector2(0, 170), new Vector2(1000, 280));
            Imagem("Papel", balao.transform, arredondado, C("Mostarda_Clara"), Vector2.zero, new Vector2(986, 266));
            hud.textoDoBalao = Texto("Texto", balao.transform, "", 40, C("Grafite"), Vector2.zero, new Vector2(940, 244), FontStyle.Normal);
            hud.textoDoBalao.resizeTextForBestFit = true; hud.textoDoBalao.resizeTextMinSize = 24; hud.textoDoBalao.resizeTextMaxSize = 40;
            hud.balao = balao.gameObject;
            balao.gameObject.SetActive(false);

            hud.marca = Imagem("Marca", raiz, S("figura_estrela"), C("Mostarda"), Vector2.zero, new Vector2(110, 110)).rectTransform;

            // Cartão do roteiro (no começo, com a bicicleta parada).
            var cartao = Imagem("Cartao_Do_Roteiro", raiz, arredondado, C("Branco", .96f), new Vector2(0, -20), new Vector2(1160, 600), true);
            hud.cartaoDoRoteiro = cartao.gameObject;
            Imagem("Faixa", cartao.transform, arredondado, C("Petroleo"), new Vector2(0, 225), new Vector2(1100, 110));
            hud.tituloDoRoteiro = Texto("Titulo", cartao.transform, "", 56, C("Branco"), new Vector2(0, 225), new Vector2(1060, 100));
            hud.tituloDoRoteiro.resizeTextForBestFit = true; hud.tituloDoRoteiro.resizeTextMinSize = 30; hud.tituloDoRoteiro.resizeTextMaxSize = 56;
            hud.textoDoRoteiro = Texto("Texto", cartao.transform, "", 40, C("Grafite"), new Vector2(0, 70), new Vector2(1040, 180), FontStyle.Normal);
            hud.textoDoRoteiro.resizeTextForBestFit = true; hud.textoDoRoteiro.resizeTextMinSize = 24; hud.textoDoRoteiro.resizeTextMaxSize = 40;
            var novidade = Imagem("Novidade", cartao.transform, arredondado, C("Mostarda_Clara"), new Vector2(0, -80), new Vector2(1040, 80));
            hud.novidadeDoRoteiro = Texto("Texto", novidade.transform, "", 34, C("Petroleo_Escuro"), Vector2.zero, new Vector2(1010, 70));
            hud.novidadeDoRoteiro.resizeTextForBestFit = true; hud.novidadeDoRoteiro.resizeTextMinSize = 20; hud.novidadeDoRoteiro.resizeTextMaxSize = 34;
            botaoRoteiro = Botao("Botao_Vamos", cartao.transform, "Vamos!", "Verde", new Vector2(160, -215), new Vector2(340, 110), 46);
            botaoOuvirRoteiro = Botao("Botao_Ouvir_Roteiro", cartao.transform, "Ouvir", "Petroleo_Claro", new Vector2(-230, -215), new Vector2(220, 110), 36);
            hud.botaoOuvirRoteiro = botaoOuvirRoteiro.gameObject;
            cartao.gameObject.SetActive(false);

            hud.corGanha = Color.white;
            hud.corPerdida = new Color(.4f, .45f, .5f, .3f);
            return hud;
        }
    }
}
