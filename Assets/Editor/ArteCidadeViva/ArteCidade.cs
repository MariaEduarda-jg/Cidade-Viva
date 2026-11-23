using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Prédios que não existem nos pacotes: escola, padaria, comércio e a Brasília modernista
    // (curvas brancas, pilotis, lajes soltas). A frente de cada prédio é +Z.
    public static class ArteCidade
    {
        public static void GerarPrefabs()
        {
            Escola();
            Padaria();
            Loja();
            TorreComercial();
            BlocoDaSuperquadra();
            Catedral();
            Congresso();
            PalacioDosArcos();
            MuseuDaCupula();
            PonteDosArcos();
            AssetDatabase.SaveAssets();
        }

        static void Janela(Transform pai, float x, float yBase, float zFrente, float largura, float altura)
        {
            F.Caixa(pai, "Moldura", "Branco", x, yBase - .08f, zFrente, largura + .16f, altura + .16f, .08f);
            F.Caixa(pai, "Janela", "Vidro", x, yBase, zFrente, largura, altura, .14f);
        }

        static void Escola()
        {
            var r = F.Grupo("Escola").transform;
            F.Caixa(r, "Base", "Cinza", 0, 0, 0, 14.4f, .3f, 7.4f);
            F.Caixa(r, "Corpo", "Mostarda_Clara", 0, .3f, 0, 14f, 4f, 7f);
            F.Caixa(r, "Cobertura", "Branco", 0, 4.3f, 0, 14.8f, .35f, 7.8f);
            F.Caixa(r, "Faixa", "Petroleo", 0, 3.4f, 3.5f, 14f, .75f, .1f);
            F.Face(r, "Letreiro", "Tex_letreiro_escola", F.V(0, 3.775f, 3.56f), 2.6f, .65f);
            F.Caixa(r, "Porta", "Petroleo_Escuro", 0, .3f, 3.5f, 1.8f, 2.4f, .12f);
            F.Caixa(r, "Porta_Vidro_E", "Vidro", -.42f, 1.5f, 3.5f, .5f, .9f, .16f);
            F.Caixa(r, "Porta_Vidro_D", "Vidro", .42f, 1.5f, 3.5f, .5f, .9f, .16f);
            F.Caixa(r, "Degrau", "Cinza_Claro", 0, 0, 4.5f, 3.6f, .3f, 2.2f);
            F.Caixa(r, "Marquise", "Branco", 0, 2.9f, 4.45f, 3.6f, .18f, 2f);
            foreach (float x in new[] { -1.55f, 1.55f })
                F.Cilindro(r, "Pilar", "Branco", F.V(x, 1.6f, 5.2f), F.V(.16f, 2.6f, .16f));
            foreach (float x in new[] { 2.5f, 4.2f, 5.9f })
            {
                Janela(r, x, 1.5f, 3.5f, 1.3f, 1.3f);
                Janela(r, -x, 1.5f, 3.5f, 1.3f, 1.3f);
            }
            F.Cilindro(r, "Mastro", "Cinza", F.V(6.2f, 2.9f, 5.2f), F.V(.08f, 5.8f, .08f));
            F.Cubo(r, "Bandeira", "Verde", F.V(6.7f, 5.3f, 5.2f), F.V(.9f, .6f, .03f));
            F.Cubo(r, "Bandeira_Losango", "Mostarda", F.V(6.7f, 5.3f, 5.2f), F.V(.34f, .34f, .04f), F.V(0, 0, 45));
            F.Salvar(r.gameObject, "Predios");
        }

        static void Padaria()
        {
            var r = F.Grupo("Padaria").transform;
            F.Caixa(r, "Corpo", "Areia", 0, 0, 0, 7f, 3.7f, 6f);
            F.Caixa(r, "Platibanda", "Marrom_Claro", 0, 3.7f, 0, 7.2f, .35f, 6.2f);
            F.Caixa(r, "Rodape", "Marrom", 0, 0, 3f, 7f, .5f, .08f);
            F.Caixa(r, "Vitrine_Moldura", "Branco", -1.3f, .62f, 3f, 3.56f, 1.66f, .08f);
            F.Caixa(r, "Vitrine", "Vidro", -1.3f, .7f, 3f, 3.4f, 1.5f, .14f);
            F.Caixa(r, "Porta", "Marrom", 2.2f, 0, 3f, 1.2f, 2.2f, .12f);
            F.Caixa(r, "Porta_Vidro", "Vidro", 2.2f, 1.1f, 3f, .8f, .9f, .16f);
            F.Caixa(r, "Faixa", "Marrom", 0, 3.02f, 3f, 7f, .6f, .1f);
            F.Face(r, "Letreiro", "Tex_letreiro_padaria", F.V(0, 3.32f, 3.06f), 2.2f, .55f);
            for (int i = 0; i < 7; i++)
                F.Cubo(r, "Toldo_" + (i + 1), i % 2 == 0 ? "Terracota" : "Branco",
                    F.V(-3f + i, 2.6f, 3.6f), F.V(1f, .06f, 1.35f), F.V(22, 0, 0));
            // Pão no telhado: a "logo" da padaria, pra criança reconhecer de longe.
            F.Capsula(r, "Pao", "Marrom_Claro", F.V(0, 4.5f, 0), F.V(.95f, 2.5f, .95f), F.V(0, 0, 90));
            foreach (float x in new[] { -.6f, 0f, .6f })
                F.Cubo(r, "Pao_Corte", "Areia", F.V(x, 4.93f, 0), F.V(.12f, .12f, .7f), F.V(0, 25, 0));
            F.Salvar(r.gameObject, "Predios");
        }

        static void Loja()
        {
            var r = F.Grupo("Predio_Comercial_Loja").transform;
            F.Caixa(r, "Corpo", "Petroleo_Claro", 0, 0, 0, 8f, 7f, 7f);
            F.Caixa(r, "Platibanda", "Branco", 0, 7f, 0, 8.3f, .3f, 7.3f);
            F.Caixa(r, "Rodape", "Petroleo", 0, 0, 3.5f, 8f, .5f, .08f);
            F.Caixa(r, "Porta", "Petroleo_Escuro", 0, 0, 3.5f, 1.3f, 2.5f, .12f);
            foreach (float x in new[] { -2.4f, 2.4f })
            {
                F.Caixa(r, "Vitrine_Moldura", "Branco", x, .52f, 3.5f, 2.96f, 2.16f, .08f);
                F.Caixa(r, "Vitrine", "Vidro", x, .6f, 3.5f, 2.8f, 2f, .14f);
            }
            F.Cubo(r, "Toldo", "Mostarda", F.V(0, 3.2f, 4.1f), F.V(8f, .08f, 1.4f), F.V(20, 0, 0));
            foreach (float x in new[] { -2.85f, -.95f, .95f, 2.85f })
                Janela(r, x, 4.4f, 3.5f, 1.2f, 1.5f);
            F.Salvar(r.gameObject, "Predios");
        }

        static void TorreComercial()
        {
            var r = F.Grupo("Predio_Comercial_Torre").transform;
            F.Caixa(r, "Terreo_Vidro", "Vidro", 0, 0, 0, 8f, 3.6f, 8f);
            F.Caixa(r, "Porta", "Petroleo_Escuro", 0, 0, 4f, 2f, 2.6f, .12f);
            foreach (float x in new[] { -4.1f, 4.1f })
                foreach (float z in new[] { -4.1f, 4.1f })
                    F.Cilindro(r, "Pilar", "Branco", F.V(x, 1.8f, z), F.V(.6f, 3.6f, .6f));
            F.Caixa(r, "Corpo", "Branco", 0, 3.6f, 0, 9f, 16f, 9f);
            for (int i = 0; i < 5; i++)
                F.Caixa(r, "Janelas_Andar_" + (i + 1), "Vidro", 0, 4.5f + i * 3.2f, 0, 9.1f, 1.5f, 9.1f);
            F.Caixa(r, "Brise", "Petroleo", -3.4f, 3.6f, 4.5f, 1.2f, 16f, .3f);
            F.Caixa(r, "Laje", "Cinza_Claro", 0, 19.6f, 0, 9.3f, .3f, 9.3f);
            F.Caixa(r, "Casa_De_Maquinas", "Cinza", 1.2f, 19.9f, -1f, 4f, 1.4f, 4f);
            F.Salvar(r.gameObject, "Predios");
        }

        static void BlocoDaSuperquadra()
        {
            var r = F.Grupo("Brasilia_Bloco_Superquadra").transform;
            for (int i = 0; i < 6; i++)
                foreach (float z in new[] { -3.5f, 3.5f })
                    F.Cilindro(r, "Pilotis", "Branco_Concreto", F.V(-13f + i * 5.2f, 1.4f, z), F.V(.65f, 2.8f, .65f));
            F.Caixa(r, "Saguao", "Vidro", 4f, 0, 0, 5f, 2.8f, 4f);
            F.Caixa(r, "Corpo", "Branco_Concreto", 0, 2.8f, 0, 30f, 12f, 10f);
            for (int i = 0; i < 4; i++)
            {
                float y = 3.7f + i * 3f;
                F.Caixa(r, "Janelas_Frente_" + (i + 1), "Vidro", -2.6f, y, 5f, 23.8f, 1.5f, .14f);
                F.Caixa(r, "Janelas_Fundo_" + (i + 1), "Vidro", 0, y, -5f, 29f, 1.5f, .14f);
            }
            F.Caixa(r, "Cobogo", "Mostarda", 12.2f, 3.2f, 5f, 4.8f, 11.2f, .16f);
            F.Caixa(r, "Caixa_De_Escada", "Petroleo", -9f, 0, -5.9f, 3.2f, 16.2f, 2.4f);
            F.Caixa(r, "Laje", "Branco", 0, 14.8f, 0, 30.6f, .3f, 10.6f);
            F.Salvar(r.gameObject, "Predios");
        }

        // Inspirada na Catedral: 16 costelas brancas curvas em volta de um miolo de vidro.
        public static void RefazerCatedral()
        {
            Catedral();
            AssetDatabase.SaveAssets();
        }

        static void Catedral()
        {
            var r = F.Grupo("Brasilia_Catedral").transform;
            F.Cilindro(r, "Base", "Cinza_Claro", F.V(0, .1f, 0), F.V(17f, .2f, 17f));
            F.M(r, "Vitral_Baixo", "Funil", "Vidro", F.V(0, 4.1f, 0), F.V(12.4f, 7.8f, 12.4f));
            F.M(r, "Vitral_Alto", "Tronco", "Vidro", F.V(0, 9.8f, 0), F.V(6.3f, 3.6f, 6.3f));

            // Perfil em hipérbole (cintura a 8,5 m), amostrado em trechos curtos pra curva sair lisa.
            const int trechos = 9;
            var perfil = new Vector2[trechos + 1];
            for (int i = 0; i <= trechos; i++)
            {
                float y = Mathf.Lerp(.2f, 12.2f, i / (float)trechos);
                perfil[i] = new Vector2(Mathf.Sqrt(6.76f + .654f * (y - 8.5f) * (y - 8.5f)), y);
            }
            var costelas = F.Grupo("Costelas", r).transform;
            for (int k = 0; k < 16; k++)
            {
                float a = k * Mathf.PI * 2f / 16f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var c = F.Grupo("Costela_" + (k + 1), costelas).transform;
                for (int i = 0; i < trechos; i++)
                {
                    Vector3 p = dir * perfil[i].x + Vector3.up * perfil[i].y;
                    Vector3 q = dir * perfil[i + 1].x + Vector3.up * perfil[i + 1].y;
                    float d = Mathf.Lerp(.62f, .34f, (i + 1) / (float)trechos);
                    // Sem esferas nas juntas (eram 110 mil triângulos): cada trecho passa um pouco do ponto e cobre a junta.
                    Vector3 dir2 = (q - p).normalized;
                    F.Barra(c, "Trecho_" + (i + 1), "Branco_Concreto", p - dir2 * d * .45f, q + dir2 * d * .45f, d);
                }
            }
            F.Barra(r, "Cruz_Haste", "Branco_Concreto", F.V(0, 11.6f, 0), F.V(0, 13.6f, 0), .12f);
            F.Barra(r, "Cruz_Braco", "Branco_Concreto", F.V(-.4f, 13.1f, 0), F.V(.4f, 13.1f, 0), .12f);
            // 150 pedaços viram uma malha por material (menos chamadas de desenho no WebGL).
            OtimizacaoWebGL.Combinar(r, "Catedral");
            F.Salvar(r.gameObject, "Predios");
        }

        // Inspirado no Congresso: plataforma, duas torres, a cúpula e a cuia.
        static void Congresso()
        {
            var r = F.Grupo("Brasilia_Congresso").transform;
            F.Caixa(r, "Espelho_Dagua", "Petroleo_Claro", -13f, 0, 14f, 16f, .06f, 9f);
            F.Caixa(r, "Plataforma", "Branco_Concreto", 0, 0, 0, 44f, 2.4f, 16f);
            F.Caixa(r, "Plataforma_Sombra", "Cinza_Claro", 0, 0, 0, 43.6f, 1.6f, 16.1f);
            foreach (float x in new[] { -2.3f, 2.3f })
            {
                F.Caixa(r, "Torre", "Branco_Concreto", x, 2.4f, -3f, 3.4f, 27f, 8f);
                F.Caixa(r, "Torre_Janelas", "Vidro", x, 3.4f, -3f, 3.5f, 25f, 7.2f);
            }
            F.Caixa(r, "Passarela", "Branco_Concreto", 0, 13f, -3f, 1.4f, 3f, 5f);
            F.M(r, "Cupula_Senado", "Semiesfera", "Branco_Concreto", F.V(-12f, 2.4f, 1f), F.V(11f, 7f, 11f));
            F.M(r, "Cuia_Camara", "Cuia", "Branco_Concreto", F.V(13f, 2.4f, 1f), F.V(15f, 8f, 15f));
            F.Cubo(r, "Rampa", "Branco_Concreto", F.V(0, 1.2f, 13.5f), F.V(5f, .25f, 11.5f), F.V(12.3f, 0, 0));
            F.Salvar(r.gameObject, "Predios");
        }

        // Inspirado nos palácios (Itamaraty/Alvorada): caixa de vidro, laje solta e arcada branca em volta.
        static void PalacioDosArcos()
        {
            var r = F.Grupo("Brasilia_Palacio_Arcos").transform;
            F.Caixa(r, "Espelho_Dagua", "Petroleo_Claro", 0, 0, 12.5f, 30f, .06f, 7f);
            F.Caixa(r, "Piso", "Branco_Concreto", 0, 0, 0, 30f, .5f, 16f);
            F.Caixa(r, "Salao", "Vidro", 0, .5f, 0, 22f, 6.5f, 9f);
            for (int i = 0; i < 9; i++)
            {
                float x = -11f + i * 2.75f;
                F.Caixa(r, "Montante_Frente", "Petroleo", x, .5f, 4.5f, .14f, 6.5f, .12f);
                F.Caixa(r, "Montante_Fundo", "Petroleo", x, .5f, -4.5f, .14f, 6.5f, .12f);
            }
            F.Caixa(r, "Laje", "Branco_Concreto", 0, 7f, 0, 30f, .6f, 16f);
            for (int i = 0; i < 6; i++)
            {
                float x = -12f + i * 4.8f;
                F.M(r, "Arco_Frente_" + (i + 1), "Arco_Palacio", "Branco_Concreto", F.V(x, .5f, 7.4f), Vector3.one);
                F.M(r, "Arco_Fundo_" + (i + 1), "Arco_Palacio", "Branco_Concreto", F.V(x, .5f, -7.4f), Vector3.one);
            }
            for (int i = 0; i < 3; i++)
            {
                float z = -4.8f + i * 4.8f;
                F.M(r, "Arco_Lado_E_" + (i + 1), "Arco_Palacio", "Branco_Concreto", F.V(-14.5f, .5f, z), Vector3.one, F.V(0, 90, 0));
                F.M(r, "Arco_Lado_D_" + (i + 1), "Arco_Palacio", "Branco_Concreto", F.V(14.5f, .5f, z), Vector3.one, F.V(0, 90, 0));
            }
            F.Salvar(r.gameObject, "Predios");
        }

        // Inspirado no Museu Nacional: cúpula branca com um anel-rampa solto na frente.
        static void MuseuDaCupula()
        {
            var r = F.Grupo("Brasilia_Museu_Cupula").transform;
            F.Cilindro(r, "Base", "Cinza_Claro", F.V(0, .1f, 0), F.V(27f, .2f, 27f));
            F.M(r, "Cupula", "Semiesfera", "Branco_Concreto", F.V(0, .2f, 0), F.V(20f, 20f, 20f));
            F.M(r, "Anel", "Arco_Anel", "Branco_Concreto", F.V(0, 4.2f, 0), Vector3.one, F.V(90, 0, 0));
            foreach (float graus in new[] { 30f, 90f, 150f })
            {
                float a = graus * Mathf.Deg2Rad;
                F.Cilindro(r, "Pilar_Do_Anel", "Branco_Concreto", F.V(12.2f * Mathf.Cos(a), 2.1f, 12.2f * Mathf.Sin(a)), F.V(.5f, 3.9f, .5f));
            }
            F.Caixa(r, "Entrada", "Petroleo_Escuro", 0, .2f, 9.5f, 2.6f, 2.8f, 1.2f);
            F.Caixa(r, "Entrada_Moldura", "Branco_Concreto", 0, .2f, 9.6f, 3.2f, 3.1f, 1.1f);
            F.Salvar(r.gameObject, "Predios");
        }

        // Inspirada na Ponte JK: três arcos cruzando o tabuleiro em diagonal, alternados.
        static void PonteDosArcos()
        {
            var r = F.Grupo("Brasilia_Ponte_Arcos").transform;
            var agua = F.Plano(r, "Agua", "Agua", F.V(0, .05f, 0), 72f, 30f);
            agua.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            F.Caixa(r, "Tabuleiro", "Cinza_Claro", 0, 3.5f, 0, 66f, .7f, 9f);
            F.Caixa(r, "Pista", "Asfalto", 0, 4.2f, 0, 66f, .05f, 6.6f);
            foreach (float z in new[] { -4.3f, 4.3f })
                F.Caixa(r, "Guarda_Corpo", "Branco_Concreto", 0, 4.2f, z, 66f, .7f, .2f);
            for (int i = 0; i < 3; i++)
            {
                float x = -22f + i * 22f;
                F.M(r, "Arco_" + (i + 1), "Arco_Ponte", "Branco_Concreto", F.V(x, 0, 0), Vector3.one, F.V(0, i % 2 == 0 ? 28 : -28, 0));
                F.Caixa(r, "Pilar_" + (i + 1), "Cinza", x - 11f, 0, 0, 1.4f, 3.5f, 5f);
            }
            F.Caixa(r, "Pilar_4", "Cinza", 33f - 1f, 0, 0, 1.4f, 3.5f, 5f);
            F.Salvar(r.gameObject, "Predios");
        }
    }
}
