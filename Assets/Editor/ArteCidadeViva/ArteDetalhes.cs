using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Detalhes de cenário feitos com formas da paleta e prefabs que já existem no projeto (2026-11-19),
    // usados por mais de um construtor: boca de túnel em arco, morro com árvores e pedras, margem de rio.
    public static class ArteDetalhes
    {
        static GameObject Prefab(Transform pai, string prefab, Vector3 pos, float rotY, float escala)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ArteImportador.PastaPrefabs}/{prefab}.prefab");
            if (asset == null) { Debug.LogError("[Detalhes] Prefab não encontrado: " + prefab); return new GameObject("FALTOU_" + prefab); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, pai);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(0, rotY, 0);
            go.transform.localScale = Vector3.one * escala;
            return go;
        }

        static void SemSombra(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Fachada de túnel com boca em arco. Origem do grupo = meio da fachada, no chão; a fachada olha pra -Z local
        // (girar o grupo com rotY pra virar). largura/altura = da boca; profundidade = espessura do bloco do portal,
        // que vai de z = 0 (fachada) até z = +profundidade.
        public static Transform BocaDeTunel(Transform pai, string nome, Vector3 pos, float rotY, float largura, float altura, float profundidade)
        {
            var g = F.Grupo(nome, pai, pos, new Vector3(0, rotY, 0)).transform;
            float r = largura / 2f;
            float reto = Mathf.Max(.4f, altura - r);
            float W = largura + 2.6f, H = altura + 1.3f;

            // Bloco do portal, com o vão do arco escuro na frente.
            F.Cubo(g, "Portal", "Branco_Concreto", F.V(0, H / 2f, profundidade / 2f), F.V(W, H, profundidade));
            F.Cubo(g, "Vao", "Grafite", F.V(0, reto / 2f, -.03f), F.V(largura, reto, .1f));
            F.Cilindro(g, "Vao_Arco", "Grafite", F.V(0, reto, -.03f), F.V(largura, .05f, largura), F.V(90, 0, 0));
            // Fundo ainda mais escuro, pra boca parecer funda.
            F.Cubo(g, "Fundo", "Asfalto_Escuro", F.V(0, reto * .45f, -.06f), F.V(largura * .55f, reto * .9f, .05f));
            F.Cilindro(g, "Fundo_Arco", "Asfalto_Escuro", F.V(0, reto * .9f, -.06f), F.V(largura * .55f, .03f, largura * .55f), F.V(90, 0, 0));
            // Luzinhas lá dentro.
            for (int i = 0; i < 3; i++)
                F.Cubo(g, "Luz_De_Dentro", "Luz_Amarela", F.V(0, reto + r * (.75f - i * .18f), -.09f), F.V(.5f - i * .12f, .07f, .02f));

            // Aduelas: blocos em volta do arco, alternando as cores, e os dois pés retos.
            const int n = 11;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI * (i + .5f) / n;
                var p = new Vector3(Mathf.Cos(a) * (r + .32f), reto + Mathf.Sin(a) * (r + .32f), -.12f);
                F.Cubo(g, "Aduela", i % 2 == 0 ? "Mostarda_Clara" : "Cinza_Claro", p, F.V(.55f, Mathf.PI * (r + .32f) / n * .92f, .22f), F.V(0, 0, a * Mathf.Rad2Deg));
            }
            foreach (float s in new[] { -1f, 1f })
                for (int j = 0; j < 2; j++)
                    F.Cubo(g, "Pe_Do_Arco", j % 2 == 0 ? "Cinza_Claro" : "Mostarda_Clara", F.V(s * (r + .32f), reto * (j + .5f) / 2f, -.12f), F.V(.55f, reto / 2f * .94f, .22f));

            // Cornija, faixa de altura (zebrada) e a grama em cima.
            F.Cubo(g, "Cornija", "Cinza", F.V(0, H + .1f, profundidade / 2f - .1f), F.V(W + .4f, .2f, profundidade + .4f));
            int listras = Mathf.Max(6, Mathf.RoundToInt(largura / .5f));
            for (int i = 0; i < listras; i++)
                F.Cubo(g, "Zebrado", i % 2 == 0 ? "Mostarda" : "Grafite", F.V(-largura / 2f + (i + .5f) * largura / listras, altura + .5f, -.13f), F.V(largura / listras, .3f, .06f));
            F.Cubo(g, "Grama_Em_Cima", "Verde_Claro", F.V(0, H + .3f, profundidade / 2f), F.V(W + .2f, .2f, profundidade + .2f));
            SemSombra(Prefab(g, "Natureza/Arbusto_1", F.V(-W / 2f + .7f, H + .35f, profundidade / 2f), 20, .7f));
            SemSombra(Prefab(g, "Natureza/Arbusto_2", F.V(W / 2f - .8f, H + .35f, profundidade / 2f), 140, .7f));

            // Luminárias dos dois lados da boca.
            foreach (float s in new[] { -1f, 1f })
            {
                F.Cubo(g, "Suporte", "Cinza_Escuro", F.V(s * (r + 1f), altura + .1f, -.25f), F.V(.08f, .08f, .4f));
                F.Cubo(g, "Lampada", "Luz_Amarela", F.V(s * (r + 1f), altura - .05f, -.42f), F.V(.3f, .16f, .18f));
            }

            // Muros de ala, descendo em degraus dos dois lados.
            foreach (float s in new[] { -1f, 1f })
                for (int j = 0; j < 3; j++)
                {
                    float hh = H * (.75f - j * .22f);
                    var m = F.Cubo(g, "Muro_De_Ala", "Branco_Concreto", Vector3.zero, F.V(1.3f, hh, .5f));
                    m.transform.localPosition = F.V(s * (W / 2f + .55f + j * 1.05f), hh / 2f, .25f + j * .55f);
                    m.transform.localEulerAngles = F.V(0, -s * 28f, 0);
                }
            return g;
        }

        // Árvores, arbustos e pedras na superfície de um morro feito com uma esfera achatada (centro no chão,
        // diâmetros "tamanho"). Só na metade da frente (-Z) e longe do retângulo livre (ex.: a boca do túnel).
        public static void EnfeitarMorro(Transform pai, string nome, Vector3 centro, Vector3 tamanho, int quantos, int semente, Rect livre)
        {
            var g = F.Grupo(nome, pai).transform;
            var rnd = new System.Random(semente);
            string[] arvores = { "Natureza/Arvore_Pinheiro", "Natureza/Arvore_Comum_2", "Natureza/Arvore_Simples_Grande", "Natureza/Arvore_LowPoly", "Natureza/Arvore_Pinheiro", "Natureza/Arvore_Betula" };
            string[] baixos = { "Natureza/Arbusto_1", "Natureza/Arbusto_2", "Natureza/Pedra_Musgo", "Natureza/Pedra_2", "Natureza/Arbusto_Frutinhas", "Natureza/Flores" };
            int feitos = 0, tentativas = 0;
            while (feitos < quantos && tentativas++ < quantos * 20)
            {
                float u = (float)rnd.NextDouble() * 2f - 1f;          // -1..1 em X
                float v = -(float)rnd.NextDouble() * .95f;            // só a frente
                if (u * u + v * v > .85f) continue;
                float x = centro.x + u * tamanho.x / 2f, z = centro.z + v * tamanho.z / 2f;
                if (livre.Contains(new Vector2(x, z))) continue;
                float y = centro.y + tamanho.y / 2f * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u - v * v)) - .25f;
                bool alto = rnd.NextDouble() < .55;
                string qual = alto ? arvores[rnd.Next(arvores.Length)] : baixos[rnd.Next(baixos.Length)];
                float esc = alto ? .7f + (float)rnd.NextDouble() * .5f : 1f + (float)rnd.NextDouble() * .8f;
                Prefab(g, qual, new Vector3(x, y, z), (float)rnd.NextDouble() * 360f, esc * Mathf.Clamp(tamanho.y / 20f, .45f, 1.3f));
                feitos++;
            }
        }

        // Margem de rio que corre em X (de x0 a x1, margem na linha z): capim alto, plantas, pedras e,
        // na água (lado aguaSinal), vitórias-régias (traços brancos de reflexo pareciam faixa de rua: tirados). Pula o trecho livre (ex.: onde passa a rua/ponte).
        public static void MargemDeRio(Transform pai, string nome, float x0, float x1, float z, float aguaSinal, float y, int semente, float livreDe, float livreAte)
        {
            var g = F.Grupo(nome, pai).transform;
            var rnd = new System.Random(semente);
            string[] margem = { "Natureza/Grama_Alta", "Natureza/Grama_Alta_2", "Natureza/Planta_1", "Natureza/Grama_Alta", "Natureza/Pedra_1", "Natureza/Planta_2", "Natureza/Pedra_3" };
            for (float x = x0; x <= x1; x += 2.2f + (float)rnd.NextDouble() * 2.5f)
            {
                if (x > livreDe && x < livreAte) continue;
                string qual = margem[rnd.Next(margem.Length)];
                float dz = -aguaSinal * (float)rnd.NextDouble() * .8f;
                var go = Prefab(g, qual, new Vector3(x, y, z + dz), (float)rnd.NextDouble() * 360f, qual.Contains("Pedra") ? .9f + (float)rnd.NextDouble() * .6f : 1.1f + (float)rnd.NextDouble() * .6f);
                SemSombra(go);
                if (rnd.NextDouble() < .3)
                    SemSombra(Prefab(g, "Natureza/Vitoria_Regia", new Vector3(x + 1f, y - .02f, z + aguaSinal * (1.2f + (float)rnd.NextDouble() * 1.5f)), (float)rnd.NextDouble() * 360f, 1f + (float)rnd.NextDouble() * .4f));
            }
        }
    }
}
