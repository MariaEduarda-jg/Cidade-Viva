using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Formas que o Unity não tem como primitiva (prisma, cone, cúpula, cuia, arco).
    // Ficam salvas como assets em Assets/Art/Malhas e são usadas pelos prefabs feitos de primitivas.
    public static class ArteMalhas
    {
        public const string Pasta = "Assets/Art/Malhas";

        public static Mesh Malha(string nome)
        {
            var m = AssetDatabase.LoadAssetAtPath<Mesh>($"{Pasta}/{nome}.asset");
            if (m == null) throw new System.ArgumentException("Malha não existe: " + nome);
            return m;
        }

        public static void GerarMalhas()
        {
            ArtePaleta.GarantirPasta(Pasta);

            // Perfis (raio, y) de baixo pra cima por fora; cada lista é um trecho liso, a troca de lista é uma quina.
            Revolucao("Prisma8", 8, 22.5f, true,
                P(0, -.5f, .5f, -.5f), P(.5f, -.5f, .5f, .5f), P(.5f, .5f, 0, .5f));
            Revolucao("Cone", 24, 0, false,
                P(0, -.5f, .5f, -.5f), P(.5f, -.5f, 0, .5f));
            Revolucao("Tronco", 24, 0, false,
                P(0, -.5f, .38f, -.5f), P(.38f, -.5f, .5f, .5f), P(.5f, .5f, 0, .5f));
            Revolucao("Saia", 24, 0, false,
                P(0, -.5f, .5f, -.5f), P(.5f, -.5f, .28f, .5f), P(.28f, .5f, 0, .5f));
            Revolucao("Funil", 32, 0, false,
                P(0, -.5f, .5f, -.5f), P(.5f, -.5f, .2f, .5f), P(.2f, .5f, 0, .5f));

            var cupula = new List<Vector2>();
            for (int i = 0; i <= 10; i++)
            {
                float a = i / 10f * Mathf.PI / 2f;
                cupula.Add(new Vector2(.5f * Mathf.Cos(a), .5f * Mathf.Sin(a)));
            }
            Revolucao("Semiesfera", 32, 0, false, P(0, 0, .5f, 0), cupula);

            var fora = new List<Vector2>();
            var dentro = new List<Vector2>();
            for (int i = 0; i <= 10; i++)
            {
                float a = i / 10f * Mathf.PI / 2f;
                fora.Add(new Vector2(.5f * Mathf.Sin(a), .5f - .5f * Mathf.Cos(a)));
                dentro.Add(new Vector2(.46f * Mathf.Cos(a), .5f - .46f * Mathf.Sin(a)));
            }
            Revolucao("Cuia", 32, 0, false, fora, P(.5f, .5f, .46f, .5f), dentro);

            Arco("Arco_Palacio", 2.3f, 6.5f, .35f, .7f);
            Arco("Arco_Anel", 13f, 13f, 1.6f, .35f);
            Arco("Arco_Ponte", 11f, 10f, 1f, 1.3f);
            AssetDatabase.SaveAssets();
        }

        static List<Vector2> P(float r0, float y0, float r1, float y1)
        {
            return new List<Vector2> { new Vector2(r0, y0), new Vector2(r1, y1) };
        }

        static void Revolucao(string nome, int lados, float grausIniciais, bool facetado, params List<Vector2>[] trechos)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            foreach (var perfil in trechos)
            {
                if (facetado)
                {
                    for (int i = 0; i < lados; i++)
                        for (int j = 0; j < perfil.Count - 1; j++)
                        {
                            int b = v.Count;
                            v.Add(Ponto(perfil[j], i, lados, grausIniciais));
                            v.Add(Ponto(perfil[j + 1], i, lados, grausIniciais));
                            v.Add(Ponto(perfil[j + 1], i + 1, lados, grausIniciais));
                            v.Add(Ponto(perfil[j], i + 1, lados, grausIniciais));
                            t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                        }
                    continue;
                }

                int baseV = v.Count;
                int n = perfil.Count;
                for (int i = 0; i < lados; i++)
                    for (int j = 0; j < n; j++)
                        v.Add(Ponto(perfil[j], i, lados, grausIniciais));
                for (int i = 0; i < lados; i++)
                {
                    int i2 = (i + 1) % lados;
                    for (int j = 0; j < n - 1; j++)
                    {
                        int a = baseV + i * n + j, b = baseV + i * n + j + 1, c = baseV + i2 * n + j + 1, d = baseV + i2 * n + j;
                        t.AddRange(new[] { a, b, c, a, c, d });
                    }
                }
            }
            Salvar(nome, v, t);
        }

        static Vector3 Ponto(Vector2 p, int i, int lados, float grausIniciais)
        {
            float a = (grausIniciais + 360f * i / lados) * Mathf.Deg2Rad;
            return new Vector3(p.x * Mathf.Cos(a), p.y, p.x * Mathf.Sin(a));
        }

        // Meio anel elíptico em pé no plano XY (base em y=0), com espessura e profundidade em Z, em medidas reais.
        static void Arco(string nome, float rx, float ry, float espessura, float profundidade)
        {
            const int seg = 32;
            float z = profundidade / 2f;
            var foraF = new List<Vector3>(); var foraT = new List<Vector3>();
            var dentroF = new List<Vector3>(); var dentroT = new List<Vector3>();
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.PI * i / seg;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                foraF.Add(new Vector3(rx * c, ry * s, z)); foraT.Add(new Vector3(rx * c, ry * s, -z));
                dentroF.Add(new Vector3((rx - espessura) * c, (ry - espessura) * s, z));
                dentroT.Add(new Vector3((rx - espessura) * c, (ry - espessura) * s, -z));
            }
            var v = new List<Vector3>();
            var t = new List<int>();
            Faixa(v, t, foraF, foraT, new Vector3(1, 0, 0));
            Faixa(v, t, dentroF, dentroT, new Vector3(-1, 0, 0));
            Faixa(v, t, foraF, dentroF, new Vector3(0, 0, 1));
            Faixa(v, t, foraT, dentroT, new Vector3(0, 0, -1));
            Faixa(v, t, new List<Vector3> { foraF[0], foraT[0] }, new List<Vector3> { dentroF[0], dentroT[0] }, Vector3.down);
            Faixa(v, t, new List<Vector3> { foraF[seg], foraT[seg] }, new List<Vector3> { dentroF[seg], dentroT[seg] }, Vector3.down);
            Salvar(nome, v, t);
        }

        // Liga duas fileiras de pontos com vértices compartilhados (liso ao longo da faixa).
        // "normalDoInicio" diz pra que lado a primeira face deve olhar.
        static void Faixa(List<Vector3> v, List<int> t, List<Vector3> a, List<Vector3> b, Vector3 normalDoInicio)
        {
            int b0 = v.Count;
            for (int i = 0; i < a.Count; i++) { v.Add(a[i]); v.Add(b[i]); }
            bool inverter = Vector3.Dot(Vector3.Cross(b[0] - a[0], b[1] - a[0]), normalDoInicio) < 0;
            for (int i = 0; i < a.Count - 1; i++)
            {
                int p = b0 + i * 2, q = p + 1, r = p + 3, s = p + 2;
                if (inverter) t.AddRange(new[] { p, r, q, p, s, r });
                else t.AddRange(new[] { p, q, r, p, r, s });
            }
        }

        static void Salvar(string nome, List<Vector3> v, List<int> t)
        {
            string caminho = $"{Pasta}/{nome}.asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(caminho);
            bool novo = m == null;
            if (novo) m = new Mesh();
            m.Clear();
            m.name = nome;
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.SetUVs(0, new Vector2[v.Count]);
            m.RecalculateNormals();
            m.RecalculateBounds();
            if (novo) AssetDatabase.CreateAsset(m, caminho);
            else EditorUtility.SetDirty(m);
        }
    }
}
