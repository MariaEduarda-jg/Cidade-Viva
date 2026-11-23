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
    // Monta do zero a mecânica "Central de Sinais" (semáforo, semáforo de pedestre, sinal verde, sinal
    // amarelo e sinal vermelho), com os dois modos na mesma cena: "Motorista Mirim" (dirigir obedecendo
    // os semáforos) e "Sinaleiro" (acender os semáforos de um cruzamento). Gera a config, os níveis de
    // cada modo e a cena Minigame_Sinais, e confere se os trechos citados existem nas definições.
    public static class SinaisConstrutor
    {
        const string CaminhoDaCena = "Assets/Scenes/Minigame_Sinais.unity";
        const string CaminhoDaConfig = "Assets/Configs/Sinais_Config.asset";
        const string CaminhoDosNiveisDoMotorista = "Assets/Configs/Niveis/Niveis_SinaisMotorista.asset";
        const string CaminhoDosNiveisDoSinaleiro = "Assets/Configs/Niveis/Niveis_SinaisSinaleiro.asset";
        public const string NomeDaCena = "Minigame_Sinais";
        // Nomes com que o progresso de cada modo é guardado no perfil.
        public const string AreaDoMotorista = "Motorista Mirim";
        public const string AreaDoSinaleiro = "Sinaleiro";

        [MenuItem("Cidade Viva/Minigames/Reconstruir a cena da Central de Sinais")]
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

            // Depois de criar a cena: trocar de cena descarrega assets que ninguém está usando.
            PrepararIcones();
            VerbetesConstrutor.Atualizar();
            var config = PrepararConfig();
            ConferirTrechos();
            var niveisDoMotorista = SalvarNiveisDoMotorista();
            var niveisDoSinaleiro = SalvarNiveisDoSinaleiro();

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 45f;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AmbienteConstrutor.CaminhoPrefab) == null) AmbienteConstrutor.CriarPrefab();
            AmbienteConstrutor.AplicarNaCenaAberta(true);
            cam.transform.SetPositionAndRotation(F.V(0, 7.5f, -11f), Quaternion.Euler(24f, 0, 0));

            var motorista = MundoDoMotorista();
            var sinaleiro = MundoDoSinaleiro();

            // ---------------- efeitos ----------------
            var efeitos = F.Grupo("Efeitos").transform;
            var estrelinhas = TravessiaConstrutor.Particulas(efeitos, "Estrelinhas", 14, .7f, 3.5f, .3f, .6f, "Mostarda", "Mostarda_Clara", "Branco");
            var confete = TravessiaConstrutor.Particulas(efeitos, "Confete", 70, 1.6f, 6.5f, .3f, 2.5f,
                "Mostarda", "Vermelho", "Petroleo_Claro", "Verde", "Rosa", "Laranja");
            var fumaca = TravessiaConstrutor.Particulas(efeitos, "Fumaca", 16, .9f, 2.2f, .55f, -.2f, "Cinza_Claro", "Cinza", "Branco");

            // ---------------- interface ----------------
            var canvas = NovoCanvas("Canvas");
            NovoEventSystem(false);
            var hud = Hud(canvas.transform, motorista, sinaleiro);
            var fim = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabFim), canvas.transform);
            var selecao = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NiveisConstrutor.PrefabSelecao), canvas.transform);
            var abas = Abas(canvas.transform, hud);

            // ---------------- gerente ----------------
            var gerenteGo = new GameObject("SinaisManager", typeof(AudioSource));
            var som = gerenteGo.GetComponent<AudioSource>();
            som.playOnAwake = false;
            som.volume = .8f;
            var gerente = gerenteGo.AddComponent<SinaisManager>();
            gerente.nomeDaArea = AreaDoMotorista;
            gerente.telaFimDeJogo = fim.GetComponent<TelaFimDeJogo>();
            gerente.telaDeSelecao = selecao.GetComponent<TelaSelecaoDeNivel>();
            gerente.niveis = niveisDoMotorista;
            gerente.config = config;
            gerente.hud = hud;
            gerente.som = som;
            gerente.cameraDoJogo = cam;
            gerente.niveisPorModo = new[] { niveisDoMotorista, niveisDoSinaleiro };
            gerente.areasPorModo = new[] { AreaDoMotorista, AreaDoSinaleiro };
            gerente.mundosPorModo = new[] { motorista.transform.parent.gameObject, sinaleiro.mundo.gameObject };
            gerente.motorista = motorista;
            gerente.sinaleiro = sinaleiro;
            gerente.estrelinhas = estrelinhas;
            gerente.confete = confete;
            gerente.fumaca = fumaca;
            for (int i = 0; i < abas.Length; i++)
                UnityEventTools.AddIntPersistentListener(abas[i].onClick, gerente.TrocarModo, i);

            EditorSceneManager.SaveScene(cena, CaminhoDaCena);
            if (EditorBuildSettings.scenes.All(s => s.path != CaminhoDaCena))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(CaminhoDaCena, true)).ToArray();
        }

        // ------------------------------------------------------------------ semáforos

        static Renderer[] Luzes(string nome, GameObject[] postes)
        {
            return postes.SelectMany(p => p.GetComponentsInChildren<Renderer>(true)).Where(r => r.name == nome).ToArray();
        }

        // Liga um SinalDeTransito às luzes dos postes (prefab Semaforo_Carros: três esferas).
        static SinalDeTransito SinalDeVeiculos(Transform pai, params GameObject[] postes)
        {
            var sinal = F.Grupo("Sinal_De_Veiculos", pai).AddComponent<SinalDeTransito>();
            sinal.luzesVermelhas = Luzes("Luz_Vermelha", postes);
            sinal.luzesAmarelas = Luzes("Luz_Amarela", postes);
            sinal.luzesVerdes = Luzes("Luz_Verde", postes);
            sinal.materialVermelho = ArtePaleta.Mat("Luz_Vermelha");
            sinal.materialAmarelo = ArtePaleta.Mat("Luz_Amarela");
            sinal.materialVerde = ArtePaleta.Mat("Luz_Verde");
            sinal.materialApagado = ArtePaleta.Mat("Luz_Apagada");
            return sinal;
        }

        // Semáforo de pedestre (prefab Semaforo_Pedestres: os quadros "pare" e "siga"): só vermelho e verde.
        static SinalDeTransito SinalDePedestres(Transform pai, params GameObject[] postes)
        {
            var sinal = F.Grupo("Sinal_De_Pedestre", pai).AddComponent<SinalDeTransito>();
            sinal.luzesVermelhas = Luzes("Luz_Pare", postes);
            sinal.luzesVerdes = Luzes("Luz_Siga", postes);
            return sinal;
        }

        // ------------------------------------------------------------------ mundo do Motorista Mirim

        enum Lugar { Nenhum, Bairro, Praca, Escola, Obra, Bonde, Hospital }

        // O passeio: cada trecho é uma reta, com um lugar (a "emQue" metros do começo dela) e a curva que
        // vem depois (1 = direita, -1 = esquerda, 0 = acabou). Direita e esquerda se alternam, então a
        // rota nunca volta por cima de si mesma.
        static readonly (float reta, Lugar lugar, float emQue, int curva)[] Trechos =
        {
            (92f, Lugar.Bairro, 66f, 1),
            (72f, Lugar.Praca, 46f, -1),
            (72f, Lugar.Escola, 46f, 1),
            (110f, Lugar.Obra, 46f, -1),
            (72f, Lugar.Bonde, 46f, 1),
            (100f, Lugar.Hospital, 46f, 0),
        };
        const float RaioDaCurva = 14f, LadoDaPista = 1.8f, PassoDaRota = .25f, Prolongamento = 300f, ComprimentoDaTransversal = 640f;

        static Vector3 Frente(float giro) { float r = giro * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(r), 0, Mathf.Cos(r)); }
        static Vector3 Direita(float giro) { float r = giro * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(r), 0, -Mathf.Sin(r)); }

        // Pedaço reto de pista entre dois pontos.
        static void Pedaco(Transform pai, string nome, string mat, Vector3 a, Vector3 b, float largura, float yBase, float altura)
        {
            var go = SemSombra(F.Cubo(pai, nome, mat, (a + b) / 2f + Vector3.up * (yBase + altura / 2f),
                F.V(largura, altura, Vector3.Distance(a, b) + .5f)));
            go.transform.rotation = Quaternion.LookRotation(b - a);
        }

        static void PistaEntre(Transform chao, Vector3 a, Vector3 b, float giroA, float giroB)
        {
            Pedaco(chao, "Pista", "Asfalto", a, b, 7.2f, -.09f, .1f);
            foreach (float lado in new[] { -4.6f, 4.6f })
                Pedaco(chao, "Calcada", "Cinza_Claro", a + Direita(giroA) * lado, b + Direita(giroB) * lado, 2f, -.02f, .1f);
        }

        static SinaisMotorista MundoDoMotorista()
        {
            var mundo = F.Grupo("Mundo_Motorista").transform;
            var chao = F.Grupo("Chao", mundo).transform;
            var tracos = F.Grupo("Tracejado", chao).transform;
            var bairro = F.Grupo("Bairro", mundo).transform;
            var modo = F.Grupo("Motorista", mundo).AddComponent<SinaisMotorista>();
            var lista = new List<SinaisCruzamento>();

            string[] casas = { "Casa_01", "Casa_02", "Casa_03", "Casa_05", "Casa_06", "Casa_07", "Casa_08" };
            string[] arvores = { "Arvore_Comum_1", "Arvore_Comum_2", "Arvore_Comum_3", "Arvore_Betula" };

            var centro = new List<Vector3>();   // meio da rua, ponto a ponto
            var lados = new List<Vector3>();    // quanto o carrinho sai pra direita do meio da rua (já em mundo)
            Vector3 pos = F.V(0, 0, -20f);
            float giro = 0f;
            int n = 0;
            float paraOTraco = 0f;
            var limites = new Bounds(pos, Vector3.zero);

            void Ponto(float desvio)
            {
                centro.Add(pos);
                lados.Add(Direita(giro) * desvio);
                limites.Encapsulate(pos);
                paraOTraco -= 1f;
                if (paraOTraco <= 0f)
                {
                    paraOTraco = 4f;
                    var traco = SemSombra(F.Cubo(tracos, "Traco", "Branco", pos + Vector3.up * .016f, F.V(.14f, .012f, 1.6f)));
                    traco.transform.rotation = Quaternion.LookRotation(Frente(giro));
                }
            }

            foreach (var t in Trechos)
            {
                Vector3 comeco = pos;
                Vector3 fimDaReta = pos + Frente(giro) * t.reta;
                PistaEntre(chao, comeco, fimDaReta, giro, giro);

                // Casas e árvores nas retas, longe do lugar e das curvas (a câmera passa por dentro delas).
                for (float d = 10f; d < t.reta - 10f; d += 15f, n++)
                {
                    if (Mathf.Abs(d - t.emQue) < 24f) continue;
                    float lado = n % 2 == 0 ? -1f : 1f;
                    Vector3 onde = comeco + Frente(giro) * d;
                    P(bairro, "Predios/" + casas[n % casas.Length], onde + Direita(giro) * lado * 11.5f, giro + (lado < 0 ? 90 : -90));
                    P(bairro, "Natureza/" + arvores[n % arvores.Length], onde + Direita(giro) * -lado * 9f, n * 47, .8f);
                }

                // Lugar com semáforo.
                var raiz = F.Grupo("Lugar_" + t.lugar, mundo, comeco + Frente(giro) * t.emQue, F.V(0, giro, 0));
                lista.Add(MontarLugar(raiz, t.lugar, lista.Count));

                for (float d = 0f; d < t.reta; d += 1f)
                {
                    float desvio = LadoDaPista;
                    if (t.lugar == Lugar.Obra)
                    {
                        // Na obra o carrinho passa pela outra mão, depois da linha.
                        float z = d - t.emQue;
                        float meio = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-4f, 4f, z)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(12f, 20f, z)));
                        desvio = Mathf.Lerp(LadoDaPista, -LadoDaPista, meio);
                    }
                    Ponto(desvio);
                    pos += Frente(giro);
                }

                if (t.curva == 0) continue;
                const float degrau = 10f;
                float passo = RaioDaCurva * degrau * Mathf.Deg2Rad;
                Vector3 eixo = pos + Direita(giro) * RaioDaCurva * t.curva;
                for (float a = 0f; a < 90f; a += degrau)
                {
                    Vector3 antes = pos;
                    float giroAntes = giro;
                    Ponto(LadoDaPista);
                    giro += degrau * t.curva;
                    pos = eixo - Direita(giro) * RaioDaCurva * t.curva;
                    PistaEntre(chao, antes, pos, giroAntes, giro);
                    if (passo > 1.5f)
                    {
                        // Pontos intermediários deixam a curva lisa na rota.
                        Vector3 meioDoArco = eixo - Direita(giroAntes + degrau * t.curva / 2f) * RaioDaCurva * t.curva;
                        Vector3 guardado = pos;
                        float giroGuardado = giro;
                        pos = meioDoArco;
                        giro = giroAntes + degrau * t.curva / 2f;
                        Ponto(LadoDaPista);
                        pos = guardado;
                        giro = giroGuardado;
                    }
                }
            }
            SemSombra(F.Plano(chao, "Gramado", "Verde_Claro", limites.center + Vector3.down * .12f, limites.size.x + 1400f, limites.size.z + 1400f));
            // A rua continua antes do começo e depois do fim do passeio, até sumir na neblina.
            PistaEntre(chao, F.V(0, 0, -20f) + Vector3.back * Prolongamento, F.V(0, 0, -20f), 0f, 0f);
            PistaEntre(chao, pos, pos + Frente(giro) * Prolongamento, giro, giro);

            // A rota do carrinho, com pontos a distâncias iguais.
            var pista = new List<Vector3>();
            for (int i = 0; i < centro.Count; i++) pista.Add(centro[i] + lados[i] + Vector3.up * .01f);
            var rota = new List<Vector3> { pista[0] };
            float sobra = 0f;
            for (int i = 1; i < pista.Count; i++)
            {
                Vector3 a = pista[i - 1], b = pista[i];
                float tam = Vector3.Distance(a, b);
                float d = PassoDaRota - sobra;
                while (d <= tam)
                {
                    rota.Add(Vector3.Lerp(a, b, d / tam));
                    d += PassoDaRota;
                }
                sobra = tam - (d - PassoDaRota);
            }
            modo.rota = rota.ToArray();
            modo.passoDaRota = PassoDaRota;
            modo.ladoDaPista = LadoDaPista;
            modo.cruzamentos = lista.ToArray();

            // Onde fica a linha de cada lugar, medido na rota.
            foreach (var c in lista)
            {
                Vector3 linha = c.transform.TransformPoint(F.V(LadoDaPista, 0, ZDaLinha));
                int melhor = 0;
                float menor = float.MaxValue;
                for (int i = 0; i < rota.Count; i++)
                {
                    float dist = (rota[i] - linha).sqrMagnitude;
                    if (dist < menor) { menor = dist; melhor = i; }
                }
                c.sDaLinha = melhor * PassoDaRota;
            }

            // Arco de chegada (o jogo põe depois do último lugar do nível).
            var chegada = F.Grupo("Chegada", mundo).transform;
            foreach (float x in new[] { -4.2f, 4.2f })
                F.Cubo(chegada, "Poste", "Petroleo", F.V(x, 2.6f, 0), F.V(.4f, 5.2f, .4f));
            F.Cubo(chegada, "Faixa", "Mostarda", F.V(0, 5f, 0), F.V(8.8f, 1f, .3f));
            for (int i = 0; i < 8; i++)
                SemSombra(F.Cubo(chegada, "Xadrez", i % 2 == 0 ? "Branco" : "Grafite", F.V(-3.15f + i * .9f, .02f, 0), F.V(.9f, .014f, 1f)));
            modo.chegada = chegada;

            modo.carro = P(mundo, "Carros/Carro_Taxi", rota[0]).transform;
            modo.carro.name = "Carrinho";
            return modo;
        }

        const float ZDaLinha = -6.5f;

        // Semáforos dos veículos dos dois lados da pista, virados pra quem vem, e a linha de parada.
        static SinaisCruzamento BaseDoLugar(GameObject raiz)
        {
            var c = raiz.AddComponent<SinaisCruzamento>();
            var t = raiz.transform;
            SemSombra(F.Cubo(t, "Linha_De_Parada", "Branco", F.V(LadoDaPista, .022f, ZDaLinha), F.V(3.4f, .014f, .4f)));
            c.sinal = SinalDeVeiculos(t,
                P(t, "Props/Semaforo_Carros", F.V(5.3f, .08f, ZDaLinha - .4f), 180, 1.7f),
                P(t, "Props/Semaforo_Carros", F.V(-5.3f, .08f, ZDaLinha - .4f), 180, 1.7f));
            return c;
        }

        // Faixa de pedestre logo depois da linha, com semáforo de pedestre e quem atravessa.
        static void FaixaComPedestres(SinaisCruzamento c, params string[] gente)
        {
            var t = c.transform;
            var faixa = F.Grupo("Faixa_De_Pedestre", t).transform;
            for (int i = 0; i < 8; i++)
                SemSombra(F.Cubo(faixa, "Listra", "Branco", F.V(-3.15f + i * .9f, .022f, -4.6f), F.V(.45f, .014f, 2.2f)));
            var pedestres = F.Grupo("Pedestres", t).transform;
            c.sinalDePedestre = SinalDePedestres(t,
                P(pedestres, "Props/Semaforo_Pedestres", F.V(-5.4f, .08f, -3f), 90, 1.3f),
                P(pedestres, "Props/Semaforo_Pedestres", F.V(5.4f, .08f, -3f), -90, 1.3f));
            var lista = new List<Transform>();
            for (int i = 0; i < gente.Length; i++)
            {
                float lado = i % 2 == 0 ? -1f : 1f;
                lista.Add(P(pedestres, gente[i], F.V(lado * 4.2f, .08f, -5.3f + i * .6f), lado * 90f).transform);
            }
            c.pedestres = lista.ToArray();
        }

        // Rua transversal e os veículos que passam por ela (um de cada lado).
        static void Transversal(SinaisCruzamento c)
        {
            SemSombra(F.Caixa(c.transform, "Transversal", "Asfalto", 0, -.01f, 0, ComprimentoDaTransversal, .1f, 7f));
        }

        static void Veiculos(SinaisCruzamento c, params (string prefab, Vector3 pos, float giro, Vector3 direcao)[] lista)
        {
            var grupo = F.Grupo("Quem_Passa", c.transform).transform;
            c.carros = lista.Select(v => P(grupo, v.prefab, v.pos, v.giro).transform).ToArray();
            c.direcaoDosCarros = lista.Select(v => v.direcao).ToArray();
        }

        // Cada lugar tem um motivo diferente pra ter semáforo.
        static SinaisCruzamento MontarLugar(GameObject raiz, Lugar lugar, int indice)
        {
            var c = BaseDoLugar(raiz);
            var t = raiz.transform;
            switch (lugar)
            {
                case Lugar.Bairro:
                    c.recadoDoLugar = "Cruzamento: tem carro na outra rua.";
                    Transversal(c);
                    FaixaComPedestres(c, "Personagens/Pedestre_Mulher", "Personagens/Idoso");
                    // Na outra rua: os carros vêm de longe, param na linha deles e passam um de cada vez.
                    SemSombra(F.Cubo(t, "Linha_Da_Outra_Rua", "Branco", F.V(-9.6f, .1f, -1.7f), F.V(.4f, .014f, 3.2f)));
                    SemSombra(F.Cubo(t, "Linha_Da_Outra_Rua", "Branco", F.V(9.6f, .1f, 1.7f), F.V(.4f, .014f, 3.2f)));
                    Veiculos(c, ("Carros/Carro_Hatch", F.V(-12f, .09f, -1.7f), 90f, Vector3.right), ("Carros/Carro_Van", F.V(12f, .09f, 1.7f), -90f, Vector3.left));
                    c.esperaDosCarros = 18f;
                    c.fimDosCarros = 45f;
                    break;
                case Lugar.Praca:
                    c.recadoDoLugar = "Praça: muita gente passeando.";
                    FaixaComPedestres(c, "Personagens/Familia_Mae_Animado", "Personagens/Idosa");
                    P(t, "Predios/Padaria", F.V(14f, 0, 8f), -90, .85f);
                    P(t, "Props/Banco_De_Praca", F.V(-8f, 0, 3f), 90);
                    P(t, "Props/Banco_De_Praca", F.V(-8f, 0, 9f), 90);
                    P(t, "Natureza/Flores", F.V(-11f, 0, 6f), 0, 1.6f);
                    P(t, "Natureza/Arvore_Comum_1", F.V(-13f, 0, 12f), 40, .9f);
                    P(t, "Natureza/Arvore_Comum_3", F.V(-13f, 0, -2f), 120, .9f);
                    break;
                case Lugar.Escola:
                    c.recadoDoLugar = "Escola: as crianças vão atravessar.";
                    FaixaComPedestres(c, "Personagens/Familia_Filha_Animado", "Personagens/Familia_FilhoMaisNovo_Animado", "Personagens/Mini_Mulher_1");
                    P(t, "Predios/Escola", F.V(-16f, 0, 8f), 90);
                    P(t, "Carros/Onibus_Escolar", F.V(LadoDaPista + 6f, .02f, 10f));
                    break;
                case Lugar.Obra:
                    c.recadoDoLugar = "Obra: uma mão de cada vez!";
                    foreach (float z in new[] { 2f, 6f, 10f, 14f, 18f })
                        P(t, "Enchente/Cone_Obra_1", F.V(.6f, .01f, z));
                    P(t, "Props/Barreira_Obra", F.V(2f, .01f, 2f));
                    P(t, "Props/Barreira_Obra", F.V(2f, .01f, 18f), 180);
                    SemSombra(F.Cubo(t, "Terra", "Cinza", F.V(2.2f, .05f, 10f), F.V(2.4f, .1f, 14f)));
                    P(t, "Personagens/Trabalhador", F.V(2.2f, .1f, 8f), -90);
                    P(t, "Personagens/Trabalhadora", F.V(2.4f, .1f, 12f), 200);
                    // Do outro lado da obra há outra linha e outro semáforo: a outra mão espera ali, em fila,
                    // e passa quando o carrinho está no vermelho (uma mão de cada vez).
                    SemSombra(F.Cubo(t, "Linha_Da_Outra_Mao", "Branco", F.V(-LadoDaPista, .022f, 25f), F.V(3.4f, .014f, .4f)));
                    c.sinalDaOutraMao = SinalDeVeiculos(t,
                        P(t, "Props/Semaforo_Carros", F.V(-5.3f, .08f, 25.4f), 0, 1.7f),
                        P(t, "Props/Semaforo_Carros", F.V(5.3f, .08f, 25.4f), 0, 1.7f));
                    Veiculos(c, ("Carros/Carro_SUV", F.V(-LadoDaPista, .01f, 28f), 180f, Vector3.back), ("Carros/Carro_Picape", F.V(-LadoDaPista, .01f, 36f), 180f, Vector3.back));
                    c.esperaDosCarros = 38f;
                    c.fimDosCarros = 22f;
                    c.velocidadeDosCarros = 8f;
                    break;
                case Lugar.Bonde:
                    c.recadoDoLugar = "Trilhos: o bonde vai passar.";
                    Transversal(c);
                    foreach (float z in new[] { -.75f, .75f })
                        SemSombra(F.Cubo(t, "Trilho", "Grafite", F.V(0, .1f, z), F.V(ComprimentoDaTransversal, .04f, .14f)));
                    // O bonde não fica parado esperando: sai de longe quando o sinal do carrinho fica amarelo.
                    Veiculos(c, ("Carros/Metro", F.V(-22f, .1f, 0), 90f, Vector3.right));
                    c.surgeNoAmarelo = true;
                    c.esperaDosCarros = 44f;
                    c.fimDosCarros = 58f;
                    c.velocidadeDosCarros = 10f;
                    break;
                case Lugar.Hospital:
                    c.recadoDoLugar = "Hospital: a ambulância sai por aqui.";
                    Transversal(c);
                    FaixaComPedestres(c, "Personagens/Medico", "Personagens/Medica");
                    P(t, "Obras/Hospital", F.V(20f, 0, 23f), -90);
                    // Também vem de longe, em movimento, quando o sinal do carrinho fica amarelo.
                    Veiculos(c, ("Carros/Ambulancia", F.V(14f, .09f, 1.7f), -90f, Vector3.left));
                    c.surgeNoAmarelo = true;
                    c.esperaDosCarros = 28f;
                    c.fimDosCarros = 70f;
                    c.velocidadeDosCarros = 10f;
                    break;
            }
            return c;
        }

        // ------------------------------------------------------------------ mundo do Sinaleiro

        // Um cruzamento visto de cima: via A (azul) em X, via B (laranja) em Z, faixa de pedestres na via A.
        static SinaisSinaleiro MundoDoSinaleiro()
        {
            // Bem longe da avenida do outro modo.
            var mundo = F.Grupo("Mundo_Sinaleiro", null, F.V(600, 0, 0)).transform;
            var modo = mundo.gameObject.AddComponent<SinaisSinaleiro>();
            modo.mundo = mundo;

            var chao = F.Grupo("Chao", mundo).transform;
            SemSombra(F.Plano(chao, "Gramado", "Verde_Claro", F.V(0, -.12f, 0), 300, 300));
            SemSombra(F.Caixa(chao, "Via_A", "Asfalto", 0, -.09f, 0, 96f, .1f, 7f));
            SemSombra(F.Caixa(chao, "Via_B", "Asfalto", 0, -.085f, 0, 7f, .1f, 96f));
            foreach (float x in new[] { -9.5f, 9.5f })
                foreach (float z in new[] { -9.5f, 9.5f })
                    SemSombra(F.Caixa(chao, "Calcada", "Cinza_Claro", x, -.02f, z, 12f, .1f, 12f));
            // Via A com bordas azuis e linha de parada.
            foreach (float z in new[] { -3.3f, 3.3f })
                foreach (float x in new[] { -27f, 27f })
                    SemSombra(F.Cubo(chao, "Borda_Azul", "Petroleo_Claro", F.V(x, .02f, z), F.V(42f, .014f, .3f)));
            SemSombra(F.Cubo(chao, "Linha_Da_Via_A", "Branco", F.V(modo.linhaDaVia[0] + .2f, .022f, modo.pistaDaVia[0]), F.V(.4f, .014f, 3.2f)));
            for (float x = -38f; x <= -16f; x += 11f)
            {
                SemSombra(F.Cubo(chao, "Seta_Azul", "Petroleo_Claro", F.V(x, .02f, modo.pistaDaVia[0]), F.V(2f, .014f, .3f)));
                SemSombra(F.Cubo(chao, "Seta_Azul_Ponta", "Petroleo_Claro", F.V(x + 1.1f, .02f, modo.pistaDaVia[0]), F.V(.4f, .014f, 1f)));
            }

            // O que é só da via B: bordas laranja, linha, setas e o semáforo dela.
            var viaB = F.Grupo("Enfeites_Da_Via_B", mundo).transform;
            foreach (float x in new[] { -3.3f, 3.3f })
                foreach (float z in new[] { -27f, 27f })
                    SemSombra(F.Cubo(viaB, "Borda_Laranja", "Laranja", F.V(x, .024f, z), F.V(.3f, .014f, 42f)));
            SemSombra(F.Cubo(viaB, "Linha_Da_Via_B", "Branco", F.V(modo.pistaDaVia[1], .026f, modo.linhaDaVia[1] + .2f), F.V(3.2f, .014f, .4f)));
            for (float z = -38f; z <= -16f; z += 11f)
            {
                SemSombra(F.Cubo(viaB, "Seta_Laranja", "Laranja", F.V(modo.pistaDaVia[1], .024f, z), F.V(.3f, .014f, 2f)));
                SemSombra(F.Cubo(viaB, "Seta_Laranja_Ponta", "Laranja", F.V(modo.pistaDaVia[1], .024f, z + 1.1f), F.V(1f, .014f, .4f)));
            }
            modo.enfeitesDaViaB = viaB.gameObject;

            // O que é só da faixa de pedestres: as listras e o semáforo de pedestre.
            var faixa = F.Grupo("Enfeites_Da_Faixa", mundo).transform;
            for (int i = 0; i < 8; i++)
                SemSombra(F.Cubo(faixa, "Listra", "Branco", F.V(modo.xDosPedestres, .024f, -3.15f + i * .9f), F.V(3.2f, .014f, .45f)));
            modo.enfeitesDaFaixa = faixa.gameObject;

            // Semáforos grandes e virados pra câmera, cada um com a base da cor da via dele.
            GameObject Poste(Transform pai, string prefab, string corDaBase, Vector3 pos, float escala)
            {
                F.Cilindro(pai, "Base_" + corDaBase, corDaBase, pos + F.V(0, .06f, 0), F.V(1.9f, .12f, 1.9f));
                return P(pai, prefab, pos + F.V(0, .1f, 0), 180, escala);
            }
            var sinais = F.Grupo("Semaforos", mundo).transform;
            var sinalA = SinalDeVeiculos(sinais, Poste(sinais, "Props/Semaforo_Carros", "Petroleo_Claro", F.V(-11.6f, 0, -5.4f), 2.3f));
            sinalA.name = "Sinal_Da_Via_A";
            var sinalB = SinalDeVeiculos(viaB, Poste(viaB, "Props/Semaforo_Carros", "Laranja", F.V(5.6f, 0, -7f), 2.3f));
            sinalB.name = "Sinal_Da_Via_B";
            modo.sinaisDasVias = new[] { sinalA, sinalB };
            modo.sinalDePedestre = SinalDePedestres(faixa, Poste(faixa, "Props/Semaforo_Pedestres", "Cinza_Claro", F.V(-5.2f, 0, -5.4f), 2.2f));

            // Bairro: coisas altas só do lado de lá.
            P(mundo, "Predios/Casa_02", F.V(-11f, 0, 12f), 180);
            P(mundo, "Predios/Padaria", F.V(11f, 0, 12.5f), 180, .85f);
            P(mundo, "Natureza/Arvore_Comum_1", F.V(-22f, 0, 9f), 30, .9f);
            P(mundo, "Natureza/Arvore_Comum_2", F.V(22f, 0, 9f), 100, .9f);
            P(mundo, "Natureza/Flores", F.V(9f, 0, -8f), 0, 1.6f);
            P(mundo, "Props/Banco_De_Praca", F.V(11f, .08f, -11f), 0);
            P(mundo, "Natureza/Arbusto_1", F.V(-14f, 0, -10f), 0, .9f);

            // Modelos inativos de quem chega.
            var modelos = F.Grupo("Modelos", mundo).transform;
            GameObject Modelo(string prefab)
            {
                var go = P(modelos, prefab, Vector3.zero);
                go.name = "Modelo_" + go.name;
                go.SetActive(false);
                return go;
            }
            modo.modelosDeCarro = new[] { "Carro_Hatch", "Carro_Sedan", "Carro_Taxi", "Carro_SUV", "Carro_Compacto_Azul", "Carro_Picape" }.Select(c => Modelo("Carros/" + c)).ToArray();
            modo.modeloDaAmbulancia = Modelo("Carros/Ambulancia");
            modo.modelosDePedestre = new[] { "Pedestre_Mulher", "Idoso", "Familia_Filha_Animado", "Idosa" }.Select(p => Modelo("Personagens/" + p)).ToArray();

            var pose = F.Grupo("Pose_Da_Camera", mundo, F.V(0, 37f, -30f)).transform;
            pose.LookAt(mundo.position + F.V(0, 0, -1.5f));
            modo.poseDaCamera = pose;
            return modo;
        }

        // ------------------------------------------------------------------ regras (conferência com as definições)

        // Confere os trechos citados com as definições e grava as regras em Temp/sinais_regras.txt.
        // O que NÃO vem direto das definições (lista da conferência e do Docs/Revisao_Coordenacao.md).
        public static readonly string[] NaoVemDasDefinicoes =
        {
                         "Parar \"antes da linha\" e \"esperar o verde\": a definição de sinal vermelho só diz \"devem parar\".",
                         "Quanto é \"diminuir a velocidade\" no amarelo: o jogo exige cruzar a linha a no máximo metade da velocidade máxima; a definição não dá número e não manda parar.",
                         "A ordem das luzes (verde, amarelo, vermelho, verde) e quanto tempo cada uma dura.",
                         "Motorista Mirim: o amarelo acender quando o carrinho chega perto (é pra garantir que a criança encontre o amarelo). Depois de um tempo fixo ele vira vermelho. O acerto do amarelo é baixar a velocidade: cruzar a linha rápido é erro.",
                         "Motorista Mirim: os lugares do passeio (cruzamento, praça, escola, obra com uma mão de cada vez, trilhos do bonde, saída do hospital). Nenhuma definição fala deles; são situações escolhidas pro semáforo fazer sentido.",
                         "A tolerância: sinal que acabou de mudar há menos de 0,7 s ainda vale a cor de antes, se ela exigir menos.",
                         "No Sinaleiro, carro no amarelo passa devagar em vez de parar (segue a definição ao pé da letra).",
                         "Que carros de duas vias, ou carro e pedestre, não podem passar juntos: as definições falam em \"organizar a passagem\", não em conflito. A \"quase batida\" é encenação do jogo.",
                         "O semáforo de pedestre abrir junto com o vermelho dos carros (Motorista Mirim): é o ciclo que o jogo escolheu.",
                         "A AMBULÂNCIA: a definição dela não fala de semáforo. No jogo ela obedece o sinal como os outros carros e conta um erro se ficar parada no vermelho por mais de 8 s. Ela não fura o sinal.",
                         "A buzina de quem espera muito: só efeito sonoro, sem regra.",
                         "Em que jogada cada cartão abre: sinal verde, amarelo e vermelho no Motorista Mirim (primeira vez que a regra é cumprida); semáforo e semáforo de pedestre no Sinaleiro (primeiro carro e primeiro pedestre que passam no verde).",
        };
        // Regras da última conferência: {verbete, trecho literal, regra} (usadas pelo RevisaoConstrutor).
        public static readonly List<string[]> RegrasDaRevisao = new List<string[]>();

        [MenuItem("Cidade Viva/Verbetes/Conferir trechos da Central de Sinais com as definições")]
        public static bool ConferirTrechos()
        {
            var config = AssetDatabase.LoadAssetAtPath<SinaisConfig>(CaminhoDaConfig);
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
                lista.AppendLine($"    definição: {v.definicao}");
            }
            lista.AppendLine("REGRAS DERIVADAS DAS DEFINIÇÕES");
            Regra("sinal_verde", config.trechoDoVerde, "com a luz verde, carro e pedestre passam (Motorista: pode cruzar a linha; Sinaleiro: carros e pedestres seguem)");
            Regra("sinal_amarelo", config.trechoDoAmarelo, "com a luz amarela, o carro tem de ir mais devagar (Motorista: cruzar a linha devagar; Sinaleiro: os carros passam devagar)");
            Regra("sinal_vermelho", config.trechoDoVermelho, "com a luz vermelha, carro e pedestre param (Motorista: não pode cruzar a linha; Sinaleiro: param e fazem fila)");
            Regra("semaforo", config.trechoDoSemaforo, "o semáforo organiza a passagem de carros e pessoas (Sinaleiro: abrir uma passagem de cada vez)");
            Regra("semaforo", "luzes vermelha, amarela e verde", "o semáforo de veículos tem três luzes (painel da via: verde, amarelo e vermelho)");
            Regra("semaforo_de_pedestre", config.trechoDoSemaforoDePedestre, "o semáforo de pedestre organiza a passagem de pedestres (eles só atravessam com o verde dele)");
            Regra("semaforo_de_pedestre", "luzes vermelha e verde", "o semáforo de pedestre só tem duas luzes (painel: verde e vermelho, sem amarelo)");

            lista.AppendLine().AppendLine("O QUE NÃO VEM DIRETO DAS DEFINIÇÕES (escolhas do jogo, pra revisão)");
            foreach (string item in NaoVemDasDefinicoes)
                lista.AppendLine("  - " + item);

            lista.AppendLine().AppendLine(falhas.Count == 0 ? "CONFERÊNCIA: tudo certo." : "CONFERÊNCIA: " + falhas.Count + " falha(s):");
            foreach (string f in falhas) lista.AppendLine("  - " + f);
            File.WriteAllText("Temp/sinais_regras.txt", lista.ToString(), Encoding.UTF8);
            if (falhas.Count == 0) Debug.Log("[Sinais] Trechos conferidos com as definições. Lista em Temp/sinais_regras.txt.");
            else Debug.LogError("[Sinais] Trechos que não batem com as definições:\n" + string.Join("\n", falhas));
            return falhas.Count == 0;
        }

        // ------------------------------------------------------------------ níveis e config

        static NivelConfig Base(string objetivo, float tempo, string[] regras)
        {
            return new NivelConfig
            {
                objetivo = objetivo, tempoEmSegundos = tempo, regrasExtras = new List<string>(regras),
                // As estrelas são calculadas pelo SinaisManager (sem erros e no tempo); isto só alimenta o texto da seleção.
                estrelas = new CriterioDeEstrelas { medida = MedidaDeEstrelas.Erros, para2Estrelas = 1, para3Estrelas = 0 },
            };
        }

        static NivelConfig Motorista(string objetivo, float tempo, float verde, float amarelo, float vermelho, params string[] regras)
        {
            var nivel = Base(objetivo, tempo, regras);
            nivel.extras.Add(new ParametroExtra { nome = "tempoVerde", valor = verde });
            nivel.extras.Add(new ParametroExtra { nome = "tempoAmarelo", valor = amarelo });
            nivel.extras.Add(new ParametroExtra { nome = "tempoVermelho", valor = vermelho });
            return nivel;
        }

        static NivelConfig Sinaleiro(string objetivo, int carros, int pedestres, float intervalo, float tempo, params string[] regras)
        {
            var nivel = Base(objetivo, tempo, regras);
            nivel.quantidadeDeItens = carros;
            nivel.extras.Add(new ParametroExtra { nome = "pedestres", valor = pedestres });
            nivel.extras.Add(new ParametroExtra { nome = "intervalo", valor = intervalo });
            return nivel;
        }

        static LevelConfig Salvar(string caminho, params NivelConfig[] niveis)
        {
            ArtePaleta.GarantirPasta(Path.GetDirectoryName(caminho).Replace('\\', '/'));
            var asset = AssetDatabase.LoadAssetAtPath<LevelConfig>(caminho);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(asset, caminho);
            }
            asset.niveis = new List<NivelConfig>(niveis);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static LevelConfig SalvarNiveisDoMotorista()
        {
            // Três níveis, cada um com um papel (os lugares e os semáforos de cada um estão em PrepararConfig):
            // 1 aprende as três luzes, uma de cada vez; 2 os sinais mudam quando o carrinho chega; 3 o passeio inteiro.
            return Salvar(CaminhoDosNiveisDoMotorista,
                Motorista("Aprenda as três luzes: verde, amarela e vermelha!", 70f, 6f, 5f, 5f, SinaisManager.RegraDicaVisivel),
                Motorista("Fique de olho: o sinal muda quando você chega perto!", 90f, 6f, 5f, 5f),
                Motorista("O passeio inteiro pelo bairro, com tempo!", 170f, 6f, 5f, 5f));
        }

        static LevelConfig SalvarNiveisDoSinaleiro()
        {
            const string duas = SinaisSinaleiro.RegraDuasVias, pedestres = SinaisSinaleiro.RegraPedestres, ambulancia = SinaisSinaleiro.RegraAmbulancia;
            return Salvar(CaminhoDosNiveisDoSinaleiro,
                Sinaleiro("Uma via só. Abra o verde para os carros passarem!", 6, 0, 3.5f, 60f),
                Sinaleiro("Duas vias! Um verde de cada vez.", 10, 0, 3.5f, 90f, duas),
                Sinaleiro("Agora os pedestres também querem passar.", 10, 4, 3.5f, 110f, duas, pedestres),
                Sinaleiro("Mais carros e mais pedestres.", 16, 8, 2.6f, 130f, duas, pedestres),
                Sinaleiro("Vem uma ambulância: ela não pode ficar esperando!", 16, 8, 2.6f, 140f, duas, pedestres, ambulancia));
        }

        static SinaisConfig PrepararConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<SinaisConfig>(CaminhoDaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<SinaisConfig>();
                AssetDatabase.CreateAsset(config, CaminhoDaConfig);
            }
            const ComportamentoDoSemaforo ciclo = ComportamentoDoSemaforo.Ciclo, verde = ComportamentoDoSemaforo.FicaVerde,
                vermelho = ComportamentoDoSemaforo.FicaVermelho, fecha = ComportamentoDoSemaforo.FechaAoChegar;
            // A estas medidas, quem diminui logo que a luz acende ainda cruza no amarelo ou para no vermelho, sem pressa.
            config.distanciaDoAmarelo = 24f;
            config.tempoDoAmarelo = 5f;
            config.roteiros = new[]
            {
                // Lugares: 0 cruzamento do bairro, 1 praça, 2 escola, 3 obra, 4 trilhos do bonde, 5 hospital.
                new RoteiroDoNivel { primeiroLugar = 0, semaforos = new[] { verde, fecha, vermelho } },
                new RoteiroDoNivel { primeiroLugar = 3, semaforos = new[] { vermelho, fecha, fecha } },
                new RoteiroDoNivel { primeiroLugar = 0, semaforos = new[] { ciclo, fecha, ciclo, fecha, fecha, fecha } },
            };
            config.somDoTic = TravessiaConstrutor.Som("Interface/tick_001.ogg");
            config.somDeAcerto = TravessiaConstrutor.Som("Interface/confirmation_002.ogg");
            config.somDeErro = TravessiaConstrutor.Som("Interface/error_004.ogg");
            config.somDaTosse = TravessiaConstrutor.Som("Efeitos/tropeco.wav");
            config.somDaBuzina = TravessiaConstrutor.Som("Efeitos/buzina.wav");
            config.somDaSirene = TravessiaConstrutor.Som("Efeitos/sirene.wav");
            config.somDeComemoracao = TravessiaConstrutor.Som("Interface/confirmation_003.ogg");
            config.somDeBotao = TravessiaConstrutor.Som("Interface/select_001.ogg");
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        // ------------------------------------------------------------------ interface

        static SinaisHUD Hud(Transform canvas, SinaisMotorista motorista, SinaisSinaleiro sinaleiro)
        {
            var raiz = Tela("HUD", canvas);
            var hud = raiz.gameObject.AddComponent<SinaisHUD>();
            var voltarAoHub = raiz.gameObject.AddComponent<VoltarAoHub>();

            // Nível, os dois critérios das estrelas e os erros (em cima, à esquerda).
            var cartao = Imagem("Painel_Do_Nivel", raiz, arredondado, C("Branco", .92f), new Vector2(210, -125), new Vector2(370, 220));
            TravessiaConstrutor.Ancorar(cartao, new Vector2(0, 1));
            hud.textoDoNivel = Texto("Nivel", cartao.transform, "", 30, C("Petroleo"), new Vector2(0, 78), new Vector2(350, 44));
            var estrelas = new Image[2];
            var rotulos = new Text[2];
            for (int i = 0; i < 2; i++)
            {
                estrelas[i] = Imagem("Estrela_" + (i + 1), cartao.transform, S("figura_estrela"), Color.white, new Vector2(-85 + i * 170, 18), new Vector2(72, 72));
                rotulos[i] = Texto("Rotulo_" + (i + 1), cartao.transform, "", 30, C("Grafite"), new Vector2(-85 + i * 170, -40), new Vector2(160, 42), FontStyle.Normal);
                rotulos[i].resizeTextForBestFit = true; rotulos[i].resizeTextMinSize = 24; rotulos[i].resizeTextMaxSize = 30;
            }
            hud.estrelasDosCriterios = estrelas;
            hud.rotulosDosCriterios = rotulos;
            hud.textoDosErros = Texto("Erros", cartao.transform, "", 26, C("Cinza_Escuro"), new Vector2(0, -82), new Vector2(340, 36), FontStyle.Normal);

            var hubBotao = Botao("Botao_VoltarAoHub", raiz, "", "Petroleo_Claro", new Vector2(-100, -95), new Vector2(120, 120), 0, S("ui_seta_voltar"), circulo);
            TravessiaConstrutor.Ancorar(hubBotao, new Vector2(1, 1));
            UnityEventTools.AddPersistentListener(hubBotao.onClick, voltarAoHub.Voltar);

            // Recado do momento (em cima, no meio), relógio e dica.
            var recado = Imagem("Painel_Do_Recado", raiz, arredondado, C("Branco", .95f), new Vector2(60, -85), new Vector2(900, 110));
            TravessiaConstrutor.Ancorar(recado, new Vector2(.5f, 1));
            hud.textoDoRecado = Texto("Texto", recado.transform, "", 44, C("Petroleo"), Vector2.zero, new Vector2(860, 98));
            hud.textoDoRecado.resizeTextForBestFit = true; hud.textoDoRecado.resizeTextMinSize = 28; hud.textoDoRecado.resizeTextMaxSize = 44;

            var tempo = Imagem("Painel_Do_Tempo", raiz, arredondado, C("Petroleo_Escuro", .9f), new Vector2(60, -166), new Vector2(720, 36));
            TravessiaConstrutor.Ancorar(tempo, new Vector2(.5f, 1));
            var area = Ret("Area", tempo.transform, Vector2.zero, Vector2.zero);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(5, 5); area.offsetMax = new Vector2(-5, -5);
            var cheio = Imagem("Cheio", area, arredondado, C("Mostarda"), Vector2.zero, Vector2.zero);
            var rtCheio = (RectTransform)cheio.transform;
            rtCheio.anchorMin = Vector2.zero; rtCheio.anchorMax = Vector2.one; rtCheio.offsetMin = rtCheio.offsetMax = Vector2.zero;
            hud.painelDoTempo = tempo.gameObject;
            hud.barraDeTempo = rtCheio;

            var dica = Imagem("Painel_Da_Dica", raiz, arredondado, C("Mostarda_Clara", .97f), new Vector2(60, -228), new Vector2(1000, 72));
            TravessiaConstrutor.Ancorar(dica, new Vector2(.5f, 1));
            hud.painelDaDica = dica.gameObject;
            hud.textoDaDica = Texto("Texto", dica.transform, "", 34, C("Grafite"), Vector2.zero, new Vector2(970, 64));
            hud.textoDaDica.resizeTextForBestFit = true; hud.textoDaDica.resizeTextMinSize = 22; hud.textoDaDica.resizeTextMaxSize = 34;

            // ---------------- Motorista Mirim: frear, acelerar, velocímetro e a luz do próximo semáforo
            var dirigir = Tela("Controles_Do_Motorista", raiz);
            hud.controlesDoMotorista = dirigir.gameObject;
            var frear = Botao("Botao_Frear", dirigir, "Frear", "Vermelho", new Vector2(220, 130), new Vector2(360, 180), 46, S("ui_seta_baixo"));
            TravessiaConstrutor.Ancorar(frear, new Vector2(0, 0));
            var acelerar = Botao("Botao_Acelerar", dirigir, "Acelerar", "Verde_Escuro", new Vector2(-220, 130), new Vector2(360, 180), 46, S("ui_seta_cima"));
            TravessiaConstrutor.Ancorar(acelerar, new Vector2(1, 0));
            foreach (var (botao, acelera) in new[] { (frear, false), (acelerar, true) })
            {
                var segurar = botao.gameObject.AddComponent<BotaoDeSegurar>();
                segurar.motorista = motorista;
                segurar.acelera = acelera;
            }
            var velocimetro = Imagem("Velocimetro", dirigir, arredondado, C("Petroleo_Escuro", .92f), new Vector2(0, 110), new Vector2(640, 70));
            TravessiaConstrutor.Ancorar(velocimetro, new Vector2(.5f, 0));
            var areaDaVelocidade = Ret("Area", velocimetro.transform, Vector2.zero, Vector2.zero);
            areaDaVelocidade.anchorMin = Vector2.zero; areaDaVelocidade.anchorMax = Vector2.one;
            areaDaVelocidade.offsetMin = new Vector2(8, 8); areaDaVelocidade.offsetMax = new Vector2(-8, -8);
            var barra = Imagem("Cheio", areaDaVelocidade, arredondado, C("Petroleo_Claro"), Vector2.zero, Vector2.zero);
            hud.barraDeVelocidade = (RectTransform)barra.transform;
            hud.barraDeVelocidade.anchorMin = Vector2.zero; hud.barraDeVelocidade.anchorMax = Vector2.one;
            hud.barraDeVelocidade.offsetMin = hud.barraDeVelocidade.offsetMax = Vector2.zero;
            var marca = Imagem("Marca_De_Devagar", areaDaVelocidade, null, C("Branco"), Vector2.zero, new Vector2(8, 0));
            hud.marcaDeDevagar = (RectTransform)marca.transform;
            hud.marcaDeDevagar.anchorMin = new Vector2(.5f, 0); hud.marcaDeDevagar.anchorMax = new Vector2(.5f, 1);
            hud.marcaDeDevagar.offsetMin = new Vector2(-4, -6); hud.marcaDeDevagar.offsetMax = new Vector2(4, 6);
            var devagar = Texto("Rotulo_Devagar", velocimetro.transform, "devagar", 30, C("Branco"), new Vector2(-165, 0), new Vector2(280, 44));
            devagar.resizeTextForBestFit = true; devagar.resizeTextMinSize = 18; devagar.resizeTextMaxSize = 30;

            var painelDeLuzes = Imagem("Luz_Do_Proximo_Semaforo", dirigir, arredondado, C("Grafite", .95f), new Vector2(-110, 30), new Vector2(150, 400));
            TravessiaConstrutor.Ancorar(painelDeLuzes, new Vector2(1, .5f));
            hud.luzesDoPainel = new Image[3];
            for (int i = 0; i < 3; i++)
                hud.luzesDoPainel[i] = Imagem("Luz_" + (i + 1), painelDeLuzes.transform, circulo, Color.white, new Vector2(0, 125 - i * 125), new Vector2(110, 110));
            hud.coresDasLuzes = new[] { C("Vermelho"), C("Mostarda"), C("Verde") };
            hud.corDaLuzApagada = new Color(.22f, .25f, .28f, 1f);

            // ---------------- Sinaleiro: contador e o painel dos semáforos
            var painel = Tela("Controles_Do_Sinaleiro", raiz);
            hud.controlesDoSinaleiro = painel.gameObject;
            var contador = Imagem("Painel_Do_Contador", painel, arredondado, C("Branco", .92f), new Vector2(-210, -220), new Vector2(360, 80));
            TravessiaConstrutor.Ancorar(contador, new Vector2(1, 1));
            hud.textoDoContador = Texto("Texto", contador.transform, "", 34, C("Petroleo"), Vector2.zero, new Vector2(340, 70));

            var botoes = new List<BotaoDeSinal>();
            GameObject Grupo(int grupo, string titulo, string corDoTitulo, float x, float largura, CorDoSinal[] cores)
            {
                var caixa = Imagem("Painel_" + titulo.Replace(' ', '_'), painel, arredondado, C("Branco", .94f), new Vector2(x, 135), new Vector2(largura, 250));
                TravessiaConstrutor.Ancorar(caixa, new Vector2(.5f, 0));
                var faixaDoTitulo = Imagem("Titulo", caixa.transform, arredondado, C(corDoTitulo), new Vector2(0, 88), new Vector2(largura - 30, 58));
                Texto("Texto", faixaDoTitulo.transform, titulo, 36, C(corDoTitulo == "Cinza_Claro" ? "Grafite" : "Branco"), Vector2.zero, new Vector2(largura - 50, 50));
                for (int i = 0; i < cores.Length; i++)
                {
                    var pos = new Vector2((i - (cores.Length - 1) * .5f) * 150f, -32);
                    var aro = Imagem("Aro_" + cores[i], caixa.transform, circulo, C("Grafite"), pos, new Vector2(150, 150));
                    string cor = cores[i] == CorDoSinal.Verde ? "Verde" : cores[i] == CorDoSinal.Amarelo ? "Mostarda" : "Vermelho";
                    var botao = Botao("Botao_" + cores[i], caixa.transform, "", cor, pos, new Vector2(126, 126), 0, null, circulo);
                    var b = botao.gameObject.AddComponent<BotaoDeSinal>();
                    b.sinaleiro = sinaleiro;
                    b.grupo = grupo;
                    b.cor = cores[i];
                    b.aro = aro.gameObject;
                    UnityEventTools.AddPersistentListener(botao.onClick, b.Tocar);
                    botoes.Add(b);
                }
                return caixa.gameObject;
            }
            var tres = new[] { CorDoSinal.Verde, CorDoSinal.Amarelo, CorDoSinal.Vermelho };
            var dois = new[] { CorDoSinal.Verde, CorDoSinal.Vermelho };
            hud.gruposDoPainel = new[]
            {
                Grupo(0, "Via azul", "Petroleo_Claro", -430, 480, tres),
                Grupo(1, "Via laranja", "Laranja", 70, 480, tres),
                Grupo(2, "Pedestres", "Cinza_Claro", 500, 340, dois),
            };
            hud.botoesDeSinal = botoes.ToArray();

            // Balão de explicação (no meio da tela).
            var balao = Imagem("Balao", raiz, arredondado, C("Grafite"), new Vector2(0, 90), new Vector2(900, 300));
            Imagem("Papel", balao.transform, arredondado, C("Mostarda_Clara"), Vector2.zero, new Vector2(886, 286));
            hud.textoDoBalao = Texto("Texto", balao.transform, "", 42, C("Grafite"), Vector2.zero, new Vector2(840, 262), FontStyle.Normal);
            hud.textoDoBalao.resizeTextForBestFit = true; hud.textoDoBalao.resizeTextMinSize = 26; hud.textoDoBalao.resizeTextMaxSize = 42;
            hud.balao = balao.gameObject;
            balao.gameObject.SetActive(false);

            hud.corGanha = Color.white;
            hud.corPerdida = new Color(.4f, .45f, .5f, .3f);
            return hud;
        }

        // Duas abas no alto da seleção de níveis: "Motorista Mirim" e "Sinaleiro".
        static Button[] Abas(Transform canvas, SinaisHUD hud)
        {
            var raiz = Tela("Abas_Dos_Modos", canvas);
            string[] nomes = { "Motorista Mirim", "Sinaleiro" };
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
    }
}
