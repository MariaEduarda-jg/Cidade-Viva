using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Atalhos pra montar prefabs com primitivas. Medidas em metros; "frente" do objeto é +Z.
    // Cilindro e cápsula recebem (diâmetro X, altura total, diâmetro Z).
    internal static class F
    {
        static readonly Dictionary<PrimitiveType, Mesh> primitivas = new Dictionary<PrimitiveType, Mesh>();

        public static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

        static Mesh Prim(PrimitiveType tipo)
        {
            if (primitivas.TryGetValue(tipo, out var m) && m != null) return m;
            var go = GameObject.CreatePrimitive(tipo);
            m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            primitivas[tipo] = m;
            return m;
        }

        public static GameObject Grupo(string nome, Transform pai = null, Vector3 pos = default, Vector3 rot = default)
        {
            var go = new GameObject(nome);
            if (pai != null) go.transform.SetParent(pai, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = rot;
            return go;
        }

        public static GameObject Forma(Transform pai, string nome, Mesh malha, string mat, Vector3 pos, Vector3 esc, Vector3 rot = default)
        {
            var go = Grupo(nome, pai, pos, rot);
            go.transform.localScale = esc;
            go.AddComponent<MeshFilter>().sharedMesh = malha;
            go.AddComponent<MeshRenderer>().sharedMaterial = ArtePaleta.Mat(mat);
            return go;
        }

        public static GameObject Cubo(Transform pai, string nome, string mat, Vector3 pos, Vector3 esc, Vector3 rot = default)
        {
            return Forma(pai, nome, Prim(PrimitiveType.Cube), mat, pos, esc, rot);
        }

        public static GameObject Esfera(Transform pai, string nome, string mat, Vector3 pos, Vector3 esc, Vector3 rot = default)
        {
            return Forma(pai, nome, Prim(PrimitiveType.Sphere), mat, pos, esc, rot);
        }

        public static GameObject Cilindro(Transform pai, string nome, string mat, Vector3 pos, Vector3 esc, Vector3 rot = default)
        {
            return Forma(pai, nome, Prim(PrimitiveType.Cylinder), mat, pos, new Vector3(esc.x, esc.y / 2f, esc.z), rot);
        }

        public static GameObject Capsula(Transform pai, string nome, string mat, Vector3 pos, Vector3 esc, Vector3 rot = default)
        {
            return Forma(pai, nome, Prim(PrimitiveType.Capsule), mat, pos, new Vector3(esc.x, esc.y / 2f, esc.z), rot);
        }

        public static GameObject Plano(Transform pai, string nome, string mat, Vector3 pos, float lx, float lz)
        {
            return Forma(pai, nome, Prim(PrimitiveType.Plane), mat, pos, new Vector3(lx / 10f, 1f, lz / 10f));
        }

        public static GameObject M(Transform pai, string nome, string malha, string mat, Vector3 pos, Vector3 esc, Vector3 rot = default)
        {
            return Forma(pai, nome, ArteMalhas.Malha(malha), mat, pos, esc, rot);
        }

        // Cubo posicionado pela base: (x, y da base, z) e (largura, altura, profundidade).
        public static GameObject Caixa(Transform pai, string nome, string mat, float x, float yBase, float z, float lx, float ly, float lz)
        {
            return Cubo(pai, nome, mat, new Vector3(x, yBase + ly / 2f, z), new Vector3(lx, ly, lz));
        }

        public static GameObject Barra(Transform pai, string nome, string mat, Vector3 a, Vector3 b, float diametro)
        {
            var go = Cilindro(pai, nome, mat, (a + b) / 2f, new Vector3(diametro, (b - a).magnitude, diametro));
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            return go;
        }

        // Quadrado com textura, virado pra +Z (ou girado em Y).
        public static GameObject Face(Transform pai, string nome, string mat, Vector3 pos, float largura, float altura, float rotY = 0f)
        {
            var go = Forma(pai, nome, Prim(PrimitiveType.Quad), mat, pos, new Vector3(largura, altura, 1f), new Vector3(0, 180f + rotY, 0));
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public static void Salvar(GameObject raiz, string categoria)
        {
            string pasta = $"{ArteImportador.PastaPrefabs}/{categoria}";
            ArtePaleta.GarantirPasta(pasta);
            PrefabUtility.SaveAsPrefabAsset(raiz, $"{pasta}/{raiz.name}.prefab");
            Object.DestroyImmediate(raiz);
        }
    }

    // Tudo que os pacotes não trazem e dá pra resolver bem com primitivas: sinalização,
    // mobiliário urbano, lixeiras de reciclagem, bichinhos, água e móveis da enchente.
    public static class ArteFormas
    {
        public static void GerarPrefabs()
        {
            SemaforoDeCarros();
            SemaforoDePedestres();
            Placa("Placa_Pare", "Tex_placa_pare", 0);
            Placa("Placa_FaixaDePedestre", "Tex_placa_faixa_pedestre", 1);
            Placa("Placa_Escola", "Tex_placa_escola", 1);
            Placa("Placa_Ciclovia", "Tex_placa_ciclovia", 2);
            Buraco();
            GerarItensDoRunner();
            Bicicleta();
            Banco();
            LixeiraDeRua();

            LixeiraDeReciclagem("Lixeira_Papel", "Petroleo", "Tex_rotulo_papel");
            LixeiraDeReciclagem("Lixeira_Plastico", "Vermelho", "Tex_rotulo_plastico");
            LixeiraDeReciclagem("Lixeira_Vidro", "Verde_Escuro", "Tex_rotulo_vidro");
            LixeiraDeReciclagem("Lixeira_Organico", "Marrom", "Tex_rotulo_organico");
            Jornal();
            CascaDeBanana();

            Lago();
            Rio();
            Cachorro();
            Passaro();

            AguaDaEnchente();
            Cadeira();
            Mesa();
            Sofa();
            AssetDatabase.SaveAssets();
        }

        static void SemaforoDeCarros()
        {
            var r = F.Grupo("Semaforo_Carros").transform;
            F.Cilindro(r, "Base", "Cinza_Escuro", F.V(0, .08f, 0), F.V(.34f, .16f, .34f));
            F.Cilindro(r, "Poste", "Cinza_Escuro", F.V(0, 1.5f, 0), F.V(.14f, 3f, .14f));
            F.Cubo(r, "Caixa", "Grafite", F.V(0, 3.5f, 0), F.V(.44f, 1.2f, .32f));
            F.Cubo(r, "Topo", "Grafite", F.V(0, 4.12f, 0), F.V(.5f, .06f, .38f));
            string[] nomes = { "Luz_Vermelha", "Luz_Amarela", "Luz_Verde" };
            for (int i = 0; i < 3; i++)
            {
                float y = 3.86f - i * .36f;
                F.Esfera(r, nomes[i], nomes[i], F.V(0, y, .15f), F.V(.27f, .27f, .12f));
                F.Cubo(r, "Aba_" + (i + 1), "Grafite", F.V(0, y + .16f, .22f), F.V(.32f, .03f, .16f));
            }
            F.Salvar(r.gameObject, "Props");
        }

        static void SemaforoDePedestres()
        {
            var r = F.Grupo("Semaforo_Pedestres").transform;
            F.Cilindro(r, "Base", "Cinza_Escuro", F.V(0, .07f, 0), F.V(.3f, .14f, .3f));
            F.Cilindro(r, "Poste", "Cinza_Escuro", F.V(0, 1.2f, 0), F.V(.12f, 2.4f, .12f));
            F.Cubo(r, "Caixa", "Grafite", F.V(0, 2.75f, 0), F.V(.4f, .8f, .26f));
            F.Cubo(r, "Topo", "Grafite", F.V(0, 3.17f, 0), F.V(.46f, .05f, .32f));
            F.Face(r, "Luz_Pare", "Tex_semaforo_pedestre_pare", F.V(0, 2.94f, .135f), .32f, .32f);
            F.Face(r, "Luz_Siga", "Tex_semaforo_pedestre_siga", F.V(0, 2.57f, .135f), .32f, .32f);
            F.Salvar(r.gameObject, "Props");
        }

        // forma: 0 = octógono, 1 = losango, 2 = círculo
        static void Placa(string nome, string textura, int forma)
        {
            var r = F.Grupo(nome).transform;
            F.Cilindro(r, "Poste", "Cinza", F.V(0, 1.15f, 0), F.V(.08f, 2.3f, .08f));
            const float y = 2.45f, z = .07f;
            float lado;
            if (forma == 0)
            {
                lado = .8f;
                F.M(r, "Chapa", "Prisma8", "Cinza_Claro", F.V(0, y, z), F.V(lado, .04f, lado), F.V(90, 0, 0));
            }
            else if (forma == 1)
            {
                lado = .86f;
                F.Cubo(r, "Chapa", "Cinza_Claro", F.V(0, y, z), F.V(lado / 1.4142f, lado / 1.4142f, .04f), F.V(0, 0, 45));
            }
            else
            {
                lado = .8f;
                F.Cilindro(r, "Chapa", "Cinza_Claro", F.V(0, y, z), F.V(lado, .04f, lado), F.V(90, 0, 0));
            }
            F.Face(r, "Frente", textura, F.V(0, y, z + .024f), lado, lado);
            F.Salvar(r.gameObject, "Props");
        }

        static void Buraco()
        {
            var r = F.Grupo("Buraco").transform;
            F.Cilindro(r, "Borda", "Asfalto_Escuro", F.V(0, .02f, 0), F.V(1.4f, .04f, 1.15f));
            F.Cilindro(r, "Fundo", "Grafite", F.V(.04f, .03f, -.02f), F.V(1.05f, .045f, .82f));
            F.Cubo(r, "Rachadura_1", "Asfalto_Escuro", F.V(.82f, .015f, .2f), F.V(.5f, .03f, .06f), F.V(0, 25, 0));
            F.Cubo(r, "Rachadura_2", "Asfalto_Escuro", F.V(-.8f, .015f, -.25f), F.V(.45f, .03f, .05f), F.V(0, 20, 0));
            F.Cubo(r, "Rachadura_3", "Asfalto_Escuro", F.V(-.2f, .015f, .68f), F.V(.05f, .03f, .4f), F.V(0, 15, 0));
            F.Esfera(r, "Pedrinha_1", "Cinza", F.V(.5f, .05f, .45f), F.V(.14f, .1f, .12f));
            F.Esfera(r, "Pedrinha_2", "Cinza_Escuro", F.V(-.55f, .04f, .35f), F.V(.1f, .08f, .1f));
            F.Esfera(r, "Pedrinha_3", "Cinza", F.V(-.3f, .04f, -.5f), F.V(.12f, .08f, .09f));
            F.Salvar(r.gameObject, "Props");
        }

        // Moeda e escudo que flutuam na pista dos runners (centro a ~1 m do chão).
        public static void GerarItensDoRunner()
        {
            var m = F.Grupo("Moeda").transform;
            F.Cilindro(m, "Disco", "Mostarda", F.V(0, .95f, 0), F.V(.7f, .09f, .7f), F.V(90, 0, 0));
            F.Cilindro(m, "Miolo", "Mostarda_Clara", F.V(0, .95f, 0), F.V(.5f, .12f, .5f), F.V(90, 0, 0));
            F.Salvar(m.gameObject, "Props");

            var e = F.Grupo("Escudo").transform;
            F.Esfera(e, "Corpo", "Petroleo_Claro", F.V(0, 1f, 0), F.V(.95f, 1.05f, .28f));
            F.Esfera(e, "Miolo", "Vidro", F.V(0, 1.02f, 0), F.V(.66f, .74f, .36f));
            F.Cubo(e, "Cruz_V", "Branco", F.V(0, 1.02f, 0), F.V(.12f, .44f, .4f));
            F.Cubo(e, "Cruz_H", "Branco", F.V(0, 1.06f, 0), F.V(.4f, .12f, .4f));
            F.Salvar(e.gameObject, "Props");
        }

        static void Bicicleta()
        {
            var r = F.Grupo("Bicicleta").transform;
            const float raio = .33f;
            foreach (var (nome, z) in new[] { ("Roda_Traseira", -.52f), ("Roda_Dianteira", .52f) })
            {
                var roda = F.Grupo(nome, r, F.V(0, raio, z)).transform;
                F.Cilindro(roda, "Pneu", "Grafite", Vector3.zero, F.V(raio * 2, .05f, raio * 2), F.V(0, 0, 90));
                F.Cilindro(roda, "Aro", "Cinza_Claro", Vector3.zero, F.V(raio * 1.6f, .055f, raio * 1.6f), F.V(0, 0, 90));
                F.Cilindro(roda, "Cubo", "Cinza_Escuro", Vector3.zero, F.V(.1f, .09f, .1f), F.V(0, 0, 90));
            }
            Vector3 eixoT = F.V(0, raio, -.52f), eixoD = F.V(0, raio, .52f), pedal = F.V(0, .3f, -.06f);
            Vector3 selim = F.V(0, .8f, -.24f), guidao = F.V(0, .86f, .36f);
            var q = F.Grupo("Quadro", r).transform;
            F.Barra(q, "Tubo_1", "Petroleo", eixoT, pedal, .045f);
            F.Barra(q, "Tubo_2", "Petroleo", pedal, selim, .05f);
            F.Barra(q, "Tubo_3", "Petroleo", eixoT, selim, .04f);
            F.Barra(q, "Tubo_4", "Petroleo", pedal, guidao, .05f);
            F.Barra(q, "Tubo_5", "Petroleo", selim + F.V(0, -.06f, 0), guidao + F.V(0, -.04f, 0), .05f);
            F.Barra(q, "Garfo", "Petroleo", guidao, eixoD, .045f);
            F.Barra(q, "Haste", "Cinza_Escuro", guidao, guidao + F.V(0, .14f, -.03f), .04f);
            F.Barra(q, "Guidao", "Grafite", guidao + F.V(-.24f, .14f, -.03f), guidao + F.V(.24f, .14f, -.03f), .04f);
            F.Barra(q, "Canote", "Cinza_Escuro", selim, selim + F.V(0, .1f, -.02f), .035f);
            F.Esfera(r, "Selim", "Grafite", selim + F.V(0, .12f, -.03f), F.V(.14f, .06f, .26f));
            F.Cilindro(r, "Coroa", "Cinza_Escuro", pedal, F.V(.16f, .02f, .16f), F.V(0, 0, 90));
            F.Cubo(r, "Pedal_E", "Grafite", pedal + F.V(-.11f, .08f, .08f), F.V(.1f, .025f, .07f));
            F.Cubo(r, "Pedal_D", "Grafite", pedal + F.V(.11f, -.08f, -.08f), F.V(.1f, .025f, .07f));
            F.Salvar(r.gameObject, "Props");
        }

        static void Banco()
        {
            var r = F.Grupo("Banco").transform;
            for (int i = 0; i < 3; i++)
                F.Cubo(r, "Assento_" + (i + 1), "Marrom_Claro", F.V(0, .46f, -.15f + i * .15f), F.V(1.6f, .05f, .13f));
            F.Cubo(r, "Encosto_1", "Marrom_Claro", F.V(0, .7f, -.26f), F.V(1.6f, .13f, .04f), F.V(-10, 0, 0));
            F.Cubo(r, "Encosto_2", "Marrom_Claro", F.V(0, .88f, -.29f), F.V(1.6f, .13f, .04f), F.V(-10, 0, 0));
            foreach (float x in new[] { -.65f, .65f })
            {
                F.Cubo(r, "Pe", "Grafite", F.V(x, .22f, -.02f), F.V(.06f, .44f, .46f));
                F.Cubo(r, "Suporte", "Grafite", F.V(x, .72f, -.3f), F.V(.06f, .56f, .05f), F.V(-10, 0, 0));
            }
            F.Salvar(r.gameObject, "Props");
        }

        static void LixeiraDeRua()
        {
            var r = F.Grupo("Lixeira_De_Rua").transform;
            F.Cilindro(r, "Poste", "Cinza_Escuro", F.V(0, .5f, 0), F.V(.07f, 1f, .07f));
            F.Cubo(r, "Suporte", "Cinza_Escuro", F.V(0, .85f, .08f), F.V(.06f, .06f, .2f));
            F.M(r, "Cesto", "Tronco", "Petroleo_Claro", F.V(0, .76f, .3f), F.V(.44f, .56f, .44f));
            F.Cilindro(r, "Aro", "Cinza_Escuro", F.V(0, 1.045f, .3f), F.V(.48f, .04f, .48f));
            F.Cilindro(r, "Boca", "Grafite", F.V(0, 1.06f, .3f), F.V(.38f, .02f, .38f));
            F.Salvar(r.gameObject, "Props");
        }

        static void LixeiraDeReciclagem(string nome, string cor, string rotulo)
        {
            var r = F.Grupo(nome).transform;
            F.Cubo(r, "Corpo", cor, F.V(0, .5f, 0), F.V(.64f, .84f, .58f));
            F.Cubo(r, "Tampa", cor, F.V(0, .96f, 0), F.V(.7f, .08f, .64f));
            F.Cubo(r, "Puxador", "Grafite", F.V(0, .985f, .33f), F.V(.3f, .035f, .05f));
            F.Cubo(r, "Boca", "Grafite", F.V(0, .78f, .29f), F.V(.42f, .08f, .02f));
            F.Face(r, "Rotulo", rotulo, F.V(0, .52f, .293f), .56f, .14f);
            foreach (float x in new[] { -.25f, .25f })
            {
                F.Cilindro(r, "Roda", "Grafite", F.V(x, .08f, -.2f), F.V(.16f, .07f, .16f), F.V(0, 0, 90));
                F.Cubo(r, "Pe", "Grafite", F.V(x, .04f, .2f), F.V(.07f, .08f, .07f));
            }
            F.Salvar(r.gameObject, "Lixo_Reciclagem");
        }

        static void Jornal()
        {
            var r = F.Grupo("Jornal").transform;
            F.Cubo(r, "Folhas", "Branco", F.V(0, .014f, 0), F.V(.34f, .028f, .44f));
            F.Cubo(r, "Dobra", "Areia", F.V(0, .038f, -.11f), F.V(.34f, .02f, .22f));
            F.Cubo(r, "Manchete", "Grafite", F.V(0, .0495f, -.18f), F.V(.27f, .004f, .035f));
            for (int i = 0; i < 3; i++)
                F.Cubo(r, "Linha_" + (i + 1), "Cinza", F.V(-.06f, .0495f, -.13f + i * .035f), F.V(.15f, .004f, .012f));
            F.Cubo(r, "Foto", "Cinza_Claro", F.V(.09f, .0495f, -.095f), F.V(.09f, .004f, .085f));
            for (int i = 0; i < 4; i++)
                F.Cubo(r, "Texto_" + (i + 1), "Cinza", F.V(0, .0295f, .05f + i * .04f), F.V(.27f, .004f, .012f));
            F.Salvar(r.gameObject, "Lixo_Reciclagem");
        }

        static void CascaDeBanana()
        {
            var r = F.Grupo("Casca_De_Banana").transform;
            F.Esfera(r, "Miolo", "Mostarda", F.V(0, .065f, 0), F.V(.075f, .07f, .075f));
            F.Cilindro(r, "Cabo", "Marrom_Escuro", F.V(0, .115f, 0), F.V(.025f, .05f, .025f));
            for (int i = 0; i < 4; i++)
            {
                var g = F.Grupo("Tira_" + (i + 1), r, Vector3.zero, F.V(0, i * 90 + 20, 0)).transform;
                F.Capsula(g, "Fora", "Mostarda", F.V(.1f, .03f, 0), F.V(.07f, .22f, .028f), F.V(0, 0, -72));
                F.Capsula(g, "Dentro", "Mostarda_Clara", F.V(.1f, .04f, 0), F.V(.05f, .19f, .022f), F.V(0, 0, -72));
            }
            F.Salvar(r.gameObject, "Lixo_Reciclagem");
        }

        static void Lago()
        {
            var r = F.Grupo("Lago").transform;
            F.Cilindro(r, "Margem", "Areia", F.V(0, .03f, 0), F.V(11f, .06f, 8f));
            F.Cilindro(r, "Fundo", "Petroleo_Claro", F.V(0, .04f, 0), F.V(10f, .06f, 7f));
            F.Cilindro(r, "Agua", "Agua", F.V(0, .08f, 0), F.V(10f, .02f, 7f));
            F.Salvar(r.gameObject, "Natureza");
        }

        static void Rio()
        {
            var r = F.Grupo("Rio_Trecho").transform;
            F.Caixa(r, "Margem", "Areia", 0, 0, 0, 6f, .06f, 8f);
            F.Caixa(r, "Fundo", "Petroleo_Claro", 0, 0, 0, 4.6f, .07f, 8f);
            F.Caixa(r, "Agua", "Agua", 0, .07f, 0, 4.6f, .02f, 8f);
            F.Salvar(r.gameObject, "Natureza");
        }

        static void Cachorro()
        {
            var r = F.Grupo("Cachorro").transform;
            F.Capsula(r, "Corpo", "Marrom_Claro", F.V(0, .36f, 0), F.V(.27f, .64f, .29f), F.V(90, 0, 0));
            F.Esfera(r, "Barriga", "Areia", F.V(0, .31f, .05f), F.V(.22f, .2f, .4f));
            var c = F.Grupo("Cabeca", r, F.V(0, .56f, .3f)).transform;
            F.Esfera(c, "Cranio", "Marrom_Claro", Vector3.zero, F.V(.27f, .26f, .27f));
            F.Esfera(c, "Focinho", "Areia", F.V(0, -.05f, .12f), F.V(.15f, .12f, .17f));
            F.Esfera(c, "Nariz", "Grafite", F.V(0, -.02f, .2f), F.V(.055f, .045f, .045f));
            foreach (float x in new[] { -.07f, .07f })
            {
                F.Esfera(c, "Olho", "Grafite", F.V(x, .04f, .115f), F.V(.04f, .045f, .03f));
                F.Esfera(c, "Orelha", "Marrom", F.V(x * 1.95f, .03f, -.03f), F.V(.07f, .18f, .11f), F.V(0, 0, x > 0 ? 18 : -18));
            }
            F.Cilindro(r, "Coleira", "Vermelho", F.V(0, .47f, .21f), F.V(.2f, .035f, .2f), F.V(55, 0, 0));
            foreach (float x in new[] { -.08f, .08f })
                foreach (float z in new[] { -.2f, .2f })
                {
                    F.Cilindro(r, "Perna", "Marrom_Claro", F.V(x, .14f, z), F.V(.08f, .28f, .08f));
                    F.Esfera(r, "Pata", "Areia", F.V(x, .03f, z + .02f), F.V(.09f, .06f, .12f));
                }
            F.Barra(r, "Rabo", "Marrom", F.V(0, .42f, -.3f), F.V(0, .64f, -.42f), .05f);
            F.Salvar(r.gameObject, "Natureza");
        }

        static void Passaro()
        {
            var r = F.Grupo("Passaro").transform;
            F.Esfera(r, "Corpo", "Petroleo_Claro", F.V(0, .17f, 0), F.V(.16f, .15f, .22f));
            F.Esfera(r, "Peito", "Branco", F.V(0, .155f, .035f), F.V(.13f, .11f, .16f));
            F.Esfera(r, "Cabeca", "Petroleo_Claro", F.V(0, .28f, .08f), F.V(.12f, .12f, .12f));
            F.M(r, "Bico", "Cone", "Mostarda", F.V(0, .275f, .165f), F.V(.04f, .07f, .04f), F.V(90, 0, 0));
            foreach (float x in new[] { -1f, 1f })
            {
                F.Esfera(r, "Olho", "Grafite", F.V(x * .042f, .3f, .125f), F.V(.022f, .024f, .018f));
                F.Esfera(r, "Asa", "Petroleo", F.V(x * .08f, .18f, -.01f), F.V(.03f, .1f, .16f), F.V(-15, 0, 0));
                F.Barra(r, "Perna", "Mostarda", F.V(x * .03f, .1f, 0), F.V(x * .03f, .005f, 0), .012f);
                F.Cubo(r, "Pe", "Mostarda", F.V(x * .03f, .005f, .015f), F.V(.03f, .01f, .05f));
            }
            F.Cubo(r, "Cauda", "Petroleo", F.V(0, .19f, -.15f), F.V(.07f, .015f, .13f), F.V(-25, 0, 0));
            F.Salvar(r.gameObject, "Natureza");
        }

        static void AguaDaEnchente()
        {
            var r = F.Grupo("Agua_Enchente").transform;
            var agua = F.Plano(r, "Agua", "Agua", Vector3.zero, 20f, 20f);
            agua.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            F.Salvar(r.gameObject, "Enchente");
        }

        static void Cadeira()
        {
            var r = F.Grupo("Cadeira").transform;
            F.Cubo(r, "Assento", "Marrom_Claro", F.V(0, .45f, 0), F.V(.45f, .05f, .45f));
            F.Cubo(r, "Encosto", "Marrom_Claro", F.V(0, .78f, -.2f), F.V(.45f, .3f, .05f));
            foreach (float x in new[] { -.19f, .19f })
            {
                F.Cubo(r, "Perna_Frente", "Marrom", F.V(x, .215f, .19f), F.V(.05f, .43f, .05f));
                F.Cubo(r, "Perna_Tras", "Marrom", F.V(x, .465f, -.2f), F.V(.05f, .93f, .05f));
            }
            F.Salvar(r.gameObject, "Enchente");
        }

        static void Mesa()
        {
            var r = F.Grupo("Mesa").transform;
            F.Cubo(r, "Tampo", "Marrom_Claro", F.V(0, .75f, 0), F.V(1.2f, .06f, .8f));
            foreach (float x in new[] { -.52f, .52f })
                foreach (float z in new[] { -.32f, .32f })
                    F.Cubo(r, "Perna", "Marrom", F.V(x, .36f, z), F.V(.07f, .72f, .07f));
            F.Salvar(r.gameObject, "Enchente");
        }

        static void Sofa()
        {
            var r = F.Grupo("Sofa").transform;
            F.Caixa(r, "Base", "Petroleo_Claro", 0, .06f, 0, 1.8f, .36f, .85f);
            F.Caixa(r, "Encosto", "Petroleo_Claro", 0, .3f, -.33f, 1.8f, .62f, .2f);
            foreach (float x in new[] { -.92f, .92f })
            {
                F.Caixa(r, "Braco", "Petroleo", x, .06f, 0, .2f, .58f, .87f);
                F.Caixa(r, "Almofada", "Mostarda_Clara", x * .43f, .42f, .08f, .76f, .13f, .62f);
                F.Caixa(r, "Pe_Frente", "Grafite", x * .9f, 0, .34f, .08f, .06f, .08f);
                F.Caixa(r, "Pe_Tras", "Grafite", x * .9f, 0, -.34f, .08f, .06f, .08f);
            }
            F.Salvar(r.gameObject, "Enchente");
        }
    }
}
